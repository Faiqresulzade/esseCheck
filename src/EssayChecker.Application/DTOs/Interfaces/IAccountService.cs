using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Account;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface IAccountService
{
    Task<AuthResult> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Hesabı soft delete edir (IsDeleted=true) + token/abunəlikləri deaktiv edir.</summary>
    Task<AuthResult> DeleteAccountAsync(int userId, CancellationToken cancellationToken = default);
}
