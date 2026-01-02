using System;
using System.Collections.Generic;
using Adshift.Models;

namespace Adshift.Platform
{
    /// <summary>
    /// Base platform abstraction interface for AdShift SDK.
    /// Defines all methods common to both iOS and Android platforms.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by platform-specific bridges:
    /// - <see cref="AdshiftIOSBridge"/> for iOS (using P/Invoke)
    /// - <see cref="AdshiftAndroidBridge"/> for Android (using JNI)
    /// 
    /// In Editor mode, a stub implementation is used that logs calls
    /// but doesn't perform actual SDK operations.
    /// </remarks>
    public interface IAdshiftPlatform
    {
        // ============ Lifecycle ============

        /// <summary>
        /// Initializes the SDK with the provided configuration.
        /// Must be called before <see cref="Start"/>.
        /// </summary>
        /// <param name="config">SDK configuration including API key and options.</param>
        void Initialize(AdshiftConfig config);

        /// <summary>
        /// Starts the SDK and begins tracking.
        /// </summary>
        /// <param name="callback">Optional callback with start result.</param>
        void Start(Action<AdshiftResult> callback);

        /// <summary>
        /// Stops the SDK. Can be restarted with <see cref="Start"/>.
        /// </summary>
        void Stop();

        /// <summary>
        /// Checks if the SDK is currently started.
        /// </summary>
        /// <returns>True if SDK is running, false otherwise.</returns>
        bool IsStarted();

        // ============ Configuration ============

        /// <summary>
        /// Enables or disables debug logging.
        /// </summary>
        /// <param name="enabled">True to enable verbose logs.</param>
        void SetDebugEnabled(bool enabled);

        /// <summary>
        /// Sets a custom user identifier for cross-referencing.
        /// </summary>
        /// <param name="userId">Your internal user ID.</param>
        void SetCustomerUserId(string userId);

        /// <summary>
        /// Sets the debounce interval for automatic APP_OPEN events.
        /// </summary>
        /// <param name="milliseconds">Minimum time between APP_OPEN events in ms.</param>
        void SetAppOpenDebounceMs(int milliseconds);

        // ============ Event Tracking ============

        /// <summary>
        /// Tracks an in-app event with optional parameters.
        /// </summary>
        /// <param name="eventName">Event name (use <see cref="AdshiftEventType"/> constants).</param>
        /// <param name="eventValues">Optional event parameters.</param>
        /// <param name="callback">Optional callback with tracking result.</param>
        void TrackEvent(string eventName, Dictionary<string, object> eventValues, Action<AdshiftResult> callback);

        /// <summary>
        /// Tracks a purchase event.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="revenue">Purchase amount.</param>
        /// <param name="currency">ISO 4217 currency code (e.g., "USD").</param>
        /// <param name="transactionId">Transaction ID or receipt token.</param>
        /// <param name="callback">Optional callback with tracking result.</param>
        void TrackPurchase(
            string productId,
            double revenue,
            string currency,
            string transactionId,
            Action<AdshiftResult> callback);

        // ============ Consent (GDPR/DMA) ============

        /// <summary>
        /// Sets user consent data for GDPR/DMA compliance.
        /// </summary>
        /// <param name="consent">Consent flags.</param>
        void SetConsentData(AdshiftConsent consent);

        /// <summary>
        /// Enables or disables automatic TCF v2.2 data collection from CMPs.
        /// Call before <see cref="Start"/>.
        /// </summary>
        /// <param name="enabled">True to enable TCF collection.</param>
        void EnableTCFDataCollection(bool enabled);

        /// <summary>
        /// Refreshes consent state from TCF or manual sources.
        /// </summary>
        void RefreshConsent();

        // ============ Deep Links ============

        /// <summary>
        /// Registers a listener for deep link events.
        /// </summary>
        /// <param name="listener">Callback invoked when deep links are received.</param>
        void SetDeepLinkListener(Action<AdshiftDeepLink> listener);
    }
}

