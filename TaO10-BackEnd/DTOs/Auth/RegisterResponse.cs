namespace TaO10_BackEnd.DTOs.Auth
{
    public class RegisterResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
