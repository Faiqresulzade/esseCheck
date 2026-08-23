using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class AccountPurgeRepository : IAccountPurgeRepository
{
    private readonly EssayDbContext _db;

    public AccountPurgeRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<int> PurgeExpiredDeletedAccountsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        // Bağlı məlumatlar bazanın özü tərəfindən idarə olunur:
        //  - Cascade (silinir): Essays, UserSubscriptions, DailyUsages, RefreshTokens,
        //    StudentGroups → Students, Identity-nin öz Claims/Logins/Roles/Tokens cədvəlləri.
        //  - SetNull (qalır): Lessons.CreatedByUserId — ortaq kitabxana sətri başqa müəllimlərə
        //    lazımdır, ona görə dərs silinmir, yalnız yaradan sahəsi boşalır.
        //  - DeviceTrials-in FK-sı QƏSDƏN yoxdur: cihaz qeydi hesabdan sonra da qalmalıdır,
        //    əks halda "hesabı sil, yenidən qeydiyyatdan keç" ilə pulsuz sınaq təkrar alınardı.
        //
        // DİQQƏT: burada Restrict davranışlı hər hansı FK əlavə etmək BÜTÜN təmizləməni
        // bloklayır — bu, tək toplu əməliyyatdır, bir sətir uğursuz olsa hamısı geri qayıdır.
        return await _db.Users
            .Where(u => u.IsDeleted && u.DeletedAt != null && u.DeletedAt <= cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
