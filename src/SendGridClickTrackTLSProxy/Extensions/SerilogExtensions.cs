using Serilog;

namespace SendGridClickTrackTLSProxy.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            var appName = context.Configuration["ApplicationLogging:AppName"] ?? "SendGridClickTrackTLSProxy";
            var appEnvironment = context.Configuration["ApplicationLogging:AppEnvironment"] ?? "Development";
            var releaseVersion = context.Configuration["ApplicationLogging:ReleaseVersion"] ?? "0.0.0";

            configuration
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("AppName", appName)
                .Enrich.WithProperty("AppEnvironment", appEnvironment)
                .Enrich.WithProperty("ReleaseVersion", releaseVersion)
                .WriteTo.Console();

            // Optional Seq logging
            var seqUrl = context.Configuration["ApplicationLogging:Seq:Url"];
            var seqApiKey = context.Configuration["ApplicationLogging:Seq:ApiKey"];
            if (!string.IsNullOrEmpty(seqUrl))
            {
                configuration.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
            }

            // Optional Datadog logging
            var datadogEnabled = context.Configuration.GetValue<bool>("Datadog:Enabled", true);
            var datadogApiKey = Environment.GetEnvironmentVariable("DD_API_KEY");
            
            if (datadogEnabled && !string.IsNullOrEmpty(datadogApiKey))
            {
                try
                {
                    var datadogEnvironment = Environment.GetEnvironmentVariable("DD_ENV") ?? appEnvironment;
                    var datadogSite = context.Configuration["Datadog:Site"] ?? Environment.GetEnvironmentVariable("DD_SITE") ?? "datadoghq.com";
                    var datadogService = context.Configuration["Datadog:ServiceName"] ?? "sendgrid-click-track-proxy";
                    var datadogSource = context.Configuration["Datadog:Source"] ?? "csharp";
                    var datadogHost = context.Configuration["Datadog:HostName"] ?? "railway-production";

                    var datadogConfiguration = new Serilog.Sinks.Datadog.Logs.DatadogConfiguration(
                        url: $"https://http-intake.logs.{datadogSite}",
                        port: 443,
                        useSSL: true,
                        useTCP: false);

                    configuration.WriteTo.DatadogLogs(
                        apiKey: datadogApiKey,
                        source: datadogSource,
                        service: datadogService,
                        host: datadogHost,
                        tags: new[] { $"env:{datadogEnvironment}", $"version:{releaseVersion}" },
                        configuration: datadogConfiguration);
                }
                catch (Exception ex)
                {
                    // Log Datadog configuration error to console but don't crash the app
                    Console.WriteLine($"ERROR: Failed to configure Datadog logging: {ex.Message}");
                    Console.WriteLine("Application will continue with console logging only.");
                }
            }

            // Optional file logging
            var fileLoggingSection = context.Configuration.GetSection("ApplicationLogging:FileLogging");
            var enableFileLogging = fileLoggingSection.GetValue<bool>("ApplicationLogging:Enabled");

            if (enableFileLogging)
            {
                var configPath = fileLoggingSection["ApplicationLogging:Path"] ?? "logs/application-.log";

                // Convert to absolute path if it's relative
                var logPath = Path.IsPathRooted(configPath)
                    ? configPath
                    : Path.Combine(AppContext.BaseDirectory, configPath);

                var retainedFileCountLimit = fileLoggingSection.GetValue<int?>("ApplicationLogging:RetainedFileCountLimit") ?? 31;
                var fileSizeLimitBytes = fileLoggingSection.GetValue<long?>("ApplicationLogging:FileSizeLimitBytes") ?? 100_000_000; // 100MB default
                var rollOnFileSizeLimit = fileLoggingSection.GetValue("ApplicationLogging:RollOnFileSizeLimit", true);

                // Ensure directory exists
                var logDirectory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                configuration.WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedFileCountLimit,
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    rollOnFileSizeLimit: rollOnFileSizeLimit,
                    shared: true);
            }
        });

        return builder;
    }
}