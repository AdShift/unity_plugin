//
//  AdshiftUnityBridge.mm
//  AdShift SDK Unity Bridge
//
//  Native Objective-C++ bridge between Unity C# and AdshiftSDK (Swift).
//  Exposes extern "C" functions that Unity calls via P/Invoke (DllImport).
//
//  Copyright © 2024 AdShift. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>

// Import Apple frameworks for ATT and IDFA
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <AdSupport/AdSupport.h>

// Import Unity's generated Swift header for AdshiftUnityHelper
// AdshiftUnityHelper.swift is compiled with Unity project and exposes @objc methods
// This header is generated during Xcode build and contains our helper class
#if __has_include("UnityFramework/UnityFramework-Swift.h")
#import "UnityFramework/UnityFramework-Swift.h"
#elif __has_include("Unity-iPhone-Swift.h")
#import "Unity-iPhone-Swift.h"
#elif __has_include("ProductName-Swift.h")
#import "ProductName-Swift.h"
#endif

// Forward declaration for Unity helper (will be linked at build time)
@class AdshiftUnityHelper;

// ============================================================================
// MARK: - Helper Functions
// ============================================================================

#pragma mark - String Conversion

/// Converts C string to NSString (handles NULL)
static NSString* _Nullable stringFromChar(const char* _Nullable str) {
    if (str == NULL) return nil;
    return [NSString stringWithUTF8String:str];
}

/// Converts C string to a copy that Unity can use
/// Unity will copy this string internally, so we use strdup for safety
/// Note: This creates a small memory overhead but prevents crashes from dangling pointers
static const char* _Nonnull makeCStringCopy(NSString* _Nullable str) {
    if (str == nil || str.length == 0) return "";
    return strdup([str UTF8String]);
}

#pragma mark - JSON Parsing

/// Parses JSON string to NSDictionary
static NSDictionary* _Nullable dictionaryFromJson(const char* _Nullable json) {
    if (json == NULL || strlen(json) == 0) return nil;
    
    NSString* jsonString = [NSString stringWithUTF8String:json];
    if (jsonString == nil || jsonString.length == 0) return nil;
    
    NSData* jsonData = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    if (jsonData == nil) return nil;
    
    NSError* error = nil;
    id parsed = [NSJSONSerialization JSONObjectWithData:jsonData 
                                                options:0 
                                                  error:&error];
    if (error != nil) {
        NSLog(@"[AdShift Unity] JSON parse error: %@", error.localizedDescription);
        return nil;
    }
    
    if ([parsed isKindOfClass:[NSDictionary class]]) {
        return (NSDictionary*)parsed;
    }
    
    return nil;
}

/// Serializes NSDictionary to JSON string
static const char* _Nonnull jsonFromDictionary(NSDictionary* _Nullable dict) {
    if (dict == nil || dict.count == 0) return "{}";
    
    NSError* error = nil;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:dict 
                                                       options:0 
                                                         error:&error];
    if (error != nil || jsonData == nil) {
        NSLog(@"[AdShift Unity] JSON serialize error: %@", error.localizedDescription);
        return "{}";
    }
    
    NSString* jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    return makeCStringCopy(jsonString);
}

#pragma mark - Unity Callback

/// Global storage for callback object names
static NSString* _deepLinkCallbackObject = nil;

/// Sends message back to Unity via UnitySendMessage
static void unityCallback(NSString* _Nullable objectName, const char* _Nonnull methodName, const char* _Nonnull message) {
    if (objectName == nil || objectName.length == 0) {
        NSLog(@"[AdShift Unity] Cannot send callback - no object name");
        return;
    }
    
    // UnitySendMessage is provided by Unity runtime
    extern void UnitySendMessage(const char*, const char*, const char*);
    UnitySendMessage([objectName UTF8String], methodName, message);
}

// ============================================================================
// MARK: - Extern C Functions (Called from Unity via DllImport)
// ============================================================================

