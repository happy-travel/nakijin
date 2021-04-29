using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using HappyTravel.MultiLanguage;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Data.Models.Mappers;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers
{
    public class AccommodationDataMerger : IAccommodationsDataMerger
    {
        public AccommodationDataMerger(NakijinContext context, ISuppliersPriorityService suppliersPriorityService,
            IOptions<StaticDataLoadingOptions> options, MultilingualDataHelper multilingualDataHelper,
            ILoggerFactory loggerFactory, TracerProvider tracerProvider)
        {
            _context = context;
            _suppliersPriorityService = suppliersPriorityService;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<AccommodationDataMerger>();
            _multilingualDataHelper = multilingualDataHelper;
            _tracerProvider = tracerProvider;
        }

        
        public async Task Calculate(List<Suppliers> suppliers, CancellationToken cancellationToken)
        {
            var currentSpan = Tracer.CurrentSpan;
            var tracer = _tracerProvider.GetTracer(nameof(AccommodationDataMerger));
            _context.Database.SetCommandTimeout(_options.DbCommandTimeOut);

            foreach (var supplier in suppliers)
            {
                try
                {
                    using var supplierAccommodationsDataCalculatingSpan = tracer.StartActiveSpan(
                        $"{nameof(Calculate)} accommodations of supplier {supplier.ToString()}",
                        SpanKind.Internal, currentSpan);

                    _logger.LogCalculatingAccommodationsDataStart(
                        $"Started calculation accommodations data of supplier {supplier.ToString()}");

                    // TODO: uncomment when ef core will support work with dictionaries (EF core 6) or change dictionary SupplierAccommodationCodes to JsonDocument

                    // var lastUpdatedDate = await _context.DataUpdateHistories.Where(dh => dh.Supplier == supplier)
                    //     .OrderByDescending(dh => dh.UpdateTime)
                    //     .Select(dh => dh.UpdateTime)
                    //     .FirstOrDefaultAsync(cancellationToken);

                    //var changedSupplierHotelCodes = new List<string>();
                    var notCalculatedAccommodations = new List<RichAccommodationDetails>();
                    var skip = 0;
                    do
                    {
                        // changedSupplierHotelCodes = await _context.RawAccommodations
                        //     .Where(ac => ac.Supplier == supplier && ac.Modified >= lastUpdatedDate)
                        //     .OrderBy(ac => ac.SupplierAccommodationId)
                        //     .Skip(skip)
                        //     .Take(_options.MergingBatchSize)
                        //     .Select(ac => ac.SupplierAccommodationId)
                        //     .ToListAsync(cancellationToken);
                        //
                        //  var notCalculatedAccommodations = await _context.Accommodations
                        // .Where(ac => changedSupplierHotelCodes.Contains(ac.SupplierAccommodationCodes[supplier]))
                        //     .ToListAsync(cancellationToken);

                        notCalculatedAccommodations = await _context
                            .Accommodations.Where(ac
                                => ac.IsActive && EF.Functions.JsonExists(ac.SupplierAccommodationCodes,
                                    supplier.ToString().ToLower()))
                            .Skip(skip)
                            .Take(_options.MergingBatchSize)
                            .ToListAsync(cancellationToken);

                        skip += notCalculatedAccommodations.Count;

                        await CalculateBatch(notCalculatedAccommodations, cancellationToken);
                    } while (notCalculatedAccommodations.Count > 0 /*changedSupplierHotelCodes.Count > 0*/);

                    _context.DataUpdateHistories.Add(new DataUpdateHistory
                    {
                        Supplier = supplier,
                        Type = DataUpdateTypes.DataCalculation,
                        UpdateTime = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogCalculatingAccommodationsDataFinish(
                        $"Finished calculation of supplier {supplier.ToString()} data.");
                }
                catch (TaskCanceledException)
                {
                    _logger.LogCalculatingAccommodationsDataCancel(
                        $"Calculating data of supplier {supplier.ToString()} was cancelled by client request");
                }
                catch (Exception ex)
                {
                    _logger.LogCalculatingAccommodationsDataError(ex);
                }
            }
        }


        public async Task MergeAll(CancellationToken cancellationToken)
        {
            var currentSpan = Tracer.CurrentSpan;
            var tracer = _tracerProvider.GetTracer(nameof(AccommodationDataMerger));
            _context.Database.SetCommandTimeout(_options.DbCommandTimeOut);

            try
            {
                using var accommodationsDataMergingSpan = tracer.StartActiveSpan($"{nameof(MergeAll)} accommodations",
                    SpanKind.Internal, currentSpan);

                _logger.LogMergingAccommodationsDataStart($"Started merging accommodations data");

                var notCalculatedAccommodations = new List<RichAccommodationDetails>();
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    notCalculatedAccommodations = await _context.Accommodations
                        .Where(ac => !ac.IsCalculated)
                        .OrderBy(ac => ac.Id)
                        .Take(_options.MergingBatchSize)
                        .ToListAsync(cancellationToken);

                    await CalculateBatch(notCalculatedAccommodations, cancellationToken);
                } while (notCalculatedAccommodations.Count > 0);

                _logger.LogMergingAccommodationsDataFinish($"Finished merging accommodations data");
            }
            catch (TaskCanceledException)
            {
                _logger.LogMergingAccommodationsDataCancel($"Merging accommodations was canceled by client request.");
            }
            catch (Exception ex)
            {
                _logger.LogMergingAccommodationsDataError(ex);
            }
        }


        public async Task<MultilingualAccommodation> Merge(RichAccommodationDetails accommodation)
        {
            var supplierAccommodations = await (from ac in _context.RawAccommodations
                    where accommodation.SupplierAccommodationCodes.Values.Contains(ac.SupplierAccommodationId)
                    select new RawAccommodation
                    {
                        Supplier = ac.Supplier,
                        SupplierAccommodationId = ac.SupplierAccommodationId,
                        Accommodation = ac.Accommodation
                    })
                .ToListAsync();

            return await Merge(accommodation, supplierAccommodations);
        }

        
        private async Task CalculateBatch(List<RichAccommodationDetails> notCalculatedAccommodations,
            CancellationToken cancellationToken)
        {
            var supplierAccommodationIds = notCalculatedAccommodations
                .SelectMany(ac => ac.SupplierAccommodationCodes).Select(ac => ac.Value).ToList();

            var rawAccommodations = await _context.RawAccommodations.Where(ra
                    => supplierAccommodationIds.Contains(ra.SupplierAccommodationId))
                .Select(ra => new RawAccommodation
                {
                    Accommodation = ra.Accommodation,
                    Supplier = ra.Supplier,
                    SupplierAccommodationId = ra.SupplierAccommodationId
                })
                .ToListAsync(cancellationToken);

            foreach (var ac in notCalculatedAccommodations)
            {
                var supplierAccommodations = (from ra in rawAccommodations
                    join sa in ac.SupplierAccommodationCodes on ra.SupplierAccommodationId equals sa.Value
                    where ra.Supplier == sa.Key
                    select ra).ToList();

                var calculatedData = await Merge(ac, supplierAccommodations);

                var dbAccommodation = new RichAccommodationDetails();
                dbAccommodation.Id = ac.Id;
                dbAccommodation.IsCalculated = true;
                dbAccommodation.CalculatedAccommodation = calculatedData;
                dbAccommodation.HasDirectContract = calculatedData.HasDirectContract;
                dbAccommodation.MappingData =
                    _multilingualDataHelper.GetAccommodationDataForMapping(calculatedData);
                dbAccommodation.Modified = DateTime.UtcNow;
                _context.Accommodations.Attach(dbAccommodation);

                var dbEntry = _context.Entry(dbAccommodation);
                dbEntry.Property(p => p.CalculatedAccommodation).IsModified = true;
                dbEntry.Property(p => p.HasDirectContract).IsModified = true;
                dbEntry.Property(p => p.IsCalculated).IsModified = true;
                dbEntry.Property(p => p.Modified).IsModified = true;
                dbEntry.Property(p => p.MappingData).IsModified = true;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _context.ChangeTracker.Entries()
                .Where(e => e.Entity != null)
                .Where(e => e.State != EntityState.Detached)
                .ToList()
                .ForEach(e => e.State = EntityState.Detached);
        }


        private async Task<MultilingualAccommodation> Merge(RichAccommodationDetails accommodation,
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
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Name),
                accommodationWithManualCorrection.Name, string.IsNullOrEmpty);

            var category = MergeMultilingualData(suppliersPriority[AccommodationDataTypes.Category],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Category),
                accommodationWithManualCorrection.Category, string.IsNullOrEmpty);

            var rating = MergeData(suppliersPriority[AccommodationDataTypes.Rating],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.Rating),
                accommodationWithManualCorrection.Rating,
                r => r == 0);

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

            var additionalInfo = MergeMultilingualData(suppliersPriority[AccommodationDataTypes.AdditionalInfo],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.AdditionalInfo),
                accommodationWithManualCorrection.AdditionalInfo, p => p == null || !p.Any());

            var accommodationAmenities = MergeMultilingualData(
                suppliersPriority[AccommodationDataTypes.AccommodationAmenities],
                supplierAccommodationDetails.ToDictionary(s => s.Key, s => s.Value.AccommodationAmenities),
                accommodationWithManualCorrection.AccommodationAmenities,
                p => p == null || !p.Any());

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
                String.Empty,
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
                hasDirectContract: hasDirectContract
            );
        }


        private MultilingualLocationInfo MergeLocationInfo(List<Suppliers> suppliersPriority,
            Dictionary<Suppliers, MultilingualAccommodation> supplierAccommodationDetails,
            MultilingualAccommodation accommodationWithManualCorrection)
        {
            var address = MergeMultilingualData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Address),
                accommodationWithManualCorrection.Location.Address, string.IsNullOrEmpty);

            // TODO: Get country, locality, localityZone from db 
            var country = MergeMultilingualData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Country),
                accommodationWithManualCorrection.Location.Country, string.IsNullOrEmpty);
            var locality = MergeMultilingualData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Locality),
                accommodationWithManualCorrection.Location.Locality, string.IsNullOrEmpty);
            var localityZone = MergeMultilingualData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.LocalityZone),
                accommodationWithManualCorrection.Location.LocalityZone, string.IsNullOrEmpty);

            var coordinates = MergeData(suppliersPriority,
                supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.Coordinates),
                accommodationWithManualCorrection.Location.Coordinates, point => point == default);

            var pointOfInterests = MergeData(suppliersPriority, supplierAccommodationDetails.ToDictionary(d => d.Key,
                    d => d.Value.Location.PointsOfInterests),
                accommodationWithManualCorrection.Location.PointsOfInterests,
                poi => poi == null || !poi.Any());

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
                country: country,
                locality: locality,
                localityZone: localityZone,
                coordinates: coordinates,
                locationDescriptionCode: locationDescriptionCode,
                pointsOfInterests: pointOfInterests,
                countryCode: countryCode,
                supplierLocalityCode: string.Empty,
                supplierLocalityZoneCode: String.Empty);
        }


        private ContactInfo MergeContactInfo(List<Suppliers> suppliersPriority,
            Dictionary<Suppliers, MultilingualAccommodation> supplierAccommodationDetails,
            MultilingualAccommodation accommodationWithManualCorrection)

        {
            var contactInfo = new ContactInfo(new List<string>(), new List<string>(), new List<string>(),
                new List<string>());
            if (accommodationWithManualCorrection.Contacts.Phones != null &&
                accommodationWithManualCorrection.Contacts.Phones.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.Phones);

            if (accommodationWithManualCorrection.Contacts.Emails != null &&
                accommodationWithManualCorrection.Contacts.Emails.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.Emails);

            if (accommodationWithManualCorrection.Contacts.WebSites != null &&
                accommodationWithManualCorrection.Contacts.WebSites.Any())
                contactInfo.Phones.AddRange(accommodationWithManualCorrection.Contacts.WebSites);

            if (accommodationWithManualCorrection.Contacts.Faxes != null &&
                accommodationWithManualCorrection.Contacts.Faxes.Any())
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
            Dictionary<Suppliers, MultiLanguage<T>?> suppliersData, MultiLanguage<T> manualCorrectedData,
            Func<T, bool> defaultChecker)
        {
            var result = new MultiLanguage<T>();
            foreach (var language in Enum.GetValues(typeof(Languages)))
            {
                if ((Languages) language == Languages.Unknown)
                    continue;

                var languageCode = LanguagesHelper.GetLanguageCode((Languages) language);
                var selectedLanguageData = suppliersData.Where(sd => sd.Value != null).ToDictionary(d => d.Key,
                    d => d.Value.GetValueOrDefault(languageCode));

                var manualCorrectedValue = manualCorrectedData != null
                    ? manualCorrectedData.GetValueOrDefault(languageCode)
                    : default(T);
                var mergedData = MergeData<T>(suppliersPriority, selectedLanguageData, manualCorrectedValue!,
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

        private readonly TracerProvider _tracerProvider;
        private readonly StaticDataLoadingOptions _options;
        private readonly MultilingualDataHelper _multilingualDataHelper;
        private readonly ISuppliersPriorityService _suppliersPriorityService;
        private readonly NakijinContext _context;
        private readonly ILogger<AccommodationDataMerger> _logger;
    }
}