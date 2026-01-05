using System;
using System.Collections.Generic;

namespace Adshift.Models
{
    /// <summary>
    /// Configuration for initializing the AdShift SDK.
    /// </summary>
    /// <example>
    /// <code>
    /// var config = new AdshiftConfig("your-api-key")
    /// {
    ///     IsDebug = true,
    ///     AppOpenDebounceMs = 15000
    /// };
    /// AdshiftSDK.Initialize(config);
    /// </code>
    /// </example>
    [Serializable]
    public sealed class AdshiftConfig
    {
        /// <summary>
        /// API key for authenticating with AdShift backend.
        /// Required. Get your API key from the AdShift dashboard.
        /// </summary>
        public string ApiKey { get; }

        /// <summary>
        /// Enable debug logging.
        /// When true, SDK will output verbose logs for debugging.
        /// Default: false
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// Debounce interval for automatic APP_OPEN events (in milliseconds).
        /// Controls how often APP_OPEN events are sent when app returns from background.
        /// Default: 10000 (10 seconds)
        /// </summary>
        public int AppOpenDebounceMs { get; set; }

        // ============ iOS-only options ============

        /// <summary>
        /// Disable SKAdNetwork integration (iOS only).
        /// Set to true to completely disable SKAN functionality.
        /// Default: false (SKAN enabled)
        /// </summary>
        public bool? DisableSKAN { get; set; }

        /// <summary>
        /// Wait for ATT authorization before sending install event (iOS only).
        /// When enabled, SDK will wait for App Tracking Transparency authorization
        /// before sending the install event to include IDFA if user grants permission.
        /// Default: false
        /// </summary>
        public bool? WaitForATTBeforeStart { get; set; }

        /// <summary>
        /// Timeout for ATT authorization wait in milliseconds (iOS only).
        /// Maximum time to wait for ATT response when WaitForATTBeforeStart is enabled.
        /// Range: 5000 - 120000 (5s - 2 minutes)
        /// Default: 30000 (30 seconds)
        /// </summary>
        public int? AttTimeoutMs { get; set; }

        // ============ Android-only options ============

        /// <summary>
        /// Enable OAID collection (Android only).
        /// OAID is an alternative to Google Advertising ID used on Chinese Android devices.
        /// Default: true
        /// </summary>
        public bool? CollectOaid { get; set; }

        /// <summary>
        /// Creates an AdShift SDK configuration.
        /// </summary>
        /// <param name="apiKey">Required API key from AdShift dashboard.</param>
        /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
        public AdshiftConfig(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "API key is required");
            }

            ApiKey = apiKey;
            IsDebug = false;
            AppOpenDebounceMs = 10000;
        }

        /// <summary>
        /// Creates a new AdshiftConfig with builder pattern support.
        /// </summary>
        /// <param name="apiKey">Required API key from AdShift dashboard.</param>
        /// <returns>A new AdshiftConfig instance.</returns>
        public static AdshiftConfig Create(string apiKey) => new AdshiftConfig(apiKey);

        /// <summary>
        /// Converts config to Dictionary for native bridge communication.
        /// </summary>
        /// <returns>Dictionary representation of the config.</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["apiKey"] = ApiKey,
                ["isDebug"] = IsDebug,
                ["appOpenDebounceMs"] = AppOpenDebounceMs
            };

            // iOS only - only include if set
            if (DisableSKAN.HasValue)
                dict["disableSKAN"] = DisableSKAN.Value;
            if (WaitForATTBeforeStart.HasValue)
                dict["waitForATTBeforeStart"] = WaitForATTBeforeStart.Value;
            if (AttTimeoutMs.HasValue)
                dict["attTimeoutMs"] = AttTimeoutMs.Value;

            // Android only
            if (CollectOaid.HasValue)
                dict["collectOaid"] = CollectOaid.Value;

            return dict;
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            var keyPreview = ApiKey.Length > 8 
                ? $"{ApiKey.Substring(0, 4)}...{ApiKey.Substring(ApiKey.Length - 4)}" 
                : "****";
            return $"AdshiftConfig(apiKey: {keyPreview}, isDebug: {IsDebug})";
        }
    }
}

