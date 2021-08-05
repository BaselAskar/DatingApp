using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
        public class UserRepository : IUserRepository
        {
            private readonly DataContext _data;
            private readonly IMapper _mapper;
            public UserRepository(DataContext data,IMapper mapper)
            {
                _data = data;
                _mapper = mapper;
            }
                public async Task<MemberDto> GetMemberAsync(string userName)
                {
                    return await _data.Users
                            .Where(u => u.UserName == userName)
                            .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
                            .SingleOrDefaultAsync();
                }

                public async Task<PageList<MemberDto>> GetMembersAsync(UserParams userParams)
                {
                    var query = _data.Users
                        .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
                        .AsNoTracking();

                    return await PageList<MemberDto>.CreateAsync(query,userParams.PageNumber,userParams.PageSize);
                                    
                }

                public async Task<AppUser> GetUserByUserNameAsync(string userName)
                {
                    return await _data.Users
                                    .Where(u => u.UserName == userName)
                                    .SingleOrDefaultAsync();
                }

                public async Task<bool> SaveAllAsync()
                {
                    return await _data.SaveChangesAsync() > 0;
                }

                public void UpdateUser(AppUser user)
                {
                    _data.Entry(user).State = EntityState.Modified; 
                }
        }
}