using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace HappyTravel.StaticDataMapper.Api.Filters.Authorization
{
    public class PermissionsAuthorizationHandler : AuthorizationHandler<PermissionsAuthorizationRequirement>
    {
        public PermissionsAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionsAuthorizationRequirement requirement)
        {
            var clientName = _httpContextAccessor.GetClientId();
            if (clientName is not null)
            {
                var clientPermissions = GetClientPermissions(clientName);
                if (clientPermissions is not null && clientPermissions.Value.HasFlag(requirement.Permissions))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            context.Fail();
            return Task.CompletedTask;
        }

        
        private static MapperPermissions? GetClientPermissions(string clientName)
        {
            return clientName switch
            {
                "mapper_api_client" => MapperPermissions.Read,
                "mapper_manager_client" => MapperPermissions.Edit | MapperPermissions.Read,
                _ => null
            };
        }

        private readonly IHttpContextAccessor _httpContextAccessor;
    }
}