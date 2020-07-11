using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.PropertyManagement.Data;
using HappyTravel.PropertyManagement.Data.Models.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.PropertyManagement.Api.Services.Mappers
{
    public class AccommodationPreloader : IAccommodationPreloader
    {
        public AccommodationPreloader(NakijinContext context, IHttpClientFactory clientFactory, ILoggerFactory loggerFactory, IOptions<AccommodationsPreloaderOptions> options)
        {
            _clientFactory = clientFactory;
            _context = context;
            _logger = loggerFactory.CreateLogger<AccommodationPreloader>();
            _options = options.Value;
        }


        public async Task Preload(DateTime? modificationDate = null, CancellationToken cancellationToken = default)
        {
            modificationDate ??= DateTime.MinValue;
            var date = modificationDate.Value;

            foreach (var supplier in _options.Suppliers)
            {
                var client = GetClient(supplier);

                var skip = 0;
                do
                {
                    var batch = await GetAccommodations(client, date, skip, _options.BatchSize);
                    if (!batch.Any())
                        break;

                    var ids = batch.Select(a => a.Id);
                    var existedIds = await _context.RawAccommodations
                        .Where(a => a.Supplier == supplier && ids.Contains(a.Supplier))
                        .Select(a => new {a.Id, a.Supplier, a.SupplierId})
                        .ToDictionaryAsync(a => (a.SupplierId, a.Supplier), a => a.Id, cancellationToken);

                    var newAccommodations = new ConcurrentBag<RawAccommodation>();
                    var existedAccommodations = new ConcurrentBag<RawAccommodation>();
                    Parallel.ForEach(batch, new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount}, accommodation =>
                    {
                        var bytes = JsonSerializer.SerializeToUtf8Bytes(accommodation);
                        var json = JsonDocument.Parse(bytes);

                        var entity = new RawAccommodation
                        {
                            Id = 0,
                            Accommodation = json,
                            Supplier = supplier,
                            SupplierId = accommodation.Id
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


            async Task<List<AccommodationDetails>> GetAccommodations(HttpClient client, DateTime modDate, int skip, int take)
            {
                var result = new List<AccommodationDetails>(take);

                try
                {
                    var url = $"{AccommodationUrl}?skip={skip}&top={take}&modification-date={modDate}";
                    var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    if (response.Content == null)
                        return result;

                    var stream = await response.Content.ReadAsStreamAsync();
                    result = await JsonSerializer.DeserializeAsync<List<AccommodationDetails>>(stream, cancellationToken: cancellationToken) ?? result;
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex, ex.Message);
                }

                return result;
            }


            HttpClient GetClient(string name)
            {
                var client = _clientFactory.CreateClient(name);
                
                client.DefaultRequestHeaders.Add("Accept-Language", "en");
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                return client;
            }
        }


        private const string AccommodationUrl = "";

        private readonly IHttpClientFactory _clientFactory;
        private readonly NakijinContext _context;
        private readonly ILogger<AccommodationPreloader> _logger;
        private readonly AccommodationsPreloaderOptions _options;
    }
}
