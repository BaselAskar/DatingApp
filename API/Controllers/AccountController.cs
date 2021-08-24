using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenServices _tokenServices;
        private readonly IMapper _mapper;
        public AccountController(UserManager<AppUser> userManager,SignInManager<AppUser> signInManager,ITokenServices tokenServices,IMapper mapper)
        {
            _tokenServices = tokenServices;
            _mapper = mapper;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (await IsExistedUser(registerDto.UserName)) return BadRequest("This user is recintly Existed");


            var user = _mapper.Map<AppUser>(registerDto);

            user.UserName = registerDto.UserName.ToLower();

            var result = await _userManager.CreateAsync(user,registerDto.Password);

            if(!result.Succeeded) return BadRequest(result.Errors);

            var resultOfRoles = await _userManager.AddToRoleAsync(user,"Member");

            return new UserDto
            {
                UserName = user.UserName,
                Token = await _tokenServices.CreateToken(user),
                KnownAs = user.KnownAs,
            };

        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userManager.Users
                .Include(u => u.Photos)
                .SingleOrDefaultAsync(u => u.UserName == loginDto.UserName.ToLower());

            if (user == null) return Unauthorized("This user is not existed");

            var result = await _signInManager.CheckPasswordSignInAsync(user,loginDto.Password,false);

            if (!result.Succeeded) return Unauthorized("The Password is not correct!");

            return new UserDto
            {
                UserName = user.UserName,
                Token = await _tokenServices.CreateToken(user),
                // KnownAs = user.KnownAs,
                // PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain).Url
            };
        }

        private async Task<bool> IsExistedUser(string userName)
        {
            return await _userManager.Users.AnyAsync(u => u.UserName == userName.ToLower());
        }
    }
}