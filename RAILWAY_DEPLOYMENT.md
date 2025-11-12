# Railway Deployment Guide for SendGrid Click Track TLS Proxy

Railway now uses Railpack for deployments, which doesn't natively support C#/.NET applications. This guide provides multiple approaches to deploy this application on Railway.

## Deployment Options

### Option 1: Shell Script Deployment (Recommended)

Railway will automatically detect and use the `start.sh` script that's been created in the root directory. This script will:

1. Install .NET SDK if needed
2. Build the application
3. Run the application on the PORT provided by Railway

**To deploy:**
1. Push your code to GitHub
2. Connect your GitHub repository to Railway
3. Railway should automatically detect the `start.sh` script and deploy

### Option 2: Docker Deployment

If Railway detects the Dockerfile, it will use it for a more efficient containerized deployment.

**The Dockerfile approach:**
- Uses multi-stage builds for smaller images
- Pre-compiles the application for faster startup
- Uses the official .NET runtime image

### Option 3: Explicit Railway Configuration

The `railway.json` file explicitly tells Railway to use the Dockerfile builder.

## Environment Variables

Set these in your Railway project settings:

```bash
# Required by Railway (automatically set)
PORT=<assigned by Railway>

# Application-specific (optional)
ASPNETCORE_ENVIRONMENT=Production
ApplicationLogging__AppEnvironment=Production
ApplicationLogging__ReleaseVersion=1.0.0

# SendGrid configuration (if needed)
SendGrid__ClickTrackingCustomDomain=your-domain.com

# Datadog Logging (required for centralized logging and monitoring)
DD_API_KEY=<your-datadog-api-key>
DD_ENV=production  # or staging, development, etc.

# Datadog Logging (optional)
DD_SITE=datadoghq.com  # Only set if using Datadog EU: datadoghq.eu
```

### Datadog Configuration Details

The application integrates with Datadog for centralized logging, enabling monitoring and alerting for this critical service.

**Required Environment Variables:**
- `DD_API_KEY` - Your Datadog API key for log intake (obtain from Datadog Organization Settings → API Keys)
- `DD_ENV` - Environment identifier (e.g., `production`, `staging`) used to filter logs in Datadog

**Optional Environment Variables:**
- `DD_SITE` - Datadog site URL. Defaults to `datadoghq.com`. Set to `datadoghq.eu` if using Datadog EU region.

**How it works:**
- All application logs are automatically forwarded to Datadog via Serilog
- Logs are tagged with `service:sendgrid-click-track-proxy`, `env:<DD_ENV>`, `host:railway-production`, and `version:<release-version>`
- If `DD_API_KEY` is not set, Datadog logging is gracefully disabled and logs only go to console
- If Datadog configuration fails during startup, the app continues with console logging only (fail-safe)
- Service name and host name are configured in appsettings.json and can be overridden via configuration

**Setting up monitoring:**
After logs are flowing to Datadog, create monitors for:
1. Error rate threshold alerts
2. Service availability (no logs = service down)
3. Configure alerts to route to Rootly and notify #incidents, #alerts-urgent-bree

## Port Configuration

The application is configured to listen on the PORT environment variable provided by Railway. The start scripts automatically handle this configuration.

## Troubleshooting

### If Railpack fails to detect the build method:

1. Ensure `start.sh` has execute permissions:
   ```bash
   chmod +x start.sh
   ```

2. Try setting the RAILPACK_SHELL_SCRIPT environment variable:
   ```bash
   RAILPACK_SHELL_SCRIPT=start.sh
   ```

3. Check Railway logs for specific error messages

### If the application fails to start:

1. Check that the PORT environment variable is being used correctly
2. Verify all required dependencies are installed
3. Check application logs in Railway dashboard

## Alternative: Force Docker Build

If shell script deployment isn't working, you can force Docker deployment by:

1. Removing `start.sh` temporarily
2. Ensuring `Dockerfile` exists in the root
3. Railway should automatically detect and use Docker

## Notes

- The shell script approach may have longer cold starts as it builds on each deployment
- Docker approach is more efficient but requires Docker support in Railway
- Both approaches handle the PORT environment variable automatically
- SSL/TLS is handled by Railway's proxy layer, so the app runs on HTTP internally
