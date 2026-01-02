using System;
using System.Collections.Generic;
using Adshift.Models;
using Adshift.Platform;
using UnityEngine;

namespace Adshift.Utils
{
    /// <summary>
    /// MonoBehaviour that receives callbacks from native iOS/Android code via UnitySendMessage.
    /// This GameObject must exist in the scene for native callbacks to work.
    /// </summary>
    /// <remarks>
    /// Unity's native plugin communication uses UnitySendMessage which requires:
    /// 1. A GameObject with a specific name
    /// 2. A MonoBehaviour attached with methods matching the callback names
    /// 
    /// This handler is automatically created by <see cref="AdshiftSDK"/> when needed.
    /// </remarks>
    public class AdshiftCallbackHandler : MonoBehaviour
    {
        /// <summary>
        /// The name of the GameObject that receives native callbacks.
        /// Must match the name used in native code.
        /// </summary>
        public const string OBJECT_NAME = "AdshiftCallbackHandler";

        private static AdshiftCallbackHandler _instance;

        // Callback storage
        private Action<AdshiftResult> _startCallback;
        private Action<AdshiftResult> _eventCallback;
        private Action<AdshiftDeepLink> _deepLinkListener;
        private Action<AdshiftDeepLink> _handleDeepLinkCallback;
        private Action<string> _attCallback;
        private Action<string> _gaidCallback;

        /// <summary>
        /// Gets or creates the singleton callback handler instance.
        /// </summary>
        public static AdshiftCallbackHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = GameObject.Find(OBJECT_NAME);
                    if (go == null)
                    {
                        go = new GameObject(OBJECT_NAME);
                        DontDestroyOnLoad(go);
                    }

                    _instance = go.GetComponent<AdshiftCallbackHandler>();
                    if (_instance == null)
                    {
                        _instance = go.AddComponent<AdshiftCallbackHandler>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ============ Callback Registration ============

        /// <summary>
        /// Registers a callback for SDK start completion.
        /// </summary>
        public void SetStartCallback(Action<AdshiftResult> callback)
        {
            _startCallback = callback;
        }

        /// <summary>
        /// Registers a callback for event tracking completion.
        /// </summary>
        public void SetEventCallback(Action<AdshiftResult> callback)
        {
            _eventCallback = callback;
        }

        /// <summary>
        /// Registers a listener for deep link events.
        /// </summary>
        public void SetDeepLinkListener(Action<AdshiftDeepLink> listener)
        {
            _deepLinkListener = listener;
        }

        /// <summary>
        /// Registers a callback for handleDeepLink completion.
        /// </summary>
        public void SetHandleDeepLinkCallback(Action<AdshiftDeepLink> callback)
        {
            _handleDeepLinkCallback = callback;
        }

        /// <summary>
        /// Registers a callback for ATT authorization (iOS only).
        /// </summary>
        public void SetATTCallback(Action<string> callback)
        {
            _attCallback = callback;
        }

        /// <summary>
        /// Registers a callback for GAID retrieval (Android only).
        /// </summary>
        public void SetGAIDCallback(Action<string> callback)
        {
            _gaidCallback = callback;
        }

        // ============ Native Callback Methods ============
        // These methods are called from native code via UnitySendMessage

        /// <summary>
        /// Called from native when SDK start completes.
        /// </summary>
        /// <param name="result">JSON result or "success" or error message.</param>
        public void OnStartCallback(string result)
        {
            Debug.Log($"[AdShift] OnStartCallback: {result}");
            
            var callback = _startCallback;
            _startCallback = null;

            if (string.IsNullOrEmpty(result) || result == "success")
            {
                callback?.Invoke(AdshiftResult.Success());
            }
            else
            {
                callback?.Invoke(AdshiftResult.Failure(result));
            }
        }

        /// <summary>
        /// Called from native when event tracking completes.
        /// </summary>
        /// <param name="result">JSON result or "success" or error message.</param>
        public void OnEventCallback(string result)
        {
            Debug.Log($"[AdShift] OnEventCallback: {result}");

            var callback = _eventCallback;
            _eventCallback = null;

            if (string.IsNullOrEmpty(result) || result == "success")
            {
                callback?.Invoke(AdshiftResult.Success());
            }
            else
            {
                callback?.Invoke(AdshiftResult.Failure(result));
            }
        }

        /// <summary>
        /// Called from native when a deep link is received.
        /// </summary>
        /// <param name="deepLinkJson">JSON representation of the deep link.</param>
        public void OnDeepLinkReceived(string deepLinkJson)
        {
            Debug.Log($"[AdShift] OnDeepLinkReceived: {deepLinkJson}");

            var deepLink = ParseDeepLink(deepLinkJson);
            _deepLinkListener?.Invoke(deepLink);
        }

        /// <summary>
        /// Called from native when handleDeepLink completes.
        /// </summary>
        /// <param name="deepLinkJson">JSON representation of the deep link.</param>
        public void OnHandleDeepLinkCallback(string deepLinkJson)
        {
            Debug.Log($"[AdShift] OnHandleDeepLinkCallback: {deepLinkJson}");

            var callback = _handleDeepLinkCallback;
            _handleDeepLinkCallback = null;

            var deepLink = ParseDeepLink(deepLinkJson);
            callback?.Invoke(deepLink);
            _deepLinkListener?.Invoke(deepLink);
        }

        /// <summary>
        /// Called from native when ATT authorization completes (iOS only).
        /// </summary>
        /// <param name="status">ATT status string (authorized, denied, restricted, not_determined).</param>
        public void OnATTCallback(string status)
        {
            Debug.Log($"[AdShift] OnATTCallback: {status}");

            var callback = _attCallback;
            _attCallback = null;
            callback?.Invoke(status ?? "unknown");
        }

        /// <summary>
        /// Called from native when GAID is retrieved (Android only).
        /// </summary>
        /// <param name="gaid">Google Advertising ID or empty string.</param>
        public void OnGAIDCallback(string gaid)
        {
            Debug.Log($"[AdShift] OnGAIDCallback: {gaid}");

            var callback = _gaidCallback;
            _gaidCallback = null;
            callback?.Invoke(gaid);
        }

        // ============ Helpers ============

        private static AdshiftDeepLink ParseDeepLink(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return AdshiftDeepLink.NotFound();
            }

            try
            {
                var parsed = AdshiftMiniJSON.Deserialize(json);
                if (parsed is Dictionary<string, object> dict)
                {
                    return AdshiftDeepLink.FromDictionary(dict);
                }
                return AdshiftDeepLink.NotFound();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdShift] Failed to parse deep link: {e.Message}");
                return AdshiftDeepLink.Error(e.Message);
            }
        }
    }
}

