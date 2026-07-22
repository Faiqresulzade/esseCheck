namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>Şəkildən mətn oxumaq (OCR).</summary>
public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream imageStream, string contentType, CancellationToken cancellationToken = default);
}
