using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.MapperContracts.Internal.Mappings.Enums;
using HappyTravel.Nakijin.Api.Converters.StaticDataPublication;
using HappyTravel.Nakijin.Api.Models.StaticDataPublications;
using HappyTravel.Nakijin.Api.Services.StaticDataPublication;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Nakijin.Api.Services
{
    public class LocationManagementService : ILocationManagementService
    {
        public LocationManagementService(NakijinContext context, AccommodationChangePublisher accommodationChangePublisher, LocationChangePublisher locationChangePublisher)
        {
            _context = context;
            _accommodationChangePublisher = accommodationChangePublisher;
            _locationChangePublisher = locationChangePublisher;
        }
        
        
        public async Task<Result> Deactivate(string localityHtId, CancellationToken cancellationToken = default)
        {
            var (_, isGettingLocalityToDeactivateFailure, localityToDeactivate, gettingLocalityToRemoveError ) = await GetLocality(localityHtId, cancellationToken);
            if (isGettingLocalityToDeactivateFailure)
                return Result.Failure<(Locality removableLocality, Locality subtitutionalLocality)>(gettingLocalityToRemoveError);

            var zonesToDeactivate = await GetZones(localityToDeactivate, cancellationToken);
            
            var dependentAccommodations = await GetAccommodations(localityToDeactivate, cancellationToken);
            RemoveRelations(dependentAccommodations);
            
            DeactivateLocality(localityToDeactivate);
            DeactivateZones(zonesToDeactivate);
            
            var updateAndPublishTask = Task.WhenAll(_context.SaveChangesAsync(cancellationToken), 
                PublishLocations(new(){localityToDeactivate}, zonesToDeactivate, dependentAccommodations));
            await updateAndPublishTask;
            
            return Result.SuccessIf(updateAndPublishTask.IsCompletedSuccessfully, updateAndPublishTask.Exception != null 
                ? string.Join(Environment.NewLine, updateAndPublishTask.Exception.InnerExceptions.SelectMany(e => e.ToString().ToList()))
                : "An error occurred during the data saving process");
        }
        
        
        private async Task<Result<Locality>> GetLocality(string htId, CancellationToken cancellationToken = default)
        {
            var (_, isGettingIdFailure, id, gettingIdError) = GetId(htId, MapperLocationTypes.Locality);
            if (isGettingIdFailure)
                return Result.Failure<Locality>(gettingIdError);
            
            var (_, isGettingLocalityFailure, locality, gettingLocalityError) = await GetLocality(id, cancellationToken);
            if (isGettingLocalityFailure)
                return Result.Failure<Locality>(gettingLocalityError);

            return locality!;
        }
        
 
        private Result<int> GetId(string htId, MapperLocationTypes validType)
        {
            var (_, isFailure, (idType, id), error) = HtId.Parse(htId);
            if (isFailure)
                return Result.Failure<int>(error);

            if (idType != validType)
                return Result.Failure<int>($"HtId has the wrong type '{idType}'. The valid type is '{validType}'");

            return id;
        }
        

        private async Task<Result<Locality?>> GetLocality(int id, CancellationToken cancellationToken = default)
        {
            var locality = await _context.Localities.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
            
            return locality ?? Result.Failure<Locality?>($"Locality with id '{id}' doesn't exist");
        }

        
        private Task<List<RichAccommodationDetails>> GetAccommodations(Locality locality, CancellationToken cancellationToken = default)
            => _context.Accommodations.Where(l => l.LocalityId == locality.Id).ToListAsync(cancellationToken);

        
        private Task<List<LocalityZone>> GetZones(Locality locality, CancellationToken cancellationToken = default)
            => _context.LocalityZones.Where(z => z.LocalityId == locality.Id).ToListAsync(cancellationToken);

        
        private Task<LocalityZone> GetZone(int zoneId, CancellationToken cancellationToken)
            => _context.LocalityZones.SingleOrDefaultAsync(z => z.Id == zoneId, cancellationToken);

        
        private void RemoveRelations(List<RichAccommodationDetails> accommodations)
        { 
            var modified = DateTime.UtcNow;
            foreach (var accommodation in accommodations)
            {
                accommodation.LocalityId = null;
                accommodation.Modified = modified;
                accommodation.LocalityZoneId = null;
                accommodation.IsCalculated = false;
            }
            _context.UpdateRange(accommodations);
        }
        
        
        private void DeactivateLocality(Locality locality)
        {
            locality.Modified = DateTime.UtcNow;
            locality.IsActive = false;
            _context.Update(locality);
        }
        
        
        private void DeactivateZones(List<LocalityZone> localityZones)
        {
            foreach (var localityZone in localityZones)
            {
                localityZone.Modified = DateTime.UtcNow;
                localityZone.IsActive = false;
            }
            _context.UpdateRange(localityZones);
        }
        
        
        private async Task<Result> PublishLocations(List<Locality> localities, List<LocalityZone> zones, List<RichAccommodationDetails> accommodations)
        {
            //  TODO Publishing of zones was removed based on the code review comment. It can be found in cb7695123e81c5bf268b3 commit
            //  https://github.com/happy-travel/nakijin/pull/120#discussion_r655149958
            var localitiesIds = localities.Select(a => a.Id).ToList();
            var accommodationIds = accommodations.Select(a => a.Id).ToList();
            
            var publishLocalitiesTask =_locationChangePublisher.PublishRemovedLocalities(localitiesIds).AsTask();
            
            // TODO: When will be updater, better to not publish accommodations here, as now we publish changes on data calculation
            var publicAccommodationsTask = _accommodationChangePublisher.PublishUpdated(accommodations.Select(AccommodationDataConverter.Convert).ToList());
            
            await Task.WhenAll(publishLocalitiesTask, publicAccommodationsTask);
            
            if (!publishLocalitiesTask.IsCompletedSuccessfully)
                return Result.Failure($"Failed to publish locations removing. Ids '{string.Join($", ", localitiesIds)}'");
            
            if (!publicAccommodationsTask.IsCompletedSuccessfully)
                return Result.Failure($"Failed to publish accommodations updating. Ids '{string.Join($", ", accommodationIds)}'");
            
            return Result.Success();
        }

        
        
        private readonly LocationChangePublisher _locationChangePublisher;
        private readonly AccommodationChangePublisher _accommodationChangePublisher;
        private readonly NakijinContext _context;
    }
}