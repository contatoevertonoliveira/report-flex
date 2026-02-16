using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;

public static class ImagesStatic
{
    public static void MapLegacyImages(WebApplication app)
    {
        var path = @"D:\backup_08_2025\Sistemas\CSHARP\report-flex\Report_Flex_C\images";
        var provider = new PhysicalFileProvider(path);
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = provider,
            RequestPath = "/images-legacy",
            ContentTypeProvider = contentTypeProvider
        });
    }
}
