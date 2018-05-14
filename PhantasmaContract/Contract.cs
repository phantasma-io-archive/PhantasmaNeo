/*

Phantasma Smart Contract
========================

Author: Sérgio Flores

Phantasma mail protocol 

txid: 0xf1f418b3235214aba77bb9ecd72b820c3f74419835aa58f045e195d88aba996a

script hash: 0xde1a53be359e8be9f3d11627bcca40548a2d5bc1


pubkey: 029ada24a94e753729768b90edee9d24ec9027cb64cea406f8ab296fce264597f4

*/
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class PhantasmaContract : Framework.SmartContract
    {
        //  params: 0710
        // return : 05

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                // param Owner must be script hash
                bool isOwner = Runtime.CheckWitness(Team_Address);

                if (isOwner)
                {
                    return true;
                }
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                #region PROTOCOL METHODS
                if (operation == "getInboxFromAddress")
                {
                    if (args.Length != 1) return false;
                    byte[] address = (byte[])args[0];
                    return GetInboxFromAddress(address);
                }
                if (operation == "getAddressFromInbox")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetAddressFromInbox(mailbox);
                }
                if (operation == "registerInbox")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] name = (byte[])args[1];
                    return RegisterInbox(owner, name);
                }
                if (operation == "unregisterInbox")
                {
                    if (args.Length != 1) return false;
                    byte[] owner = (byte[])args[0];
                    return UnregisterInbox(owner);
                }
                if (operation == "sendMessage")
                {
                    if (args.Length != 3) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    byte[] hash = (byte[])args[2];
                    return SendMessage(owner, to, hash);
                }
                if (operation == "removeMessage")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] hash = (byte[])args[1];
                    return RemoveMessage(owner, hash);
                }
                if (operation == "getInboxCount")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetInboxCount(mailbox);
                }

                if (operation == "getInboxContent")
                {
                    if (args.Length != 2) return false;
                    byte[] mailbox = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];
                    return GetInboxContent(mailbox, index);
                }
                #endregion

                #region SALE METHODS
                if (operation == "deploy") return Deploy();
                if (operation == "mintTokens") return MintTokens();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();

                if (operation == "whitelistCheck")
                {
                    if (args.Length != 1) return "invalid args";
                    byte[] address = (byte[])args[0];
                    return WhitelistCheck(address);
                }

                if (operation == "whitelistAdd")
                {
                    if (args.Length == 0) return "invalid args";
                    return WhitelistAdd(args);
                }

                if (operation == "whitelistRemove")
                {
                    if (args.Length == 0) return "invalid args";
                    return WhitelistRemove(args);
                }

                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }

                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }

                if (operation == "decimals") return Decimals();
                #endregion

                #region CROSSCHAIN METHODS
                if (operation == "chainSwap")
                {
                    if (args.Length != 3) return false;
                    byte[] neo_address = (byte[])args[0];
                    byte[] phantasma_address = (byte[])args[1];
                    BigInteger amount = (BigInteger)args[2];
                    return ExecuteChainSwap(neo_address, phantasma_address, amount);
                }
                #endregion
            }

            //refund if not can purchase
            byte[] sender = GetSender();
            var neo_value = GetContributeValue();

            var purchase_amount = CheckPurchaseAmount(sender, neo_value, false);
            return purchase_amount > 0;
        }

        #region CROSSCHAIN API
        private static string ExecuteChainSwap(byte[] neo_address, byte[] phantasma_address, BigInteger amount)
        {
            if (!Runtime.CheckWitness(neo_address)) return "invalid owner";

           if (amount <= 0)
                return false;

            var balance = BalanceOf(neo_address);

            if (balance < amount)
            {
                return "not enough balance";
            }

            // burn those tokens in this chain
            balance -= amount;
            Storage.Put(Storage.CurrentContext, neo_address, balance);
            OnBurn(neo_address, amount);

            var current_total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", current_total_supply - amount);

            // update swap ID, this makes sure every token swap has a unique ID
            var last_swap_index = Storage.Get(Storage.CurrentContext, "chain_swap").AsBigInteger();
            last_swap_index = last_swap_index + 1;
            Storage.Put(Storage.CurrentContext, "chain_swap", last_swap_index);

            // do a notify event
            OnChainSwap(neo_address, phantasma_address, amount, last_swap_index);

            return "ok";
        }
        #endregion

        #region PROTOCOL API
        private static readonly byte[] inbox_prefix = { (byte)'M', (byte)'B', (byte)'O', (byte)'X' };
        private static readonly byte[] address_prefix = { (byte)'M', (byte)'A', (byte)'D', (byte)'R' };
        private static readonly byte[] inbox_size_prefix = { (byte)'M', (byte)'S', (byte)'I', (byte)'Z' };
        private static readonly byte[] inbox_content_prefix = { (byte)'M', (byte)'C', (byte)'N', (byte)'T' };

        private static byte[] GetInboxFromAddress(byte[] address)
        {
            var key = address_prefix.Concat(address);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        private static byte[] GetAddressFromInbox(byte[] mailbox)
        {
            var key = inbox_prefix.Concat(mailbox);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        private static bool RegisterInbox(byte[] owner, byte[] mailbox)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            //if (!VerifySignature(owner, signature)) return false;

            var key = inbox_prefix.Concat(mailbox);
            byte[] value = Storage.Get(Storage.CurrentContext, key);

            // verify if name already in use
            if (value != null) return false;

            // save owner of name 
            Storage.Put(Storage.CurrentContext, key, owner);

            // save reverse mapping address => name
            key = address_prefix.Concat(owner);
            Storage.Put(Storage.CurrentContext, key, mailbox);

            return true;
        }

        private static bool UnregisterInbox(byte[] owner)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            //if (!VerifySignature(owner, signature)) return false;

            // delete reverse mapping address => name
            var key = address_prefix.Concat(owner);
            var mailbox = Storage.Get(Storage.CurrentContext, key);
            Storage.Delete(Storage.CurrentContext, key);

            // delete mapping name => address
            key = inbox_prefix.Concat(mailbox);
            Storage.Delete(Storage.CurrentContext, key);
            return true;
        }

        private static bool SendMessage(byte[] owner, byte[] to, byte[] hash)
        {
            if (!Runtime.CheckWitness(owner)) return false;

            return SendMessageVerified(to, hash);
        }

        private static bool SendMessageVerified(byte[] to, byte[] hash)
        {
            var key = inbox_prefix.Concat(to);
            var value = Storage.Get(Storage.CurrentContext, key);

            // verify if name exists
            if (value == null) return false;

            // get mailbox current size
            key = inbox_size_prefix.Concat(to);
            value = Storage.Get(Storage.CurrentContext, key);

            // increase size and save
            var mailcount = value.AsBigInteger() + 1;
            value = mailcount.AsByteArray();
            Storage.Put(Storage.CurrentContext, key, value);

            key = inbox_content_prefix.Concat(to);
            value = mailcount.AsByteArray();
            key = key.Concat(value);

            // save mail content / hash
            Storage.Put(Storage.CurrentContext, key, hash);
            return true;
        }

        // Delete received message from inbox.
        // Very slow. Each Get costs 0.1 gas
        // Search by "hash" (value) is not ideal since it's content-dependent
        private static bool RemoveMessage(byte[] owner, byte[] hash)
        {
            if (!Runtime.CheckWitness(owner)) return false;

            var key = inbox_prefix.Concat(owner);
            var value = Storage.Get(Storage.CurrentContext, key);
            // verify if name exists
            if (value == null) return false;

            // get mailbox current size
            key = inbox_size_prefix.Concat(owner);
            var mailcount = Storage.Get(Storage.CurrentContext, key).AsBigInteger();

            var basekey = inbox_content_prefix.Concat(owner);

            for (BigInteger i = 0; i < mailcount; i = i+1)
            {
                key = basekey.Concat(i.AsByteArray());
                var val = Storage.Get(Storage.CurrentContext, key);
                if (val == hash)
                {
                    Storage.Delete(Storage.CurrentContext, key);
                    return true;
                }
            }

            return false;
        }

        private static BigInteger GetInboxCount(byte[] mailbox)
        {
            // get mailbox current size
            var key = inbox_size_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);

            return value.AsBigInteger();
        }

        private static byte[] GetInboxContent(byte[] mailbox, BigInteger index)
        {
            if (index <= 0)
            {
                return null;
            }

            // get mailbox current size
            var key = inbox_size_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);

            var size = value.AsBigInteger();
            if (index > size)
            {
                return null;
            }

            key = inbox_content_prefix.Concat(mailbox);
            value = index.AsByteArray();
            key = key.Concat(value);

            // save mail content / hash
            value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        #endregion

        #region TOKEN SALE
        //Token Settings
        public static string Name() => "Phantasma";
        public static string Symbol() => "SOUL";
        public static byte Decimals() => 8;

        public static readonly byte[] Team_Address = "AX7hwi7MAXMquDFQ2NSbWqyDWnjS2t7MNJ".ToScriptHash();
        public static readonly byte[] Platform_Address = "ALD9pd6nsWKZbB64Uni3JtDAEQ6ejSjdtJ".ToScriptHash();        
        public static readonly byte[] Presale_Address = "AShRtCXAfzXtkFKgnjjeFpyNnGHjP6hzJ5".ToScriptHash();
        public static readonly byte[] Whitelist_Address = "AWUtY1cefNshv9xWZVGyKVMmB1Tm5eAzhi".ToScriptHash();

        private const ulong soul_decimals = 100000000; //decided by Decimals()
        private const ulong neo_decimals = 100000000;

        //ICO Settings
        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        private const ulong max_supply = 100000000 * soul_decimals; // total token amount
        private const ulong team_supply = 20000000 * soul_decimals; // company token amount
        private const ulong platform_supply = 15000000 * soul_decimals; // company token amount
        private const ulong presale_supply = 45000000 * soul_decimals; // employee token amount

        private const ulong token_swap_rate = 420 * soul_decimals; // how many tokens you get per NEO
        private const ulong token_individual_cap = 554 * token_swap_rate; // max tokens than an individual can buy from to the sale

        private const uint ico_start_time = 1526947200; // 22 May 00h00 UTC
        private const uint ico_war_time = 1526958000; // 22 May 03h00 UTC
        private const uint ico_end_time = 1527552000; // 29 May 00h00 UTC

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> OnTransferred;

        [DisplayName("mint")]
        public static event Action<byte[], BigInteger> OnMint;

        [DisplayName("burn")]
        public static event Action<byte[], BigInteger> OnBurn;

        [DisplayName("refund")]
        public static event Action<byte[], BigInteger> OnRefund;

        [DisplayName("whitelist_add")]
        public static event Action<byte[]> OnWhitelistAdd;

        [DisplayName("whitelist_remove")]
        public static event Action<byte[]> OnWhitelistRemove;

        [DisplayName("chain_swap")]
        public static event Action<byte[], byte[], BigInteger, BigInteger> OnChainSwap;

        private static readonly byte[] whitelist_prefix = { (byte)'W', (byte)'L', (byte)'S', (byte)'T' };

        // checks if address is on the whitelist
        public static string WhitelistCheck(byte[] addressScriptHash)
        {
            var key = whitelist_prefix.Concat(addressScriptHash);
            var val = Storage.Get(Storage.CurrentContext, key).AsBigInteger();
            if (val > 0) return "on";
            else return "off";
        }

        // adds address to the whitelist
        public static string WhitelistAdd(object[] addresses)
        {
            if (!Runtime.CheckWitness(Whitelist_Address))
            {
                return "not owner";
            }

            foreach (var entry in addresses)
            {
                var addressScriptHash = (byte[])entry;
                var key = whitelist_prefix.Concat(addressScriptHash);

                var val = Storage.Get(Storage.CurrentContext, key).AsBigInteger();

                if (val > 0)
                {
                    continue;
                }

                val = 1;
                Storage.Put(Storage.CurrentContext, key, val);

                OnWhitelistAdd(addressScriptHash);
            }

            return "ok";
        }

        // removes address from the whitelist
        public static string WhitelistRemove(object[] addresses)
        {
            if (!(Runtime.CheckWitness(Whitelist_Address)))
            {
                return "not owner";
            }

            foreach (var entry in addresses)
            {
                var addressScriptHash = (byte[])entry;
                var key = whitelist_prefix.Concat(addressScriptHash);

                var val = Storage.Get(Storage.CurrentContext, key).AsBigInteger();

                if (val == 0)
                {
                    continue;
                }

                Storage.Delete(Storage.CurrentContext, key);

                OnWhitelistRemove(addressScriptHash);
            }

            return "ok";
        }

        // initialization parameters, only once
        public static bool Deploy()
        {
            var current_total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            if (current_total_supply != 0)
            {
                return false;
            }

            var initialSupply = team_supply + presale_supply + platform_supply;

            OnMint(Team_Address, team_supply);
            Storage.Put(Storage.CurrentContext, Team_Address, team_supply);

            OnMint(Team_Address, presale_supply);
            Storage.Put(Storage.CurrentContext, Presale_Address, presale_supply);

            OnMint(Team_Address, platform_supply);
            Storage.Put(Storage.CurrentContext, Platform_Address, platform_supply);

            Storage.Put(Storage.CurrentContext, "lastDistribution", ico_start_time);

            Storage.Put(Storage.CurrentContext, "totalSupply", initialSupply);

            return true;
        }

        // The function MintTokens is only usable by the chosen wallet
        // contract to mint a number of tokens proportional to the
        // amount of neo sent to the wallet contract. The function
        // can only be called during the tokenswap period
        public static bool MintTokens()
        {
            byte[] sender = GetSender();
            // contribute asset is not neo
            if (sender.Length == 0)
            {
                return false;
            }

            var contribute_value = GetContributeValue();

            // calculate how many tokens 
            var token_amount = CheckPurchaseAmount(sender, contribute_value, true);
            if (token_amount <= 0)
            {
                return false;
            }

            // adjust total supply
            var current_total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", current_total_supply + token_amount);

            return true;
        }

        // get the total token supply
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // function that is always called when someone wants to transfer tokens.
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (from == to) return true;
            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            OnTransferred(from, to, value);
            return true;
        }

        // Get the account balance of another account with address
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        //  Check how many tokens can be purchased given sender, amount of neo and current conditions
        private static BigInteger CheckPurchaseAmount(byte[] sender, BigInteger neo_value, bool apply)
        {
            BigInteger tokens_to_refund = 0;
            BigInteger tokens_to_give = (neo_value / neo_decimals) * token_swap_rate;

            //Runtime.Notify(new object[] { "NEO", neo_value });
            //Runtime.Notify(new object[] { "SOUL", tokens_to_give });

            BigInteger current_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            BigInteger tokens_available = max_supply - current_supply;

            var cur_time = Runtime.Time;

            if (cur_time < ico_start_time || cur_time >= ico_end_time)
            {
                tokens_to_refund = tokens_to_give;
                tokens_to_give = 0;
            }

            // check global hard cap
            if (tokens_to_give > tokens_available)
            {
                tokens_to_refund += (tokens_to_give - tokens_available);
                tokens_to_give = tokens_available;
            }

            var key = whitelist_prefix.Concat(sender);

            var whitelist_entry = Storage.Get(Storage.CurrentContext, key).AsBigInteger();

            if (whitelist_entry <= 0) // not whitelisted
            {
                tokens_to_refund += tokens_to_give;
                tokens_to_give = 0;
            }
            else
            {
                var balance = Storage.Get(Storage.CurrentContext, sender).AsBigInteger();
                var new_balance = tokens_to_give + balance;

                // check individual cap
                if (cur_time  < ico_war_time && new_balance > token_individual_cap)
                {
                    var diff = (new_balance - token_individual_cap);
                    tokens_to_refund += diff;
                    tokens_to_give -= diff;
                }
            }

            if (apply)
            {
                if (tokens_to_refund > 0)
                {
                    // convert amount to NEO
                    OnRefund(sender, (tokens_to_refund / token_swap_rate) * neo_decimals);
                }

                if (tokens_to_give > 0)
                {
                    // mint tokens to sender
                    CreditTokensToAddress(sender, tokens_to_give);

                    Storage.Put(Storage.CurrentContext, "totalSupply", current_supply + tokens_to_give);
                }
            }

            return tokens_to_give;
        }

        // check whether asset is neo and get sender script hash
        private static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            var receiver = GetReceiver();
            foreach (TransactionOutput output in reference)
            {
                if (output.ScriptHash != receiver && output.AssetId == neo_asset_id)
                {
                    return output.ScriptHash;
                }
            }
            return new byte[] { };
        }

        // get smart contract script hash
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        // get all you contribute neo amount
        private static BigInteger GetContributeValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            BigInteger value = 0;
            var receiver = GetReceiver();
            // get the total amount of Neo
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == receiver && output.AssetId == neo_asset_id)
                {
                    value += output.Value;
                }
            }
            return value;
        }

        private static void CreditTokensToAddress(byte[] addressScriptHash, BigInteger amount)
        {
            var balance = Storage.Get(Storage.CurrentContext, addressScriptHash).AsBigInteger();
            Storage.Put(Storage.CurrentContext, addressScriptHash, amount + balance);
            OnTransferred(null, addressScriptHash, amount);
        }

        #endregion
    }
}
