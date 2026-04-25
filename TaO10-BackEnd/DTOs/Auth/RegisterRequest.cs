using System.ComponentModel.DataAnnotations;

namespace TaO10_BackEnd.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string FullName { get; set; } = null!;
        [Required]
        public string Phone { get; set; } = null!;
        [Required]
        public string Location { get; set; } = null!;
    }
}
