namespace Neo.SmartContract.Framework.Services.Neo
{
    public class TransactionInput : IApiInterface
    {
        public byte[] PrevHash
        {
            get;
        }

        public ushort PrevIndex
        {
            get;
        }
    }
}
