using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace EssayChecker.Infrastructure.Imaging;

/// <summary>
/// Şəkli AI-a göndərməzdən əvvəl kiçildir.
///
/// SƏBƏB — XƏRC. Vision modelləri şəkli "tile"-lara bölüb hər tile üçün token sayır, yəni xərc
/// ölçü ilə artır. Real ölçmə (2026-09-03, gpt-5.6-luna, eyni mətn):
///   1000x700  ->  1119 prompt token,  $0.00056
///   3024x4032 -> 14639 prompt token,  $0.00397   (7 dəfə baha)
/// Telefon fotoları məhz ikinci ölçüdədir.
///
/// Model seçimi burada kömək etmir: eyni böyük foto üçün gpt-4o-mini də, gpt-5.6-luna da praktiki
/// eyni məbləği tutur ($0.00397) — xərci diktə edən şəklin ölçüsüdür, modelin adı yox.
///
/// Kitabxana seçimi: SkiaSharp (MIT). System.Drawing .NET 6-dan sonra yalnız Windows-dur, bizim
/// production isə Linux konteynerdir; ImageSharp isə 4.x-dən etibarən kommersiya lisenziyası
/// tələb edir. Linux native faylları SkiaSharp.NativeAssets.Linux.NoDependencies ilə gəlir —
/// fontconfig kimi əlavə sistem paketi lazım deyil (biz yalnız decode/resize/encode edirik).
/// </summary>
internal static class ImageDownscaler
{
    /// <summary>Uzun tərəfin maksimum piksel sayı. Bundan kiçik şəkillər olduğu kimi saxlanılır (böyüdülmür).</summary>
    public const int MaxEdge = 1600;

    private const int JpegQuality = 85;

    /// <summary>
    /// Şəkli lazım gələrsə kiçildir və JPEG kimi qaytarır.
    ///
    /// Şəkil oxuna bilməsə (zədəli fayl, dəstəklənməyən format) orijinal baytlar olduğu kimi
    /// qaytarılır — burada sorğunu sındırmaq mənasızdır, qoy AI özü daha aydın xəta versin.
    /// </summary>
    public static (byte[] Data, string ContentType) Prepare(byte[] original, string contentType, ILogger logger)
    {
        try
        {
            // SKCodec EXIF çevrilməsini oxuyur — telefon fotoları çox vaxt "yan" saxlanılır və
            // düzəldilməsə AI-a yan gedər, transkripsiya korlanar.
            using var codec = SKCodec.Create(new MemoryStream(original));
            if (codec is null)
            {
                logger.LogWarning("Şəkil formatı tanınmadı, orijinal ölçü göndərilir.");
                return (original, contentType);
            }

            using var bitmap = SKBitmap.Decode(codec);
            if (bitmap is null)
            {
                logger.LogWarning("Şəkil dekod edilmədi, orijinal ölçü göndərilir.");
                return (original, contentType);
            }

            using var oriented = ApplyOrientation(bitmap, codec.EncodedOrigin);

            var longestEdge = Math.Max(oriented.Width, oriented.Height);
            var scale = longestEdge > MaxEdge ? (double)MaxEdge / longestEdge : 1.0;

            var width = Math.Max(1, (int)Math.Round(oriented.Width * scale));
            var height = Math.Max(1, (int)Math.Round(oriented.Height * scale));

            using var resized = scale < 1.0
                ? oriented.Resize(new SKImageInfo(width, height), SKSamplingOptions.Default)
                : oriented.Copy();

            if (resized is null)
            {
                logger.LogWarning("Şəkil ölçüsü dəyişdirilmədi, orijinal göndərilir.");
                return (original, contentType);
            }

            using var image = SKImage.FromBitmap(resized);
            using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);

            var result = encoded.ToArray();
            logger.LogDebug(
                "Şəkil AI üçün hazırlandı: {OriginalKb} KB -> {NewKb} KB ({Width}x{Height}).",
                original.Length / 1024, result.Length / 1024, width, height);

            return (result, "image/jpeg");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Şəkli kiçiltmək mümkün olmadı, orijinal ölçü göndərilir.");
            return (original, contentType);
        }
    }

    /// <summary>Stream variantı — OCR endpoint-i faylı stream kimi verir.</summary>
    public static async Task<(byte[] Data, string ContentType)> PrepareAsync(
        Stream source, string contentType, ILogger logger, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        return Prepare(buffer.ToArray(), contentType, logger);
    }

    /// <summary>EXIF-dəki çevrilməni real piksellərə tətbiq edir.</summary>
    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.Default or SKEncodedOrigin.TopLeft)
            return source.Copy();

        var swapSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var target = new SKBitmap(
            swapSides ? source.Height : source.Width,
            swapSides ? source.Width : source.Height);

        using var canvas = new SKCanvas(target);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Scale(-1, 1, source.Width / 2f, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.RotateDegrees(180, source.Width / 2f, source.Height / 2f); break;
            case SKEncodedOrigin.BottomLeft: canvas.Scale(1, -1, 1, source.Height / 2f); break;
            case SKEncodedOrigin.LeftTop: canvas.Translate(target.Width, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(target.Width, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(0, target.Height); canvas.RotateDegrees(270); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, target.Height); canvas.RotateDegrees(270); break;
        }

        using var sourceImage = SKImage.FromBitmap(source);
        canvas.DrawImage(sourceImage, 0, 0, SKSamplingOptions.Default, paint: null);
        return target;
    }
}
