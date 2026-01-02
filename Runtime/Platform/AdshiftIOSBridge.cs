#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Adshift.Models;
using Adshift.Utils;
using UnityEngine;

namespace Adshift.Platform
{
    /// <summary>
    /// iOS platform implementation using P/Invoke (DllImport) to call native Objective-C code.
    /// </summary>
    /// <remarks>
    /// Native methods are implemented in Plugins/iOS/AdshiftUnityBridge.mm
    /// Callbacks from native use UnitySendMessage to a GameObject.
    /// </remarks>
    public class AdshiftIOSBridge : IAdshiftIOSPlatform
    {
        private const string CALLBACK_OBJECT_NAME = "AdshiftCallbackHandler";
        
        private bool _isInitialized;

        // ============ Native Method Declarations ============
        // These will be implemented in Plugins/iOS/AdshiftUnityBridge.mm

        [DllImport("__Internal")]
        private static extern void _adshift_initialize(string configJson);

        [DllImport("__Internal")]
        private static extern void _adshift_start(string callbackObjectName);

        [DllImport("__Internal")]
        private static extern void _adshift_stop();

        [DllImport("__Internal")]
        private static extern bool _adshift_isStarted();

        [DllImport("__Internal")]
        private static extern void _adshift_setDebugEnabled(bool enabled);

        [DllImport("__Internal")]
        private static extern void _adshift_setCustomerUserId(string userId);

        [DllImport("__Internal")]
        private static extern void _adshift_setAppOpenDebounceMs(int milliseconds);

        [DllImport("__Internal")]
        private static extern void _adshift_trackEvent(string eventName, string eventValuesJson, string callbackObjectName);

        [DllImport("__Internal")]
        private static extern void _adshift_trackPurchase(string productId, double revenue, string currency, string transactionId, string callbackObjectName);

        [DllImport("__Internal")]
        private static extern void _adshift_setConsentData(string consentJson);

        [DllImport("__Internal")]
        private static extern void _adshift_enableTCFDataCollection(bool enabled);

        [DllImport("__Internal")]
        private static extern void _adshift_refreshConsent();

        [DllImport("__Internal")]
        private static extern void _adshift_setDeepLinkListener(string callbackObjectName);

        [DllImport("__Internal")]
        private static extern void _adshift_setDisableSKAN(bool disabled);

        [DllImport("__Internal")]
        private static extern void _adshift_setWaitForATTBeforeStart(bool wait);

        [DllImport("__Internal")]
        private static extern void _adshift_setAttTimeoutMs(int milliseconds);

        [DllImport("__Internal")]
        private static extern void _adshift_handleDeepLink(string url, string callbackObjectName);

        [DllImport("__Internal")]
        private static extern void _adshift_requestTrackingAuthorization(string callbackObjectName);

        [DllImport("__Internal")]
        private static extern int _adshift_getTrackingAuthorizationStatus();

        [DllImport("__Internal")]
        private static extern string _adshift_getIDFA();

        // ============ IAdshiftPlatform Implementation ============

        public void Initialize(AdshiftConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[AdShift iOS] Initialize called with null config");
                return;
            }

            string configJson = DictionaryToJson(config.ToDictionary());
            _adshift_initialize(configJson);
            _isInitialized = true;
        }

