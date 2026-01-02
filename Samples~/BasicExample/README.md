# AdShift SDK - Basic Example

This example demonstrates all core features of the AdShift Unity SDK.
Mirrors the iOS Sample App functionality for parity testing.

## Quick Setup

### Option 1: Add to Existing Scene (Recommended)

1. Create an empty GameObject in your scene
2. Rename it to `AdshiftDemo`
3. Add the `AdshiftDemo.cs` script component
4. Set your API Key in the Inspector
5. Play the scene!

### Option 2: Create New Scene

1. Create a new scene: `File > New Scene`
2. Create an empty GameObject: `GameObject > Create Empty`
3. Rename it to `AdshiftDemo`
4. Add the `AdshiftDemo.cs` script
5. Set your API Key
6. Save and play

## Configuration

In the Inspector, you can configure:

| Property | Description | Default |
|----------|-------------|---------|
| **Api Key** | Your AdShift API key | Required |
| **Is Debug** | Enable debug logging | `true` |
| **App Open Debounce Ms** | Debounce time for app open events | `5000` |
| **Collect Oaid** | Collect OAID on Android (China) | `false` |

## Features Demonstrated

### SDK Lifecycle
- **Initialize**: Configure and initialize the SDK
- **Start**: Begin attribution and tracking
- **Stop**: Stop SDK (for privacy compliance)

### Event Tracking (25+ Event Types)
- **Predefined Events**: PURCHASE, ADD_TO_CART, LOGIN, etc.
- **Purchase with Revenue**: Sends a purchase event with $9.99 value
- **Custom Events**: Track any custom event with optional value

### Consent (GDPR/DMA)
- **GDPR Allow**: Grant all consent permissions
- **GDPR Block**: Deny all consent permissions
- **Non-GDPR User**: Mark user as outside GDPR regions
- **Enable/Disable TCF**: Toggle TCF v2.2 data collection
- **Refresh Consent**: Manually refresh consent state

### Settings
- **Customer ID**: Set custom user identifier
- **Debounce**: Change APP_OPEN debounce interval

### iOS Specific
- **Request ATT**: Request App Tracking Transparency permission
- **ATT Status**: Check current ATT authorization status
- **Get IDFA**: Retrieve the IDFA (if available)

> Note: SKAN conversion values are managed automatically by the SDK

### Android Specific
- **Get GAID**: Retrieve Google Advertising ID

## Using the Demo

1. **Set your API Key** in the Inspector before running
2. Click **Initialize** to configure the SDK
3. Click **Start** to begin attribution
4. Test various features using the on-screen buttons
5. Watch the log area for results and callbacks

## UI Features

The demo uses Runtime IMGUI for instant visual testing:

- **Status Indicators**: 🟢/🔴 for Initialized/Started states
- **Collapsible Sections**: Toggle Consent/Events/Platform sections
- **Event Picker**: Navigate through 25+ event types with < > buttons
- **Deep Link Display**: Shows received deep links in real-time
- **Scrollable Logs**: Full history of SDK operations

## Callbacks

The demo automatically subscribes to:

```csharp
AdshiftSDK.OnDeepLinkReceived  // Deep link received (direct or deferred)
```

SDK start result is handled via callback:
```csharp
AdshiftSDK.Start(result => {
    if (result.IsSuccess) { /* SDK started */ }
    else { /* Handle error: result.ErrorMessage */ }
});
```

## Testing Deep Links

### iOS
1. Configure URL schemes in Xcode
2. Add `LSApplicationQueriesSchemes` to Info.plist
3. Test with: `yourscheme://path?param=value`

### Android
1. Configure intent filters in AndroidManifest.xml
2. Test with: `adb shell am start -a android.intent.action.VIEW -d "yourscheme://path"`

## Code Reference

See the `AdshiftDemo.cs` script for implementation details. Key patterns:

```csharp
using Adshift;
using Adshift.Models;

// Initialize with configuration
var config = new AdshiftConfig(apiKey)
{
    IsDebug = true,
    AppOpenDebounceMs = 5000
};
AdshiftSDK.Initialize(config);

// Start SDK
AdshiftSDK.Start(result => {
    Debug.Log(result.IsSuccess ? "Started!" : result.ErrorMessage);
});

// Stop SDK
AdshiftSDK.Stop();

// Track predefined events
AdshiftSDK.TrackEvent(AdshiftEventType.Purchase, new Dictionary<string, object> {
    { "product_id", "premium" },
    { "revenue", 9.99 }
});

// Track purchase with revenue (for SKAN)
AdshiftSDK.TrackPurchase("premium_subscription", 9.99, "USD", "txn_123");

// Set GDPR consent
AdshiftSDK.SetConsentData(AdshiftConsent.ForGDPRUser(
    hasConsentForDataUsage: true,
    hasConsentForAdsPersonalization: true,
    hasConsentForAdStorage: true
));

// Enable TCF data collection
AdshiftSDK.EnableTCFDataCollection(true);
AdshiftSDK.RefreshConsent();

// Set customer ID
AdshiftSDK.SetCustomerUserId("user_12345");

// iOS: Request ATT
AdshiftSDK.Instance.iOS?.RequestTrackingAuthorization(status => {
    Debug.Log($"ATT: {status}");
});

// iOS: Get IDFA
string idfa = AdshiftSDK.Instance.iOS?.GetIDFA();

// Android: Get GAID
AdshiftSDK.Instance.Android?.GetGoogleAdvertisingId(gaid => {
    Debug.Log($"GAID: {gaid}");
});

// Deep link listener
AdshiftSDK.OnDeepLinkReceived += deepLink => {
    Debug.Log($"Deep link: {deepLink.DeepLinkUrl}");
};
```

## Parity with iOS Sample App

This demo covers all features from the native iOS Sample App:

| iOS Sample Feature | Unity Demo | Status |
|-------------------|------------|--------|
| Initialize/Start/Stop | ✅ | Complete |
| 25+ Event Types | ✅ | Complete |
| Track Purchase with Revenue | ✅ | Complete |
| GDPR Allow/Block | ✅ | Complete |
| Non-GDPR User | ✅ | Complete |
| TCF Enable/Toggle | ✅ | Complete |
| Refresh Consent | ✅ | Complete |
| Customer ID | ✅ | Complete |
| Debounce Setting | ✅ | Complete |
| ATT Request | ✅ | Complete |
| ATT Status | ✅ | Complete |
| Get IDFA | ✅ | Complete |
| Deep Link Listener | ✅ | Complete |
| SKAN CV | N/A | Automatic (SDK managed) |
| SKAN Debug Panel | ❌ | Native SDK debug |

## Troubleshooting

### SDK not initializing
- Verify API key is correct
- Check console for error messages
- Ensure native SDKs are properly imported via EDM4U

### Callbacks not firing
- Make sure SDK is both initialized AND started
- Check that demo script is active in scene
- Verify network connectivity

### Platform features not available
- iOS features only work on iOS devices/simulators
- Android features only work on Android devices
- Use Unity Remote for limited testing

