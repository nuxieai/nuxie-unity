# Public API

Namespace `Nuxie.Unity`; `Nuxie.Client` implements `INuxieClient`. Configuration/input objects use setters and are copied at invocation; returned models and collections are immutable.

| Member | Result and meaning |
| --- | --- |
| Status | Setup state, native/wrapper versions, typed setup error. |
| Features | Global snapshot: Unknown, Reconciling, Ready; ulong identity generation/revision and Feature map. Reading does not fetch. |
| ConfigureAsync(NuxieOptions) | Shared asynchronous setup; conflicting options require shutdown. |
| GetIdentityAsync() | Coherent CustomerId, AnonymousId and IsIdentified. |
| IdentifyAsync(customerId, IdentityOptions) | Identity transition; optional properties/set-once properties. Concurrent identity transitions reject with identityBusy. |
| ResetAsync() | Rotate to anonymous identity and invalidate old observations. |
| SetLocaleAsync(locale) | Null restores device locale. Native profile synchronization applies the change. |
| TriggerAsync(eventName, properties) | Native event acceptance, not Journey completion. |
| DismissAsync() | Native presentation dismissal completion. |
| HasFeatureAsync(featureId, FeatureQuery) | CacheFirst or Remote policy, optional entity, positive integer RequiredBalance. |
| ConsumeFeatureAsync(featureId, FeatureCommand) | Durable command with explicit OperationId, Quantity and optional EntityId. Check Accepted. |
| RestorePurchasesAsync() | Restored, NoPurchases or Failed. |
| ShutdownAsync() | Explicit native shutdown; scene destruction does not invoke it. |
| SubscribeStatus / SubscribeFeatures | IDisposable subscription; immediate current state and subsequent updates. |
| ObserveFeature(featureId, observer) | IDisposable selected observation; suppresses unrelated Feature updates. |
| SubscribeActivity / SubscribeAppAction / SubscribeErrors | IDisposable main-thread notifications. Listener exceptions are isolated. |

Feature access: Allowed, Unlimited, nullable Balance, Type (Boolean, Metered, CreditSystem). Unknown does not mean denied. Ready can be empty. Entity queries use explicitly assigned grants and do not mutate the global snapshot. Delayed reads after an identity/session change fail as staleOperation.

Consumption receipt: CustomerId, FeatureId, OperationId, nullable OccurredAtMs, Quantity, Accepted, Code, nullable Balance, Unlimited, Active, IdempotentReplay. Preserve operation IDs across retries; Accepted may be true when Active is false after the last unit. RequiredBalance and Quantity must be integers from 1 through 2^53−1. Balances and optional timestamps use double. Generations and revisions preserve ulong precision using decimal strings across the private bridge.

Use `NuxieBilling.External(controller)` for existing checkout ownership. INuxiePurchaseController exposes Task<PurchaseResult> PurchaseAsync(StoreProduct) and Task<RestoreResult> RestorePurchasesAsync(). Results preserve Purchased/Cancelled/Pending/Failed and Restored/NoPurchases/Failed. StoreProduct includes exact product/offer IDs, placement, display fields, eligibility evidence and immutable Details containing native pricing phases and introductory terms. The native delegate enforces timeout and exactly-once settlement. Do not grant access just because checkout started.

NuxieException.Code distinguishes invalidArgument, notConfigured, alreadyConfigured, wrongThread, unsupportedPlatform, staleOperation, identityBusy and native errors. No public cancellation claims to undo a durable debit. A scene can stop observing while native work continues; retry uncertain commands with the original ID.
