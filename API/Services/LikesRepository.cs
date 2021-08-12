using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DataContext _data;
        public LikesRepository(DataContext data)
        {
            _data = data;
        }
        public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId)
        {
            return await _data.Likes.FindAsync(sourceUserId,likedUserId);
        }
        public async Task<PageList<LikeDto>> GetUserLikes(LikeParams likeParams)
        {
            var users = _data.Users.OrderBy(u => u.UserName).AsQueryable();
            var likes = _data.Likes.AsQueryable();

            if (likeParams.Predicate == "liked")
            {
                likes = likes.Where(like => like.SourceUserId == likeParams.UserId);
                users = likes.Select(like => like.LikedUser);
            }

            if (likeParams.Predicate == "likedBy")
            {
                likes = likes.Where(like => like.LikedUserId == likeParams.UserId);
                users = likes.Select(like => like.SourceUser);
            }

            var likeUsers =  users.Select(user => new LikeDto
            {
                Id = user.Id,
                UserName = user.UserName,
                KnownAs = user.KnownAs,
                Age = user.DateOfBirth.CalculateAge(),
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain).Url,
                City = user.City
            });
            return await PageList<LikeDto>.CreateAsync(likeUsers,likeParams.PageNumber,likeParams.PageSize);
        }
        public async Task<AppUser> GetUserWithLikes(int userId)
        {
            return await _data.Users
                    .Include(x => x.LikedUser)
                    .FirstOrDefaultAsync(x => x.Id == userId);
        }
    }
}