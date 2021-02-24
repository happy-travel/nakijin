﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Data.Models.Mappers;
using HappyTravel.SecurityClient;
using HappyTravel.StaticDataMapper.Api.Infrastructure;
using HappyTravel.StaticDataMapper.Api.Models;
using LocationNameNormalizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.StaticDataMapper.Api.Services.Workers
{
    public class AccommodationPreloader : IAccommodationPreloader
    {
        public AccommodationPreloader(NakijinContext context,
            IConnectorClient connectorClient, ILoggerFactory loggerFactory,
            IOptions<StaticDataLoadingOptions> options, IOptions<SuppliersOptions> supplierOptions,
            ILocationNameNormalizer locationNameNormalizer)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<AccommodationPreloader>();
            _options = options.Value;
            _connectorClient = connectorClient;
            _suppliersOptions = supplierOptions.Value;
            _locationNameNormalizer = locationNameNormalizer;
        }


        public async Task Preload(List<Suppliers> suppliers, DateTime? modificationDate = null,
            CancellationToken cancellationToken = default)
        {
            modificationDate ??= DateTime.MinValue;
            _context.Database.SetCommandTimeout(_options.DbCommandTimeOut);

            foreach (var supplier in suppliers)
            {
                var skip = 0;
                do
                {
                    var batch = await GetAccommodations(supplier, modificationDate.Value, skip, _options.BatchSize);
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

                    skip += _options.BatchSize;
                } while (true);
            }


            async Task<List<MultilingualAccommodation>> GetAccommodations(Suppliers supplier, DateTime modDate,
                int skip, int take)
            {
                var url = _suppliersOptions.SuppliersUrls[supplier] +
                    $"{AccommodationUrl}?skip={skip}&top={take}&modification-date={modDate}";
                var (_, isFailure, response, error) = await _connectorClient.Get<List<MultilingualAccommodation>>(
                    new Uri(url), cancellationToken: cancellationToken);

                if (isFailure)
                {
                    _logger.Log(LogLevel.Error, error.Detail);
                    return new List<MultilingualAccommodation>(0);
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
    }
}