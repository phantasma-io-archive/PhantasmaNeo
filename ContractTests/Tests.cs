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
using System.Collections.Generic;

/*
 * DOCS HERE => http://docs.neo.org/en-us/sc/test.html
 * NOTE - When pushing parameters into the script stack, don't forget that order is inverted
 */
namespace PhantasmaTests
{
    public class TestEnviroment
    {
        public readonly Emulator api;
        public readonly KeyPair owner_keys;
        public readonly KeyPair team_keys;        
        public readonly KeyPair[] whitelisted_buyerKeys;
        public readonly DebugClient debugger;
        public readonly NEP5 token;

        public readonly int swap_rate;

        public TestEnviroment(int buyers_count)
        {
            debugger = new DebugClient();

            // this is the key for the NEO "issuer" in the virtual chain used for testing
            owner_keys = KeyPair.GenerateAddress();

            this.api = new Emulator(owner_keys);

            this.api.SetLogger(x => {
                if (api.Chain.HasDebugger)
                {
                    debugger.WriteLine(x);
                }
                Debug.WriteLine(x);
            });

            Transaction tx;

            // create a random key for the team
            team_keys = KeyPair.GenerateAddress();
            // since the real team address is hardcoded in the contract, use BypassKey to give same permissions to this key
            this.api.Chain.BypassKey(new UInt160(PhantasmaContract.Team_Address), new UInt160(team_keys.address.AddressToScriptHash()));

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
                var indices = new int[buyers_count];

                var addresses = new HashSet<string>();
                for (int i=0; i<buyers_count; i++)
                {
                    string address;
                    do
                    {
                        whitelisted_buyerKeys[i] = KeyPair.GenerateAddress();
                        address = whitelisted_buyerKeys[i].address;
                    } while (addresses.Contains(address));

                    addresses.Add(address);
                }

                for (int i = 0; i < buyers_count; i++)
                {
                    api.SendAsset(owner_keys, whitelisted_buyerKeys[i].address, "NEO", 100);

                    api.CallContract(team_keys, ContractTests.contract_script_hash, "whitelistAddFree", new object[] { whitelisted_buyerKeys[i].address.AddressToScriptHash() });
                }
            }

            this.token = new NEP5(api, ContractTests.contract_script_hash);

            this.swap_rate = (int)(PhantasmaContract.token_swap_rate / PhantasmaContract.soul_decimals);

            // do deploy
            Assert.IsTrue(this.token.TotalSupply == 0);

            tx = TokenSale.Deploy(this.token, this.team_keys);
            Assert.IsNotNull(tx);

            var notifications = this.api.Chain.GetNotifications(tx);
            Assert.IsNotNull(notifications);
            Assert.IsTrue(notifications.Count == 3);

            var account = this.api.Chain.GetAccount(ContractTests.contract_script_hash);
            Assert.IsTrue(account.storage.entries.Count > 0);

            var presale_balance = this.token.BalanceOf(PhantasmaContract.Presale_Address);
            Assert.IsTrue(presale_balance == (PhantasmaContract.presale_supply / PhantasmaContract.soul_decimals));

            var plaform_balance = this.token.BalanceOf(PhantasmaContract.Platform_Address);
            Assert.IsTrue(plaform_balance == (PhantasmaContract.platform_supply / PhantasmaContract.soul_decimals));

            var expected_supply = (PhantasmaContract.initialSupply / PhantasmaContract.soul_decimals);
            Assert.IsTrue(this.token.TotalSupply == expected_supply);
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

        public decimal GetBalanceOf(UInt160 script_hash, string symbol)
        {
            var balances = api.GetAssetBalancesOf(script_hash);
            return balances.ContainsKey(symbol) ? balances[symbol] : 0;
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

            var end_date = PhantasmaContract.ico_end_time.ToDateTime();
            Assert.IsTrue(end_date.Day == 29);
            Assert.IsTrue(end_date.Month == 5);
            Assert.IsTrue(end_date.Year == 2018);
            Assert.IsTrue(end_date.Hour == 0);
            Assert.IsTrue(end_date.Minute == 0);
        }

