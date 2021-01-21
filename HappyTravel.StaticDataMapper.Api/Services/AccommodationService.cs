using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.StaticDataMapper.Data.Models;
using Microsoft.EntityFrameworkCore;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.StaticDataMapper.Data.Models.Accommodations;
using HappyTravel.StaticDataMapper.Api.Services.Workers;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(NakijinContext context, IAccommodationsDataMerger accommodationsDataMerger)
        {
            _context = context;
            _accommodationsDataMerger = accommodationsDataMerger;
        }


        public async Task<Result> MatchUncertain(int uncertainMatchId)
        {
            var uncertainMatch = await _context.AccommodationUncertainMatches
                .SingleOrDefaultAsync(um => um.Id == uncertainMatchId && um.IsActive);

            if (uncertainMatch == default)
                return Result.Success();

            var firstAccommodation =
                await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == uncertainMatch.ExistingHtId);
            var secondAccommodation =
                await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == uncertainMatch.NewHtId);

            foreach (var supplierAccommodation in secondAccommodation.SupplierAccommodationCodes)
                firstAccommodation.SupplierAccommodationCodes.TryAdd(supplierAccommodation.Key,
                    supplierAccommodation.Value);

            var utcDate = DateTime.UtcNow;
            
            firstAccommodation.IsCalculated = false;
            firstAccommodation.Modified = utcDate;

            secondAccommodation.IsActive = false;
            secondAccommodation.Modified = utcDate;

            uncertainMatch.IsActive = false;
            uncertainMatch.Modified = utcDate;

            _context.Update(firstAccommodation);
            _context.Update(secondAccommodation);
            _context.Update(uncertainMatch);
            await _context.SaveChangesAsync();

            await _accommodationsDataMerger.Merge(firstAccommodation);

            return Result.Success();
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


        public async Task<Result> AddSuppliersPriority(int id,
            Dictionary<AccommodationDataTypes, List<Suppliers>> suppliersPriority)
        {
            var accommodation = await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == id);
            if (accommodation == default)
                return Result.Failure($"Accommodation with {nameof(id)} {id} does not exist.");

            accommodation.SuppliersPriority = suppliersPriority;
            accommodation.Modified = DateTime.UtcNow;
            accommodation.IsCalculated = false;

            _context.Update(accommodation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }


        public async Task<Result> AddManualCorrection(int id, MultilingualAccommodation manualCorrectedAccommodation)
        {
            var accommodation = await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == id);
            if (accommodation == default)
                return Result.Failure($"Accommodation with {nameof(id)} {id} does not exist.");

            accommodation.AccommodationWithManualCorrections = manualCorrectedAccommodation;
            accommodation.Modified = DateTime.UtcNow;
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

            var calculatedData = await _accommodationsDataMerger.Merge(accommodation);

            accommodation.CalculatedAccommodation = calculatedData;
            accommodation.Modified = DateTime.UtcNow;
            accommodation.IsCalculated = true;

            _context.Update(accommodation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        private Accommodation MapToAccommodation(int htId, MultilingualAccommodation accommodation, string language)
        {
            var name = accommodation.Name.GetValueOrDefault(language);
            var accommodationAmenities = accommodation.AccommodationAmenities.GetValueOrDefault(language);
            var additionalInfo = accommodation.AdditionalInfo.GetValueOrDefault(language);
            var category = accommodation.Category.GetValueOrDefault(language);
            var address = accommodation.Location.Address.GetValueOrDefault(language);
            var localityName = accommodation.Location.Locality.GetValueOrDefault(language);
            var countryName = accommodation.Location.Country.GetValueOrDefault(language);
            var localityZoneName = accommodation.Location.LocalityZone.GetValueOrDefault(language);
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


        private readonly IAccommodationsDataMerger _accommodationsDataMerger;
        private readonly NakijinContext _context;
    }
}