using System;
using System.Collections.Generic;
using Adshift.Models;
using Adshift.Platform;
using Adshift.Utils;
using UnityEngine;

namespace Adshift
{
    /// <summary>
    /// Main entry point for the AdShift SDK.
    /// Use this class to initialize, configure, and interact with the SDK.
    /// </summary>
    /// <remarks>
    /// ## Quick Start
    /// 
    /// ```csharp
    /// // 1. Initialize SDK
    /// AdshiftSDK.Initialize(new AdshiftConfig(
    ///     apiKey: "your-api-key",
    ///     isDebug: true
    /// ));
    /// 
    /// // 2. (Optional) Configure iOS-specific features
    /// AdshiftSDK.Instance.iOS?.SetWaitForATTBeforeStart(true);
    /// 
    /// // 3. Start tracking
    /// AdshiftSDK.Start(result => {
    ///     if (result.IsSuccess) Debug.Log("AdShift started!");
    /// });
    /// 
    /// // 4. Track events
    /// AdshiftSDK.TrackEvent(AdshiftEventType.AddToCart, new Dictionary&lt;string, object&gt; {
    ///     { "product_id", "SKU123" },
    ///     { "price", 29.99 }
    /// });
    /// ```
    /// </remarks>
    public sealed class AdshiftSDK
    {
        #region Singleton

        private static readonly Lazy<AdshiftSDK> _lazyInstance = new Lazy<AdshiftSDK>(() => new AdshiftSDK());

        /// <summary>
        /// Singleton instance of the SDK.
        /// Use this for platform-specific features (iOS, Android properties).
        /// </summary>
        public static AdshiftSDK Instance => _lazyInstance.Value;

        private AdshiftSDK()
        {
            _platform = AdshiftPlatformFactory.GetPlatform();
            _ios = new AdshiftIOSFeatures(AdshiftPlatformFactory.GetIOSPlatform());
            _android = new AdshiftAndroidFeatures(AdshiftPlatformFactory.GetAndroidPlatform());
        }

        #endregion

        #region Private Fields

        private readonly IAdshiftPlatform _platform;
        private readonly AdshiftIOSFeatures _ios;
        private readonly AdshiftAndroidFeatures _android;
        private bool _isInitialized;

        #endregion

        #region Platform-Specific Access

        /// <summary>
        /// iOS-specific SDK features (SKAN, ATT).
        /// Returns null on non-iOS platforms.
        /// </summary>
        /// <example>
        /// <code>
        /// // Safe null-conditional access
        /// AdshiftSDK.Instance.iOS?.SetDisableSKAN(false);
        /// AdshiftSDK.Instance.iOS?.SetWaitForATTBeforeStart(true);
        /// </code>
        /// </example>
        public AdshiftIOSFeatures iOS => _ios.IsAvailable ? _ios : null;

        /// <summary>
        /// Android-specific SDK features (OAID).
        /// Returns null on non-Android platforms.
        /// </summary>
        /// <example>
        /// <code>
        /// // Safe null-conditional access
        /// AdshiftSDK.Instance.Android?.SetCollectOaid(true);
        /// </code>
        /// </example>
        public AdshiftAndroidFeatures Android => _android.IsAvailable ? _android : null;

        #endregion

        #region Events

        private event Action<AdshiftDeepLink> _onDeepLinkReceived;

        /// <summary>
        /// Event fired when a deep link is received (direct or deferred).
        /// </summary>
        /// <example>
        /// <code>
        /// AdshiftSDK.OnDeepLinkReceived += deepLink => {
        ///     if (deepLink.Status == AdshiftDeepLinkStatus.Found)
        ///     {
        ///         Debug.Log($"Deep link: {deepLink.DeepLinkUrl}");
        ///         // Route user based on deep link parameters
        ///     }
        /// };
        /// </code>
        /// </example>
        public static event Action<AdshiftDeepLink> OnDeepLinkReceived
        {
            add
            {
                Instance._onDeepLinkReceived += value;
                Instance.EnsureDeepLinkListenerRegistered();
            }
            remove => Instance._onDeepLinkReceived -= value;
        }

        private bool _deepLinkListenerRegistered;

        private void EnsureDeepLinkListenerRegistered()
        {
            if (_deepLinkListenerRegistered) return;
            
            _platform.SetDeepLinkListener(deepLink =>
            {
                _onDeepLinkReceived?.Invoke(deepLink);
            });
            _deepLinkListenerRegistered = true;
        }

        #endregion

        #region Static Lifecycle Methods

