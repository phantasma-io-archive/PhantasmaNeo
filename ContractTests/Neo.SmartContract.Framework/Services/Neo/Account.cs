namespace Neo.SmartContract.Framework.Services.Neo
{
    public class Account
    {
        public byte[] ScriptHash
        {
            get;
        }

        public byte[][] Votes
        {
            get;
        }

        public long GetBalance(byte[] asset_id) { return 0; }
    }
}
