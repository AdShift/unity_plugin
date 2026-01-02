using System;
using System.Collections.Generic;
using Adshift.Models;
using UnityEngine;

namespace Adshift.Platform
{
    /// <summary>
    /// Factory for creating platform-specific SDK implementations.
    /// </summary>
    public static class AdshiftPlatformFactory
    {
        private static IAdshiftPlatform _instance;

        /// <summary>
        /// Gets the platform-specific SDK implementation.
        /// Returns the same instance on subsequent calls (singleton per platform).
        /// </summary>
        /// <returns>Platform implementation of <see cref="IAdshiftPlatform"/>.</returns>
        public static IAdshiftPlatform GetPlatform()
        {
            if (_instance != null)
            {
                return _instance;
            }

#if UNITY_EDITOR
            Debug.Log("[AdShift] Running in Editor mode - using stub implementation");
            _instance = new AdshiftEditorStub();
#elif UNITY_IOS
            _instance = new AdshiftIOSBridge();
#elif UNITY_ANDROID
            _instance = new AdshiftAndroidBridge();
#else
            Debug.LogWarning("[AdShift] Unsupported platform - using stub implementation");
            _instance = new AdshiftEditorStub();
#endif

            return _instance;
        }

        /// <summary>
        /// Gets the iOS-specific platform implementation.
        /// Returns null if not running on iOS.
        /// </summary>
        /// <returns>iOS platform or null.</returns>
        public static IAdshiftIOSPlatform GetIOSPlatform()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return GetPlatform() as IAdshiftIOSPlatform;
#else
            return null;
#endif
        }

        /// <summary>
        /// Gets the Android-specific platform implementation.
        /// Returns null if not running on Android.
        /// </summary>
        /// <returns>Android platform or null.</returns>
        public static IAdshiftAndroidPlatform GetAndroidPlatform()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return GetPlatform() as IAdshiftAndroidPlatform;
#else
            return null;
#endif
        }

        /// <summary>
        /// Resets the platform instance. Useful for testing.
        /// </summary>
        internal static void Reset()
        {
            _instance = null;
        }
    }

    /// <summary>
    /// Editor/unsupported platform stub implementation.
    /// Logs method calls but doesn't perform actual SDK operations.
    /// </summary>
    internal class AdshiftEditorStub : IAdshiftPlatform, IAdshiftIOSPlatform, IAdshiftAndroidPlatform
    {
        private bool _isStarted;
        private bool _debugEnabled;
        private Action<AdshiftDeepLink> _deepLinkListener;

        // ============ IAdshiftPlatform ============

        public void Initialize(AdshiftConfig config)
        {
            Debug.Log($"[AdShift Editor] Initialize: ApiKey={config.ApiKey.Substring(0, Math.Min(config.ApiKey.Length, 8))}...");
        }

        public void Start(Action<AdshiftResult> callback)
        {
            Debug.Log("[AdShift Editor] Start");
            _isStarted = true;
            callback?.Invoke(AdshiftResult.Success());
        }

        public void Stop()
        {
            Debug.Log("[AdShift Editor] Stop");
            _isStarted = false;
        }

        public bool IsStarted()
        {
            return _isStarted;
        }

        public void SetDebugEnabled(bool enabled)
        {
            _debugEnabled = enabled;
            Debug.Log($"[AdShift Editor] SetDebugEnabled: {enabled}");
        }

        public void SetCustomerUserId(string userId)
        {
            Debug.Log($"[AdShift Editor] SetCustomerUserId: {userId}");
        }

        public void SetAppOpenDebounceMs(int milliseconds)
        {
            Debug.Log($"[AdShift Editor] SetAppOpenDebounceMs: {milliseconds}");
        }

        public void TrackEvent(string eventName, Dictionary<string, object> eventValues, Action<AdshiftResult> callback)
        {
            var valuesStr = eventValues != null ? string.Join(", ", eventValues) : "null";
            Debug.Log($"[AdShift Editor] TrackEvent: {eventName}, values={valuesStr}");
            callback?.Invoke(AdshiftResult.Success());
        }

        public void TrackPurchase(string productId, double revenue, string currency, string transactionId, Action<AdshiftResult> callback)
        {
            Debug.Log($"[AdShift Editor] TrackPurchase: productId={productId}, revenue={revenue}, currency={currency}");
            callback?.Invoke(AdshiftResult.Success());
        }

        public void SetConsentData(AdshiftConsent consent)
        {
            Debug.Log($"[AdShift Editor] SetConsentData: {consent}");
        }

        public void EnableTCFDataCollection(bool enabled)
        {
            Debug.Log($"[AdShift Editor] EnableTCFDataCollection: {enabled}");
        }

        public void RefreshConsent()
        {
            Debug.Log("[AdShift Editor] RefreshConsent");
        }

        public void SetDeepLinkListener(Action<AdshiftDeepLink> listener)
        {
            Debug.Log("[AdShift Editor] SetDeepLinkListener registered");
            _deepLinkListener = listener;
        }

        // ============ IAdshiftIOSPlatform ============

        public void SetDisableSKAN(bool disabled)
        {
            Debug.Log($"[AdShift Editor] SetDisableSKAN: {disabled}");
        }

        public void SetWaitForATTBeforeStart(bool wait)
        {
            Debug.Log($"[AdShift Editor] SetWaitForATTBeforeStart: {wait}");
        }

        public void SetAttTimeoutMs(int milliseconds)
        {
            Debug.Log($"[AdShift Editor] SetAttTimeoutMs: {milliseconds}");
        }

        public void HandleDeepLink(string url, Action<AdshiftDeepLink> callback)
        {
            Debug.Log($"[AdShift Editor] HandleDeepLink: {url}");
            var deepLink = AdshiftDeepLink.Found(url, null, false);
            callback?.Invoke(deepLink);
            _deepLinkListener?.Invoke(deepLink);
        }

        public void RequestTrackingAuthorization(Action<string> callback)
        {
            Debug.Log("[AdShift Editor] RequestTrackingAuthorization (simulated: authorized)");
            callback?.Invoke("authorized");
        }

        public string GetTrackingAuthorizationStatus()
        {
            Debug.Log("[AdShift Editor] GetTrackingAuthorizationStatus");
            return "authorized";
        }

        public string GetIDFA()
        {
            Debug.Log("[AdShift Editor] GetIDFA");
            return "00000000-0000-0000-0000-000000000000";
        }

        // ============ IAdshiftAndroidPlatform ============

        public void SetCollectOaid(bool collect)
        {
            Debug.Log($"[AdShift Editor] SetCollectOaid: {collect}");
        }

        public void SetOaidData(string oaid)
        {
            Debug.Log($"[AdShift Editor] SetOaidData: {oaid}");
        }

        public void HandleAppLinkIntent()
        {
            Debug.Log("[AdShift Editor] HandleAppLinkIntent");
        }

        public void GetGoogleAdvertisingId(Action<string> callback)
        {
            Debug.Log("[AdShift Editor] GetGoogleAdvertisingId (simulated)");
            callback?.Invoke("00000000-0000-0000-0000-000000000000");
        }
    }
}

