/*
 * AdshiftUnityBridge.java
 * AdShift SDK Unity Bridge for Android
 *
 * Native Java bridge between Unity C# and AdShift Android SDK (Kotlin).
 * Provides static methods that Unity calls via AndroidJavaClass.
 *
 * Copyright © 2024 AdShift. All rights reserved.
 */

package com.adshift.unity;

import android.app.Activity;
import android.content.Intent;
import android.util.Log;

import com.adshift.sdk.core.AdShiftLib;
import com.adshift.sdk.core.AdShiftConsent;
import com.adshift.sdk.core.AdShiftRequestListener;
import com.adshift.sdk.core.ConsentHint;
import com.adshift.sdk.core.ConsentOptions;
import com.adshift.sdk.core.deeplink.DeepLinkListener;
import com.adshift.sdk.core.deeplink.DeepLinkResult;
import com.adshift.sdk.core.deeplink.DeepLinkStatus;
import com.unity3d.player.UnityPlayer;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;

/**
 * Unity bridge for AdShift Android SDK.
 * All methods are static and called from Unity C# via AndroidJavaClass.
 * 
 * The native AdShift SDK uses Kotlin object with @JvmStatic methods,
 * so we can call them directly as static methods (e.g., AdShiftLib.start()).
 */
public class AdshiftUnityBridge {

    private static final String TAG = "AdShiftUnity";
    
    // Callback method names (must match AdshiftCallbackHandler.cs)
    private static final String ON_START_CALLBACK = "OnStartCallback";
    private static final String ON_EVENT_CALLBACK = "OnEventCallback";
    private static final String ON_DEEP_LINK_RECEIVED = "OnDeepLinkReceived";

    // Stored callback object name for deep links
    private static String deepLinkCallbackObject = null;

    // ============================================================================
    // Lifecycle
    // ============================================================================

    /**
     * Initializes the SDK with configuration.
     * 
     * @param activity Current Unity activity
     * @param configJson JSON configuration string
     */
    public static void initialize(Activity activity, String configJson) {
        if (activity == null) {
            Log.e(TAG, "Initialize failed: activity is null");
            return;
        }

        try {
            JSONObject config = new JSONObject(configJson);
            
            String apiKey = config.optString("apiKey", "");
            if (apiKey.isEmpty()) {
                Log.e(TAG, "Initialize failed: no API key");
                return;
            }

            boolean isDebug = config.optBoolean("isDebug", false);
            int appOpenDebounceMs = config.optInt("appOpenDebounceMs", 10000);
            boolean collectOaid = config.optBoolean("collectOaid", true);

            // Initialize SDK - note: method is initSdk (lowercase 's')
            AdShiftLib.initSdk(activity.getApplicationContext(), apiKey);
            
            // Apply configuration after init
            AdShiftLib.setDebugLog(isDebug);
            AdShiftLib.setAppOpenDebounceMs(appOpenDebounceMs);
            AdShiftLib.setCollectOaid(collectOaid);

            Log.i(TAG, "SDK initialized with apiKey: " + apiKey.substring(0, Math.min(4, apiKey.length())) + "...");

        } catch (JSONException e) {
            Log.e(TAG, "Initialize failed: invalid JSON - " + e.getMessage());
        } catch (Exception e) {
            Log.e(TAG, "Initialize failed: " + e.getMessage());
        }
    }

    /**
     * Starts the SDK.
     * 
     * @param callbackObjectName Unity GameObject name for callbacks
     */
    public static void start(final String callbackObjectName) {
        try {
            AdShiftLib.start(new AdShiftRequestListener() {
                @Override
                public void onSuccess() {
                    sendUnityMessage(callbackObjectName, ON_START_CALLBACK, "success");
                }

                @Override
                public void onError(int code, String error) {
                    sendUnityMessage(callbackObjectName, ON_START_CALLBACK, error != null ? error : "Unknown error");
                }
            });
        } catch (Exception e) {
            Log.e(TAG, "Start failed: " + e.getMessage());
            sendUnityMessage(callbackObjectName, ON_START_CALLBACK, e.getMessage());
        }
    }

    /**
     * Stops the SDK.
     */
    public static void stop() {
        try {
            AdShiftLib.stop();
        } catch (Exception e) {
            Log.e(TAG, "Stop failed: " + e.getMessage());
        }
    }

