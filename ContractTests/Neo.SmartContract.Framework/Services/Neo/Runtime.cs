namespace Neo.SmartContract.Framework.Services.Neo
{
    public static class Runtime
    {
        public static TriggerType Trigger
        {
            get;
        }

        public static uint Time
        {
            get;
        }

        public static bool CheckWitness(byte[] hashOrPubkey) { return false; }

        public static void Notify(params object[] state) { }

        public static void Log(string message) { }
    }
}
