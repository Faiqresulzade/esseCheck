using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.DTOs.Subscriptions;

/// <summary>Client Google Play Billing ilə satınalma etdikdən sonra göndərir.</summary>
public sealed class VerifyGooglePurchaseRequest
{
    /// <summary>Google Play subscription/product ID (məs. "pro_monthly").</summary>
    [Required(ErrorMessage = "ProductId boş ola bilməz.")]
    public string ProductId { get; set; } = null!;

    [Required(ErrorMessage = "PurchaseToken boş ola bilməz.")]
    public string PurchaseToken { get; set; } = null!;
}
