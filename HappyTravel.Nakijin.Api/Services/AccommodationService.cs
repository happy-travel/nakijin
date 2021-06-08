using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Nakijin.Api.Services
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(NakijinContext context, AccommodationMappingsCache mappingsCache)
        {
            _mappingsCache = mappingsCache;
            _context = context;
        }


        public async Task<Result<Accommodation>> Get(Suppliers supplier, string supplierAccommodationCode,
            string languageCode)
        {
            var searchJson = "{" + $"\"{supplier.ToString().FirstCharToLower()}\":\"{supplierAccommodationCode}\"" + "}";
            var accommodation = await _context.Accommodations
                .Where(ac => ac.IsActive && EF.Functions.JsonContains(ac.SupplierAccommodationCodes, searchJson))
                .Select(ac => new
                {
                    Id = ac.Id,
                    CountryId = ac.CountryId,
                    LocalityId = ac.LocalityId,
                    LocalityZoneId = ac.LocalityZoneId,
                    Data = ac.CalculatedAccommodation,
                    Modified = ac.Modified
                })
                .SingleOrDefaultAsync();

            if (accommodation == null)
                return Result.Failure<Accommodation>("Accommodation does not exists");

            return MapToAccommodation(accommodation.Id, accommodation.CountryId,
                accommodation.LocalityId, accommodation.LocalityZoneId,
                accommodation.Data, languageCode, accommodation.Modified);
        }


        public async Task<Result<Accommodation>> Get(string htId, string languageCode)
        {
            var (_, isFailure, actualHtId, error) = await GetActualAccommodationHtId(htId);
            if (isFailure)
                return Result.Failure<Accommodation>(error);

            var accommodations = await GetRichDetails(new List<int>(actualHtId));
            var accommodation = accommodations.SingleOrDefault(a => a.Id == actualHtId);

            if (accommodation == default)
                return Result.Failure<Accommodation>("Accommodation does not exists");
            
            return MapToAccommodation(htId: accommodation.Id, 
                htCountryId: accommodation.CountryId,
                htLocalityId: accommodation.LocalityId, 
                htLocalityZoneId: accommodation.LocalityZoneId, 
                accommodation: accommodation.CalculatedAccommodation,
                language: languageCode,
                modified: accommodation.Modified);
        }


        public async Task<List<Accommodation>> Get(List<string> htIds, string languageCode)
        {
            var results = await Task.WhenAll(htIds.Select(GetActualAccommodationHtId));
            var ids = results.Where(r => r.IsSuccess)
                .Select(r => r.Value)
                .ToList();

            return (await GetRichDetails(ids))
                .Select(a => MapToAccommodation(htId: a.Id, 
                    htCountryId: a.CountryId,
                    htLocalityId: a.LocalityId, 
                    htLocalityZoneId: a.LocalityZoneId, 
                    accommodation: a.CalculatedAccommodation,
                    language: languageCode,
                    modified: a.Modified))
                .ToList();
        }


        public Task<DateTime> GetLastModifiedDate()
            => _context.Accommodations.OrderByDescending(d => d.Modified).Select(l => l.Modified).FirstOrDefaultAsync();


        public async Task<List<Accommodation>> Get(int skip, int top, IEnumerable<Suppliers> suppliersFilter,
            bool? hasDirectContractFilter, string languageCode)
        {
            var suppliersKeys = suppliersFilter.Select(s => s.ToString().FirstCharToLower()).ToArray();
            var accommodationsQuery = _context.Accommodations
                .Where(ac => ac.IsActive);


            if (suppliersKeys.Any())
            {
                accommodationsQuery = accommodationsQuery.Where(ac
                    => EF.Functions.JsonExistAny(ac.SupplierAccommodationCodes, suppliersKeys));
            }

            if (hasDirectContractFilter != null)
            {
                accommodationsQuery =
                    accommodationsQuery.Where(ac => ac.HasDirectContract == hasDirectContractFilter.Value);
            }

            accommodationsQuery = accommodationsQuery
                .OrderBy(ac => ac.Id)
                .Skip(skip)
                .Take(top);

            var accommodations = await accommodationsQuery
                .Select(ac => new
                {
                    Id = ac.Id,
                    CountryId = ac.CountryId,
                    LocalityId = ac.LocalityId,
                    LocalityZoneId = ac.LocalityZoneId,
                    Data = ac.CalculatedAccommodation
                })
                .ToListAsync();

            return accommodations.Select(ac
                    => MapToAccommodation(ac.Id, ac.CountryId, ac.LocalityId, ac.LocalityZoneId, ac.Data,
                        languageCode))
                .ToList();
        }

        private Task<List<RichAccommodationDetails>> GetRichDetails(ICollection<int> ids)
            => _context.Accommodations
                .Where(ac => ac.IsActive && ids.Contains(ac.Id))
                .Select(ac => new RichAccommodationDetails
                {
                    Id = ac.Id,
                    CountryId = ac.CountryId,
                    LocalityId = ac.LocalityId,
                    LocalityZoneId = ac.LocalityZoneId,
                    CalculatedAccommodation = ac.CalculatedAccommodation,
                    Modified = ac.Modified
                })
                .ToListAsync();

        private Accommodation MapToAccommodation(int htId, int htCountryId, int? htLocalityId, int? htLocalityZoneId,
            MultilingualAccommodation accommodation, string language, DateTime? modified = null)
        {
            var name = accommodation.Name.GetValueOrDefault(language);
            var accommodationAmenities = accommodation.AccommodationAmenities.GetValueOrDefault(language);
            var additionalInfo = accommodation.AdditionalInfo.GetValueOrDefault(language);
            var category = accommodation.Category.GetValueOrDefault(language);
            var address = accommodation.Location.Address.GetValueOrDefault(language);
            var localityName = accommodation.Location.Locality?.GetValueOrDefault(language);
            var countryName = accommodation.Location.Country.GetValueOrDefault(language);
            var localityZoneName = accommodation.Location.LocalityZone?.GetValueOrDefault(language);
            var textualDescriptions = new List<TextualDescription>();
            var accommodationHtId = HtId.Create(AccommodationMapperLocationTypes.Accommodation, htId);
            var countryHtId = HtId.Create(AccommodationMapperLocationTypes.Country, htCountryId);
            var localityHtId = htLocalityId is not null
                ? HtId.Create(AccommodationMapperLocationTypes.Locality, htLocalityId.Value)
                : string.Empty;
            var localityZoneHtId = htLocalityZoneId is not null
                ? HtId.Create(AccommodationMapperLocationTypes.LocalityZone, htLocalityZoneId.Value)
                : string.Empty;

            foreach (var descriptions in accommodation.TextualDescriptions)
            {
                var description = descriptions.Description.GetValueOrDefault(language);
                textualDescriptions.Add(new TextualDescription(descriptions.Type, description));
            }

            return new Accommodation(
                htId.ToString(),
                name,
                accommodationAmenities,
                additionalInfo,
                category,
                accommodation.Contacts,
                new LocationInfo(
                    accommodation.Location.CountryCode,
                    countryHtId,
                    countryName,
                    localityHtId,
                    localityName,
                    localityZoneHtId,
                    localityZoneName,
                    accommodation.Location.Coordinates,
                    address,
                    accommodation.Location.LocationDescriptionCode,
                    accommodation.Location.PointsOfInterests,
                    accommodation.Location.IsHistoricalBuilding
                ),
                accommodation.Photos,
                accommodation.Rating,
                accommodation.Schedule,
                textualDescriptions,
                accommodation.Type,
                accommodationHtId,
                modified: modified
            );
        }


        private async Task<Result<int>> GetActualAccommodationHtId(string htId)
        {
            var (_, isFailure, (type, id), error) = HtId.Parse(htId);
            if (isFailure)
                return Result.Failure<int>(error);

            if (type != AccommodationMapperLocationTypes.Accommodation)
                return Result.Failure<int>($"{type} is not supported");

            return await _mappingsCache.GetActualHtId(id);
        }
        

        private readonly AccommodationMappingsCache _mappingsCache;
        private readonly NakijinContext _context;
    }
}