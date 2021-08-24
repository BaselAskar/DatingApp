using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        public AdminController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-role")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await _userManager.Users
                            .Include(ur => ur.UserRoles)
                            .ThenInclude(r => r.Role)
                            .Select(u => new
                            {
                                u.Id,
                                UserName = u.UserName,
                                Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                            })
                            .ToListAsync();
            

            return Ok(users);
        }

        [HttpPost("edit-roles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName,[FromQuery]string roles)
        {
            var selectedRoles = roles.Split(",");

            var user = await _userManager.FindByNameAsync(userName);

            var userRoles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.AddToRolesAsync(user,selectedRoles.Except(userRoles));

            if (!result.Succeeded) return BadRequest("Faild to add roles");

            result = await _userManager.RemoveFromRolesAsync(user,userRoles.Except(selectedRoles));

            if (!result.Succeeded) return BadRequest("Faild to remove roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photo-to-moderate")]
        public ActionResult GetPhotoForModerating()
        {
            return Ok("Admin and Moderator can see this");

        }

    }
}