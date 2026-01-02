using System;
using Adshift.Platform;

namespace Adshift
{
    /// <summary>
    /// Android-specific features for the AdShift SDK.
    /// Access via <see cref="AdshiftSDK.Instance"/>.Android property.
    /// </summary>
    /// <remarks>
    /// This class is null on non-Android platforms. Always use null-conditional access:
    /// <code>
    /// AdshiftSDK.Instance.Android?.SetCollectOaid(true);
    /// </code>
    /// </remarks>
    public sealed class AdshiftAndroidFeatures
    {
        private readonly IAdshiftAndroidPlatform _platform;

        /// <summary>
        /// Whether Android features are available on the current platform.
        /// </summary>
        public bool IsAvailable => _platform != null;

        internal AdshiftAndroidFeatures(IAdshiftAndroidPlatform platform)
        {
            _platform = platform;
        }

        #region OAID (Open Advertising ID)

        /// <summary>
        /// Enables or disables OAID collection.
        /// OAID is used on Chinese Android devices that don't have Google Play Services.
        /// </summary>
        /// <param name="collect">True to collect OAID, false to disable.</param>
        /// <remarks>
        /// OAID collection requires the MSA SDK to be integrated.
        /// If disabled, the SDK will still work but may have reduced attribution accuracy
        /// on devices without Google Advertising ID.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Enable OAID for Chinese market
        /// AdshiftSDK.Instance.Android?.SetCollectOaid(true);
        /// </code>
        /// </example>
        public void SetCollectOaid(bool collect)
        {
            EnsureAvailable();
            _platform.SetCollectOaid(collect);
        }

        /// <summary>
        /// Sets OAID manually if you've already collected it.
        /// Use this if you're managing OAID collection separately.
        /// </summary>
        /// <param name="oaid">The OAID string.</param>
        /// <exception cref="ArgumentNullException">Thrown if oaid is null or empty.</exception>
        /// <example>
        /// <code>
        /// // If you've collected OAID elsewhere
        /// AdshiftSDK.Instance.Android?.SetOaidData("12345678-1234-1234-1234-123456789012");
        /// </code>
        /// </example>
        public void SetOaidData(string oaid)
        {
            EnsureAvailable();
            if (string.IsNullOrEmpty(oaid))
                throw new ArgumentNullException(nameof(oaid));

            _platform.SetOaidData(oaid);
        }

        #endregion

        #region Deep Links

        /// <summary>
        /// Handles the current activity's incoming app link intent.
        /// Call this when your app is opened via an App Link.
        /// </summary>
        /// <remarks>
        /// On Android, App Links are received via Intent data.
        /// This method extracts and processes the URL from the current Unity activity.
        /// 
        /// Call this in your main scene's Start() method to handle the launch intent.
        /// </remarks>
        /// <example>
        /// <code>
        /// void Start()
        /// {
        ///     AdshiftSDK.Initialize(config);
        ///     
        ///     // Handle deep link from app launch
        ///     AdshiftSDK.Instance.Android?.HandleAppLinkIntent();
        ///     
        ///     AdshiftSDK.Start();
        /// }
        /// </code>
        /// </example>
        public void HandleAppLinkIntent()
        {
            EnsureAvailable();
            _platform.HandleAppLinkIntent();
        }

        #endregion

        #region Google Advertising ID

        /// <summary>
        /// Gets the Google Advertising ID asynchronously.
        /// </summary>
        /// <param name="callback">Callback with GAID or empty string if not available.</param>
        /// <remarks>
        /// GAID retrieval requires Google Play Services and user consent.
        /// Returns empty string if:
        /// - Google Play Services not available
        /// - User has opted out of ad tracking
        /// - Device doesn't support GAID
        /// </remarks>
        /// <example>
        /// <code>
        /// AdshiftSDK.Instance.Android?.GetGoogleAdvertisingId(gaid =>
        /// {
        ///     if (!string.IsNullOrEmpty(gaid))
        ///         Debug.Log($"GAID: {gaid}");
        ///     else
        ///         Debug.Log("GAID not available");
        /// });
        /// </code>
        /// </example>
        public void GetGoogleAdvertisingId(Action<string> callback)
        {
            EnsureAvailable();
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            _platform.GetGoogleAdvertisingId(callback);
        }

        #endregion

        #region Private Helpers

        private void EnsureAvailable()
        {
            if (_platform == null)
            {
                throw new PlatformNotSupportedException(
                    "Android features are not available on this platform. " +
                    "Use null-conditional access: AdshiftSDK.Instance.Android?.Method()");
            }
        }

        #endregion
    }
}

