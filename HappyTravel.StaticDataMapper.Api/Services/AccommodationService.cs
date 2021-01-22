using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
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
            var accommodationWithId = await _context.Accommodations
                .Where(ac => EF.Functions.JsonContains(ac.SupplierAccommodationCodes, searchJson))
                .Select(ac => new {ac.CalculatedAccommodation, ac.Id})
                .SingleOrDefaultAsync();

            if (accommodationWithId == null)
                return Result.Failure<Accommodation>("Accommodation does not exists");

            return Result.Success(MapToAccommodation(accommodationWithId.Id,
                accommodationWithId.CalculatedAccommodation, languageCode));
        }


        public async Task<Result<Accommodation>> Get(int accommodationId, string languageCode)
        {
            var accommodation = await _context.Accommodations
                .Where(ac => ac.Id == accommodationId)
                .Select(ac => ac.CalculatedAccommodation)
                .SingleOrDefaultAsync();

            if (accommodation.Equals(default(MultilingualAccommodation)))
                return Result.Failure<Accommodation>("Accommodation does not exists");

            return Result.Success(MapToAccommodation(accommodationId, accommodation, languageCode));
        }

        
        public async Task<List<Accommodation>> Get(int skip, int top, string languageCode)
        {
            var accommodations = await _context.Accommodations
                .Where(ac => ac.IsActive)
                .OrderBy(ac => ac.Id)
                .Skip(skip)
                .Take(top)
                .Select(ac => new
                {
                    HtId = ac.Id,
                    Data = ac.CalculatedAccommodation
                })
                .ToListAsync();

            return accommodations.Select(ac => MapToAccommodation(ac.HtId, ac.Data, languageCode)).ToList();
        }


        private Accommodation MapToAccommodation(int htId, MultilingualAccommodation accommodation, string language)
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
                    countryName,
                    localityName,
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
                accommodation.Type
            );
        }

        private readonly NakijinContext _context;
    }
}