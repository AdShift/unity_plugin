using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Adshift;
using Adshift.Models;

namespace Adshift.Demo
{
    /// <summary>
    /// Demo script showcasing all AdShift SDK features.
    /// Mirrors iOS Sample App functionality for parity testing.
    /// </summary>
    public class AdshiftDemo : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool isDebug = true;
        [SerializeField] private int appOpenDebounceMs = 5000;
        
        [Header("Android Settings")]
        [SerializeField] private bool collectOaid = false;
        
        // Platform-specific API keys - replace with your keys from AdShift Dashboard
        private string ApiKey
        {
            get
            {
#if UNITY_IOS
                return "YOUR_IOS_API_KEY";
#elif UNITY_ANDROID
                return "YOUR_ANDROID_API_KEY";
#else
                return "YOUR_API_KEY_HERE";
#endif
            }
        }
        
        [Header("UI References (Optional - leave empty for runtime UI)")]
        [SerializeField] private Text logText;
        [SerializeField] private ScrollRect logScrollRect;
        
        // Runtime UI state
        private bool useRuntimeUI = false;
        private List<string> logs = new List<string>();
        private Vector2 scrollPosition;
        
        // Custom event input
        private string customEventName = "test_event";
        private string customEventValue = "10.5";
        
        // Customer ID input
        private string customerIdInput = "user_12345";
        
        // Debounce input
        private string debounceInput = "0";
        
        // SDK state
        private bool sdkInitialized = false;
        private bool sdkStarted = false;
        private bool tcfEnabled = false;
        
        // Event picker
        private int selectedEventIndex = 0;
        private readonly string[] eventTypeNames = {
            "PURCHASE", "ADD_TO_CART", "ADD_TO_WISHLIST", "ADD_PAYMENT_INFO",
            "INITIATED_CHECKOUT", "CONTENT_VIEW", "LIST_VIEW", "SEARCH",
            "COMPLETE_REGISTRATION", "LOGIN", "TUTORIAL_COMPLETION", 
            "SUBSCRIBE", "START_TRIAL", "LEVEL_ACHIEVED", "ACHIEVEMENT_UNLOCKED",
            "SPENT_CREDIT", "RATE", "SHARE", "INVITE", "RE_ENGAGE", "UPDATE",
            "OPENED_FROM_PUSH", "TRAVEL_BOOKING", "AD_CLICK", "AD_VIEW"
        };
        private readonly string[] eventTypeValues = {
            AdshiftEventType.Purchase, AdshiftEventType.AddToCart, 
            AdshiftEventType.AddToWishList, AdshiftEventType.AddPaymentInfo,
            AdshiftEventType.InitiatedCheckout, AdshiftEventType.ContentView,
            AdshiftEventType.ListView, AdshiftEventType.Search,
            AdshiftEventType.CompleteRegistration, AdshiftEventType.Login,
            AdshiftEventType.TutorialCompletion, AdshiftEventType.Subscribe,
            AdshiftEventType.StartTrial, AdshiftEventType.LevelAchieved,
            AdshiftEventType.AchievementUnlocked, AdshiftEventType.SpentCredit,
            AdshiftEventType.Rate, AdshiftEventType.Share, AdshiftEventType.Invite,
            AdshiftEventType.ReEngage, AdshiftEventType.Update,
            AdshiftEventType.OpenedFromPushNotification, AdshiftEventType.TravelBooking,
            AdshiftEventType.AdClick, AdshiftEventType.AdView
        };
        
        // Deep link info
        private AdshiftDeepLink lastDeepLink;
        private string deepLinkText = "No deep link received";
        
        // Consent snapshot display
        private string consentSnapshotText = "Consent: not refreshed yet";
        
        // UI sections visibility
        private bool showConsentSection = true;
        private bool showEventsSection = true;
        private bool showPlatformSection = true;
        
        private void Awake()
        {
            useRuntimeUI = (logText == null);
            
            Log("AdShift Demo initialized");
            Log($"Platform: {Application.platform}");
            Log($"API Key: {(string.IsNullOrEmpty(ApiKey) ? "NOT SET" : ApiKey.Substring(0, Mathf.Min(8, ApiKey.Length)) + "...")}");
        }
        
        private void Start()
        {
            // Subscribe to SDK callbacks
            AdshiftSDK.OnDeepLinkReceived += OnDeepLinkReceived;
            
            Log("Callbacks registered. Ready to initialize SDK.");
        }
        
