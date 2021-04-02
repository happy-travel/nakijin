using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using Microsoft.EntityFrameworkCore;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Api.Services.Workers;

namespace HappyTravel.Nakijin.Api.Services
{
    public class AccommodationManagementService : IAccommodationManagementService
    {
        public AccommodationManagementService(NakijinContext context,
            IAccommodationsDataMerger accommodationsDataMerger)
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

            var (_, isFailure, error) = await Match(uncertainMatch.FirstHtId, uncertainMatch.SecondHtId);
            if (isFailure)
                return Result.Failure(error);

            await AddOrUpdateMappings(uncertainMatch.FirstHtId, uncertainMatch.SecondHtId);

            uncertainMatch.IsActive = false;
            uncertainMatch.Modified = DateTime.UtcNow;

            _context.Update(uncertainMatch);
            await _context.SaveChangesAsync();

            // No need to check for failure
            await RecalculateData(uncertainMatch.FirstHtId);

            return Result.Success();
        }


        public async Task<Result> MatchAccommodations(int htId, int htIdToMatch)
        {
            var (_, isFailure, error) = await Match(htId, htIdToMatch);
            if (isFailure)
                return Result.Failure(error);

            await AddOrUpdateMappings(htId, htIdToMatch);
            await _context.SaveChangesAsync();
            // No need to check for failure
            await RecalculateData(htId);

            return Result.Success();
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


        // This method only make changes on db context - not on db.
        private async Task<Result> Match(int htId, int htIdToMatch)
        {
            var accommodation =
                await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == htId && ac.IsActive);
            var accommodationToMatch =
                await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == htIdToMatch && ac.IsActive);

            if (accommodation == default || accommodationToMatch == default)
                return Result.Failure("Wrong accommodation Id");

            foreach (var supplierAccommodation in accommodationToMatch.SupplierAccommodationCodes)
                if (!accommodation.SupplierAccommodationCodes.TryAdd(supplierAccommodation.Key,
                    supplierAccommodation.Value))
                    return Result.Failure("Accommodations have dependencies from the same provider.");

            var utcDate = DateTime.UtcNow;

            accommodation.IsCalculated = false;
            accommodation.Modified = utcDate;

            accommodationToMatch.IsActive = false;
            accommodationToMatch.Modified = utcDate;

            _context.Update(accommodation);
            _context.Update(accommodationToMatch);

            return Result.Success();
        }

        // This method only make changes on db context - not on db.
        // If ht mappings will be not large, may be this method will be used from mapping worker
        private async Task AddOrUpdateMappings(int htId, int htIdToMap)
        {
            var utcDate = DateTime.UtcNow;
            var dbHtAccommodationMapping = new HtAccommodationMapping
            {
                HtId = htId,
                MappedHtIds = new HashSet<int>() {htIdToMap},
                Modified = utcDate,
                IsActive = true
            };

            var htAccommodationMappingToDeactivate = await
                _context.HtAccommodationMappings
                    .Where(hm => hm.HtId == htIdToMap && hm.IsActive)
                    .SingleOrDefaultAsync();

            if (htAccommodationMappingToDeactivate != default)
            {
                dbHtAccommodationMapping.MappedHtIds.UnionWith(htAccommodationMappingToDeactivate.MappedHtIds);
                htAccommodationMappingToDeactivate.IsActive = false;
                htAccommodationMappingToDeactivate.Modified = utcDate;
                _context.Update(htAccommodationMappingToDeactivate);
            }

            var htAccommodationMapping = await _context.HtAccommodationMappings
                .Where(hm => hm.HtId == htId)
                .SingleOrDefaultAsync();

            if (htAccommodationMapping != default)
            {
                htAccommodationMapping.MappedHtIds.UnionWith(dbHtAccommodationMapping.MappedHtIds);
                htAccommodationMapping.Modified = utcDate;
                _context.Update(htAccommodationMapping);

                return;
            }

            dbHtAccommodationMapping.Created = utcDate;
            _context.Add(dbHtAccommodationMapping);
        }


        private readonly IAccommodationsDataMerger _accommodationsDataMerger;
        private readonly NakijinContext _context;
    }
}