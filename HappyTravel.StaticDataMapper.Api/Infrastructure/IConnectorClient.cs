using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public interface IConnectorClient
    {
        Task<Result<TResponse, ProblemDetails>> Get<TResponse>(Uri endpoint,
            CancellationToken cancellationToken = default);
    }
}