        [Test]
        public void FailBuyDuringSaleNoWhitelist()
        {
            var env = new TestEnviroment(0);

            var neo_amount = 5;

            var original_neo_balance = env.GetBalanceOf(contract_script_hash, "NEO");

            var random_buyerKeys = KeyPair.GenerateAddress();
            env.api.SendAsset(env.owner_keys, random_buyerKeys.address, "NEO", neo_amount);

            env.api.Chain.Time = PhantasmaContract.ico_start_time + 1;

            var tx = TokenSale.MintTokens(env.token, random_buyerKeys, "NEO", neo_amount);
            Assert.IsNull(tx);

            var balance = env.token.BalanceOf(random_buyerKeys);
            Assert.IsTrue(balance == 0);

            var current_neo_balance = env.GetBalanceOf(contract_script_hash, "NEO");
            Assert.IsTrue(current_neo_balance == original_neo_balance);
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

            Assert.IsNull(tx);

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

            Assert.IsNull(tx);

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

            var total_bough = 0;
            for (int i=0; i<purchases.Length; i++)
            {
                var tx = TokenSale.MintTokens(env.token, env.whitelisted_buyerKeys[n], "NEO", purchases[i]);
                total_bough += purchases[i];

                Assert.IsNotNull(tx);

                var notifications = env.api.Chain.GetNotifications(tx);
                //Assert.IsNotNull(notifications);
                //Assert.IsTrue(notifications.Count == 1);

                // advance time
                env.api.Chain.Time += (uint)(5 + n % 20);
                Assert.IsTrue(env.api.Chain.Time > PhantasmaContract.ico_start_time);
                Assert.IsTrue(env.api.Chain.Time < PhantasmaContract.ico_war_time);

                var new_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);
                var expected_balance = original_balance + total_bough * (int)(PhantasmaContract.token_swap_rate / PhantasmaContract.soul_decimals);
                Assert.IsTrue(new_balance == expected_balance);
            }
        }

        [Test]
        public void SimulateFullSaleAllFilled()
        {
            var env = new TestEnviroment(2058);

            // go back to the past
            env.api.Chain.Time = PhantasmaContract.ico_start_time / 2;

            for (int n = 0; n < env.whitelisted_buyerKeys.Length; n++)
            {
                env.api.SendAsset(env.owner_keys, env.whitelisted_buyerKeys[n].address, "NEO", 60);
            }

            var initial_supply = env.token.TotalSupply;

            var first_cap = (uint)(PhantasmaContract.token_initial_cap / PhantasmaContract.token_swap_rate);
            var first_round_supply = 5613426; // PhantasmaContract.sale1_supply / PhantasmaContract.soul_decimals;
            var first_round_neo = (uint)(first_round_supply) / env.swap_rate;
            var first_round_count = first_round_neo / first_cap;

            var first_round_extra_neo = first_round_neo - first_round_count * first_cap;

            if (first_round_extra_neo > 0)
            {

                first_round_count++;
            }

            long total_amount = 0;

            Assert.IsTrue(first_round_count <= env.whitelisted_buyerKeys.Length);

            env.api.Chain.Time = PhantasmaContract.ico_start_time + 1;
            for (int n = 0; n < first_round_count; n++)
            {
                var is_whitelisted = env.IsWhitelisted(env.whitelisted_buyerKeys[n].address.AddressToScriptHash());
                Assert.IsTrue(is_whitelisted);

                var original_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);
                Assert.IsTrue(original_balance == 0);

                var neo_amount = (first_round_extra_neo>0 && n == first_round_count-1)? first_round_extra_neo: 10;
                total_amount += neo_amount;

                env.api.Chain.Time++;
                Assert.IsTrue(env.api.Chain.Time > PhantasmaContract.ico_start_time);
                Assert.IsTrue(env.api.Chain.Time < PhantasmaContract.ico_war_time);

                var mintTx = TokenSale.MintTokens(env.token, env.whitelisted_buyerKeys[n], "NEO", neo_amount);
                Assert.IsNotNull(mintTx);

                var notifications = env.api.Chain.GetNotifications(mintTx);
                //Assert.IsNotNull(notifications);
                //Assert.IsTrue(notifications.Count == 1);

                var token_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

                var expected_balance = original_balance + neo_amount * env.swap_rate;
                Assert.IsTrue(token_balance == expected_balance);
            }

            var expected_supply = initial_supply + total_amount * env.swap_rate;
            Assert.IsTrue(expected_supply == env.token.TotalSupply);

            uint second_round_supply = 702975;
            var second_round_neo = second_round_supply / env.swap_rate;
            var second_round_count = second_round_neo / 50;
            var second_round_extra_neo = second_round_neo - (second_round_count * 50);

            if (second_round_extra_neo > 0)
            {

                second_round_count++;
            }

