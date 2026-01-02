using System;
using Adshift.Models;

namespace Adshift.Platform
{
    /// <summary>
    /// iOS-specific platform interface extending <see cref="IAdshiftPlatform"/>.
    /// </summary>
    /// <remarks>
    /// Contains methods only available on iOS:
    /// - SKAdNetwork (SKAN) control
    /// - App Tracking Transparency (ATT) handling
    /// - URL-based deep link handling
    /// </remarks>
    public interface IAdshiftIOSPlatform : IAdshiftPlatform
    {
        // ============ SKAdNetwork (SKAN) ============

        /// <summary>
        /// Disables SKAdNetwork integration.
        /// Must be set before <see cref="IAdshiftPlatform.Start"/>.
        /// </summary>
        /// <param name="disabled">True to disable SKAN completely.</param>
        void SetDisableSKAN(bool disabled);

        // ============ App Tracking Transparency (ATT) ============

        /// <summary>
        /// Configures SDK to wait for ATT authorization before sending install event.
        /// Must be set before <see cref="IAdshiftPlatform.Start"/>.
        /// </summary>
        /// <param name="wait">True to wait for ATT response.</param>
        void SetWaitForATTBeforeStart(bool wait);

        /// <summary>
        /// Sets the timeout for ATT authorization wait.
        /// Only effective when <see cref="SetWaitForATTBeforeStart"/> is true.
        /// </summary>
        /// <param name="milliseconds">Timeout in milliseconds (5000-120000).</param>
        void SetAttTimeoutMs(int milliseconds);

        /// <summary>
        /// Requests ATT authorization from the user.
        /// Shows the native iOS tracking permission dialog.
        /// </summary>
        /// <param name="callback">Callback with status string (authorized, denied, restricted, not_determined).</param>
        void RequestTrackingAuthorization(Action<string> callback);

        /// <summary>
        /// Gets the current ATT authorization status.
        /// </summary>
        /// <returns>Status string (authorized, denied, restricted, not_determined).</returns>
        string GetTrackingAuthorizationStatus();

        /// <summary>
        /// Gets the IDFA if available.
        /// </summary>
        /// <returns>IDFA string or null if not available.</returns>
        string GetIDFA();

        // ============ Deep Links ============

        /// <summary>
        /// Handles an incoming deep link URL.
        /// Call this when your app receives a URL (Universal Link or custom scheme).
        /// </summary>
        /// <param name="url">The deep link URL string.</param>
        /// <param name="callback">Optional callback with deep link result.</param>
        void HandleDeepLink(string url, Action<AdshiftDeepLink> callback);
    }
}

