# Changelog

All notable changes to the AdShift Unity Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2025-01-01

### Added

- Initial release of AdShift Unity Plugin
- **Core Features:**
  - SDK initialization and lifecycle management
  - Install attribution tracking
  - In-app event tracking with custom parameters
  - Purchase event tracking with revenue
- **iOS Support:**
  - Native iOS SDK integration via XCFramework
  - SKAdNetwork 4.0+ support (Fine & Coarse conversion values)
  - App Tracking Transparency (ATT) integration
  - Automatic framework linking via post-process build
- **Android Support:**
  - Native Android SDK integration via AAR
  - Google Advertising ID (GAID) support
  - OAID support for Chinese devices
  - Automatic manifest configuration
- **Privacy & Compliance:**
  - Manual consent management (GDPR/DMA)
  - TCF 2.2 automatic consent collection
  - Consent-aware identifier handling
- **Deep Linking:**
  - Direct deep link handling
  - Deferred deep link resolution
  - Deep link event callbacks
- **Developer Experience:**
  - Unity Package Manager (UPM) support
  - Comprehensive API documentation
  - Example scene with test UI
  - Editor mock for testing without builds

### Platforms

- iOS 15.0+
- Android API 21+ (Android 5.0)
- Unity 2022.3 LTS+

