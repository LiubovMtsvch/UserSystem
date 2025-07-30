using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication.Data;
using WebApplication.Models;
using WebApplication.DTOs;
using Microsoft.AspNetCore.Identity;

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
                    return StatusCode(403, "User is blocked and cannot register.");


                if (!existingUser.IsDeleted)
                    return Conflict("User with such email already exists.");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash =  BCrypt.Net.BCrypt.HashPassword(dto.Password),
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
                    u.IsBlocked, 
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
                return Unauthorized(new { message = "User can't log in (blocked)" });


            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Incorrect password");


            user.LastLoginTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Log in was successful",
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

        [AllowAnonymous]
        [HttpPost("reset-request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] EmailDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || user.IsDeleted)
                return NotFound("User not found");

            // Сгенерировать простой токен (в реальности использовать JWT или Guid + Expiry)
            var token = Guid.NewGuid().ToString();

            // Можно сохранить токен в базе, но здесь просто добавим его в URL
            var resetUrl = $"https://yourdomain.com/reset-password.html?email={user.Email}&token={token}";

            // Отладочно: вывод в консоль
            Console.WriteLine($"Reset link: {resetUrl}");

            return Ok("Reset link has been sent to your email (simulated).");
        }

        public class EmailDto
        {
            public string Email { get; set; }
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || user.IsDeleted)
                return NotFound("User not found");

            // Пропускаем проверку токена для упрощения (но на проде обязателен!)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Password has been reset.");
        }

        public class ResetPasswordDto
        {
            public string Email { get; set; }
            public string Token { get; set; } // сейчас не проверяется
            public string NewPassword { get; set; }
        }


    }
}
