using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    /// <summary>
    ///     Prevents authorization policies execution for not authenticated clients
    /// </summary>
    public class ForbidUnauthenticatedPolicyEvaluator : IPolicyEvaluator
    {
        public ForbidUnauthenticatedPolicyEvaluator(PolicyEvaluator defaultEvaluator)
        {
            _defaultEvaluator = defaultEvaluator;
        }


        public Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
            => _defaultEvaluator.AuthenticateAsync(policy, context);


        public Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context,
            object? resource)
        {
            if (!authenticationResult.Succeeded)
                return Task.FromResult(PolicyAuthorizationResult.Challenge());

            return _defaultEvaluator.AuthorizeAsync(policy, authenticationResult, context, resource);
        }


        private readonly PolicyEvaluator _defaultEvaluator;
    }
}