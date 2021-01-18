using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.StaticDataMapper.Data.Models;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Api.Infrastructure;
using HappyTravel.StaticDataMapper.Api.Models;
using LocationNameNormalizer;
using LocationNameNormalizer.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public class LocationMapper : ILocationMapper
    {
        public LocationMapper(NakijinContext context, ICountriesCache countriesCache, ILocalitiesCache localitiesCache,
            LocalityZonesCache localityZonesCache,
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
                        Names = ac.Accommodation.RootElement.GetProperty("location").GetProperty("country")
                            .GetString(),
                        Code = ac.Accommodation.RootElement.GetProperty("location").GetProperty("countryCode")
                            .GetString()
                    })
                .Distinct().ToListAsync(cancellationToken);

            var existingCountries = new List<Country>();
            var newCountries = new List<Country>();

            foreach (var country in countries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var defaultName = LanguageHelper.GetValue(country.Names, DefaultLanguageCode);
                var code = _locationNameNormalizer.GetNormalizedCountryCode(defaultName, country.Code);
                var dbCountry = new Country
                {
                    Code = code,
                    Names = JsonDocument.Parse(NormalizeCountryNamesJson(country.Names))
                };

                // Maybe after testing we will change to get data from db
                var cached = await _countriesCache.Get(code);
                if (cached != default)
                {
                    dbCountry.Id = cached.Id;
                    dbCountry.Names =
                        JsonDocument.Parse(LanguageHelper.MergeLanguages(dbCountry.Names.RootElement.ToString(),
                            cached.Names.RootElement.ToString()));
                    dbCountry.SupplierCountryCodes = new Dictionary<Suppliers, string>(cached.SupplierCountryCodes);
                    dbCountry.SupplierCountryCodes.TryAdd(supplier, code);

                    existingCountries.Add(dbCountry);
                }
                else
                {
                    dbCountry.SupplierCountryCodes = new Dictionary<Suppliers, string> {{supplier, code}};
                    newCountries.Add(dbCountry);
                }
            }

            _context.UpdateRange(existingCountries);
            _context.AddRange(newCountries);
            await _context.SaveChangesAsync(cancellationToken);
        }


        private async Task MapLocalities(Suppliers supplier, CancellationToken cancellationToken)
        {
            await ConstructCountriesCache();
            await ConstructLocalitiesCache();

            // After testing maybe will change to batches 
            var localities = await _context.RawAccommodations.Where(ac => ac.Supplier == supplier)
                .Select(ac
                    => new
                    {
                        CountryCode = ac.Accommodation.RootElement.GetProperty("location").GetProperty("countryCode")
                            .GetString(),
                        CountryNames = ac.Accommodation.RootElement.GetProperty("location").GetProperty("country")
                            .GetString(),
                        LocalityCode = ac.Accommodation.RootElement.GetProperty("location").GetProperty("localityCode")
                            .GetString(),
                        LocalityNames = ac.Accommodation.RootElement.GetProperty("location").GetProperty("locality")
                            .GetString()
                    })
                .Distinct().ToListAsync(cancellationToken);

            foreach (var locality in localities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var defaultCountryName = LanguageHelper.GetValue(locality.CountryNames, DefaultLanguageCode);
                var countryCode =
                    _locationNameNormalizer.GetNormalizedCountryCode(defaultCountryName, locality.CountryCode);
                var defaultLocalityName = _locationNameNormalizer.GetNormalizedLocalityName(defaultCountryName,
                    LanguageHelper.GetValue(locality.LocalityNames, DefaultLanguageCode));

                // Maybe after testing will be db call instead of cache 
                var cached = await _localitiesCache.Get(countryCode, defaultLocalityName);

                var existingLocalities = new List<Locality>();
                var newLocalities = new List<Locality>();
                var dbLocality = new Locality
                {
                    Names = JsonDocument.Parse(NormalizeLocalityNamesJson(defaultCountryName, locality.LocalityNames))
                };
                if (cached != default)
                {
                    dbLocality.Id = cached.Id;
                    dbLocality.CountryId = cached.CountryId;
                    dbLocality.Names =
                        JsonDocument.Parse(LanguageHelper.MergeLanguages(dbLocality.Names.RootElement.ToString(),
                            cached.Names.RootElement.ToString()));
                    dbLocality.SupplierLocalityCodes =
                        new Dictionary<Suppliers, string>(cached.SupplierLocalityCodes);
                    dbLocality.SupplierLocalityCodes.TryAdd(supplier, locality.LocalityCode);
                    existingLocalities.Add(dbLocality);
                }
                else
                {
                    var cachedCountry = await _countriesCache.Get(countryCode);
                    dbLocality.CountryId = cachedCountry.Id;
                    dbLocality.SupplierLocalityCodes = new Dictionary<Suppliers, string>
                        {{supplier, locality.LocalityCode}};
                    newLocalities.Add(dbLocality);
                }

                _context.Update(existingLocalities);
                _context.Add(newLocalities);
                await _context.SaveChangesAsync();
            }
        }

        private async Task MapLocalityZones(Suppliers supplier, CancellationToken cancellationToken)
        {
            await ConstructCountriesCache();
            await ConstructLocalitiesCache();

            var localityZones = await _context.RawAccommodations.Where(ac => ac.Supplier == supplier)
                .Select(ac
                    => new
                    {
                        LocalityNames = ac.Accommodation.RootElement.GetProperty("location").GetProperty("locality")
                            .GetString(),
                        CountryCode = ac.Accommodation.RootElement.GetProperty("location").GetProperty("countryCode")
                            .GetString(),
                        CountryNames = ac.Accommodation.RootElement.GetProperty("location").GetProperty("countryName")
                            .GetString(),
                        LocalityZoneNames = ac.Accommodation.RootElement.GetProperty("location")
                            .GetProperty("localityZone")
                            .GetString(),
                        LocalityZoneCode = ac.Accommodation.RootElement.GetProperty("location")
                            .GetProperty("localityZoneCode")
                            .GetString()
                    })
                .Distinct().ToListAsync(cancellationToken);

            var normalizedLocalityZones = localityZones.Select(lz =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var defaultCountryName = LanguageHelper.GetValue(lz.CountryNames, DefaultLanguageCode);
                var countryCode =
                    _locationNameNormalizer.GetNormalizedCountryCode(defaultCountryName, lz.CountryCode);
                var defaultLocalityName = _locationNameNormalizer.GetNormalizedLocalityName(defaultCountryName,
                    LanguageHelper.GetValue(lz.LocalityNames, DefaultLanguageCode));
                var cachedLocality = _localitiesCache.Get(countryCode, defaultLocalityName).Result;
                var normalizedNames = NormalizeLocalityZoneNamesJson(lz.LocalityZoneNames);
                return new
                {
                    LocalityId = cachedLocality.Id,
                    Names = normalizedNames,
                    Code = lz.LocalityZoneCode,
                    DefaultName = LanguageHelper.GetValue(normalizedNames, DefaultLanguageCode)
                };
            }).ToList();

            var existingDbZones = await _context.LocalityZones.Where(lz =>
                normalizedLocalityZones.Select(nl => nl.LocalityId + nl.DefaultName)
                    .Contains(lz.LocalityId + lz.Names.RootElement.GetProperty("en").GetString())).ToListAsync();

            var localityZonesToUpdate = (from nz in normalizedLocalityZones
                join dz in existingDbZones
                    on nz.LocalityId + nz.DefaultName equals dz.LocalityId +
                    LanguageHelper.GetValue(dz.Names.RootElement.ToString(), DefaultLanguageCode)
                let sz = dz.SupplierLocalityZoneCodes.TryAdd(supplier, nz.Code)
                select new LocalityZone
                {
                    Id = dz.Id,
                    LocalityId = dz.LocalityId,
                    SupplierLocalityZoneCodes = dz.SupplierLocalityZoneCodes,
                    Names = JsonDocument.Parse(LanguageHelper.MergeLanguages(nz.Names, dz.Names.RootElement.ToString()))
                }).ToList();

            var newLocalityZones = normalizedLocalityZones
                .Where(nz =>
                    !localityZonesToUpdate.Select(dz => dz.LocalityId +
                            LanguageHelper.GetValue(dz.Names.RootElement.ToString(), DefaultLanguageCode))
                        .Contains(nz.LocalityId + nz.DefaultName))
                .Select(nz => new LocalityZone
                {
                    LocalityId = nz.LocalityId,
                    Names = JsonDocument.Parse(nz.Names),
                    SupplierLocalityZoneCodes = new Dictionary<Suppliers, string> {{supplier, nz.Code}},
                    IsActive = true,
                });

            _context.Update(localityZonesToUpdate);
            _context.AddRange(newLocalityZones);

            await _context.SaveChangesAsync(cancellationToken);
        }


        private string NormalizeCountryNamesJson(string? countryNamesJson)
        {
            if (string.IsNullOrWhiteSpace(countryNamesJson))
                return Constants.DefaultJsonString;

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(countryNamesJson);
            var normalized =
                deserialized.ToDictionary(s => (s.Key, _locationNameNormalizer.GetNormalizedCountryName(s.Value)));
            return JsonConvert.SerializeObject(normalized);
        }

        private string NormalizeLocalityNamesJson(string defaultCountry, string? localityNamesJson)
        {
            if (string.IsNullOrWhiteSpace(localityNamesJson))
                return Constants.DefaultJsonString;

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(localityNamesJson);
            var normalized =
                deserialized.ToDictionary(
                    s => (s.Key, _locationNameNormalizer.GetLocalityNames(defaultCountry, s.Value)));
            return JsonConvert.SerializeObject(normalized);
        }

        private string NormalizeLocalityZoneNamesJson(string? localityZoneNamesJson)
        {
            if (string.IsNullOrWhiteSpace(localityZoneNamesJson))
                return Constants.DefaultJsonString;

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(localityZoneNamesJson);
            var normalized =
                deserialized.ToDictionary(
                    s => (s.Key, s.Value.ToNormalizedName()));
            return JsonConvert.SerializeObject(normalized);
        }


        public async Task ConstructCountriesCache()
        {
            // Countries data are not large, so not need to get by batches 
            var countries = await _context.Countries.ToListAsync();

            foreach (var country in countries)
                await _countriesCache.Set(country.Code, country);
        }

        public async Task ConstructLocalitiesCache()
        {
            var localities = new List<KeyValuePair<string, Locality>>();
            int skip = 0;

            do
            {
                localities = await _context.Localities.Join(_context.Countries, l => l.CountryId, c => c.Id, (l, c) =>
                        new KeyValuePair<string, Locality>(c.Code, l))
                    .OrderBy(l => l.Value.Id).Skip(skip).Take(_batchSize).ToListAsync();
                skip += localities.Count;

                foreach (var locality in localities)
                {
                    var defaultLocality = LanguageHelper.GetValue(locality.Value.Names, DefaultLanguageCode);
                    await _localitiesCache.Set(locality.Key, defaultLocality, locality.Value);
                }
            } while (localities.Count > 0);
        }

        public async Task ConstructLocalityZonesCache()
        {
            var localityZones = new List<KeyValuePair<(string CountryCode,string LocalityName), LocalityZone>>();
            int skip = 0;
            do
            {
                localityZones = await (from z in _context.LocalityZones
                        join l in _context.Localities on z.LocalityId equals l.Id
                        join c in _context.Countries on l.CountryId equals c.Id
                        select new
                            KeyValuePair<(string CountryCode, string LocalityName), LocalityZone>(
                                // TODO: check
                                ValueTuple.Create(c.Code, l.Names.RootElement.GetProperty(Constants.DefaultLanguageCode).GetString()),
                                z))
                    .Skip(skip).Take(_batchSize).ToListAsync();

                skip += localityZones.Count();

                foreach (var localityZone in localityZones)
                {
                    var defaultLocalityZoneName =
                        LanguageHelper.GetValue(localityZone.Value.Names, Constants.DefaultLanguageCode);
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