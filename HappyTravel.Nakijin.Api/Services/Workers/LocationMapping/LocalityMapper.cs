using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.LocationNameNormalizer;
using HappyTravel.Nakijin.Api.Comparers;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Api.Models;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;
using HappyTravel.Nakijin.Api.Services.StaticDataPublication;
using HappyTravel.Nakijin.Api.Services.Validators;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers.LocationMapping
{
    public class LocalityMapper : ILocalityMapper
    {
        public LocalityMapper(NakijinContext context, ILocationMapperDataRetrieveService locationMapperDataRetrieveService,
            ILocationNameNormalizer locationNameNormalizer, MultilingualDataHelper multilingualDataHelper, LocationChangePublisher locationChangePublisher,
            ILocalityValidator localityValidator,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<LocalityMapper>();
            _locationNameNormalizer = locationNameNormalizer;
            _multilingualDataHelper = multilingualDataHelper;
            _locationMapperDataRetrieveService = locationMapperDataRetrieveService;
            _locationChangePublisher = locationChangePublisher;
            _localityValidator = localityValidator;
        }


        public async Task Map(Suppliers supplier, Tracer tracer, TelemetrySpan parentSpan, CancellationToken cancellationToken)
        {
            using var localityMappingSpan =
                tracer.StartActiveSpan("Map Localities", SpanKind.Internal, parentSpan);
            _logger.LogMappingLocalitiesStart(supplier.ToString());

            var countries = await _locationMapperDataRetrieveService.GetCountries();

            foreach (var country in countries)
            {
                _logger.LogMappingLocalitiesOfSpecifiedCountryStart(supplier.ToString(), country.Code);

                var changedLocalityPairs = new Dictionary<int, int>();
                var dbNormalizedLocalities = await _locationMapperDataRetrieveService.GetNormalizedLocalitiesByCountry(country.Code, cancellationToken);
                var notSuppliersLocalities = dbNormalizedLocalities
                    .Where(l => !l.SupplierLocalityCodes.ContainsKey(supplier)).ToList();
                var suppliersLocalities = dbNormalizedLocalities
                    .Where(l => l.SupplierLocalityCodes.ContainsKey(supplier)).ToList();

                var localities = await _context.RawAccommodations
                    .Where(ac => ac.Supplier == supplier && ac.LocalityNames != null && ac.CountryCode == country.Code)
                    .Select(ac
                        => new
                        {
                            CountryCode = ac.CountryCode,
                            CountryNames = ac.CountryNames,
                            LocalityCode = ac.SupplierLocalityCode,
                            LocalityNames = ac.LocalityNames
                        })
                    .Distinct().ToListAsync(cancellationToken);

                localities = localities.GroupBy(l => l.LocalityNames.En).Select(l => l.First()).ToList();

                var localitiesToUpdate = new List<Locality>();
                var newLocalities = new List<Locality>();
                var utcDate = DateTime.UtcNow;

                foreach (var locality in localities)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var defaultCountryName = locality.CountryNames.GetValueOrDefault(Constants.DefaultLanguageCode);
                    var defaultLocalityName = locality.LocalityNames.GetValueOrDefault(Constants.DefaultLanguageCode);
                    var normalizedLocalityName = _locationNameNormalizer.GetNormalizedLocalityName(defaultCountryName, defaultLocalityName);
                    var normalizedCountryName = _locationNameNormalizer.GetNormalizedCountryName(defaultCountryName);

                    if (!_localityValidator.IsNormalizedValid(normalizedCountryName, normalizedLocalityName))
                    {
                        _logger.LogMappingInvalidLocality(defaultLocalityName, defaultCountryName, supplier.ToString(),
                            JsonSerializer.Serialize(new {country.Code, country.Id, country.Name}), JsonSerializer.Serialize(locality));
                        continue;
                    }

                    var dbNotSuppliersLocality =
                        notSuppliersLocalities.FirstOrDefault(l => l.Names.En == normalizedLocalityName);
                    var dbSuppliersLocality =
                        suppliersLocalities.FirstOrDefault(l => l.Names.En == normalizedLocalityName);

                    var dbLocality = new Locality
                    {
                        Names = _multilingualDataHelper.NormalizeLocalityMultilingualNames(defaultCountryName, locality.LocalityNames),
                        IsActive = true,
                        Modified = utcDate
                    };
                    if (dbNotSuppliersLocality != default)
                    {
                        dbLocality.Id = dbNotSuppliersLocality.Id;
                        dbLocality.CountryId = dbNotSuppliersLocality.CountryId;
                        dbLocality.Names = MultiLanguageHelpers.MergeMultilingualStrings(dbLocality.Names, dbNotSuppliersLocality.Names);
                        dbLocality.SupplierLocalityCodes =
                            new Dictionary<Suppliers, string>(dbNotSuppliersLocality.SupplierLocalityCodes);
                        dbLocality.SupplierLocalityCodes.TryAdd(supplier, locality.LocalityCode);
                        if (dbSuppliersLocality != default)
                        {
                            changedLocalityPairs.Add(dbSuppliersLocality.Id, dbNotSuppliersLocality.Id);

                            foreach (var sup in dbSuppliersLocality.SupplierLocalityCodes)
                                dbLocality.SupplierLocalityCodes.TryAdd(sup.Key, sup.Value);

                            dbSuppliersLocality.IsActive = false;
                            localitiesToUpdate.Add(dbSuppliersLocality);
                        }

                        localitiesToUpdate.Add(dbLocality);
                    }
                    else if (dbSuppliersLocality == default)
                    {
                        dbLocality.Created = utcDate;
                        dbLocality.CountryId = country.Id;
                        dbLocality.SupplierLocalityCodes = new Dictionary<Suppliers, string>
                            {{supplier, locality.LocalityCode}};
                        newLocalities.Add(dbLocality);
                    }
                }

                // TODO: Remove Distinct 
                _context.UpdateRange(localitiesToUpdate.Distinct(new LocalityComparer()));
                _context.AddRange(newLocalities.Distinct(new LocalityComparer()));
                await ChangeLocalityDependencies(changedLocalityPairs, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                await _locationChangePublisher.PublishRemovedLocalities(changedLocalityPairs.Keys.ToList());
                await _locationChangePublisher.PublishAddedLocalities(newLocalities
                    .Distinct(new LocalityComparer())
                    .Select(l => new LocalityData(l.Id, l.Names.En, country.Name, country.Code))
                    .ToList());

                _context.ChangeTracker.Entries()
                    .Where(e => e.Entity != null)
                    .Where(e => e.State != EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = EntityState.Detached);

                localityMappingSpan.AddEvent($"Done mapping localities of country with code {country.Code}");

                _logger.LogMappingLocalitiesOfSpecifiedCountryFinish(supplier.ToString(), country.Code);
            }

            _logger.LogMappingLocalitiesFinish(supplier.ToString());
        }


        private async Task ChangeLocalityDependencies(Dictionary<int, int> localityChangedPairs, CancellationToken cancellationToken)
        {
            var utcDate = DateTime.UtcNow;
            var dbLocalityZones = await _context.LocalityZones
                .Where(lz => localityChangedPairs.Keys.Contains(lz.LocalityId)).Select(lz => new
                {
                    LocalityZoneId = lz.Id,
                    LocalityId = lz.LocalityId
                }).ToListAsync(cancellationToken);

            var localityZones = dbLocalityZones.Select(lz => new LocalityZone
            {
                Id = lz.LocalityZoneId,
                LocalityId = localityChangedPairs[lz.LocalityId],
                Modified = utcDate
            }).ToList();

            foreach (var localityZone in localityZones)
            {
                _context.Attach(localityZone);
                _context.Entry(localityZone).Property(lz => lz.LocalityId).IsModified = true;
                _context.Entry(localityZone).Property(lz => lz.Modified).IsModified = true;
            }

            var dbAccommodations = await _context.Accommodations
                .Where(ac => ac.LocalityId != null && localityChangedPairs.Keys.Contains(ac.LocalityId.Value))
                .Select(ac => new
                {
                    AccommodationId = ac.Id,
                    LocalityId = ac.LocalityId
                }).ToListAsync(cancellationToken);

            var accommodations = dbAccommodations.Select(ac => new RichAccommodationDetails
            {
                Id = ac.AccommodationId,
                LocalityId = localityChangedPairs[ac.LocalityId!.Value],
                Modified = utcDate
            }).ToList();

            foreach (var accommodation in accommodations)
            {
                _context.Attach(accommodation);
                _context.Entry(accommodation).Property(l => l.LocalityId).IsModified = true;
                _context.Entry(accommodation).Property(l => l.Modified).IsModified = true;
            }
        }


        private readonly ILocalityValidator _localityValidator;
        private readonly ILogger<LocalityMapper> _logger;
        private readonly ILocationMapperDataRetrieveService _locationMapperDataRetrieveService;
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly MultilingualDataHelper _multilingualDataHelper;
        private readonly LocationChangePublisher _locationChangePublisher;
        private readonly NakijinContext _context;
    }
}