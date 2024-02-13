using Flurl.Http;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Helpers;

public static class BooruHelper
{
    public static async Task<BooruImage> GetFeaturedImage()
    {
        var booruSettings = GetBooruSettings();

        var results = await $"{booruSettings.Endpoint}/api/{booruSettings.Version}/json/images/featured"
            .WithHeader("user-agent", $"Izzy-Moonbot (Linux x86_64) Flurl.Http/3.2.4 DotNET/8.0")
            .SetQueryParam("key", booruSettings.Token)
            .GetAsync()
            .ReceiveJson();

        var image = new BooruImage
        {
            CreatedAt = results.image.created_at,
            Id = results.image.id,
            Spoilered = results.image.spoilered,
            ThumbnailsGenerated = results.image.thumbnails_generated,
            // Special parameters which need to be initialised outside the object assignment.
            Format = results.image.format switch
            {
                "png" => BooruImageFormat.PNG,
                "jpg" => BooruImageFormat.JPG,
                "jpeg" => BooruImageFormat.JPEG,
                "svg" => BooruImageFormat.SVG,
                "webm" => BooruImageFormat.WebM,
                "gif" => BooruImageFormat.GIF,
                _ => throw new ArgumentOutOfRangeException(nameof(results.image.format))
            }
        };
        image.Representations = new BooruImagesRepresentations(image.Id, image.Format, image.CreatedAt);

        return image;
    }

    private static BooruSettings GetBooruSettings()
    {
        var config = new ConfigurationBuilder()
#if DEBUG
            .AddJsonFile("appsettings.Development.json")
#else
            .AddJsonFile("appsettings.json")
#endif
            .Build();

        var section = config.GetSection(nameof(BooruSettings));
        var settings = section.Get<BooruSettings>() ?? throw new NullReferenceException("Booru settings is null!");

        return settings;
    }
}

public class BooruImage
{
    public DateTimeOffset CreatedAt { get; set; }
    public BooruImageFormat Format { get; set; }
    public long Id { get; set; }
    public BooruImagesRepresentations? Representations { get; set; }
    public bool Spoilered { get; set; }
    public bool ThumbnailsGenerated { get; set; }

    public BooruImage()
    { }
}

public class BooruImagesRepresentations(long id, BooruImageFormat format, DateTimeOffset createdAt)
{
    public string Full { get; } =
            $"https://static.manebooru.art/img/view/{createdAt.Year}/{createdAt.Month}/{createdAt.Day}/{id}.{ImageFormatToString(format)}";

    private readonly string _rawUrl = $"https://static.manebooru.art/img/{createdAt.Year}/{createdAt.Month}/{createdAt.Day}/{id}";
    private readonly BooruImageFormat _format = format;

    private static string ImageFormatToString(BooruImageFormat format)
    {
        return format switch
        {
            BooruImageFormat.PNG => "png",
            BooruImageFormat.JPG => "jpg",
            BooruImageFormat.JPEG => "jpeg",
            BooruImageFormat.SVG => "svg",
            BooruImageFormat.WebM => "webm",
            BooruImageFormat.GIF => "gif",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private string GetRepresentation(string key) => $"{_rawUrl}/{key}.{ImageFormatToString(_format)}";

    public string Large => GetRepresentation("large");
    public string Medium => GetRepresentation("medium");
    public string Small => GetRepresentation("small");
    public string Tall => GetRepresentation("tall");
    public string Thumbnail => GetRepresentation("thumb");
    public string ThumbnailSmall => GetRepresentation("thumb_small");
    public string ThumbnailTiny => GetRepresentation("thumb_tiny");
}

public enum BooruImageFormat
{
    PNG,
    JPG,
    JPEG,
    SVG,
    WebM,
    GIF
}