        private void OnDestroy()
        {
            AdshiftSDK.OnDeepLinkReceived -= OnDeepLinkReceived;
        }
        
        #region SDK Callbacks
        
        private void OnStartCallback(AdshiftResult result)
        {
            if (result.IsSuccess)
            {
                sdkStarted = true;
                Log($"✅ SDK Started Successfully!");
            }
            else
            {
                Log($"❌ SDK Start Failed: {result.ErrorMessage} (code: {result.ErrorCode})");
            }
        }
        
        private void OnDeepLinkReceived(AdshiftDeepLink deepLink)
        {
            lastDeepLink = deepLink;
            string linkType = deepLink.IsDeferred ? "Deferred" : "Direct";
            
            deepLinkText = $"🔗 {linkType} Deep Link!\n";
            deepLinkText += $"URL: {deepLink.DeepLinkUrl ?? "N/A"}\n";
            
            if (deepLink.Params != null && deepLink.Params.Count > 0)
            {
                foreach (var kvp in deepLink.Params)
                {
                    deepLinkText += $"  {kvp.Key}: {kvp.Value}\n";
                }
            }
            
            Log($"🔗 Deep Link Received: {deepLink.DeepLinkUrl}");
        }
        
        #endregion
        
        #region SDK Actions
        
        private void InitializeSDK()
        {
            if (string.IsNullOrEmpty(ApiKey) || ApiKey == "YOUR_API_KEY_HERE")
            {
                Log("⚠️ Please set your API Key first!");
                return;
            }
            
            Log("Initializing SDK...");
            
            var config = new AdshiftConfig(ApiKey)
            {
                IsDebug = isDebug,
                AppOpenDebounceMs = appOpenDebounceMs,
                CollectOaid = collectOaid
            };
            
            AdshiftSDK.Initialize(config);
            sdkInitialized = true;
            
            Log($"SDK Initialized:");
            Log($"   Debug: {config.IsDebug}");
            Log($"   AppOpenDebounce: {config.AppOpenDebounceMs}ms");
        }
        
        private void StartSDK()
        {
            if (!sdkInitialized)
            {
                Log("⚠️ Initialize SDK first!");
                return;
            }
            
            Log("Starting SDK...");
            AdshiftSDK.Start(OnStartCallback);
        }
        
        private void StopSDK()
        {
            if (!sdkStarted)
            {
                Log("⚠️ SDK not running");
                return;
            }
            
            Log("Stopping SDK...");
            AdshiftSDK.Stop();
            sdkStarted = false;
            Log("🛑 SDK Stopped");
        }
        
        private void TrackSelectedEvent()
        {
            if (!sdkStarted)
            {
                Log("⚠️ Start SDK first!");
                return;
            }
            
            string eventType = eventTypeValues[selectedEventIndex];
            string eventName = eventTypeNames[selectedEventIndex];
            
            Log($"Tracking {eventName}...");
            AdshiftSDK.TrackEvent(eventType, new Dictionary<string, object>
            {
                { "timestamp", System.DateTimeOffset.Now.ToUnixTimeSeconds() }
            }, result =>
            {
                if (result.IsSuccess)
                    Log($"✓ {eventName} sent");
                else
                    Log($"✗ {eventName} failed: {result.ErrorMessage}");
            });
        }
        
        private void TrackPurchaseWithRevenue()
        {
            if (!sdkStarted)
            {
                Log("⚠️ Start SDK first!");
                return;
            }
            
            Log("Tracking PURCHASE with revenue...");
            AdshiftSDK.TrackPurchase(
                productId: "premium_subscription",
                revenue: 9.99,
                currency: "USD",
                transactionId: $"txn_{System.DateTimeOffset.Now.ToUnixTimeMilliseconds()}",
                callback: result =>
                {
                    if (result.IsSuccess)
                        Log("✓ Purchase (9.99 USD) sent");
                    else
                        Log($"✗ Purchase failed: {result.ErrorMessage}");
                }
            );
        }
        
        private void TrackCustomEvent()
        {
            if (!sdkStarted)
            {
                Log("⚠️ Start SDK first!");
                return;
            }
            
            Dictionary<string, object> values = null;
            if (float.TryParse(customEventValue, out float parsedValue))
            {
                values = new Dictionary<string, object> { { "value", parsedValue } };
            }
            
            Log($"Tracking custom: '{customEventName}'...");
            AdshiftSDK.TrackEvent(customEventName, values);
            Log("✓ Custom event sent");
        }
        
