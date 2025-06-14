# SendGrid Click Tracking TLS Proxy

SendGrid custom domain click tracking TLS proxy using YARP and .NET 9. Includes logging and health check monitoring for production use.

## Overview

This application solves a critical limitation with SendGrid's click tracking feature: the lack of HTTPS/TLS support for custom domains. It provides a secure, production-ready proxy that enables HTTPS click tracking while maintaining full compatibility with SendGrid's service.

## The Problem

SendGrid's click tracking feature has several limitations:

- **No HTTPS support**: Click tracking links only work over HTTP, not secure HTTPS
- **SendGrid domain only**: By default, links use `sendgrid.net` domain
- **Custom domain limitations**: While SendGrid allows custom domains, they don't provide TLS certificates for them

### Why This Matters

Modern web security standards and corporate networks often:
- Automatically redirect HTTP to HTTPS
- Block or warn about insecure HTTP connections
- Require all traffic to be encrypted

This means SendGrid's HTTP-only click tracking often fails in production environments.

## The Solution

SendGrid Click Tracking TLS Proxy acts as a secure intermediary:

1. **Receives requests** to your custom domain over HTTPS
2. **Validates and processes** the click tracking data
3. **Forwards requests** to SendGrid's infrastructure
4. **Returns responses** securely to the user

## Key Features

- **🔒 Full TLS/HTTPS support** for custom domains
- **🔄 HTTP to HTTPS upgrade** for legacy links
- **📊 Production-ready logging** with structured output
- **❤️ Health check monitoring** for proactive alerting
- **⚡ High performance** using YARP (Yet Another Reverse Proxy)
- **🏢 Enterprise ready** with .NET 9 foundation

## How It Works

```
User clicks link → Your Custom Domain (HTTPS) → Proxy → SendGrid → Target URL
```

The proxy seamlessly handles the TLS termination while preserving all SendGrid click tracking functionality.

## Prerequisites

⚠️ **Important**: Before deploying this proxy, ensure your SendGrid custom domain click tracking is fully configured and working over HTTP. Test all functionality before proceeding with HTTPS setup.

## Configuration

### Required Settings

**`SendGrid:ClickTrackingCustomDomain`**
- Must exactly match your SendGrid custom domain configuration
- Only requests matching this domain will be proxied to SendGrid
- Non-matching requests are logged as errors

**`SendGrid:ClickTrackingPathsToMatch`**
- Array of root paths that must match for forwarding to SendGrid
- Default paths are typically `["ls", "wf"]` but may vary

### Configuration Example

```json
{
  "SendGrid": {
    "ClickTrackingCustomDomain": "clickme.mydomain.com",
    "ClickTrackingPathsToMatch": ["ls", "wf", "newpath"]
  }
}
```

With this configuration:

✅ **These requests will be proxied:**
- `https://clickme.mydomain.com/ls/click?upn=u001.abc123`
- `http://clickme.mydomain.com/wf/open?upn=u001.abc123` (upgraded to HTTPS)
- `https://clickme.mydomain.com/newpath/click?upn=u001.abc123`

❌ **These requests will NOT be proxied:**
- `https://someother.mydomain.com/ls/click?upn=u001.abc123` (wrong domain)
- `https://clickme.mydomain.com/randompath/click?upn=u001.abc123` (wrong path)
- `https://sub.clickme.mydomain.com/ls/click?upn=u001.abc123` (subdomain)

## Health Checks

Health endpoint: `https://[your-domain]/health/sendgrid`

✅ **Working**: `https://clickme.mydomain.com/health/sendgrid`  
❌ **Not working**: `https://someother.com/health/sendgrid` or `https://clickme.mydomain.com/healthcheck/sendgrid`

## Deployment Options

### Option 1: Cloud Hosted (Recommended)
Deploy to Azure App Service, AWS App Runner, or similar platforms where TLS is managed at the platform level.

- **TLS Handling**: Managed by hosting platform
- **Kestrel Config**: Default HTTP configuration
- **Backend Requests**: HTTP to SendGrid (always)

### Option 2: Direct Deployment
Deploy directly to VMs or containers where you manage TLS certificates.

- **TLS Handling**: Configure Kestrel with your certificates
- **Kestrel Config**: HTTPS configuration required
- **Backend Requests**: HTTP to SendGrid (always)

> **Note**: Backend requests to SendGrid are always HTTP because the custom domain host header won't match SendGrid's TLS certificate.

## Getting Started

** THE BELOW NEEDS MORE CLARIFICATION **

** ALSO NEED TO WORK ON BUILD ARTIFACTS **

### For Azure App Service

1. **Download the latest release** from GitHub releases
2. **Extract and deploy** to your App Service
3. **Configure custom domain** and TLS certificate in Azure
4. **Update application settings** with your SendGrid configuration
5. **Test health endpoint** to verify deployment

### For Self-Hosted Deployment

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/SendGridClickTrackTLSProxy.git
   ```

2. **Configure settings** in `appsettings.json`

3. **Build and run**:
   ```bash
   dotnet build
   dotnet run
   ```

## Monitoring and Troubleshooting

- **Health Checks**: Monitor `/health/sendgrid` endpoint
- **Structured Logging**: All requests and errors are logged with context
- **Configuration Validation**: Invalid domains/paths are clearly logged
- **Performance Metrics**: YARP provides built-in performance monitoring

---

*This proxy is essential for organizations requiring secure, professional email communications with reliable click tracking.*