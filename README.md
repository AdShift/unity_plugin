# AdShift Unity Plugin

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com/)
[![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android-green.svg)](https://github.com/AdShift/unity_plugin)
[![License](https://img.shields.io/badge/license-Proprietary-red.svg)](LICENSE)

Official AdShift SDK plugin for Unity. Enable mobile attribution, in-app event tracking, SKAdNetwork 4.0+ integration, deep linking, and GDPR/TCF 2.2 compliance in your Unity games.

---

## Features

- ✅ **Install Attribution** — Accurate install tracking across platforms
- ✅ **In-App Event Tracking** — Track user actions and conversions
- ✅ **SKAdNetwork 4.0+** — Full support for iOS privacy-preserving attribution
- ✅ **Deep Linking** — Direct and deferred deep link support
- ✅ **GDPR/DMA Compliance** — Manual consent and TCF 2.2 support
- ✅ **Offline Mode** — Events are cached and sent when connectivity returns
- ✅ **Cross-Platform** — Single API for iOS and Android

---

## Requirements

| Platform | Minimum Version |
|----------|-----------------|
| **Unity** | 2022.3 LTS+ |
| **iOS** | 15.0+ |
| **Android** | API 21+ (Android 5.0) |

### Dependencies

This plugin uses **External Dependency Manager for Unity (EDM4U)** to automatically resolve native SDK dependencies.

**EDM4U is included with:**
- Google Mobile Ads Unity Plugin
- Firebase Unity SDK
- Or install separately: [EDM4U GitHub](https://github.com/googlesamples/unity-jar-resolver)

---

## Installation

### Option 1: Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/AdShift/unity_plugin.git`
4. Click **Add**

### Option 2: Manual Installation

1. Download from [Releases](https://github.com/AdShift/unity_plugin/releases)
2. Import the `.unitypackage` into your project

---

## Quick Start

```csharp
using Adshift;
using Adshift.Models;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // 1. Initialize SDK
        var config = new AdshiftConfig("your-api-key")
        {
            IsDebug = true,
            AppOpenDebounceMs = 10000
        };
        AdshiftSDK.Initialize(config);

        // 2. Start tracking
        AdshiftSDK.Start(result =>
        {
            if (result.IsSuccess)
                Debug.Log("AdShift started!");
            else
                Debug.LogError($"Start failed: {result.ErrorMessage}");
        });

        // 3. Track events
        AdshiftSDK.TrackEvent(AdshiftEventType.LevelAchieved, new Dictionary<string, object>
        {
            { "level", 5 },
            { "score", 1000 }
        });

        // 4. Listen for deep links
        AdshiftSDK.OnDeepLinkReceived += deepLink =>
        {
            Debug.Log($"Deep link: {deepLink.DeepLinkUrl}");
        };
    }
}
```

---

## API Reference

### Lifecycle

```csharp
// Initialize SDK with configuration
AdshiftSDK.Initialize(AdshiftConfig config);

// Start tracking (with optional callback)
AdshiftSDK.Start(Action<AdshiftResult> callback = null);

// Stop tracking
AdshiftSDK.Stop();

// Check if SDK is started
bool isStarted = AdshiftSDK.IsStarted();
```

### Configuration

```csharp
// Create config with API key (required)
var config = new AdshiftConfig("your-api-key")
{
    IsDebug = true,                    // Enable debug logs
    AppOpenDebounceMs = 10000,         // Debounce for APP_OPEN events
    
    // iOS only
    DisableSKAN = false,               // Disable SKAdNetwork
    WaitForATTBeforeStart = true,      // Wait for ATT before install
    AttTimeoutMs = 30000,              // ATT timeout (5s-120s)
    
    // Android only
    CollectOaid = true                 // Collect OAID (China)
};

// Set customer ID (after initialize)
AdshiftSDK.SetCustomerUserId("user_12345");

// Change debounce at runtime
AdshiftSDK.SetAppOpenDebounceMs(30000);
```

### Event Tracking

```csharp
// Track predefined event
AdshiftSDK.TrackEvent(AdshiftEventType.Purchase, new Dictionary<string, object>
{
    { "product_id", "premium" },
    { "price", 9.99 }
});

// Track custom event
AdshiftSDK.TrackEvent("custom_event_name");

// Track purchase with revenue (for SKAN attribution)
AdshiftSDK.TrackPurchase(
    productId: "premium_subscription",
    revenue: 9.99,
    currency: "USD",
    transactionId: "txn_123"
);
```

### Consent (GDPR/DMA)

```csharp
// GDPR user - grant all consent
AdshiftSDK.SetConsentData(AdshiftConsent.ForGDPRUser(
    hasConsentForDataUsage: true,
    hasConsentForAdsPersonalization: true,
    hasConsentForAdStorage: true
));

// GDPR user - deny consent
AdshiftSDK.SetConsentData(AdshiftConsent.ForGDPRUser(false, false, false));

// Non-GDPR user
AdshiftSDK.SetConsentData(AdshiftConsent.ForNonGDPRUser());

// Enable TCF 2.2 auto-collection (call before Start)
AdshiftSDK.EnableTCFDataCollection(true);

// Refresh consent state (after CMP dialog)
AdshiftSDK.RefreshConsent();
```

### Deep Links

```csharp
// Listen for deep links (direct and deferred)
AdshiftSDK.OnDeepLinkReceived += deepLink =>
{
    Debug.Log($"URL: {deepLink.DeepLinkUrl}");
    Debug.Log($"Is Deferred: {deepLink.IsDeferred}");
    
    // Access parameters
    if (deepLink.Parameters != null)
    {
        foreach (var param in deepLink.Parameters)
            Debug.Log($"{param.Key}: {param.Value}");
    }
};
```

---

## Platform-Specific Features

### iOS

```csharp
// Request ATT permission
AdshiftSDK.Instance.iOS?.RequestTrackingAuthorization(status =>
{
    Debug.Log($"ATT Status: {status}"); // authorized, denied, restricted, not_determined
});

// Check ATT status (returns null if unknown)
string? status = AdshiftSDK.Instance.iOS?.GetTrackingAuthorizationStatus();
if (status != null) Debug.Log($"ATT: {status}");

// Get IDFA (returns null if not available)
string? idfa = AdshiftSDK.Instance.iOS?.GetIDFA();
if (idfa != null) Debug.Log($"IDFA: {idfa}");

// Handle deep link manually
AdshiftSDK.Instance.iOS?.HandleDeepLink(url, deepLink =>
{
    Debug.Log($"Deep link resolved: {deepLink.DeepLinkUrl}");
});
```

**Required Info.plist entries:**
- `NSUserTrackingUsageDescription` - ATT dialog message
- `NSAdvertisingAttributionReportEndpoint` - For SKAN (set to your endpoint)

### Android

```csharp
// Get Google Advertising ID
AdshiftSDK.Instance.Android?.GetGoogleAdvertisingId(gaid =>
{
    Debug.Log($"GAID: {gaid}");
});
```

**Permissions (auto-added):**
- `android.permission.INTERNET`
- `android.permission.ACCESS_NETWORK_STATE`
- `com.google.android.gms.permission.AD_ID`

---

## Example

Import the example scene from Package Manager:

1. **Window → Package Manager**
2. Find **AdShift SDK**
3. Expand **Samples**
4. Click **Import** next to "Basic Example"

The example includes UI buttons to test all SDK features.

---

## Support

- 📖 **Documentation:** https://dev.adshift.com/docs/unity-sdk
- 🐛 **Issues:** https://github.com/AdShift/unity_plugin/issues
- 📧 **Email:** support@adshift.com

---

## License

Copyright © 2025 AdShift. All rights reserved.
See [LICENSE](LICENSE) for details.
