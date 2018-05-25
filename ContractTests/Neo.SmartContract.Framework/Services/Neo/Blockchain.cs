namespace Neo.SmartContract.Framework.Services.Neo
{
    public static class Blockchain
    {
        public static uint GetHeight() { return 0; }

        public static Header GetHeader(uint height) { return null; }

        public static Header GetHeader(byte[] hash) { return null; }

        public static Block GetBlock(uint height) { return null; }

        public static Block GetBlock(byte[] hash) { return null; }

        public static Transaction GetTransaction(byte[] hash) { return null; }

        public static Account GetAccount(byte[] script_hash) { return null; }

        public static byte[][] GetValidators() { return null; }

        public static Asset GetAsset(byte[] asset_id) { return null; }

        public static Contract GetContract(byte[] script_hash) { return null; }
    }
}
