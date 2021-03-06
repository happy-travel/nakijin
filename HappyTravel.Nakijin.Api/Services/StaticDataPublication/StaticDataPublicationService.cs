﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace HappyTravel.Nakijin.Api.Services.StaticDataPublication
{
    public class StaticDataPublicationService : IStaticDataPublicationService
    {
        public StaticDataPublicationService(IRedisCacheClient redisCacheClient, ILogger<StaticDataPublicationService> logger,
            IOptions<StaticDataPublicationOptions> updateOptions)
        {
            _streamName = updateOptions.Value.StreamName;
            _logger = logger;
            _database = redisCacheClient.GetDbFromConfiguration().Database;
            if (!InitStreamIfNeeded())
                ClearStream();
        }


        public async Task Publish(Location location, UpdateEventTypes type)
        {
            await _database.StreamAddAsync(_streamName, new[] {Build(location, type)});
            _logger.LogSingleLocationPublished(location.HtId);
        }


        public async Task Publish(List<Location> locations, UpdateEventTypes type, CancellationToken cancellationToken = default)
        {
            const int batchSize = 1000;
            using (await _mutex.LockAsync(cancellationToken))
            {
                foreach (var batchOfLocations in Split(locations, batchSize))
                {
                    await _database.StreamAddAsync(_streamName, Build(batchOfLocations, type));
                    _logger.LogLocationsPublished(batchOfLocations.Count);
                }
            }
        }


        private bool InitStreamIfNeeded()
        {
            try
            {
                // Throws an exceptions if the stream doesn't exist
                _database.StreamInfo(_streamName);

                return false;
            }
            catch
            {
                // A value must be added to init the stream
                var initId = _database.StreamAdd(_streamName, new[] {new NameValueEntry("init", "init")});
                _database.StreamDelete(_streamName, new[] {initId});

                return true;
            }
        }


        private void ClearStream()
        {
            var streamInfo = _database.StreamInfo(_streamName);
            if (!streamInfo.FirstEntry.IsNull && !streamInfo.LastEntry.IsNull)
            {
                var messagesToClear = _database.StreamRange(_streamName, streamInfo.FirstEntry.Id, streamInfo.LastEntry.Id);
                _database.StreamDelete(_streamName, messagesToClear.Select(m => m.Id).ToArray());
            }
        }


        private NameValueEntry Build(Location location, UpdateEventTypes type)
        {
            var entry = new LocationPublicationEntry(type, location);
            return new(location.HtId, JsonSerializer.Serialize(entry));
        }


        private NameValueEntry[] Build(List<Location> batchOfLocations, UpdateEventTypes type)
            => batchOfLocations.Select(l => Build(l, type))
                .ToArray();


        private static IEnumerable<List<T>> Split<T>(List<T> items, int batchSize)
        {
            for (var i = 0; i < items.Count; i += batchSize)
                yield return items.GetRange(i, Math.Min(batchSize, items.Count - i));
        }


        private readonly AsyncLock _mutex = new();
        private readonly string _streamName;
        private readonly IDatabase _database;
        private readonly ILogger<StaticDataPublicationService> _logger;
    }
}