    /**
     * Checks if SDK is started.
     * 
     * @return true if started
     */
    public static boolean isStarted() {
        try {
            return AdShiftLib.isStarted();
        } catch (Exception e) {
            Log.e(TAG, "IsStarted failed: " + e.getMessage());
            return false;
        }
    }

    // ============================================================================
    // Configuration
    // ============================================================================

    /**
     * Enables or disables debug logging.
     * 
     * @param enabled true to enable debug logs
     */
    public static void setDebugEnabled(boolean enabled) {
        try {
            AdShiftLib.setDebugLog(enabled);
        } catch (Exception e) {
            Log.e(TAG, "SetDebugEnabled failed: " + e.getMessage());
        }
    }

    /**
     * Sets custom user ID.
     * 
     * @param userId User identifier
     */
    public static void setCustomerUserId(String userId) {
        try {
            AdShiftLib.setCustomerUserId(userId != null ? userId : "");
        } catch (Exception e) {
            Log.e(TAG, "SetCustomerUserId failed: " + e.getMessage());
        }
    }

    /**
     * Sets app open debounce interval.
     * 
     * @param milliseconds Debounce time in ms
     */
    public static void setAppOpenDebounceMs(int milliseconds) {
        try {
            AdShiftLib.setAppOpenDebounceMs(milliseconds);
        } catch (Exception e) {
            Log.e(TAG, "SetAppOpenDebounceMs failed: " + e.getMessage());
        }
    }

    // ============================================================================
    // Event Tracking
    // ============================================================================

    /**
     * Tracks an in-app event.
     * 
     * @param eventName Event name
     * @param eventValuesJson Event parameters as JSON
     * @param callbackObjectName Unity GameObject for callback
     */
    public static void trackEvent(String eventName, String eventValuesJson, final String callbackObjectName) {
        try {
            Map<String, Object> values = jsonToMap(eventValuesJson);
            
            AdShiftRequestListener listener = null;
            if (callbackObjectName != null && !callbackObjectName.isEmpty()) {
                listener = new AdShiftRequestListener() {
                    @Override
                    public void onSuccess() {
                        sendUnityMessage(callbackObjectName, ON_EVENT_CALLBACK, "success");
                    }

                    @Override
                    public void onError(int code, String error) {
                        sendUnityMessage(callbackObjectName, ON_EVENT_CALLBACK, error != null ? error : "Unknown error");
                    }
                };
            }
            
            AdShiftLib.trackEvent(eventName, values, listener);
            
        } catch (Exception e) {
            Log.e(TAG, "TrackEvent failed: " + e.getMessage());
            if (callbackObjectName != null && !callbackObjectName.isEmpty()) {
                sendUnityMessage(callbackObjectName, ON_EVENT_CALLBACK, e.getMessage());
            }
        }
    }

    /**
     * Tracks a purchase event.
     * 
     * @param productId Product identifier
     * @param revenue Purchase amount
     * @param currency Currency code
     * @param transactionId Transaction ID (purchase token)
     * @param callbackObjectName Unity GameObject for callback
     */
    public static void trackPurchase(
            String productId,
            double revenue,
            String currency,
            String transactionId,
            final String callbackObjectName) {
        try {
            AdShiftRequestListener listener = null;
            if (callbackObjectName != null && !callbackObjectName.isEmpty()) {
                listener = new AdShiftRequestListener() {
                    @Override
                    public void onSuccess() {
                        sendUnityMessage(callbackObjectName, ON_EVENT_CALLBACK, "success");
                    }

                    @Override
                    public void onError(int code, String error) {
                        sendUnityMessage(callbackObjectName, ON_EVENT_CALLBACK, error != null ? error : "Unknown error");
                    }
                };
            }
            
            AdShiftLib.trackPurchase(productId, revenue, currency, transactionId, listener);
            
        } catch (Exception e) {
            Log.e(TAG, "TrackPurchase failed: " + e.getMessage());
            if (callbackObjectName != null && !callbackObjectName.isEmpty()) {
                sendUnityMessage(callbackObjectName, ON_EVENT_CALLBACK, e.getMessage());
            }
        }
    }

    // ============================================================================
    // Consent (GDPR/DMA)
    // ============================================================================

