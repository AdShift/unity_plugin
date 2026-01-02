using System;
using System.Collections.Generic;
using System.Text;

namespace Adshift
{
    /// <summary>
    /// Status of a deep link resolution.
    /// </summary>
    public enum DeepLinkStatus
    {
        /// <summary>
        /// Deep link was found and resolved successfully.
        /// </summary>
        Found,

        /// <summary>
        /// No deep link was found (e.g., organic install).
        /// </summary>
        NotFound,

        /// <summary>
        /// Error occurred while resolving deep link.
        /// </summary>
        Error
    }

    /// <summary>
    /// Response from deep link handling.
    /// Contains the resolved deep link URL, parameters, and status.
    /// </summary>
    /// <example>
    /// <code>
    /// AdshiftSDK.OnDeepLinkReceived += (deepLink) =>
    /// {
    ///     if (deepLink.Status == DeepLinkStatus.Found)
    ///     {
    ///         string productId = deepLink.GetParam("product_id");
    ///         // Navigate to product
    ///     }
    /// };
    /// </code>
    /// </example>
    [Serializable]
    public sealed class AdshiftDeepLink
    {
        /// <summary>
        /// The deep link URL string.
        /// </summary>
        public string DeepLinkUrl { get; }

        /// <summary>
        /// Query parameters extracted from the deep link.
        /// </summary>
        public Dictionary<string, object> Params { get; }

        /// <summary>
        /// Whether this is a deferred deep link.
        /// Deferred deep links are resolved after app install,
        /// typically from ad campaigns.
        /// </summary>
        public bool IsDeferred { get; }

        /// <summary>
        /// Status of the deep link resolution.
        /// </summary>
        public DeepLinkStatus Status { get; }

