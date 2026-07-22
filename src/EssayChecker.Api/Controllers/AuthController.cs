using System.Security.Claims;
using EssayChecker.Application.DTOs.Auth;
using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Qeydiyyat (Register).</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>Giriş (Login) — uğurlu olduqda JWT qaytarır. 5 yanlış cəhddən sonra hesab 15 dəqiqə bloklanır.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await _authService.LoginAsync(request, cancellationToken);
        if (outcome.Succeeded)
            return Ok(outcome.Response);

        if (outcome.FailureReason == LoginFailureReason.LockedOut)
        {
            return StatusCode(StatusCodes.Status423Locked, new
            {
                message = "Hesabınız çoxlu yanlış cəhd səbəbindən müvəqqəti bloklanıb.",
                lockoutEndsAt = outcome.LockoutEndsAt
            });
        }

        return Unauthorized(new { message = "E-mail və ya şifrə yanlışdır." });
    }

    /// <summary>Refresh token ilə yeni access + refresh token alır (rotasiya).</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (response is null)
            return Unauthorized(new { message = "Refresh token etibarsız və ya vaxtı bitib." });

        return Ok(response);
    }

    /// <summary>Çıxış — refresh token-i etibarsız edir.</summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    /// <summary>Şifrəni unutmusunuz — e-mail-ə sıfırlama linki göndərir.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Yeni şifrə yarat — token ilə şifrəni dəyişir və təsdiq e-maili göndərir.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>Cari istifadəçinin profili (JWT tələb olunur).</summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var profile = await _authService.GetProfileAsync(userId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }
}
