﻿using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace HappyTravel.Nakijin.Api.Services
{
    public interface ILocationManagementService
    {
        Task<Result> RemoveLocality(string removableHtId, string substitutionalHtId, string? substitutionalZoneHtId = null, CancellationToken cancellationToken = default);
    }
}