        /// <summary>
        /// Error message if Status is Error.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Creates a deep link response.
        /// Prefer using factory methods Found, NotFound, or Error.
        /// </summary>
        public AdshiftDeepLink(
            DeepLinkStatus status,
            string deepLinkUrl = null,
            Dictionary<string, object> @params = null,
            bool isDeferred = false,
            string errorMessage = null)
        {
            Status = status;
            DeepLinkUrl = deepLinkUrl;
            Params = @params ?? new Dictionary<string, object>();
            IsDeferred = isDeferred;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a successful deep link response.
        /// </summary>
        /// <param name="deepLinkUrl">The resolved deep link URL.</param>
        /// <param name="params">Query parameters from the deep link.</param>
        /// <param name="isDeferred">Whether this is a deferred deep link.</param>
        /// <returns>AdshiftDeepLink with Found status.</returns>
        public static AdshiftDeepLink Found(
            string deepLinkUrl,
            Dictionary<string, object> @params = null,
            bool isDeferred = false)
        {
            return new AdshiftDeepLink(
                status: DeepLinkStatus.Found,
                deepLinkUrl: deepLinkUrl,
                @params: @params,
                isDeferred: isDeferred
            );
        }

        /// <summary>
        /// Creates a "not found" deep link response.
        /// Use this for organic installs or when no deep link data is available.
        /// </summary>
        /// <returns>AdshiftDeepLink with NotFound status.</returns>
        public static AdshiftDeepLink NotFound()
        {
            return new AdshiftDeepLink(status: DeepLinkStatus.NotFound);
        }

        /// <summary>
        /// Creates an error deep link response.
        /// </summary>
        /// <param name="message">Error message describing what went wrong.</param>
        /// <returns>AdshiftDeepLink with Error status.</returns>
        public static AdshiftDeepLink Error(string message)
        {
            return new AdshiftDeepLink(
                status: DeepLinkStatus.Error,
                errorMessage: message
            );
        }

        /// <summary>
        /// Gets a parameter value by key.
        /// </summary>
        /// <param name="key">Parameter key.</param>
        /// <returns>Parameter value as string, or null if not found.</returns>
        public string GetParam(string key)
        {
            if (Params != null && Params.TryGetValue(key, out var value))
            {
                return value?.ToString();
            }
            return null;
        }

        /// <summary>
        /// Gets a parameter value by key with a default value.
        /// </summary>
        /// <param name="key">Parameter key.</param>
        /// <param name="defaultValue">Default value if key not found.</param>
        /// <returns>Parameter value as string, or defaultValue if not found.</returns>
        public string GetParam(string key, string defaultValue)
        {
            return GetParam(key) ?? defaultValue;
        }

        /// <summary>
        /// Checks if a parameter exists.
        /// </summary>
        /// <param name="key">Parameter key.</param>
        /// <returns>True if parameter exists, false otherwise.</returns>
        public bool HasParam(string key)
        {
            return Params != null && Params.ContainsKey(key);
        }

        /// <summary>
        /// Creates deep link from a Dictionary (native bridge response).
        /// </summary>
        /// <param name="dict">Dictionary containing deep link data.</param>
        /// <returns>AdshiftDeepLink instance.</returns>
        public static AdshiftDeepLink FromDictionary(Dictionary<string, object> dict)
        {
            if (dict == null)
            {
                return NotFound();
            }

            var status = DeepLinkStatus.NotFound;
            if (dict.TryGetValue("status", out var statusObj))
            {
                var statusStr = statusObj?.ToString() ?? "notFound";
                status = ParseStatus(statusStr);
            }

            string deepLinkUrl = null;
            if (dict.TryGetValue("deepLink", out var urlObj) || dict.TryGetValue("deepLinkUrl", out urlObj))
            {
                deepLinkUrl = urlObj?.ToString();
            }

            Dictionary<string, object> @params = null;
            if (dict.TryGetValue("params", out var paramsObj) && paramsObj is Dictionary<string, object> paramsDict)
            {
                @params = paramsDict;
            }

            bool isDeferred = false;
            if (dict.TryGetValue("isDeferred", out var deferredObj))
            {
                if (deferredObj is bool deferredBool) isDeferred = deferredBool;
                else if (deferredObj is string deferredStr) bool.TryParse(deferredStr, out isDeferred);
            }

            string errorMessage = null;
            if (dict.TryGetValue("errorMessage", out var errorObj))
            {
                errorMessage = errorObj?.ToString();
            }

            return new AdshiftDeepLink(
                status: status,
                deepLinkUrl: deepLinkUrl,
                @params: @params,
                isDeferred: isDeferred,
                errorMessage: errorMessage
            );
        }

        private static DeepLinkStatus ParseStatus(string statusStr)
        {
            if (string.IsNullOrEmpty(statusStr))
                return DeepLinkStatus.NotFound;

            // Handle both PascalCase and camelCase
            switch (statusStr.ToLowerInvariant())
            {
                case "found":
                    return DeepLinkStatus.Found;
                case "notfound":
                case "not_found":
                    return DeepLinkStatus.NotFound;
                case "error":
                    return DeepLinkStatus.Error;
                default:
                    return DeepLinkStatus.NotFound;
            }
        }

        /// <summary>
        /// Converts deep link to Dictionary for native bridge communication.
        /// </summary>
        /// <returns>Dictionary representation of the deep link.</returns>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["status"] = Status.ToString().ToLowerInvariant(),
                ["deepLink"] = DeepLinkUrl,
                ["params"] = Params,
                ["isDeferred"] = IsDeferred,
                ["errorMessage"] = ErrorMessage
            };
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"AdshiftDeepLink(status: {Status}");
            
            if (!string.IsNullOrEmpty(DeepLinkUrl))
                sb.Append($", url: {DeepLinkUrl}");
            
            if (IsDeferred)
                sb.Append(", deferred: true");
            
            if (Params != null && Params.Count > 0)
                sb.Append($", params: {Params.Count} items");
            
            if (!string.IsNullOrEmpty(ErrorMessage))
                sb.Append($", error: {ErrorMessage}");
            
            sb.Append(")");
            return sb.ToString();
        }
    }
}

