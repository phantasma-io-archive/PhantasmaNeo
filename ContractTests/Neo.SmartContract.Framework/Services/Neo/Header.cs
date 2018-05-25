namespace Neo.SmartContract.Framework.Services.Neo
{
    public class Header : IScriptContainer
    {
        public byte[] Hash
        {
            get;
        }

        public uint Version
        {
            get;
        }

        public byte[] PrevHash
        {
            get;
        }

        public byte[] MerkleRoot
        {
            get;
        }

        public uint Timestamp
        {
            get;
        }

        public uint Index
        {
            get;
        }

        public ulong ConsensusData
        {
            get;
        }

        public byte[] NextConsensus
        {
            get;
        }
    }
}
