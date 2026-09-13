using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Nuxie.Unity
{
    public interface INuxieClient
    {
        NuxieStatus Status { get; }
        FeatureSnapshot Features { get; }
        Task ConfigureAsync(NuxieOptions options);
        Task ShutdownAsync();
        Task<Identity> GetIdentityAsync();
        Task IdentifyAsync(string customerId, IdentityOptions options = null);
        Task ResetAsync();
        Task SetLocaleAsync(string locale);
        Task TriggerAsync(string eventName, IReadOnlyDictionary<string, object> properties = null);
        Task DismissAsync();
        Task<FeatureAccess> HasFeatureAsync(string featureId, FeatureQuery query = null);
        Task<FeatureConsumption> ConsumeFeatureAsync(string featureId, FeatureCommand command);
        Task<RestoreResult> RestorePurchasesAsync();
        IDisposable SubscribeStatus(Action<NuxieStatus> observer);
        IDisposable SubscribeFeatures(Action<FeatureSnapshot> observer);
        IDisposable ObserveFeature(string featureId, Action<FeatureState> observer);
        IDisposable SubscribeActivity(Action<NuxieActivity> observer);
        IDisposable SubscribeAppAction(Action<AppAction> observer);
        IDisposable SubscribeErrors(Action<NuxieException> observer);
    }
}
