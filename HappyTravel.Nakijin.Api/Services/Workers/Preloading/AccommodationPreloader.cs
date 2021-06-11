using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Mappers;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Api.Models;
using HappyTravel.LocationNameNormalizer;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers.Preloading
{
    public class AccommodationPreloader : IAccommodationPreloader
    {
        public AccommodationPreloader(NakijinContext context,
            IConnectorClient connectorClient, ILoggerFactory loggerFactory,
            IOptions<StaticDataLoadingOptions> options, IOptions<SuppliersOptions> supplierOptions,
            ILocationNameNormalizer locationNameNormalizer, TracerProvider tracerProvider)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<AccommodationPreloader>();
            _options = options.Value;
            _connectorClient = connectorClient;
            _suppliersOptions = supplierOptions.Value;
            _locationNameNormalizer = locationNameNormalizer;
            _tracerProvider = tracerProvider;
        }


        public async Task Preload(List<Suppliers> suppliers, CancellationToken cancellationToken = default)
        {
            var currentSpan = Tracer.CurrentSpan;
            var tracer = _tracerProvider.GetTracer(nameof(AccommodationPreloader));
            
            _context.Database.SetCommandTimeout(_options.DbCommandTimeOut);

            foreach (var supplier in suppliers)
            {
                try
                {
                    var updateDate = DateTime.UtcNow;
                    var lastUpdateDate = await GetLastUpdateDate(supplier);

                    using var supplierAccommodationsPreloadingSpan = tracer.StartActiveSpan(
                        $"{nameof(Preload)} accommodations of {supplier.ToString()}", SpanKind.Internal, currentSpan);

                    cancellationToken.ThrowIfCancellationRequested();
                    await Preload(supplier, lastUpdateDate, cancellationToken);

                    await AddUpdateDateToHistory(supplier, updateDate);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogPreloadingAccommodationsCancel(
                        $"Preloading accommodations of {supplier.ToString()} was canceled by client request.");
                }
                catch (Exception ex)
                {
                    _logger.LogPreloadingAccommodationsError(ex);
                }
            }


            Task<DateTime> GetLastUpdateDate(Suppliers supplier)
                => _context.DataUpdateHistories
                    .Where(dh => dh.Supplier == supplier && dh.Type == DataUpdateTypes.Preloading)
                    .OrderByDescending(dh => dh.UpdateTime)
                    .Select(dh => dh.UpdateTime)
                    .FirstOrDefaultAsync(cancellationToken);


            Task AddUpdateDateToHistory(Suppliers supplier, DateTime date)
            {
                _context.DataUpdateHistories.Add(new DataUpdateHistory
                {
                    Supplier = supplier,
                    Type = DataUpdateTypes.Preloading,
                    UpdateTime = date
                });
                
                return _context.SaveChangesAsync(cancellationToken);
            }
        }


        private async Task Preload(Suppliers supplier, DateTime modificationDate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogPreloadingAccommodationsStart($"Started Preloading accommodations of {supplier.ToString()}.");

            var skip = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batch = await GetAccommodations(supplier, modificationDate, skip,
                    _options.PreloadingBatchSize);
                if (!batch.Any())
                    break;

                var ids = batch.Select(a => a.SupplierCode);
                var existedIds = await _context.RawAccommodations
                    .Where(a => a.Supplier == supplier && ids.Contains(a.SupplierAccommodationId))
                    .Select(a => new {a.Id, a.Supplier, SupplierId = a.SupplierAccommodationId})
                    .ToDictionaryAsync(a => (a.SupplierId, a.Supplier), a => a.Id, cancellationToken);

                var newAccommodations = new ConcurrentBag<RawAccommodation>();
                var existedAccommodations = new ConcurrentBag<RawAccommodation>();
                var utcDate = DateTime.UtcNow;
                Parallel.ForEach(batch, new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount},
                    accommodation =>
                    {
                        // System.Text.Json don't support serialization of Double.Nan value, that is why here we use Newtonsoft serialization
                        // TODO Change to System.Text.Json serialization
                        var str = JsonConvert.SerializeObject(accommodation);
                        var json = JsonDocument.Parse(str);

                        var defaultCountryName =
                            accommodation.Location.Country.GetValueOrDefault(Constants.DefaultLanguageCode);
                        var normalizedCountryCode =
                            _locationNameNormalizer.GetNormalizedCountryCode(defaultCountryName,
                                accommodation.Location.CountryCode);
                        var entity = new RawAccommodation
                        {
                            Id = 0,
                            CountryCode = normalizedCountryCode,
                            CountryNames = accommodation.Location.Country,
                            SupplierLocalityCode = accommodation.Location.SupplierLocalityCode,
                            LocalityNames = accommodation.Location.Locality,
                            SupplierLocalityZoneCode = accommodation.Location.SupplierLocalityZoneCode,
                            LocalityZoneNames = accommodation.Location.LocalityZone,
                            Accommodation = json,
                            Supplier = supplier,
                            SupplierAccommodationId = accommodation.SupplierCode,
                            Modified = utcDate
                        };

                        if (existedIds.TryGetValue((accommodation.SupplierCode, supplier), out var existedId))
                        {
                            entity.Id = existedId;
                            existedAccommodations.Add(entity);

                            return;
                        }

                        entity.Created = utcDate;
                        newAccommodations.Add(entity);
                    });

                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                // Because performance
                _context.RawAccommodations.AddRange(newAccommodations);
                _context.RawAccommodations.UpdateRange(existedAccommodations);
                await _context.SaveChangesAsync(cancellationToken);

                _context.ChangeTracker.Entries()
                    .Where(e => e.Entity != null)
                    .Where(e => e.State != EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = EntityState.Detached);

                skip += _options.PreloadingBatchSize;
            } while (true);

            _logger.LogPreloadingAccommodationsFinish($"Finished Preloading accommodations of {supplier.ToString()} .");


            async Task<List<MultilingualAccommodation>> GetAccommodations(Suppliers supplier, DateTime modDate,
                int skip, int take)
            {
                var url = _suppliersOptions.SuppliersUrls[supplier] +
                    $"{AccommodationUrl}?skip={skip}&top={take}&modification-date={modDate}";
                var (_, isFailure, response, error) = await _connectorClient.Get<List<MultilingualAccommodation>>(
                    new Uri(url), cancellationToken: cancellationToken);

                if (isFailure)
                {
                    throw new Exception($"Problem occured on loading accommodations from supplier '{supplier}'. Error message is {error.Detail}");
                }

                return response;
            }
        }


        private const string AccommodationUrl = "accommodations";
        private readonly ILocationNameNormalizer _locationNameNormalizer;
        private readonly IConnectorClient _connectorClient;
        private readonly SuppliersOptions _suppliersOptions;
        private readonly NakijinContext _context;
        private readonly ILogger<AccommodationPreloader> _logger;
        private readonly StaticDataLoadingOptions _options;
        private readonly TracerProvider _tracerProvider;
    }
}