using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers.LocationMapping
{
    public class LocationMapper : ILocationMapper
    {
        public LocationMapper(NakijinContext context, ICountryMapper countryMapper, ILocalityMapper localityMapper,
            ILocalityZoneMapper localityZoneMapper,
            IOptions<StaticDataLoadingOptions> options,
            ILoggerFactory loggerFactory, TracerProvider tracerProvider)
        {
            _context = context;
            _dbCommandTimeOut = options.Value.DbCommandTimeOut;
            _logger = loggerFactory.CreateLogger<LocationMapper>();
            _tracerProvider = tracerProvider;
            _countryMapper = countryMapper;
            _localityMapper = localityMapper;
            _localityZoneMapper = localityZoneMapper;
        }


        public async Task MapLocations(List<Suppliers> suppliers, CancellationToken cancellationToken = default)
        {
            var currentSpan = Tracer.CurrentSpan;
            var tracer = _tracerProvider.GetTracer(nameof(LocationMapper));

            _context.Database.SetCommandTimeout(_dbCommandTimeOut);

            foreach (var supplier in suppliers)
            {
                try
                {
                    using var supplierLocationsMappingSpan = tracer.StartActiveSpan(
                        $"{nameof(MapLocations)} of {supplier.ToString()}",
                        SpanKind.Internal, currentSpan);

                    _logger.LogMappingLocationsStart(
                        $"Started Mapping locations of {supplier.ToString()}.");

                    cancellationToken.ThrowIfCancellationRequested();
                    await _countryMapper.Map(supplier, tracer, supplierLocationsMappingSpan, cancellationToken);
                    await _localityMapper.Map(supplier, tracer, supplierLocationsMappingSpan, cancellationToken);
                    await _localityZoneMapper.Map(supplier, tracer, supplierLocationsMappingSpan, cancellationToken);

                    _logger.LogMappingLocationsFinish(
                        $"Finished Mapping locations of {supplier.ToString()}.");
                }
                catch (TaskCanceledException)
                {
                    _logger.LogMappingLocationsCancel(
                        $"Mapping locations of {supplier.ToString()} was canceled by client request.");
                }
                catch (Exception ex)
                {
                    _logger.LogMappingLocationsError(ex);
                }
            }
        }


        private readonly NakijinContext _context;
        private readonly ICountryMapper _countryMapper;
        private readonly ILocalityMapper _localityMapper;
        private readonly ILocalityZoneMapper _localityZoneMapper;
        private readonly ILogger<LocationMapper> _logger;
        private readonly int _dbCommandTimeOut;
        private readonly TracerProvider _tracerProvider;
    }
}