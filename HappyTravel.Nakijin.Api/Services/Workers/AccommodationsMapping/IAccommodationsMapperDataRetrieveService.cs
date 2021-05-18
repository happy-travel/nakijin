using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Models.Mappers;
using HappyTravel.Nakijin.Data.Models;
using NetTopologySuite.Index.Strtree;
using Contracts = HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Nakijin.Api.Services.Workers.AccommodationsMapping
{
    public interface IAccommodationsMapperDataRetrieveService
    {
        Task<List<Contracts.MultilingualAccommodation>> GetAccommodationsForMapping(string countryCode, Suppliers supplier, int skip, int take,
            DateTime lastUpdatedDate, CancellationToken cancellationToken);
        
        Task<List<(string SupplierCode, SlimAccommodationData AccommodationKeyData)>> GeCountryAccommodationBySupplier(string countryCode, Suppliers supplier);

        Task<List<(string Code, int Id)>> GetCountries(Suppliers supplier);

        Task<List<Tuple<int, int>>> GetActiveCountryUncertainMatchesBySupplier(string countryCode, Suppliers supplier, CancellationToken cancellationToken);

        Task<Dictionary<string, int>> GetLocalitiesByCountry(int countryId);

        Task<Dictionary<(int LocalityId, string LocalityZoneName), int>> GetLocalityZonesByCountry(int countryId);

        Task<Dictionary<int, (int Id, HashSet<int> MappedHtIds)>> GetHtAccommodationMappings();

        Task<DateTime> GetLastMappingDate(Suppliers supplier, CancellationToken cancellationToken);

        Task<STRtree<SlimAccommodationData>> GetCountryAccommodationsTree(string countryCode, Suppliers supplier);
    }
}