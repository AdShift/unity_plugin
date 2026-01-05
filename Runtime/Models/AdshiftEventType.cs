using System.Collections.Generic;

namespace Adshift.Models
{
    /// <summary>
    /// Predefined event types for AdShift SDK.
    /// Use these constants for consistent event naming across your app.
    /// </summary>
    /// <example>
    /// <code>
    /// // Using predefined event type
    /// AdshiftSDK.TrackEvent(AdshiftEventType.AddToCart, new Dictionary&lt;string, object&gt;
    /// {
    ///     { "product_id", "SKU123" },
    ///     { "price", 29.99 }
    /// });
    /// 
    /// // You can also use custom event names
    /// AdshiftSDK.TrackEvent("my_custom_event");
    /// </code>
    /// </example>
    public static class AdshiftEventType
    {
        // ============ E-commerce Events ============

        /// <summary>
        /// User added item to cart.
        /// </summary>
        public const string AddToCart = "as_add_to_cart";

        /// <summary>
        /// User added item to wishlist.
        /// </summary>
        public const string AddToWishList = "as_add_to_wishlist";

        /// <summary>
        /// User added payment info.
        /// </summary>
        public const string AddPaymentInfo = "as_add_payment_info";

        /// <summary>
        /// User initiated checkout.
        /// </summary>
        public const string InitiatedCheckout = "as_initiated_checkout";

        /// <summary>
        /// User completed a purchase.
        /// </summary>
        public const string Purchase = "as_purchase";

        /// <summary>
        /// User viewed content/product.
        /// </summary>
        public const string ContentView = "as_content_view";

        /// <summary>
        /// User viewed a list of items.
        /// </summary>
        public const string ListView = "as_list_view";

        /// <summary>
        /// User searched.
        /// </summary>
        public const string Search = "as_search";

        // ============ User Lifecycle Events ============

        /// <summary>
        /// User completed registration.
        /// </summary>
        public const string CompleteRegistration = "as_complete_registration";

        /// <summary>
        /// User logged in.
        /// </summary>
        public const string Login = "as_login";

        /// <summary>
        /// User completed tutorial.
        /// </summary>
        public const string TutorialCompletion = "as_tutorial_completion";

        /// <summary>
        /// User subscribed.
        /// </summary>
        public const string Subscribe = "as_subscribe";

        /// <summary>
        /// User started a trial.
        /// </summary>
        public const string StartTrial = "as_start_trial";

        // ============ Gaming Events ============

        /// <summary>
        /// User achieved a level.
        /// </summary>
        public const string LevelAchieved = "as_level_achieved";

        /// <summary>
        /// User unlocked an achievement.
        /// </summary>
        public const string AchievementUnlocked = "as_achievement_unlocked";

        /// <summary>
        /// User spent credits/currency.
        /// </summary>
        public const string SpentCredit = "as_spent_credits";

        // ============ Engagement Events ============

        /// <summary>
        /// User rated the app/content.
        /// </summary>
        public const string Rate = "as_rate";

        /// <summary>
        /// User shared content.
        /// </summary>
        public const string Share = "as_share";

        /// <summary>
        /// User invited others.
        /// </summary>
        public const string Invite = "as_invite";

        /// <summary>
        /// User re-engaged with the app.
        /// </summary>
        public const string ReEngage = "as_re_engage";

        /// <summary>
        /// User updated the app.
        /// </summary>
        public const string Update = "as_update";

        /// <summary>
        /// User opened from push notification.
        /// </summary>
        public const string OpenedFromPushNotification = "as_opened_from_push_notification";

        // ============ Travel Events ============

        /// <summary>
        /// User made a travel booking.
        /// </summary>
        public const string TravelBooking = "as_travel_booking";

        // ============ Ad Events ============

        /// <summary>
        /// User clicked an ad.
        /// </summary>
        public const string AdClick = "as_ad_click";

        /// <summary>
        /// User viewed an ad.
        /// </summary>
        public const string AdView = "as_ad_view";

        // ============ Location Events ============

        /// <summary>
        /// User location changed.
        /// </summary>
        public const string LocationChanged = "as_location_changed";

        /// <summary>
        /// User location coordinates updated.
        /// </summary>
        public const string LocationCoordinates = "as_location_coordinates";

        // ============ Other Events ============

        /// <summary>
        /// Order ID event.
        /// </summary>
        public const string OrderId = "as_order_id";

        /// <summary>
        /// Customer segment event.
        /// </summary>
        public const string CustomerSegment = "as_customer_segment";

        /// <summary>
        /// Returns all predefined event types.
        /// </summary>
        public static IReadOnlyList<string> All => new[]
        {
            // E-commerce
            AddToCart,
            AddToWishList,
            AddPaymentInfo,
            InitiatedCheckout,
            Purchase,
            ContentView,
            ListView,
            Search,
            // User Lifecycle
            CompleteRegistration,
            Login,
            TutorialCompletion,
            Subscribe,
            StartTrial,
            // Gaming
            LevelAchieved,
            AchievementUnlocked,
            SpentCredit,
            // Engagement
            Rate,
            Share,
            Invite,
            ReEngage,
            Update,
            OpenedFromPushNotification,
            // Travel
            TravelBooking,
            // Ad
            AdClick,
            AdView,
            // Location
            LocationChanged,
            LocationCoordinates,
            // Other
            OrderId,
            CustomerSegment
        };

        /// <summary>
        /// Checks if a given event name is a predefined event type.
        /// </summary>
        /// <param name="eventName">Event name to check.</param>
        /// <returns>True if it's a predefined event type, false otherwise.</returns>
        public static bool IsPredefined(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return false;
            
            foreach (var type in All)
            {
                if (type == eventName) return true;
            }
            return false;
        }
    }
}

