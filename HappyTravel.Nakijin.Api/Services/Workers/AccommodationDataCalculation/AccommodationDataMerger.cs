using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Data.Models.Mappers;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers.AccommodationDataCalculation
{
    public class AccommodationDataMerger : IAccommodationDataMerger
    {
        public AccommodationDataMerger(NakijinContext context, AccommodationMergerHelper mergerHelper,
            IOptions<StaticDataLoadingOptions> options, MultilingualDataHelper multilingualDataHelper,
            ILoggerFactory loggerFactory, TracerProvider tracerProvider)
        {
            _context = context;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<AccommodationDataMerger>();
            _multilingualDataHelper = multilingualDataHelper;
            _tracerProvider = tracerProvider;
            _mergerHelper = mergerHelper;
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

                    var lastUpdatedDate = await GetLastUpdateDate(supplier);
                    var updateDate = DateTime.UtcNow;
                    
                    var changedSupplierHotelCodes = new List<string>();
                    var skip = 0;
                    do
                    {
                        changedSupplierHotelCodes = await _context.RawAccommodations
                            .Where(ac => ac.Supplier == supplier && ac.Modified >= lastUpdatedDate)
                            .OrderBy(ac => ac.SupplierAccommodationId)
                            .Skip(skip)
                            .Take(_options.MergingBatchSize)
                            .Select(ac => ac.SupplierAccommodationId)
                            .ToListAsync(cancellationToken);

                        if (changedSupplierHotelCodes.Count == 0)
                            break;

                        var entityType = _context.Model.FindEntityType(typeof(RichAccommodationDetails))!;
                        var tableName = entityType.GetTableName()!;
                        var columnName = entityType.GetProperty(nameof(RichAccommodationDetails.SupplierAccommodationCodes))
                            .GetColumnName(StoreObjectIdentifier.Table(tableName, null))!;

                        var parameters = new List<string>(changedSupplierHotelCodes);
                        parameters.Add(supplier.ToString().FirstCharToLower());

                        // TODO: remove raw sql when ef core will support queries with dictionaries
                        var notCalculatedAccommodations = await _context.Accommodations
                            .FromSqlRaw(
                                @$"SELECT * FROM ""{tableName}"" a 
                                   WHERE a.""{columnName}""->> {{{changedSupplierHotelCodes.Count}}} 
                                   in ({string.Join(',', changedSupplierHotelCodes.Select((_, index) => $"{{{index}}}"))})",
                                parameters.Select(p => (object) p).ToArray())
                            .Where(a => a.IsActive)
                            .ToListAsync(cancellationToken);

                        skip += changedSupplierHotelCodes.Count;

                        await CalculateBatch(notCalculatedAccommodations, cancellationToken);
                    } while (changedSupplierHotelCodes.Count > 0);

                    await AddUpdateDateToHistory(supplier, updateDate);
                    
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


            Task<DateTime> GetLastUpdateDate(Suppliers supplier)
                => _context.DataUpdateHistories.Where(dh => dh.Supplier == supplier && 
                        dh.Type == DataUpdateTypes.DataCalculation)
                    .OrderByDescending(dh => dh.UpdateTime)
                    .Select(dh => dh.UpdateTime)
                    .FirstOrDefaultAsync(cancellationToken);
            
            
            Task AddUpdateDateToHistory(Suppliers supplier, DateTime date)
            {
                _context.DataUpdateHistories.Add(new DataUpdateHistory
                {
                    Supplier = supplier,
                    Type = DataUpdateTypes.DataCalculation,
                    UpdateTime = date
                });
                
                return _context.SaveChangesAsync(cancellationToken);
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

            return await _mergerHelper.Merge(accommodation, supplierAccommodations);
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

                var calculatedData = await _mergerHelper.Merge(ac, supplierAccommodations);

                var dbAccommodation = new RichAccommodationDetails();
                dbAccommodation.Id = ac.Id;
                dbAccommodation.IsCalculated = true;
                dbAccommodation.CalculatedAccommodation = calculatedData;
                dbAccommodation.HasDirectContract = calculatedData.HasDirectContract;
                dbAccommodation.KeyData =
                    _multilingualDataHelper.GetAccommodationKeyData(calculatedData);
                dbAccommodation.Modified = DateTime.UtcNow;
                _context.Accommodations.Attach(dbAccommodation);

                var dbEntry = _context.Entry(dbAccommodation);
                dbEntry.Property(p => p.CalculatedAccommodation).IsModified = true;
                dbEntry.Property(p => p.HasDirectContract).IsModified = true;
                dbEntry.Property(p => p.IsCalculated).IsModified = true;
                dbEntry.Property(p => p.Modified).IsModified = true;
                dbEntry.Property(p => p.KeyData).IsModified = true;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _context.ChangeTracker.Entries()
                .Where(e => e.Entity != null)
                .Where(e => e.State != EntityState.Detached)
                .ToList()
                .ForEach(e => e.State = EntityState.Detached);
        }


        private readonly AccommodationMergerHelper _mergerHelper;
        private readonly TracerProvider _tracerProvider;
        private readonly StaticDataLoadingOptions _options;
        private readonly MultilingualDataHelper _multilingualDataHelper;
        private readonly NakijinContext _context;
        private readonly ILogger<AccommodationDataMerger> _logger;
    }
}