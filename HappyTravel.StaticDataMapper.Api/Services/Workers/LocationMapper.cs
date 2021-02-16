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
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public class LocationMapper : ILocationMapper
    {
        public LocationMapper(NakijinContext context, ILocationNameNormalizer locationNameNormalizer,
            IOptions<StaticDataLoadingOptions> options,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _dbCommandTimeOut = options.Value.DbCommandTimeOut;
            _locationNameNormalizer = locationNameNormalizer;
            _logger = loggerFactory.CreateLogger<LocationMapper>();
        }


        public async Task MapLocations(Suppliers supplier, CancellationToken cancellationToken = default)
        {
            try
            {
                // Only here needed large timeout
                _context.Database.SetCommandTimeout(_dbCommandTimeOut);

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
            var countries = await GetNormalizedCountries();

            var countryPairsChanged = new Dictionary<int, int>();
            var notSupplierCountries = countries.Where(c => !c.SupplierCountryCodes.ContainsKey(supplier)).ToList();
            var supplierCountries = countries.Where(c => c.SupplierCountryCodes.ContainsKey(supplier)).ToList();

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

                var defaultName = country.Names.GetValueOrDefault(DefaultLanguageCode);
                var code = _locationNameNormalizer.GetNormalizedCountryCode(defaultName, country.Code);
                var dbCountry = new Country
                {
                    Code = code,
                    Names = NormalizeCountryMultiLingualNames(country.Names),
                    IsActive = true,
                    Modified = utcDate
                };

                var existing = notSupplierCountries.FirstOrDefault(c => c.Code == code);
                var existingOfSupplier = supplierCountries.FirstOrDefault(c => c.Code == code);
                if (existing != default)
                {
                    dbCountry.Id = existing.Id;
                    dbCountry.Names = MultiLanguageHelpers.Merge(dbCountry.Names, existing.Names);
                    dbCountry.SupplierCountryCodes = new Dictionary<Suppliers, string>(existing.SupplierCountryCodes);
                    dbCountry.SupplierCountryCodes.TryAdd(supplier, code);

                    if (existingOfSupplier != default)
                    {
                        countryPairsChanged.Add(existingOfSupplier.Id, dbCountry.Id);
                        foreach (var sup in existingOfSupplier.SupplierCountryCodes)
                            dbCountry.SupplierCountryCodes.TryAdd(sup.Key, sup.Value);
                        existingOfSupplier.IsActive = false;
                        countriesToUpdate.Add(existingOfSupplier);
                    }

                    countriesToUpdate.Add(dbCountry);
                }
                else if (existingOfSupplier == default)
                {
                    dbCountry.SupplierCountryCodes = new Dictionary<Suppliers, string> {{supplier, code}};
                    dbCountry.Created = utcDate;
                    newCountries.Add(dbCountry);
                }
            }

            // TODO: Remove Distinct ( in connectors may be the same data in different forms normalized or not that is why needed distinct here )
            _context.UpdateRange(countriesToUpdate.Distinct(new CountryComparer()));
            _context.AddRange(newCountries.Distinct(new CountryComparer()));
            await ChangeCountryDependencies(countryPairsChanged);

            await _context.SaveChangesAsync(cancellationToken);


            async Task ChangeCountryDependencies(Dictionary<int, int> countryChangedPairs)
            {
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
        }


        private async Task MapLocalities(Suppliers supplier, CancellationToken cancellationToken)
        {
            var countries = await GetCountries();

            foreach (var country in countries)
            {
                var changedLocalityPairs = new Dictionary<int, int>();
                var dbNormalizedLocalities = await GetNormalizedLocalitiesByCountry(country.Code, cancellationToken);
                var notSupplierLocalities = dbNormalizedLocalities
                    .Where(l => !l.SupplierLocalityCodes.ContainsKey(supplier)).ToList();
                var supplierLocalities = dbNormalizedLocalities
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

                    var defaultCountryName = locality.CountryNames.GetValueOrDefault(DefaultLanguageCode);
                    var defaultLocalityName = locality.LocalityNames.GetValueOrDefault(DefaultLanguageCode);
                    var normalizedLocalityName =
                        _locationNameNormalizer.GetNormalizedLocalityName(defaultCountryName, defaultLocalityName);

                    var existing = notSupplierLocalities.FirstOrDefault(l => l.Names.En == normalizedLocalityName);
                    var existingOfSupplier = supplierLocalities.FirstOrDefault(l => l.Names.En == defaultLocalityName);

                    var dbLocality = new Locality
                    {
                        Names = NormalizeLocalityMultilingualNames(defaultCountryName, locality.LocalityNames),
                        IsActive = true,
                        Modified = utcDate
                    };
                    if (existing != default)
                    {
                        dbLocality.Id = existing.Id;
                        dbLocality.CountryId = existing.CountryId;
                        dbLocality.Names = MultiLanguageHelpers.Merge(dbLocality.Names, existing.Names);
                        dbLocality.SupplierLocalityCodes =
                            new Dictionary<Suppliers, string>(existing.SupplierLocalityCodes);
                        dbLocality.SupplierLocalityCodes.TryAdd(supplier, locality.LocalityCode);
                        if (existingOfSupplier != default)
                        {
                            changedLocalityPairs.Add(existingOfSupplier.Id, existing.Id);

                            foreach (var sup in existingOfSupplier.SupplierLocalityCodes)
                                dbLocality.SupplierLocalityCodes.TryAdd(sup.Key, sup.Value);
                            existingOfSupplier.IsActive = false;
                            localitiesToUpdate.Add(existingOfSupplier);
                        }

                        localitiesToUpdate.Add(dbLocality);
                    }
                    else if (existingOfSupplier == default)
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
                await ChangeLocalityDependencies(changedLocalityPairs);

                await _context.SaveChangesAsync(cancellationToken);

                _context.ChangeTracker.Entries()
                    .Where(e => e.Entity != null)
                    .Where(e => e.State != EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = EntityState.Detached);
            }


            async Task ChangeLocalityDependencies(Dictionary<int, int> localityChangedPairs)
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
        }

        private async Task MapLocalityZones(Suppliers supplier, CancellationToken cancellationToken)
        {
            var countries = await GetCountries();

            foreach (var country in countries)
            {
                var changedLocalityZonesPairs = new Dictionary<int, int>();
                var countryLocalities = await GetNormalizedLocalitiesByCountry(country.Code, cancellationToken);
                var dbNormalizedLocalityZones =
                    await GetNormalizedLocalityZonesByCountry(country.Code, cancellationToken);
                var notSupplierLocalityZones = dbNormalizedLocalityZones
                    .Where(l => !l.LocalityZone.SupplierLocalityZoneCodes.ContainsKey(supplier)).ToList();
                var supplierLocalityZones = dbNormalizedLocalityZones
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
                    .GroupBy(lz => lz.CountryNames.En)
                    .Select(lz => lz.First())
                    .ToList();

                var localityZonesToUpdate = new List<LocalityZone>();
                var localityZonesToAdd = new List<LocalityZone>();
                var utcDate = DateTime.UtcNow;

                foreach (var zone in localityZonesToMap)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var defaultCountryName = zone.CountryNames.GetValueOrDefault(DefaultLanguageCode);
                    var defaultLocalityName = zone.LocalityNames.GetValueOrDefault(DefaultLanguageCode);
                    var normalizedLocalityName =
                        _locationNameNormalizer.GetNormalizedLocalityName(defaultCountryName, defaultLocalityName);
                    var defaultLocalityZone = zone.LocalityZoneNames.GetValueOrDefault(DefaultLanguageCode);
                    var normalizedLocalityZone = defaultLocalityZone.ToNormalizedName();

                    var existing = notSupplierLocalityZones.FirstOrDefault(lz
                        => lz.DefaultLocality == normalizedLocalityName
                        && lz.LocalityZone.Names.En == normalizedLocalityZone);
                    var existingOfSupplier = supplierLocalityZones.FirstOrDefault(lz
                        => lz.DefaultLocality == normalizedLocalityName
                        && lz.LocalityZone.Names.En == normalizedLocalityZone);

                    var dbLocalityZone = new LocalityZone
                    {
                        Names = NormalizeLocalityZoneMultilingualNames(zone.LocalityZoneNames),
                        IsActive = true,
                        Modified = utcDate
                    };

                    if (existing != default)
                    {
                        dbLocalityZone.Id = existing.LocalityZone.Id;
                        dbLocalityZone.LocalityId = existing.LocalityZone.LocalityId;
                        dbLocalityZone.Names =
                            MultiLanguageHelpers.Merge(dbLocalityZone.Names, existing.LocalityZone.Names);
                        dbLocalityZone.SupplierLocalityZoneCodes =
                            new Dictionary<Suppliers, string>(existing.LocalityZone.SupplierLocalityZoneCodes);
                        dbLocalityZone.SupplierLocalityZoneCodes.TryAdd(supplier, zone.LocalityZoneCode);
                        if (existingOfSupplier != default)
                        {
                            changedLocalityZonesPairs.Add(existingOfSupplier.LocalityZone.Id, existing.LocalityZone.Id);
                            foreach (var sup in existingOfSupplier.LocalityZone.SupplierLocalityZoneCodes)
                                dbLocalityZone.SupplierLocalityZoneCodes.TryAdd(sup.Key, sup.Value);
                            existingOfSupplier.LocalityZone.IsActive = false;
                            localityZonesToUpdate.Add(existingOfSupplier.LocalityZone);
                        }

                        localityZonesToUpdate.Add(dbLocalityZone);
                    }
                    else if (existingOfSupplier == default)
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
                await ChangeLocalityZoneDependencies(changedLocalityZonesPairs);
             
                await _context.SaveChangesAsync(cancellationToken);
                
                _context.ChangeTracker.Entries()
                    .Where(e => e.Entity != null)
                    .Where(e => e.State != EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = EntityState.Detached);
            }


            async Task ChangeLocalityZoneDependencies(Dictionary<int, int> localityZoneChangedPairs)
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

        
        // TODO: Maybe will be added normalization of raw data (not final data)
        private async Task<List<Country>> GetNormalizedCountries()
        {
            var dbCountries = await _context.Countries.Where(c => c.IsActive).ToListAsync();
            var normalizedCountries = new List<Country>();
            foreach (var country in dbCountries)
            {
                var defaultName = country.Names.GetValueOrDefault(DefaultLanguageCode);
                var code = _locationNameNormalizer.GetNormalizedCountryCode(defaultName, country.Code);
                var normalizedNames = NormalizeCountryMultiLingualNames(country.Names);
                normalizedCountries.Add(new Country
                {
                    Id = country.Id,
                    Code = code,
                    Names = normalizedNames,
                    IsActive = country.IsActive,
                    Modified = country.Modified
                });
            }

            return normalizedCountries;
        }

        private async Task<List<Locality>> GetNormalizedLocalitiesByCountry(string countryCode,
            CancellationToken cancellationToken)
        {
            var localities = await (from l in _context.Localities
                join c in _context.Countries on l.CountryId equals c.Id
                where l.IsActive && c.IsActive && c.Code == countryCode
                select new
                {
                    Locality = l,
                    CountryName = c.Names.En
                }).ToListAsync(cancellationToken);

            return localities.Select(l => new Locality
            {
                Id = l.Locality.Id,
                Names = NormalizeLocalityMultilingualNames(l.CountryName, l.Locality.Names),
                CountryId = l.Locality.CountryId,
                SupplierLocalityCodes = l.Locality.SupplierLocalityCodes,
                Created = l.Locality.Created,
                Modified = l.Locality.Modified
            }).ToList();
        }

        private async Task<List<(string DefaultLocality, LocalityZone LocalityZone)>>
            GetNormalizedLocalityZonesByCountry(
                string countryCode, CancellationToken cancellationToken)
        {
            var localityZones = await (from lz in _context.LocalityZones
                join l in _context.Localities on lz.LocalityId equals l.Id
                join c in _context.Countries on l.CountryId equals c.Id
                where lz.IsActive && l.IsActive && c.IsActive && c.Code == countryCode
                select new
                {
                    LocalityName = l.Names.En,
                    Zone = lz
                }).ToListAsync(cancellationToken);

            return localityZones.Select(lz => (lz.LocalityName, new LocalityZone
            {
                Id = lz.Zone.Id,
                LocalityId = lz.Zone.LocalityId,
                Names = NormalizeLocalityZoneMultilingualNames(lz.Zone.Names),
                SupplierLocalityZoneCodes = lz.Zone.SupplierLocalityZoneCodes,
                Created = lz.Zone.Created,
                Modified = lz.Zone.Modified,
                IsActive = lz.Zone.IsActive
            })).ToList();
        }


        private Task<List<(string Code, int Id)>> GetCountries()
            => _context.Countries.Where(c => c.IsActive).Select(c => ValueTuple.Create(c.Code, c.Id)).ToListAsync();


        private const string DefaultLanguageCode = "en";
        private readonly NakijinContext _context;
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly ILogger<LocationMapper> _logger;
        private readonly int _dbCommandTimeOut;
    }
}