namespace TaO10_BackEnd.DTOs.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
    }
}
