using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Nakijin.Data.Models;
using OpenTelemetry.Trace;

namespace HappyTravel.Nakijin.Api.Services.Workers.LocationMapping
{
    public interface ICountryMapper
    {
        Task Map(Suppliers supplier, Tracer tracer, TelemetrySpan parentSpan, CancellationToken cancellationToken);
    }
}