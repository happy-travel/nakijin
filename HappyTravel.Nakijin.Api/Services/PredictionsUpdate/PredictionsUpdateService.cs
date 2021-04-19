using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Api.Models.PredictionsUpdate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using HappyTravel.Nakijin.Api.Infrastructure.Helpers;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using Nito.AsyncEx;

namespace HappyTravel.Nakijin.Api.Services.PredictionsUpdate
{
    public class PredictionsUpdateService: IPredictionsUpdateService
    {
        public PredictionsUpdateService(IRedisCacheClient redisCacheClient, ILogger<PredictionsUpdateService> logger,IOptions<PredictionUpdateOptions> updateOptions)
        {
            _stream = updateOptions.Value.Stream;
            _logger = logger;
            _database = redisCacheClient.GetDbFromConfiguration().Database;
            if (!InitStream())
                ClearStream();
        }
        
        
        public async Task Publish(Location location, EventTypes type)
        {
            await _database.StreamAddAsync(_stream, new []{Build(location, type)});
            _logger.LogLocationsPublished( $"Location '{location.HtId}' has been published");
        }

        
        public async Task Publish(List<Location> locations, EventTypes type, CancellationToken cancellationToken = default)
        {
            const int batchSize = 1000;
            using (await _mutex.LockAsync(cancellationToken))
            {
                foreach (var batchOfLocations in ListHelper.Split(locations, batchSize))
                {
                    await _database.StreamAddAsync(_stream, Build(batchOfLocations, type));
                    _logger.LogLocationsPublished( $"{batchOfLocations} locations have been published");
                }
            }
        }

        
        private bool InitStream()
        {
            try
            {
                // Throws an exceptions if the stream doesn't exist
                _database.StreamInfo(_stream);

                return false;
            }
            catch
            {
                // A value must be added to init the stream
                var initId = _database.StreamAdd(_stream, new[] {new NameValueEntry("init", "init")});
                _database.StreamDelete(_stream, new[] {initId});

                return true;
            }
        }


        private void ClearStream()
        {
            var streamInfo = _database.StreamInfo(_stream);
            if (!streamInfo.FirstEntry.IsNull && !streamInfo.LastEntry.IsNull)
            {
                var messagesToClear = _database.StreamRange(_stream, streamInfo.FirstEntry.Id, streamInfo.LastEntry.Id);
                _database.StreamDelete(_stream, messagesToClear.Select(m => m.Id).ToArray());
            }
        }

        
        NameValueEntry Build(Location location, EventTypes type)
        {
            var entry = new LocationEntry(type, location);
            return new(location.HtId, JsonSerializer.Serialize(entry));
        }
        
        
        NameValueEntry[] Build(List<Location> batchOfLocations, EventTypes type) 
            => batchOfLocations.Select(l => Build(l, type))
                .ToArray();


        private readonly AsyncLock _mutex = new ();
        private readonly string _stream;
        private readonly IDatabase _database;
        private readonly ILogger<PredictionsUpdateService> _logger;
    }
}