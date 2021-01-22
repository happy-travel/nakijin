using Microsoft.AspNetCore.Authorization;

namespace HappyTravel.StaticDataMapper.Api.Filters.Authorization
{
    public class PermissionsAuthorizationRequirement : IAuthorizationRequirement
    {
        public PermissionsAuthorizationRequirement(MapperPermissions permissions)
        {
            Permissions = permissions;
        }
        
        public MapperPermissions Permissions { get; }
    }
}