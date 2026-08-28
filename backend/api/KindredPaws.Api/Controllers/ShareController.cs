using System.Net;
using System.Text.Json;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("p")]
[AllowAnonymous]
public sealed class ShareController(SocialRepository posts, AnimalRepository animals, IMinioService minioService, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ContentResult> Get(Guid id, CancellationToken ct)
    {
        var frontendBaseUrl = (configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var targetUrl = $"{frontendBaseUrl}/p/{id}";
        var post = await posts.GetPostAsync(id, ct);
        if (post is null) return Content(BuildHtml("Kindred Paws", "Esta publicación ya no está disponible.", null, targetUrl), "text/html");

        var animal = await animals.GetAsync(post.AnimalId, ct);
        var primaryMedia = post.Media.FirstOrDefault(x => x.IsPrimary) ?? post.Media.FirstOrDefault();
        var imageUrl = primaryMedia is null ? null : await minioService.GetImageUrlAsync(primaryMedia.ObjectKey, primaryMedia.ContentType, ct);
        var title = animal is null ? "Kindred Paws" : $"{animal.Name} — {animal.Shelter?.Name ?? "Kindred Paws"}";
        var description = string.IsNullOrWhiteSpace(post.Caption) ? "Ayuda a encontrarle un hogar." : post.Caption;

        return Content(BuildHtml(title, description, imageUrl, targetUrl), "text/html");
    }

    private static string BuildHtml(string title, string description, string? imageUrl, string targetUrl)
    {
        var imageTag = imageUrl is null ? string.Empty : $"""<meta property="og:image" content="{WebUtility.HtmlEncode(imageUrl)}" />""";
        return $"""
            <!doctype html>
            <html lang="es">
            <head>
            <meta charset="utf-8" />
            <title>{WebUtility.HtmlEncode(title)}</title>
            <meta property="og:type" content="article" />
            <meta property="og:title" content="{WebUtility.HtmlEncode(title)}" />
            <meta property="og:description" content="{WebUtility.HtmlEncode(description)}" />
            <meta property="og:url" content="{WebUtility.HtmlEncode(targetUrl)}" />
            {imageTag}
            <meta name="description" content="{WebUtility.HtmlEncode(description)}" />
            <meta http-equiv="refresh" content="0;url={WebUtility.HtmlEncode(targetUrl)}" />
            <script>window.location.replace({JsonSerializer.Serialize(targetUrl)});</script>
            </head>
            <body></body>
            </html>
            """;
    }
}
