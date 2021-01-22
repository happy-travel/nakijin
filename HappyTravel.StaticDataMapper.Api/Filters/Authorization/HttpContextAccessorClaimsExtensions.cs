using System.Linq;
using Microsoft.AspNetCore.Http;

namespace HappyTravel.StaticDataMapper.Api.Filters.Authorization
{
    public static class HttpContextAccessorClaimsExtensions
    {
        public static string? GetClientId(this IHttpContextAccessor contextAccessor)
            => contextAccessor.HttpContext?.User
                .Claims
                .SingleOrDefault(c => c.Type == "client_id")?.Value;
    }
}