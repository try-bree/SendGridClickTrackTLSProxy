using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace SendGridClickTrackTLSProxy.Middleware
{
    public class DeepLinkingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DeepLinkingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _aasaContent;
        private readonly string _assetLinksContent;

        public DeepLinkingMiddleware(
            RequestDelegate next,
            ILogger<DeepLinkingMiddleware> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _environment = environment;

            // Load AASA content from file or configuration
            var aasaPath = configuration["AppleAppSiteAssociation:FilePath"] ?? "apple-app-site-association.json";
            var assetLinksPath = configuration["AndroidAssetLinks:FilePath"] ?? "assetlinks.json";
            
            if (Path.IsPathRooted(aasaPath))
            {
                _aasaContent = LoadFileContent(aasaPath, "AASA");
            }
            else
            {
                var fullPath = Path.Combine(environment.ContentRootPath, aasaPath);
                _aasaContent = LoadFileContent(fullPath, "AASA");
            }

            if (Path.IsPathRooted(assetLinksPath))
            {
                _assetLinksContent = LoadFileContent(assetLinksPath, "AssetLinks");
            }
            else
            {
                var fullPath = Path.Combine(environment.ContentRootPath, assetLinksPath);
                _assetLinksContent = LoadFileContent(fullPath, "AssetLinks");
            }

            if (string.IsNullOrEmpty(_aasaContent))
            {
                _logger.LogWarning("Apple App Site Association file not found or empty at {Path}", aasaPath);
            }

            if (string.IsNullOrEmpty(_assetLinksContent))
            {
                _logger.LogWarning("Android Asset Links file not found or empty at {Path}", assetLinksPath);
            }
        }

        private string LoadFileContent(string path, string fileType)
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading {FileType} file from {Path}", fileType, path);
            }
            return string.Empty;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is a request for the AASA file (iOS)
            if (context.Request.Path.StartsWithSegments("/.well-known/apple-app-site-association") ||
                context.Request.Path.StartsWithSegments("/apple-app-site-association"))
            {
                if (!string.IsNullOrEmpty(_aasaContent))
                {
                    _logger.LogInformation("Serving Apple App Site Association file for host {Host}", 
                        context.Request.Host.Value);

                    context.Response.ContentType = "application/json";
                    context.Response.Headers.Add("Cache-Control", "max-age=3600"); // Cache for 1 hour
                    await context.Response.WriteAsync(_aasaContent);
                    return;
                }
                else
                {
                    _logger.LogWarning("AASA file requested but not available for host {Host}", 
                        context.Request.Host.Value);
                    context.Response.StatusCode = 404;
                    return;
                }
            }

            // Check if this is a request for the Android Asset Links file
            if (context.Request.Path.StartsWithSegments("/.well-known/assetlinks.json"))
            {
                if (!string.IsNullOrEmpty(_assetLinksContent))
                {
                    _logger.LogInformation("Serving Android Asset Links file for host {Host}", 
                        context.Request.Host.Value);

                    context.Response.ContentType = "application/json";
                    context.Response.Headers.Add("Cache-Control", "max-age=3600"); // Cache for 1 hour
                    await context.Response.WriteAsync(_assetLinksContent);
                    return;
                }
                else
                {
                    _logger.LogWarning("Asset Links file requested but not available for host {Host}", 
                        context.Request.Host.Value);
                    context.Response.StatusCode = 404;
                    return;
                }
            }

            await _next(context);
        }
    }

    public static class DeepLinkingMiddlewareExtensions
    {
        public static IApplicationBuilder UseDeepLinking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DeepLinkingMiddleware>();
        }
    }
}
