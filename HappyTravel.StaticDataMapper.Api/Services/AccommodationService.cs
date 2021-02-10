using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.StaticDataMapper.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(NakijinContext context)
        {
            _context = context;
        }


        public async Task<Result<Accommodation>> Get(Suppliers supplier, string supplierAccommodationCode,
            string languageCode)
        {
            var searchJson = "{" + $"\"{supplier.ToString().ToLower()}\":\"{supplierAccommodationCode}\"" + "}";
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


        public async Task<Result<Accommodation>> Get(int accommodationId, string languageCode)
        {
            var accommodation = await _context.Accommodations
                .Where(ac => ac.IsActive && ac.Id == accommodationId)
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
                accommodation.LocalityId, accommodation.LocalityZoneId, accommodation.Data, languageCode,
                accommodation.Modified);
        }


        public Task<Result<Accommodation>> Get(string htId, string languageCode)
        {
            var (_, isFailure, (type, id), error) = HtId.Parse(htId);
            if (isFailure)
                return Task.FromResult(Result.Failure<Accommodation>(error));

            if (type != AccommodationMapperLocationTypes.Accommodation)
                return Task.FromResult(Result.Failure<Accommodation>($"{type} is not supported"));

            return Get(id, languageCode);
        }
        

        public Task<DateTime> GetLastModifiedDate()
            => _context.Accommodations.OrderByDescending(d => d.Modified).Select(l => l.Modified).FirstOrDefaultAsync();


        public async Task<List<Accommodation>> Get(int skip, int top, string languageCode)
        {
            var accommodations = await _context.Accommodations
                .Where(ac => ac.IsActive)
                .OrderBy(ac => ac.Id)
                .Skip(skip)
                .Take(top)
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
                        htCountryId.ToString(),
                        countryName,
                        htLocalityId?.ToString(),
                        localityName,
                        htLocalityZoneId?.ToString(),
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
                    HtId.Create(AccommodationMapperLocationTypes.Accommodation, htId),
                modified: modified
            );
        }

        private readonly NakijinContext _context;
    }
}