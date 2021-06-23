using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.MultiLanguage;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Data.Models.Mappers;
using HappyTravel.SuppliersCatalog;
using Newtonsoft.Json;

namespace HappyTravel.Nakijin.Api.Services.Workers.AccommodationDataCalculation
{
    public class AccommodationMergerHelper
    {
        public AccommodationMergerHelper(ISuppliersPriorityService suppliersPriorityService, MultilingualDataHelper multilingualDataHelper)
        {
            _multilingualDataHelper = multilingualDataHelper;
            _suppliersPriorityService = suppliersPriorityService;
        }


        public async Task<MultilingualAccommodation> Merge(RichAccommodationDetails accommodation,
            List<RawAccommodation> supplierAccommodations)
        {
            // Checking match of supplier and accommodation
            supplierAccommodations = (from sa in supplierAccommodations
                join acs in accommodation.SupplierAccommodationCodes
                    on new {Supplier = sa.Supplier, SupplierAccommodationId = sa.SupplierAccommodationId}
                    equals new {Supplier = acs.Key, SupplierAccommodationId = acs.Value}
                select sa).ToList();

            var supplierAccommodationDetails = supplierAccommodations.ToDictionary(d => d.Supplier,
                d => _multilingualDataHelper.NormalizeAccommodation(
                    JsonConvert.DeserializeObject<MultilingualAccommodation>(d.Accommodation.RootElement
                        .ToString()!)));
            
            var suppliersPriority = accommodation.SuppliersPriority.Any()
                ? accommodation.SuppliersPriority
                : await _suppliersPriorityService.Get();

            var accommodationWithManualCorrection = accommodation.AccommodationWithManualCorrections;

            var name = MergeMultilingualData(suppliersPriority[AccommodationDataTypes.Name],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Name)!,
                accommodationWithManualCorrection.Name, string.IsNullOrEmpty);

            var category = MergeMultilingualData(suppliersPriority[AccommodationDataTypes.Category],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Category)!,
                accommodationWithManualCorrection.Category, string.IsNullOrEmpty);

