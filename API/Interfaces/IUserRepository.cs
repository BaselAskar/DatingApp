using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<MemberDto>> GetMembersAsync();
        Task<MemberDto> GetMemberAsync(string userName);
        Task<AppUser> GetUserByUserNameAsync(string userName);
        Task<bool> SaveAllAsync();
        void UpdateUser (AppUser user);
        
    }
}