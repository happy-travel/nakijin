using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.LocationNameNormalizer;
using HappyTravel.LocationNameNormalizer.Extensions;
using HappyTravel.MultiLanguage;
using Contracts = HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Api.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;

namespace HappyTravel.Nakijin.Api.Services.Workers
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
                supplierCode: accommodation.SupplierCode,
                name: NormalizeMultilingualNames(accommodation.Name)!,
                accommodationAmenities: accommodation.AccommodationAmenities,
                additionalInfo: accommodation.AdditionalInfo,
                category: accommodation.Category,
                contacts: accommodation.Contacts,
                new MultilingualLocationInfo(
                    _locationNameNormalizer.GetNormalizedCountryCode(accommodation.Location.Country.En,
                        accommodation.Location.CountryCode),
                    country: NormalizeCountryMultiLingualNames(accommodation.Location.Country),
                    coordinates: accommodation.Location.Coordinates,
                    address: accommodation.Location.Address,
                    locationDescriptionCode: accommodation.Location.LocationDescriptionCode,
                    pointsOfInterests: accommodation.Location.PointsOfInterests,
                    supplierLocalityCode: accommodation.Location.SupplierLocalityCode,
                    locality: NormalizeMultilingualLocality(accommodation.Location),
                    supplierLocalityZoneCode: accommodation.Location.SupplierLocalityZoneCode,
                    localityZone: NormalizeMultilingualNames(accommodation.Location.LocalityZone)
                ),
                photos: accommodation.Photos,
                rating: accommodation.Rating,
                schedule: accommodation.Schedule,
                textualDescriptions: accommodation.TextualDescriptions,
                type: accommodation.Type,
                hasDirectContract: accommodation.HasDirectContract,
                isActive: accommodation.IsActive
            );


            MultiLanguage<string>? NormalizeMultilingualLocality(in MultilingualLocationInfo location)
            {
                if (location.Locality is null)
                    return null;

                var defaultCountry = location.Country.GetValueOrDefault(Constants.DefaultLanguageCode);

                return NormalizeLocalityMultilingualNames(defaultCountry, location.Locality);
            }
        }


        public MultiLanguage<string> NormalizeCountryMultiLingualNames(MultiLanguage<string> countryNames)
        {
            var normalized = new MultiLanguage<string>();
            var allNames = countryNames.GetAll();

            foreach (var name in allNames)
                normalized.TrySetValue(name.languageCode, _locationNameNormalizer.GetNormalizedCountryName(name.value));

            return normalized;
        }


        public MultiLanguage<string> NormalizeLocalityMultilingualNames(string defaultCountry,
            MultiLanguage<string> localityNames)
        {
            var normalizedLocalityNames = new MultiLanguage<string>();
            var allNames = localityNames.GetAll();

            foreach (var name in allNames)
                normalizedLocalityNames.TrySetValue(name.languageCode,
                    _locationNameNormalizer.GetNormalizedLocalityName(defaultCountry, name.value));

            return normalizedLocalityNames;
        }


        public MultiLanguage<string>? NormalizeMultilingualNames(in MultiLanguage<string>? name)
        {
            if (name is null)
                return null;

            var result = new MultiLanguage<string>();
            var allValues = name.GetAll();
            foreach (var item in allValues)
                result.TrySetValue(item.languageCode, item.value.ToNormalizedName());

            return result;
        }


        public AccommodationKeyData GetAccommodationKeyData(Contracts.MultilingualAccommodation accommodation)
            => new AccommodationKeyData
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