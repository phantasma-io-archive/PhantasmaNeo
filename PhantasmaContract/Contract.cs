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
            if (address.AsBigInteger() == 0)
                return false;
            return true;
        }

        private static bool ValidateMailboxMame(byte[] mailbox_name)
        {
            if (mailbox_name.Length <= 4 || mailbox_name.Length >= 20)
                return false;
            if (mailbox_name.AsBigInteger() == 0)
                return false;

            int index = 0;
            while (index < mailbox_name.Length)
            {
                var c = mailbox_name[index];
                index++;

                if (c >= 97 && c <= 122) continue; // lowercase allowed
                if (c == 95) continue; // underscore allowed
                if (c >= 48 && c <= 57) continue; // numbers allowed

                return false;
            }

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

                // Check if attached assets are accepted
                byte[] sender = GetAssetSender();
                var neo_value = GetContributeValue();
                var purchase_amount = CheckPurchaseAmount(sender, neo_value, false);
                return purchase_amount > 0;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                #region PROTOCOL METHODS
                if (operation == "getMailboxFromAddress")
                {
                    if (args.Length != 1) return false;
                    byte[] address = (byte[])args[0];
                    return GetMailboxFromAddress(address);
                }
                else if (operation == "getAddressFromMailbox")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetAddressFromMailbox(mailbox);
                }
                else if (operation == "registerMailbox")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] name = (byte[])args[1];
                    return RegisterMailbox(owner, name);
                }
                else if (operation == "unregisterMailbox")
                {
                    if (args.Length != 1) return false;
                    byte[] owner = (byte[])args[0];
                    return UnregisterMailbox(owner);
                }
                else if (operation == "sendMessage")
                {
                    if (args.Length != 3) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    byte[] hash = (byte[])args[2];
                    return SendMessage(owner, to, hash);
                }
                else if (operation == "removeInboxMessage")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];
                    return RemoveInboxMessage(owner, index);
                }
                else if (operation == "removeOutboxMessage")
                {
                    if (args.Length != 3) return false;
                    byte[] owner = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];
                    return RemoveOutboxMessage(owner, index);
                }
                else if (operation == "getInboxCount")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetInboxCount(mailbox);
                }
                else if (operation == "getInboxContent")
                {
                    if (args.Length != 2) return false;
                    byte[] mailbox = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];
                    return GetInboxContent(mailbox, index);
                }
                else if (operation == "getOutboxCount")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetOutboxCount(mailbox);
                }
                else if (operation == "getOutboxContent")
                {
                    if (args.Length != 2) return false;
                    byte[] mailbox = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];
                    return GetOutboxContent(mailbox, index);
                }
                #endregion

                #region NEP5 METHODS
                else if (operation == "totalSupply") return TotalSupply();
                else if (operation == "name") return Name();
                else if (operation == "symbol") return Symbol();

                else if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }

                else if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }

                else if (operation == "decimals") return Decimals();
                #endregion

                #region NEP5.1 METHODS
                else if (operation == "allowance")
                {
                    if (args.Length != 2) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    return Allowance(from, to);
                }

                else if (operation == "approve")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Approve(from, to, value);
                }

                else if (operation == "transferFrom")
                {
                    if (args.Length != 4) return false;
                    byte[] sender = (byte[])args[0];
                    byte[] from = (byte[])args[1];
                    byte[] to = (byte[])args[2];
                    BigInteger value = (BigInteger)args[3];
                    return TransferFrom(sender, from, to, value);
                }
                #endregion

                #region SALE METHODS
                else if (operation == "deploy") return Deploy();
                else if (operation == "mintTokens") return MintTokens();

                else if (operation == "availableTokens") return AvailableTokens();

                else if (operation == "whitelistCheck")
                {
                    if (args.Length != 1) return false;
                    byte[] address = (byte[])args[0];
                    return WhitelistCheck(address);
                }

                else if (operation == "whitelistAdd")
                {
                    if (args.Length == 0) return false;
                    return WhitelistAdd(args);
                }

                else if (operation == "whitelistRemove")
                {
                    if (args.Length == 0) return false;
                    return WhitelistRemove(args);
                }

                #endregion

                #region VESTING
                else if (operation == "unlockTeam")
                {
                    if (args.Length != 0) return false;
                    return UnlockTeam();
                }

                else if (operation == "unlockAdvisor")
                {
                    if (args.Length != 0) return false;
                    return UnlockAdvisor();
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

            return false;
        }

        #region CROSSCHAIN API
        public static bool ExecuteChainSwap(byte[] neo_address, byte[] phantasma_address, BigInteger amount)
        {
            if (!Runtime.CheckWitness(neo_address))
                return false;

            if (phantasma_address == null || phantasma_address.Length < 20)
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
        private static readonly byte[] box_names_prefix = { (byte)'M', (byte)'A', (byte)'D', (byte)'R' };
        private static readonly byte[] box_owners_prefix = { (byte)'M', (byte)'B', (byte)'O', (byte)'X' };

        private static readonly byte[] inbox_size_prefix = { (byte)'M', (byte)'S', (byte)'I', (byte)'Z' };
        private static readonly byte[] inbox_content_prefix = { (byte)'M', (byte)'S', (byte)'I', (byte)'C' };

        private static readonly byte[] outbox_size_prefix = { (byte)'M', (byte)'S', (byte)'O', (byte)'Z' };
        private static readonly byte[] outbox_content_prefix = { (byte)'M', (byte)'S', (byte)'O', (byte)'C' };

        public static byte[] GetMailboxFromAddress(byte[] address)
        {
            if (!ValidateAddress(address)) return null;
            var key = box_names_prefix.Concat(address);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        public static byte[] GetAddressFromMailbox(byte[] mailbox)
        {
            if (!ValidateMailboxMame(mailbox)) return null;
            var key = box_owners_prefix.Concat(mailbox);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        public static bool RegisterMailbox(byte[] owner, byte[] mailbox)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            if (!ValidateMailboxMame(mailbox)) return false;

            // verify if name already in use
            var box_owner_key = box_owners_prefix.Concat(mailbox);
            byte[] box_owner = Storage.Get(Storage.CurrentContext, box_owner_key);
            if (box_owner != null) return false;

            // verify if address already has mailbox. must unregister first
            var box_name_key = box_names_prefix.Concat(owner);
            byte[] box_name = Storage.Get(Storage.CurrentContext, box_name_key);
            if (box_name != null) return false;

            // save owner of name
            Storage.Put(Storage.CurrentContext, box_owner_key, owner);
            // save reverse mapping address => name
            Storage.Put(Storage.CurrentContext, box_name_key, mailbox);
            return true;
        }

        // this deletes a box and all messages within it
        private static void DeleteBox(byte[] box_count_prefix, byte[] box_content_prefix, byte[] box_name)
        {
            // get mailbox current size
            var box_count_key = box_count_prefix.Concat(box_name);
            var message_count = Storage.Get(Storage.CurrentContext, box_count_key).AsBigInteger();

            while (message_count > 0)
            {
                var box_content_key = box_content_prefix.Concat(box_name);
                var value = message_count.AsByteArray();
                box_content_key = box_content_key.Concat(value);

                // delete mail content 
                Storage.Delete(Storage.CurrentContext, box_content_key);
            }

            Storage.Put(Storage.CurrentContext, box_count_key, message_count);
        }

        public static bool UnregisterMailbox(byte[] owner)
        {
            if (!Runtime.CheckWitness(owner)) return false;

            // delete reverse mapping address => name
            var key = box_names_prefix.Concat(owner);
            var mailbox = Storage.Get(Storage.CurrentContext, key);
            Storage.Delete(Storage.CurrentContext, key);

            // delete mapping name => address
            key = box_owners_prefix.Concat(mailbox);
            Storage.Delete(Storage.CurrentContext, key);

            DeleteBox(inbox_size_prefix, inbox_content_prefix, mailbox);
            DeleteBox(outbox_size_prefix, outbox_content_prefix, mailbox);

            return true;
        }

        private static void AppendToBox(byte[] box_count_prefix, byte[] box_content_prefix, byte[] box_name, byte[] message_content)
        {
            // get mailbox current size
            var box_count_key = box_count_prefix.Concat(box_name);
            var value = Storage.Get(Storage.CurrentContext, box_count_key);

            // increase size and save
            var message_count = value.AsBigInteger() + 1;
            value = message_count.AsByteArray();
            Storage.Put(Storage.CurrentContext, box_count_key, value);

            var box_content_key = box_content_prefix.Concat(box_name);
            value = message_count.AsByteArray();
            box_content_key = box_content_key.Concat(value);

            // save mail content / hash
            Storage.Put(Storage.CurrentContext, box_content_key, message_content);
        }

        public static bool SendMessage(byte[] owner, byte[] to_mailbox, byte[] hash)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            if (!ValidateMailboxMame(to_mailbox)) return false;

            var from_box_key = box_names_prefix.Concat(owner);
            var from_mailbox = Storage.Get(Storage.CurrentContext, from_box_key);
            // verify if name exists
            if (from_mailbox == null) return false;

            AppendToBox(inbox_size_prefix, inbox_content_prefix, to_mailbox, hash);
            AppendToBox(outbox_size_prefix, outbox_content_prefix, from_mailbox, hash);
            return true;
        }

        // Delete received message from message box. By Index, with an optional content hash check
        // Index is 1-based
        private static bool RemoveMessage(byte[] owner, BigInteger index, byte[] box_count_prefix, byte[] box_content_prefix)
        {
            if (index <= 0) return false;
            if (!Runtime.CheckWitness(owner)) return false;

            var mailbox_key = box_names_prefix.Concat(owner);
            var mailbox = Storage.Get(Storage.CurrentContext, mailbox_key);
            // verify if name exists
            if (mailbox == null) return false;

            // get mailbox current size
            var box_size_key = box_count_prefix.Concat(mailbox);
            var temp = Storage.Get(Storage.CurrentContext, box_size_key);
            var mailcount = temp.AsBigInteger();
            if (index > mailcount) return false;

            // copy value in last box
            var basekey = box_content_prefix.Concat(mailbox);
            var lastkey = basekey.Concat(temp);
            var lastval = Storage.Get(Storage.CurrentContext, lastkey);
            Storage.Delete(Storage.CurrentContext, lastkey);

            // decrease mailbox size
            mailcount = mailcount - 1;
            Storage.Put(Storage.CurrentContext, box_size_key, mailcount);

            // move last value to the removed index
            var indexkey = basekey.Concat(index.AsByteArray());
            Storage.Put(Storage.CurrentContext, indexkey, lastval);

            return true;
        }

        public static bool RemoveInboxMessage(byte[] owner, BigInteger index)
        {
            return RemoveMessage(owner, index, inbox_size_prefix, inbox_content_prefix);
        }

        public static bool RemoveOutboxMessage(byte[] owner, BigInteger index)
        {
            return RemoveMessage(owner, index, outbox_size_prefix, outbox_content_prefix);
        }

        // we assume the mailbox name is never created internally,
        // it must come as an outside input and be validated there
        public static BigInteger GetInboxCount(byte[] mailbox)
        {
            if (!ValidateMailboxMame(mailbox)) return 0;
            var key = inbox_size_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);
            return value.AsBigInteger();
        }

        public static BigInteger GetOutboxCount(byte[] mailbox)
        {
            if (!ValidateMailboxMame(mailbox)) return 0;
            var key = outbox_size_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);
            return value.AsBigInteger();
        }

        // Index is 1-based
        public static byte[] GetInboxContent(byte[] mailbox, BigInteger index)
        {
            if (!ValidateMailboxMame(mailbox)) return null;
            if (index <= 0) return null;

            var mailbox_size = GetInboxCount(mailbox);
            if (index > mailbox_size) return null;

            var key = inbox_content_prefix.Concat(mailbox).Concat(index.AsByteArray());
            return Storage.Get(Storage.CurrentContext, key);
        }

        // Index is 1-based
        public static byte[] GetOutboxContent(byte[] mailbox, BigInteger index)
        {
            if (!ValidateMailboxMame(mailbox)) return null;
            if (index <= 0) return null;

            var mailbox_size = GetOutboxCount(mailbox);
            if (index > mailbox_size) return null;

            var key = outbox_content_prefix.Concat(mailbox).Concat(index.AsByteArray());
            return Storage.Get(Storage.CurrentContext, key);
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

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from == to) return true;

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
            if (!ValidateAddress(address)) return 0;
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        //
        // NEP5.1 extension methods
        //

        // Transfers tokens from the 'from' address to the 'to' address
        // The Sender must have an allowance from 'From' in order to send it to the 'To'
        // This matches the ERC20 version
        public static bool TransferFrom(byte[] sender, byte[] from, byte[] to, BigInteger value)
        {
            if (!Runtime.CheckWitness(sender)) return false;
            if (!ValidateAddress(from)) return false;
            if (!ValidateAddress(to)) return false;
            if (value <= 0) return false;

            BigInteger from_value = BalanceOf(from);
            if (from_value < value) return false;
            if (from == to) return true;

            // allowance of [from] to [sender]
            byte[] allowance_key = from.Concat(sender);
            BigInteger allowance = Storage.Get(Storage.CurrentContext, allowance_key).AsBigInteger();
            if (allowance < value) return false;

            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);

            if (allowance == value)
                Storage.Delete(Storage.CurrentContext, allowance_key);
            else
                Storage.Put(Storage.CurrentContext, allowance_key, allowance - value);

            // Sender sends tokens to 'To'
            BigInteger to_value = BalanceOf(to);
            Storage.Put(Storage.CurrentContext, to, to_value + value);

            OnTransferred(from, to, value);
            return true;
        }

        // Gives approval to the 'to' address to use amount of tokens from the 'from' address
        // This does not guarantee that the funds will be available later to be used by the 'to' address
        // 'From' is the Tx Sender. Each call overwrites the previous value. This matches the ERC20 version
        public static bool Approve(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (!ValidateAddress(to)) return false;
            if (from == to) return false;

            BigInteger from_value = BalanceOf(from);
            if (from_value < value) return false;

            // overwrite previous value
            byte[] allowance_key = from.Concat(to);
            Storage.Put(Storage.CurrentContext, allowance_key, value);
            OnApproved(from, to, value);
            return true;
        }

        // Gets the amount of tokens allowed by 'from' address to be used by 'to' address
        public static BigInteger Allowance(byte[] from, byte[] to)
        {
            if (!ValidateAddress(from)) return 0;
            if (!ValidateAddress(to)) return 0;
            byte[] allowance_key = from.Concat(to);
            return Storage.Get(Storage.CurrentContext, allowance_key).AsBigInteger();
        }

        #endregion

        #region TOKEN SALE

        public static readonly byte[] Team_Address = "AGUNSWYyZDVQpzL6YbuSYc5qqbG7fDcMuZ".ToScriptHash();
        public static readonly byte[] Advisor_Address = "AKvFhNqJUkGzHCiwrEAfSEG3fP1fNtji1F".ToScriptHash();
        public static readonly byte[] Platform_Address = "AQFQmVQi9VReLhym1tF3UfPk4EG3VKbAwN".ToScriptHash();
        public static readonly byte[] Presale_Address = "ARWHJefSbhayC2gurKkpjMHm5ReaJZLLJ3".ToScriptHash();

        public static readonly byte[] Whitelist_Address1 = "AYQ3FMw4oEjqEaHp4n6ZxJTpBuyp5FTCge".ToScriptHash();
        public static readonly byte[] Whitelist_Address2 = "ATMSoKwfupymhmej3iLA12HabyuHPNGwDx".ToScriptHash();
        public static readonly byte[] Whitelist_Address3 = "AQgTdM2NAQvbRpCcBcXJWZnfTdESHaFFdc".ToScriptHash();
        public static readonly byte[] Whitelist_Address4 = "ALokKyd98P6EQiqoV8CdyQbs3sgAteUyX8".ToScriptHash();
        public static readonly byte[] Whitelist_Address5 = "ALtsedMdcrbsExZc6hr2va8cAzfia99ViU".ToScriptHash();
        public static readonly byte[] Whitelist_Address6 = "AeqDFJj492eDdo8Be8qhhMdSLjDZYt7k37".ToScriptHash();

        public const ulong soul_decimals = 100000000; //decided by Decimals()
        public const ulong neo_decimals = 100000000;

        //ICO Settings
        public static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };

        public const ulong max_supply = 91136510 * soul_decimals; // total token amount
        public const ulong team_supply = 14500000 * soul_decimals; // team token amount
        public const ulong advisor_supply = 5500000 * soul_decimals; // advisor token amount
        public const ulong platform_supply = 15000000 * soul_decimals; // company token amount
        public const ulong presale_supply = 43503435 * soul_decimals; // presale token amount

        public const ulong team_monthly_supply = 1450000 * soul_decimals; // team monthly share
        public const ulong advisor_monthly_supply = 550000 * soul_decimals; // advisor monthly share

        public const ulong token_swap_rate = 273 * soul_decimals; // how many tokens you get per NEO
        public const ulong token_initial_cap = 10 * token_swap_rate; // max tokens than an individual can buy from to the sale in the first round, guaranteed
        public const ulong token_war_cap = 60 * token_swap_rate; // max tokens than an individual can buy from to the sale in the second round, not guaranteed

        public const uint ico_start_time = 1526947200; // 22 May 00h00 UTC       
        public const uint ico_war_time = 1527033600; // 23 May 00h00 UTC
        public const uint ico_end_time = 1527552000; // 29 May 00h00 UTC

        [DisplayName("whitelist_add")]
        public static event Action<byte[]> OnWhitelistAdd;

        [DisplayName("whitelist_remove")]
        public static event Action<byte[]> OnWhitelistRemove;

        [DisplayName("chain_swap")]
        public static event Action<byte[], byte[], BigInteger, BigInteger> OnChainSwap;

        private static readonly byte[] whitelist_prefix = { (byte)'W', (byte)'L', (byte)'S', (byte)'T' };
        private static readonly byte[] bought_prefix = { (byte)'B', (byte)'G', (byte)'T', (byte)'H' };
        private static readonly byte[] mint_prefix = { (byte)'M', (byte)'I', (byte)'N', (byte)'T' };

        // checks if address is on the whitelist
        public static bool WhitelistCheck(byte[] address)
        {
            if (!ValidateAddress(address)) return false;
            var key = whitelist_prefix.Concat(address);
            var val = Storage.Get(Storage.CurrentContext, key).AsBigInteger();
            if (val > 0) return true;
            else return false;
        }

        private static bool IsWhitelistingWitness()
        {
            if (Runtime.CheckWitness(Team_Address))
                return true;
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
                if (val == 0)
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

            var initialSupply = team_supply + advisor_supply + presale_supply + platform_supply;

            // team and advisor supply is locked, storage stays at zero here

            OnMint(Presale_Address, presale_supply);
            Storage.Put(Storage.CurrentContext, Presale_Address, presale_supply);

            OnMint(Platform_Address, platform_supply);
            Storage.Put(Storage.CurrentContext, Platform_Address, platform_supply);

            Storage.Put(Storage.CurrentContext, "totalSupply", initialSupply);

            return true;
        }

        // The function MintTokens is only usable by the chosen wallet
        // contract to mint a number of tokens proportional to the
        // amount of neo sent to the wallet contract. The function
        // can only be called during the tokenswap period
        public static bool MintTokens()
        {
            byte[] sender = GetAssetSender();
            // contribute asset is not neo
            if (sender.Length == 0)
            {
                return false;
            }

            var tx_key = mint_prefix.Concat(sender);
            var last_tx = Storage.Get(Storage.CurrentContext, tx_key);

            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            if (last_tx == tx.Hash)
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

            // mint tokens to sender
            CreditTokensToAddress(sender, token_amount);
            var bought_key = bought_prefix.Concat(sender);
            var total_bought = Storage.Get(Storage.CurrentContext, bought_key).AsBigInteger();
            Storage.Put(Storage.CurrentContext, bought_key, total_bought + token_amount);

            // adjust total supply
            var current_total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", current_total_supply + token_amount);
            Storage.Put(Storage.CurrentContext, tx_key, tx.Hash);

            return true;
        }

        public static BigInteger AvailableTokens()
        {
            BigInteger current_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            BigInteger tokens_available = max_supply - current_supply;
            return tokens_available;
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

            var bought_key = bought_prefix.Concat(sender);
            var total_bought = Storage.Get(Storage.CurrentContext, bought_key).AsBigInteger();

            var whitelist_key = whitelist_prefix.Concat(sender);
            var whitelist_entry = Storage.Get(Storage.CurrentContext, whitelist_key).AsBigInteger();
            if (whitelist_entry <= 0) // not whitelisted
            {
                tokens_to_refund += tokens_to_give;
                tokens_to_give = 0;
            }
            else
            {
                var new_balance = tokens_to_give + total_bought;

                // check individual cap
                BigInteger individual_cap;

                if (cur_time < ico_war_time)
                {
                    individual_cap = token_initial_cap;
                }
                else
                {
                    individual_cap = token_war_cap;
                }

                if (new_balance > individual_cap)
                {
                    var diff = (new_balance - individual_cap);
                    tokens_to_refund += diff;
                    tokens_to_give -= diff;
                }
            }

            if (apply)
            {
                // here we do partial refunds only, full refunds are done in verification trigger!
                if (tokens_to_refund > 0 && tokens_to_give > 0)
                {
                    // convert amount to NEO
                    OnRefund(sender, (tokens_to_refund / token_swap_rate) * neo_decimals);
                }
            }

            return tokens_to_give;
        }

        // check whether asset is neo and get sender script hash
        private static byte[] GetAssetSender()
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
            var receiver = GetReceiver();

            TransactionOutput[] inputs = tx.GetReferences();
            foreach (var input in inputs)
            {
                if (input.ScriptHash == receiver)
                {
                    return 0;
                }
            }

            TransactionOutput[] outputs = tx.GetOutputs();
            BigInteger value = 0;

            // get the total amount of Neo
            foreach (var output in outputs)
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
            OnMint(addressScriptHash, amount);
        }

        #endregion

        #region VESTING
        public static bool UnlockTeam()
        {
            if (!Runtime.CheckWitness(Team_Address))
            {
                return false;
            }

            var key = "team_lock";
            var lockStage = Storage.Get(Storage.CurrentContext, key).AsBigInteger();

            uint unlockTime;
            if (lockStage == 0) { unlockTime = 1550793600; }
            else
            if (lockStage == 1) { unlockTime = 1558483200; }
            else
            if (lockStage == 2) { unlockTime = 1566432000; }
            else
            if (lockStage == 3) { unlockTime = 1574380800; }
            else
            if (lockStage == 4) { unlockTime = 1582329600; }
            else
            if (lockStage == 5) { unlockTime = 1590105600; }
            else
            if (lockStage == 6) { unlockTime = 1598054400; }
            else
            if (lockStage == 7) { unlockTime = 1606003200; }
            else
            if (lockStage == 8) { unlockTime = 1613952000; }
            else
            if (lockStage == 9) { unlockTime = 1621641600; }
            else
            {
                return false;
            }

            if (Runtime.Time < unlockTime)
            {
                return false;
            }

            lockStage = lockStage + 1;
            Storage.Put(Storage.CurrentContext, key, lockStage);

            var amount = team_monthly_supply;

            CreditTokensToAddress(Team_Address, amount);
            return true;
        }

        public static bool UnlockAdvisor()
        {
            if (!Runtime.CheckWitness(Advisor_Address))
            {
                return false;
            }

            var key = "advisor_lock";
            var lockStage = Storage.Get(Storage.CurrentContext, key).AsBigInteger();

            uint unlockTime;
            if (lockStage == 0) { unlockTime = 1534896000; }
            else
            if (lockStage == 1) { unlockTime = 1537574400; }
            else
            if (lockStage == 2) { unlockTime = 1540166400; }
            else
            if (lockStage == 3) { unlockTime = 1542844800; }
            else
            if (lockStage == 4) { unlockTime = 1545523200; }
            else
            if (lockStage == 5) { unlockTime = 1548115200; }
            else
            if (lockStage == 6) { unlockTime = 1550793600; }
            else
            if (lockStage == 7) { unlockTime = 1553212800; }
            else
            if (lockStage == 8) { unlockTime = 1555891200; }
            else
            if (lockStage == 9) { unlockTime = 1558483200; }
            else
            {
                return false;
            }

            if (Runtime.Time < unlockTime)
            {
                return false;
            }

            lockStage = lockStage + 1;
            Storage.Put(Storage.CurrentContext, key, lockStage);

            var amount = advisor_monthly_supply;

            CreditTokensToAddress(Advisor_Address, amount);
            return true;
        }
        #endregion

    }
}
