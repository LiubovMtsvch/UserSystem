using System.ComponentModel.DataAnnotations;

namespace WebApplication.Models //определение полей базы данных
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        public bool IsBlocked { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginTime { get; set; } = DateTime.UtcNow;
    }
}
