using HappyTravel.EdoContracts.Accommodations.Internals;
using LocationNameNormalizer;
using HappyTravel.MultiLanguage;
using LocationNameNormalizer.Extensions;
using Contracts = HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Api.Models;
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public class MultilingualDataHelper
    {
        public MultilingualDataHelper(ILocationNameNormalizer locationNameNormalizer)
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
                    _locationNameNormalizer.GetNormalizedCountryCode(accommodation.Location.Country.En,
                        accommodation.Location.CountryCode),
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
        
        public  AccommodationDataForMapping GetAccommodationDataForMapping(Contracts.MultilingualAccommodation accommodation)
            => new AccommodationDataForMapping
            {
                DefaultName = accommodation.Name.En,
                DefaultCountryName = accommodation.Location.Country.En,
                DefaultLocalityName = accommodation.Location.Locality?.En,
                DefaultLocalityZoneName = accommodation.Location.LocalityZone?.En,
                Address = accommodation.Location.Address.En,
                Rating = accommodation.Rating,
                ContactInfo = accommodation.Contacts,
                Coordinates = accommodation.Location.Coordinates
            };

        private readonly ILocationNameNormalizer _locationNameNormalizer;
    }
}