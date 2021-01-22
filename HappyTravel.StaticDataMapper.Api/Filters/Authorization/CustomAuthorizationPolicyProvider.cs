using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HappyTravel.StaticDataMapper.Api.Filters.Authorization
{
    public class CustomAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        public CustomAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }


        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(PermissionsAttribute.PolicyPrefix) && 
                Enum.TryParse(policyName.Substring(PermissionsAttribute.PolicyPrefix.Length), out MapperPermissions permissions))
            {
                return Task.FromResult(new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionsAuthorizationRequirement(permissions))
                    .Build());
            }
            

            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
        
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() 
            => _fallbackPolicyProvider.GetDefaultPolicyAsync();
        
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() 
            => _fallbackPolicyProvider.GetFallbackPolicyAsync();
        
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
    }
}