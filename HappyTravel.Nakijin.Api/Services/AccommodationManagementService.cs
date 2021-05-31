using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using Microsoft.EntityFrameworkCore;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Api.Services.StaticDataPublication;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Api.Services.Workers.AccommodationDataCalculation;

namespace HappyTravel.Nakijin.Api.Services
{
    public class AccommodationManagementService : IAccommodationManagementService
    {
        public AccommodationManagementService(NakijinContext context,
            IAccommodationDataMerger accommodationDataMerger, AccommodationMappingsCache mappingsCache,
            AccommodationChangePublisher accommodationChangePublisher)
        {
            _context = context;
            _accommodationDataMerger = accommodationDataMerger;
            _mappingsCache = mappingsCache;
            _accommodationChangePublisher = accommodationChangePublisher;
        }


        public async Task<Result> MatchUncertain(int uncertainMatchId)
        {
            var uncertainMatch = await _context.AccommodationUncertainMatches
                .SingleOrDefaultAsync(um => um.Id == uncertainMatchId && um.IsActive);

            if (uncertainMatch == default)
                return Result.Success();

            var (_, isFailure, error) = await Match(uncertainMatch.SourceHtId, uncertainMatch.HtIdToMatch);
            if (isFailure)
                return Result.Failure(error);

            await AddOrUpdateMappings(uncertainMatch.SourceHtId, uncertainMatch.HtIdToMatch);
            
            uncertainMatch.IsActive = false;
            uncertainMatch.Modified = DateTime.UtcNow;

            _context.Update(uncertainMatch);
            await _context.SaveChangesAsync();

            await _mappingsCache.Fill();
            
            await _accommodationChangePublisher.PublishRemoved(uncertainMatch.HtIdToMatch);

            return Result.Success();
        }


        public async Task<Result> MatchAccommodations(int sourceHtId, int htIdToMatch)
        {
            var (_, isFailure, error) = await Match(sourceHtId, htIdToMatch);
            if (isFailure)
                return Result.Failure(error);

            await AddOrUpdateMappings(sourceHtId, htIdToMatch);
            await _context.SaveChangesAsync();

            await _mappingsCache.Fill();

            await _accommodationChangePublisher.PublishRemoved(htIdToMatch);
            
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

            var calculatedData = await _accommodationDataMerger.Merge(accommodation);

            accommodation.CalculatedAccommodation = calculatedData;
            accommodation.Modified = DateTime.UtcNow;
            accommodation.IsCalculated = true;

            _context.Update(accommodation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }


        // This method only make changes on db context - not on db.
        private async Task<Result> Match(int sourceHtId, int htIdToMatch)
        {
            var sourceAccommodation =
                await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == sourceHtId && ac.IsActive);
            var accommodationToMatch =
                await _context.Accommodations.SingleOrDefaultAsync(ac => ac.Id == htIdToMatch && ac.IsActive);

            if (sourceAccommodation == default || accommodationToMatch == default)
                return Result.Failure("Wrong accommodation Id");

            foreach (var supplierAccommodation in accommodationToMatch.SupplierAccommodationCodes)
            {
                if (!sourceAccommodation.SupplierAccommodationCodes.TryAdd(supplierAccommodation.Key,
                    supplierAccommodation.Value))
                    return Result.Failure("Accommodations have dependencies from the same provider.");
            }

            var utcDate = DateTime.UtcNow;

            sourceAccommodation.IsCalculated = false;
            sourceAccommodation.Modified = utcDate;

            accommodationToMatch.IsActive = false;
            accommodationToMatch.DeactivationReason = DeactivationReasons.MatchingWithOther;
            accommodationToMatch.Modified = utcDate;

            _context.Update(sourceAccommodation);
            _context.Update(accommodationToMatch);

            return Result.Success();
        }

        // This method only make changes on db context - not on db.
        // If ht mappings will be not large, may be this method will be used from mapping worker
        private async Task AddOrUpdateMappings(int sourceHtId, int htIdToMap)
        {
            var utcDate = DateTime.UtcNow;
            var dbSourceAccommodationMapping = new HtAccommodationMapping
            {
                HtId = sourceHtId,
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
                dbSourceAccommodationMapping.MappedHtIds.UnionWith(htAccommodationMappingToDeactivate.MappedHtIds);
                htAccommodationMappingToDeactivate.IsActive = false;
                htAccommodationMappingToDeactivate.Modified = utcDate;
                _context.Update(htAccommodationMappingToDeactivate);
            }

            var htAccommodationMapping = await _context.HtAccommodationMappings
                .Where(hm => hm.HtId == sourceHtId)
                .SingleOrDefaultAsync();

            if (htAccommodationMapping != default)
            {
                htAccommodationMapping.MappedHtIds.UnionWith(dbSourceAccommodationMapping.MappedHtIds);
                htAccommodationMapping.Modified = utcDate;
                _context.Update(htAccommodationMapping);

                return;
            }

            dbSourceAccommodationMapping.Created = utcDate;
            _context.Add(dbSourceAccommodationMapping);
        }


        private readonly AccommodationChangePublisher _accommodationChangePublisher;
        private readonly IAccommodationDataMerger _accommodationDataMerger;
        private readonly NakijinContext _context;
        private readonly AccommodationMappingsCache _mappingsCache;
    }
}