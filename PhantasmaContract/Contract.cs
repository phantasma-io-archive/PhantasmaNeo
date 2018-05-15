/*

Phantasma Smart Contract
========================

Author: Sérgio Flores

Phantasma mail protocol 

txid: <to fill>
script hash: <to fill>
pubkey: <to fill>

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

        #region UTILITY METHODS

        private static bool ValidateAddress(byte[] address)
        {
            if (address.Length != 20)
                return false;
            if (address.AsBigInteger().IsZero)
                return false;
            return true;
        }

        private static bool ValidateMailboxMame(byte[] mailbox_name)
        {
            if (mailbox_name.Length < 5 || mailbox_name.Length > 20)
                return false;
            if (mailbox_name.AsBigInteger().IsZero)
                return false;
            return true;
        }

        #endregion

        // params: 0710
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
                    if (args.Length != 3) return false;
                    byte[] owner = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];
                    byte[] hash = (byte[])args[2];
                    return RemoveMessage(owner, index, hash);
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


                #region NEP5 METHODS
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();

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

                #region NEP5.1 METHODS
                if (operation == "allowance")
                {
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    return Allowance(from, to);
                }

                if (operation == "approve")
                {
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Approve(from, to, value);
                }

                if (operation == "transferFrom")
                {
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return TransferFrom(from, to, value);
                }
                #endregion

                #region SALE METHODS
                if (operation == "deploy") return Deploy();
                if (operation == "mintTokens") return MintTokens();

                if (operation == "whitelistCheck")
                {
                    if (args.Length != 1) return false;
                    byte[] address = (byte[])args[0];
                    return WhitelistCheck(address);
                }

                if (operation == "whitelistAdd")
                {
                    if (args.Length == 0) return false;
                    return WhitelistAdd(args);
                }

                if (operation == "whitelistRemove")
                {
                    if (args.Length == 0) return false;
                    return WhitelistRemove(args);
                }

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
        private static bool ExecuteChainSwap(byte[] neo_address, byte[] phantasma_address, BigInteger amount)
        {
            if (!Runtime.CheckWitness(neo_address))
                return false;
            if (!ValidateAddress(phantasma_address))
                return false;

           if (amount <= 0)
                return false;

            var balance = BalanceOf(neo_address);
            if (balance < amount)
                return false;

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

            return true;
        }
        #endregion

        #region PROTOCOL API
        private static readonly byte[] inbox_prefix = { (byte)'M', (byte)'B', (byte)'O', (byte)'X' };
        private static readonly byte[] address_prefix = { (byte)'M', (byte)'A', (byte)'D', (byte)'R' };
        private static readonly byte[] inbox_size_prefix = { (byte)'M', (byte)'S', (byte)'I', (byte)'Z' };
        private static readonly byte[] inbox_content_prefix = { (byte)'M', (byte)'C', (byte)'N', (byte)'T' };

        private static byte[] GetInboxFromAddress(byte[] address)
        {
            // unnecessary since this method only reads data
            // if (!ValidateAddress(address)) return null;
            var key = address_prefix.Concat(address);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        private static byte[] GetAddressFromInbox(byte[] mailbox)
        {
            // unnecessary since this method only reads data
            // if (!ValidateMailboxMame(mailbox)) return null;
            var key = inbox_prefix.Concat(mailbox);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        private static bool RegisterInbox(byte[] owner, byte[] mailbox)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            //if (!VerifySignature(owner, signature)) return false;
            if (!ValidateMailboxMame(mailbox)) return false;

            // verify if name already in use
            var inbox_key = inbox_prefix.Concat(mailbox);
            byte[] inbox_value = Storage.Get(Storage.CurrentContext, inbox_key);
            if (inbox_value != null) return false;

            // verify if address already has mailbox. must unregister first
            var address_key = address_prefix.Concat(owner);
            byte[] address_value = Storage.Get(Storage.CurrentContext, address_key);
            if (address_value != null) return false;

            // save owner of name
            Storage.Put(Storage.CurrentContext, inbox_key, owner);
            // save reverse mapping address => name
            Storage.Put(Storage.CurrentContext, address_key, mailbox);
            return true;
        }

        private static bool UnregisterInbox(byte[] owner)
        {
            if (!Runtime.CheckWitness(owner)) return false;

            // delete reverse mapping address => name
            var key = address_prefix.Concat(owner);
            var mailbox = Storage.Get(Storage.CurrentContext, key);
            Storage.Delete(Storage.CurrentContext, key);

            // delete mapping name => address
            key = inbox_prefix.Concat(mailbox);
            Storage.Delete(Storage.CurrentContext, key);

            // delete inbox size
            key = inbox_size_prefix.Concat(mailbox);
            Storage.Delete(Storage.CurrentContext, key);

            // Should we also delete all the messages?
            return true;
        }

        private static bool SendMessage(byte[] owner, byte[] to_mailbox, byte[] hash)
        {
            if (!Runtime.CheckWitness(owner)) return false;

            return SendMessageVerified(to_mailbox, hash);
        }

        private static bool SendMessageVerified(byte[] to_mailbox, byte[] hash)
        {
            var key = inbox_prefix.Concat(to_mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);

            // verify if name exists
            if (value == null) return false;

            // get mailbox current size
            key = inbox_size_prefix.Concat(to_mailbox);
            value = Storage.Get(Storage.CurrentContext, key);

            // increase size and save
            var mailcount = value.AsBigInteger() + 1;
            value = mailcount.AsByteArray();
            Storage.Put(Storage.CurrentContext, key, value);

            key = inbox_content_prefix.Concat(to_mailbox);
            value = mailcount.AsByteArray();
            key = key.Concat(value);

            // save mail content / hash
            Storage.Put(Storage.CurrentContext, key, hash);
            return true;
        }

        // Delete received message from inbox. By Index, with an optional content hash check
        // Index is 1-based
        private static bool RemoveMessage(byte[] owner, BigInteger index, byte[] hash)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            if (index <= 0) return false;

            var inbox_key = inbox_prefix.Concat(owner);
            var mailbox = Storage.Get(Storage.CurrentContext, inbox_key);
            // verify if name exists
            if (mailbox == null) return false;

            // get mailbox current size
            var mailcount = GetInboxCount(mailbox);
            if (index > mailcount) return false;

            var basekey = inbox_content_prefix.Concat(mailbox);
            var lastkey = basekey.Concat((mailcount-1).AsByteArray());
            var indexkey= basekey.Concat(index.AsByteArray());

            if (hash.AsBigInteger() != 0)
            {
                // check if this is the right message to delete
                var indexval = Storage.Get(Storage.CurrentContext, lastkey);
                if (indexval != hash)  return false;
            }

            // move last value to the removed index
            var lastval = Storage.Get(Storage.CurrentContext, lastkey);
            Storage.Put(Storage.CurrentContext, indexkey, lastval);
            // decrease mailbox size
            var sizekey = inbox_size_prefix.Concat(mailbox);
            Storage.Put(Storage.CurrentContext, sizekey, mailcount-1);
            return true;
        }

        private static BigInteger GetInboxCount(byte[] mailbox)
        {
            // we assume the mailbox name is never created internally,
            // it must come as an outside input and be validated there
            // if (!ValidateMailboxMame(mailbox)) return 0;

            // get mailbox current size
            var key = inbox_size_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);
            return value.AsBigInteger();
        }

        // Index is 1-based
        private static byte[] GetInboxContent(byte[] mailbox, BigInteger index)
        {
            // unnecessary, only reads data
            // if (!ValidateMailboxMame(mailbox)) return null;

            if (index <= 0)
            {
                return null;
            }

            var mailbox_size = GetInboxCount(mailbox);
            if (index > mailbox_size)
            {
                return null;
            }

            var key = inbox_content_prefix.Concat(mailbox);
            var value = index.AsByteArray();
            key = key.Concat(value);
            value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        #endregion

        #region NEP5
        //Token Settings
        public static string Name() => "Phantasma";
        public static string Symbol() => "SOUL";
        public static byte Decimals() => 8;

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> OnTransferred;

        [DisplayName("mint")]
        public static event Action<byte[], BigInteger> OnMint;

        [DisplayName("burn")]
        public static event Action<byte[], BigInteger> OnBurn;

        [DisplayName("refund")]
        public static event Action<byte[], BigInteger> OnRefund;

        [DisplayName("approve")]
        public static event Action<byte[], byte[], BigInteger> OnApproved;

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
            if (!ValidateAddress(to)) return false;
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
            // unnecessary since this method only reads data
            // if (!ValidateAddress(address)) return 0;
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        // Transfers tokens from the 'from' address to the 'to' address
        // if the 'to' address has been given an allowance to use on behalf of the 'from' address
        public static bool TransferFrom(byte[] from, byte[] to, BigInteger value)
        {
            if (!ValidateAddress(from)) return false;
            if (!ValidateAddress(to)) return false;
            if (value <= 0) return false;
            if (from == to) return true;

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;

            byte[] allowance_key = from.Concat(to);

            BigInteger allowance = Storage.Get(Storage.CurrentContext, allowance_key).AsBigInteger();

            if (allowance < value) return false;

            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);

            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);

            if (allowance == value)
                Storage.Delete(Storage.CurrentContext, allowance_key);
            else
                Storage.Put(Storage.CurrentContext, allowance_key, allowance - value);

            OnTransferred(from, to, value);

            return true;
        }

        // Gives approval to the 'to' address to use amount of tokens from the 'from' address
        // This does not guarantee that the funds will be available later to be used by the 'to' address
        public static bool Approve(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (!ValidateAddress(to)) return false;
            if (from == to) return false;
            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;

            byte[] allowance_key = from.Concat(to);

            BigInteger current_approved_amount = Storage.Get(Storage.CurrentContext, allowance_key).AsBigInteger();

            BigInteger new_approved_amount = current_approved_amount + value;

            Storage.Put(Storage.CurrentContext, allowance_key, new_approved_amount);

            OnApproved(from, to, current_approved_amount);

            return true;
        }

        // Gets the amount of tokens allowed by 'from' address to be used by 'to' address
        public static BigInteger Allowance(byte[] from, byte[] to)
        {
            // unnecessary since this method only reads data
            // if (!ValidateAddress(from)) return 0;
            // if (!ValidateAddress(to)) return 0;
            byte[] allowance_key = from.Concat(to);
            return Storage.Get(Storage.CurrentContext, allowance_key).AsBigInteger();
        }

        #endregion

        #region TOKEN SALE

        public static readonly byte[] Team_Address = "AX7hwi7MAXMquDFQ2NSbWqyDWnjS2t7MNJ".ToScriptHash();
        public static readonly byte[] Platform_Address = "ALD9pd6nsWKZbB64Uni3JtDAEQ6ejSjdtJ".ToScriptHash();        
        public static readonly byte[] Presale_Address = "AShRtCXAfzXtkFKgnjjeFpyNnGHjP6hzJ5".ToScriptHash();

        public static readonly byte[] Whitelist_Address1 = "AU3HnDtGjiH4WGPSFAGBTDXPCxZgoCnoJJ".ToScriptHash();
        public static readonly byte[] Whitelist_Address2 = "ATMSoKwfupymhmej3iLA12HabyuHPNGwDx".ToScriptHash();
        public static readonly byte[] Whitelist_Address3 = "AQgTdM2NAQvbRpCcBcXJWZnfTdESHaFFdc".ToScriptHash();
        public static readonly byte[] Whitelist_Address4 = "ALokKyd98P6EQiqoV8CdyQbs3sgAteUyX8".ToScriptHash();
        public static readonly byte[] Whitelist_Address5 = "ALtsedMdcrbsExZc6hr2va8cAzfia99ViU".ToScriptHash();
        public static readonly byte[] Whitelist_Address6 = "AeqDFJj492eDdo8Be8qhhMdSLjDZYt7k37".ToScriptHash();

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

        [DisplayName("whitelist_add")]
        public static event Action<byte[]> OnWhitelistAdd;

        [DisplayName("whitelist_remove")]
        public static event Action<byte[]> OnWhitelistRemove;

        [DisplayName("chain_swap")]
        public static event Action<byte[], byte[], BigInteger, BigInteger> OnChainSwap;

        private static readonly byte[] whitelist_prefix = { (byte)'W', (byte)'L', (byte)'S', (byte)'T' };

        // checks if address is on the whitelist
        public static bool WhitelistCheck(byte[] addressScriptHash)
        {
            // unnecessary since this method only reads data
            // if (!ValidateAddress(addressScriptHash)) return false;
            var key = whitelist_prefix.Concat(addressScriptHash);
            var val = Storage.Get(Storage.CurrentContext, key).AsBigInteger();
            if (val > 0) return true;
            else return false;
        }

        private static bool IsWhitelistingWitness()
        {
            if (Runtime.CheckWitness(Whitelist_Address1))
                return true;
            if (Runtime.CheckWitness(Whitelist_Address2))
                return true;
            if (Runtime.CheckWitness(Whitelist_Address3))
                return true;
            if (Runtime.CheckWitness(Whitelist_Address4))
                return true;
            if (Runtime.CheckWitness(Whitelist_Address5))
                return true;
            if (Runtime.CheckWitness(Whitelist_Address6))
                return true;
            return false;
        }

        // adds address to the whitelist
        public static bool WhitelistAdd(object[] addresses)
        {
            if (!IsWhitelistingWitness())
                return false;

            foreach (var entry in addresses)
            {
                var addressScriptHash = (byte[])entry;
                if (!ValidateAddress(addressScriptHash))
                    continue;

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

            return true;
        }

        // removes address from the whitelist
        public static bool WhitelistRemove(object[] addresses)
        {
            if (!IsWhitelistingWitness())
                return false;

            foreach (var entry in addresses)
            {
                var addressScriptHash = (byte[])entry;
                if (!ValidateAddress(addressScriptHash))
                    continue;

                var key = whitelist_prefix.Concat(addressScriptHash);
                var val = Storage.Get(Storage.CurrentContext, key).AsBigInteger();
                if (val.IsZero)
                {
                    continue;
                }

                Storage.Delete(Storage.CurrentContext, key);
                OnWhitelistRemove(addressScriptHash);
            }

            return true;
        }

        // initialization parameters, only once
        public static bool Deploy()
        {
            // Only Team/Admmin/Owner can deploy
            if (!Runtime.CheckWitness(Team_Address))
                return false;

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


        //  Check how many tokens can be purchased given sender, amount of neo and current conditions
        // only called from a verified context
        private static BigInteger CheckPurchaseAmount(byte[] sender, BigInteger neo_value, bool apply)
        {
            BigInteger tokens_to_refund = 0;
            BigInteger tokens_to_give = 0;

            var cur_time = Runtime.Time;
            if (cur_time >= ico_end_time || cur_time < ico_start_time)
            {
                // most common case
                if (apply == false)
                    return 0;

                tokens_to_refund = tokens_to_give;
            }
            else
            {
                tokens_to_give = (neo_value / neo_decimals) * token_swap_rate;
            }

            BigInteger current_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            BigInteger tokens_available = max_supply - current_supply;

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

        // only called from a verified context
        private static void CreditTokensToAddress(byte[] addressScriptHash, BigInteger amount)
        {
            var balance = Storage.Get(Storage.CurrentContext, addressScriptHash).AsBigInteger();
            Storage.Put(Storage.CurrentContext, addressScriptHash, amount + balance);
            OnTransferred(null, addressScriptHash, amount);
        }

        #endregion
    }
}
