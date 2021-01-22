using Microsoft.AspNetCore.Authorization;

namespace HappyTravel.StaticDataMapper.Api.Filters.Authorization
{
    public class PermissionsAttribute : AuthorizeAttribute
    {
        public PermissionsAttribute(MapperPermissions permissions)
        {
            Policy = $"{PolicyPrefix}{permissions}";
        }

        public const string PolicyPrefix = "MapperPermissions_";
    }
}