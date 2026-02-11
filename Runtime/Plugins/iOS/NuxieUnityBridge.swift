import Foundation

#if canImport(Nuxie)
import Nuxie
#endif

@_cdecl("NuxieUnity_Invoke")
public func NuxieUnity_Invoke(
  _ methodPtr: UnsafePointer<CChar>?,
  _ argsJsonPtr: UnsafePointer<CChar>?,
  _ callbackObjectPtr: UnsafePointer<CChar>?,
  _ callbackMethodPtr: UnsafePointer<CChar>?
) -> UnsafeMutablePointer<CChar>? {
  let method = methodPtr.map(String.init(cString:)) ?? ""
  let argsJson = argsJsonPtr.map(String.init(cString:)) ?? "{}"
  let callbackObject = callbackObjectPtr.map(String.init(cString:)) ?? "__NuxieBridgeHost"
  let callbackMethod = callbackMethodPtr.map(String.init(cString:)) ?? "OnNuxieNativeEvent"

  let response = UnityNuxieBridge.shared.invoke(
    method: method,
    argsJson: argsJson,
    callbackObjectName: callbackObject,
    callbackMethodName: callbackMethod
  )

  return strdup(response)
}

@_cdecl("NuxieUnity_FreeCString")
public func NuxieUnity_FreeCString(_ pointer: UnsafeMutablePointer<CChar>?) {
  guard let pointer else { return }
  free(pointer)
}

private final class UnityNuxieBridge {
  static let shared = UnityNuxieBridge()

  private let stateQueue = DispatchQueue(label: "io.nuxie.unity.bridge.state")

#if canImport(Nuxie)
  private var triggerHandles: [String: TriggerHandle] = [:]
  private lazy var purchaseDelegateBridge = UnityPurchaseDelegateBridge(emit: emitEnvelope)
  private lazy var delegateBridge = UnityDelegateBridge(emit: emitEnvelope)
#endif

  private var callbackObjectName: String = "__NuxieBridgeHost"
  private var callbackMethodName: String = "OnNuxieNativeEvent"

  func invoke(method: String, argsJson: String, callbackObjectName: String, callbackMethodName: String) -> String {
    self.callbackObjectName = callbackObjectName
    self.callbackMethodName = callbackMethodName

    guard let args = parseArgs(argsJson) else {
      return errorResponse(code: "INVALID_CONFIGURATION", message: "Failed to parse args JSON")
    }

#if canImport(Nuxie)
    switch method {
    case "configure":
      guard let apiKey = args["apiKey"] as? String, !apiKey.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else {
        return errorResponse(code: "MISSING_API_KEY", message: "Nuxie API key is required.")
      }

      let options = args["options"] as? [String: Any]
      let usingPurchaseController = args["usingPurchaseController"] as? Bool ?? false

      do {
        let config = makeConfiguration(apiKey: apiKey, options: options, usePurchaseController: usingPurchaseController)
        NuxieSDK.shared.delegate = delegateBridge
        try NuxieSDK.shared.setup(with: config)
        return okResponse(value: nil)
      } catch {
        return errorResponse(code: "INVALID_CONFIGURATION", message: error.localizedDescription, nativeStack: String(describing: error))
      }

    case "shutdown":
      let result: Result<Bool, Error> = blocking {
        await NuxieSDK.shared.shutdown()
        return true
      }
      switch result {
      case .success:
        clearState()
        return okResponse(value: nil)
      case .failure(let error):
        return errorResponse(code: "NATIVE_ERROR", message: error.localizedDescription, nativeStack: String(describing: error))
      }

    case "identify":
      guard let distinctId = args["distinctId"] as? String else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "identify requires distinctId")
      }
      let userProperties = args["userProperties"] as? [String: Any]
      let userPropertiesSetOnce = args["userPropertiesSetOnce"] as? [String: Any]
      NuxieSDK.shared.identify(distinctId, userProperties: userProperties, userPropertiesSetOnce: userPropertiesSetOnce)
      return okResponse(value: nil)

