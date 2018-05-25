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
    public class TestEnviroment
    {
        public readonly NeoEmulator api;
        public readonly KeyPair owner_keys;
        public readonly KeyPair team_keys;
        public readonly KeyPair[] whitelisted_buyerKeys;
        public readonly DebugClient debugger;
        public readonly NEP5 token;

        public TestEnviroment(int buyers_count)
        {
            debugger = new DebugClient();

            // this is the key for the NEO "issuer" in the virtual chain used for testing
            owner_keys = KeyPair.GenerateAddress();

            this.api = new NeoEmulator(owner_keys);

            this.api.SetLogger(x => {
                if (api.Chain.HasDebugger)
                {
                    debugger.WriteLine(x);
                }
                Debug.WriteLine(x);
            });

            // create a random key for the team
            team_keys = KeyPair.GenerateAddress();
            // since the real team address is hardcoded in the contract, use BypassKey to give same permissions to this key
            this.api.Chain.BypassKey(new UInt160(PhantasmaContract.Team_Address), new UInt160(team_keys.address.AddressToScriptHash()));


            api.Chain.AttachDebugger(debugger);

            Transaction tx;

            tx = api.SendAsset(owner_keys, team_keys.address, "GAS", 800);
            Assert.IsNotNull(tx);

            var balances = api.GetAssetBalancesOf(team_keys.address);
            Assert.IsTrue(balances.ContainsKey("GAS"));
            Assert.IsTrue(balances["GAS"] == 800);

            tx = api.DeployContract(team_keys, ContractTests.contract_script_bytes, "0710".HexToBytes(), 5, ContractPropertyState.HasStorage, "Phantasma", "1.0", "phantasma.io", "info@phantasma.io", "Phantasma Smart Contract");
            Assert.IsNotNull(tx);

            if (buyers_count > 0)
            {
                whitelisted_buyerKeys = new KeyPair[buyers_count];
                for (int i = 0; i < whitelisted_buyerKeys.Length; i++)
                {
                    whitelisted_buyerKeys[i] = KeyPair.GenerateAddress();
                    api.SendAsset(owner_keys, whitelisted_buyerKeys[i].address, "NEO", 100);

                    api.CallContract(team_keys, ContractTests.contract_script_hash, "whitelistAdd", new object[] { whitelisted_buyerKeys[i].address.AddressToScriptHash() });
                }
            }

            this.token = new NEP5(api, ContractTests.contract_script_hash);
        }

        #region UTILS
        public bool IsWhitelisted(byte[] script_hash)
        {
            var whitelist_result = api.InvokeScript(ContractTests.contract_script_hash, "whitelistCheckOne", new object[] { script_hash });
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

    [TestFixture]
    public class ContractTests
    {
        public static byte[] contract_script_bytes { get;  set; }
        public static UInt160 contract_script_hash { get; set; }

        private string contract_folder;
        
        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            var temp = TestContext.CurrentContext.TestDirectory.Split(new char[] { '\\', '/' }).ToList();

            for (int i = 0; i < 3; i++)
            {
                temp.RemoveAt(temp.Count - 1);
            }

            temp.Add("PhantasmaContract");
            temp.Add("bin");
            temp.Add("Debug");

            contract_folder = String.Join("\\", temp.ToArray());

            contract_script_bytes = File.ReadAllBytes(contract_folder + "/PhantasmaContract.avm");
            contract_script_hash = contract_script_bytes.ToScriptHash();

            Assert.IsNotNull(contract_script_bytes);
        }

        [Test]
        public void TestNEP5()
        {
            var env = new TestEnviroment(0);

            Assert.IsTrue(env.token.Name == "Phantasma");
            Assert.IsTrue(env.token.Symbol == "SOUL");
            Assert.IsTrue(env.token.Decimals == 8);
        }

        [Test]
        public void TestSaleTime()
        {
            Assert.IsTrue(PhantasmaContract.ico_start_time < PhantasmaContract.ico_war_time);
            Assert.IsTrue(PhantasmaContract.ico_war_time < PhantasmaContract.ico_end_time);

            var start_date = PhantasmaContract.ico_start_time.ToDateTime();
            Assert.IsTrue(start_date.Day == 27);
            Assert.IsTrue(start_date.Month == 5);
            Assert.IsTrue(start_date.Year == 2018);
            Assert.IsTrue(start_date.Hour == 0);
            Assert.IsTrue(start_date.Minute == 0);

            var war_date = PhantasmaContract.ico_war_time.ToDateTime();
            Assert.IsTrue(war_date.Day == 28);
            Assert.IsTrue(war_date.Month == 5);
            Assert.IsTrue(war_date.Year == 2018);
            Assert.IsTrue(war_date.Hour == 0);
            Assert.IsTrue(war_date.Minute == 0);

            var end_date = PhantasmaContract.ico_start_time.ToDateTime();
            Assert.IsTrue(end_date.Day == 29);
            Assert.IsTrue(end_date.Month == 5);
            Assert.IsTrue(end_date.Year == 2018);
            Assert.IsTrue(end_date.Hour == 0);
            Assert.IsTrue(end_date.Minute == 0);
        }

        [Test]
        public void TestTokenDeploy()
        {
            var env = new TestEnviroment(0);

            env.api.Chain.DettachDebugger();
            Assert.IsTrue(env.token.TotalSupply == 0);

            var tx = TokenSale.Deploy(env.token, env.team_keys);
            Assert.IsNotNull(tx);

            var notifications = env.api.Chain.GetNotifications(tx);
            Assert.IsNotNull(notifications);
            Assert.IsTrue(notifications.Count == 2);

            var account = env.api.Chain.GetAccount(contract_script_hash);
            Assert.IsTrue(account.storage.entries.Count > 0);

            var presale_balance = env.token.BalanceOf(PhantasmaContract.Presale_Address);
            Assert.IsTrue(presale_balance == (PhantasmaContract.presale_supply / PhantasmaContract.soul_decimals));

            var plaform_balance = env.token.BalanceOf(PhantasmaContract.Platform_Address);
            Assert.IsTrue(plaform_balance == (PhantasmaContract.platform_supply / PhantasmaContract.soul_decimals));

            Assert.IsTrue(env.token.TotalSupply == (PhantasmaContract.initialSupply / PhantasmaContract.soul_decimals));
        }

        [Test]
        public void FailBuyDuringSaleNoWhitelist()
        {
            var env = new TestEnviroment(0);

            var neo_amount = 5;

            var random_buyerKeys = KeyPair.GenerateAddress();
            env.api.SendAsset(env.owner_keys, random_buyerKeys.address, "NEO", neo_amount);

            env.api.Chain.Time = PhantasmaContract.ico_start_time + 1;

            var tx = TokenSale.MintTokens(env.token, random_buyerKeys, "NEO", neo_amount);

            Assert.IsNotNull(tx);

            var notifications = env.api.Chain.GetNotifications(tx);
            Assert.IsNull(notifications);

            var balance = env.token.BalanceOf(random_buyerKeys);
            Assert.IsTrue(balance == 0);
        }

        [Test]
        public void FailBuyOutsideSalePeriod()
        {
            var env = new TestEnviroment(0);

            var random_buyerKeys = KeyPair.GenerateAddress();

            var original_balance = env.token.BalanceOf(random_buyerKeys);

            var neo_amount = 5;
            env.api.SendAsset(env.owner_keys, random_buyerKeys.address, "NEO", neo_amount);

            env.api.Chain.Time = PhantasmaContract.ico_start_time - 100;

            var tx = TokenSale.MintTokens(env.token, random_buyerKeys, "NEO", neo_amount);

            Assert.IsNotNull(tx);

            var notifications = env.api.Chain.GetNotifications(tx);
            //Assert.IsNull(notifications);

            var new_balance = env.token.BalanceOf(random_buyerKeys);
            Assert.IsTrue(new_balance == original_balance);
        }

        [Test]
        public void FailBuyOutsideSalePeriodEvenIfWhitelisted()
        {
            var env = new TestEnviroment(1);

            var n = 0;

            var original_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

            var is_whitelisted = env.IsWhitelisted(env.whitelisted_buyerKeys[n].address.AddressToScriptHash());
            Assert.IsTrue(is_whitelisted);

            var neo_amount = 5;
            env.api.SendAsset(env.owner_keys, env.whitelisted_buyerKeys[n].address, "NEO", neo_amount);

            env.api.Chain.Time = PhantasmaContract.ico_start_time - 100;

            var tx = TokenSale.MintTokens(env.token, env.whitelisted_buyerKeys[n], "NEO", neo_amount);

            Assert.IsNotNull(tx);

            var notifications = env.api.Chain.GetNotifications(tx);
            Assert.IsNull(notifications);

            var new_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);
            Assert.IsTrue(new_balance == original_balance);
        }

        [Test]
        public void TestBuyDuringSaleWhitelistedSinglePurchase()
        {
            var env = new TestEnviroment(1);

            var n = 0;

            var original_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

            var is_whitelisted = env.IsWhitelisted(env.whitelisted_buyerKeys[n].address.AddressToScriptHash());
            Assert.IsTrue(is_whitelisted);

            env.api.Chain.Time = PhantasmaContract.ico_start_time + 1;

            var neo_amount = 5;
            env.api.SendAsset(env.owner_keys, env.whitelisted_buyerKeys[n].address, "NEO", neo_amount);

            var tx = TokenSale.MintTokens(env.token, env.whitelisted_buyerKeys[n], "NEO", neo_amount);

            Assert.IsNotNull(tx);

            var notifications = env.api.Chain.GetNotifications(tx);
            //Assert.IsNotNull(notifications);
            //Assert.IsTrue(notifications.Count == 1);

            var new_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

            var expected_balance = original_balance + neo_amount * (int)(PhantasmaContract.token_swap_rate / PhantasmaContract.soul_decimals);
            Assert.IsTrue(new_balance == expected_balance);
        }

        [Test]
        public void TestBuyDuringSaleWhitelistedMultiplePurchases()
        {
            var env = new TestEnviroment(1);

            var n = 0;

            var original_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

            var is_whitelisted = env.IsWhitelisted(env.whitelisted_buyerKeys[n].address.AddressToScriptHash());
            Assert.IsTrue(is_whitelisted);

            env.api.Chain.Time = PhantasmaContract.ico_start_time + 100;

            // total should be 10 or less
            var purchases = new int[] { 3, 2, 4, 1 };
            var neo_amount = purchases.Sum();
            env.api.SendAsset(env.owner_keys, env.whitelisted_buyerKeys[n].address, "NEO", neo_amount);

            for (int i=0; i<purchases.Length; i++)
            {
                var tx = TokenSale.MintTokens(env.token, env.whitelisted_buyerKeys[n], "NEO", purchases[i]);

                Assert.IsNotNull(tx);

                var notifications = env.api.Chain.GetNotifications(tx);
                //Assert.IsNotNull(notifications);
                //Assert.IsTrue(notifications.Count == 1);

                // advance time
                env.api.Chain.Time += (uint)(1000 * purchases[i] * 20);
            }

            var new_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

            var expected_balance = original_balance + neo_amount * (int)(PhantasmaContract.token_swap_rate / PhantasmaContract.soul_decimals);
            Assert.IsTrue(new_balance == expected_balance);
        }

        [Test]
        public void WithdrawNeoAfterSale()
        {
            var env = new TestEnviroment(20);

            env.api.Chain.Time = PhantasmaContract.ico_start_time + 1;

            var total_amount = 0;

            for (int n=0; n<env.whitelisted_buyerKeys.Length; n++)
            {
                var is_whitelisted = env.IsWhitelisted(env.whitelisted_buyerKeys[n].address.AddressToScriptHash());
                Assert.IsTrue(is_whitelisted);

                var neo_amount = 1 + n % 10;
                total_amount += neo_amount;

                env.api.SendAsset(env.owner_keys, env.whitelisted_buyerKeys[n].address, "NEO", neo_amount);

                Assert.IsTrue(env.api.Chain.Time > PhantasmaContract.ico_start_time);
                Assert.IsTrue(env.api.Chain.Time < PhantasmaContract.ico_war_time);

                var tx = TokenSale.MintTokens(env.token, env.whitelisted_buyerKeys[n], "NEO", neo_amount);

                Assert.IsNotNull(tx);

                var notifications = env.api.Chain.GetNotifications(tx);
                //Assert.IsNotNull(notifications);
                //Assert.IsTrue(notifications.Count == 1);

                var balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

                var expected_balance = neo_amount * (int)(PhantasmaContract.token_swap_rate / PhantasmaContract.soul_decimals);
                Assert.IsTrue(balance == expected_balance);
            }

            var balances = env.api.GetAssetBalancesOf(contract_script_hash);
            Assert.IsTrue(balances.ContainsKey("NEO"));
            Assert.IsTrue(balances["NEO"] == total_amount);
        }


    }
}
