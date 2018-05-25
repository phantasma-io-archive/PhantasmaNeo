namespace Neo.SmartContract.Framework.Services.Neo
{
    public class Contract
    {
        public byte[] Script
        {
            get;
        }

        public StorageContext StorageContext
        {
            get;
        }

        public static Contract Create(byte[] script, byte[] parameter_list, byte return_type, bool need_storage, string name, string version, string author, string email, string description) { return null; }

        public static Contract Migrate(byte[] script, byte[] parameter_list, byte return_type, bool need_storage, string name, string version, string author, string email, string description) { return null; }

        public static void Destroy() { }
    }
}
