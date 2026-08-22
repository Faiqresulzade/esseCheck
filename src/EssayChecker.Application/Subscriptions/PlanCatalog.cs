using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.Subscriptions;

/// <summary>
/// Planlar ekranı üçün plan kataloqu (qiymət + xüsusiyyətlər). Bax PlanPolicy — buradakı
/// DailyLimit/Unlimited dəyərləri PlanPolicy ilə əl ilə sinxronlaşdırılır, avtomatik deyil,
/// çünki bu, yalnız DİSPLAY məlumatıdır (real limit yoxlaması həmişə PlanPolicy-dən keçir).
/// </summary>
public static class PlanCatalog
{
    public static IReadOnlyList<PlanInfoResponse> All { get; } = new[]
    {
        new PlanInfoResponse(
            SubscriptionPlan.Free, "Free", 0m, "USD", "ay",
            Unlimited: false, DailyLimit: 1,
            Features: new[]
            {
                "Gündə 1 esse yoxlama",
                "Limitsiz tarixçə",
                "Gündə 1 AI dərs izahı",
                "Şagird analitikası"
            }),
        new PlanInfoResponse(
            SubscriptionPlan.Pro, "Pro", 2.99m, "USD", "ay",
            Unlimited: false, DailyLimit: 10,
            Features: new[]
            {
                "Gündə 10 esse yoxlama",
                "Limitsiz tarixçə",
                "Gündə 1 AI dərs izahı",
                "Şagird analitikası"
            }),
        new PlanInfoResponse(
            // QEYD: əvvəllər ProPlus "limitsiz esse" idi (DailyLimit: null). Yeni 4 pilləli
            // struktura keçidlə bu, 20/gün ədədi həddə dəyişdi (bax PlanPolicy). Bu, mövcud
            // ödəyən ProPlus abunəçiləri üçün davranış dəyişikliyidir — diqqətli olun.
            SubscriptionPlan.ProPlus, "Pro Plus", 5.99m, "USD", "ay",
            Unlimited: false, DailyLimit: 20,
            Features: new[]
            {
                "Gündə 20 esse yoxlama",
                "Limitsiz tarixçə",
                "Gündə 2 AI dərs izahı",
                "Şagird analitikası"
            }),
        new PlanInfoResponse(
            // Price: istifadəçi tərəfindən təsdiqlənib (2026-08-23). Bu, real Play Console
            // qiyməti DEYİL — o, ayrıca Play Console-da regional valyuta ilə qurulur; burada
            // göstərilən $10.99 yalnız display üçündür, faktiki tutulan məbləğ Play Console-un
            // özündə təyin olunmalıdır.
            //
            // QEYD — "Limitsiz": marketinq mətni "Limitsiz esse yoxlama" deyir, backend isə
            // 40/gün fair-use həddi tutur (real ölçülmüş AI xərcinə görə, bax PlanPolicy
            // şərhi). Buradakı Unlimited/DailyLimit sahələri REAL enforced dəyəri əks etdirir
            // ki, /api/subscription/usage ilə ziddiyyət yaranmasın — mobil tətbiqin öz
            // marketinq mətni istənilən formada qala bilər, bu, yalnız API kontraktıdır.
            SubscriptionPlan.Premium, "Premium", 10.99m, "USD", "ay",
            Unlimited: false, DailyLimit: 40,
            Features: new[]
            {
                "Gündə 40 esse yoxlama",
                "Limitsiz tarixçə",
                "Gündə 4 AI dərs izahı",
                "Şagird analitikası"
            })
    };
}
