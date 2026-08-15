namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>
/// Referal (dəvət et, endirim qazan) mükafatını idarə edir. Mükafat yalnız dəvət edilən
/// istifadəçinin İLK təsdiqlənmiş ödənişli abunəliyində verilir — yeniləmələr və plan
/// dəyişiklikləri təkrar mükafat gətirmir.
/// </summary>
public interface IReferralRewardService
{
    /// <summary>
    /// Cari satınalma referal mükafatını tetikləyirmi (proqram aktivdirmi VƏ bu, istifadəçinin
    /// ilk abunəliyidirmi)? Abunəlik cədvəlinə hər hansı dəyişiklikdən ƏVVƏL, təmiz vəziyyətdə
    /// çağırılmalıdır — əks halda yeni yazılan abunəlik "ilk deyil" kimi görünər.
    /// </summary>
    Task<bool> IsRewardTriggerAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dəvət edənin abunəliyini bonus günlərlə uzadır. Bütün xətalar udulub yalnız loglanır —
    /// referal mükafatı heç vaxt əsas satınalma axınını poza bilməz.
    /// </summary>
    Task TryGrantRewardAsync(int referredUserId, CancellationToken cancellationToken = default);
}
