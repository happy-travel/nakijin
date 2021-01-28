using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.StaticDataMapper.Api.Models.LocationInfo;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;
using Location = HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo.Location;

namespace HappyTravel.StaticDataMapper.Api.Services.LocationMappingInfo
{
    public interface ILocationMappingInfoService
    {
        Task<Result<LocationMapping>> Get(string htId, string languageCode);
        
        Task<List<Location>> Get(AccommodationMapperLocationTypes locationType, string languageCode, DateTime modified,
            int skip, int top, CancellationToken cancellationToken = default);
    }
}