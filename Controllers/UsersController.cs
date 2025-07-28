using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication.Data;
using WebApplication.Models;
using WebApplication.DTOs;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    [Authorize]
    public class UsersController : ControllerBase

    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // 📥 Регистрация (доступна без авторизации)
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return Conflict("User with such email already exists");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                RegisteredAt = DateTime.UtcNow,
                LastLoginTime = DateTime.UtcNow,
                IsBlocked = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Register was successful");
        }

        //condition of user
        private async Task<bool> IsBlockedOrDeleted(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user == null || user.IsBlocked /* || user.IsDeleted */;
        }

        //sorted by register data
        [AllowAnonymous]
        [HttpGet("all")]

        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Where(u => !u.IsBlocked /* && !u.IsDeleted */)
                .OrderByDescending(u => u.LastLoginTime)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.RegisteredAt
                })
                .ToListAsync();

            return Ok(users);
        }

        //get one user by email
        [HttpGet("{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            if (await IsBlockedOrDeleted(email))
                return Forbid("User is blocked or not found");

            var user = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.RegisteredAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User not found");

            return Ok(user);
        }

        //block
        [HttpPost("block")]
        public async Task<IActionResult> BlockUsers([FromBody] List<int> userIds)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            foreach (var user in users)
            {
                user.IsBlocked = true;
            }
            await _context.SaveChangesAsync();
            return Ok("User iss blocked");
        }

        //unblock
        [HttpPost("unblock")]
        public async Task<IActionResult> UnblockUsers([FromBody] List<int> userIds)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            foreach (var user in users)
            {
                user.IsBlocked = false;
            }
            await _context.SaveChangesAsync();
            return Ok("User was unblocked");
        }

        //delete user
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteUsers([FromBody] List<int> userIds)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();
            return Ok("User was deleted");
        }
    }
}
