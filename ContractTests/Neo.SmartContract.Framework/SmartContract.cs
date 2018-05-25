namespace Neo.SmartContract.Framework
{
    public class SmartContract
    {        
        protected static byte[] Sha1(byte[] data) { return null; }

        protected static byte[] Sha256(byte[] data) { return null; }

        protected static byte[] Hash160(byte[] data) { return null; }

        protected static byte[] Hash256(byte[] data) { return null; }

        protected static bool VerifySignature(byte[] signature, byte[] pubkey) { return false; }

        protected static bool VerifySignatures(byte[][] signature, byte[][] pubkey) { return false; }
    }
}
