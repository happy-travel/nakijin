using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Data;
using HappyTravel.PropertyManagement.Data.Models;
using Microsoft.EntityFrameworkCore;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using Newtonsoft.Json;
using ContactInfo = HappyTravel.PropertyManagement.Data.Models.Accommodations.ContactInfo;

namespace HappyTravel.PropertyManagement.Api.Services
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(NakijinContext context, ISuppliersPriorityService suppliersPriorityService)
        {
            _context = context;
            _suppliersPriorityService = suppliersPriorityService;
        }

        public async Task<Result> AddSuppliersPriorityToAccommodation(int id,
            Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority)
        {
            var accommodation = await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == id);
            if (accommodation == default)
                return Result.Failure($"Accommodation with {nameof(id)} {id} does not exist.");

            accommodation.SuppliersPriority = suppliersPriority;
            accommodation.IsCalculated = false;

            _context.Update(accommodation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> AddManualCorrectionToAccommodation(int id, Accommodation manualCorrectedAccommodation)
        {
            var accommodation = await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == id);
            if (accommodation == default)
                return Result.Failure($"Accommodation with {nameof(id)} {id} does not exist.");

            accommodation.AccommodationWithManualCorrections = manualCorrectedAccommodation;
            accommodation.IsCalculated = false;

            _context.Update(accommodation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> RecalculateAccommodationData(int id)
        {
            var accommodation = await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == id);
            if (accommodation.IsCalculated)
                return Result.Failure($"Accommodation data with {nameof(id)} {id} already calculated");

            var supplierAccommodations = await (from ac in _context.RawAccommodations
                where accommodation.SupplierAccommodationCodes.Values.Contains(ac.SupplierAccommodationId)
                select new
                {
                    Supplier = ac.Supplier,
                    SupplierAccommodationId = ac.SupplierAccommodationId,
                    AccommodationDetails = ac.Accommodation
                }).ToListAsync();

            // Checking coincidence of supplier and accommodation
            supplierAccommodations = (from sa in supplierAccommodations
                join acs in accommodation.SupplierAccommodationCodes
                    on new {Supplier = sa.Supplier, SupplierAccommodationId = sa.SupplierAccommodationId}
                    equals new {Supplier = acs.Key, SupplierAccommodationId = acs.Value}
                select sa).ToList();


            var calculatedData = await MergeAccommodationsData(accommodation, supplierAccommodations
                .ToDictionary(d => d.Supplier,
                    d => JsonConvert.DeserializeObject<AccommodationDetails>(
                        d.AccommodationDetails.RootElement.ToString())));
            accommodation.CalculatedAccommodation = calculatedData;
            accommodation.IsCalculated = true;

            _context.Update(accommodation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Accommodation> MergeAccommodationsData(WideAccommodationDetails wideAccommodationDetails,
            Dictionary<Suppliers, AccommodationDetails> supplierAccommodationDetails)
        {
            var suppliersPriority = wideAccommodationDetails.SuppliersPriority.Any()
                ? wideAccommodationDetails.SuppliersPriority
                : await _suppliersPriorityService.Get();

            var accommodationWithManualCorrection =
                wideAccommodationDetails.AccommodationWithManualCorrections ?? new Accommodation();

            var name = MergeData(suppliersPriority[AccommodationDataTypes.Name],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Name),
                accommodationWithManualCorrection.Name, string.IsNullOrEmpty);

            var category = MergeData(suppliersPriority[AccommodationDataTypes.Category],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Category),
                accommodationWithManualCorrection.Category, string.IsNullOrEmpty);

            var rating = MergeData(suppliersPriority[AccommodationDataTypes.Rating],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Rating),
                accommodationWithManualCorrection.Rating,
                r => r == AccommodationRatings.Unknown || r == 0);

            var contactInfo = MergeContactInfo(suppliersPriority[AccommodationDataTypes.ContactInfo],
                supplierAccommodationDetails, accommodationWithManualCorrection);

            var locationInfo = MergeLocationInfo(suppliersPriority[AccommodationDataTypes.LocationInfo],
                supplierAccommodationDetails, accommodationWithManualCorrection);

            var pictures = MergeData(suppliersPriority[AccommodationDataTypes.Pictures],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Pictures),
                accommodationWithManualCorrection.Pictures, p => p == null || !p.Any());

            var textualDescriptions = MergeData(suppliersPriority[AccommodationDataTypes.TextualDescriptions],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.TextualDescriptions),
                accommodationWithManualCorrection.TextualDescriptions,
                p => p == null || !p.Any());

            var additionalInfo = MergeData(suppliersPriority[AccommodationDataTypes.AdditionalInfo],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.AdditionalInfo),
                accommodationWithManualCorrection.AdditionalInfo, p => p == null || !p.Any());

            var accommodationAmenities = MergeData(suppliersPriority[AccommodationDataTypes.AccommodationAmenities],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.AccommodationAmenities),
                accommodationWithManualCorrection.AccommodationAmenities,
                p => p == null || !p.Any());

            var roomAmenities = MergeData(suppliersPriority[AccommodationDataTypes.RoomAmenities],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.RoomAmenities),
                accommodationWithManualCorrection.RoomAmenities, p => p == null || !p.Any());

            var typeDescription = MergeData(suppliersPriority[AccommodationDataTypes.TypeDescription],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.TypeDescription),
                accommodationWithManualCorrection.TypeDescription, string.IsNullOrEmpty);

            var scheduleInfo = MergeScheduleInfo(suppliersPriority[AccommodationDataTypes.Schedule],
                supplierAccommodationDetails, accommodationWithManualCorrection);
            var propertyType = MergeData(suppliersPriority[AccommodationDataTypes.PropertyType],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Type),
                accommodationWithManualCorrection.Type, t => t == PropertyTypes.Any);

            return new Accommodation
            {
                Name = name,
                Category = category,
                Location = locationInfo,
                Pictures = pictures,
                Rating = rating,
                Type = propertyType,
                AccommodationAmenities = accommodationAmenities,
                TypeDescription = typeDescription,
                ContactInfo = contactInfo,
                AdditionalInfo = additionalInfo,
                RoomAmenities = roomAmenities,
                ScheduleInfo = scheduleInfo,
                TextualDescriptions = textualDescriptions,
            };
        }

        private SlimLocationInfo MergeLocationInfo(in List<Suppliers> suppliersPriority,
            in Dictionary<Suppliers, AccommodationDetails> supplierAccommodationDetails,
            in Accommodation accommodationWithManualCorrection)
        {
            var address = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Address),
                accommodationWithManualCorrection.Location.Address, string.IsNullOrEmpty);
            var country = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Country),
                accommodationWithManualCorrection.Location.Country, string.IsNullOrEmpty);
            var locality = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Locality),
                accommodationWithManualCorrection.Location.Locality, string.IsNullOrEmpty);
            var localityZone = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.LocalityZone),
                accommodationWithManualCorrection.Location.LocalityZone, string.IsNullOrEmpty);

            var coordinates = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Coordinates),
                accommodationWithManualCorrection.Location.Coordinates, point => point == default);

            return new SlimLocationInfo(address, country, locality, localityZone, coordinates);
        }

        private ContactInfo MergeContactInfo(in List<Suppliers> suppliersPriority,
            in Dictionary<Suppliers, AccommodationDetails> supplierAccommodationDetails,
            in Accommodation accommodationWithManualCorrection)

        {
            var contactInfo = new ContactInfo();
            if (accommodationWithManualCorrection.ContactInfo.Phones.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.ContactInfo.Phones);

            if (accommodationWithManualCorrection.ContactInfo.Emails.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.ContactInfo.Emails);

            if (accommodationWithManualCorrection.ContactInfo.WebSites.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.ContactInfo.WebSites);

            if (accommodationWithManualCorrection.ContactInfo.Faxes.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.ContactInfo.Faxes);

            foreach (var supplier in suppliersPriority)
            {
                if (supplierAccommodationDetails.TryGetValue(supplier, out var accommodationDetails))
                {
                    if (!string.IsNullOrEmpty(accommodationDetails.Contacts.Phone)
                        && contactInfo.Phones.All(p
                            => p.ToNormalizedPhoneNumber() !=
                            accommodationDetails.Contacts.Phone.ToNormalizedPhoneNumber()))
                        contactInfo.Phones.Add(accommodationDetails.Contacts.Phone);

                    if (!string.IsNullOrEmpty(accommodationDetails.Contacts.Email)
                        && contactInfo.Emails.All(e
                            => e.ToLowerInvariant() != accommodationDetails.Contacts.Email.ToLowerInvariant()))
                        contactInfo.Emails.Add(accommodationDetails.Contacts.Email);

                    if (!string.IsNullOrEmpty(accommodationDetails.Contacts.WebSite)
                        && contactInfo.WebSites.All(w
                            => w.ToLowerInvariant() != accommodationDetails.Contacts.WebSite.ToLowerInvariant()))
                        contactInfo.WebSites.Add(accommodationDetails.Contacts.WebSite);

                    if (!string.IsNullOrEmpty(accommodationDetails.Contacts.Fax)
                        && contactInfo.Faxes.All(f
                            => f.ToLowerInvariant() != accommodationDetails.Contacts.Fax.ToLowerInvariant()))
                        contactInfo.Emails.Add(accommodationDetails.Contacts.Fax);
                }
            }


            return contactInfo;
        }

        private ScheduleInfo MergeScheduleInfo(in List<Suppliers> suppliersPriority,
            in Dictionary<Suppliers, AccommodationDetails> supplierAccommodationDetails,
            in Accommodation accommodationWithManualCorrection)
        {
            var checkInTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.CheckInTime),
                accommodationWithManualCorrection.ScheduleInfo.CheckInTime, String.IsNullOrEmpty);

            var checkOutTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.CheckOutTime),
                accommodationWithManualCorrection.ScheduleInfo.CheckOutTime, String.IsNullOrEmpty);

            var portersStartTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.PortersStartTime),
                accommodationWithManualCorrection.ScheduleInfo.PortersStartTime, String.IsNullOrEmpty);

            var portersEndTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.PortersEndTime),
                accommodationWithManualCorrection.ScheduleInfo.PortersEndTime, String.IsNullOrEmpty);

            var roomServiceStartTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.RoomServiceStartTime),
                accommodationWithManualCorrection.ScheduleInfo.RoomServiceStartTime, String.IsNullOrEmpty);

            var roomServiceEndTime = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Schedule.RoomServiceEndTime),
                accommodationWithManualCorrection.ScheduleInfo.RoomServiceEndTime, String.IsNullOrEmpty);

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


        private readonly ISuppliersPriorityService _suppliersPriorityService;
        private readonly NakijinContext _context;
    }
}