        private void SetCustomerId()
        {
            if (!sdkInitialized)
            {
                Log("⚠️ Initialize SDK first!");
                return;
            }
            
            if (string.IsNullOrEmpty(customerIdInput))
            {
                Log("⚠️ Enter customer ID!");
                return;
            }
            
            AdshiftSDK.SetCustomerUserId(customerIdInput);
            Log($"✓ Customer ID set: {customerIdInput}");
        }
        
        private void SetDebounce()
        {
            if (!sdkInitialized)
            {
                Log("⚠️ Initialize SDK first!");
                return;
            }
            
            if (int.TryParse(debounceInput, out int ms))
            {
                AdshiftSDK.SetAppOpenDebounceMs(ms);
                Log($"✓ Debounce set: {ms}ms");
            }
            else
            {
                Log("⚠️ Invalid debounce value!");
            }
        }
        
        private void SetGDPRConsent(bool hasConsent)
        {
            if (!sdkInitialized)
            {
                Log("⚠️ Initialize SDK first!");
                return;
            }
            
            Log($"Setting GDPR consent: {hasConsent}...");
            var consent = AdshiftConsent.ForGDPRUser(
                hasConsentForDataUsage: hasConsent,
                hasConsentForAdsPersonalization: hasConsent,
                hasConsentForAdStorage: hasConsent
            );
            AdshiftSDK.SetConsentData(consent);
            Log($"✓ GDPR consent: {(hasConsent ? "ALLOW all" : "BLOCK all")}");
            RefreshConsentSnapshot();
        }
        
        private void SetNonGDPRUser()
        {
            if (!sdkInitialized)
            {
                Log("⚠️ Initialize SDK first!");
                return;
            }
            
            Log("Setting as non-GDPR user...");
            AdshiftSDK.SetConsentData(AdshiftConsent.ForNonGDPRUser());
            Log("✓ Set as non-GDPR user");
            RefreshConsentSnapshot();
        }
        
        private void ToggleTCF()
        {
            if (!sdkInitialized)
            {
                Log("⚠️ Initialize SDK first!");
                return;
            }
            
            tcfEnabled = !tcfEnabled;
            AdshiftSDK.EnableTCFDataCollection(tcfEnabled);
            Log($"✓ TCF data collection: {(tcfEnabled ? "ENABLED" : "DISABLED")}");
            
            if (tcfEnabled)
            {
                RefreshConsentSnapshot();
            }
        }
        
        private void RefreshConsentSnapshot()
        {
            if (!sdkInitialized)
            {
                Log("⚠️ Initialize SDK first!");
                return;
            }
            
            AdshiftSDK.RefreshConsent();
            consentSnapshotText = $"TCF: {(tcfEnabled ? "ON" : "OFF")}\nConsent refreshed at {System.DateTime.Now:HH:mm:ss}";
            Log("✓ Consent refreshed");
        }
        
        private void RequestATT()
        {
#if UNITY_IOS
            Log("Requesting ATT permission...");
            AdshiftSDK.Instance.iOS?.RequestTrackingAuthorization((status) =>
            {
                Log($"ATT Status: {status}");
            });
#else
            Log("⚠️ ATT is iOS only");
#endif
        }
        
        private void CheckATTStatus()
        {
#if UNITY_IOS
            var status = AdshiftSDK.Instance.iOS?.GetTrackingAuthorizationStatus();
            Log($"Current ATT Status: {status ?? "unknown"}");
#else
            Log("⚠️ ATT is iOS only");
#endif
        }
        
        private void GetIDFA()
        {
#if UNITY_IOS
            var idfa = AdshiftSDK.Instance.iOS?.GetIDFA();
            Log($"IDFA: {idfa ?? "Not available"}");
#else
            Log("⚠️ IDFA is iOS only");
#endif
        }
        
        private void GetGAID()
        {
#if UNITY_ANDROID
            Log("Getting GAID...");
            AdshiftSDK.Instance.Android?.GetGoogleAdvertisingId((gaid) =>
            {
                Log($"GAID: {gaid ?? "Not available"}");
            });
#else
            Log("⚠️ GAID is Android only");
#endif
        }
        
        
        private void ClearLogs()
        {
            logs.Clear();
            if (logText != null) logText.text = "";
        }
        
        #endregion
        
        #region Logging
        