    case "reset":
      let keepAnonymousId = args["keepAnonymousId"] as? Bool ?? true
      NuxieSDK.shared.reset(keepAnonymousId: keepAnonymousId)
      return okResponse(value: nil)

    case "getDistinctId":
      return okResponse(value: NuxieSDK.shared.getDistinctId())

    case "getAnonymousId":
      return okResponse(value: NuxieSDK.shared.getAnonymousId())

    case "getIsIdentified":
      return okResponse(value: NuxieSDK.shared.isIdentified)

    case "startTrigger":
      guard let requestId = args["requestId"] as? String,
            let eventName = args["eventName"] as? String
      else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "startTrigger requires requestId and eventName")
      }

      let options = args["options"] as? [String: Any]
      let properties = options?["properties"] as? [String: Any]
      let userProperties = options?["userProperties"] as? [String: Any]
      let userPropertiesSetOnce = options?["userPropertiesSetOnce"] as? [String: Any]

      let handle = NuxieSDK.shared.trigger(
        eventName,
        properties: properties,
        userProperties: userProperties,
        userPropertiesSetOnce: userPropertiesSetOnce
      ) { update in
        let isTerminal = self.isTerminal(update)
        self.emitEnvelope(
          type: "trigger_update",
          requestId: requestId,
          payload: [
            "update": self.triggerUpdateDictionary(update),
            "isTerminal": isTerminal,
          ]
        )

        if isTerminal {
          self.stateQueue.async {
            self.triggerHandles.removeValue(forKey: requestId)
          }
        }
      }

      stateQueue.async {
        self.triggerHandles[requestId] = handle
      }
      return okResponse(value: nil)

    case "cancelTrigger":
      let requestId = args["requestId"] as? String ?? ""
      stateQueue.async {
        if let handle = self.triggerHandles.removeValue(forKey: requestId) {
          handle.cancel()
        }
      }
      return okResponse(value: nil)

    case "showFlow":
      guard let flowId = args["flowId"] as? String else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "showFlow requires flowId")
      }

      Task { @MainActor in
        do {
          try await NuxieSDK.shared.showFlow(with: flowId)
          self.emitEnvelope(type: "flow_presented", payload: ["flowId": flowId])
        } catch {
          self.emitEnvelope(
            type: "flow_dismissed",
            payload: [
              "flowId": flowId,
              "reason": "error",
              "error": error.localizedDescription,
            ]
          )
        }
      }
      return okResponse(value: nil)

    case "refreshProfile":
      let result: Result<[String: Any], Error> = blocking {
        let response = try await NuxieSDK.shared.refreshProfile()
        return self.toDictionary(response)
      }
      return mapResult(result)

    case "hasFeature":
      guard let featureId = args["featureId"] as? String else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "hasFeature requires featureId")
      }
      let requiredBalance = int(args["requiredBalance"])
      let entityId = args["entityId"] as? String
      let result: Result<[String: Any], Error> = blocking {
        let access: FeatureAccess
        if let requiredBalance {
          access = try await NuxieSDK.shared.hasFeature(featureId, requiredBalance: requiredBalance, entityId: entityId)
        } else {
          access = try await NuxieSDK.shared.hasFeature(featureId)
        }
        return self.featureAccessDictionary(access)
      }
      return mapResult(result)

    case "getCachedFeature":
      guard let featureId = args["featureId"] as? String else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "getCachedFeature requires featureId")
      }
      let entityId = args["entityId"] as? String
      let result: Result<[String: Any]?, Error> = blocking {
        let access = await NuxieSDK.shared.getCachedFeature(featureId, entityId: entityId)
        return access.map(self.featureAccessDictionary)
      }
      switch result {
      case .success(let value):
        return okResponse(value: value)
      case .failure(let error):
        return errorResponse(code: "NATIVE_ERROR", message: error.localizedDescription, nativeStack: String(describing: error))
      }

    case "checkFeature":
      guard let featureId = args["featureId"] as? String else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "checkFeature requires featureId")
      }
      let requiredBalance = int(args["requiredBalance"])
      let entityId = args["entityId"] as? String
      let result: Result<[String: Any], Error> = blocking {
        let response = try await NuxieSDK.shared.checkFeature(featureId, requiredBalance: requiredBalance, entityId: entityId)
        return self.featureCheckResultDictionary(response)
      }
      return mapResult(result)

    case "refreshFeature":
      guard let featureId = args["featureId"] as? String else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "refreshFeature requires featureId")
      }
      let requiredBalance = int(args["requiredBalance"])
      let entityId = args["entityId"] as? String
      let result: Result<[String: Any], Error> = blocking {
        let response = try await NuxieSDK.shared.refreshFeature(featureId, requiredBalance: requiredBalance, entityId: entityId)
        return self.featureCheckResultDictionary(response)
      }
      return mapResult(result)

    case "useFeature":
      guard let featureId = args["featureId"] as? String else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "useFeature requires featureId")
      }
      let amount = double(args["amount"]) ?? 1
      let entityId = args["entityId"] as? String
      let metadata = args["metadata"] as? [String: Any]
      NuxieSDK.shared.useFeature(featureId, amount: amount, entityId: entityId, metadata: metadata)
      return okResponse(value: nil)

    case "useFeatureAndWait":
      guard let featureId = args["featureId"] as? String else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "useFeatureAndWait requires featureId")
      }
      let amount = double(args["amount"]) ?? 1
      let entityId = args["entityId"] as? String
      let setUsage = args["setUsage"] as? Bool ?? false
      let metadata = args["metadata"] as? [String: Any]

      let result: Result<[String: Any], Error> = blocking {
        let response = try await NuxieSDK.shared.useFeatureAndWait(
          featureId,
          amount: amount,
          entityId: entityId,
          setUsage: setUsage,
          metadata: metadata
        )
        return self.featureUsageResultDictionary(response)
      }
      return mapResult(result)

    case "flushEvents":
      let result: Result<Bool, Error> = blocking {
        await NuxieSDK.shared.flushEvents()
      }
      return mapResult(result)

    case "getQueuedEventCount":
      let result: Result<Int, Error> = blocking {
        await NuxieSDK.shared.getQueuedEventCount()
      }
      return mapResult(result)

    case "pauseEventQueue":
      let result: Result<Bool, Error> = blocking {
        await NuxieSDK.shared.pauseEventQueue()
        return true
      }
      return mapResult(result)

    case "resumeEventQueue":
      let result: Result<Bool, Error> = blocking {
        await NuxieSDK.shared.resumeEventQueue()
        return true
      }
      return mapResult(result)

    case "completePurchase":
      guard let requestId = args["requestId"] as? String,
            let result = args["result"] as? [String: Any]
      else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "completePurchase requires requestId and result")
      }
      purchaseDelegateBridge.completePurchase(requestId: requestId, payload: result)
      return okResponse(value: nil)

    case "completeRestore":
      guard let requestId = args["requestId"] as? String,
            let result = args["result"] as? [String: Any]
      else {
        return errorResponse(code: "INVALID_CONFIGURATION", message: "completeRestore requires requestId and result")
      }
      purchaseDelegateBridge.completeRestore(requestId: requestId, payload: result)
      return okResponse(value: nil)

    default:
      return errorResponse(code: "NATIVE_ERROR", message: "Unsupported method '\(method)'")
    }
