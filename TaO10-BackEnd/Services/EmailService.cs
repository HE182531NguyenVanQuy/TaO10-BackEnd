using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TaO10_BackEnd.Interfaces;

namespace TaO10_BackEnd.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordAsync(string toEmail, string temporaryPassword)
    {
        var server = _config["SmtpSettings:Server"];
        var portString = _config["SmtpSettings:Port"];
        var senderName = _config["SmtpSettings:SenderName"];
        var senderEmail = _config["SmtpSettings:SenderEmail"];
        var userName = _config["SmtpSettings:UserName"];
        var password = _config["SmtpSettings:Password"];

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogWarning("SMTP settings are not configured. Skipping sending email to {Email}.", toEmail);
            return;
        }

        if (!int.TryParse(portString, out var port))
            port = 587;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName ?? "TaO10", senderEmail));
        try
        {
            message.To.Add(MailboxAddress.Parse(toEmail));
        }
        catch
        {
            _logger.LogWarning("Invalid recipient email address: {Email}", toEmail);
            return;
        }

        message.Subject = "TaO10 - Mật khẩu tạm thời";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.TextBody = $"Xin chào,\n\nMật khẩu đăng nhập tạm thời của bạn là: {temporaryPassword}\nVui lòng đăng nhập và đổi mật khẩu ngay.\n\nNếu bạn không yêu cầu mật khẩu này, hãy bỏ qua email này.";
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            // Accept all SSL certificates (for dev/test). In production, remove this line.
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(server, port, SecureSocketOptions.StartTls);

            if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
            {
                await client.AuthenticateAsync(userName, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Temporary password email sent to {Email}", toEmail);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // swallow or rethrow depending on desired behavior; we swallow to not block registration
        }
    }
}