    /**
     * Sets consent data from JSON.
     * 
     * @param consentJson JSON with consent flags
     */
    public static void setConsentData(String consentJson) {
        try {
            JSONObject json = new JSONObject(consentJson);
            
            boolean isGDPR = json.optBoolean("isUserSubjectToGDPR", false);
            boolean dataUsage = json.optBoolean("hasConsentForDataUsage", true);
            boolean personalization = json.optBoolean("hasConsentForAdsPersonalization", true);
            boolean adStorage = json.optBoolean("hasConsentForAdStorage", true);

            AdShiftConsent consent;
            if (isGDPR) {
                // Note: forGDPRUser has @JvmStatic so we can call it directly
                consent = AdShiftConsent.forGDPRUser(dataUsage, personalization, adStorage);
            } else {
                consent = AdShiftConsent.forNonGDPRUser();
            }

            AdShiftLib.setConsentData(consent);

        } catch (JSONException e) {
            Log.e(TAG, "SetConsentData failed: invalid JSON - " + e.getMessage());
        } catch (Exception e) {
            Log.e(TAG, "SetConsentData failed: " + e.getMessage());
        }
    }

    /**
     * Enables or disables TCF data collection.
     * 
     * @param enabled true to enable
     */
    public static void enableTCFDataCollection(boolean enabled) {
        try {
            AdShiftLib.enableTCFDataCollection(enabled);
        } catch (Exception e) {
            Log.e(TAG, "EnableTCFDataCollection failed: " + e.getMessage());
        }
    }

    /**
     * Refreshes consent state from TCF/GPP sources.
     */
    public static void refreshConsent() {
        try {
            // Using default ConsentHint.AUTO and ConsentOptions.default()
            AdShiftLib.refreshConsent();
        } catch (Exception e) {
            Log.e(TAG, "RefreshConsent failed: " + e.getMessage());
        }
    }

    // ============================================================================
    // Deep Links
    // ============================================================================

    /**
     * Sets deep link listener.
     * 
     * @param callbackObjectName Unity GameObject for callbacks
     */
    public static void setDeepLinkListener(final String callbackObjectName) {
        deepLinkCallbackObject = callbackObjectName;
        
        try {
            AdShiftLib.setDeepLinkListener(new DeepLinkListener() {
                @Override
                public void onDeepLinking(DeepLinkResult result) {
                    if (deepLinkCallbackObject != null) {
                        String json = serializeDeepLinkResult(result);
                        sendUnityMessage(deepLinkCallbackObject, ON_DEEP_LINK_RECEIVED, json);
                    }
                }
            });
        } catch (Exception e) {
            Log.e(TAG, "SetDeepLinkListener failed: " + e.getMessage());
        }
    }

    /**
     * Handles app link intent.
     * 
     * @param activity Current activity with intent
     */
    public static void handleAppLinkIntent(Activity activity) {
        if (activity == null) {
            Log.e(TAG, "HandleAppLinkIntent failed: activity is null");
            return;
        }

        try {
            Intent intent = activity.getIntent();
            AdShiftLib.handleAppLinkIntent(intent);
        } catch (Exception e) {
            Log.e(TAG, "HandleAppLinkIntent failed: " + e.getMessage());
        }
    }

    // ============================================================================
    // Android-Specific (OAID, GAID)
    // ============================================================================

    /**
     * Enables or disables OAID collection.
     * 
     * @param collect true to collect OAID
     */
    public static void setCollectOaid(boolean collect) {
        try {
            AdShiftLib.setCollectOaid(collect);
        } catch (Exception e) {
            Log.e(TAG, "SetCollectOaid failed: " + e.getMessage());
        }
    }

    /**
     * Sets OAID manually.
     * 
     * @param oaid OAID string
     */
    public static void setOaidData(String oaid) {
        try {
            AdShiftLib.setOaidData(oaid != null ? oaid : "");
        } catch (Exception e) {
            Log.e(TAG, "SetOaidData failed: " + e.getMessage());
        }
    }

