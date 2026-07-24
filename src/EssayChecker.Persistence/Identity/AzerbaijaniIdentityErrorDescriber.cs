using Microsoft.AspNetCore.Identity;

namespace EssayChecker.Persistence.Identity;

/// <summary>ASP.NET Core Identity-nin defolt (İngilis) xəta mesajlarını Azərbaycan dilinə tərcümə edir.</summary>
public class AzerbaijaniIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Naməlum xəta baş verdi." };

    public override IdentityError ConcurrencyFailure() =>
        new() { Code = nameof(ConcurrencyFailure), Description = "Məlumat başqa bir əməliyyat tərəfindən dəyişdirilib. Zəhmət olmasa yenidən cəhd edin." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Şifrə yanlışdır." };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = "Token etibarsızdır." };

    public override IdentityError LoginAlreadyAssociated() =>
        new() { Code = nameof(LoginAlreadyAssociated), Description = "Bu giriş məlumatları artıq başqa bir hesaba bağlıdır." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = $"'{userName}' istifadəçi adı yalnız hərf və rəqəmlərdən ibarət ola bilər." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = $"'{email}' düzgün e-mail ünvanı deyil." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"'{userName}' artıq istifadə olunur." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"'{email}' e-mail ünvanı artıq qeydiyyatdan keçib." };

    public override IdentityError InvalidRoleName(string? role) =>
        new() { Code = nameof(InvalidRoleName), Description = $"'{role}' rol adı etibarsızdır." };

    public override IdentityError DuplicateRoleName(string role) =>
        new() { Code = nameof(DuplicateRoleName), Description = $"'{role}' rolu artıq mövcuddur." };

    public override IdentityError UserAlreadyHasPassword() =>
        new() { Code = nameof(UserAlreadyHasPassword), Description = "İstifadəçinin artıq şifrəsi var." };

    public override IdentityError UserLockoutNotEnabled() =>
        new() { Code = nameof(UserLockoutNotEnabled), Description = "Bu istifadəçi üçün bloklama (lockout) aktiv deyil." };

    public override IdentityError UserAlreadyInRole(string role) =>
        new() { Code = nameof(UserAlreadyInRole), Description = $"İstifadəçi artıq '{role}' rolundadır." };

    public override IdentityError UserNotInRole(string role) =>
        new() { Code = nameof(UserNotInRole), Description = $"İstifadəçi '{role}' rolunda deyil." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Şifrə ən azı {length} simvoldan ibarət olmalıdır." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifrədə ən azı bir xüsusi simvol olmalıdır (məs. @, #, $)." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Şifrədə ən azı bir rəqəm olmalıdır (0-9)." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Şifrədə ən azı bir kiçik hərf olmalıdır (a-z)." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "Şifrədə ən azı bir böyük hərf olmalıdır (A-Z)." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Şifrədə ən azı {uniqueChars} unikal simvol olmalıdır." };

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Bərpa kodu etibarsızdır və ya artıq istifadə olunub." };
}
