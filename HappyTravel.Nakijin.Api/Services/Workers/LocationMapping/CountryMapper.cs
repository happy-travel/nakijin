using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.LocationNameNormalizer;
using HappyTravel.Nakijin.Api.Comparers;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Api.Models;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;
using HappyTravel.Nakijin.Api.Services.StaticDataPublication;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers.LocationMapping
{
    public class CountryMapper : ICountryMapper
    {
        public CountryMapper(NakijinContext context, ILocationMapperDataRetrieveService locationMapperDataRetrieveService,
            ILocationNameNormalizer locationNameNormalizer, MultilingualDataHelper multilingualDataHelper, LocationChangePublisher locationChangePublisher,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<CountryMapper>();
            _locationNameNormalizer = locationNameNormalizer;
            _multilingualDataHelper = multilingualDataHelper;
            _locationMapperDataRetrieveService = locationMapperDataRetrieveService;
            _locationChangePublisher = locationChangePublisher;
        }


        public async Task Map(Suppliers supplier, Tracer tracer, TelemetrySpan parentSpan, CancellationToken cancellationToken)
        {
            using var countryMappingSpan = tracer.StartActiveSpan("Map Countries", SpanKind.Internal, parentSpan);
            _logger.LogMappingCountriesStart(
                $"Started Mapping countries of {supplier.ToString()}.");

            var countries = await _locationMapperDataRetrieveService.GetNormalizedCountries();

            var countryPairsChanged = new Dictionary<int, int>();
            var notSuppliersCountries = countries.Where(c => !c.SupplierCountryCodes.ContainsKey(supplier)).ToList();
            var suppliersCountries = countries.Where(c => c.SupplierCountryCodes.ContainsKey(supplier)).ToList();

            var countriesToMap = await _context.RawAccommodations.Where(ac => ac.Supplier == supplier)
                .Select(ac
                    => new
                    {
                        Names = ac.CountryNames,
                        Code = ac.CountryCode
                    })
                .Distinct().ToListAsync(cancellationToken);

            countriesToMap = countriesToMap.GroupBy(c => c.Code).Select(c => c.First()).ToList();
            var countriesToUpdate = new List<Country>();
            var newCountries = new List<Country>();

            var utcDate = DateTime.UtcNow;
            foreach (var country in countriesToMap)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var defaultName = country.Names.GetValueOrDefault(Constants.DefaultLanguageCode);
                if (!defaultName.IsValid())
                    continue;

                var code = _locationNameNormalizer.GetNormalizedCountryCode(defaultName, country.Code);
                var dbCountry = new Country
                {
                    Code = code,
                    Names = _multilingualDataHelper.NormalizeCountryMultiLingualNames(country.Names),
                    IsActive = true,
                    Modified = utcDate
                };

                var dbNotSuppliersCountry = notSuppliersCountries.FirstOrDefault(c => c.Code == code);
                var dbSuppliersCountry = suppliersCountries.FirstOrDefault(c => c.Code == code);
                if (dbNotSuppliersCountry != default)
                {
                    dbCountry.Id = dbNotSuppliersCountry.Id;
                    dbCountry.Names = MultiLanguageHelpers.MergeMultilingualStrings(dbCountry.Names, dbNotSuppliersCountry.Names);
                    dbCountry.SupplierCountryCodes =
                        new Dictionary<Suppliers, string>(dbNotSuppliersCountry.SupplierCountryCodes);
                    dbCountry.SupplierCountryCodes.TryAdd(supplier, code);

                    if (dbSuppliersCountry != default)
                    {
                        countryPairsChanged.Add(dbSuppliersCountry.Id, dbCountry.Id);
                        foreach (var sup in dbSuppliersCountry.SupplierCountryCodes)
                            dbCountry.SupplierCountryCodes.TryAdd(sup.Key, sup.Value);
                        dbSuppliersCountry.IsActive = false;
                        countriesToUpdate.Add(dbSuppliersCountry);
                    }

                    countriesToUpdate.Add(dbCountry);
                }
                else if (dbSuppliersCountry == default)
                {
                    dbCountry.SupplierCountryCodes = new Dictionary<Suppliers, string> {{supplier, code}};
                    dbCountry.Created = utcDate;
                    newCountries.Add(dbCountry);
                }
            }

            // TODO: Remove Distinct ( in connectors may be the same data in different forms normalized or not that is why needed distinct here )

            _context.UpdateRange(countriesToUpdate.Distinct(new CountryComparer()));
            _context.AddRange(newCountries.Distinct(new CountryComparer()));
            await ChangeCountryDependencies(countryPairsChanged, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await _locationChangePublisher.PublishAddedCountries(newCountries
                .Distinct(new CountryComparer())
                .Select(c => new CountryData(c.Id, c.Names.En, c.Code))
                .ToList());

            await _locationChangePublisher.PublishRemovedCountries(countryPairsChanged.Keys.ToList());

            _logger.LogMappingCountriesFinish(
                $"Finished Mapping countries of {supplier.ToString()}.");
        }


        private async Task ChangeCountryDependencies(Dictionary<int, int> countryChangedPairs, CancellationToken cancellationToken)
        {
            var utcDate = DateTime.UtcNow;
            var dbLocalities = await _context.Localities
                .Where(l => countryChangedPairs.Keys.Contains(l.CountryId))
                .Select(l => new
                {
                    LocalityId = l.Id,
                    CountryId = l.CountryId
                }).ToListAsync(cancellationToken);

            var localities = dbLocalities.Select(l => new Locality
            {
                Id = l.LocalityId,
                CountryId = countryChangedPairs[l.CountryId],
                Modified = utcDate
            });

            foreach (var locality in localities)
            {
                _context.Attach(locality);
                _context.Entry(locality).Property(l => l.CountryId).IsModified = true;
                _context.Entry(locality).Property(l => l.Modified).IsModified = true;
            }

            var dbAccommodations = await _context.Accommodations
                .Where(ac => countryChangedPairs.Keys.Contains(ac.CountryId))
                .Select(ac => new
                {
                    AccommodationId = ac.Id,
                    CountryId = ac.CountryId
                }).ToListAsync(cancellationToken);

            var accommodations = dbAccommodations.Select(ac => new RichAccommodationDetails
            {
                Id = ac.AccommodationId,
                CountryId = countryChangedPairs[ac.CountryId],
                Modified = utcDate
            }).ToList();

            foreach (var accommodation in accommodations)
            {
                _context.Attach(accommodation);
                _context.Entry(accommodation).Property(l => l.CountryId).IsModified = true;
                _context.Entry(accommodation).Property(l => l.Modified).IsModified = true;
            }
        }


        private readonly ILogger<CountryMapper> _logger;
        private readonly ILocationMapperDataRetrieveService _locationMapperDataRetrieveService;
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly MultilingualDataHelper _multilingualDataHelper;
        private readonly LocationChangePublisher _locationChangePublisher;
        private readonly NakijinContext _context;
    }
}