#else
    return errorResponse(code: "NATIVE_ERROR", message: "Nuxie iOS SDK is unavailable in this build.")
#endif
  }

  private func parseArgs(_ raw: String) -> [String: Any]? {
    guard let data = raw.data(using: .utf8) else {
      return nil
    }

    do {
      let value = try JSONSerialization.jsonObject(with: data)
      return value as? [String: Any]
    } catch {
      return nil
    }
  }

#if canImport(Nuxie)
  private func clearState() {
    stateQueue.async {
      self.triggerHandles.removeAll()
    }
  }

  private func makeConfiguration(apiKey: String, options: [String: Any]?, usePurchaseController: Bool) -> NuxieConfiguration {
    let config = NuxieConfiguration(apiKey: apiKey)
    guard let options else {
      if usePurchaseController {
        config.purchaseDelegate = purchaseDelegateBridge
      }
      return config
    }

    if let environment = options["environment"] as? String {
      switch environment {
      case "production": config.environment = .production
      case "staging": config.environment = .staging
      case "development": config.environment = .development
      case "custom": config.environment = .custom
      default: break
      }
    }

    if let endpoint = options["apiEndpoint"] as? String, let url = URL(string: endpoint) {
      config.apiEndpoint = url
      config.environment = .custom
    }

    if let logLevel = options["logLevel"] as? String {
      switch logLevel {
      case "verbose": config.logLevel = .verbose
      case "debug": config.logLevel = .debug
      case "info": config.logLevel = .info
      case "warning": config.logLevel = .warning
      case "error": config.logLevel = .error
      case "none": config.logLevel = .none
      default: break
      }
    }

    if let value = options["enableConsoleLogging"] as? Bool { config.enableConsoleLogging = value }
    if let value = options["enableFileLogging"] as? Bool { config.enableFileLogging = value }
    if let value = options["redactSensitiveData"] as? Bool { config.redactSensitiveData = value }
    if let value = timeInterval(options["requestTimeoutSeconds"]) { config.requestTimeout = value }
    if let value = int(options["retryCount"]) { config.retryCount = value }
    if let value = timeInterval(options["retryDelaySeconds"]) { config.retryDelay = value }
    if let value = timeInterval(options["syncIntervalSeconds"]) { config.syncInterval = value }
    if let value = options["enableCompression"] as? Bool { config.enableCompression = value }
    if let value = int(options["eventBatchSize"]) { config.eventBatchSize = value }
    if let value = int(options["flushAt"]) { config.flushAt = value }
    if let value = timeInterval(options["flushIntervalSeconds"]) { config.flushInterval = value }
    if let value = int(options["maxQueueSize"]) { config.maxQueueSize = value }
    if let value = int64(options["maxCacheSizeBytes"]) { config.maxCacheSize = value }
    if let value = timeInterval(options["cacheExpirationSeconds"]) { config.cacheExpiration = value }
    if let value = options["enableEncryption"] as? Bool { config.enableEncryption = value }
    if let value = timeInterval(options["featureCacheTtlSeconds"]) { config.featureCacheTTL = value }
    if let value = timeInterval(options["defaultPaywallTimeoutSeconds"]) { config.defaultPaywallTimeout = value }
    if let value = options["respectDoNotTrack"] as? Bool { config.respectDoNotTrack = value }
    if let value = options["localeIdentifier"] as? String { config.localeIdentifier = value.isEmpty ? nil : value }
    if let value = options["isDebugMode"] as? Bool { config.isDebugMode = value }
    if let value = options["enablePlugins"] as? Bool { config.enablePlugins = value }
    if let value = int64(options["maxFlowCacheSizeBytes"]) { config.maxFlowCacheSize = value }
    if let value = timeInterval(options["flowCacheExpirationSeconds"]) { config.flowCacheExpiration = value }
    if let value = int(options["maxConcurrentFlowDownloads"]) { config.maxConcurrentFlowDownloads = value }
    if let value = timeInterval(options["flowDownloadTimeoutSeconds"]) { config.flowDownloadTimeout = value }

    if let value = options["customStoragePath"] as? String, let url = parseURL(value) {
      config.customStoragePath = url
    }

    if let value = options["flowCacheDirectory"] as? String, let url = parseURL(value) {
      config.flowCacheDirectory = url
    }

    if let linking = options["eventLinkingPolicy"] as? String {
      config.eventLinkingPolicy = (linking == "keep_separate" || linking == "keepSeparate")
        ? .keepSeparate
        : .migrateOnIdentify
    }

    if usePurchaseController {
      config.purchaseDelegate = purchaseDelegateBridge
    }

    return config
  }

  private func mapResult<T>(_ result: Result<T, Error>) -> String {
    switch result {
    case .success(let value):
      return okResponse(value: value)
    case .failure(let error):
      return errorResponse(code: "NATIVE_ERROR", message: error.localizedDescription, nativeStack: String(describing: error))
    }
  }

  private func blocking<T>(_ operation: @escaping () async throws -> T) -> Result<T, Error> {
    let semaphore = DispatchSemaphore(value: 0)
    var output: Result<T, Error>?

    Task {
      do {
        output = .success(try await operation())
      } catch {
        output = .failure(error)
      }
      semaphore.signal()
    }

    semaphore.wait()
    return output ?? .failure(NSError(domain: "io.nuxie.unity", code: -1, userInfo: [NSLocalizedDescriptionKey: "Operation failed"]))
  }

  private func isTerminal(_ update: TriggerUpdate) -> Bool {
    switch update {
    case .error, .journey:
      return true
    case .entitlement(let entitlement):
      switch entitlement {
      case .allowed, .denied:
        return true
      case .pending:
        return false
      }
    case .decision(let decision):
      switch decision {
      case .allowedImmediate, .deniedImmediate, .noMatch, .suppressed:
        return true
      default:
        return false
      }
    }
  }

  private func triggerUpdateDictionary(_ update: TriggerUpdate) -> [String: Any] {
    switch update {
    case .decision(let decision):
      return ["kind": "decision", "decision": triggerDecisionDictionary(decision)]
    case .entitlement(let entitlement):
      return ["kind": "entitlement", "entitlement": entitlementDictionary(entitlement)]
    case .journey(let journey):
      return ["kind": "journey", "journey": journeyDictionary(journey)]
    case .error(let error):
      return [
        "kind": "error",
        "error": [
          "code": error.code,
          "message": error.message,
        ],
      ]
    }
  }

  private func triggerDecisionDictionary(_ decision: TriggerDecision) -> [String: Any] {
    switch decision {
    case .noMatch:
      return ["type": "no_match"]
    case .allowedImmediate:
      return ["type": "allowed_immediate"]
    case .deniedImmediate:
      return ["type": "denied_immediate"]
    case .journeyStarted(let ref):
      return ["type": "journey_started", "ref": journeyRefDictionary(ref)]
    case .journeyResumed(let ref):
      return ["type": "journey_resumed", "ref": journeyRefDictionary(ref)]
    case .flowShown(let ref):
      return ["type": "flow_shown", "ref": journeyRefDictionary(ref)]
    case .suppressed(let reason):
      return ["type": "suppressed", "reason": suppressReasonDictionary(reason)]
    }
  }

  private func entitlementDictionary(_ entitlement: EntitlementUpdate) -> [String: Any] {
    switch entitlement {
    case .pending:
      return ["type": "pending"]
    case .denied:
      return ["type": "denied"]
    case .allowed(let source):
      return ["type": "allowed", "source": gateSourceString(source)]
    }
  }

  private func suppressReasonDictionary(_ reason: SuppressReason) -> [String: Any] {
    switch reason {
    case .alreadyActive:
      return ["reason": "already_active"]
    case .reentryLimited:
      return ["reason": "reentry_limited"]
    case .holdout:
      return ["reason": "holdout"]
    case .noFlow:
      return ["reason": "no_flow"]
    case .unknown(let rawReason):
      return ["reason": "unknown", "rawReason": rawReason]
    }
  }

  private func journeyRefDictionary(_ ref: JourneyRef) -> [String: Any] {
    [
      "journeyId": ref.journeyId,
      "campaignId": ref.campaignId,
      "flowId": ref.flowId as Any,
    ]
  }

  private func journeyDictionary(_ update: JourneyUpdate) -> [String: Any] {
    [
      "journeyId": update.journeyId,
      "campaignId": update.campaignId,
      "flowId": update.flowId as Any,
      "exitReason": update.exitReason.rawValue,
      "goalMet": update.goalMet,
      "goalMetAtEpochMillis": update.goalMetAt.map { Int($0.timeIntervalSince1970 * 1000) } as Any,
      "durationSeconds": update.durationSeconds as Any,
      "flowExitReason": update.flowExitReason as Any,
    ]
  }

  private func gateSourceString(_ source: GateSource) -> String {
    switch source {
    case .cache: return "cache"
    case .purchase: return "purchase"
    case .restore: return "restore"
    }
  }

  private func featureAccessDictionary(_ access: FeatureAccess?) -> [String: Any]? {
    guard let access else { return nil }
    return [
      "allowed": access.allowed,
      "unlimited": access.unlimited,
      "balance": access.balance as Any,
      "type": access.type.rawValue,
    ]
  }

  private func featureCheckResultDictionary(_ result: FeatureCheckResult) -> [String: Any] {
    [
      "customerId": result.customerId,
      "featureId": result.featureId,
      "requiredBalance": result.requiredBalance,
      "code": result.code,
      "allowed": result.allowed,
      "unlimited": result.unlimited,
      "balance": result.balance as Any,
      "type": result.type.rawValue,
      "preview": result.preview?.value as Any,
    ]
  }

  private func featureUsageResultDictionary(_ result: FeatureUsageResult) -> [String: Any] {
    var payload: [String: Any] = [
      "success": result.success,
      "featureId": result.featureId,
      "amountUsed": result.amountUsed,
      "message": result.message as Any,
    ]

    if let usage = result.usage {
      payload["usage"] = [
        "current": usage.current,
        "limit": usage.limit as Any,
        "remaining": usage.remaining as Any,
      ]
    }

    return payload
  }

  private func toDictionary<T: Encodable>(_ value: T) -> [String: Any] {
    do {
      let data = try JSONEncoder().encode(value)
      if let object = try JSONSerialization.jsonObject(with: data) as? [String: Any] {
        return object
      }
    } catch {
      return [:]
    }

    return [:]
  }

  private func int(_ value: Any?) -> Int? {
    if let value = value as? Int {
      return value
    }

    if let value = value as? NSNumber {
      return value.intValue
    }

    return nil
  }

  private func int64(_ value: Any?) -> Int64? {
    if let value = value as? Int64 {
      return value
    }

    if let value = value as? NSNumber {
      return value.int64Value
    }

    return nil
  }

  private func double(_ value: Any?) -> Double? {
    if let value = value as? Double {
      return value
    }

    if let value = value as? NSNumber {
      return value.doubleValue
    }

    return nil
  }

  private func timeInterval(_ value: Any?) -> TimeInterval? {
    if let value = value as? TimeInterval {
      return value
    }

    if let value = value as? NSNumber {
      return value.doubleValue
    }

    return nil
  }

  private func parseURL(_ raw: String) -> URL? {
    if raw.hasPrefix("/") {
      return URL(fileURLWithPath: raw)
    }

    return URL(string: raw)
  }
