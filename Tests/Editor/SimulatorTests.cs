using System.Threading.Tasks;
using NUnit.Framework;
using Nuxie.Unity.Editor;
namespace Nuxie.Unity.Tests
{
    public sealed class SimulatorTests
    {
        [Test] public async Task SimulatorIsExplicitAndReplaysConsumption()
        {
            using var simulator = new NuxieEditorSimulator();
            var client = simulator.Client;
            await client.ConfigureAsync(new NuxieOptions { IosApiKey = "simulated", AndroidApiKey = "simulated" });
            Assert.AreEqual("SIMULATED",client.Status.NativeVersion);
            await client.IdentifyAsync("player");
            var command = new FeatureCommand { OperationId = "turn-1",EntityId = "a" };
            var first = await client.ConsumeFeatureAsync("energy",command);
            var replay = await client.ConsumeFeatureAsync("energy",command);
            Assert.IsTrue(first.Accepted); Assert.IsTrue(replay.IdempotentReplay); Assert.AreEqual(first.Balance,replay.Balance);
            await client.ShutdownAsync();
        }
    }
}
