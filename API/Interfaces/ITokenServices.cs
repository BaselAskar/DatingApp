using System.Threading.Tasks;
using API.Entities;

namespace API.Interfaces
{
        public interface ITokenServices
    {
        Task<string> CreateToken(AppUser user);
    }
}