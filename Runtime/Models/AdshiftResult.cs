using System;

namespace Adshift
{
    /// <summary>
    /// Result of an SDK operation (success or failure).
    /// Used for async callbacks from native SDK.
    /// </summary>
    /// <example>
    /// <code>
    /// AdshiftSDK.TrackEvent("login", null, (result) =>
    /// {
    ///     if (result.IsSuccess)
    ///     {
    ///         Debug.Log("Event tracked successfully");
    ///     }
    ///     else
    ///     {
    ///         Debug.LogError($"Failed: {result.ErrorMessage} (code: {result.ErrorCode})");
    ///     }
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class AdshiftResult
    {
        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Error code if operation failed. 0 for success.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Error message if operation failed. Null for success.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Whether the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        private AdshiftResult(bool isSuccess, int errorCode, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static AdshiftResult Success()
        {
            return new AdshiftResult(true, 0, null);
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <param name="errorCode">Error code from native SDK.</param>
        /// <param name="errorMessage">Human-readable error message.</param>
        public static AdshiftResult Failure(int errorCode, string errorMessage)
        {
            return new AdshiftResult(false, errorCode, errorMessage);
        }

        /// <summary>
        /// Creates a failure result from an exception.
        /// </summary>
        /// <param name="exception">Exception that occurred.</param>
        public static AdshiftResult FromException(Exception exception)
        {
            return new AdshiftResult(false, -1, exception?.Message ?? "Unknown error");
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            if (IsSuccess)
            {
                return "AdshiftResult(Success)";
            }
            return $"AdshiftResult(Failure: [{ErrorCode}] {ErrorMessage})";
        }
    }

    /// <summary>
    /// Result of an SDK operation with a typed value.
    /// </summary>
    /// <typeparam name="T">Type of the result value.</typeparam>
    [Serializable]
    public sealed class AdshiftResult<T>
    {
        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// The result value if operation was successful.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Error code if operation failed. 0 for success.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Error message if operation failed. Null for success.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Whether the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        private AdshiftResult(bool isSuccess, T value, int errorCode, string errorMessage)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a successful result with value.
        /// </summary>
        /// <param name="value">The result value.</param>
        public static AdshiftResult<T> Success(T value)
        {
            return new AdshiftResult<T>(true, value, 0, null);
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <param name="errorCode">Error code from native SDK.</param>
        /// <param name="errorMessage">Human-readable error message.</param>
        public static AdshiftResult<T> Failure(int errorCode, string errorMessage)
        {
            return new AdshiftResult<T>(false, default, errorCode, errorMessage);
        }

        /// <summary>
        /// Gets the value or a default if operation failed.
        /// </summary>
        /// <param name="defaultValue">Default value to return on failure.</param>
        /// <returns>Value if success, defaultValue if failure.</returns>
        public T GetValueOrDefault(T defaultValue = default)
        {
            return IsSuccess ? Value : defaultValue;
        }

        /// <summary>
        /// Converts to non-generic AdshiftResult (loses the value).
        /// </summary>
        public AdshiftResult ToResult()
        {
            return IsSuccess ? AdshiftResult.Success() : AdshiftResult.Failure(ErrorCode, ErrorMessage);
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            if (IsSuccess)
            {
                return $"AdshiftResult<{typeof(T).Name}>(Success: {Value})";
            }
            return $"AdshiftResult<{typeof(T).Name}>(Failure: [{ErrorCode}] {ErrorMessage})";
        }
    }
}