        public void Start(Action<AdshiftResult> callback)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AdShift iOS] Start called before Initialize");
                callback?.Invoke(AdshiftResult.Failure("SDK not initialized"));
                return;
            }

            AdshiftCallbackHandler.Instance.SetStartCallback(callback);
            _adshift_start(CALLBACK_OBJECT_NAME);
        }

        public void Stop()
        {
            _adshift_stop();
        }

        public bool IsStarted()
        {
            return _adshift_isStarted();
        }

        public void SetDebugEnabled(bool enabled)
        {
            _adshift_setDebugEnabled(enabled);
        }

        public void SetCustomerUserId(string userId)
        {
            _adshift_setCustomerUserId(userId ?? "");
        }

        public void SetAppOpenDebounceMs(int milliseconds)
        {
            _adshift_setAppOpenDebounceMs(milliseconds);
        }

        public void TrackEvent(string eventName, Dictionary<string, object> eventValues, Action<AdshiftResult> callback)
        {
            AdshiftCallbackHandler.Instance.SetEventCallback(callback);
            string valuesJson = eventValues != null ? DictionaryToJson(eventValues) : "{}";
            _adshift_trackEvent(eventName, valuesJson, CALLBACK_OBJECT_NAME);
        }

        public void TrackPurchase(string productId, double revenue, string currency, string transactionId, Action<AdshiftResult> callback)
        {
            AdshiftCallbackHandler.Instance.SetEventCallback(callback);
            _adshift_trackPurchase(productId, revenue, currency, transactionId, CALLBACK_OBJECT_NAME);
        }

        public void SetConsentData(AdshiftConsent consent)
        {
            string consentJson = DictionaryToJson(consent.ToDictionary());
            _adshift_setConsentData(consentJson);
        }

        public void EnableTCFDataCollection(bool enabled)
        {
            _adshift_enableTCFDataCollection(enabled);
        }

        public void RefreshConsent()
        {
            _adshift_refreshConsent();
        }

        public void SetDeepLinkListener(Action<AdshiftDeepLink> listener)
        {
            AdshiftCallbackHandler.Instance.SetDeepLinkListener(listener);
            _adshift_setDeepLinkListener(CALLBACK_OBJECT_NAME);
        }

        // ============ IAdshiftIOSPlatform Implementation ============

        public void SetDisableSKAN(bool disabled)
        {
            _adshift_setDisableSKAN(disabled);
        }

        public void SetWaitForATTBeforeStart(bool wait)
        {
            _adshift_setWaitForATTBeforeStart(wait);
        }

        public void SetAttTimeoutMs(int milliseconds)
        {
            _adshift_setAttTimeoutMs(milliseconds);
        }

        public void HandleDeepLink(string url, Action<AdshiftDeepLink> callback)
        {
            AdshiftCallbackHandler.Instance.SetHandleDeepLinkCallback(callback);
            _adshift_handleDeepLink(url, CALLBACK_OBJECT_NAME);
        }

        public void RequestTrackingAuthorization(Action<string> callback)
        {
            AdshiftCallbackHandler.Instance.SetATTCallback(callback);
            _adshift_requestTrackingAuthorization(CALLBACK_OBJECT_NAME);
        }

        public string? GetTrackingAuthorizationStatus()
        {
            int status = _adshift_getTrackingAuthorizationStatus();
            return status switch
            {
                0 => "not_determined",
                1 => "restricted",
                2 => "denied",
                3 => "authorized",
                _ => null
            };
        }

        public string? GetIDFA()
        {
            string idfa = _adshift_getIDFA();
            return string.IsNullOrEmpty(idfa) ? null : idfa;
        }

        // ============ Helpers ============

        private static string DictionaryToJson(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0) return "{}";
            
            // Simple JSON serialization (Unity's JsonUtility doesn't support Dictionary)
            var parts = new List<string>();
            foreach (var kvp in dict)
            {
                string value;
                if (kvp.Value == null)
                {
                    value = "null";
                }
                else if (kvp.Value is string s)
                {
                    value = $"\"{EscapeJson(s)}\"";
                }
                else if (kvp.Value is bool b)
                {
                    value = b ? "true" : "false";
                }
                else if (kvp.Value is int || kvp.Value is long || kvp.Value is float || kvp.Value is double)
                {
                    value = kvp.Value.ToString();
                }
                else
                {
                    value = $"\"{EscapeJson(kvp.Value.ToString())}\"";
                }
                parts.Add($"\"{EscapeJson(kvp.Key)}\":{value}");
            }
            return "{" + string.Join(",", parts) + "}";
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
#endif

