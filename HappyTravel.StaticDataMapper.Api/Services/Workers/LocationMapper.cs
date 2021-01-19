using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.StaticDataMapper.Data.Models;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Infrastructure;
using LocationNameNormalizer;
using LocationNameNormalizer.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HappyTravel.MultiLanguage;
using HappyTravel.StaticDataMapper.Api.Comparers;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public class LocationMapper : ILocationMapper
    {
        public LocationMapper(NakijinContext context, ICountriesCache countriesCache, ILocalitiesCache localitiesCache,
            ILocalityZonesCache localityZonesCache,
            ILocationNameNormalizer locationNameNormalizer, IOptions<AccommodationsPreloaderOptions> options,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _countriesCache = countriesCache;
            _localitiesCache = localitiesCache;
            _localityZonesCache = localityZonesCache;
            _batchSize = options.Value.BatchSize;
            _locationNameNormalizer = locationNameNormalizer;
            _logger = loggerFactory.CreateLogger<LocationMapper>();
        }


        public async Task MapLocations(Suppliers supplier, CancellationToken cancellationToken = default)
        {
            try
            {
                await MapCountries(supplier, cancellationToken);
                await MapLocalities(supplier, cancellationToken);
                await MapLocalityZones(supplier, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.Log(LogLevel.Information,
                    $"Mapping locations of {supplier.ToString()} was canceled by client request.");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"Mapping locations of {supplier.ToString()} was stopped because of {ex.Message}");
            }
        }

        private async Task MapCountries(Suppliers supplier, CancellationToken cancellationToken)
        {
            await ConstructCountriesCache();

            var countries = await _context.RawAccommodations.Where(ac => ac.Supplier == supplier)
                .Select(ac
                    => new
                    {
                        Names = ac.CountryNames,
                        Code = ac.CountryCode
                    })
                .Distinct().ToListAsync(cancellationToken);

            var existingCountries = new List<Country>();
            var newCountries = new List<Country>();

            foreach (var country in countries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var defaultName = country.Names.GetValueOrDefault(DefaultLanguageCode);
                var code = _locationNameNormalizer.GetNormalizedCountryCode(defaultName, country.Code);
                var dbCountry = new Country
                {
                    Code = code,
                    Names = NormalizeCountryMultiLingualNames(country.Names),
                    IsActive = true
                };

                // Maybe after testing we will change to get data from db
                var cached = await _countriesCache.Get(code);
                if (cached != default)
                {
                    dbCountry.Id = cached.Id;
                    dbCountry.Names = MultiLanguageHelpers.Merge(dbCountry.Names, cached.Names);
                    dbCountry.SupplierCountryCodes = new Dictionary<Suppliers, string>(cached.SupplierCountryCodes);
                    // TODO: Test and remove this 
                    dbCountry.SupplierCountryCodes.TryAdd(supplier, code);

                    existingCountries.Add(dbCountry);
                }
                else
                {
                    dbCountry.SupplierCountryCodes = new Dictionary<Suppliers, string> {{supplier, code}};
                    newCountries.Add(dbCountry);
                }
            }

            // TODO: Remove Distinct ( in connectors may be the same data in different forms normalized or not that is why needed distinct here )
            // Change distinct to deactivate not needed and change relation with accommodation
            _context.UpdateRange(existingCountries.Distinct(new CountryComparer()));
            _context.AddRange(newCountries.Distinct(new CountryComparer()));
            await _context.SaveChangesAsync(cancellationToken);
        }


        private async Task MapLocalities(Suppliers supplier, CancellationToken cancellationToken)
        {
            await ConstructCountriesCache();
            await ConstructLocalitiesCache();

            // After testing of all connectors maybe will be changed to batches 
            var localities = await _context.RawAccommodations
                .Where(ac => ac.Supplier == supplier && ac.LocalityNames != null)
                .Select(ac
                    => new
                    {
                        CountryCode = ac.CountryCode,
                        CountryNames = ac.CountryNames,
                        LocalityCode = ac.SupplierLocalityCode,
                        LocalityNames = ac.LocalityNames
                    })
                .Distinct().ToListAsync(cancellationToken);
            var existingLocalities = new List<Locality>();
            var newLocalities = new List<Locality>();
            foreach (var locality in localities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var defaultCountryName = locality.CountryNames.GetValueOrDefault(DefaultLanguageCode);
                // TODO: review and remove 
                var countryCode =
                    _locationNameNormalizer.GetNormalizedCountryCode(defaultCountryName, locality.CountryCode);
                var defaultLocalityName = locality.LocalityNames.GetValueOrDefault(DefaultLanguageCode);
                var normalizedLocalityName =
                    _locationNameNormalizer.GetNormalizedLocalityName(defaultCountryName, defaultLocalityName);

                var cached = await _localitiesCache.Get(countryCode, normalizedLocalityName);

                var dbLocality = new Locality
                {
                    Names = NormalizeLocalityMultilingualNames(defaultCountryName, locality.LocalityNames),
                    IsActive = true
                };
                if (cached != default)
                {
                    dbLocality.Id = cached.Id;
                    dbLocality.CountryId = cached.CountryId;
                    dbLocality.Names = MultiLanguageHelpers.Merge(dbLocality.Names, cached.Names);
                    dbLocality.SupplierLocalityCodes =
                        new Dictionary<Suppliers, string>(cached.SupplierLocalityCodes);
                    dbLocality.SupplierLocalityCodes.TryAdd(supplier, locality.LocalityCode);
                    existingLocalities.Add(dbLocality);
                }
                else
                {
                    var cachedCountry = await _countriesCache.Get(countryCode);
                    dbLocality.CountryId = cachedCountry!.Id;
                    dbLocality.SupplierLocalityCodes = new Dictionary<Suppliers, string>
                        {{supplier, locality.LocalityCode}};
                    newLocalities.Add(dbLocality);
                }
            }

            // TODO: Remove Distinct 
            _context.UpdateRange(existingLocalities.Distinct(new LocalityComparer()));
            _context.AddRange(newLocalities.Distinct(new LocalityComparer()));
            await _context.SaveChangesAsync();
        }

        private async Task MapLocalityZones(Suppliers supplier, CancellationToken cancellationToken)
        {
            await ConstructCountriesCache();
            await ConstructLocalitiesCache();

            var localityZones = await _context.RawAccommodations
                .Where(ac => ac.Supplier == supplier && ac.LocalityZoneNames != null)
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

            var normalizedLocalityZones = localityZones.Select(lz =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var defaultCountryName = lz.CountryNames.GetValueOrDefault(DefaultLanguageCode);
                var countryCode =
                    _locationNameNormalizer.GetNormalizedCountryCode(defaultCountryName, lz.CountryCode);
                var defaultLocalityName = lz.LocalityNames.GetValueOrDefault(DefaultLanguageCode);
                var normalizedLocalityName =
                    _locationNameNormalizer.GetNormalizedLocalityName(defaultCountryName, defaultLocalityName);
                var cachedLocality = _localitiesCache.Get(countryCode, normalizedLocalityName).Result;
                var normalizedNames = NormalizeLocalityZoneMultilingualNames(lz.LocalityZoneNames);
                var defaultNormalized = normalizedNames.GetValueOrDefault(DefaultLanguageCode);
                return new
                {
                    LocalityId = cachedLocality!.Id,
                    Names = normalizedNames,
                    Code = lz.LocalityZoneCode,
                    DefaultName = defaultNormalized
                };
            }).ToList();

            var normalizedKeyNames = normalizedLocalityZones.Select(nl => nl.DefaultName + nl.LocalityId).ToList();

            var existingDbZones = await _context.LocalityZones
                .Where(lz => normalizedKeyNames.Contains(lz.Names.En + lz.LocalityId))
                .ToListAsync();

            var localityZonesToUpdate = (from nz in normalizedLocalityZones
                join dz in existingDbZones
                    on nz.LocalityId + nz.DefaultName equals dz.LocalityId + dz.Names.En
                let sz = dz.SupplierLocalityZoneCodes.TryAdd(supplier, nz.Code)
                select new LocalityZone
                {
                    Id = dz.Id,
                    LocalityId = dz.LocalityId,
                    SupplierLocalityZoneCodes = dz.SupplierLocalityZoneCodes,
                    Names = MultiLanguageHelpers.Merge(nz.Names, dz.Names),
                    IsActive = true 
                }).ToList();

            var newLocalityZones = normalizedLocalityZones
                .Where(nz =>
                    !localityZonesToUpdate.Select(dz => dz.LocalityId + dz.Names.En)
                        .Contains(nz.LocalityId + nz.DefaultName))
                .Select(nz => new LocalityZone
                {
                    LocalityId = nz.LocalityId,
                    Names = nz.Names,
                    SupplierLocalityZoneCodes = new Dictionary<Suppliers, string> {{supplier, nz.Code}},
                    IsActive = true,
                });

            _context.UpdateRange(localityZonesToUpdate.Distinct(new LocalityZoneComparer()));
            _context.AddRange(newLocalityZones.Distinct(new LocalityZoneComparer()));

            await _context.SaveChangesAsync(cancellationToken);
        }


        private MultiLanguage<string> NormalizeCountryMultiLingualNames(MultiLanguage<string> countryNames)
        {
            var normalized = new MultiLanguage<string>();
            var allNames = countryNames.GetAll();

            foreach (var name in allNames)
                normalized.TrySetValue(name.languageCode, _locationNameNormalizer.GetNormalizedCountryName(name.value));

            return normalized;
        }

        private MultiLanguage<string> NormalizeLocalityMultilingualNames(string defaultCountry,
            MultiLanguage<string> localityNames)
        {
            var normalizedLocalityNames = new MultiLanguage<string>();
            var allNames = localityNames.GetAll();

            foreach (var name in allNames)
                normalizedLocalityNames.TrySetValue(name.languageCode,
                    _locationNameNormalizer.GetNormalizedLocalityName(defaultCountry, name.value));

            return normalizedLocalityNames;
        }

        private MultiLanguage<string> NormalizeLocalityZoneMultilingualNames(MultiLanguage<string> localityZoneNames)
        {
            var normalizedLocalityZoneNames = new MultiLanguage<string>();
            var allNames = localityZoneNames.GetAll();

            foreach (var name in allNames)
                normalizedLocalityZoneNames.TrySetValue(name.languageCode, name.value.ToNormalizedName());

            return normalizedLocalityZoneNames;
        }

        public async Task ConstructLocationsCache()
        {
            await ConstructCountriesCache();
            await ConstructLocalitiesCache();
            await ConstructLocalityZonesCache();
        }


        private async Task ConstructCountriesCache()
        {
            // Countries data are not large, so not need to get by batches 
            var countries = await _context.Countries.ToListAsync();

            foreach (var country in countries)
                await _countriesCache.Set(country.Code, country);
        }

        private async Task ConstructLocalitiesCache()
        {
            var localities = new List<KeyValuePair<string, Locality>>();
            int skip = 0;

            do
            {
                localities = await _context.Localities.Join(_context.Countries, l => l.CountryId, c => c.Id, (l, c)
                        => new
                        {
                            CountryCode = c.Code,
                            Locality = l
                        })
                    .OrderBy(l => l.Locality.Id)
                    .Skip(skip)
                    .Take(_batchSize)
                    .Select(l => new KeyValuePair<string, Locality>(l.CountryCode, l.Locality))
                    .ToListAsync();

                skip += localities.Count;

                foreach (var locality in localities)
                {
                    var defaultLocality = locality.Value.Names.GetValueOrDefault(DefaultLanguageCode);
                    await _localitiesCache.Set(locality.Key, defaultLocality, locality.Value);
                }
            } while (localities.Count > 0);
        }

        private async Task ConstructLocalityZonesCache()
        {
            var localityZones = new List<KeyValuePair<(string CountryCode, string LocalityName), LocalityZone>>();
            int skip = 0;
            do
            {
                localityZones = await (from z in _context.LocalityZones
                        join l in _context.Localities on z.LocalityId equals l.Id
                        join c in _context.Countries on l.CountryId equals c.Id
                        select new
                            KeyValuePair<(string CountryCode, string LocalityName), LocalityZone>(
                                ValueTuple.Create(c.Code, l.Names.En),z))
                    .Skip(skip).Take(_batchSize).ToListAsync();

                skip += localityZones.Count();

                foreach (var localityZone in localityZones)
                {
                    var defaultLocalityZoneName = localityZone.Value.Names.GetValueOrDefault(DefaultLanguageCode);
                    await _localityZonesCache.Set(localityZone.Key.Item1, localityZone.Key.Item2,
                        defaultLocalityZoneName, localityZone.Value);
                }
            } while (localityZones.Count > 0);
        }

        private const string DefaultLanguageCode = "en";
        private readonly ILocalitiesCache _localitiesCache;
        private readonly ICountriesCache _countriesCache;
        private readonly ILocalityZonesCache _localityZonesCache;
        private readonly NakijinContext _context;
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly ILogger<LocationMapper> _logger;
        private readonly int _batchSize;
    }
}