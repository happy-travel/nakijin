using System.Threading.Tasks;

namespace HappyTravel.SecurityClient
{
    public interface ISecurityTokenManager
    {
        Task<string> Get();
        Task Refresh();
    }
}