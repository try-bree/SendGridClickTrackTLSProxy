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
```

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
