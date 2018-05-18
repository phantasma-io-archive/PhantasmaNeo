using System;
using System.IO;

using NUnit.Framework;
using System.Linq;
using Neo.Lux.Core;
using Neo.Emulator;
using Neo.Lux.Utils;
using Neo.Lux.Cryptography;
using Neo.SmartContract;

/*
 * DOCS HERE => http://docs.neo.org/en-us/sc/test.html
 * NOTE - When pushing parameters into the script stack, don't forget that order is inverted
 */
namespace PhantasmaTests
{
    [TestFixture]
    public class ContractTests
    {
        private byte[] contract_script_bytes;

        private string ContractFolder;

        private Emulator api;
        private UInt160 contract_script_hash;

        private KeyPair ownerKeys;

        public static readonly string ownerWIF = "KxDgvEKzgSBPPfuVfw67oPQBSjidEiqTHURKSDL1R7yGaGYAeYnr";

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            this.api = new Emulator();

            var temp = TestContext.CurrentContext.TestDirectory.Split(new char[] { '\\', '/' }).ToList();

            for (int i=0; i<3; i++)
            {
                temp.RemoveAt(temp.Count - 1);
            }

            temp.Add("PhantasmaContract");
            temp.Add("bin");
            temp.Add("Debug");

            ContractFolder = String.Join("\\", temp.ToArray());

            ownerKeys = KeyPair.FromWIF(ownerWIF);

            // path is hardcoded for now...
            contract_script_bytes = File.ReadAllBytes(ContractFolder  +  "/PhantasmaContract.avm");
            contract_script_hash = contract_script_bytes.ToScriptHash();
        }

        [Test]
        public void TestDeploy()
        {
            Assert.IsNotNull(contract_script_bytes);

            var tx = api.DeployContract(ownerKeys, contract_script_bytes, "0710".HexToBytes(), 5, ContractPropertyState.HasStorage, "Phantasma", "1.0", "phantasma.io", "info@phantasma.io", "Phantasma Smart Contract");
            Assert.IsNotNull(tx);

            var token = new NEP5(api, contract_script_hash);
            Assert.IsNotNull(token);

            token.Deploy(ownerKeys);
        }

        [Test]
        public void TestNEP5()
        {
            var token = new NEP5(api, contract_script_hash);

            Assert.IsTrue(token.Name == "Phantasma");
            Assert.IsTrue(token.Symbol == "SOUL");
            Assert.IsTrue(token.Decimals == 8);
        }

        [Test]
        public void TestSale()
        {
            api.SetTime(PhantasmaContract.ico_start_time - 100);

            var token = new NEP5(api, contract_script_hash);

            Assert.IsTrue(token.Name == "Phantasma");
            Assert.IsTrue(token.Symbol == "SOUL");
            Assert.IsTrue(token.Decimals == 8);
        }

    }
}
