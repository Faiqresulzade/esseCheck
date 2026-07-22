namespace EssayChecker.Application.Common;

/// <summary>AI (OpenRouter) xidmətindən qaynaqlanan xəta. Transient olduqda yenidən cəhd etmək olar.</summary>
public sealed class AiServiceException : Exception
{
    public bool IsTransient { get; }

    public AiServiceException(string message, bool isTransient = false, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }
}