#endif

  private func okResponse(value: Any?) -> String {
    var object: [String: Any] = ["ok": true]
    object["value"] = value
    return serializeJSON(object)
  }

  private func errorResponse(code: String, message: String, nativeStack: String? = nil) -> String {
    var error: [String: Any] = [
      "code": code,
      "message": message,
    ]

    if let nativeStack {
      error["nativeStack"] = nativeStack
    }

    return serializeJSON([
      "ok": false,
      "error": error,
    ])
  }

  private func serializeJSON(_ value: Any) -> String {
    guard JSONSerialization.isValidJSONObject(value) else {
      return "{\"ok\":false,\"error\":{\"code\":\"NATIVE_ERROR\",\"message\":\"Invalid response payload\"}}"
    }

    do {
      let data = try JSONSerialization.data(withJSONObject: value)
      return String(data: data, encoding: .utf8)
        ?? "{\"ok\":false,\"error\":{\"code\":\"NATIVE_ERROR\",\"message\":\"Invalid UTF-8 response\"}}"
    } catch {
      return "{\"ok\":false,\"error\":{\"code\":\"NATIVE_ERROR\",\"message\":\"JSON serialization failed\"}}"
    }
  }

  private func emitEnvelope(type: String, requestId: String? = nil, payload: [String: Any]) {
    var envelope: [String: Any] = [
      "type": type,
      "timestampMs": Int(Date().timeIntervalSince1970 * 1000),
      "payload": payload,
    ]

    if let requestId {
      envelope["requestId"] = requestId
    }

    let json = serializeJSON(envelope)
    sendUnityMessage(json)
  }

  private func sendUnityMessage(_ payload: String) {
    guard let objectCString = callbackObjectName.cString(using: .utf8),
          let methodCString = callbackMethodName.cString(using: .utf8),
          let payloadCString = payload.cString(using: .utf8)
    else {
      return
    }

    objectCString.withUnsafeBufferPointer { objectBuffer in
      methodCString.withUnsafeBufferPointer { methodBuffer in
        payloadCString.withUnsafeBufferPointer { payloadBuffer in
          guard let objectBase = objectBuffer.baseAddress,
                let methodBase = methodBuffer.baseAddress,
                let payloadBase = payloadBuffer.baseAddress
          else {
            return
          }

          UnitySendMessage(objectBase, methodBase, payloadBase)
        }
      }
    }
  }
}

