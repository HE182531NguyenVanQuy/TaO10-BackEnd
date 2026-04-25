using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.DTOs.Auth;
using TaO10_BackEnd.Interfaces;
using TaO10_BackEnd.Models;
using TaO10_BackEnd.Helpers;

namespace TaO10_BackEnd.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtHelper _jwtHelper;
    private readonly IEmailService _emailService;

    public AuthService(AppDbContext context, JwtHelper jwtHelper, IEmailService emailService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jwtHelper = jwtHelper ?? throw new ArgumentNullException(nameof(jwtHelper));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Vui lòng nhập email của bạn.", nameof(request.Email));

        var fullName = request.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Vui lòng nhập họ và tên của bạn.", nameof(request.FullName));

        var location = request.Location?.Trim();
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Vui lòng nhập địa điểm của bạn.", nameof(request.Location));

        var normalizedEmail = email.ToLowerInvariant();
        var phone = request.Phone.Trim();

        try
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);

            if (emailExists)
                throw new InvalidOperationException("Email đã tồn tại.");

            // If phone provided, ensure it's unique
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var phoneExists = await _context.Users
                    .AnyAsync(u => u.Phone != null && u.Phone == phone);

                if (phoneExists)
                    throw new InvalidOperationException("Số điện thoại đã được đăng ký.");
            }

            // Generate temporary 6-digit numeric password
            var tempPassword = GenerateSixDigitPassword();
            var hashed = PasswordHasher.HashPassword(tempPassword);

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashed,
                FullName = fullName,
                Phone = phone,
                Role = "customer",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                AvatarUrl = "",
                Location = location,
                TotalScore = 0,
                TotalExams = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send temporary password to user's email (fire-and-forget)
            _ = _emailService.SendPasswordAsync(email, tempPassword);

            return new RegisterResponse
            {
                Email = user.Email,
                FullName = user.FullName
            };
        }
        catch (DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? string.Empty;
            if (inner.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                inner.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Try to determine whether duplicate is for email or phone
                if (!string.IsNullOrWhiteSpace(phone) && inner.IndexOf("phone", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("Số điện thoại đã được đăng ký.", dbEx);

                throw new InvalidOperationException("Email đã tồn tại.", dbEx);
            }

            throw new ApplicationException("Lỗi cơ sở dữ liệu khi tạo người dùng.", dbEx);
        }
    }

    private static string GenerateSixDigitPassword()
    {
        // generate secure 6-digit numeric string
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return value.ToString("D6");
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Vui lòng nhập email của bạn.", nameof(request.Email));

        var password = request.Password ?? string.Empty;
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Vui lòng nhập mật khẩu của bạn.", nameof(request.Password));

        var normalizedEmail = email.ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);

        if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Thông tin đăng nhập không hợp lệ.");

        // Generate tokens
        var accessToken = _jwtHelper.GenerateToken(user);
        var refreshToken = GenerateSecureToken();

        var refreshTokenEntity = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Vui lòng nhập Refresh token.", nameof(refreshToken));

        var tokenEntity = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (tokenEntity == null ||
            tokenEntity.IsRevoked ==true ||
            tokenEntity.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");

        // Revoke old token
        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;

        // Create new refresh token and access token
        var newRefreshToken = GenerateSecureToken();
        var newRefreshEntity = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = tokenEntity.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshEntity);

        var user = tokenEntity.User ?? throw new ApplicationException("Refresh token không tham chiếu đến người dùng.");
        var newAccessToken = _jwtHelper.GenerateToken(user);

        await _context.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Vui lòng nhập Refresh token.", nameof(refreshToken));

        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (tokenEntity == null)
            throw new KeyNotFoundException("Refresh token không tồn tại.");

        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static string GenerateSecureToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        // base64 is fine for storage and transmission; remove padding for compactness if desired
        return Convert.ToBase64String(randomNumber);
    }
}