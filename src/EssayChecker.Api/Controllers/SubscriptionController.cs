using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EssayChecker.Api.Models;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Application.Settings;
using EssayChecker.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EssayChecker.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUsageLimitService _usageLimitService;
    private readonly GooglePlaySettings _googlePlaySettings;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        IUsageLimitService usageLimitService,
        IOptions<GooglePlaySettings> googlePlaySettings,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _usageLimitService = usageLimitService;
        _googlePlaySettings = googlePlaySettings.Value;
        _logger = logger;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Plan kataloqu (Planlar ekranı).</summary>
    [AllowAnonymous]
    [HttpGet("plans")]
    public IActionResult Plans() => Ok(_subscriptionService.GetPlans());

    /// <summary>Cari istifadəçinin aktiv abunəliyi.</summary>
    [HttpGet]
    public async Task<IActionResult> My(CancellationToken cancellationToken) =>
        Ok(await _subscriptionService.GetMySubscriptionAsync(UserId, cancellationToken));

    /// <summary>Manual/admin plan keçidi (test məqsədilə). Google Play üçün /google/verify istifadə edin.</summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken)
    {
        if (request.Plan == SubscriptionPlan.Free)
            return BadRequest(new { message = "Free plana abunə olmaq lazım deyil. Ləğv üçün /cancel istifadə edin." });

        var result = await _subscriptionService.SubscribeAsync(UserId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Abunəliyi ləğv edir (Free-yə keçir).</summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken) =>
        Ok(await _subscriptionService.CancelAsync(UserId, cancellationToken));

    /// <summary>Gündəlik istifadə statusu (Əsas səhifədəki "pulsuz şans" bloku).</summary>
    [HttpGet("usage")]
    public async Task<IActionResult> Usage(CancellationToken cancellationToken) =>
        Ok(await _usageLimitService.GetStatusAsync(UserId, cancellationToken));

    /// <summary>
    /// Client Google Play Billing ilə satınalma etdikdən sonra çağırır (productId + purchaseToken).
    /// PurchaseToken birbaşa Google Play Developer API ilə təsdiqlənir.
    /// </summary>
    [HttpPost("google/verify")]
    public async Task<IActionResult> VerifyGooglePurchase(
        [FromBody] VerifyGooglePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _subscriptionService.VerifyGooglePurchaseAsync(UserId, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Google Cloud Pub/Sub push endpoint-i (RTDN). Google çağırır, istifadəçi tərəfindən çağırılmır.
    /// Doğrulama üçün query string-də paylaşılan sirr tələb olunur: ?secret=...
    /// </summary>
    [AllowAnonymous]
    [HttpPost("google/rtdn")]
    public async Task<IActionResult> GooglePlayNotification(
        [FromQuery] string? secret,
        [FromBody] PubSubPushEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(secret) || !ConstantTimeEquals(secret, _googlePlaySettings.RtdnSharedSecret))
            return Unauthorized();

        try
        {
            await _subscriptionService.ProcessGooglePlayNotificationAsync(
                envelope.Message.Data, envelope.Message.MessageId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Pub/Sub eyni mesajı yenidən çatdıracaq (idempotent emal sayəsində təhlükəsizdir),
            // ona görə xətanı loglayıb 200 qaytarırıq ki, sonsuz retry storminə səbəb olmasın.
            _logger.LogError(ex, "Google RTDN emalı zamanı xəta baş verdi.");
        }

        return Ok();
    }

    /// <summary>Timing-attack-a qarşı sabit vaxtlı müqayisə.</summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return bytesA.Length == bytesB.Length &&
               CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
