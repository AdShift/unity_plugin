//
//  AdshiftUnityHelper.swift
//  AdShift SDK Unity Helper
//
//  Swift helper that bridges Objective-C++ to Swift SDK.
//  Provides @objc compatible wrappers for Swift-only features.
//
//  Copyright © 2024 AdShift. All rights reserved.
//

import Foundation
import AdshiftSDK

/// Unity helper class that provides @objc compatible methods for the Swift SDK.
/// This is needed because Swift enums with associated values (like ASInAppEventType.customEvent)
/// are not automatically exported to Objective-C.
@available(iOS 15.0, *)
@objc public class AdshiftUnityHelper: NSObject {
    
    /// Shared instance
    @objc public static let shared = AdshiftUnityHelper()
    
    private override init() {
        super.init()
    }
    
    // MARK: - Event Tracking
    
    /// Tracks an event with string name (Unity-compatible)
    /// - Parameters:
    ///   - eventName: Event name string (e.g., "as_add_to_cart" or custom name)
    ///   - values: Optional event parameters as dictionary
    ///   - completion: Completion handler called with result
    @objc public func trackEvent(
        _ eventName: String,
        values: [String: Any]?,
        completion: (([String: Any]?, Error?) -> Void)?
    ) {
        Task { @MainActor in
            // Convert string to ASInAppEventType
            let eventType = self.eventTypeFromString(eventName)
            await Adshift.shared.track(event: eventType, values: values, completionHandler: completion)
        }
    }
    
    /// Tracks a purchase event (Unity-compatible)
    @objc public func trackPurchase(
        productId: String,
        price: Double,
        currency: String,
        transactionId: String,
        completion: (([String: Any]?, Error?) -> Void)?
    ) {
        Task { @MainActor in
            await Adshift.shared.trackPurchase(
                productId: productId,
                price: price,
                currency: currency,
                token: transactionId,
                completionHandler: completion
            )
        }
    }
    
    // MARK: - Deep Links
    
    /// Handles a deep link URL (Unity-compatible)
    /// - Parameters:
    ///   - urlString: Deep link URL as string
    ///   - completion: Completion handler with serialized result
    @objc public func handleDeepLink(
        _ urlString: String,
        completion: @escaping ([String: Any]?, Error?) -> Void
    ) {
        guard let url = URL(string: urlString) else {
            let error = NSError(domain: "AdshiftUnity", code: -1, userInfo: [
                NSLocalizedDescriptionKey: "Invalid URL: \(urlString)"
            ])
            completion(nil, error)
            return
        }
        
        Task { @MainActor in
            do {
                let response = try await Adshift.shared.handleDeepLink(url: url)
                let result = self.serializeDeepLinkResponse(response)
                completion(result, nil)
            } catch {
                completion(nil, error)
            }
        }
    }
    
    // MARK: - Consent
    
    /// Sets consent data for GDPR user (Unity-compatible)
    @objc public func setConsentForGDPRUser(
        hasConsentForDataUsage: Bool,
        hasConsentForAdsPersonalization: Bool,
        hasConsentForAdStorage: Bool
    ) {
        Task { @MainActor in
            let consent = AdShiftConsent.forGDPRUser(
                hasConsentForDataUsage: hasConsentForDataUsage,
                hasConsentForAdsPersonalization: hasConsentForAdsPersonalization,
                hasConsentForAdStorage: hasConsentForAdStorage
            )
            Adshift.shared.setConsentData(consent)
        }
    }
    
    /// Sets consent data for non-GDPR user (Unity-compatible)
    @objc public func setConsentForNonGDPRUser() {
        Task { @MainActor in
            let consent = AdShiftConsent.forNonGDPRUser()
            Adshift.shared.setConsentData(consent)
        }
    }
    
    // MARK: - Private Helpers
    
    /// Converts event name string to ASInAppEventType
    private func eventTypeFromString(_ name: String) -> ASInAppEventType {
        // Map known event names to their enum cases
        switch name {
        case "as_level_achieved": return .levelAchieved
        case "as_add_payment_info": return .addPaymentInfo
        case "as_add_to_cart": return .addToCart
        case "as_add_to_wishlist": return .addToWishList
        case "as_complete_registration": return .completeRegistration
        case "as_tutorial_completion": return .tutorialCompletion
        case "as_initiated_checkout": return .initiatedCheckout
        case "as_purchase": return .purchase
        case "as_rate": return .rate
        case "as_search": return .search
        case "as_spent_credits": return .spentCredit
        case "as_achievement_unlocked": return .achievementUnlocked
        case "as_content_view": return .contentView
        case "as_travel_booking": return .travelBooking
        case "as_share": return .share
        case "as_invite": return .invite
        case "as_login": return .login
        case "as_re_engage": return .reEngage
        case "as_update": return .update
        case "as_opened_from_push_notification": return .openedFromPushNotification
        case "as_list_view": return .listView
        case "as_subscribe": return .subscribe
        case "as_start_trial": return .startTrial
        case "as_ad_click": return .adClick
        case "as_ad_view": return .adView
        case "as_location_changed": return .locationChanged
        case "as_location_coordinates": return .locationCoordinates
        case "as_order_id": return .orderId
        case "as_customer_segment": return .customerSegment
        default: return .customEvent(name)
        }
    }
    
    /// Serializes DeeplinkResponse to dictionary for Unity
    private func serializeDeepLinkResponse(_ response: DeeplinkResponse) -> [String: Any] {
        var result: [String: Any] = [:]
        
        if let deeplink = response.deeplink {
            result["deepLink"] = deeplink
        }
        
        if let params = response.params {
            result["params"] = params
        }
        
        result["isDeferred"] = response.isDeferred ?? false
        
        if let status = response.status {
            switch status {
            case .found:
                result["status"] = "Found"
            case .notFound:
                result["status"] = "NotFound"
            @unknown default:
                result["status"] = response.deeplink != nil ? "Found" : "NotFound"
            }
        } else {
            result["status"] = response.deeplink != nil ? "Found" : "NotFound"
        }
        
        if let error = response.error {
            result["status"] = "Error"
            result["errorMessage"] = "\(error)"
        }
        
        return result
    }
}

