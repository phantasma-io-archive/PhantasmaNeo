namespace Neo.SmartContract.Framework
{
    public class Map<TKey, TValue>
    {
        public Map() { }

        public TValue this[TKey key]
        {
            get { return default(TValue); }
            set { }
        }

        public TKey[] Keys
        {
            get { return null; }
        }

        public TValue[] Values
        {
            get { return null; }
        }

        public bool HasKey(TKey key) { return false; }

        public void Remove(TKey key) {  }
    }
}
