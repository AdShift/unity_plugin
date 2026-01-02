#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using Adshift.Models;
using Adshift.Utils;
using UnityEngine;

namespace Adshift.Platform
{
    /// <summary>
    /// Android platform implementation using JNI (AndroidJavaClass) to call native Kotlin/Java code.
    /// </summary>
    /// <remarks>
    /// Native methods are called on the AdShiftLib singleton class from the Android SDK.
    /// Callbacks from native use UnitySendMessage to a GameObject.
    /// </remarks>
    public class AdshiftAndroidBridge : IAdshiftAndroidPlatform
    {
        private const string ANDROID_WRAPPER_CLASS = "com.adshift.unity.AdshiftUnityBridge";
        private const string CALLBACK_OBJECT_NAME = "AdshiftCallbackHandler";

        private static AndroidJavaClass _adshiftBridge;
        private static AndroidJavaObject _currentActivity;

        private bool _isInitialized;

        // ============ Static Initialization ============

        private static AndroidJavaClass GetBridge()
        {
            if (_adshiftBridge == null)
            {
                try
                {
                    _adshiftBridge = new AndroidJavaClass(ANDROID_WRAPPER_CLASS);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AdShift Android] Failed to get bridge class: {e.Message}");
                }
            }
            return _adshiftBridge;
        }

        private static AndroidJavaObject GetCurrentActivity()
        {
            if (_currentActivity == null)
            {
                try
                {
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        _currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AdShift Android] Failed to get current activity: {e.Message}");
                }
            }
            return _currentActivity;
        }

        // ============ IAdshiftPlatform Implementation ============

        public void Initialize(AdshiftConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[AdShift Android] Initialize called with null config");
                return;
            }

            var bridge = GetBridge();
            var activity = GetCurrentActivity();
            
            if (bridge == null || activity == null)
            {
                Debug.LogError("[AdShift Android] Bridge or Activity not available");
                return;
            }

            try
            {
                string configJson = DictionaryToJson(config.ToDictionary());
                bridge.CallStatic("initialize", activity, configJson);
                _isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] Initialize failed: {e.Message}");
            }
        }

        public void Start(Action<AdshiftResult> callback)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AdShift Android] Start called before Initialize");
                callback?.Invoke(AdshiftResult.Failure("SDK not initialized"));
                return;
            }

            AdshiftCallbackHandler.Instance.SetStartCallback(callback);

            try
            {
                GetBridge()?.CallStatic("start", CALLBACK_OBJECT_NAME);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] Start failed: {e.Message}");
                callback?.Invoke(AdshiftResult.Failure(e.Message));
            }
        }

        public void Stop()
        {
            try
            {
                GetBridge()?.CallStatic("stop");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] Stop failed: {e.Message}");
            }
        }

        public bool IsStarted()
        {
            try
            {
                return GetBridge()?.CallStatic<bool>("isStarted") ?? false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] IsStarted failed: {e.Message}");
                return false;
            }
        }

        public void SetDebugEnabled(bool enabled)
        {
            try
            {
                GetBridge()?.CallStatic("setDebugEnabled", enabled);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] SetDebugEnabled failed: {e.Message}");
            }
        }

        public void SetCustomerUserId(string userId)
        {
            try
            {
                GetBridge()?.CallStatic("setCustomerUserId", userId ?? "");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] SetCustomerUserId failed: {e.Message}");
            }
        }

        public void SetAppOpenDebounceMs(int milliseconds)
        {
            try
            {
                GetBridge()?.CallStatic("setAppOpenDebounceMs", milliseconds);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] SetAppOpenDebounceMs failed: {e.Message}");
            }
        }

        public void TrackEvent(string eventName, Dictionary<string, object> eventValues, Action<AdshiftResult> callback)
        {
            AdshiftCallbackHandler.Instance.SetEventCallback(callback);

            try
            {
                string valuesJson = eventValues != null ? DictionaryToJson(eventValues) : "{}";
                GetBridge()?.CallStatic("trackEvent", eventName, valuesJson, CALLBACK_OBJECT_NAME);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] TrackEvent failed: {e.Message}");
                callback?.Invoke(AdshiftResult.Failure(e.Message));
            }
        }

        public void TrackPurchase(string productId, double revenue, string currency, string transactionId, Action<AdshiftResult> callback)
        {
            AdshiftCallbackHandler.Instance.SetEventCallback(callback);

            try
            {
                GetBridge()?.CallStatic("trackPurchase", productId, revenue, currency, transactionId, CALLBACK_OBJECT_NAME);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] TrackPurchase failed: {e.Message}");
                callback?.Invoke(AdshiftResult.Failure(e.Message));
            }
        }

        public void SetConsentData(AdshiftConsent consent)
        {
            try
            {
                string consentJson = DictionaryToJson(consent.ToDictionary());
                GetBridge()?.CallStatic("setConsentData", consentJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] SetConsentData failed: {e.Message}");
            }
        }

        public void EnableTCFDataCollection(bool enabled)
        {
            try
            {
                GetBridge()?.CallStatic("enableTCFDataCollection", enabled);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] EnableTCFDataCollection failed: {e.Message}");
            }
        }

        public void RefreshConsent()
        {
            try
            {
                GetBridge()?.CallStatic("refreshConsent");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] RefreshConsent failed: {e.Message}");
            }
        }

        public void SetDeepLinkListener(Action<AdshiftDeepLink> listener)
        {
            AdshiftCallbackHandler.Instance.SetDeepLinkListener(listener);

            try
            {
                GetBridge()?.CallStatic("setDeepLinkListener", CALLBACK_OBJECT_NAME);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] SetDeepLinkListener failed: {e.Message}");
            }
        }

        // ============ IAdshiftAndroidPlatform Implementation ============

        public void SetCollectOaid(bool collect)
        {
            try
            {
                GetBridge()?.CallStatic("setCollectOaid", collect);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] SetCollectOaid failed: {e.Message}");
            }
        }

        public void SetOaidData(string oaid)
        {
            try
            {
                GetBridge()?.CallStatic("setOaidData", oaid ?? "");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] SetOaidData failed: {e.Message}");
            }
        }

        public void HandleAppLinkIntent()
        {
            var activity = GetCurrentActivity();
            if (activity == null)
            {
                Debug.LogError("[AdShift Android] HandleAppLinkIntent: Activity not available");
                return;
            }

            try
            {
                GetBridge()?.CallStatic("handleAppLinkIntent", activity);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] HandleAppLinkIntent failed: {e.Message}");
            }
        }

        public void GetGoogleAdvertisingId(Action<string> callback)
        {
            AdshiftCallbackHandler.Instance.SetGAIDCallback(callback);

            try
            {
                GetBridge()?.CallStatic("getGoogleAdvertisingId", CALLBACK_OBJECT_NAME);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift Android] GetGoogleAdvertisingId failed: {e.Message}");
                callback?.Invoke("");
            }
        }

        // ============ Helpers ============

        private static string DictionaryToJson(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0) return "{}";

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

