namespace WebApplication.DTOs
{
    public class RegisterUserDto
    {
        public string Name { get; set; }     // Имя пользователя
        public string Email { get; set; }    // Email как логин
        public string Password { get; set; } // Пароль (перед хэшированием)
    }
}
