using Serilog;
using SendGridClickTrackTLSProxy.Services;
using SendGridClickTrackTLSProxy.Middleware;
using SendGridClickTrackTLSProxy.HealthChecks;
using SendGridClickTrackTLSProxy.YARP;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using SendGridClickTrackTLSProxy.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureKestrelWithTls();

builder.AddSerilogLogging();

builder.Services.AddSingleton<SendGridHealthTracker>();
builder.Services.AddSingleton<SendGridHealthTrackingConfigService>();
builder.Services.AddHealthChecks()
    .AddCheck<SendGridHealthCheck>("sendgrid", tags: ["sendgrid"]);

var proxyConfig = new SendGridProxyConfiguration(builder.Configuration);
var configProvider = proxyConfig.CreateProxyConfig();

builder.Services.AddSingleton<IProxyConfigProvider>(configProvider);

builder.Services.AddReverseProxy()
    .AddTransforms(builderContext =>
    {
        // we ALWAYS pass through original host headers so client request hostname MUST match sendgrid custom domain
        builderContext.AddOriginalHost(true);
    });

var app = builder.Build();

// We throw an ERROR in the logs and return a 404 when a request host header does NOT match the SendGrid:ClickTrackingCustomDomain from appsettings.json
app.UseMiddleware<HostHeaderValidationMiddleware>(); //NOTE a port is included in host header - for when testing locally - really you have to test using ports 80 and 443 or host header match fails 

// Serve deep linking files for iOS Universal Links and Android App Links
app.UseDeepLinking();

var logRequestHeaders = builder.Configuration.GetValue<bool>("ApplicationLogging:LogRequestHeaders");
if (logRequestHeaders)
{
    app.UseMiddleware<RequestHeadersLoggingMiddleware>();
}

app.UseSerilogRequestLogging();

if (builder.Configuration.GetValue<bool>("Kestrel:Tls:RedirectHttpToHttps"))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<SendGridHealthTrackingMiddleware>();

app.MapHealthChecks("/health/sendgrid", HealthCheckConfiguration.GetHealthCheckOptions("sendgrid"));

app.MapReverseProxy();

app.Run();