# Universal Links & App Links Setup for SendGrid Click Tracking

This proxy now includes support for serving both Apple App Site Association (AASA) files and Android Asset Links files, enabling iOS Universal Links and Android App Links to work with your SendGrid click tracking URLs.

## How It Works

### iOS Universal Links
When iOS encounters a link like `https://url7257.app.trybree.com/uni/ls/click?upn=...`, it will:
1. Look for the AASA file at `https://url7257.app.trybree.com/.well-known/apple-app-site-association`
2. Check if your app is configured to handle these paths
3. Open the app instead of the browser if everything matches

### Android App Links
When Android encounters the same link, it will:
1. Look for the Asset Links file at `https://url7257.app.trybree.com/.well-known/assetlinks.json`
2. Verify the app's signature matches
3. Open the app if verified

## Configuration Steps

### 1. Update the AASA File

Edit `apple-app-site-association.json` with your app's details:

```json
{
  "applinks": {
    "apps": [],
    "details": [
      {
        "appID": "YOUR_TEAM_ID.com.yourcompany.yourapp",
        "paths": [
          "/uni/*",    // Required for SendGrid Universal Links
          "/ls/*",     // Standard SendGrid click tracking
          "/wf/*"      // Standard SendGrid open tracking
        ]
      }
    ]
  }
}
```

Replace:
- `YOUR_TEAM_ID` with your Apple Developer Team ID
- `com.yourcompany.yourapp` with your app's bundle identifier

### 2. Update the Android Asset Links File

The `assetlinks.json` file is already configured for Bree:

```json
[
  {
    "relation": ["delegate_permission/common.handle_all_urls"],
    "target": {
      "namespace": "android_app",
      "package_name": "com.breeapp",
      "sha256_cert_fingerprints": [
        "DC:E7:29:D3:53:CF:CF:2D:16:A7:48:11:C2:ED:E5:DE:05:65:A9:01:BB:F2:39:AD:75:1B:81:F2:D5:FE:46:44"
      ]
    }
  }
]
```

### 3. Configure File Locations (Optional)

If you need to place the files in different locations, update `appsettings.json`:

```json
{
  "AppleAppSiteAssociation": {
    "FilePath": "path/to/your/apple-app-site-association.json"
  },
  "AndroidAssetLinks": {
    "FilePath": "path/to/your/assetlinks.json"
  }
}
```

### 4. Update Your Mobile Apps

#### iOS Configuration
Ensure your app's entitlements include the SendGrid click tracking domain:

```swift
// In your app's Associated Domains capability:
applinks:url7257.app.trybree.com
```

Or in your Expo config:

```typescript
associatedDomains: [
  'applinks:url7257.app.trybree.com',
  // ... other domains
]
```

#### Android Configuration
In your Android app's `AndroidManifest.xml`:

```xml
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    
    <data android:scheme="https"
          android:host="url7257.app.trybree.com"
          android:pathPrefix="/uni/" />
    <data android:pathPrefix="/ls/" />
    <data android:pathPrefix="/wf/" />
</intent-filter>
```

### 5. Deploy and Test

1. Deploy the proxy with both deep linking files
2. Verify the files are accessible:
   ```bash
   # iOS AASA
   curl https://url7257.app.trybree.com/.well-known/apple-app-site-association
   
   # Android Asset Links
   curl https://url7257.app.trybree.com/.well-known/assetlinks.json
   ```
3. Test a SendGrid link on both iOS and Android devices with your app installed

## How the Middleware Works

The `DeepLinkingMiddleware`:
- Intercepts requests to both:
  - `/.well-known/apple-app-site-association` (iOS)
  - `/.well-known/assetlinks.json` (Android)
- Serves the configured JSON files with proper content type
- Adds caching headers for performance
- Works alongside the existing SendGrid proxy functionality

## Troubleshooting

### AASA File Not Serving

Check the logs for warnings about the AASA file:
- Ensure the file exists at the configured path
- Verify the JSON is valid
- Check file permissions

### Universal Links Not Working

1. Use Apple's AASA validator: https://search.developer.apple.com/appsearch-validation-tool
2. Ensure your domain exactly matches (no www prefix if not configured)
3. Delete and reinstall the app to refresh AASA caching
4. Check that the paths in AASA match your SendGrid URL pattern

### Testing Locally

For local testing:
1. Use ngrok or similar to expose your local instance with HTTPS
2. Update your app to use the ngrok domain temporarily
3. Remember iOS caches AASA files aggressively

## Railway Deployment Notes

When deploying to Railway:
- The AASA file will be included in your Docker image
- Ensure the file is copied in your Dockerfile if using custom builds
- The middleware will serve it automatically at the correct path

## Security Considerations

- The AASA file is public by design (Apple requires it)
- Only include app IDs you control
- The file doesn't expose any sensitive information
- Paths should match only your intended URLs