    /**
     * Gets Google Advertising ID asynchronously.
     * 
     * @param callbackObjectName Unity GameObject for callback
     */
    public static void getGoogleAdvertisingId(final String callbackObjectName) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            Log.e(TAG, "GetGoogleAdvertisingId failed: no activity");
            sendUnityMessage(callbackObjectName, "OnGAIDCallback", "");
            return;
        }

        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    com.google.android.gms.ads.identifier.AdvertisingIdClient.Info adInfo =
                        com.google.android.gms.ads.identifier.AdvertisingIdClient.getAdvertisingIdInfo(activity);
                    
                    String gaid = "";
                    if (adInfo != null && !adInfo.isLimitAdTrackingEnabled()) {
                        gaid = adInfo.getId();
                    }
                    
                    sendUnityMessage(callbackObjectName, "OnGAIDCallback", gaid != null ? gaid : "");
                    
                } catch (Exception e) {
                    Log.e(TAG, "GetGoogleAdvertisingId failed: " + e.getMessage());
                    sendUnityMessage(callbackObjectName, "OnGAIDCallback", "");
                }
            }
        }).start();
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    /**
     * Sends a message to Unity via UnitySendMessage.
     * UnitySendMessage must be called from the main thread in some Unity versions,
     * but modern Unity handles cross-thread calls internally.
     */
    private static void sendUnityMessage(String objectName, String methodName, String message) {
        if (objectName == null || objectName.isEmpty()) {
            Log.w(TAG, "Cannot send Unity message: no object name");
            return;
        }
        
        try {
            UnityPlayer.UnitySendMessage(objectName, methodName, message != null ? message : "");
        } catch (Exception e) {
            Log.e(TAG, "UnitySendMessage failed: " + e.getMessage());
        }
    }

    /**
     * Converts JSON string to Map<String, Object>.
     * Handles basic types: String, Boolean, Integer, Long, Double.
     */
    private static Map<String, Object> jsonToMap(String json) {
        Map<String, Object> map = new HashMap<>();
        
        if (json == null || json.isEmpty() || json.equals("{}")) {
            return map;
        }

        try {
            JSONObject jsonObject = new JSONObject(json);
            Iterator<String> keys = jsonObject.keys();
            
            while (keys.hasNext()) {
                String key = keys.next();
                Object value = jsonObject.get(key);
                
                // Handle JSONObject.NULL
                if (value == JSONObject.NULL) {
                    continue; // Skip null values
                }
                
                // JSONObject can return Integer, Long, Double, Boolean, String, JSONObject, JSONArray
                // For nested objects/arrays, we'd need recursive handling - for now, skip them
                if (value instanceof JSONObject) {
                    Log.w(TAG, "Nested JSON objects not supported in event values, skipping key: " + key);
                    continue;
                }
                
                map.put(key, value);
            }
        } catch (JSONException e) {
            Log.e(TAG, "JSON parse error: " + e.getMessage());
        }

        return map;
    }

    /**
     * Serializes DeepLinkResult to JSON string for Unity.
     * Format matches AdshiftDeepLink.FromDictionary in C#.
     */
    private static String serializeDeepLinkResult(DeepLinkResult result) {
        try {
            JSONObject json = new JSONObject();
            
            // Deep link URL (property is 'uri' in Kotlin data class)
            if (result.getUri() != null) {
                json.put("deepLink", result.getUri().toString());
            } else {
                json.put("deepLink", JSONObject.NULL);
            }
            
            // Parameters (property is 'queryParams' in Kotlin data class)
            Map<String, ?> params = result.getQueryParams();
            if (params != null && !params.isEmpty()) {
                json.put("params", new JSONObject(params));
            } else {
                json.put("params", JSONObject.NULL);
            }
            
            // Is deferred
            json.put("isDeferred", result.isDeferred());
            
            // Status - map to string that matches C# enum
            String statusStr = "NotFound";
            DeepLinkStatus status = result.getStatus();
            if (status != null) {
                switch (status) {
                    case FOUND:
                        statusStr = "Found";
                        break;
                    case NOT_FOUND:
                        statusStr = "NotFound";
                        break;
                    case ERROR:
                        statusStr = "Error";
                        break;
                }
            }
            json.put("status", statusStr);
            
            return json.toString();
            
        } catch (JSONException e) {
            Log.e(TAG, "Failed to serialize DeepLinkResult: " + e.getMessage());
            return "{\"status\":\"Error\",\"errorMessage\":\"Serialization failed\",\"isDeferred\":false}";
        }
    }
}
