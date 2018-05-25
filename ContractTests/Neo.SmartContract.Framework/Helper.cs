using Neo.Lux.Utils;
using System;
using System.Numerics;

namespace Neo.SmartContract.Framework
{
    public static class Helper
    {
        public static BigInteger AsBigInteger(this byte[] source) { return 0; }

        public static byte[] AsByteArray(this BigInteger source) { return null; }

        public static byte[] AsByteArray(this string source) { return null; }

        public static string AsString(this byte[] source) { return null; }

        public static byte[] Concat(this byte[] first, byte[] second) { return null; }

        public static byte[] HexToBytes(this string hex) { return null; }

        public static byte[] Range(this byte[] source, int index, int count) { return null; }

        public static byte[] Take(this byte[] source, int count) { return null; }

        public static Delegate ToDelegate(this byte[] source) { return null; }

        public static byte[] ToScriptHash(this string address) { return LuxUtils.AddressToScriptHash(address); }

        public static byte[] Serialize(this object source) { return null; }

        public static object Deserialize(this byte[] source) { return null; }
    }
}
