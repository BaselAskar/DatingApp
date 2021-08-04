using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly DataContext _data;
        private readonly ITokenServices _tokenServices;
        private readonly IMapper _mapper;
        public AccountController(DataContext data,ITokenServices tokenServices,IMapper mapper)
        {
            _data = data;
            _tokenServices = tokenServices;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (await IsExistedUser(registerDto.UserName)) return BadRequest("This user is recintly Existed");

            var hmac = new HMACSHA512();

            var user = _mapper.Map<AppUser>(registerDto);

            user.UserName = registerDto.UserName.ToLower();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
            user.PasswordSalt = hmac.Key;

            await _data.Users.AddAsync(user);
            await _data.SaveChangesAsync();

            return new UserDto
            {
                UserName = user.UserName,
                Token = _tokenServices.CreateToken(user),
                KnownAs = user.KnownAs,
            };

        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto)
        {
            var user = await _data.Users
                .Include(u => u.Photos)
                .SingleOrDefaultAsync(u => u.UserName == loginDto.UserName.ToLower());

            if (user == null) return NotFound("This user is not existed");

            var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (var i = 0;i<computedHash.Length;i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return BadRequest("Password is not correct");
            }

            return new UserDto
            {
                UserName = loginDto.UserName,
                Token = _tokenServices.CreateToken(user),
                KnownAs = user.KnownAs,
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain).Url
            };
        }

        private async Task<bool> IsExistedUser(string userName)
        {
            return await _data.Users.AnyAsync(u => u.UserName == userName.ToLower());
        }
    }
}