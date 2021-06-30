using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace HappyTravel.Nakijin.Api.Services
{
    public interface ILocationManagementService
    {
        Task<Result> Deactivate(string localityHtId, CancellationToken cancellationToken = default);
    }
}