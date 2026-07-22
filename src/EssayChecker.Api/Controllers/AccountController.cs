using System.Security.Claims;
using EssayChecker.Application.DTOs.Account;
using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Profili redaktə et (ad/soyad).</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountService.UpdateProfileAsync(UserId, request, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>Şifrəni dəyiş (cari şifrə ilə).</summary>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountService.ChangePasswordAsync(UserId, request, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>Hesabı sil (soft delete).</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        var result = await _accountService.DeleteAccountAsync(UserId, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
