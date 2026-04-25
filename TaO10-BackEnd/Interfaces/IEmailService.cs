using System.Threading.Tasks;

namespace TaO10_BackEnd.Interfaces;

public interface IEmailService
{
    Task SendPasswordAsync(string toEmail, string temporaryPassword);
}
