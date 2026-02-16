using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;

public static class ImagesStatic
{
    public static void MapLegacyImages(WebApplication app)
    {
        var candidates = new[]
        {
            Path.Combine(app.Environment.ContentRootPath, "..", "Report_Flex_C", "images"),
            @"D:\backup_08_2025\Sistemas\CSHARP\report-flex\Report_Flex_C\images"
        };
        string? existing = null;
        foreach (var c in candidates)
        {
            if (Directory.Exists(c))
            {
                existing = c;
                break;
            }
        }
        if (existing != null)
        {
            var provider = new PhysicalFileProvider(existing);
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = provider,
                RequestPath = "/images-legacy",
                ContentTypeProvider = contentTypeProvider
            });
        }
    }
}
