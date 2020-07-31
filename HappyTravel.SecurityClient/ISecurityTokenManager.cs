using System.Threading.Tasks;

namespace HappyTravel.SecurityTokenManager
{
    public interface ISecurityTokenManager
    {
        Task<string> Get();
        Task Refresh();
    }
}