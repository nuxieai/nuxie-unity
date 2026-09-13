using Nuxie.Unity.Internal;
namespace Nuxie.Unity
{
    public static class Nuxie
    {
        private static Client client;
        public static INuxieClient Client => client ?? (client = new Client(new NativeTransport()));
        internal static void ResetRuntime() { client?.Detach(); client = null; }
    }
}