            env.api.Chain.Time = PhantasmaContract.ico_war_time + 1;
            for (int n = 0; n < second_round_count; n++)
            {
                var is_whitelisted = env.IsWhitelisted(env.whitelisted_buyerKeys[n].address.AddressToScriptHash());
                Assert.IsTrue(is_whitelisted);

                var original_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

                var neo_amount = (second_round_extra_neo > 0 && n == second_round_count - 1) ? second_round_extra_neo : 50;
                total_amount += neo_amount;

                env.api.Chain.Time++;
                Assert.IsTrue(env.api.Chain.Time > PhantasmaContract.ico_war_time);
                Assert.IsTrue(env.api.Chain.Time < PhantasmaContract.ico_end_time);

                var mintTx = TokenSale.MintTokens(env.token, env.whitelisted_buyerKeys[n], "NEO", neo_amount);
                Assert.IsNotNull(mintTx);

                var notifications = env.api.Chain.GetNotifications(mintTx);
                //Assert.IsNotNull(notifications);
                //Assert.IsTrue(notifications.Count == 1);

                var token_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

                var expected_balance = original_balance + neo_amount * env.swap_rate;
                Assert.IsTrue(token_balance == expected_balance);
            }

            // test if cap cannot be exceed
            for (int n = 0; n < 20; n++)
            {
                var is_whitelisted = env.IsWhitelisted(env.whitelisted_buyerKeys[n].address.AddressToScriptHash());
                Assert.IsTrue(is_whitelisted);

                var original_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

                var neo_amount = n + 1;

                env.api.Chain.Time++;
                Assert.IsTrue(env.api.Chain.Time > PhantasmaContract.ico_war_time);
                Assert.IsTrue(env.api.Chain.Time < PhantasmaContract.ico_end_time);

                var mintTx = TokenSale.MintTokens(env.token, env.whitelisted_buyerKeys[n], "NEO", neo_amount);
                Assert.IsNull(mintTx);
              
                var token_balance = env.token.BalanceOf(env.whitelisted_buyerKeys[n]);

                Assert.IsTrue(token_balance == original_balance);
            }

            expected_supply = initial_supply + total_amount * env.swap_rate;
            Assert.IsTrue(expected_supply == 91136374);
            Assert.IsTrue(expected_supply == env.token.TotalSupply);

            var neo_balance = env.GetBalanceOf(contract_script_hash, "NEO");
            Assert.IsTrue(neo_balance == total_amount);

            var tx = env.api.WithdrawAsset(env.team_keys, contract_script_hash.ToAddress(), "NEO", neo_balance, contract_script_bytes);
            Assert.IsNotNull(tx);

            neo_balance = env.GetBalanceOf(contract_script_hash, "NEO");
            Assert.IsTrue(neo_balance == 0);

