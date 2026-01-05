using System;
using System.Collections.Generic;

namespace Adshift.Models
{
    /// <summary>
    /// User consent data for GDPR/DMA compliance.
    /// Use factory methods to create consent objects.
    /// </summary>
    /// <example>
    /// <code>
    /// // For GDPR users who accepted all
    /// var consent = AdshiftConsent.ForGDPRUser(
    ///     hasConsentForDataUsage: true,
    ///     hasConsentForAdsPersonalization: true,
    ///     hasConsentForAdStorage: true
    /// );
    /// 
    /// // For non-GDPR users
    /// var consent = AdshiftConsent.ForNonGDPRUser();
    /// 
    /// AdshiftSDK.SetConsentData(consent);
    /// </code>
    /// </example>
    [Serializable]
    public sealed class AdshiftConsent
    {
        /// <summary>
        /// Whether the user is subject to GDPR.
        /// </summary>
        public bool IsUserSubjectToGDPR { get; }

        /// <summary>
        /// Whether user has consented to data usage.
        /// </summary>
        public bool HasConsentForDataUsage { get; }

        /// <summary>
        /// Whether user has consented to ads personalization.
        /// </summary>
        public bool HasConsentForAdsPersonalization { get; }

        /// <summary>
        /// Whether user has consented to ad storage.
        /// </summary>
        public bool HasConsentForAdStorage { get; }

        /// <summary>
        /// Creates a consent object with explicit values.
        /// Prefer using factory methods ForGDPRUser or ForNonGDPRUser.
        /// </summary>
        public AdshiftConsent(
            bool isUserSubjectToGDPR,
            bool hasConsentForDataUsage,
            bool hasConsentForAdsPersonalization,
            bool hasConsentForAdStorage)
        {
            IsUserSubjectToGDPR = isUserSubjectToGDPR;
            HasConsentForDataUsage = hasConsentForDataUsage;
            HasConsentForAdsPersonalization = hasConsentForAdsPersonalization;
            HasConsentForAdStorage = hasConsentForAdStorage;
        }

        /// <summary>
        /// Creates consent for a user subject to GDPR.
        /// Use this when user is in a GDPR region and has provided explicit consent.
        /// </summary>
        /// <param name="hasConsentForDataUsage">Whether user consented to data usage.</param>
        /// <param name="hasConsentForAdsPersonalization">Whether user consented to personalized ads.</param>
        /// <param name="hasConsentForAdStorage">Whether user consented to ad storage.</param>
        /// <returns>AdshiftConsent configured for GDPR user.</returns>
        public static AdshiftConsent ForGDPRUser(
            bool hasConsentForDataUsage,
            bool hasConsentForAdsPersonalization,
            bool hasConsentForAdStorage)
        {
            return new AdshiftConsent(
                isUserSubjectToGDPR: true,
                hasConsentForDataUsage: hasConsentForDataUsage,
                hasConsentForAdsPersonalization: hasConsentForAdsPersonalization,
                hasConsentForAdStorage: hasConsentForAdStorage
            );
        }

        /// <summary>
        /// Creates consent for a user NOT subject to GDPR.
        /// Use this when user is in a non-GDPR region (e.g., US, Asia).
        /// All consent flags are set to true by default.
        /// </summary>
        /// <returns>AdshiftConsent configured for non-GDPR user.</returns>
        public static AdshiftConsent ForNonGDPRUser()
        {
            return new AdshiftConsent(
                isUserSubjectToGDPR: false,
                hasConsentForDataUsage: true,
                hasConsentForAdsPersonalization: true,
                hasConsentForAdStorage: true
            );
        }

        /// <summary>
        /// Determines whether consent is granted for tracking.
        /// If user is not subject to GDPR, consent is assumed granted.
        /// If subject to GDPR, both data usage and ad storage must be granted.
        /// </summary>
        /// <returns>True if tracking is allowed, false otherwise.</returns>
        public bool IsConsentGranted()
        {
            if (!IsUserSubjectToGDPR) return true;
            return HasConsentForDataUsage && HasConsentForAdStorage;
        }

        /// <summary>
        /// Converts consent to Dictionary for native bridge communication.
        /// </summary>
        /// <returns>Dictionary representation of the consent.</returns>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["isUserSubjectToGDPR"] = IsUserSubjectToGDPR,
                ["hasConsentForDataUsage"] = HasConsentForDataUsage,
                ["hasConsentForAdsPersonalization"] = HasConsentForAdsPersonalization,
                ["hasConsentForAdStorage"] = HasConsentForAdStorage
            };
        }

        /// <summary>
        /// Creates consent from a Dictionary (native bridge response).
        /// </summary>
        /// <param name="dict">Dictionary containing consent data.</param>
        /// <returns>AdshiftConsent instance.</returns>
        public static AdshiftConsent FromDictionary(Dictionary<string, object> dict)
        {
            if (dict == null)
            {
                return ForNonGDPRUser();
            }

            return new AdshiftConsent(
                isUserSubjectToGDPR: GetBool(dict, "isUserSubjectToGDPR", false),
                hasConsentForDataUsage: GetBool(dict, "hasConsentForDataUsage", false),
                hasConsentForAdsPersonalization: GetBool(dict, "hasConsentForAdsPersonalization", false),
                hasConsentForAdStorage: GetBool(dict, "hasConsentForAdStorage", false)
            );
        }

        private static bool GetBool(Dictionary<string, object> dict, string key, bool defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is bool boolValue) return boolValue;
                if (value is string stringValue && bool.TryParse(stringValue, out var parsed)) return parsed;
            }
            return defaultValue;
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"AdshiftConsent(gdpr: {IsUserSubjectToGDPR}, dataUsage: {HasConsentForDataUsage}, " +
                   $"personalization: {HasConsentForAdsPersonalization}, storage: {HasConsentForAdStorage})";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is AdshiftConsent other)
            {
                return IsUserSubjectToGDPR == other.IsUserSubjectToGDPR &&
                       HasConsentForDataUsage == other.HasConsentForDataUsage &&
                       HasConsentForAdsPersonalization == other.HasConsentForAdsPersonalization &&
                       HasConsentForAdStorage == other.HasConsentForAdStorage;
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + IsUserSubjectToGDPR.GetHashCode();
                hash = hash * 31 + HasConsentForDataUsage.GetHashCode();
                hash = hash * 31 + HasConsentForAdsPersonalization.GetHashCode();
                hash = hash * 31 + HasConsentForAdStorage.GetHashCode();
                return hash;
            }
        }
    }
}

