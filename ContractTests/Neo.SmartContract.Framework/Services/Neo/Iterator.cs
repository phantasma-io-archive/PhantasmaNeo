namespace Neo.SmartContract.Framework.Services.Neo
{
    public class Iterator<TKey, TValue>
    {
        public bool Next() { return false; }

        public TKey Key
        {
            get;
        }

        public TValue Value
        {
            get;
        }
    }
}
