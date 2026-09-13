using System;
using System.Threading.Tasks;
namespace Nuxie.Unity.Samples
{
    public static class LabChecks
    {
        public static async Task RunAsync(INuxieClient sdk,string customer,string feature,string entityA,string entityB,string operationId,Action<string> record)
        {
            void Check(bool ok,string label) { if (!ok) throw new InvalidOperationException(label); record("PASS " + label); }
            await sdk.SetLocaleAsync("en_US"); await sdk.SetLocaleAsync(null);
            await sdk.ResetAsync(); await sdk.IdentifyAsync(customer); await Ready(sdk);
            var identity = await sdk.GetIdentityAsync(); Check(identity.CustomerId == customer && identity.IsIdentified,"identity and profile readiness");
            var a = await sdk.HasFeatureAsync(feature,new FeatureQuery { EntityId = entityA });
            var b = await sdk.HasFeatureAsync(feature,new FeatureQuery { EntityId = entityB,Policy = FeatureQueryPolicy.Remote });
            var remote = await sdk.HasFeatureAsync(feature,new FeatureQuery { EntityId = entityA,Policy = FeatureQueryPolicy.Remote });
            Check(a.Equals(remote),"cache-first entity query matches authority"); Check(a.Allowed && a.Balance >= 1,"entity A has a spendable grant");
            var missing = await sdk.HasFeatureAsync(feature,new FeatureQuery { EntityId = "unknown-" + operationId }); Check(!missing.Allowed,"unknown entity denies");
            var command = new FeatureCommand { OperationId = operationId,Quantity = 1,EntityId = entityA };
            var first = await sdk.ConsumeFeatureAsync(feature,command); Check(first.Accepted,"consumption committed");
            var replay = await sdk.ConsumeFeatureAsync(feature,command);
            Check(replay.Accepted && replay.IdempotentReplay && first.Balance == replay.Balance && first.OccurredAtMs == replay.OccurredAtMs && replay.CustomerId == customer && replay.FeatureId == feature,"retry preserves original receipt");
            var afterA = await sdk.HasFeatureAsync(feature,new FeatureQuery { EntityId = entityA,Policy = FeatureQueryPolicy.Remote });
            var afterB = await sdk.HasFeatureAsync(feature,new FeatureQuery { EntityId = entityB,Policy = FeatureQueryPolicy.Remote });
            Check(afterA.Balance == a.Balance - (first.IdempotentReplay ? 0 : 1),"entity A charged once"); Check(afterB.Balance == b.Balance,"entity B unchanged");
            await sdk.ResetAsync(); var anonymous = await sdk.GetIdentityAsync(); Check(!anonymous.IsIdentified && anonymous.CustomerId == anonymous.AnonymousId && anonymous.AnonymousId != identity.AnonymousId,"reset rotates identity");
            await sdk.IdentifyAsync(customer); await Ready(sdk); record("API CHECKS PASSED: " + operationId);
        }
        private static async Task Ready(INuxieClient sdk)
        {
            if (sdk.Features.State == FeatureStateKind.Ready) return;
            var ready = new TaskCompletionSource<bool>();
            using (sdk.SubscribeFeatures(s => { if (s.State == FeatureStateKind.Ready) ready.TrySetResult(true); }))
            {
                if (await Task.WhenAny(ready.Task,Task.Delay(15000)) != ready.Task) throw new TimeoutException("Native Feature profile did not become ready");
                await ready.Task;
            }
        }
    }
}
