using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{   
    [ServiceFilter(typeof(LogUserActivity))]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LikesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public LikesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost("{userName}")]
        public async Task<IActionResult> AddLike(string userName)
        {
            var sourceUserId = User.GetUserId();
            var likedUser = await _unitOfWork.UserRepository.GetUserByUserNameAsync(userName);
            var sourceUser = await _unitOfWork.LikesRepository.GetUserWithLikes(sourceUserId);

            if (sourceUser.UserName == userName) return BadRequest("You can't like your self");

            if (likedUser == null) return NotFound("This user is not existed");

            var userLike = await _unitOfWork.LikesRepository.GetUserLike(sourceUserId,likedUser.Id);

            if  (userLike != null) return BadRequest("You are already like this user");

            userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };

            sourceUser.LikedUser.Add(userLike);

            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Faild to like user");
        }

        [HttpGet]
        public async Task<ActionResult<PageList<LikeDto>>> GetUserLikes([FromQuery]string predicate,LikeParams likeParams)
        {
            var userId = User.GetUserId();
            
            return Ok(await _unitOfWork.LikesRepository.GetUserLikes(likeParams));
        }
    }
}