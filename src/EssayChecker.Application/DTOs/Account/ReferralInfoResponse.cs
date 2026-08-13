namespace EssayChecker.Application.DTOs.Account;

/// <summary>
/// <paramref name="Enabled"/> false olanda referal proqramı hələ aktiv deyil — frontend bu
/// halda popup/Ayarlar sahəsini göstərməməlidir. <paramref name="ReferralCode"/> yalnız
/// <paramref name="Enabled"/>=true olanda dolu olur.
/// </summary>
public sealed record ReferralInfoResponse(bool Enabled, string? ReferralCode, string? PlayStoreUrl);
