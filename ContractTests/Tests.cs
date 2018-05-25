using System;
using System.IO;

using NUnit.Framework;
using System.Linq;
using Neo.Lux.Core;
using Neo.Lux.Utils;
using Neo.Lux.Cryptography;
using Neo.SmartContract;
using System.Diagnostics;
using System.Numerics;
using Neo.Lux.Debugger;

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

        private NeoEmulator api;
        private UInt160 contract_script_hash;

        private KeyPair ownerKeys;
        private KeyPair teamKeys;
        private KeyPair[] whitelisted_buyerKeys;

        private DebugClient debugger = new DebugClient();

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            // this is the key for the NEO "issuer" in the virtual chain used for testing
            ownerKeys = KeyPair.GenerateAddress();

            this.api = new NeoEmulator(ownerKeys);

            this.api.SetLogger(x => {
                if (api.Chain.HasDebugger)
                {
                    debugger.WriteLine(x);
                }
                Debug.WriteLine(x);
            });

            // create a random key for the team
            teamKeys = KeyPair.GenerateAddress();
            // since the real team address is hardcoded in the contract, use BypassKey to give same permissions to this key
            this.api.Chain.BypassKey(new UInt160(PhantasmaContract.Team_Address), new UInt160(teamKeys.address.AddressToScriptHash()));

            var temp = TestContext.CurrentContext.TestDirectory.Split(new char[] { '\\', '/' }).ToList();

            for (int i=0; i<3; i++)
            {
                temp.RemoveAt(temp.Count - 1);
            }

            temp.Add("PhantasmaContract");
            temp.Add("bin");
            temp.Add("Debug");

            ContractFolder = String.Join("\\", temp.ToArray());

            contract_script_bytes = File.ReadAllBytes(ContractFolder  +  "/PhantasmaContract.avm");
            contract_script_hash = contract_script_bytes.ToScriptHash();

            Assert.IsNotNull(contract_script_bytes);

            api.Chain.AttachDebugger(debugger);                       

            Transaction tx;

            tx = api.SendAsset(ownerKeys, teamKeys.address, "GAS", 800);
            Assert.IsNotNull(tx);

            var balances = api.GetAssetBalancesOf(teamKeys.address);
            Assert.IsTrue(balances.ContainsKey("GAS"));
            Assert.IsTrue(balances["GAS"] == 800);

            tx = api.DeployContract(teamKeys, contract_script_bytes, "0710".HexToBytes(), 5, ContractPropertyState.HasStorage, "Phantasma", "1.0", "phantasma.io", "info@phantasma.io", "Phantasma Smart Contract");
            Assert.IsNotNull(tx);


            whitelisted_buyerKeys = new KeyPair[4];
            for (int i=0; i< whitelisted_buyerKeys.Length; i++)
            {
                whitelisted_buyerKeys[i] = KeyPair.GenerateAddress();
                api.SendAsset(ownerKeys, whitelisted_buyerKeys[i].address, "NEO", 100);

                api.CallContract(teamKeys, contract_script_hash, "whitelistAdd", new object[] { whitelisted_buyerKeys[i].address.AddressToScriptHash() });
            }
        }

        [Test]
        public void TestNEP5()
        {
            var token = new NEP5(api, contract_script_hash);

            Assert.IsTrue(token.Name == "Phantasma");
            Assert.IsTrue(token.Symbol == "SOUL");
            Assert.IsTrue(token.Decimals == 8);
        }

        [Test, Order(1)]
        public void TestTokenDeploy()
        {
            var token = new NEP5(api, contract_script_hash);
            Assert.IsNotNull(token);

            api.Chain.DettachDebugger();
            Assert.IsTrue(token.TotalSupply == 0);

            var tx = TokenSale.Deploy(token, teamKeys);
            Assert.IsNotNull(tx);

            var notifications = api.Chain.GetNotifications(tx);
            Assert.IsNotNull(notifications);
            Assert.IsTrue(notifications.Count == 2);

            var account = api.Chain.GetAccount(contract_script_hash);
            Assert.IsTrue(account.storage.entries.Count > 0);

            var presale_balance = token.BalanceOf(PhantasmaContract.Presale_Address);
            Assert.IsTrue(presale_balance == (PhantasmaContract.presale_supply / PhantasmaContract.soul_decimals));

            var plaform_balance = token.BalanceOf(PhantasmaContract.Platform_Address);
            Assert.IsTrue(plaform_balance == (PhantasmaContract.platform_supply / PhantasmaContract.soul_decimals));
        }

        [Test]
        public void FailBuyDuringSaleNoWhitelist()
        {
            var neo_amount = 5;

            var random_buyerKeys = KeyPair.GenerateAddress();
            api.SendAsset(ownerKeys, random_buyerKeys.address, "NEO", neo_amount);

            api.Chain.Time = PhantasmaContract.ico_start_time + 1;

            var token = new NEP5(api, contract_script_hash);

            var tx = TokenSale.MintTokens(token, random_buyerKeys, "NEO", neo_amount);

            Assert.IsNotNull(tx);

            var notifications = api.Chain.GetNotifications(tx);
            //Assert.IsNull(notifications);

            var balance = token.BalanceOf(random_buyerKeys);
            Assert.IsTrue(balance == 0);
        }

        [Test]
        public void FailBuyOutsideSalePeriod()
        {
            var random_buyerKeys = KeyPair.GenerateAddress();

            var token = new NEP5(api, contract_script_hash);
            var original_balance = token.BalanceOf(random_buyerKeys);

            var neo_amount = 5;
            api.SendAsset(ownerKeys, random_buyerKeys.address, "NEO", neo_amount);

            api.Chain.Time = PhantasmaContract.ico_start_time - 100;

            var tx = TokenSale.MintTokens(token, random_buyerKeys, "NEO", neo_amount);

            Assert.IsNotNull(tx);

            var notifications = api.Chain.GetNotifications(tx);
            //Assert.IsNull(notifications);

            var new_balance = token.BalanceOf(random_buyerKeys);
            Assert.IsTrue(new_balance == original_balance);
        }

        [Test]
        public void FailBuyOutsideSalePeriodEvenIfWhitelisted()
        {
            var n = 0;

            var token = new NEP5(api, contract_script_hash);
            var original_balance = token.BalanceOf(whitelisted_buyerKeys[n]);

            var is_whitelisted = IsWhitelisted(whitelisted_buyerKeys[n].address.AddressToScriptHash());
            Assert.IsTrue(is_whitelisted);

            var neo_amount = 5;
            api.SendAsset(ownerKeys, whitelisted_buyerKeys[n].address, "NEO", neo_amount);

            api.Chain.Time = PhantasmaContract.ico_start_time - 100;

            var tx = TokenSale.MintTokens(token, whitelisted_buyerKeys[n], "NEO", neo_amount);

            Assert.IsNotNull(tx);

            var notifications = api.Chain.GetNotifications(tx);
            //Assert.IsNull(notifications);

            var new_balance = token.BalanceOf(whitelisted_buyerKeys[n]);
            Assert.IsTrue(new_balance == original_balance);
        }

        [Test]
        public void TestBuyDuringSaleWhitelistedSinglePurchase()
        {
            var n = 0;

            var token = new NEP5(api, contract_script_hash);
            var original_balance = token.BalanceOf(whitelisted_buyerKeys[n]);

            var is_whitelisted = IsWhitelisted(whitelisted_buyerKeys[n].address.AddressToScriptHash());
            Assert.IsTrue(is_whitelisted);

            api.Chain.Time = PhantasmaContract.ico_start_time + 1;

            var neo_amount = 5;
            api.SendAsset(ownerKeys, whitelisted_buyerKeys[n].address, "NEO", neo_amount);

            var tx = TokenSale.MintTokens(token, whitelisted_buyerKeys[n], "NEO", neo_amount);

            Assert.IsNotNull(tx);

            var notifications = api.Chain.GetNotifications(tx);
            //Assert.IsNotNull(notifications);
            //Assert.IsTrue(notifications.Count == 1);

            var new_balance = token.BalanceOf(whitelisted_buyerKeys[n]);

            var expected_balance = original_balance + neo_amount * (int)(PhantasmaContract.token_swap_rate / PhantasmaContract.soul_decimals);
            Assert.IsTrue(new_balance == expected_balance);
        }

        [Test]
        public void TestBuyDuringSaleWhitelistedMultiplePurchases()
        {
            var n = 1;

            var token = new NEP5(api, contract_script_hash);
            var original_balance = token.BalanceOf(whitelisted_buyerKeys[n]);

            var is_whitelisted = IsWhitelisted(whitelisted_buyerKeys[n].address.AddressToScriptHash());
            Assert.IsTrue(is_whitelisted);

            api.Chain.Time = PhantasmaContract.ico_start_time + 100;

            // total should be 10 or less
            var purchases = new int[] { 3, 2, 4, 1 };
            var neo_amount = purchases.Sum();
            api.SendAsset(ownerKeys, whitelisted_buyerKeys[n].address, "NEO", neo_amount);

            for (int i=0; i<purchases.Length; i++)
            {
                var tx = TokenSale.MintTokens(token, whitelisted_buyerKeys[n], "NEO", purchases[i]);

                Assert.IsNotNull(tx);

                var notifications = api.Chain.GetNotifications(tx);
                //Assert.IsNotNull(notifications);
                //Assert.IsTrue(notifications.Count == 1);

                api.Chain.Time += (uint)(1000 * purchases[i] * 20);
            }

            var new_balance = token.BalanceOf(whitelisted_buyerKeys[n]);

            var expected_balance = original_balance + neo_amount * (int)(PhantasmaContract.token_swap_rate / PhantasmaContract.soul_decimals);
            Assert.IsTrue(new_balance == expected_balance);
        }

        #region UTILS
        private bool IsWhitelisted(byte[] script_hash)
        {
            var whitelist_result = api.InvokeScript(contract_script_hash, "whitelistCheckOne", new object[] { script_hash });
            if (whitelist_result == null)
            {
                return false;
            }

            var bytes = (byte[])whitelist_result.stack[0];
            var is_whitelisted = bytes != null && bytes.Length > 0 ? bytes[0] : 0;
            return is_whitelisted == 1;
        }
        #endregion
    }
}
