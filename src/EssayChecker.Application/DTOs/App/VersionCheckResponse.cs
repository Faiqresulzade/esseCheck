namespace EssayChecker.Application.DTOs.App;

public sealed record VersionCheckResponse(
    bool UpdateAvailable,
    string? LatestVersion,
    string? PlayStoreUrl);
