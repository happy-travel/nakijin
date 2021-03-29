using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Nakijin.Api.Models.LocationInfo;
using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;
using Location = HappyTravel.Nakijin.Api.Models.LocationServiceInfo.Location;

namespace HappyTravel.Nakijin.Api.Services.LocationMappingInfo
{
    public interface ILocationMappingInfoService
    {
        Task<Result<List<LocationMapping>>> Get(List<string> htIds, string languageCode);
        
        Task<List<Location>> Get(AccommodationMapperLocationTypes locationType, string languageCode, DateTime modified,
            int skip, int top, CancellationToken cancellationToken = default);
    }
}