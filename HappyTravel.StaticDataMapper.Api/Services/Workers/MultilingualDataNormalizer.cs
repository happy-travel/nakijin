using HappyTravel.EdoContracts.Accommodations.Internals;
using LocationNameNormalizer;
using HappyTravel.MultiLanguage;
using LocationNameNormalizer.Extensions;
using Contracts = HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Api.Models;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public class MultilingualDataNormalizer
    {
        public MultilingualDataNormalizer(ILocationNameNormalizer locationNameNormalizer)
        {
            _locationNameNormalizer = locationNameNormalizer;
        }


        public Contracts.MultilingualAccommodation NormalizeAccommodation(
            in Contracts.MultilingualAccommodation accommodation)
        {
            return new Contracts.MultilingualAccommodation
            (
                accommodation.SupplierCode,
                NormalizeMultilingualName(accommodation.Name)!,
                accommodation.AccommodationAmenities,
                accommodation.AdditionalInfo,
                accommodation.Category,
                accommodation.Contacts,
                new MultilingualLocationInfo(
                    accommodation.Location.CountryCode,
                    NormalizeMultilingualCountry(accommodation.Location),
                    accommodation.Location.Coordinates,
                    accommodation.Location.Address,
                    accommodation.Location.LocationDescriptionCode,
                    accommodation.Location.PointsOfInterests,
                    accommodation.Location.SupplierLocalityCode,
                    NormalizeMultilingualLocality(accommodation.Location),
                    accommodation.Location.SupplierLocalityZoneCode,
                    NormalizeMultilingualName(accommodation.Location.LocalityZone)
                ),
                accommodation.Photos,
                accommodation.Rating,
                accommodation.Schedule,
                accommodation.TextualDescriptions,
                accommodation.Type
            );


            // TODO: remove locations normalization and get it from db
            MultiLanguage<string> NormalizeMultilingualCountry(in MultilingualLocationInfo location)
            {
                var result = new MultiLanguage<string>();
                var allValues = location.Country.GetAll();
                foreach (var item in allValues)
                    result.TrySetValue(item.languageCode, _locationNameNormalizer.GetNormalizedCountryName(item.value));

                return result;
            }


            MultiLanguage<string>? NormalizeMultilingualLocality(in MultilingualLocationInfo location)
            {
                if (location.Locality == null)
                    return null;

                var result = new MultiLanguage<string>();
                var defaultCountry = location.Country.GetValueOrDefault(Constants.DefaultLanguageCode);
                var allValues = location.Locality.GetAll();
                foreach (var item in allValues)
                    result.TrySetValue(item.languageCode,
                        _locationNameNormalizer.GetNormalizedLocalityName(defaultCountry, item.value));

                return result;
            }


            MultiLanguage<string>? NormalizeMultilingualName(in MultiLanguage<string>? name)
            {
                if (name == null)
                    return null;

                var result = new MultiLanguage<string>();
                var allValues = name.GetAll();
                foreach (var item in allValues)
                    result.TrySetValue(item.languageCode, item.value.ToNormalizedName());

                return result;
            }
        }

        private readonly ILocationNameNormalizer _locationNameNormalizer;
    }
}