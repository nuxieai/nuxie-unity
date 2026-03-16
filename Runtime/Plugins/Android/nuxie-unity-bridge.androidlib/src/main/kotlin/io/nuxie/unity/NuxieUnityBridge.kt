package io.nuxie.unity

import com.unity3d.player.UnityPlayer
import io.nuxie.sdk.campaigns.Campaign
import io.nuxie.sdk.NuxieDelegate
import io.nuxie.sdk.NuxieSDK
import io.nuxie.sdk.config.Environment
import io.nuxie.sdk.config.EventLinkingPolicy
import io.nuxie.sdk.config.LogLevel
import io.nuxie.sdk.config.NuxieConfiguration
import io.nuxie.sdk.features.FeatureAccess
import io.nuxie.sdk.features.FeatureCheckResult
import io.nuxie.sdk.features.FeatureType
import io.nuxie.sdk.features.FeatureUsageResult
import io.nuxie.sdk.flows.RemoteFlow
import io.nuxie.sdk.network.models.ProfileResponse
import io.nuxie.sdk.purchases.NuxiePurchaseDelegate
import io.nuxie.sdk.purchases.PurchaseOutcome
import io.nuxie.sdk.purchases.PurchaseResult
import io.nuxie.sdk.purchases.RestoreResult
import io.nuxie.sdk.triggers.EntitlementUpdate
import io.nuxie.sdk.triggers.GateSource
import io.nuxie.sdk.triggers.JourneyExitReason
import io.nuxie.sdk.triggers.JourneyRef
import io.nuxie.sdk.triggers.JourneyUpdate
import io.nuxie.sdk.triggers.SuppressReason
import io.nuxie.sdk.triggers.TriggerDecision
import io.nuxie.sdk.triggers.TriggerHandle
import io.nuxie.sdk.triggers.TriggerUpdate
import java.util.UUID
import java.util.concurrent.ConcurrentHashMap
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.withTimeout
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.doubleOrNull
import kotlinx.serialization.json.longOrNull
import org.json.JSONArray
import org.json.JSONObject

object NuxieUnityBridge {
  private val sdk: NuxieSDK = NuxieSDK.shared()
  private val triggerHandles = ConcurrentHashMap<String, TriggerHandle>()
  private val purchaseBridge = UnityPurchaseDelegateBridge(::emitEnvelope)

  private var callbackObjectName: String = "__NuxieBridgeHost"
  private var callbackMethodName: String = "OnNuxieNativeEvent"