            var rating = MergeData(suppliersPriority[AccommodationDataTypes.Rating],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Rating),
                accommodationWithManualCorrection.Rating,
                r => r == 0);

            var contactInfo = MergeContactInfo(suppliersPriority[AccommodationDataTypes.ContactInfo],
                supplierAccommodationDetails, accommodationWithManualCorrection);

            var locationInfo = MergeLocationInfo(suppliersPriority[AccommodationDataTypes.LocationInfo],
                supplierAccommodationDetails, accommodationWithManualCorrection, accommodation.Country?.Names, accommodation.Locality?.Names, accommodation.LocalityZone?.Names);

            var photos = MergeData(suppliersPriority[AccommodationDataTypes.Photos],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Photos),
                accommodationWithManualCorrection.Photos, p => p == null! || !p.Any());

            var textualDescriptions = MergeData(suppliersPriority[AccommodationDataTypes.TextualDescriptions],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.TextualDescriptions),
                accommodationWithManualCorrection.TextualDescriptions,
                p => p == null! || !p.Any());

            var additionalInfo = MergeMultilingualData(suppliersPriority[AccommodationDataTypes.AdditionalInfo],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.AdditionalInfo)!,
                accommodationWithManualCorrection.AdditionalInfo, p => p == null! || !p.Any());

            var accommodationAmenities = MergeMultilingualData(
                suppliersPriority[AccommodationDataTypes.AccommodationAmenities],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.AccommodationAmenities)!,
                accommodationWithManualCorrection.AccommodationAmenities,
                p => p == null! || !p.Any());

            var scheduleInfo = MergeScheduleInfo(suppliersPriority[AccommodationDataTypes.Schedule],
                supplierAccommodationDetails, accommodationWithManualCorrection);

            var propertyType = MergeData(suppliersPriority[AccommodationDataTypes.PropertyType],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Type),
                accommodationWithManualCorrection.Type, t => t == 0);

            var hasDirectContractList = supplierAccommodationDetails.Select(ac => ac.Value.HasDirectContract).ToList();
            hasDirectContractList.Add(accommodationWithManualCorrection.HasDirectContract);
            var hasDirectContract = MergeBoolData(hasDirectContractList);

            return new MultilingualAccommodation
            (
                string.Empty,
                name: name,
                category: category,
                location: locationInfo,
                photos: photos,
                rating: rating,
                type: propertyType,
                accommodationAmenities: accommodationAmenities,
                contacts: contactInfo,
                additionalInfo: additionalInfo,
                schedule: scheduleInfo,
                textualDescriptions: textualDescriptions,
                hasDirectContract: hasDirectContract,
                isActive: true
            );
        }


        private MultilingualLocationInfo MergeLocationInfo(List<Suppliers> suppliersPriority,
            Dictionary<Suppliers, MultilingualAccommodation> supplierAccommodationDetails,
            MultilingualAccommodation accommodationWithManualCorrection, 
            MultiLanguage<string>? country, 
            MultiLanguage<string>? locality, 
            MultiLanguage<string>? localityZone)
        {
            var address = MergeMultilingualData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Address)!,
                accommodationWithManualCorrection.Location.Address, string.IsNullOrEmpty);

            var coordinates = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Coordinates),
                accommodationWithManualCorrection.Location.Coordinates, point => point == default);

            var pointOfInterests = MergeData(suppliersPriority, supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.PointsOfInterests),
                accommodationWithManualCorrection.Location.PointsOfInterests,
                poi => poi == null! || !poi.Any());

            var locationDescriptionCode = MergeData(suppliersPriority, supplierAccommodationDetails.ToDictionary(
                    d => d.Key,
                    d => d.Value.Location.LocationDescriptionCode),
                accommodationWithManualCorrection.Location.LocationDescriptionCode,
                c => c == LocationDescriptionCodes.Unspecified);

            // CountryCode must be the same, but for understandability merging too
            var countryCode = MergeData(suppliersPriority, supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.CountryCode), accommodationWithManualCorrection.Location.CountryCode,
                string.IsNullOrEmpty);

            return new MultilingualLocationInfo(
                address: address,
                country: country!,
                locality: locality,
                localityZone: localityZone,
                coordinates: coordinates,
                locationDescriptionCode: locationDescriptionCode,
                pointsOfInterests: pointOfInterests,
                countryCode: countryCode,
                supplierLocalityCode: string.Empty,
                supplierLocalityZoneCode: string.Empty);
        }


        private ContactInfo MergeContactInfo(List<Suppliers> suppliersPriority,
            Dictionary<Suppliers, MultilingualAccommodation> supplierAccommodationDetails,
            MultilingualAccommodation accommodationWithManualCorrection)

        {
            var contactInfo = new ContactInfo(new List<string>(), new List<string>(), new List<string>(),
                new List<string>());
            if (accommodationWithManualCorrection.Contacts.Phones.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.Phones);

            if (accommodationWithManualCorrection.Contacts.Emails.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.Emails);

            if (accommodationWithManualCorrection.Contacts.WebSites.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.WebSites);

            if (accommodationWithManualCorrection.Contacts.Faxes.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.Faxes);

            foreach (var supplier in suppliersPriority)
            {
                if (supplierAccommodationDetails.TryGetValue(supplier, out var accommodationDetails))
                {
                    contactInfo.Phones.AddRange(GetNotExistingContacts(accommodationDetails.Contacts.Phones,
                        contactInfo.Phones,
                        p => p.ToNormalizedPhoneNumber()));

                    contactInfo.Emails.AddRange(GetNotExistingContacts(accommodationDetails.Contacts.Emails,
                        contactInfo.Emails,
                        e => e.ToLowerInvariant()));

                    contactInfo.WebSites.AddRange(GetNotExistingContacts(accommodationDetails.Contacts.WebSites,
                        contactInfo.WebSites,
                        w => w.ToLowerInvariant()));

                    contactInfo.Faxes.AddRange(GetNotExistingContacts(accommodationDetails.Contacts.Faxes,
                        contactInfo.Faxes,
                        f => f.ToLowerInvariant()));
                }
            }


            List<string> GetNotExistingContacts(List<string> source, List<string> contacts,
                Func<string, string> normalizer)
            {
                var result = new List<string>();
                foreach (var contact in contacts)
                {
                    if (source.All(c => normalizer(c) != normalizer(contact)))
                        result.Add(contact);
                }

                return result;
            }


            return contactInfo;
        }


        private ScheduleInfo MergeScheduleInfo(List<Suppliers> suppliersPriority,
            Dictionary<Suppliers, MultilingualAccommodation> supplierAccommodationDetails,
            MultilingualAccommodation accommodationWithManualCorrection)
        {
            var checkInTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.CheckInTime),
                accommodationWithManualCorrection.Schedule.CheckInTime, String.IsNullOrEmpty);

            var checkOutTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.CheckOutTime),
                accommodationWithManualCorrection.Schedule.CheckOutTime, String.IsNullOrEmpty);

            var portersStartTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.PortersStartTime),
                accommodationWithManualCorrection.Schedule.PortersStartTime, String.IsNullOrEmpty);

            var portersEndTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.PortersEndTime),
                accommodationWithManualCorrection.Schedule.PortersEndTime, String.IsNullOrEmpty);

            var roomServiceStartTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.RoomServiceStartTime),
                accommodationWithManualCorrection.Schedule.RoomServiceStartTime, String.IsNullOrEmpty);

            var roomServiceEndTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.RoomServiceEndTime),
                accommodationWithManualCorrection.Schedule.RoomServiceEndTime, String.IsNullOrEmpty);

            return new ScheduleInfo(checkInTime, checkOutTime, portersStartTime, portersEndTime, roomServiceStartTime,
                roomServiceEndTime);
        }


        private T MergeData<T>(List<Suppliers> suppliersPriority, Dictionary<Suppliers, T> suppliersData,
            T manualCorrectedData, Func<T, bool> defaultChecker)
        {
            if (!defaultChecker(manualCorrectedData))
                return manualCorrectedData;

            foreach (var supplier in suppliersPriority)
            {
                if (suppliersData.TryGetValue(supplier, out var data))
                    if (!defaultChecker(data))
                        return data;
            }

            return manualCorrectedData;
        }


        private MultiLanguage<T> MergeMultilingualData<T>(List<Suppliers> suppliersPriority,
            Dictionary<Suppliers, MultiLanguage<T>?> suppliersData, MultiLanguage<T>? manualCorrectedData,
            Func<T, bool> defaultChecker)
        {
            var result = new MultiLanguage<T>();
            foreach (var language in Enum.GetValues(typeof(Languages)))
            {
                if ((Languages) language == Languages.Unknown)
                    continue;

                var languageCode = LanguagesHelper.GetLanguageCode((Languages) language);
                var selectedLanguageData = suppliersData.Where(sd => sd.Value != null).ToDictionary(d => d.Key,
                    d => d.Value!.GetValueOrDefault(languageCode));

                var manualCorrectedValue = manualCorrectedData != null
                    ? manualCorrectedData.GetValueOrDefault(languageCode)
                    : default(T);
                var mergedData = MergeData(suppliersPriority, selectedLanguageData, manualCorrectedValue!,
                    defaultChecker);

                result.TrySetValue(languageCode, mergedData);
            }

            return result;
        }


        private bool MergeBoolData(IEnumerable<bool> data)
        {
            foreach (var boolItem in data)
                if (boolItem == true)
                    return true;

            return false;
        }


        private readonly MultilingualDataHelper _multilingualDataHelper;
        private readonly ISuppliersPriorityService _suppliersPriorityService;
    }
}