        /// <summary>
        /// Initializes the SDK with the provided configuration.
        /// Must be called before any other SDK methods.
        /// </summary>
        /// <param name="config">SDK configuration including API key.</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        /// <example>
        /// <code>
        /// AdshiftSDK.Initialize(new AdshiftConfig(
        ///     apiKey: "your-api-key",
        ///     isDebug: kDebug,
        ///     appOpenDebounceMs: 30000  // 30 seconds
        /// ));
        /// </code>
        /// </example>
        public static void Initialize(AdshiftConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Instance._Initialize(config);
        }

        /// <summary>
        /// Starts the SDK and begins tracking.
        /// Call after <see cref="Initialize"/> and any pre-start configuration.
        /// </summary>
        /// <param name="callback">Optional callback invoked when start completes.</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <example>
        /// <code>
        /// // With callback
        /// AdshiftSDK.Start(result => {
        ///     if (result.IsSuccess)
        ///         Debug.Log("SDK started successfully!");
        ///     else
        ///         Debug.LogError($"SDK start failed: {result.ErrorMessage}");
        /// });
        /// 
        /// // Without callback (fire and forget)
        /// AdshiftSDK.Start();
        /// </code>
        /// </example>
        public static void Start(Action<AdshiftResult> callback = null)
        {
            Instance.EnsureInitialized();
            Instance._Start(callback);
        }

        /// <summary>
        /// Stops the SDK. All tracking stops immediately.
        /// Can be restarted with <see cref="Start"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <remarks>
        /// Use this for legal/privacy compliance when user opts out.
        /// </remarks>
        public static void Stop()
        {
            Instance.EnsureInitialized();
            Instance._platform.Stop();
        }

        /// <summary>
        /// Checks if the SDK is currently started and tracking.
        /// </summary>
        /// <returns>True if SDK is running, false otherwise.</returns>
        public static bool IsStarted()
        {
            return Instance._isInitialized && Instance._platform.IsStarted();
        }

        #endregion

        #region Static Configuration Methods

        /// <summary>
        /// Enables or disables debug logging.
        /// </summary>
        /// <param name="enabled">True to enable verbose logs.</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <example>
        /// <code>
        /// #if DEBUG
        /// AdshiftSDK.SetDebugEnabled(true);
        /// #endif
        /// </code>
        /// </example>
        public static void SetDebugEnabled(bool enabled)
        {
            Instance.EnsureInitialized();
            Instance._platform.SetDebugEnabled(enabled);
        }

        /// <summary>
        /// Sets a custom user identifier.
        /// Use this to associate events with your own user IDs.
        /// </summary>
        /// <param name="userId">Your internal user ID.</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown if userId is null or empty.</exception>
        /// <example>
        /// <code>
        /// // Set after user logs in
        /// AdshiftSDK.SetCustomerUserId("user_12345");
        /// </code>
        /// </example>
        public static void SetCustomerUserId(string userId)
        {
            Instance.EnsureInitialized();
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            Instance._platform.SetCustomerUserId(userId);
        }

        /// <summary>
        /// Sets the minimum interval between automatic APP_OPEN events.
        /// </summary>
        /// <param name="milliseconds">Debounce interval in milliseconds. Default: 10000 (10s).</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <example>
        /// <code>
        /// // Send APP_OPEN max once every 30 seconds
        /// AdshiftSDK.SetAppOpenDebounceMs(30000);
        /// </code>
        /// </example>
        public static void SetAppOpenDebounceMs(int milliseconds)
        {
            Instance.EnsureInitialized();
            Instance._platform.SetAppOpenDebounceMs(milliseconds);
        }

        #endregion

        #region Static Event Tracking Methods

        /// <summary>
        /// Tracks an in-app event.
        /// </summary>
        /// <param name="eventName">Event name. Use <see cref="AdshiftEventType"/> constants or custom strings.</param>
        /// <param name="eventValues">Optional event parameters (strings, numbers, booleans).</param>
        /// <param name="callback">Optional callback invoked when tracking completes.</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown if eventName is null or empty.</exception>
        /// <example>
        /// <code>
        /// // Predefined event with parameters
        /// AdshiftSDK.TrackEvent(
        ///     AdshiftEventType.AddToCart,
        ///     new Dictionary&lt;string, object&gt; {
        ///         { "product_id", "SKU123" },
        ///         { "price", 29.99 },
        ///         { "quantity", 1 }
        ///     }
        /// );
        /// 
        /// // Custom event without parameters
        /// AdshiftSDK.TrackEvent("tutorial_step_3");
        /// 
        /// // With completion callback
        /// AdshiftSDK.TrackEvent("level_start", values, result => {
        ///     if (result.IsFailure) Debug.LogWarning($"Event failed: {result.ErrorMessage}");
        /// });
        /// </code>
        /// </example>
        public static void TrackEvent(
            string eventName,
            Dictionary<string, object> eventValues = null,
            Action<AdshiftResult> callback = null)
        {
            Instance.EnsureInitialized();
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentNullException(nameof(eventName));

            Instance._platform.TrackEvent(eventName, eventValues, callback);
        }