#if canImport(Nuxie)
@MainActor
private final class UnityDelegateBridge: NuxieDelegate {
  private let emit: (String, String?, [String: Any]) -> Void

  init(emit: @escaping (String, String?, [String: Any]) -> Void) {
    self.emit = emit
  }

  func featureAccessDidChange(_ featureId: String, from oldValue: FeatureAccess?, to newValue: FeatureAccess) {
    emit("feature_access_changed", nil, [
      "featureId": featureId,
      "from": featureAccessDictionary(oldValue) as Any,
      "to": featureAccessDictionary(newValue) as Any,
    ])
  }

  private func featureAccessDictionary(_ access: FeatureAccess?) -> [String: Any]? {
    guard let access else { return nil }
    return [
      "allowed": access.allowed,
      "unlimited": access.unlimited,
      "balance": access.balance as Any,
      "type": access.type.rawValue,
    ]
  }
}

private final class UnityPurchaseDelegateBridge: NuxiePurchaseDelegate {
  private let emit: (String, String?, [String: Any]) -> Void
  private let timeoutSeconds: TimeInterval
  private let lock = NSLock()

  private var purchaseContinuations: [String: CheckedContinuation<PurchaseOutcome, Never>] = [:]
  private var restoreContinuations: [String: CheckedContinuation<RestoreResult, Never>] = [:]