extern "C" {

#pragma mark - Lifecycle

/// Initializes the SDK with configuration JSON
/// Config JSON: {"apiKey": "...", "isDebug": true, "appOpenDebounceMs": 10000, ...}
void _adshift_initialize(const char* configJson) {
    NSDictionary* config = dictionaryFromJson(configJson);
    if (config == nil) {
        NSLog(@"[AdShift Unity] Initialize failed - invalid config JSON");
        return;
    }
    
    // Extract config values
    NSString* apiKey = config[@"apiKey"];
    BOOL isDebug = [config[@"isDebug"] boolValue];
    int appOpenDebounceMs = [config[@"appOpenDebounceMs"] intValue];
    BOOL disableSKAN = [config[@"disableSKAN"] boolValue];
    BOOL waitForATT = [config[@"waitForATTBeforeStart"] boolValue];
    int attTimeoutMs = [config[@"attTimeoutMs"] intValue];
    
    // Use helper to initialize SDK
    [[AdshiftUnityHelper shared] initializeWithApiKey:apiKey
                                              isDebug:isDebug
                                    appOpenDebounceMs:appOpenDebounceMs
                                          disableSKAN:disableSKAN
                                waitForATTBeforeStart:waitForATT
                                         attTimeoutMs:attTimeoutMs];
    
    NSLog(@"[AdShift Unity] SDK initialized");
}

/// Starts the SDK
void _adshift_start(const char* callbackObjectName) {
    NSString* callbackObject = stringFromChar(callbackObjectName);
    
    [[AdshiftUnityHelper shared] startWithCompletion:^(NSDictionary* _Nullable result, NSError* _Nullable error) {
        if (callbackObject != nil && callbackObject.length > 0) {
            if (error != nil) {
                unityCallback(callbackObject, "OnStartCallback", [error.localizedDescription UTF8String]);
            } else {
                unityCallback(callbackObject, "OnStartCallback", "success");
            }
        }
    }];
}

/// Stops the SDK
void _adshift_stop(void) {
    [[AdshiftUnityHelper shared] stop];
}

/// Checks if SDK is started
bool _adshift_isStarted(void) {
    return [[AdshiftUnityHelper shared] isStarted];
}

#pragma mark - Configuration

/// Enables/disables debug logging
void _adshift_setDebugEnabled(bool enabled) {
    [[AdshiftUnityHelper shared] setDebugEnabled:enabled];
}

/// Sets custom user ID
void _adshift_setCustomerUserId(const char* userId) {
    NSString* userIdStr = stringFromChar(userId);
    if (userIdStr == nil) return;
    
    [[AdshiftUnityHelper shared] setCustomerUserId:userIdStr];
}

/// Sets app open debounce interval
void _adshift_setAppOpenDebounceMs(int milliseconds) {
    [[AdshiftUnityHelper shared] setAppOpenDebounceMs:milliseconds];
}

#pragma mark - Event Tracking

/// Tracks an in-app event
void _adshift_trackEvent(const char* eventName, const char* eventValuesJson, const char* callbackObjectName) {
    NSString* name = stringFromChar(eventName);
    if (name == nil || name.length == 0) {
        NSLog(@"[AdShift Unity] TrackEvent failed - no event name");
        return;
    }
    
    NSDictionary* values = dictionaryFromJson(eventValuesJson);
    NSString* callbackObject = stringFromChar(callbackObjectName);
    
    // Use Swift helper to handle event type conversion
    [[AdshiftUnityHelper shared] trackEvent:name 
                                     values:values 
                                 completion:^(NSDictionary* _Nullable response, NSError* _Nullable error) {
        if (callbackObject != nil && callbackObject.length > 0) {
            if (error != nil) {
                unityCallback(callbackObject, "OnEventCallback", [error.localizedDescription UTF8String]);
            } else {
                unityCallback(callbackObject, "OnEventCallback", "success");
            }
        }
    }];
}

/// Tracks a purchase event
void _adshift_trackPurchase(const char* productId, double revenue, const char* currency, const char* transactionId, const char* callbackObjectName) {
    NSString* productIdStr = stringFromChar(productId);
    NSString* currencyStr = stringFromChar(currency);
    NSString* txIdStr = stringFromChar(transactionId);
    NSString* callbackObject = stringFromChar(callbackObjectName);
    
    if (productIdStr == nil || currencyStr == nil || txIdStr == nil) {
        NSLog(@"[AdShift Unity] TrackPurchase failed - missing required parameters");
        return;
    }
    
    // Use Swift helper
    [[AdshiftUnityHelper shared] trackPurchaseWithProductId:productIdStr 
                                                     price:revenue 
                                                  currency:currencyStr 
                                             transactionId:txIdStr 
                                                completion:^(NSDictionary* _Nullable response, NSError* _Nullable error) {
        if (callbackObject != nil && callbackObject.length > 0) {
            if (error != nil) {
                unityCallback(callbackObject, "OnEventCallback", [error.localizedDescription UTF8String]);
            } else {
                unityCallback(callbackObject, "OnEventCallback", "success");
            }
        }
    }];
}

#pragma mark - Consent (GDPR/DMA)

/// Sets consent data from JSON
/// JSON: {"isUserSubjectToGDPR": true, "hasConsentForDataUsage": true, ...}
void _adshift_setConsentData(const char* consentJson) {
    NSDictionary* consentDict = dictionaryFromJson(consentJson);
    if (consentDict == nil) {
        NSLog(@"[AdShift Unity] SetConsentData failed - invalid JSON");
        return;
    }
    
    BOOL isGDPR = [consentDict[@"isUserSubjectToGDPR"] boolValue];
    BOOL dataUsage = [consentDict[@"hasConsentForDataUsage"] boolValue];
    BOOL personalization = [consentDict[@"hasConsentForAdsPersonalization"] boolValue];
    BOOL adStorage = [consentDict[@"hasConsentForAdStorage"] boolValue];
    
    if (isGDPR) {
        [[AdshiftUnityHelper shared] setConsentForGDPRUserWithHasConsentForDataUsage:dataUsage 
                                                       hasConsentForAdsPersonalization:personalization 
                                                               hasConsentForAdStorage:adStorage];
    } else {
        [[AdshiftUnityHelper shared] setConsentForNonGDPRUser];
    }
}

/// Enables/disables TCF data collection
void _adshift_enableTCFDataCollection(bool enabled) {
    [[AdshiftUnityHelper shared] enableTCFDataCollection:enabled];
}

/// Refreshes consent state
void _adshift_refreshConsent(void) {
    [[AdshiftUnityHelper shared] refreshConsent];
}

#pragma mark - Deep Links

/// Registers deep link listener
void _adshift_setDeepLinkListener(const char* callbackObjectName) {
    _deepLinkCallbackObject = stringFromChar(callbackObjectName);
    
    [[AdshiftUnityHelper shared] setDeepLinkListener:^(NSDictionary* _Nonnull response) {
        if (_deepLinkCallbackObject != nil) {
            const char* json = jsonFromDictionary(response);
            unityCallback(_deepLinkCallbackObject, "OnDeepLinkReceived", json);
        }
    }];
}

/// Handles incoming deep link URL
void _adshift_handleDeepLink(const char* url, const char* callbackObjectName) {
    NSString* urlString = stringFromChar(url);
    NSString* callbackObject = stringFromChar(callbackObjectName);
    
    if (urlString == nil || urlString.length == 0) {
        NSLog(@"[AdShift Unity] HandleDeepLink failed - no URL");
        return;
    }
    
    // Use Swift helper for async handling
    [[AdshiftUnityHelper shared] handleDeepLink:urlString 
                                     completion:^(NSDictionary* _Nullable result, NSError* _Nullable error) {
        if (callbackObject != nil && callbackObject.length > 0) {
            NSMutableDictionary* response = [NSMutableDictionary dictionary];
            
            if (error != nil) {
                response[@"status"] = @"Error";
                response[@"errorMessage"] = error.localizedDescription;
                response[@"isDeferred"] = @NO;
            } else if (result != nil) {
                [response addEntriesFromDictionary:result];
            } else {
                response[@"status"] = @"NotFound";
                response[@"isDeferred"] = @NO;
            }
            
            const char* json = jsonFromDictionary(response);
            unityCallback(callbackObject, "OnHandleDeepLinkCallback", json);
        }
    }];
}

#pragma mark - iOS-Specific (SKAN, ATT)

/// Disables SKAdNetwork
void _adshift_setDisableSKAN(bool disabled) {
    [[AdshiftUnityHelper shared] setDisableSKAN:disabled];
}

/// Sets wait for ATT before start
void _adshift_setWaitForATTBeforeStart(bool wait) {
    [[AdshiftUnityHelper shared] setWaitForATTBeforeStart:wait];
}

/// Sets ATT timeout
void _adshift_setAttTimeoutMs(int milliseconds) {
    [[AdshiftUnityHelper shared] setAttTimeoutMs:milliseconds];
}

#pragma mark - ATT Permission

/// Requests App Tracking Transparency authorization
void _adshift_requestTrackingAuthorization(const char* callbackObjectName) {
    NSString* callbackObject = stringFromChar(callbackObjectName);
    
    dispatch_async(dispatch_get_main_queue(), ^{
        if (@available(iOS 14, *)) {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                if (callbackObject != nil && callbackObject.length > 0) {
                    const char* statusStr;
                    switch (status) {
                        case ATTrackingManagerAuthorizationStatusNotDetermined:
                            statusStr = "not_determined";
                            break;
                        case ATTrackingManagerAuthorizationStatusRestricted:
                            statusStr = "restricted";
                            break;
                        case ATTrackingManagerAuthorizationStatusDenied:
                            statusStr = "denied";
                            break;
                        case ATTrackingManagerAuthorizationStatusAuthorized:
                            statusStr = "authorized";
                            break;
                        default:
                            statusStr = "unknown";
                            break;
                    }
                    unityCallback(callbackObject, "OnATTCallback", statusStr);
                }
            }];
        } else {
            // iOS < 14, no ATT needed
            if (callbackObject != nil && callbackObject.length > 0) {
                unityCallback(callbackObject, "OnATTCallback", "authorized");
            }
        }
    });
}

/// Gets current ATT authorization status
int _adshift_getTrackingAuthorizationStatus(void) {
    __block int result = 0; // not_determined
    
    if (@available(iOS 14, *)) {
        result = (int)[ATTrackingManager trackingAuthorizationStatus];
    } else {
        // iOS < 14, always authorized
        result = 3;
    }
    
    return result;
}

/// Gets IDFA if available
const char* _adshift_getIDFA(void) {
    NSUUID* idfa = [[ASIdentifierManager sharedManager] advertisingIdentifier];
    NSString* idfaStr = [idfa UUIDString];
    
    // Check if it's the zero IDFA (tracking not allowed)
    if ([idfaStr isEqualToString:@"00000000-0000-0000-0000-000000000000"]) {
        return "";
    }
    
    return makeCStringCopy(idfaStr);
}

} // extern "C"
