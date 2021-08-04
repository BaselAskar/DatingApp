using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoServices _photoServices;

        public UsersController(IUserRepository userRepository,IMapper mapper,IPhotoServices photoServices)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _photoServices = photoServices;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetMembers()
        {
            return Ok(await _userRepository.GetMembersAsync());
        }

        [HttpGet("{userName}")]
        public async Task<ActionResult<MemberDto>> GetMemberByUserName(string userName)
        {
            return await _userRepository.GetMemberAsync(userName);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] MemberUpdateDto memberUpdateDto)
        {
            var user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());

            _mapper.Map(memberUpdateDto,user);

            _userRepository.UpdateUser(user);

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Faild to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());

            var result = await _photoServices.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0) photo.IsMain = true;

            user.Photos.Add(photo);

            if (await _userRepository.SaveAllAsync())
            {
                return _mapper.Map<PhotoDto>(photo);
            }

            return BadRequest("Uploading photo is failed");
        }

        [HttpDelete("delete-photo/{publicId}")]
        public async Task<IActionResult> DeletePhoto(string publicId)
        {
            var user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());

            var photo = user.Photos.FirstOrDefault(p => p.PublicId == publicId);
            if (photo == null) return NotFound();
            if (photo.IsMain) return BadRequest("It is not Allowed to delete a main photo");

            if (photo.PublicId != null)
            {
                 var result = await _photoServices.DeletePhotoAsync(publicId);
            
                if (result.Error != null) return BadRequest(result.Error.Message);

            }

            user.Photos.Remove(photo);

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Faild to delete the photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<IActionResult> SetMainPhoto(int photoId)
        {
            var user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());

            var photo = user.Photos.FirstOrDefault(u => u.Id == photoId);
            if (photo.IsMain) return BadRequest("This photo is already main");

            var currentMain = user.Photos.FirstOrDefault(u => u.IsMain);
            if (currentMain != null) currentMain.IsMain = false;

            photo.IsMain = true;

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Faild to set main photo");
        }
    }
}