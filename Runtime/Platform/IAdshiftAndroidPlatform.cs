using System;
using Adshift.Models;

namespace Adshift.Platform
{
    /// <summary>
    /// Android-specific platform interface extending <see cref="IAdshiftPlatform"/>.
    /// </summary>
    /// <remarks>
    /// Contains methods only available on Android:
    /// - OAID (Open Advertising ID) for Chinese devices
    /// - Intent-based deep link handling
    /// </remarks>
    public interface IAdshiftAndroidPlatform : IAdshiftPlatform
    {
        // ============ OAID (Chinese Device ID) ============

        /// <summary>
        /// Enables or disables OAID collection.
        /// OAID is an alternative to GAID used on Chinese Android devices.
        /// </summary>
        /// <param name="collect">True to collect OAID (default), false to opt-out.</param>
        void SetCollectOaid(bool collect);

        /// <summary>
        /// Manually sets the OAID value.
        /// Use this if you collect OAID through your own implementation.
        /// </summary>
        /// <param name="oaid">The OAID string.</param>
        void SetOaidData(string oaid);

        // ============ Deep Links ============

        /// <summary>
        /// Handles an incoming Android Intent for deep linking.
        /// Call this from your Unity Activity when receiving an App Link intent.
        /// </summary>
        /// <remarks>
        /// This method extracts the deep link URL from the Android Intent
        /// and processes it for attribution. Typically called from:
        /// - UnityPlayerActivity.onNewIntent()
        /// - Custom Activity handling App Links
        /// </remarks>
        void HandleAppLinkIntent();

        // ============ Google Advertising ID ============

        /// <summary>
        /// Gets the Google Advertising ID asynchronously.
        /// </summary>
        /// <param name="callback">Callback with GAID or empty string if not available.</param>
        void GetGoogleAdvertisingId(Action<string> callback);
    }
}

