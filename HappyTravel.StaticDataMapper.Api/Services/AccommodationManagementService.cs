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
    public class AccommodationManagementService : IAccommodationManagementService
    {
        public AccommodationManagementService(NakijinContext context, IAccommodationsDataMerger accommodationsDataMerger)
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

        

        private readonly IAccommodationsDataMerger _accommodationsDataMerger;
        private readonly NakijinContext _context;
    }
}