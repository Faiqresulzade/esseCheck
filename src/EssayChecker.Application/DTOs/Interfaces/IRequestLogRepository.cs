namespace EssayChecker.Application.DTOs.Interfaces;

public interface IRequestLogRepository
{
    /// <summary>Verilmiş tarixdən köhnə logları silir (saxlama müddəti bitmiş). Silinən sayı qaytarılır.</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