        /// <summary>
        /// Tracks a purchase event with revenue information.
        /// Use this for revenue attribution and SKAdNetwork conversion values.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="revenue">Purchase amount (actual revenue).</param>
        /// <param name="currency">ISO 4217 currency code (e.g., "USD", "EUR").</param>
        /// <param name="transactionId">Transaction ID or purchase token.</param>
        /// <param name="callback">Optional callback invoked when tracking completes.</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown if revenue is negative.</exception>
        /// <example>
        /// <code>
        /// AdshiftSDK.TrackPurchase(
        ///     productId: "premium_subscription",
        ///     revenue: 9.99,
        ///     currency: "USD",
        ///     transactionId: "TXN_12345"
        /// );
        /// </code>
        /// </example>
        public static void TrackPurchase(
            string productId,
            double revenue,
            string currency,
            string transactionId,
            Action<AdshiftResult> callback = null)
        {
            Instance.EnsureInitialized();

            if (string.IsNullOrEmpty(productId))
                throw new ArgumentNullException(nameof(productId));
            if (string.IsNullOrEmpty(currency))
                throw new ArgumentNullException(nameof(currency));
            if (string.IsNullOrEmpty(transactionId))
                throw new ArgumentNullException(nameof(transactionId));
            if (revenue < 0)
                throw new ArgumentException("Revenue cannot be negative", nameof(revenue));

            Instance._platform.TrackPurchase(productId, revenue, currency, transactionId, callback);
        }

        #endregion

        #region Static Consent Methods

        /// <summary>
        /// Sets user consent data for GDPR/DMA compliance.
        /// </summary>
        /// <param name="consent">Consent configuration.</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown if consent is null.</exception>
        /// <example>
        /// <code>
        /// // For GDPR users who granted all consent
        /// AdshiftSDK.SetConsentData(AdshiftConsent.ForGDPRUser(
        ///     hasConsentForDataUsage: true,
        ///     hasConsentForAdsPersonalization: true,
        ///     hasConsentForAdStorage: true
        /// ));
        /// 
        /// // For non-GDPR users
        /// AdshiftSDK.SetConsentData(AdshiftConsent.ForNonGDPRUser());
        /// </code>
        /// </example>
        public static void SetConsentData(AdshiftConsent consent)
        {
            Instance.EnsureInitialized();
            if (consent == null)
                throw new ArgumentNullException(nameof(consent));

            Instance._platform.SetConsentData(consent);
        }

        /// <summary>
        /// Enables or disables automatic TCF v2.2 data collection.
        /// When enabled, SDK reads consent strings from SharedPreferences/NSUserDefaults
        /// (typically set by CMPs like Google Funding Choices).
        /// </summary>
        /// <param name="enabled">True to enable TCF collection.</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        /// <remarks>
        /// Call this BEFORE <see cref="Start"/> for it to take effect.
        /// </remarks>
        /// <example>
        /// <code>
        /// AdshiftSDK.Initialize(config);
        /// AdshiftSDK.EnableTCFDataCollection(true);  // Before Start!
        /// AdshiftSDK.Start();
        /// </code>
        /// </example>
        public static void EnableTCFDataCollection(bool enabled)
        {
            Instance.EnsureInitialized();
            Instance._platform.EnableTCFDataCollection(enabled);
        }

        /// <summary>
        /// Refreshes consent state from CMP or manual settings.
        /// Call this after CMP dialog closes to pick up updated consent.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if SDK not initialized.</exception>
        public static void RefreshConsent()
        {
            Instance.EnsureInitialized();
            Instance._platform.RefreshConsent();
        }

        #endregion

        #region Private Instance Methods

        private void _Initialize(AdshiftConfig config)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[AdShift] SDK already initialized. Ignoring duplicate Initialize() call.");
                return;
            }

            // Ensure callback handler exists
            var _ = AdshiftCallbackHandler.Instance;

            _platform.Initialize(config);
            _isInitialized = true;

            Debug.Log($"[AdShift] SDK initialized (v{GetVersion()})");
        }

        private void _Start(Action<AdshiftResult> callback)
        {
            _platform.Start(callback);
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    "AdshiftSDK not initialized. Call AdshiftSDK.Initialize(config) first.");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the SDK version.
        /// </summary>
        /// <returns>SDK version string.</returns>
        public static string GetVersion()
        {
            return "1.0.0";
        }

        /// <summary>
        /// Checks if the SDK has been initialized.
        /// </summary>
        /// <returns>True if initialized, false otherwise.</returns>
        public static bool IsInitialized()
        {
            return Instance._isInitialized;
        }

        #endregion
    }
}

