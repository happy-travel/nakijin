using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.MapperContracts.Internal.Mappings;
using HappyTravel.MapperContracts.Internal.Mappings.Enums;
using Location = HappyTravel.Nakijin.Api.Models.LocationServiceInfo.Location;

namespace HappyTravel.Nakijin.Api.Services.LocationMappingInfo
{
    public interface ILocationMappingInfoService
    {
        Task<Result<List<LocationMapping>>> Get(List<string> htIds, string languageCode);
        
        Task<List<Location>> Get(MapperLocationTypes locationType, string languageCode, DateTime modified,
            int skip, int top, CancellationToken cancellationToken = default);
    }
}