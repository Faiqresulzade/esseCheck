using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Domain.Entities.Essays;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface IEssayRepository
{
    Task AddAsync(Essay essay, CancellationToken cancellationToken = default);

    Task<Essay?> GetByIdAsync(int userId, int essayId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tarixçə siyahısı (səhifələnmiş, yüngül proyeksiya) + say + ortalama bal. Öz esseləri və
    /// şagird esseləri eyni siyahıdadır; <paramref name="studentId"/> / <paramref name="groupId"/>
    /// ilə daraldıla bilər.
    /// </summary>
    Task<EssayHistoryResponse> GetHistoryAsync(int userId, string? search, int? studentId, int? groupId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int userId, int essayId, CancellationToken cancellationToken = default);

    Task<int> DeleteAllAsync(int userId, CancellationToken cancellationToken = default);
}
