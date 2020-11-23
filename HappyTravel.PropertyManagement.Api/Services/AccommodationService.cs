using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.PropertyManagement.Data;
using HappyTravel.PropertyManagement.Data.Models;
using Microsoft.EntityFrameworkCore;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Api.Services.Workers;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;

namespace HappyTravel.PropertyManagement.Api.Services
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(NakijinContext context, IAccommodationsDataMerger accommodationsDataMerger)
        {
            _context = context;
            _accommodationsDataMerger = accommodationsDataMerger;
        }


        public async Task<Result<Accommodation>> Get(Suppliers supplier, string supplierAccommodationCode)
        {
            var searchJson = "{" + $"\"{supplier.ToString().ToLower()}\":\"{supplierAccommodationCode}\"" + "}";
            var accommodation = await _context.Accommodations
                .Where(ac => EF.Functions.JsonContains(ac.SupplierAccommodationCodes, searchJson))
                .Select(ac => ac.CalculatedAccommodation)
                .SingleOrDefaultAsync();

            if (accommodation.Equals(default(Accommodation)))
                return Result.Failure<Accommodation>("Accommodation does not exists");

            return Result.Success(accommodation);
        }


        public async Task<Result<Accommodation>> Get(int accommodationId)
        {
            var accommodation = await _context.Accommodations
                .Where(ac => ac.Id == accommodationId)
                .Select(ac => ac.CalculatedAccommodation)
                .SingleOrDefaultAsync();

            if (accommodation.Equals(default(Accommodation)))
                return Result.Failure<Accommodation>("Accommodation does not exists");

            return Result.Success(accommodation);
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

            var calculatedData = await _accommodationsDataMerger.Merge(accommodation);

            accommodation.CalculatedAccommodation = calculatedData;
            accommodation.IsCalculated = true;

            _context.Update(accommodation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }


        private readonly IAccommodationsDataMerger _accommodationsDataMerger;
        private readonly NakijinContext _context;
    }
}