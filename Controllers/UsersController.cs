using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication.Data;
using WebApplication.Models;
using WebApplication.DTOs;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("api/users")] // фиксированный маршрут
    
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (existingUser != null)
            {
                if (existingUser.IsBlocked)
                    return Forbid("User is blocked and cannot register.");

                if (!existingUser.IsDeleted)
                    return Conflict("User with such email already exists.");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = dto.Password,
                RegisteredAt = DateTime.UtcNow,
                LastLoginTime = DateTime.UtcNow,
                IsBlocked = false,
                IsDeleted = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Registration was successful");
        }

        private async Task<bool> IsBlockedOrDeleted(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user == null || user.IsBlocked || user.IsDeleted;
        }

       

        [AllowAnonymous]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.LastLoginTime)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.RegisteredAt,
                    Status = u.IsBlocked ? "Blocked" : "Active"
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            if (await IsBlockedOrDeleted(email))
                return Forbid("User is blocked or not found.");

            var user = await _context.Users
                .Where(u => u.Email == email && !u.IsDeleted)
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

        [HttpPost("block")]
        public async Task<IActionResult> BlockUsers([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
                return BadRequest("No user IDs provided.");

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .ToListAsync();

            if (!users.Any())
                return NotFound("Users not found.");

            foreach (var user in users)
                user.IsBlocked = true;

            await _context.SaveChangesAsync();
            return Ok("Users have been blocked.");
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return Unauthorized("Пользователь не найден.");

            if (user.IsBlocked || user.IsDeleted)
                return Unauthorized("Пользователь не может войти.");

            if (user.PasswordHash != dto.Password)
                return Unauthorized("Неверный пароль.");

            user.LastLoginTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Вход выполнен успешно",
                Redirect = "/table.html", // 👈 Фронт переходит сюда
                User = new
                {
                    user.Id,
                    user.Name,
                    user.Email
                }
            });
        }


        [HttpPost("unblock")]
        public async Task<IActionResult> UnblockUsers([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
                return BadRequest("No user IDs provided.");

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .ToListAsync();

            if (!users.Any())
                return NotFound("Users not found.");

            foreach (var user in users)
                user.IsBlocked = false;

            await _context.SaveChangesAsync();
            return Ok("Users have been unblocked.");
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteUsers([FromBody] List<int> userIds)
        {
            if (userIds == null || userIds.Count == 0)
                return BadRequest("No user IDs provided.");

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .ToListAsync();

            if (users.Count == 0)
                return NotFound("Users not found.");

            foreach (var user in users)
            {
                user.IsDeleted = true;
                Console.WriteLine($"User ID {user.Id} marked as deleted.");
            }

            await _context.SaveChangesAsync();
            return Ok("Users have been deleted.");
        }
    }
}