  init(
    timeoutSeconds: TimeInterval = 60,
    emit: @escaping (String, String?, [String: Any]) -> Void
  ) {
    self.timeoutSeconds = timeoutSeconds
    self.emit = emit
  }

  func purchase(_ product: any StoreProductProtocol) async -> PurchaseResult {
    await purchaseOutcome(product).result
  }

  func purchaseOutcome(_ product: any StoreProductProtocol) async -> PurchaseOutcome {
    let requestId = UUID().uuidString

    return await withCheckedContinuation { continuation in
      lock.lock()
      purchaseContinuations[requestId] = continuation
      lock.unlock()

      emit("purchase_request", requestId, [
        "requestId": requestId,
        "platform": "ios",
        "productId": product.id,
        "displayName": product.displayName,
        "displayPrice": product.displayPrice,
        "price": NSDecimalNumber(decimal: product.price).doubleValue,
      ])

      schedulePurchaseTimeout(requestId: requestId, fallbackProductId: product.id)
    }
  }

  func restore() async -> RestoreResult {
    let requestId = UUID().uuidString

    return await withCheckedContinuation { continuation in
      lock.lock()
      restoreContinuations[requestId] = continuation
      lock.unlock()

      emit("restore_request", requestId, [
        "requestId": requestId,
        "platform": "ios",
      ])

      scheduleRestoreTimeout(requestId: requestId)
    }
  }

