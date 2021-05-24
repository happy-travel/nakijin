using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.LocationNameNormalizer;
using HappyTravel.LocationNameNormalizer.Extensions;
using HappyTravel.Nakijin.Api.Comparers;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Api.Models;
using HappyTravel.Nakijin.Api.Services.StaticDataPublication;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers.LocationMapping
{
    public class LocalityZoneMapper : ILocalityZoneMapper
    {
        public LocalityZoneMapper(NakijinContext context, ILocationMapperDataRetrieveService locationMapperDataRetrieveService,
            ILocationNameNormalizer locationNameNormalizer, MultilingualDataHelper multilingualDataHelper, LocationChangePublisher locationChangePublisher,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<LocalityZoneMapper>();
            _locationNameNormalizer = locationNameNormalizer;
            _multilingualDataHelper = multilingualDataHelper;
            _locationMapperDataRetrieveService = locationMapperDataRetrieveService;
            _locationChangePublisher = locationChangePublisher;
        }


        public async Task Map(Suppliers supplier, Tracer tracer, TelemetrySpan parentSpan, CancellationToken cancellationToken)
        {
            using var localityZoneMappingSpan =
                tracer.StartActiveSpan("Map Locality zones", SpanKind.Internal, parentSpan);

            _logger.LogMappingLocalityZonesStart(
                $"Started Mapping locality zones of {supplier.ToString()}.");

            var countries = await _locationMapperDataRetrieveService.GetCountries();

            foreach (var country in countries)
            {
                _logger.LogMappingLocalityZonesOfSpecifiedCountryStart(
                    $"Started Mapping locality zones of {supplier.ToString()} of country with code {country.Code}.");

                var changedLocalityZonesPairs = new Dictionary<int, int>();
                var countryLocalities = await _locationMapperDataRetrieveService.GetNormalizedLocalitiesByCountry(country.Code, cancellationToken);
                var dbNormalizedLocalityZones =
                    await _locationMapperDataRetrieveService.GetNormalizedLocalityZonesByCountry(country.Code, cancellationToken);
                var notSuppliersLocalityZones = dbNormalizedLocalityZones
                    .Where(l => !l.LocalityZone.SupplierLocalityZoneCodes.ContainsKey(supplier)).ToList();
                var suppliersLocalityZones = dbNormalizedLocalityZones
                    .Where(l => l.LocalityZone.SupplierLocalityZoneCodes.ContainsKey(supplier)).ToList();

                var localityZonesToMap = await _context.RawAccommodations
                    .Where(ac => ac.Supplier == supplier && ac.LocalityZoneNames != null &&
                        ac.CountryCode == country.Code)
                    .Select(ac
                        => new
                        {
                            LocalityNames = ac.LocalityNames,
                            CountryCode = ac.CountryCode,
                            CountryNames = ac.CountryNames,
                            LocalityZoneNames = ac.LocalityZoneNames,
                            LocalityZoneCode = ac.SupplierLocalityZoneCode
                        })
                    .Distinct().ToListAsync(cancellationToken);

                localityZonesToMap = localityZonesToMap
                    .GroupBy(lz => new {LocalityName = lz.LocalityNames.En, LocalityZoneName = lz.LocalityZoneNames.En})
                    .Select(lz => lz.First())
                    .ToList();

                var localityZonesToUpdate = new List<LocalityZone>();
                var localityZonesToAdd = new List<LocalityZone>();
                var utcDate = DateTime.UtcNow;

                foreach (var zone in localityZonesToMap)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var defaultCountryName = zone.CountryNames.GetValueOrDefault(Constants.DefaultLanguageCode);
                    var defaultLocalityName = zone.LocalityNames.GetValueOrDefault(Constants.DefaultLanguageCode);
                    var normalizedLocalityName =
                        _locationNameNormalizer.GetNormalizedLocalityName(defaultCountryName, defaultLocalityName);
                    var defaultLocalityZone = zone.LocalityZoneNames.GetValueOrDefault(Constants.DefaultLanguageCode);
                    var normalizedLocalityZone = defaultLocalityZone.ToNormalizedName();
                    if (!normalizedLocalityZone.IsValid())
                        continue;

                    var dbNotSuppliersZone = notSuppliersLocalityZones.FirstOrDefault(lz
                        => lz.DefaultLocality == normalizedLocalityName
                        && lz.LocalityZone.Names.En == normalizedLocalityZone);
                    var dbSuppliersZone = suppliersLocalityZones.FirstOrDefault(lz
                        => lz.DefaultLocality == normalizedLocalityName
                        && lz.LocalityZone.Names.En == normalizedLocalityZone);

                    var dbLocalityZone = new LocalityZone
                    {
                        Names = _multilingualDataHelper.NormalizeCountryMultiLingualNames(zone.LocalityZoneNames),
                        IsActive = true,
                        Modified = utcDate
                    };

                    if (dbNotSuppliersZone != default)
                    {
                        dbLocalityZone.Id = dbNotSuppliersZone.LocalityZone.Id;
                        dbLocalityZone.LocalityId = dbNotSuppliersZone.LocalityZone.LocalityId;
                        dbLocalityZone.Names =
                            MultiLanguageHelpers.MergeMultilingualStrings(dbLocalityZone.Names,
                                dbNotSuppliersZone.LocalityZone.Names);
                        dbLocalityZone.SupplierLocalityZoneCodes =
                            new Dictionary<Suppliers, string>(dbNotSuppliersZone.LocalityZone
                                .SupplierLocalityZoneCodes);
                        dbLocalityZone.SupplierLocalityZoneCodes.TryAdd(supplier, zone.LocalityZoneCode);
                        if (dbSuppliersZone != default)
                        {
                            changedLocalityZonesPairs.Add(dbSuppliersZone.LocalityZone.Id,
                                dbNotSuppliersZone.LocalityZone.Id);

                            foreach (var sup in dbSuppliersZone.LocalityZone.SupplierLocalityZoneCodes)
                                dbLocalityZone.SupplierLocalityZoneCodes.TryAdd(sup.Key, sup.Value);

                            dbSuppliersZone.LocalityZone.IsActive = false;
                            localityZonesToUpdate.Add(dbSuppliersZone.LocalityZone);
                        }

                        localityZonesToUpdate.Add(dbLocalityZone);
                    }
                    else if (dbSuppliersZone == default)
                    {
                        dbLocalityZone.Created = utcDate;
                        dbLocalityZone.LocalityId =
                            countryLocalities.First(l => l.Names.En == normalizedLocalityName).Id;
                        dbLocalityZone.SupplierLocalityZoneCodes = new Dictionary<Suppliers, string>
                            {{supplier, zone.LocalityZoneCode}};
                        localityZonesToAdd.Add(dbLocalityZone);
                    }
                }

                _context.UpdateRange(localityZonesToUpdate.Distinct(new LocalityZoneComparer()));
                _context.AddRange(localityZonesToAdd.Distinct(new LocalityZoneComparer()));
                await ChangeLocalityZoneDependencies(changedLocalityZonesPairs, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                // await _locationsChangePublisher.PublishRemovedLocalityZones(changedLocalityZonesPairs.Keys.ToList());
                // await _locationsChangePublisher.PublishAddedLocalityZones(localityZonesToAdd
                //     .Distinct(new LocalityZoneComparer())
                //     .Select(lz => new LocalityZoneData(lz.Id, lz.Names.En,
                //         countryLocalities.First(l => l.Id == lz.LocalityId).Names.En, country.Name, country.Code))
                //     .ToList());

                _context.ChangeTracker.Entries()
                    .Where(e => e.Entity != null)
                    .Where(e => e.State != EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = EntityState.Detached);

                localityZoneMappingSpan.AddEvent($"Done mapping locality zones of country with code {country.Code}");

                _logger.LogMappingLocalityZonesOfSpecifiedCountryFinish(
                    $"Finished Mapping locality zones of {supplier.ToString()} of country with code {country.Code}.");
            }

            _logger.LogMappingLocalityZonesFinish(
                $"Finished Mapping locality zones of {supplier.ToString()}.");
        }


        async Task ChangeLocalityZoneDependencies(Dictionary<int, int> localityZoneChangedPairs, CancellationToken cancellationToken)
        {
            var utcDate = DateTime.UtcNow;

            var dbAccommodations = await _context.Accommodations
                .Where(ac => ac.LocalityZoneId != null &&
                    localityZoneChangedPairs.Keys.Contains(ac.LocalityZoneId.Value))
                .Select(ac => new
                {
                    AccommodationId = ac.Id,
                    LocalityZoneId = ac.LocalityZoneId
                }).ToListAsync(cancellationToken);

            var accommodations = dbAccommodations.Select(ac => new RichAccommodationDetails
            {
                Id = ac.AccommodationId,
                LocalityId = localityZoneChangedPairs[ac.LocalityZoneId!.Value],
                Modified = utcDate
            }).ToList();

            foreach (var accommodation in accommodations)
            {
                _context.Attach(accommodation);
                _context.Entry(accommodation).Property(l => l.LocalityZoneId).IsModified = true;
                _context.Entry(accommodation).Property(l => l.Modified).IsModified = true;
            }
        }


        private readonly ILogger<LocalityZoneMapper> _logger;
        private readonly ILocationMapperDataRetrieveService _locationMapperDataRetrieveService;
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly MultilingualDataHelper _multilingualDataHelper;
        private readonly LocationChangePublisher _locationChangePublisher;
        private readonly NakijinContext _context;
    }
}