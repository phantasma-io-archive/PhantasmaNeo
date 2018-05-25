namespace Neo.SmartContract.Framework.Services.Neo
{
    public class Transaction : IScriptContainer
    {
        public byte[] Hash
        {
            get;
        }

        public byte Type
        {
            get;
        }

        public TransactionAttribute[] GetAttributes() { return null; }

        public TransactionInput[] GetInputs() { return null; }

        public TransactionOutput[] GetOutputs() { return null; }

        public TransactionOutput[] GetReferences() { return null; }

        public TransactionOutput[] GetUnspentCoins() { return null; }
    }
}