  func completePurchase(requestId: String, payload: [String: Any]) {
    lock.lock()
    let continuation = purchaseContinuations.removeValue(forKey: requestId)
    lock.unlock()

    guard let continuation else { return }
    continuation.resume(returning: purchaseOutcome(from: payload))
  }

  func completeRestore(requestId: String, payload: [String: Any]) {
    lock.lock()
    let continuation = restoreContinuations.removeValue(forKey: requestId)
    lock.unlock()

    guard let continuation else { return }
    continuation.resume(returning: restoreResult(from: payload))
  }

  private func purchaseOutcome(from payload: [String: Any]) -> PurchaseOutcome {
    let type = (payload["type"] as? String)?.lowercased() ?? "failed"
    switch type {
    case "success":
      return PurchaseOutcome(
        result: .success,
        transactionJws: payload["transactionJws"] as? String,
        transactionId: payload["transactionId"] as? String,
        originalTransactionId: payload["originalTransactionId"] as? String,
        productId: payload["productId"] as? String
      )
    case "cancelled":
      return PurchaseOutcome(result: .cancelled, productId: payload["productId"] as? String)
    case "pending":
      return PurchaseOutcome(result: .pending, productId: payload["productId"] as? String)
    default:
      let message = (payload["message"] as? String) ?? "purchase_failed"
      return PurchaseOutcome(result: .failed(bridgeError(message)), productId: payload["productId"] as? String)
    }
  }

