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
                return Conflict("Пользователь с таким email уже существует.");
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

            return Ok("Регистрация успешна!");
        }

        // 🔍 Проверка состояния пользователя
        private async Task<bool> IsBlockedOrDeleted(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user == null || user.IsBlocked /* || user.IsDeleted */;
        }

        // 📤 Получить всех пользователей (отсортировано по входу)
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

        // 🔍 Получить одного по Email
        [HttpGet("{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            if (await IsBlockedOrDeleted(email))
                return Forbid("Пользователь заблокирован или не найден.");

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
                return NotFound("Пользователь не найден");

            return Ok(user);
        }

        // 🚫 Блокировка
        [HttpPost("block")]
        public async Task<IActionResult> BlockUsers([FromBody] List<int> userIds)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            foreach (var user in users)
            {
                user.IsBlocked = true;
            }
            await _context.SaveChangesAsync();
            return Ok("Пользователи заблокированы.");
        }

        // 🔓 Разблокировка
        [HttpPost("unblock")]
        public async Task<IActionResult> UnblockUsers([FromBody] List<int> userIds)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            foreach (var user in users)
            {
                user.IsBlocked = false;
            }
            await _context.SaveChangesAsync();
            return Ok("Пользователи разблокированы.");
        }

        // 🗑 Удаление пользователей
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteUsers([FromBody] List<int> userIds)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();
            return Ok("Пользователи удалены.");
        }
    }
}
