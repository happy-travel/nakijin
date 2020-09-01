using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.PropertyManagement.Data;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Mappers;
using HappyTravel.SecurityClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.PropertyManagement.Api.Services.Mappers
{
    public class AccommodationPreloader : IAccommodationPreloader
    {
        public AccommodationPreloader(NakijinContext context,
            IConnectorClient connectorClient, ILoggerFactory loggerFactory,
            IOptions<AccommodationsPreloaderOptions> options, IOptions<SuppliersOptions> supplierOptions)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<AccommodationPreloader>();
            _options = options.Value;
            _connectorClient = connectorClient;
            _suppliersOptions = supplierOptions.Value;
        }


        public async Task Preload(DateTime? modificationDate = null, CancellationToken cancellationToken = default)
        {
            modificationDate ??= DateTime.MinValue;

            foreach (var supplier in _options.Suppliers)
            {
                var skip = 0;
                do
                {
                    var batch = await GetAccommodations(supplier, modificationDate.Value, skip, _options.BatchSize);
                    if (!batch.Any())
                        break;

                    var ids = batch.Select(a => a.Id);
                    var existedIds = await _context.RawAccommodations
                        .Where(a => a.Supplier == supplier && ids.Contains(a.SupplierAccommodationId))
                        .Select(a => new {a.Id, a.Supplier, SupplierId = a.SupplierAccommodationId})
                        .ToDictionaryAsync(a => (a.SupplierId, a.Supplier), a => a.Id, cancellationToken);

                    var newAccommodations = new ConcurrentBag<RawAccommodation>();
                    var existedAccommodations = new ConcurrentBag<RawAccommodation>();
                    Parallel.ForEach(batch, new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount},
                        accommodation =>
                        {
                            // System.Text.Json don't support serialization of Double.Nan value, that is why here we use Newtonsoft serialization
                            // TODO Change to System.Text.Json serialization
                            var str = JsonConvert.SerializeObject(accommodation);
                            var json = JsonDocument.Parse(str);

                            var entity = new RawAccommodation
                            {
                                Id = 0,
                                CountryCode = accommodation.Location.CountryCode,
                                Accommodation = json,
                                Supplier = supplier,
                                SupplierAccommodationId = accommodation.Id
                            };

                            if (existedIds.TryGetValue((accommodation.Id, supplier), out var existedId))
                            {
                                entity.Id = existedId;
                                existedAccommodations.Add(entity);
                                return;
                            }

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

                    skip += _options.BatchSize;
                } while (true);
            }


            async Task<List<AccommodationDetails>> GetAccommodations(Suppliers supplier, DateTime modDate, int skip,
                int take)
            {
                var url = _suppliersOptions.SuppliersUrls[supplier] +
                    $"{AccommodationUrl}?skip={skip}&top={take}&modification-date={modDate}";
                var (_, isFailure, response, error) = await _connectorClient.Get<List<AccommodationDetails>>(
                    new Uri(url), cancellationToken: cancellationToken);

                if (isFailure)
                {
                    _logger.Log(LogLevel.Error, error.Detail);
                    return new List<AccommodationDetails>(0);
                }

                return response;
            }
        }


        private const string AccommodationUrl = "accommodations";

        private readonly IConnectorClient _connectorClient;
        private readonly SuppliersOptions _suppliersOptions;
        private readonly NakijinContext _context;
        private readonly ILogger<AccommodationPreloader> _logger;
        private readonly AccommodationsPreloaderOptions _options;
    }
}