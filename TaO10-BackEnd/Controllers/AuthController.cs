using Microsoft.AspNetCore.Mvc;
using TaO10_BackEnd.DTOs.Auth;
using TaO10_BackEnd.Interfaces;

namespace TaO10_BackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _authService.RegisterAsync(request);
            return CreatedAtAction(null, new { userId = result.UserId, email = result.Email });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var tokens = await _authService.LoginAsync(request);
            return Ok(tokens);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var tokens = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(tokens);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _authService.RevokeTokenAsync(request.RefreshToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Token not found" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}