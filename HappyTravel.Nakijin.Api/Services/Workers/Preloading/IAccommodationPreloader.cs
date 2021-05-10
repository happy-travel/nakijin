using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data.Models;

namespace HappyTravel.Nakijin.Api.Services.Workers
{
    public interface IAccommodationPreloader
    {
        Task Preload(List<Suppliers> suppliers, CancellationToken cancellationToken = default);
    }
}