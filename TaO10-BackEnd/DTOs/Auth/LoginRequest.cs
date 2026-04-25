using System.ComponentModel.DataAnnotations;

namespace TaO10_BackEnd.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
}
