namespace Neo.SmartContract.Framework.Services.System
{
    public static class ExecutionEngine
    {
        public static IScriptContainer ScriptContainer
        {
            get;
        }

        public static byte[] ExecutingScriptHash
        {
            get;
        }

        public static byte[] CallingScriptHash
        {
            get;
        }

        public static byte[] EntryScriptHash
        {
            get;
        }
    }
}
