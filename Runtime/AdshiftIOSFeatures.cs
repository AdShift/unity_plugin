using System;
using Adshift.Models;
using Adshift.Platform;

namespace Adshift
{
    /// <summary>
    /// iOS-specific features for the AdShift SDK.
    /// Access via <see cref="AdshiftSDK.Instance"/>.iOS property.
    /// </summary>
    /// <remarks>
    /// This class is null on non-iOS platforms. Always use null-conditional access:
    /// <code>
    /// AdshiftSDK.Instance.iOS?.SetDisableSKAN(false);
    /// </code>
    /// </remarks>
    public sealed class AdshiftIOSFeatures
    {
        private readonly IAdshiftIOSPlatform _platform;

        /// <summary>
        /// Whether iOS features are available on the current platform.
        /// </summary>
        public bool IsAvailable => _platform != null;

        internal AdshiftIOSFeatures(IAdshiftIOSPlatform platform)
        {
            _platform = platform;
        }

        #region SKAdNetwork

        /// <summary>
        /// Disables SKAdNetwork integration.
        /// Must be called BEFORE <see cref="AdshiftSDK.Start"/>.
        /// </summary>
        /// <param name="disabled">True to disable SKAN, false to enable (default).</param>
        /// <remarks>
        /// Only disable SKAN if you have a specific reason (e.g., using another MMP's SKAN).
        /// SKAN is required for iOS 14.5+ attribution.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Disable SKAN (not recommended)
        /// AdshiftSDK.Instance.iOS?.SetDisableSKAN(true);
        /// </code>
        /// </example>
        public void SetDisableSKAN(bool disabled)
        {
            EnsureAvailable();
            _platform.SetDisableSKAN(disabled);
        }

        #endregion

        #region App Tracking Transparency (ATT)

        /// <summary>
        /// Configures SDK to wait for ATT authorization before sending install event.
        /// Must be called BEFORE <see cref="AdshiftSDK.Start"/>.
        /// </summary>
        /// <param name="wait">True to wait for ATT, false to proceed immediately.</param>
        /// <remarks>
        /// When enabled, the SDK delays sending the install event until:
        /// - User responds to ATT dialog
        /// - Timeout expires (<see cref="SetAttTimeoutMs"/>)
        /// 
        /// This allows collecting IDFA if user grants permission.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Wait for ATT before install event
        /// AdshiftSDK.Instance.iOS?.SetWaitForATTBeforeStart(true);
        /// AdshiftSDK.Instance.iOS?.SetAttTimeoutMs(60000); // 60 seconds
        /// AdshiftSDK.Start();
        /// 
        /// // Show ATT dialog in your app
        /// ATTrackingManager.RequestTrackingAuthorization(...);
        /// </code>
        /// </example>
        public void SetWaitForATTBeforeStart(bool wait)
        {
            EnsureAvailable();
            _platform.SetWaitForATTBeforeStart(wait);
        }

        /// <summary>
        /// Sets the timeout for waiting for ATT authorization.
        /// Must be called BEFORE <see cref="AdshiftSDK.Start"/>.
        /// </summary>
        /// <param name="milliseconds">Timeout in milliseconds. Range: 5000-120000. Default: 30000.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if timeout is outside valid range.</exception>
        /// <remarks>
        /// If user doesn't respond to ATT within this timeout, install event is sent without IDFA.
        /// </remarks>
        public void SetAttTimeoutMs(int milliseconds)
        {
            EnsureAvailable();
            if (milliseconds < 5000 || milliseconds > 120000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(milliseconds),
                    milliseconds,
                    "ATT timeout must be between 5000 (5s) and 120000 (2min)");
            }
            _platform.SetAttTimeoutMs(milliseconds);
        }

        #endregion

        #region ATT Permission

        /// <summary>
        /// Requests App Tracking Transparency authorization from the user.
        /// Shows the native iOS tracking permission dialog.
        /// </summary>
        /// <param name="callback">Callback with the authorization status string.</param>
        /// <remarks>
        /// Possible status values:
        /// - "authorized": User granted permission
        /// - "denied": User denied permission
        /// - "restricted": Tracking is restricted (parental controls, etc.)
        /// - "not_determined": User hasn't responded yet
        /// </remarks>
        /// <example>
        /// <code>
        /// AdshiftSDK.Instance.iOS?.RequestTrackingAuthorization(status => {
        ///     Debug.Log($"ATT Status: {status}");
        ///     if (status == "authorized")
        ///     {
        ///         Debug.Log("IDFA available for attribution");
        ///     }
        /// });
        /// </code>
        /// </example>
        public void RequestTrackingAuthorization(Action<string> callback)
        {
            EnsureAvailable();
            _platform.RequestTrackingAuthorization(callback);
        }

        /// <summary>
        /// Gets the current App Tracking Transparency authorization status.
        /// </summary>
        /// <returns>Authorization status string (authorized, denied, restricted, not_determined).</returns>
        public string GetTrackingAuthorizationStatus()
        {
            EnsureAvailable();
            return _platform.GetTrackingAuthorizationStatus();
        }

        /// <summary>
        /// Gets the IDFA (Identifier for Advertisers) if available.
        /// </summary>
        /// <returns>IDFA string, or null if not available (ATT denied, restricted, or not determined).</returns>
        /// <remarks>
        /// IDFA is only available when ATT status is "authorized".
        /// Returns all zeros "00000000-0000-0000-0000-000000000000" if ATT is not granted.
        /// </remarks>
        public string GetIDFA()
        {
            EnsureAvailable();
            return _platform.GetIDFA();
        }

        #endregion

        #region Deep Links

        /// <summary>
        /// Handles an incoming deep link URL.
        /// Call this when your app receives a Universal Link or custom URL scheme.
        /// </summary>
        /// <param name="url">The deep link URL string.</param>
        /// <param name="callback">Optional callback with the resolved deep link.</param>
        /// <exception cref="ArgumentNullException">Thrown if url is null or empty.</exception>
        /// <remarks>
        /// On iOS, deep links are typically received in:
        /// - AppDelegate's application:openURL:options:
        /// - AppDelegate's application:continueUserActivity:
        /// 
        /// Forward the URL to this method for attribution.
        /// </remarks>
        /// <example>
        /// <code>
        /// // In your Unity iOS callback
        /// void OnDeepLinkReceived(string url)
        /// {
        ///     AdshiftSDK.Instance.iOS?.HandleDeepLink(url, deepLink => {
        ///         if (deepLink.Status == AdshiftDeepLinkStatus.Found)
        ///         {
        ///             // Route user based on deep link
        ///             Debug.Log($"Campaign: {deepLink.Params?["campaign"]}");
        ///         }
        ///     });
        /// }
        /// </code>
        /// </example>
        public void HandleDeepLink(string url, Action<AdshiftDeepLink> callback = null)
        {
            EnsureAvailable();
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            _platform.HandleDeepLink(url, callback);
        }

        #endregion

        #region Private Helpers

        private void EnsureAvailable()
        {
            if (_platform == null)
            {
                throw new PlatformNotSupportedException(
                    "iOS features are not available on this platform. " +
                    "Use null-conditional access: AdshiftSDK.Instance.iOS?.Method()");
            }
        }

        #endregion
    }
}

