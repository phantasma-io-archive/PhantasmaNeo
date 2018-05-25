namespace Neo.SmartContract.Framework.Services.Neo
{
    public class Asset
    {
        public byte[] AssetId
        {
            get;
        }

        public byte AssetType
        {
            get;
        }

        public long Amount
        {
            get;
        }

        public long Available
        {
            get;
        }

        public byte Precision
        {
            get;
        }

        public byte[] Owner
        {
            get;
        }

        public byte[] Admin
        {
            get;
        }

        public byte[] Issuer
        {
            get;
        }

        public Asset Create(byte asset_type, string name, long amount, byte precision, byte[] owner, byte[] admin, byte[] issuer) { return null; }

        public uint Renew(byte years) { return 0; }
    }
}