        private void Log(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            
            logs.Add(logEntry);
            Debug.Log($"[AdshiftDemo] {message}");
            
            if (logs.Count > 100) logs.RemoveAt(0);
            
            if (logText != null)
            {
                logText.text = string.Join("\n", logs);
                if (logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
        
        #endregion
        
        #region Runtime IMGUI
        
        private void OnGUI()
        {
            if (!useRuntimeUI) return;
            
            float scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
            scale = Mathf.Clamp(scale, 1f, 3f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            
            float scaledWidth = Screen.width / scale;
            float scaledHeight = Screen.height / scale;
            
            // Styles
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            GUIStyle smallHeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            GUIStyle logStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true };
            
            float btnW = 100;
            float btnH = 28;
            float pad = 8;
            float x = pad;
            float y = pad;
            
            // === TITLE ===
            GUI.Label(new Rect(x, y, 300, 25), "🎯 AdShift SDK Demo", headerStyle);
            y += 28;
            
            // Status
            string initIcon = sdkInitialized ? "🟢" : "🔴";
            string startIcon = sdkStarted ? "🟢" : "🟠";
            GUI.Label(new Rect(x, y, 300, 18), $"{initIcon} Init  {startIcon} Started  TCF: {(tcfEnabled ? "ON" : "OFF")}", logStyle);
            y += 22;
            
            // === SDK LIFECYCLE ===
            GUI.Label(new Rect(x, y, 200, 20), "SDK Lifecycle", smallHeaderStyle);
            y += 20;
            
            if (GUI.Button(new Rect(x, y, btnW, btnH), "Initialize", buttonStyle)) InitializeSDK();
            if (GUI.Button(new Rect(x + btnW + pad, y, btnW, btnH), "Start", buttonStyle)) StartSDK();
            if (GUI.Button(new Rect(x + (btnW + pad) * 2, y, btnW, btnH), "Stop", buttonStyle)) StopSDK();
            y += btnH + pad;
            
            // === CONSENT (collapsible) ===
            showConsentSection = GUI.Toggle(new Rect(x, y, 200, 20), showConsentSection, " 📋 Consent (GDPR/TCF)", smallHeaderStyle);
            y += 22;
            
            if (showConsentSection)
            {
                if (GUI.Button(new Rect(x, y, btnW, btnH), "GDPR Allow", buttonStyle)) SetGDPRConsent(true);
                if (GUI.Button(new Rect(x + btnW + pad, y, btnW, btnH), "GDPR Block", buttonStyle)) SetGDPRConsent(false);
                if (GUI.Button(new Rect(x + (btnW + pad) * 2, y, btnW, btnH), "Non-GDPR", buttonStyle)) SetNonGDPRUser();
                y += btnH + pad;
                
                string tcfLabel = tcfEnabled ? "✓ TCF ON" : "TCF OFF";
                if (GUI.Button(new Rect(x, y, btnW, btnH), tcfLabel, buttonStyle)) ToggleTCF();
                if (GUI.Button(new Rect(x + btnW + pad, y, btnW, btnH), "Refresh", buttonStyle)) RefreshConsentSnapshot();
                y += btnH + pad;
                
                GUI.Label(new Rect(x, y, scaledWidth - pad * 2, 30), consentSnapshotText, logStyle);
                y += 32;
            }
            
            // === EVENTS (collapsible) ===
            showEventsSection = GUI.Toggle(new Rect(x, y, 200, 20), showEventsSection, " 📊 Events", smallHeaderStyle);
            y += 22;
            
            if (showEventsSection)
            {
                // Event picker row
                GUI.Label(new Rect(x, y, 50, btnH), "Event:");
                selectedEventIndex = Mathf.Clamp(selectedEventIndex, 0, eventTypeNames.Length - 1);
                
                // Simple prev/next buttons instead of dropdown
                if (GUI.Button(new Rect(x + 50, y, 25, btnH), "<", buttonStyle))
                    selectedEventIndex = (selectedEventIndex - 1 + eventTypeNames.Length) % eventTypeNames.Length;
                
                GUI.Label(new Rect(x + 78, y, 120, btnH), eventTypeNames[selectedEventIndex]);
                
                if (GUI.Button(new Rect(x + 200, y, 25, btnH), ">", buttonStyle))
                    selectedEventIndex = (selectedEventIndex + 1) % eventTypeNames.Length;
                
                if (GUI.Button(new Rect(x + 230, y, 70, btnH), "Track", buttonStyle)) TrackSelectedEvent();
                y += btnH + pad;
                
                // Purchase with revenue
                if (GUI.Button(new Rect(x, y, 150, btnH), "Purchase ($9.99)", buttonStyle)) TrackPurchaseWithRevenue();
                y += btnH + pad;
                
                // Custom event row
                GUI.Label(new Rect(x, y, 50, 20), "Custom:");
                customEventName = GUI.TextField(new Rect(x + 55, y, 80, 20), customEventName);
                GUI.Label(new Rect(x + 140, y, 35, 20), "Val:");
                customEventValue = GUI.TextField(new Rect(x + 175, y, 40, 20), customEventValue);
                if (GUI.Button(new Rect(x + 220, y, 60, 22), "Send", buttonStyle)) TrackCustomEvent();
                y += 26;
            }
            
            // === CUSTOMER ID & DEBOUNCE ===
            GUI.Label(new Rect(x, y, 200, 20), "📝 Settings", smallHeaderStyle);
            y += 20;
            
            GUI.Label(new Rect(x, y, 70, 20), "Customer:");
            customerIdInput = GUI.TextField(new Rect(x + 70, y, 100, 20), customerIdInput);
            if (GUI.Button(new Rect(x + 175, y, 50, 22), "Set", buttonStyle)) SetCustomerId();
            y += 26;
            
            GUI.Label(new Rect(x, y, 70, 20), "Debounce:");
            debounceInput = GUI.TextField(new Rect(x + 70, y, 50, 20), debounceInput);
            GUI.Label(new Rect(x + 125, y, 25, 20), "ms");
            if (GUI.Button(new Rect(x + 155, y, 50, 22), "Set", buttonStyle)) SetDebounce();
            y += 28;
            
            // === PLATFORM SPECIFIC (collapsible) ===
            showPlatformSection = GUI.Toggle(new Rect(x, y, 200, 20), showPlatformSection, " 📱 Platform Features", smallHeaderStyle);
            y += 22;
            
            if (showPlatformSection)
            {
#if UNITY_IOS
                if (GUI.Button(new Rect(x, y, btnW, btnH), "Request ATT", buttonStyle)) RequestATT();
                if (GUI.Button(new Rect(x + btnW + pad, y, btnW, btnH), "ATT Status", buttonStyle)) CheckATTStatus();
                y += btnH + pad;
                
                if (GUI.Button(new Rect(x, y, btnW, btnH), "Get IDFA", buttonStyle)) GetIDFA();
                y += btnH + pad;
#elif UNITY_ANDROID
                if (GUI.Button(new Rect(x, y, btnW, btnH), "Get GAID", buttonStyle)) GetGAID();
                y += btnH + pad;
#else
                GUI.Label(new Rect(x, y, 300, 20), "(Platform features on iOS/Android only)", logStyle);
                y += 24;
#endif
            }
            
            // === DEEP LINK STATUS ===
            GUI.Label(new Rect(x, y, 200, 20), "🔗 Deep Link", smallHeaderStyle);
            y += 20;
            GUI.Box(new Rect(x, y, scaledWidth - pad * 2, 45), "");
            GUI.Label(new Rect(x + 5, y + 3, scaledWidth - pad * 2 - 10, 40), deepLinkText, logStyle);
            y += 50;
            
            // === LOGS ===
            float logY = y;
            float logH = scaledHeight - logY - 40;
            
            GUI.Label(new Rect(x, logY - 2, 50, 18), "📜 Logs");
            if (GUI.Button(new Rect(scaledWidth - 60 - pad, logY - 5, 60, 22), "Clear", buttonStyle)) ClearLogs();
            logY += 18;
            
            GUI.Box(new Rect(x, logY, scaledWidth - pad * 2, logH), "");
            
            string allLogs = string.Join("\n", logs);
            float contentH = logStyle.CalcHeight(new GUIContent(allLogs), scaledWidth - pad * 4);
            contentH = Mathf.Max(contentH, logH - 10);
            
            scrollPosition = GUI.BeginScrollView(
                new Rect(x + 3, logY + 3, scaledWidth - pad * 2 - 6, logH - 6),
                scrollPosition,
                new Rect(0, 0, scaledWidth - pad * 4 - 20, contentH)
            );
            GUI.Label(new Rect(0, 0, scaledWidth - pad * 4 - 20, contentH), allLogs, logStyle);
            GUI.EndScrollView();
        }
        
        #endregion
    }
}
