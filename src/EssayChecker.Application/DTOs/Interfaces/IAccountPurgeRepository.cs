namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>Silmə tələbi 30+ gün əvvəl olan hesabları həqiqətən (bərpaolunmaz) silir.</summary>
public interface IAccountPurgeRepository
{
    /// <summary>
    /// <c>IsDeleted=true</c> və <c>DeletedAt &lt;= cutoffUtc</c> olan bütün istifadəçiləri silir.
    /// Cascade FK-lar sayəsində bağlı esse/abunəlik/token qeydləri də avtomatik silinir.
    /// Silinən istifadəçi sayı qaytarılır.
    /// </summary>
    Task<int> PurgeExpiredDeletedAccountsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
