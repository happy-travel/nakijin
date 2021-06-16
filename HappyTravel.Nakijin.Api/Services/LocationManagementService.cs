using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.MapperContracts.Internal.Mappings.Enums;
using HappyTravel.Nakijin.Api.Services.StaticDataPublication;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Nakijin.Api.Services
{
    public class LocationManagementService : ILocationManagementService
    {
        public LocationManagementService(NakijinContext context, LocationChangePublisher locationChangePublisher)
        {
            _context = context;
            _locationChangePublisher = locationChangePublisher;
        }
        
        
        // Substitutes the locality htIdToRemove for accommodations with substitutionalHtId.
        // Zone for accommodations will be substituted if substitutionalZoneHtId is specified 
        public async Task<Result> RemoveLocality(string removableHtId, string substitutionalHtId, string? substitutionalZoneHtId = null, CancellationToken cancellationToken = default)
        {
            var (_, isGettingLocalityToRemoveFailure, localityToRemove, gettingLocalityToRemoveError ) = await GetLocality(removableHtId, cancellationToken);
            if (isGettingLocalityToRemoveFailure)
                return Result.Failure(gettingLocalityToRemoveError);

            var (_, isGettingSubstitutionalLocalityFailure, substitutionalLocality, gettingSubstitutionalLocalityError) = await GetLocality(substitutionalHtId, cancellationToken);
            if (isGettingSubstitutionalLocalityFailure)
                return Result.Failure(gettingSubstitutionalLocalityError);

            if (localityToRemove.CountryId != substitutionalLocality.CountryId)
                return Result.Failure("The locality to remove and the substitutional locality must have the same country");
            
            LocalityZone substitutionalZone = null!;
            if (!string.IsNullOrEmpty(substitutionalZoneHtId))
            {
                var gettingZoneIdResult = GetId(substitutionalZoneHtId, MapperLocationTypes.LocalityZone);
                if (gettingZoneIdResult.IsFailure)
                    return Result.Failure(gettingZoneIdResult.Error);

                substitutionalZone = await GetZone(gettingZoneIdResult.Value, cancellationToken);
                if (substitutionalZone == null)
                    return Result.Failure($"Failed to find substitutional zone '{substitutionalZoneHtId}'");
                    
                if (substitutionalLocality.Id != substitutionalZone.LocalityId)
                    return Result.Failure($"Substitutional zone '{substitutionalZoneHtId}' doesn't belong to substitutional locality {substitutionalHtId}");
            }

            var zonesToRemove = await GetZones(localityToRemove!, cancellationToken);
            
            var accommodationsOfLocationToRemove = await GetAccommodations(localityToRemove!, cancellationToken);
            ModifyRelation(accommodationsOfLocationToRemove, substitutionalLocality, substitutionalZone, cancellationToken);
            
            _context.UpdateRange(accommodationsOfLocationToRemove);
            _context.RemoveRange(zonesToRemove);
            var updateAndPublishTask = Task.WhenAll(_context.SaveChangesAsync(cancellationToken), PublishLocations(new(){localityToRemove}, zonesToRemove));
            await updateAndPublishTask;
            
            return Result.SuccessIf(updateAndPublishTask.IsCompletedSuccessfully, updateAndPublishTask.Exception != null 
                ? string.Join(Environment.NewLine, updateAndPublishTask.Exception.InnerExceptions.SelectMany(e => e.ToString().ToList()))
                : "An error occurred during the saving changes");
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
        
        
        private void ModifyRelation(List<RichAccommodationDetails> accommodations, Locality locality, LocalityZone zone = null!, CancellationToken cancellationToken = default)
        { 
            var modified = DateTime.UtcNow;
            foreach (var accommodation in accommodations)
            {
                accommodation.LocalityId = locality.Id;
                accommodation.Modified = modified;
                accommodation.LocalityZoneId = zone?.Id;
                accommodation.IsCalculated = false;
            }
        }

        
        private async Task<Result> PublishLocations(List<Locality?> localities, List<LocalityZone> zones)
        {
            var localitiesIds = localities.Select(a => a.Id).ToList();
            var zoneIds = zones.Select(z => z.Id).ToList();
            var publishTask =_locationChangePublisher.PublishRemovedLocalities(localitiesIds).AsTask();
            await publishTask;
            if (!publishTask.IsCompletedSuccessfully)
                return Result.Failure($"Failed to publish locations removal. Ids '{string.Join($", ", localitiesIds)}'");

            publishTask = _locationChangePublisher.PublishRemovedLocalityZones(zoneIds).AsTask();
            await publishTask;
            if (!publishTask.IsCompletedSuccessfully)
                return Result.Failure($"Failed to publish zones removal. Ids '{string.Join($", ", zoneIds)}'");

            return Result.Success();
        }
        
        
        private readonly LocationChangePublisher _locationChangePublisher;
        private readonly NakijinContext _context;
    }
}