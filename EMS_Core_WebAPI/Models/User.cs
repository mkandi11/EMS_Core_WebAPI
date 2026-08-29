using System.ComponentModel.DataAnnotations;

namespace EMS_Core_WebAPI.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
        public string PasswordHash { get; set; } = string.Empty;
    }

    public class Login
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}
