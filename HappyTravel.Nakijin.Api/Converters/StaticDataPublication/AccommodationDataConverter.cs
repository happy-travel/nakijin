using HappyTravel.Nakijin.Api.Models.StaticDataPublications;
using HappyTravel.Nakijin.Data.Models.Accommodations;

namespace HappyTravel.Nakijin.Api.Converters.StaticDataPublication
{
    public static class AccommodationDataConverter
    {
        public static AccommodationData Convert(RichAccommodationDetails accommodationDetails)
            => new(id: accommodationDetails.Id,
                name: accommodationDetails.KeyData.DefaultName,
                localityName: string.Empty,
                countryName: accommodationDetails.KeyData.DefaultCountryName,
                countryCode: accommodationDetails.CountryCode,
                coordinates: accommodationDetails.KeyData.Coordinates);
    }
}