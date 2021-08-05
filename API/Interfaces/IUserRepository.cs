using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface IUserRepository
    {
        Task<PageList<MemberDto>> GetMembersAsync(UserParams userParams);
        Task<MemberDto> GetMemberAsync(string userName);
        Task<AppUser> GetUserByUserNameAsync(string userName);
        Task<bool> SaveAllAsync();
        void UpdateUser (AppUser user);
        
    }
}