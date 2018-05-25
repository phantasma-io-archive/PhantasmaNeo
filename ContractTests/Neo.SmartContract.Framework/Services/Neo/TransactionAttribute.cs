namespace Neo.SmartContract.Framework.Services.Neo
{
    public class TransactionAttribute : IApiInterface
    {
        public byte Usage
        {
            get;
        }

        public byte[] Data
        {
            get;
        }
    }
}
