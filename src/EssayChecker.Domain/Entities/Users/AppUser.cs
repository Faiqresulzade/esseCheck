using Microsoft.AspNetCore.Identity;

namespace EssayChecker.Domain.Entities.Users;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    /// <summary>Hesabın soft-delete edildiyi vaxt (UTC) — saxlama müddətinin (30 gün) başlanğıcı.</summary>
    public DateTime? DeletedAt { get; set; }

    public DateTime? LastLoginDate { get; set; }

    /// <summary>Bu istifadəçinin dostlarını dəvət etmək üçün paylaşdığı unikal kod (lazy-generated).</summary>
    public string? ReferralCode { get; set; }

    /// <summary>Qeydiyyat zamanı hansı istifadəçinin referal koduyla gəlib (varsa).</summary>
    public int? ReferredByUserId { get; set; }

    /// <summary>Dəvət edən istifadəçiyə mükafat (əlavə günlər) artıq verilibmi — təkrar verilməsin deyə.</summary>
    public bool ReferralRewardGranted { get; set; }
}
