using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Models.Mappers;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using Newtonsoft.Json;
using Contracts = HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Nakijin.Api.Services.Workers.AccommodationMapping
{
    public class AccommodationMapperDataRetrieveService : IAccommodationMapperDataRetrieveService
    {
        public AccommodationMapperDataRetrieveService(NakijinContext context, IOptions<StaticDataLoadingOptions> options)
        {
            _context = context;
            _batchSize = options.Value.MappingBatchSize;
        }


        public async Task<List<Contracts.MultilingualAccommodation>> GetAccommodationsForMapping(string countryCode,
            Suppliers supplier, int skip, int take, DateTime lastUpdatedDate, CancellationToken cancellationToken)
        {
            var accommodations = await (from ac in _context.RawAccommodations
                    where ac.Supplier == supplier
                        && ac.CountryCode == countryCode
                        && ac.Modified > lastUpdatedDate
                    select ac)
                .OrderBy(ac => ac.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return accommodations.Select(ac
                    => JsonConvert.DeserializeObject<Contracts.MultilingualAccommodation>(ac.Accommodation.RootElement
                        .ToString()!))
                .ToList();
        }


        public async Task<List<(string SupplierCode, SlimAccommodationData AccommodationKeyData)>> GeCountryAccommodationBySupplier(string countryCode,
            Suppliers supplier)
        {
            var countryAccommodations = new List<SlimAccommodationData>();
            var accommodations = new List<SlimAccommodationData>();
            var skip = 0;
            do
            {
                accommodations = await _context.Accommodations.Where(ac
                        => ac.CountryCode == countryCode && EF.Functions.JsonExists(ac.SupplierAccommodationCodes,
                            supplier.ToString().FirstCharToLower()))
                    .OrderBy(ac => ac.Id)
                    .Skip(skip)
                    .Take(_batchSize)
                    .Select(ac => new SlimAccommodationData
                    {
                        HtId = ac.Id,
                        SupplierAccommodationCodes = ac.SupplierAccommodationCodes,
                        IsActive = ac.IsActive,
                        DeactivationReason = ac.DeactivationReason
                    })
                    .ToListAsync();

                skip += _batchSize;
                countryAccommodations.AddRange(accommodations);
            } while (accommodations.Count > 0);

            return countryAccommodations.Select(ac => (ac.SupplierAccommodationCodes[supplier], ac)).ToList();
        }


        public async Task<STRtree<SlimAccommodationData>> GetCountryAccommodationsTree(string countryCode,
            Suppliers supplier)
        {
            var countryAccommodations = new List<SlimAccommodationData>();
            var accommodations = new List<SlimAccommodationData>();
            var skip = 0;
            do
            {
                accommodations = await _context.Accommodations.Where(ac
                        => ac.CountryCode == countryCode && !EF.Functions.JsonExists(ac.SupplierAccommodationCodes,
                            supplier.ToString().FirstCharToLower()) && ac.IsActive)
                    .OrderBy(ac => ac.Id)
                    .Skip(skip)
                    .Take(_batchSize)
                    .Select(ac => new SlimAccommodationData
                    {
                        HtId = ac.Id,
                        KeyData = ac.KeyData,
                        SupplierAccommodationCodes = ac.SupplierAccommodationCodes
                    })
                    .ToListAsync();

                skip += _batchSize;
                countryAccommodations.AddRange(accommodations);
            } while (accommodations.Count > 0);

            if (!countryAccommodations.Any() || countryAccommodations.Count == 1)
                return new STRtree<SlimAccommodationData>();

            var tree = new STRtree<SlimAccommodationData>(countryAccommodations.Count);
            foreach (var ac in countryAccommodations)
            {
                if (!ac.KeyData.Coordinates.IsEmpty() && ac.KeyData.Coordinates.IsValid())
                    tree.Insert(new Point(ac.KeyData.Coordinates.Longitude,
                        ac.KeyData.Coordinates.Latitude).EnvelopeInternal, ac);
            }

            tree.Build();
            return tree;
        }


        public async Task<List<(string Code, int Id)>> GetCountries(Suppliers supplier)
        {
            var countries = await _context.Countries
                .Where(c => c.IsActive && EF.Functions.JsonExists(c.SupplierCountryCodes,
                    supplier.ToString().FirstCharToLower()))
                .OrderBy(c => c.Code)
                .Select(c => ValueTuple.Create(c.Code, c.Id))
                .ToListAsync();

            return countries;
        }


        public Task<List<Tuple<int, int>>> GetActiveCountryUncertainMatchesBySupplier(string countryCode,
            Suppliers supplier, CancellationToken cancellationToken)
            => (from um in _context.AccommodationUncertainMatches
                join sourceAc in _context.Accommodations on um.SourceHtId equals sourceAc.Id
                join acToMatch in _context.Accommodations on um.HtIdToMatch equals acToMatch.Id
                where um.IsActive && sourceAc.CountryCode == countryCode && acToMatch.CountryCode == countryCode &&
                    (EF.Functions.JsonExists(sourceAc.SupplierAccommodationCodes,
                        supplier.ToString().FirstCharToLower()) || EF.Functions.JsonExists(acToMatch.SupplierAccommodationCodes,
                        supplier.ToString().FirstCharToLower()))
                select new Tuple<int, int>(um.SourceHtId, um.HtIdToMatch)).ToListAsync(cancellationToken);


        public Task<Dictionary<string, int>> GetLocalitiesByCountry(int countryId)
            => _context.Localities.Where(l => l.CountryId == countryId && l.IsActive)
                .Select(l => new {Name = l.Names.En, l.Id}).ToDictionaryAsync(l => l.Name, l => l.Id);


        public Task<Dictionary<(int LocalityId, string LocalityZoneName), int>> GetLocalityZonesByCountry(
            int countryId)
            => (from z in _context.LocalityZones
                join l in _context.Localities on z.LocalityId equals l.Id
                where z.IsActive && l.IsActive && l.CountryId == countryId
                select new
                {
                    LocalityId = l.Id,
                    LocalityZoneName = z.Names.En,
                    Id = z.Id
                }).ToDictionaryAsync(z => (z.LocalityId, z.LocalityZoneName), z => z.Id);


        public Task<Dictionary<int, (int Id, HashSet<int> MappedHtIds)>> GetHtAccommodationMappings()
            => _context.HtAccommodationMappings
                .Where(m => m.IsActive)
                .Select(m => new
                {
                    Id = m.Id,
                    HtId = m.HtId,
                    MappedHtIds = m.MappedHtIds
                })
                .ToDictionaryAsync(m => m.HtId, m => (m.Id, m.MappedHtIds));


        public Task<DateTime> GetLastMappingDate(Suppliers supplier, CancellationToken cancellationToken)
            => _context.DataUpdateHistories.Where(h => h.Supplier == supplier && h.Type == DataUpdateTypes.Mapping)
                .OrderByDescending(h => h.UpdateTime)
                .Select(h => h.UpdateTime).FirstOrDefaultAsync(cancellationToken);


        private readonly int _batchSize;
        private readonly NakijinContext _context;
    }
}