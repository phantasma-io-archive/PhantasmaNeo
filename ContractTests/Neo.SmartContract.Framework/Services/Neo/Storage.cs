using System.Numerics;

namespace Neo.SmartContract.Framework.Services.Neo
{
    public static class Storage
    {
        public static StorageContext CurrentContext
        {
            get;
        }

        public static byte[] Get(StorageContext context, byte[] key){ return null; }

        public static byte[] Get(StorageContext context, string key){ return null; }

        public static void Put(StorageContext context, byte[] key, byte[] value){}

        public static void Put(StorageContext context, byte[] key, BigInteger value){}

        public static void Put(StorageContext context, byte[] key, string value){}

        public static void Put(StorageContext context, string key, byte[] value){}

        public static void Put(StorageContext context, string key, BigInteger value){}

        public static void Put(StorageContext context, string key, string value){}

        public static void Delete(StorageContext context, byte[] key){}

        public static void Delete(StorageContext context, string key){}

        public static Iterator<byte[], byte[]> Find(StorageContext context, byte[] prefix){ return null; }

        public static Iterator<string, byte[]> Find(StorageContext context, string prefix){ return null; }
    }
}
