using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.PropertyManagement.Api.Services
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(NakijinContext context, ISuppliersPriorityService suppliersPriorityService,
            IOptions<AccommodationsPreloaderOptions> options, ILoggerFactory loggerFactory)
        {
            _context = context;
            _suppliersPriorityService = suppliersPriorityService;
            _batchSize = options.Value.BatchSize;
            _logger = loggerFactory.CreateLogger<AccommodationService>();
        }


        public async Task MergeAccommodationsData(CancellationToken cancellationToken)
        {
            var notCalculatedAccommodations = new List<RichAccommodationDetails>();
            var skip = 0;
            try
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    notCalculatedAccommodations = await _context.Accommodations
                        .Where(ac => !ac.IsCalculated)
                        .OrderBy(ac => ac.Id)
                        .Skip(skip)
                        .Take(_batchSize)
                        .ToListAsync();
                    skip += notCalculatedAccommodations.Count;

                    foreach (var ac in notCalculatedAccommodations)
                    {
                        var calculatedData = await MergeData(ac);

                        var dbAccommodation = new RichAccommodationDetails();
                        dbAccommodation.Id = ac.Id;
                        dbAccommodation.IsCalculated = true;
                        dbAccommodation.CalculatedAccommodation = calculatedData;
                        _context.Accommodations.Attach(dbAccommodation);
                        _context.Entry(dbAccommodation).Property(p => p.CalculatedAccommodation).IsModified = true;
                        _context.Entry(dbAccommodation).Property(p => p.IsCalculated).IsModified = true;
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                } while (notCalculatedAccommodations.Count > 0);
            }
            catch (TaskCanceledException)
            {
                // TODO: Use generated logging extension methods
                _logger.Log(LogLevel.Information,
                    $"Merging accommodations was canceled by client request.");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"Merging accommodations was stopped because of {ex.Message}");
            }
        }


        public async Task<Result> AddSuppliersPriority(int id, Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority)
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


        public async Task<Result> AddManualCorrection(int id, Accommodation manualCorrectedAccommodation)
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


        public async Task<Result> RecalculateData(int id)
        {
            var accommodation = await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == id);
            if (accommodation.IsCalculated)
                return Result.Failure($"Accommodation data with {nameof(id)} {id} already calculated");

            var calculatedData = await MergeData(accommodation);

            accommodation.CalculatedAccommodation = calculatedData;
            accommodation.IsCalculated = true;

            _context.Update(accommodation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }


        public async Task<Accommodation> MergeData(RichAccommodationDetails accommodation)
        {
            var supplierAccommodations = await (from ac in _context.RawAccommodations
                where accommodation.SupplierAccommodationCodes.Values.Contains(ac.SupplierAccommodationId)
                select new
                {
                    Supplier = ac.Supplier,
                    SupplierAccommodationId = ac.SupplierAccommodationId,
                    AccommodationDetails = ac.Accommodation
                }).ToListAsync();

            // Checking match of supplier and accommodation
            supplierAccommodations = (from sa in supplierAccommodations
                join acs in accommodation.SupplierAccommodationCodes
                    on new {Supplier = sa.Supplier, SupplierAccommodationId = sa.SupplierAccommodationId}
                    equals new {Supplier = acs.Key, SupplierAccommodationId = acs.Value}
                select sa).ToList();

            var supplierAccommodationDetails = supplierAccommodations.ToDictionary(d => d.Supplier,
                d => JsonConvert.DeserializeObject<Accommodation>(d.AccommodationDetails.RootElement.ToString()));

            
            var suppliersPriority = accommodation.SuppliersPriority.Any()
                ? accommodation.SuppliersPriority
                : await _suppliersPriorityService.Get();

            var accommodationWithManualCorrection = accommodation.AccommodationWithManualCorrections;
            // var accommodationWithManualCorrection =
            //     wideAccommodationDetails.AccommodationWithManualCorrections ?? new Accommodation();

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

            var photos = MergeData(suppliersPriority[AccommodationDataTypes.Photos],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Photos),
                accommodationWithManualCorrection.Photos, p => p == null || !p.Any());

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

            var scheduleInfo = MergeScheduleInfo(suppliersPriority[AccommodationDataTypes.Schedule],
                supplierAccommodationDetails, accommodationWithManualCorrection);

            var propertyType = MergeData(suppliersPriority[AccommodationDataTypes.PropertyType],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Type),
                accommodationWithManualCorrection.Type, t => t == PropertyTypes.Any);

            return new Accommodation
            (
                id: String.Empty,
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
                textualDescriptions: textualDescriptions
            );
        }


        private LocationInfo MergeLocationInfo(List<Suppliers> suppliersPriority, Dictionary<Suppliers, Accommodation> supplierAccommodationDetails,
            Accommodation accommodationWithManualCorrection)
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

            var pointOfInterests = MergeData(suppliersPriority, supplierAccommodationDetails.ToDictionary(d => d.Key,
                d => d.Value.Location.PointsOfInterests), accommodationWithManualCorrection.Location.PointsOfInterests, poi => poi == null || !poi.Any());

            var locationDescriptionCode = MergeData(suppliersPriority, supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.LocationDescriptionCode), accommodationWithManualCorrection.Location.LocationDescriptionCode,
                c => c == LocationDescriptionCodes.Unspecified);

            // CountryCode must be the same, but for understandability merging too
            var countryCode = MergeData(suppliersPriority, supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.CountryCode), accommodationWithManualCorrection.Location.CountryCode,
                string.IsNullOrEmpty);

            return new LocationInfo(
                address: address,
                country: country,
                locality: locality,
                localityZone: localityZone,
                coordinates: coordinates,
                locationDescriptionCode: locationDescriptionCode,
                pointsOfInterests: pointOfInterests,
                countryCode: countryCode,
                localityCode: String.Empty,
                localityZoneCode: String.Empty);
        }


        private ContactInfo MergeContactInfo(List<Suppliers> suppliersPriority, Dictionary<Suppliers, Accommodation> supplierAccommodationDetails,
            Accommodation accommodationWithManualCorrection)

        {
            var contactInfo = new ContactInfo(new List<string>(), new List<string>(), new List<string>(), new List<string>());
            if (accommodationWithManualCorrection.Contacts.Phones != null && accommodationWithManualCorrection.Contacts.Phones.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.Phones);

            if (accommodationWithManualCorrection.Contacts.Emails != null && accommodationWithManualCorrection.Contacts.Emails.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.Emails);

            if (accommodationWithManualCorrection.Contacts.WebSites != null && accommodationWithManualCorrection.Contacts.WebSites.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.WebSites);

            if (accommodationWithManualCorrection.Contacts.Faxes != null && accommodationWithManualCorrection.Contacts.Faxes.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.Faxes);

            foreach (var supplier in suppliersPriority)
            {
                if (supplierAccommodationDetails.TryGetValue(supplier, out var accommodationDetails))
                {
                    contactInfo.Phones.AddRange(GetNotExistingContacts(accommodationDetails.Contacts.Phones, contactInfo.Phones,
                        p => p.ToNormalizedPhoneNumber()));

                    contactInfo.Emails.AddRange(GetNotExistingContacts(accommodationDetails.Contacts.Emails, contactInfo.Emails,
                        e => e.ToLowerInvariant()));

                    contactInfo.WebSites.AddRange(GetNotExistingContacts(accommodationDetails.Contacts.WebSites, contactInfo.WebSites,
                        w => w.ToLowerInvariant()));

                    contactInfo.Faxes.AddRange(GetNotExistingContacts(accommodationDetails.Contacts.Faxes, contactInfo.Faxes,
                        f => f.ToLowerInvariant()));
                }
            }


            List<string> GetNotExistingContacts(List<string> source, List<string> contacts, Func<string, string> normalizer)
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


        private ScheduleInfo MergeScheduleInfo(List<Suppliers> suppliersPriority, Dictionary<Suppliers, Accommodation> supplierAccommodationDetails,
            Accommodation accommodationWithManualCorrection)
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


        private T MergeData<T>(List<Suppliers> suppliersPriority, Dictionary<Suppliers, T> suppliersData, T manualCorrectedData, Func<T, bool> defaultChecker)
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

        
        private readonly int _batchSize;
        private readonly ISuppliersPriorityService _suppliersPriorityService;
        private readonly NakijinContext _context;
        private readonly ILogger<AccommodationService> _logger;
    }
}