  private func restoreResult(from payload: [String: Any]) -> RestoreResult {
    let type = (payload["type"] as? String)?.lowercased() ?? "failed"
    switch type {
    case "success":
      let restoredCount = payload["restoredCount"] as? Int ?? 0
      return .success(restoredCount: restoredCount)
    case "no_purchases":
      return .noPurchases
    default:
      let message = (payload["message"] as? String) ?? "restore_failed"
      return .failed(bridgeError(message))
    }
  }

  private func schedulePurchaseTimeout(requestId: String, fallbackProductId: String) {
    Task {
      try? await Task.sleep(nanoseconds: UInt64(timeoutSeconds * 1_000_000_000))
      lock.lock()
      let continuation = purchaseContinuations.removeValue(forKey: requestId)
      lock.unlock()

      guard let continuation else { return }
      continuation.resume(returning: PurchaseOutcome(result: .failed(bridgeError("purchase_timeout")), productId: fallbackProductId))
    }
  }

  private func scheduleRestoreTimeout(requestId: String) {
    Task {
      try? await Task.sleep(nanoseconds: UInt64(timeoutSeconds * 1_000_000_000))
      lock.lock()
      let continuation = restoreContinuations.removeValue(forKey: requestId)
      lock.unlock()

      guard let continuation else { return }
      continuation.resume(returning: .failed(bridgeError("restore_timeout")))
    }
  }

  private func bridgeError(_ message: String) -> Error {
    NSError(domain: "io.nuxie.unity", code: 1, userInfo: [NSLocalizedDescriptionKey: message])
  }
}
#endif

@_silgen_name("UnitySendMessage")
private func UnitySendMessage(
  _ obj: UnsafePointer<CChar>,
  _ method: UnsafePointer<CChar>,
  _ msg: UnsafePointer<CChar>
)
