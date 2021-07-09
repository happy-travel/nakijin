using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Models.Mappers;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.SuppliersCatalog;
using NetTopologySuite.Index.Strtree;
using Contracts = HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Nakijin.Api.Services.Workers.AccommodationMapping
{
    public interface IAccommodationMapperDataRetrieveService
    {
        Task<List<Contracts.MultilingualAccommodation>> GetAccommodationsForMapping(string countryCode, Suppliers supplier, int skip, int take,
            DateTime lastUpdatedDate, CancellationToken cancellationToken);
        
        Task<List<(string SupplierCode, SlimAccommodationData AccommodationKeyData)>> GeCountryAccommodationBySupplier(string countryCode, Suppliers supplier);

        Task<List<(string Code, int Id)>> GetCountries(Suppliers supplier);

        Task<Dictionary<int, (int SourceHtId, int HtIdToMatch)>> GetActiveCountryUncertainMatchesBySupplier(string countryCode, Suppliers supplier, CancellationToken cancellationToken);

        Task<Dictionary<string, int>> GetLocalitiesByCountry(int countryId);

        Task<Dictionary<(int LocalityId, string LocalityZoneName), int>> GetLocalityZonesByCountry(int countryId);

        Task<Dictionary<int, (int Id, HashSet<int> MappedHtIds)>> GetHtAccommodationMappings();

        Task<DateTime> GetLastMappingDate(Suppliers supplier, CancellationToken cancellationToken);

        Task<STRtree<SlimAccommodationData>> GetCountryAccommodationsTree(string countryCode, Suppliers supplier);
    }
}