            neo_balance = env.GetBalanceOf(new UInt160(env.team_keys.address.AddressToScriptHash()), "NEO");
            Assert.IsTrue(neo_balance == total_amount);
        }

        [Test]
        public void UnlockTeamVestedTokens()
        {
            var env = new TestEnviroment(0);

            var unlock_amount = (uint)(PhantasmaContract.team_monthly_supply / PhantasmaContract.soul_decimals);

            for (int i=0; i < 10; i++)
            {
                var original_balance = env.token.BalanceOf(PhantasmaContract.Team_Address);

                uint unlockTime = 0;
                if (i == 0) { unlockTime = 1550793600; }
                else
                if (i == 1) { unlockTime = 1558483200; }
                else
                if (i == 2) { unlockTime = 1566432000; }
                else
                if (i == 3) { unlockTime = 1574380800; }
                else
                if (i == 4) { unlockTime = 1582329600; }
                else
                if (i == 5) { unlockTime = 1590105600; }
                else
                if (i == 6) { unlockTime = 1598054400; }
                else
                if (i == 7) { unlockTime = 1606003200; }
                else
                if (i == 8) { unlockTime = 1613952000; }
                else
                if (i == 9) { unlockTime = 1621641600; }

                env.api.Chain.Time = unlockTime - 5;

                env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "unlockTeam", new object[] { });

                var current_balance = env.token.BalanceOf(PhantasmaContract.Team_Address);
                Assert.IsTrue(current_balance == original_balance);

                env.api.Chain.AttachDebugger(env.debugger);

                env.api.Chain.Time = unlockTime;
                env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "unlockTeam", new object[] { });

                //env.api.Chain.DettachDebugger();

                current_balance = env.token.BalanceOf(PhantasmaContract.Team_Address);
                Assert.IsTrue(current_balance == original_balance + unlock_amount);

                original_balance = current_balance;
                env.api.Chain.Time = unlockTime + 5;
                env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "unlockTeam", new object[] { });

                current_balance = env.token.BalanceOf(PhantasmaContract.Team_Address);
                Assert.IsTrue(current_balance == original_balance);
            }

            var final_balance = env.token.BalanceOf(PhantasmaContract.Team_Address);
            var expected_balance = 10 * unlock_amount;
            Assert.IsTrue(final_balance == expected_balance);
        }

        [Test]
        public void UnlockAdvisorVestedTokens()
        {
            var env = new TestEnviroment(0);

            var unlock_amount = (uint)(PhantasmaContract.advisor_monthly_supply/ PhantasmaContract.soul_decimals);

            var original_balance = env.token.BalanceOf(PhantasmaContract.Advisor_Address);

            env.api.Chain.Time = 1534895000;

            env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "unlockAdvisor", new object[] { });

            var current_balance = env.token.BalanceOf(PhantasmaContract.Advisor_Address);
            Assert.IsTrue(current_balance == original_balance);

            env.api.Chain.AttachDebugger(env.debugger);

            env.api.Chain.Time = 1534896001;
            env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "unlockAdvisor", new object[] { });

            //env.api.Chain.DettachDebugger();

            current_balance = env.token.BalanceOf(PhantasmaContract.Advisor_Address);
            Assert.IsTrue(current_balance == original_balance + unlock_amount);

            original_balance = current_balance;
            env.api.Chain.Time = 1534896002;
            env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "unlockAdvisor", new object[] { });

            current_balance = env.token.BalanceOf(PhantasmaContract.Advisor_Address);
            Assert.IsTrue(current_balance == original_balance);
        }

        [Test]
        public void TestChainSwap()
        {
            var env = new TestEnviroment(0);

            env.api.Chain.Time = 1550793601;
            env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "unlockTeam", new object[] { });

            var original_balance = env.token.BalanceOf(PhantasmaContract.Team_Address);
            Assert.IsTrue(original_balance > 0);

            uint burn_amount = 500;
            Assert.IsTrue(original_balance >= burn_amount);

            env.api.Chain.AttachDebugger(env.debugger);

            env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "chainSwap", new object[] { PhantasmaContract.Team_Address, "FE01234567890123456789FE01234567890123456789".HexToBytes(), new BigInteger((long)(burn_amount * PhantasmaContract.soul_decimals)) });

            var current_balance = env.token.BalanceOf(PhantasmaContract.Team_Address);
            Assert.IsTrue(current_balance == original_balance - burn_amount);

            var previous_balance = current_balance;
            env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "chainSwap", new object[] { PhantasmaContract.Team_Address, "FE01234567890123456789FE01234567890123456789".HexToBytes(), -1 * new BigInteger((long)(burn_amount * PhantasmaContract.soul_decimals)) });
            current_balance = env.token.BalanceOf(PhantasmaContract.Team_Address);
            Assert.IsTrue(current_balance == previous_balance);
        }

        [Test]
        public void MintRemainingTokens()
        {
            var env = new TestEnviroment(1);

            env.api.Chain.AttachDebugger(env.debugger);

            env.api.Chain.Time = PhantasmaContract.ico_war_time;
            env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "mintTokensRemaining", new object[] { });

            var current_balance = env.token.BalanceOf(PhantasmaContract.Airdrop_Address);
            Assert.IsTrue(current_balance == 0);

            env.api.Chain.Time = PhantasmaContract.ico_end_time + 1;
            env.api.CallContract(env.owner_keys, ContractTests.contract_script_hash, "mintTokensRemaining", new object[] { });

            current_balance = env.token.BalanceOf(PhantasmaContract.Airdrop_Address);
            Assert.IsTrue(current_balance == 0);

            env.api.Chain.Time = PhantasmaContract.ico_end_time + 1;
            env.api.CallContract(env.team_keys, ContractTests.contract_script_hash, "mintTokensRemaining", new object[] { });

            current_balance = env.token.BalanceOf(PhantasmaContract.Airdrop_Address);
            Assert.IsTrue(current_balance > 0);

            var expected_supply = (PhantasmaContract.max_supply / PhantasmaContract.soul_decimals);
            Assert.IsTrue(env.token.TotalSupply == expected_supply);
        }

    }
}
