using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace SendGridClickTrackTLSProxy.HealthChecks;
public static class HealthCheckConfiguration
{
    public static HealthCheckOptions GetHealthCheckOptions(string? tag = null)
    {
        return new HealthCheckOptions
        {
            Predicate = tag != null ? (check) => check.Tags.Contains(tag) : null,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var response = new
                {
                    status = report.Status.ToString(),
                    service = tag ?? "all",
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        description = x.Value.Description,
                        data = x.Value.Data
                    }),
                    duration = report.TotalDuration
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));
            }
        };
    }
}