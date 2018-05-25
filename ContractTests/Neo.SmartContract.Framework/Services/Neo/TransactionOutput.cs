namespace Neo.SmartContract.Framework.Services.Neo
{
    public class TransactionOutput : IApiInterface
    {
        public byte[] AssetId
        {
            get;
        }

        public long Value
        {
            get;
        }

        public byte[] ScriptHash
        {
            get;
        }
    }
}