  @JvmStatic
  fun invoke(
    method: String,
    argsJson: String,
    callbackObjectName: String,
    callbackMethodName: String,
  ): String {
    this.callbackObjectName = callbackObjectName
    this.callbackMethodName = callbackMethodName

    return runCatching {
      val args = jsonToMap(argsJson)
      when (method) {
        "configure" -> {
          val apiKey = args["apiKey"] as? String
            ?: return@runCatching errorResponse("MISSING_API_KEY", "Nuxie API key is required")
          if (apiKey.isBlank()) {
            return@runCatching errorResponse("MISSING_API_KEY", "Nuxie API key is required")
          }

          val context = UnityPlayer.currentActivity?.applicationContext
            ?: return@runCatching errorResponse("NATIVE_ERROR", "Unity activity is unavailable")
          val options = args["options"] as? Map<String, Any?>
          val usingPurchaseController = args["usingPurchaseController"] as? Boolean ?: false

          val config = buildConfiguration(apiKey, options, usingPurchaseController)
          sdk.delegate = object : NuxieDelegate {
            override fun featureAccessDidChange(featureId: String, from: FeatureAccess?, to: FeatureAccess) {
              emitEnvelope(
                type = "feature_access_changed",
                requestId = null,
                payload = mapOf(
                  "featureId" to featureId,
                  "from" to from?.toMap(),
                  "to" to to.toMap(),
                ),
              )
            }

            override fun flowDismissed(
              journeyId: String,
              campaignId: String?,
              screenId: String?,
              reason: String,
              error: String?,
            ) {
              emitEnvelope(
                type = "flow_dismissed",
                requestId = null,
                payload = mapOf(
                  "journeyId" to journeyId,
                  "campaignId" to campaignId,
                  "screenId" to screenId,
                  "reason" to reason,
                  "error" to error,
                ),
              )
            }
          }

          sdk.setup(context, config)
          okResponse(null)
        }

        "shutdown" -> {
          runBlocking { sdk.shutdown() }
          triggerHandles.clear()
          okResponse(null)
        }

        "identify" -> {
          val distinctId = args["distinctId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "identify requires distinctId")
          sdk.identify(
            distinctId = distinctId,
            userProperties = args["userProperties"] as? Map<String, Any?>,
            userPropertiesSetOnce = args["userPropertiesSetOnce"] as? Map<String, Any?>,
          )
          okResponse(null)
        }

        "reset" -> {
          val keepAnonymousId = args["keepAnonymousId"] as? Boolean ?: true
          sdk.reset(keepAnonymousId = keepAnonymousId)
          okResponse(null)
        }

        "getDistinctId" -> okResponse(sdk.getDistinctId())
        "getAnonymousId" -> okResponse(sdk.getAnonymousId())
        "getIsIdentified" -> okResponse(sdk.isIdentified)

        "startTrigger" -> {
          val requestId = args["requestId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "startTrigger requires requestId")
          val eventName = args["eventName"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "startTrigger requires eventName")
          val options = args["options"] as? Map<String, Any?>

          val handle = sdk.trigger(
            event = eventName,
            properties = options?.get("properties") as? Map<String, Any?>,
            userProperties = options?.get("userProperties") as? Map<String, Any?>,
            userPropertiesSetOnce = options?.get("userPropertiesSetOnce") as? Map<String, Any?>,
          ) { update ->
            val terminal = update.isTerminal()
            emitEnvelope(
              type = "trigger_update",
              requestId = requestId,
              payload = mapOf(
                "update" to update.toMap(),
                "isTerminal" to terminal,
              ),
            )

            if (terminal) {
              triggerHandles.remove(requestId)
            }
          }

          triggerHandles[requestId] = handle
          okResponse(null)
        }

        "cancelTrigger" -> {
          val requestId = args["requestId"] as? String ?: ""
          triggerHandles.remove(requestId)?.cancel()
          okResponse(null)
        }

        "showFlow" -> {
          val flowId = args["flowId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "showFlow requires flowId")
          sdk.showFlow(flowId)
          emitEnvelope(
            type = "flow_presented",
            requestId = null,
            payload = mapOf("flowId" to flowId),
          )
          okResponse(null)
        }

        "refreshProfile" -> {
          val profile = runBlocking { sdk.refreshProfile() }
          okResponse(profile.toMap())
        }

        "hasFeature" -> {
          val featureId = args["featureId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "hasFeature requires featureId")
          val requiredBalance = (args["requiredBalance"] as? Number)?.toInt()
          val entityId = args["entityId"] as? String
          val access = runBlocking {
            if (requiredBalance != null) {
              sdk.hasFeature(featureId = featureId, requiredBalance = requiredBalance, entityId = entityId)
            } else {
              sdk.hasFeature(featureId)
            }
          }
          okResponse(access.toMap())
        }

        "getCachedFeature" -> {
          val featureId = args["featureId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "getCachedFeature requires featureId")
          val entityId = args["entityId"] as? String
          val access = runBlocking { sdk.getCachedFeature(featureId = featureId, entityId = entityId) }
          okResponse(access?.toMap())
        }

        "checkFeature" -> {
          val featureId = args["featureId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "checkFeature requires featureId")
          val requiredBalance = (args["requiredBalance"] as? Number)?.toInt()
          val entityId = args["entityId"] as? String
          val result = runBlocking { sdk.checkFeature(featureId = featureId, requiredBalance = requiredBalance, entityId = entityId) }
          okResponse(result.toMap())
        }

        "refreshFeature" -> {
          val featureId = args["featureId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "refreshFeature requires featureId")
          val requiredBalance = (args["requiredBalance"] as? Number)?.toInt()
          val entityId = args["entityId"] as? String
          val result = runBlocking { sdk.refreshFeature(featureId = featureId, requiredBalance = requiredBalance, entityId = entityId) }
          okResponse(result.toMap())
        }

        "useFeature" -> {
          val featureId = args["featureId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "useFeature requires featureId")
          val amount = (args["amount"] as? Number)?.toDouble() ?: 1.0
          val entityId = args["entityId"] as? String
          val metadata = args["metadata"] as? Map<String, Any?>
          sdk.useFeature(featureId = featureId, amount = amount, entityId = entityId, metadata = metadata)
          okResponse(null)
        }

        "useFeatureAndWait" -> {
          val featureId = args["featureId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "useFeatureAndWait requires featureId")
          val amount = (args["amount"] as? Number)?.toDouble() ?: 1.0
          val entityId = args["entityId"] as? String
          val setUsage = args["setUsage"] as? Boolean ?: false
          val metadata = args["metadata"] as? Map<String, Any?>

          val result = runBlocking {
            sdk.useFeatureAndWait(
              featureId = featureId,
              amount = amount,
              entityId = entityId,
              setUsage = setUsage,
              metadata = metadata,
            )
          }

          okResponse(result.toMap())
        }

        "flushEvents" -> {
          val success = runBlocking { sdk.flushEvents() }
          okResponse(success)
        }

        "getQueuedEventCount" -> {
          val count = runBlocking { sdk.getQueuedEventCount() }
          okResponse(count)
        }

        "pauseEventQueue" -> {
          runBlocking { sdk.pauseEventQueue() }
          okResponse(null)
        }

        "resumeEventQueue" -> {
          runBlocking { sdk.resumeEventQueue() }
          okResponse(null)
        }

        "completePurchase" -> {
          val requestId = args["requestId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "completePurchase requires requestId")
          val result = args["result"] as? Map<String, Any?>
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "completePurchase requires result")
          purchaseBridge.completePurchase(requestId, result)
          okResponse(null)
        }

        "completeRestore" -> {
          val requestId = args["requestId"] as? String
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "completeRestore requires requestId")
          val result = args["result"] as? Map<String, Any?>
            ?: return@runCatching errorResponse("INVALID_CONFIGURATION", "completeRestore requires result")
          purchaseBridge.completeRestore(requestId, result)
          okResponse(null)
        }

        else -> errorResponse("NATIVE_ERROR", "Unsupported method '$method'")
      }
    }.getOrElse { error ->
      errorResponse("NATIVE_ERROR", error.message ?: "Native bridge failure", error.stackTraceToString())
    }
  }

  private fun buildConfiguration(apiKey: String, options: Map<String, Any?>?, usePurchaseController: Boolean): NuxieConfiguration {
    val config = NuxieConfiguration(apiKey)

    if (options == null) {
      if (usePurchaseController) {
        config.purchaseDelegate = purchaseBridge
      }
      return config
    }

    config.environment = when (options["environment"] as? String) {
      "production" -> Environment.PRODUCTION
      "staging" -> Environment.STAGING
      "development" -> Environment.DEVELOPMENT
      "custom" -> Environment.CUSTOM
      else -> config.environment
    }

    (options["apiEndpoint"] as? String)?.let(config::setApiEndpoint)

    config.logLevel = when (options["logLevel"] as? String) {
      "verbose" -> LogLevel.VERBOSE
      "debug" -> LogLevel.DEBUG
      "info" -> LogLevel.INFO
      "warning" -> LogLevel.WARNING
      "error" -> LogLevel.ERROR
      "none" -> LogLevel.NONE
      else -> config.logLevel
    }

    config.enableConsoleLogging = options["enableConsoleLogging"] as? Boolean ?: config.enableConsoleLogging
    config.enableFileLogging = options["enableFileLogging"] as? Boolean ?: config.enableFileLogging
    config.redactSensitiveData = options["redactSensitiveData"] as? Boolean ?: config.redactSensitiveData
    config.requestTimeoutSeconds = (options["requestTimeoutSeconds"] as? Number)?.toLong() ?: config.requestTimeoutSeconds
    config.retryCount = (options["retryCount"] as? Number)?.toInt() ?: config.retryCount
    config.retryDelaySeconds = (options["retryDelaySeconds"] as? Number)?.toLong() ?: config.retryDelaySeconds
    config.syncIntervalSeconds = (options["syncIntervalSeconds"] as? Number)?.toLong() ?: config.syncIntervalSeconds
    config.enableCompression = options["enableCompression"] as? Boolean ?: config.enableCompression
    config.eventBatchSize = (options["eventBatchSize"] as? Number)?.toInt() ?: config.eventBatchSize
    config.flushAt = (options["flushAt"] as? Number)?.toInt() ?: config.flushAt
    config.flushIntervalSeconds = (options["flushIntervalSeconds"] as? Number)?.toLong() ?: config.flushIntervalSeconds
    config.maxQueueSize = (options["maxQueueSize"] as? Number)?.toInt() ?: config.maxQueueSize
    config.maxCacheSizeBytes = (options["maxCacheSizeBytes"] as? Number)?.toLong() ?: config.maxCacheSizeBytes
    config.cacheExpirationSeconds = (options["cacheExpirationSeconds"] as? Number)?.toLong() ?: config.cacheExpirationSeconds
    config.enableEncryption = options["enableEncryption"] as? Boolean ?: config.enableEncryption
    config.customStoragePath = options["customStoragePath"] as? String ?: config.customStoragePath
    config.featureCacheTtlSeconds = (options["featureCacheTtlSeconds"] as? Number)?.toLong() ?: config.featureCacheTtlSeconds
    config.defaultPaywallTimeoutSeconds = (options["defaultPaywallTimeoutSeconds"] as? Number)?.toLong() ?: config.defaultPaywallTimeoutSeconds
    config.respectDoNotTrack = options["respectDoNotTrack"] as? Boolean ?: config.respectDoNotTrack
    config.localeIdentifier = options["localeIdentifier"] as? String ?: config.localeIdentifier
    config.isDebugMode = options["isDebugMode"] as? Boolean ?: config.isDebugMode
    config.enablePlugins = options["enablePlugins"] as? Boolean ?: config.enablePlugins
    config.maxFlowCacheSizeBytes = (options["maxFlowCacheSizeBytes"] as? Number)?.toLong() ?: config.maxFlowCacheSizeBytes
    config.flowCacheExpirationSeconds = (options["flowCacheExpirationSeconds"] as? Number)?.toLong() ?: config.flowCacheExpirationSeconds
    config.maxConcurrentFlowDownloads = (options["maxConcurrentFlowDownloads"] as? Number)?.toInt() ?: config.maxConcurrentFlowDownloads
    config.flowDownloadTimeoutSeconds = (options["flowDownloadTimeoutSeconds"] as? Number)?.toLong() ?: config.flowDownloadTimeoutSeconds
    config.flowCacheDirectory = options["flowCacheDirectory"] as? String ?: config.flowCacheDirectory

    config.eventLinkingPolicy = when (options["eventLinkingPolicy"] as? String) {
      "keep_separate", "keepSeparate" -> EventLinkingPolicy.KEEP_SEPARATE
      else -> EventLinkingPolicy.MIGRATE_ON_IDENTIFY
    }

    if (usePurchaseController) {
      config.purchaseDelegate = purchaseBridge
    }

    return config
  }

  private fun emitEnvelope(type: String, requestId: String?, payload: Map<String, Any?>) {
    val envelope = mutableMapOf<String, Any?>(
      "type" to type,
      "timestampMs" to System.currentTimeMillis(),
      "payload" to payload,
    )
    if (requestId != null) {
      envelope["requestId"] = requestId
    }

    val message = JSONObject(envelope).toString()
    runCatching {
      UnityPlayer.UnitySendMessage(callbackObjectName, callbackMethodName, message)
    }
  }

  private fun okResponse(value: Any?): String {
    return JSONObject(mapOf("ok" to true, "value" to value)).toString()
  }

  private fun errorResponse(code: String, message: String, nativeStack: String? = null): String {
    val error = mutableMapOf<String, Any?>(
      "code" to code,
      "message" to message,
    )
    if (nativeStack != null) {
      error["nativeStack"] = nativeStack
    }

    return JSONObject(
      mapOf(
        "ok" to false,
        "error" to error,
      ),
    ).toString()
  }

  private fun jsonToMap(json: String): Map<String, Any?> {
    if (json.isBlank()) return emptyMap()
    val obj = JSONObject(json)
    return obj.toMap()
  }
}

private class UnityPurchaseDelegateBridge(
  private val emit: (type: String, requestId: String?, payload: Map<String, Any?>) -> Unit,
  private val timeoutMs: Long = 60_000,
) : NuxiePurchaseDelegate {
  private val purchaseRequests = ConcurrentHashMap<String, CompletableDeferred<PurchaseOutcome>>()
  private val restoreRequests = ConcurrentHashMap<String, CompletableDeferred<RestoreResult>>()

  override suspend fun purchase(productId: String): PurchaseResult {
    return purchaseOutcome(productId).result
  }

  override suspend fun purchaseOutcome(productId: String): PurchaseOutcome {
    val requestId = UUID.randomUUID().toString()
    val deferred = CompletableDeferred<PurchaseOutcome>()
    purchaseRequests[requestId] = deferred

    emit(
      type = "purchase_request",
      requestId = requestId,
      payload = mapOf(
        "requestId" to requestId,
        "platform" to "android",
        "productId" to productId,
      ),
    )

    return try {
      withTimeout(timeoutMs) { deferred.await() }
    } catch (_: Throwable) {
      PurchaseOutcome(PurchaseResult.Failed("purchase_timeout"), productId = productId)
    } finally {
      purchaseRequests.remove(requestId)
    }
  }

  override suspend fun restore(): RestoreResult {
    val requestId = UUID.randomUUID().toString()
    val deferred = CompletableDeferred<RestoreResult>()
    restoreRequests[requestId] = deferred

    emit(
      type = "restore_request",
      requestId = requestId,
      payload = mapOf(
        "requestId" to requestId,
        "platform" to "android",
      ),
    )

    return try {
      withTimeout(timeoutMs) { deferred.await() }
    } catch (_: Throwable) {
      RestoreResult.Failed("restore_timeout")
    } finally {
      restoreRequests.remove(requestId)
    }
  }

  fun completePurchase(requestId: String, payload: Map<String, Any?>) {
    val deferred = purchaseRequests.remove(requestId) ?: return
    deferred.complete(parsePurchaseOutcome(payload))
  }

  fun completeRestore(requestId: String, payload: Map<String, Any?>) {
    val deferred = restoreRequests.remove(requestId) ?: return
    deferred.complete(parseRestoreResult(payload))
  }

  private fun parsePurchaseOutcome(payload: Map<String, Any?>): PurchaseOutcome {
    return when ((payload["type"] as? String)?.lowercase()) {
      "success" -> PurchaseOutcome(
        result = PurchaseResult.Success,
        productId = payload["productId"] as? String,
        purchaseToken = payload["purchaseToken"] as? String,
        orderId = payload["orderId"] as? String,
      )

      "cancelled" -> PurchaseOutcome(
        result = PurchaseResult.Cancelled,
        productId = payload["productId"] as? String,
      )

      "pending" -> PurchaseOutcome(
        result = PurchaseResult.Pending,
        productId = payload["productId"] as? String,
      )

      else -> PurchaseOutcome(
        result = PurchaseResult.Failed((payload["message"] as? String) ?: "purchase_failed"),
        productId = payload["productId"] as? String,
      )
    }
  }

  private fun parseRestoreResult(payload: Map<String, Any?>): RestoreResult {
    return when ((payload["type"] as? String)?.lowercase()) {
      "success" -> {
        val restoredCount = when (val raw = payload["restoredCount"]) {
          is Int -> raw
          is Long -> raw.toInt()
          is Double -> raw.toInt()
          else -> 0
        }
        RestoreResult.Success(restoredCount)
      }

      "no_purchases" -> RestoreResult.NoPurchases
      else -> RestoreResult.Failed((payload["message"] as? String) ?: "restore_failed")
    }
  }
}

private fun TriggerUpdate.isTerminal(): Boolean {
  return when (this) {
    is TriggerUpdate.Error -> true
    is TriggerUpdate.Journey -> true
    is TriggerUpdate.Decision -> when (decision) {
      TriggerDecision.NoMatch,
      TriggerDecision.AllowedImmediate,
      TriggerDecision.DeniedImmediate,
      is TriggerDecision.Suppressed,
      -> true

      else -> false
    }

    is TriggerUpdate.Entitlement -> when (entitlement) {
      is EntitlementUpdate.Allowed,
      EntitlementUpdate.Denied,
      -> true

      EntitlementUpdate.Pending -> false
    }
  }
}

private fun TriggerUpdate.toMap(): Map<String, Any?> {
  return when (this) {
    is TriggerUpdate.Decision -> mapOf("kind" to "decision", "decision" to decision.toMap())
    is TriggerUpdate.Entitlement -> mapOf("kind" to "entitlement", "entitlement" to entitlement.toMap())
    is TriggerUpdate.Journey -> mapOf("kind" to "journey", "journey" to journey.toMap())
    is TriggerUpdate.Error -> mapOf(
      "kind" to "error",
      "error" to mapOf("code" to error.code, "message" to error.message),
    )
  }
}

private fun TriggerDecision.toMap(): Map<String, Any?> {
  return when (this) {
    TriggerDecision.NoMatch -> mapOf("type" to "no_match")
    TriggerDecision.AllowedImmediate -> mapOf("type" to "allowed_immediate")
    TriggerDecision.DeniedImmediate -> mapOf("type" to "denied_immediate")
    is TriggerDecision.JourneyStarted -> mapOf("type" to "journey_started", "ref" to ref.toMap())
    is TriggerDecision.JourneyResumed -> mapOf("type" to "journey_resumed", "ref" to ref.toMap())
    is TriggerDecision.FlowShown -> mapOf("type" to "flow_shown", "ref" to ref.toMap())
    is TriggerDecision.Suppressed -> mapOf("type" to "suppressed", "reason" to reason.toMap())
  }
}

private fun JourneyRef.toMap(): Map<String, Any?> {
  return mapOf(
    "journeyId" to journeyId,
    "campaignId" to campaignId,
    "flowId" to flowId,
  )
}

private fun SuppressReason.toMap(): Map<String, Any?> {
  return when (this) {
    SuppressReason.AlreadyActive -> mapOf("reason" to "already_active")
    SuppressReason.ReentryLimited -> mapOf("reason" to "reentry_limited")
    SuppressReason.Holdout -> mapOf("reason" to "holdout")
    SuppressReason.NoFlow -> mapOf("reason" to "no_flow")
    is SuppressReason.Unknown -> mapOf("reason" to "unknown", "rawReason" to value)
  }
}

private fun EntitlementUpdate.toMap(): Map<String, Any?> {
  return when (this) {
    EntitlementUpdate.Pending -> mapOf("type" to "pending")
    EntitlementUpdate.Denied -> mapOf("type" to "denied")
    is EntitlementUpdate.Allowed -> mapOf("type" to "allowed", "source" to source.toMap())
  }
}

private fun GateSource.toMap(): String {
  return when (this) {
    GateSource.CACHE -> "cache"
    GateSource.PURCHASE -> "purchase"
    GateSource.RESTORE -> "restore"
  }
}

private fun JourneyUpdate.toMap(): Map<String, Any?> {
  return mapOf(
    "journeyId" to journeyId,
    "campaignId" to campaignId,
    "flowId" to flowId,
    "exitReason" to exitReason.toMap(),
    "goalMet" to goalMet,
    "goalMetAtEpochMillis" to goalMetAtEpochMillis,
    "durationSeconds" to durationSeconds,
    "flowExitReason" to flowExitReason,
  )
}

private fun JourneyExitReason.toMap(): String {
  return when (this) {
    JourneyExitReason.COMPLETED -> "completed"
    JourneyExitReason.DISMISSED -> "dismissed"
    JourneyExitReason.GOAL_MET -> "goal_met"
    JourneyExitReason.TRIGGER_UNMATCHED -> "trigger_unmatched"
    JourneyExitReason.EXPIRED -> "expired"
    JourneyExitReason.ERROR -> "error"
    JourneyExitReason.CANCELLED -> "cancelled"
  }
}

private fun FeatureType.toJsValue(): String {
  return when (this) {
    FeatureType.BOOLEAN -> "boolean"
    FeatureType.METERED -> "metered"
    FeatureType.CREDIT_SYSTEM -> "creditSystem"
  }
}

private fun FeatureAccess.toMap(): Map<String, Any?> {
  return mapOf(
    "allowed" to allowed,
    "unlimited" to unlimited,
    "balance" to balance,
    "type" to type.toJsValue(),
  )
}

private fun FeatureCheckResult.toMap(): Map<String, Any?> {
  return mapOf(
    "customerId" to customerId,
    "featureId" to featureId,
    "requiredBalance" to requiredBalance,
    "code" to code,
    "allowed" to allowed,
    "unlimited" to unlimited,
    "balance" to balance,
    "type" to type.toJsValue(),
    "preview" to preview?.toJsValue(),
  )
}

private fun FeatureUsageResult.toMap(): Map<String, Any?> {
  return mapOf(
    "success" to success,
    "featureId" to featureId,
    "amountUsed" to amountUsed,
    "message" to message,
    "usage" to usage?.let {
      mapOf(
        "current" to it.current,
        "limit" to it.limit,
        "remaining" to it.remaining,
      )
    },
  )
}

private fun ProfileResponse.toMap(): Map<String, Any?> {
  return mapOf(
    "campaigns" to campaigns.map { it.toMap() },
    "segments" to segments.map { mapOf("id" to it.id, "name" to it.name) },
    "flows" to flows.map { it.toMap() },
    "userProperties" to userProperties?.toJsValue(),
    "experiments" to experiments?.mapValues { (_, assignment) ->
      mapOf(
        "experimentKey" to assignment.experimentKey,
        "variantKey" to assignment.variantKey,
        "status" to assignment.status,
        "isHoldout" to assignment.isHoldout,
      )
    },
    "features" to (
      features?.map {
        mapOf(
          "id" to it.id,
          "type" to it.type.toJsValue(),
          "balance" to it.balance,
          "unlimited" to it.unlimited,
          "nextResetAt" to it.nextResetAt,
          "interval" to it.interval,
          "entities" to it.entities?.mapValues { (_, balance) -> mapOf("balance" to balance.balance) },
        )
      } ?: emptyList<Map<String, Any?>>()
      ),
    "journeys" to (
      journeys?.map {
        mapOf(
          "sessionId" to it.sessionId,
          "campaignId" to it.campaignId,
          "currentNodeId" to it.currentNodeId,
          "context" to it.context.toJsValue(),
        )
      } ?: emptyList<Map<String, Any?>>()
      ),
  )
}

private fun Campaign.toMap(): Map<String, Any?> {
  return mapOf(
    "id" to id,
    "name" to name,
    "flowId" to flowId,
    "flowNumber" to flowNumber,
    "flowName" to flowName,
    "publishedAt" to publishedAt,
    "campaignType" to campaignType,
  )
}

private fun RemoteFlow.toMap(): Map<String, Any?> {
  return mapOf("id" to id)
}

private fun JsonElement.toJsValue(): Any? {
  return when (this) {
    is JsonNull -> null
    is JsonArray -> map { it.toJsValue() }
    is JsonObject -> entries.associate { (key, value) -> key to value.toJsValue() }
    is JsonPrimitive -> {
      when {
        isString -> content
        booleanOrNull != null -> booleanOrNull
        longOrNull != null -> longOrNull
        doubleOrNull != null -> doubleOrNull
        else -> content
      }
    }
  }
}

private fun JSONObject.toMap(): Map<String, Any?> {
  val map = mutableMapOf<String, Any?>()
  val keys = keys()
  while (keys.hasNext()) {
    val key = keys.next()
    map[key] = get(key).toKotlinValue()
  }
  return map
}

private fun Any?.toKotlinValue(): Any? {
  return when (this) {
    is JSONObject -> toMap()
    is JSONArray -> (0 until length()).map { get(it).toKotlinValue() }
    JSONObject.NULL -> null
    else -> this
  }
}
