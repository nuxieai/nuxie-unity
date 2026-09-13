using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Nuxie.Unity
{
    public enum NuxieEnvironment { Production, Development }
    public enum NuxieLogLevel { Warning, Debug, Info, Error, None }
    public enum NuxieStatusKind { Unconfigured, Configuring, Configured, Failed }
    public enum FeatureStateKind { Unknown, Reconciling, Ready }
    public enum FeatureQueryPolicy { CacheFirst, Remote }
    public enum FeatureType { Boolean, Metered, CreditSystem }
    public sealed class NuxieException : Exception
    {
        public string Code { get; }
        public NuxieException(string code, string message, Exception inner = null) : base(message, inner) { Code = code; }
    }
    public sealed class NuxieOptions
    {
        public string IosApiKey { get; set; }
        public string AndroidApiKey { get; set; }
        public NuxieEnvironment Environment { get; set; }
        public NuxieLogLevel LogLevel { get; set; } = NuxieLogLevel.Warning;
        public string LocaleIdentifier { get; set; }
        public NuxieBilling Billing { get; set; } = NuxieBilling.Native;
    }
    public sealed class IdentityOptions
    {
        public IReadOnlyDictionary<string, object> Properties { get; set; }
        public IReadOnlyDictionary<string, object> PropertiesSetOnce { get; set; }
    }
    public sealed class FeatureQuery
    {
        public string EntityId { get; set; }
        public double RequiredBalance { get; set; } = 1;
        public FeatureQueryPolicy Policy { get; set; }
    }
    public sealed class FeatureCommand
    {
        public string EntityId { get; set; }
        public string OperationId { get; set; }
        public double Quantity { get; set; } = 1;
    }
    public sealed class NuxieStatus
    {
        public NuxieStatusKind State { get; }
        public NuxieException Error { get; }
        public string WrapperVersion => "0.2.0";
        public string NativeVersion { get; }
        public NuxieStatus(NuxieStatusKind state, string nativeVersion = null, NuxieException error = null)
        { State = state; NativeVersion = nativeVersion; Error = error; }
    }
    public sealed class Identity
    {
        public string CustomerId { get; }
        public string AnonymousId { get; }
        public bool IsIdentified { get; }
        public Identity(string customerId, string anonymousId, bool isIdentified)
        { CustomerId = customerId; AnonymousId = anonymousId; IsIdentified = isIdentified; }
    }
    public sealed class FeatureAccess : IEquatable<FeatureAccess>
    {
        public bool Allowed { get; }
        public bool Unlimited { get; }
        public double? Balance { get; }
        public FeatureType Type { get; }
        public FeatureAccess(bool allowed, bool unlimited, double? balance, FeatureType type)
        { Allowed = allowed; Unlimited = unlimited; Balance = balance; Type = type; }
        public bool Equals(FeatureAccess other) => other != null && Allowed == other.Allowed && Unlimited == other.Unlimited && Balance == other.Balance && Type == other.Type;
        public override bool Equals(object other) => Equals(other as FeatureAccess);
        public override int GetHashCode() => (Allowed, Unlimited, Balance, Type).GetHashCode();
    }
    public sealed class FeatureSnapshot
    {
        public FeatureStateKind State { get; }
        public ulong IdentityGeneration { get; }
        public ulong Revision { get; }
        public IReadOnlyDictionary<string, FeatureAccess> All { get; }
        public FeatureSnapshot(FeatureStateKind state, ulong generation, ulong revision, IDictionary<string, FeatureAccess> all)
        { State = state; IdentityGeneration = generation; Revision = revision; All = new ReadOnlyDictionary<string, FeatureAccess>(new Dictionary<string, FeatureAccess>(all)); }
        internal static FeatureSnapshot Empty(ulong generation = 0) => new FeatureSnapshot(FeatureStateKind.Unknown, generation, 0, new Dictionary<string, FeatureAccess>());
    }
    public sealed class FeatureState
    {
        public FeatureStateKind State { get; }
        public FeatureAccess Access { get; }
        public FeatureState(FeatureStateKind state, FeatureAccess access) { State = state; Access = access; }
    }
    public sealed class FeatureConsumption
    {
        public string CustomerId { get; }
        public string FeatureId { get; }
        public string OperationId { get; }
        public double? OccurredAtMs { get; }
        public double Quantity { get; }
        public bool Accepted { get; }
        public string Code { get; }
        public double? Balance { get; }
        public bool Unlimited { get; }
        public bool Active { get; }
        public bool IdempotentReplay { get; }
        public FeatureConsumption(string customerId, string featureId, string operationId, double? occurredAtMs,
            double quantity, bool accepted, string code, double? balance, bool unlimited, bool active, bool replay)
        { CustomerId = customerId; FeatureId = featureId; OperationId = operationId; OccurredAtMs = occurredAtMs; Quantity = quantity; Accepted = accepted; Code = code; Balance = balance; Unlimited = unlimited; Active = active; IdempotentReplay = replay; }
    }
    public sealed class ExperienceRef
    {
        public string ExperienceId { get; }
        public string ExperienceVersion { get; }
        public string JourneyId { get; }
        public ExperienceRef(string id, string version, string journey) { ExperienceId = id; ExperienceVersion = version; JourneyId = journey; }
    }
    public sealed class AppAction
    {
        public string Name { get; }
        public IReadOnlyDictionary<string, object> Payload { get; }
        public ExperienceRef Experience { get; }
        internal AppAction(string name, IReadOnlyDictionary<string, object> payload, ExperienceRef experience) { Name = name; Payload = payload; Experience = experience; }
    }
    public sealed class NuxieActivity
    {
        public string Id { get; }
        public string Name { get; }
        public int SchemaVersion { get; }
        public double TimestampMs { get; }
        public double ReceivedAtMs { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        internal NuxieActivity(string id, string name, int schema, double time, double received, IReadOnlyDictionary<string, object> properties)
        { Id = id; Name = name; SchemaVersion = schema; TimestampMs = time; ReceivedAtMs = received; Properties = properties; }
    }
    public sealed class StoreProduct
    {
        public string Platform { get; }
        public string ProductId { get; }
        public string StoreProductId { get; }
        public string BasePlanId { get; }
        public string OfferId { get; }
        public string PurchaseOptionId { get; }
        public string PlacementId { get; }
        public string DisplayName { get; }
        public string DisplayPrice { get; }
        public string EligibilityJws { get; }
        /// <summary>Immutable native-selected product metadata, including pricingPhases and introductoryTerms.</summary>
        public IReadOnlyDictionary<string, object> Details { get; }
        internal StoreProduct(IReadOnlyDictionary<string, object> details)
        {
            Details = details;
            Platform = Text("platform"); ProductId = Text("productId"); StoreProductId = Text("storeProductId");
            BasePlanId = Text("basePlanId"); OfferId = Text("offerId"); PurchaseOptionId = Text("purchaseOptionId");
            PlacementId = Text("placementId"); DisplayName = Text("displayName"); DisplayPrice = Text("displayPrice"); EligibilityJws = Text("eligibilityJws");
        }
        private string Text(string key) => Details.TryGetValue(key, out var value) ? value as string : null;
    }
    public enum PurchaseOutcome { Purchased, Cancelled, Pending, Failed }
    public enum RestoreOutcome { Restored, NoPurchases, Failed }
    public sealed class PurchaseResult
    {
        public PurchaseOutcome Outcome { get; }
        public string Message { get; }
        public PurchaseResult(PurchaseOutcome outcome, string message = null) { Outcome = outcome; Message = message; }
    }
    public sealed class RestoreResult
    {
        public RestoreOutcome Outcome { get; }
        public string Message { get; }
        public RestoreResult(RestoreOutcome outcome, string message = null) { Outcome = outcome; Message = message; }
    }
    public interface INuxiePurchaseController
    {
        Task<PurchaseResult> PurchaseAsync(StoreProduct product);
        Task<RestoreResult> RestorePurchasesAsync();
    }
    public sealed class NuxieBilling
    {
        public static NuxieBilling Native { get; } = new NuxieBilling(null);
        internal INuxiePurchaseController Controller { get; }
        private NuxieBilling(INuxiePurchaseController controller) { Controller = controller; }
        public static NuxieBilling External(INuxiePurchaseController controller) => new NuxieBilling(controller ?? throw new ArgumentNullException(nameof(controller)));
    }
}
