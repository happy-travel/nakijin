using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    internal static class ProblemDetailsBuilder
    {
        internal static ProblemDetails Build(string details, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            => new ProblemDetails
            {
                Detail = details,
                Status = (int)statusCode
            };


        internal static Result<T, ProblemDetails> Fail<T>(string details, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            => Result.Failure<T, ProblemDetails>(Build(details, statusCode));
    }
}
