/*

Phantasma Smart Contract
========================

Author: Sérgio Flores

Phantasma mail protocol 

txid: 0xf1f418b3235214aba77bb9ecd72b820c3f74419835aa58f045e195d88aba996a

script hash: 0xde1a53be359e8be9f3d11627bcca40548a2d5bc1

https://medium.com/neon-exchange/nex-ico-template-4ca7ba19fc8b

pubkey: 029ada24a94e753729768b90edee9d24ec9027cb64cea406f8ab296fce264597f4

Message types:
Mail -> The most basic message, text only (with support for some HTML tags)
Chat -> Same as mail, but every chat message is grouped into a single thread
File -> Any binary file
Image -> Same as file, but opens an imageview if avaiable
Music -> Same as file, but opens an audioplayer if avaiable
Sodoku -> Can display as text or soduku viewer if availablle
Dungeon -> Opens a playable dungeon (Phantasma World only)
*/
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;

namespace Neo.SmartContract
{
    public class PhantasmaContract : Framework.SmartContract
    {
        //  params: 0710
        // return : 05
        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {
                #region MAIL METHODS
                if (operation == "getMailboxFromAddress")
                {
                    if (args.Length != 1) return false;
                    byte[] address = (byte[])args[0];
                    return GetMailboxFromAddress(address);
                }
                if (operation == "getAddressFromMailbox")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetAddressFromMailbox(mailbox);
                }
                if (operation == "registerMailbox")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] name = (byte[])args[1];
                    return RegisterMailbox(owner, name);
                }
                if (operation == "sendMessage")
                {
                    if (args.Length != 3) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    byte[] hash = (byte[])args[2];
                    return SendMessage(owner, to, hash);
                }
                if (operation == "getMailCount")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetMailCount(mailbox);
                }

                if (operation == "getMailContent")
                {
                    if (args.Length != 2) return false;
                    byte[] mailbox = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];
                    return GetMailContent(mailbox, index);
                }

                /*          if (operation == "notifySubscribers")
                          {
                              if (args.Length != 3) return false;
                              byte[] from = (byte[])args[0];
                              byte[] signature = (byte[])args[1];
                              byte[] hash = (byte[])args[2];
                              return NotifySubscribers(from, signature, hash);
                          }

                          if (operation == "getSubscriberCount")
                          {
                              if (args.Length != 1) return false;
                              byte[] mailbox = (byte[])args[0];
                              return GetSubscriberCount(mailbox);
                          }

                          if (operation == "subscribeTo")
                          {
                              if (args.Length != 3) return false;
                              byte[] from = (byte[])args[0];
                              byte[] signature = (byte[])args[1];
                              byte[] to = (byte[])args[2];
                              return SubscribeTo(from, signature, to);
                          }*/

                #endregion

                #region TOKEN / ICO METHODS
                if (operation == "deploy") return Deploy();
                if (operation == "mintTokens") return MintTokens();
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
            }

            //you can choice refund or not refund
            byte[] sender = GetSender();
            ulong contribute_value = GetContributeValue();
            if (contribute_value > 0 && sender.Length != 0)
            {
                Refund(sender, contribute_value);
            }

            return false;
        }

        #region TOKEN API
        public static string Name() => "Souls";
        public static string Symbol() => "SOUL";
        public static readonly byte[] Owner = { 47, 60, 170, 33, 216, 40, 148, 2, 242, 150, 9, 84, 154, 50, 237, 160, 97, 90, 55, 183 }; // FIXME : change later
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        private const ulong neo_decimals = 100000000;

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        // initialization parameters, only once
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, Owner, pre_ico_cap);
            Storage.Put(Storage.CurrentContext, "totalSupply", pre_ico_cap);
            Transferred(null, Owner, pre_ico_cap);
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
            Transferred(from, to, value);
            return true;
        }

        // get the account balance of another account with address
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }
        #endregion

        #region ICO API
        //ICO Settings
        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        private const ulong total_amount = 100000000 * factor; // total token amount
        private const ulong pre_ico_cap = 30000000 * factor; // pre ico token amount
        private const ulong basic_rate = 1000 * factor;
        private const int ico_start_time = 1506787200; // FIXME: change later
        private const int ico_end_time = 1538323200; // FIXME: change later

        [DisplayName("refund")]
        public static event Action<byte[], BigInteger> Refund;

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
            ulong contribute_value = GetContributeValue();
            // the current exchange rate between ico tokens and neo during the token swap period
            ulong swap_rate = CurrentSwapRate();
            // crowdfunding failure
            if (swap_rate == 0)
            {
                Refund(sender, contribute_value);
                return false;
            }
            // you can get current swap token amount
            ulong token = CurrentSwapToken(sender, contribute_value, swap_rate);
            if (token == 0)
            {
                return false;
            }
            // crowdfunding success
            BigInteger balance = Storage.Get(Storage.CurrentContext, sender).AsBigInteger();
            Storage.Put(Storage.CurrentContext, sender, token + balance);
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", token + totalSupply);
            Transferred(null, sender, token);
            return true;
        }

        // The function CurrentSwapRate() returns the current exchange rate
        // between ico tokens and neo during the token swap period
        private static ulong CurrentSwapRate()
        {
            const int ico_duration = ico_end_time - ico_start_time;
            uint now = GetCurrentTime();
            int time = (int)now - ico_start_time;
            if (time < 0)
            {
                return 0;
            }
            else if (time < ico_duration)
            {
                return basic_rate;
            }
            else
            {
                return 0;
            }
        }

        //whether over contribute capacity, you can get the token amount
        private static ulong CurrentSwapToken(byte[] sender, ulong value, ulong swap_rate)
        {
            ulong token = value / neo_decimals * swap_rate;
            BigInteger total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            BigInteger balance_token = total_amount - total_supply;
            if (balance_token <= 0)
            {
                Refund(sender, value);
                return 0;
            }
            else if (balance_token < token)
            {
                Refund(sender, (token - balance_token) / swap_rate * neo_decimals);
                token = (ulong)balance_token;
            }
            return token;
        }

        // get all you contribute neo amount
        private static ulong GetContributeValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            // get the total amount of Neo
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == neo_asset_id)
                {
                    value += (ulong)output.Value;
                }
            }
            return value;
        }
        #endregion

        #region UTILS
        // check whether asset is neo and get sender script hash
        private static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            // you can choice refund or not refund
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == neo_asset_id) return output.ScriptHash;
            }
            return new byte[0];
        }

        // get smart contract script hash
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }


        private static uint GetCurrentTime()
        {
            uint now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            now += 15;
            return now;
        }

        #endregion

        #region MAILBOX API
        private static readonly byte[] address_mailbox_prefix = { (byte)'M', (byte)'A', (byte)'D', (byte)'R' };

        private static readonly byte[] mailbox_address_prefix = { (byte)'M', (byte)'B', (byte)'O', (byte)'X' };
        private static readonly byte[] mailbox_name_prefix = { (byte)'M', (byte)'B', (byte)'O', (byte)'N' };
        private static readonly byte[] mailbox_size_prefix = { (byte)'M', (byte)'B', (byte)'O', (byte)'Z' };
        private static readonly byte[] mailbox_content_prefix = { (byte)'M', (byte)'B', (byte)'O', (byte)'C' };

        private static readonly byte[] subscription_size_prefix = { (byte)'S', (byte)'U', (byte)'B', (byte)'Z' };
        private static readonly byte[] subscription_content_prefix = { (byte)'S', (byte)'U', (byte)'B', (byte)'C' };

        // eg: AFqqgTdf8fa3zw193EzC3LSAD1ocvc5fho =>  hello@phantasma.io
        public static byte[] GetMailboxFromAddress(byte[] neo_address)
        {
            var key = address_mailbox_prefix.Concat(neo_address);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        // eg: hello@phantasma.io => AFqqgTdf8fa3zw193EzC3LSAD1ocvc5fho
        public static byte[] GetAddressFromMailbox(byte[] mailbox)
        {
            var key = mailbox_address_prefix.Concat(mailbox);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        public static string GetMailboxName(byte[] mailbox)
        {
            var key = mailbox_address_prefix.Concat(mailbox);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value.AsString();
        }

        public static bool SetMailboxName(byte[] caller_address, byte[] mailbox, string name)
        {
            // only owner of mailbox can change its name
            //if (!Runtime.CheckWitness(caller_address)) return false;

            var key = mailbox_name_prefix.Concat(mailbox);
            Storage.Put(Storage.CurrentContext, key, name);
            return true;
        }

        // eg: register(AFqqgTdf8fa3zw193EzC3LSAD1ocvc5fho, hello@phantasma.io) => true
        private static bool RegisterMailbox(byte[] caller_address, byte[] mailbox)
        {
            // can only create mailbox for own address
            //if (!Runtime.CheckWitness(caller_address)) return false;

            // minimum name length
            if (mailbox.Length < 4) return false;

            // cannot hijack crypt mailboxes
            if (mailbox[0] == mailbox_crypt_prefix[0] && mailbox[1] == mailbox_crypt_prefix[1] && mailbox[2] == mailbox_crypt_prefix[2] && mailbox[3] == mailbox_crypt_prefix[3])
                return false;

            // verify if name already in use
            var current_owner = GetAddressFromMailbox(mailbox);
            if (current_owner != null) return false;

            // verify if address already has an associated mailbox, only one allowed per address!
            var other_box = GetMailboxFromAddress(caller_address);
            if (other_box != null) return false;

            InitMailbox(caller_address, mailbox);

            return true;
        }

        private static void InitMailbox(byte[] caller_address, byte[] mailbox)
        {
            // save owner of name 
            var owner_key = mailbox_address_prefix.Concat(mailbox);
            Storage.Put(Storage.CurrentContext, owner_key, caller_address);

            // save reverse mapping address => name
            var address_key = address_mailbox_prefix.Concat(caller_address);
            Storage.Put(Storage.CurrentContext, address_key, mailbox);
        }

        // eg: send(AFqqgTdf8fa3zw193EzC3LSAD1ocvc5fho, AFqqgTdf8fa3zw193EzC3LSAD1ocvc5fho, IPFS_hash("hello world")) => true
        private static bool SendMessage(byte[] from, byte[] to, byte[] hash)
        {
            //if (!Runtime.CheckWitness(from)) return false;

            return SendMessageVerified(to, hash);
        }

        // same as before, from is assumed to be transaction owner
        private static bool SendMessageVerified(byte[] to, byte[] hash)
        {
            var key = mailbox_address_prefix.Concat(to);
            var value = Storage.Get(Storage.CurrentContext, key);

            // if name does not exist, then fails
            if (value == null) return false;

            // get mailbox current size
            key = mailbox_size_prefix.Concat(to);
            value = Storage.Get(Storage.CurrentContext, key);

            // increase size and save
            var mailcount = value.AsBigInteger() + 1;
            value = mailcount.AsByteArray();
            Storage.Put(Storage.CurrentContext, key, value);

            key = mailbox_content_prefix.Concat(to);
            value = mailcount.AsByteArray();
            key = key.Concat(value);

            // save mail content / hash
            Storage.Put(Storage.CurrentContext, key, hash);

            return true;
        }

        // returns how many mails in a specified box
        // eg: GetMailCount("hello@phantasma.io") => 4
        private static BigInteger GetMailCount(byte[] mailbox)
        {
            // get mailbox current size
            var key = mailbox_size_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);

            return value.AsBigInteger();
        }

        // removes all mails from the mailbox
        private static void ClearMailbox(byte[] mailbox)
        {
            var key = mailbox_size_prefix.Concat(mailbox);
            Storage.Put(Storage.CurrentContext, key, 0);
        }

        // returns IPFS hash for specicied mail in a specified box
        // eg: GetMailContent("hello@phantasma.io", 0) => IFPS hash of first mail in this mailbox
        private static byte[] GetMailContent(byte[] mailbox, BigInteger index)
        {
            if (index <= 0)
            {
                return null;
            }

            // get mailbox current size
            var size = GetMailCount(mailbox);
            if (index > size)
            {
                return null;
            }

            var key = mailbox_content_prefix.Concat(mailbox);
            var value = index.AsByteArray();
            key = key.Concat(value);

            // save mail content / hash
            value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        // transfer a maibox from one NEO address to another
        public static bool TransferMailbox(byte[] from, byte[] to, byte[] mailbox)
        {
            //if (!Runtime.CheckWitness(from)) return false;

            var dest_maillbox = GetMailboxFromAddress(to);
            if (dest_maillbox != null) return false;

            // save new owner of name 
            var owner_key = mailbox_address_prefix.Concat(mailbox);
            Storage.Put(Storage.CurrentContext, owner_key, to);

            // save reverse mapping address => name
            var address_key = address_mailbox_prefix.Concat(to);
            Storage.Put(Storage.CurrentContext, address_key, mailbox);

            // delete previous mapping address => name
            var old_address_key = address_mailbox_prefix.Concat(from);
            Storage.Delete(Storage.CurrentContext, old_address_key);

            return true;
        }
        #endregion

        #region CRYPT API
        public static readonly byte[] fee_pool_address = { 47, 60, 170, 33, 216, 40, 148, 2, 242, 150, 9, 84, 154, 50, 237, 160, 97, 90, 55, 183 }; // FIXME : change later        
        public static readonly byte[] prize_pool_address = { 47, 60, 170, 33, 216, 40, 148, 2, 242, 150, 9, 84, 154, 50, 237, 160, 97, 90, 55, 183 }; // FIXME : change later        

        private static readonly byte[] crypt_owner_prefix = { (byte)'C', (byte)'R', (byte)'Y', (byte)'O' };
        private static readonly byte[] crypt_status_prefix = { (byte)'C', (byte)'R', (byte)'Y', (byte)'S' };
        private static readonly byte[] crypt_time_prefix = { (byte)'C', (byte)'R', (byte)'Y', (byte)'T' }; 
        private static readonly byte[] crypt_price_prefix = { (byte)'C', (byte)'R', (byte)'Y', (byte)'P' }; // note - both auction and subscription price!!!

        private static readonly byte[] mailbox_crypt_prefix = { (byte)'_', (byte)'C', (byte)'R', (byte)'Y'};

        private static readonly uint STATUS_INVALID = 0;
        private static readonly uint STATUS_UNCLAIMED = 1;
        private static readonly uint STATUS_OPERATING = 2;
        private static readonly uint STATUS_SALE = 3;
        private static readonly uint STATUS_ABANDONED = 4;

        private static readonly uint CRYPT_MAX_IDLE_TIME = 60 * 86400; // 60 days
        private static readonly uint CRYPT_MINIMUM_PRICE = 4;  // in SOUL
        private static readonly uint CRYPT_DEFAULT_CLAIM_PRICE = 80;  // in SOUL
        private static readonly uint CRYPT_DEFAULT_SUBSCRIPTION_PRICE = 4;  // in SOUL

        // this claim a crypt, if requisites met
        // - crypt is unclaimed, abandoned or for sale
        public static bool ClaimCrypt(byte[] caller_address, BigInteger index)
        {
            var status = GetCryptStatus(index);

            // check if crypt is up for grabs
            if (status == STATUS_INVALID || status == STATUS_OPERATING)
            {
                return false;
            }

            var index_bytes = index.AsByteArray();

            // get current price of crypt
            var current_price = GetCryptPrice(index);

            var souls = BalanceOf(caller_address);
            if (souls < current_price)
            {
                return false; // not enough SOULS for claim
            }

            BigInteger pool_ammount;
            BigInteger owner_ammount = 0;

            var crypt_owner = GetCryptOwner(index);

            if (crypt_owner != null)
            {
                pool_ammount = current_price / 5; // calculate 20%
                owner_ammount = current_price - pool_ammount;
            }
            else
            {
                pool_ammount = current_price;
            }

            if (owner_ammount > 0)
            {
                Transfer(caller_address, fee_pool_address, owner_ammount);
            }

            Transfer(caller_address, fee_pool_address, pool_ammount);

            // save current owner
            var owner_key = crypt_owner_prefix.Concat(index_bytes);
            Storage.Put(Storage.CurrentContext, owner_key, caller_address);

            // change status
            status = STATUS_OPERATING;
            var status_key = crypt_status_prefix.Concat(index_bytes);
            Storage.Put(Storage.CurrentContext, status_key, status);

            ChangeCryptPrice(index, CRYPT_DEFAULT_SUBSCRIPTION_PRICE);

            // update time
            var time = GetCurrentTime();
            var time_key = crypt_time_prefix.Concat(index_bytes);
            Storage.Put(Storage.CurrentContext, time_key, time);

            return true;
        }

        public static BigInteger GetCryptPrice(BigInteger index)
        {
            var index_bytes = index.AsByteArray();
            var price_key = crypt_price_prefix.Concat(index_bytes);
            var value = Storage.Get(Storage.CurrentContext, price_key);

            BigInteger current_price = value.AsBigInteger();
            if (current_price < 4)
            {
                current_price = 4;
            }

            return current_price;
        }

        // gets the current status for a crypt
        // also unlocks idle crypts (according to the whitepaper)
        public static BigInteger GetCryptStatus(BigInteger index)
        {
            var index_bytes = index.AsByteArray();
            var status_key = crypt_status_prefix.Concat(index_bytes);
            var temp = Storage.Get(Storage.CurrentContext, status_key);

            if (temp == null)
                return STATUS_INVALID;

            var status = temp.AsBigInteger();

            // timeout of crypts => set status to abandoned if not used for a long time
            if (status == STATUS_OPERATING)
            {
                var now = GetCurrentTime();
                var last_time = GetCryptTime(index);

                var diff = now - last_time;
                if (diff > CRYPT_MAX_IDLE_TIME)
                {
                    status = STATUS_ABANDONED;
                    Storage.Put(Storage.CurrentContext, status_key, status);
                }
            }

            return status;
        }

        private static void ChangeCryptPrice(BigInteger index, BigInteger price)
        {
            if (price < CRYPT_MINIMUM_PRICE)
            {
                price = CRYPT_MINIMUM_PRICE;
            }

            var index_bytes = index.AsByteArray();
            var price_key = crypt_price_prefix.Concat(index_bytes);
            Storage.Put(Storage.CurrentContext, price_key, price);
        }

        public static bool AuctionCrypt(byte[] caller_address, BigInteger index, BigInteger price)
        {
            var owner_address = GetCryptOwner(index);
            if (owner_address == null) return false; // no owner

            // if (owner_address != caller_address) return false; // FIXME : is this necessary?

            // only crypt owner can auction crypts
            //if (!Runtime.CheckWitness(owner_address)) return false;

            var status = GetCryptStatus(index);
            var canAuction = (status == STATUS_OPERATING || status == STATUS_ABANDONED);
            if (!canAuction) return false;

            var index_bytes = index.AsByteArray();
            var status_key = crypt_status_prefix.Concat(index_bytes);

            status = STATUS_SALE;
            Storage.Put(Storage.CurrentContext, status_key, status);

            ChangeCryptPrice(index, price);

            return true;
        }

        // 20% of each subscrption will go toward the pool (according to the whitepaper)
        public static bool ChangeCryptSubscriptionFee(byte[] caller_address, BigInteger index, BigInteger price)
        {
            var owner_address = GetCryptOwner(index);
            if (owner_address == null) return false; // no owner

            // if (owner_address != caller_address) return false; // FIXME : is this necessary?

            // only crypt owner can auction crypts
            if (!Runtime.CheckWitness(owner_address)) return false;

            var status = GetCryptStatus(index);
            if (status != STATUS_OPERATING) return false;

            ChangeCryptPrice(index, price);

            return true;
        }

        // gets the current owner of crypt
        public static byte[] GetCryptOwner(BigInteger index)
        {
            var index_bytes = index.AsByteArray();
            var owner_key = crypt_owner_prefix.Concat(index_bytes);
            var current_owner = Storage.Get(Storage.CurrentContext, owner_key);
            return current_owner;
        }

        // gets the current time of a crypt
        // the crypt time is updated whenever the status changes or new content is released
        public static BigInteger GetCryptTime(BigInteger index)
        {
            var index_bytes = index.AsByteArray();
            var time_key = crypt_owner_prefix.Concat(index_bytes);
            var temp = Storage.Get(Storage.CurrentContext, time_key);
            var time = temp.AsBigInteger();
            return time;
        }

        private static byte[] GetMailboxForCrypt(BigInteger index)
        {
            var index_bytes = index.AsByteArray();
            var mailbox = mailbox_crypt_prefix.Concat(index_bytes);
            return mailbox;
        }

        // a new crypt is generated with:
        // status = STATUS_UNCLAIMED
        // owner = null
        // time = null
        // price = custom
        // a mailbox for this crypt is also generated using mailbox_crypt_prefix  + index
        public static bool GenerateCrypt(byte[] caller_address, BigInteger index)
        {
            // only contract owner can generate crypts
            if (!Runtime.CheckWitness(caller_address)) return false;

            // check if crypt already exists
            var status = GetCryptStatus(index);
            if (status != STATUS_INVALID) return false;

            status = STATUS_UNCLAIMED;

            var index_bytes = index.AsByteArray();
            var status_key = crypt_status_prefix.Concat(index_bytes);
            Storage.Put(Storage.CurrentContext, status_key, status);

            ChangeCryptPrice(index, CRYPT_DEFAULT_CLAIM_PRICE);

            var mailbox = GetMailboxForCrypt(index);
            InitMailbox(caller_address, mailbox);
            return true;
        }
        #endregion

        #region REWARD API
        public static bool ClaimPrize(byte[] caller_address, BigInteger crypt, BigInteger index, byte[] answer)
        {
            // check caller identity
            if (!Runtime.CheckWitness(caller_address)) return false;

            byte[] claim_hash = null; // GetPrizeHashByIndex(index);
            if (claim_hash == null) return false;

            var user_hash = Sha256(answer);
            
            if (user_hash != claim_hash) return false;

            var prize_ammount = 0; // GetPrizeAmmountByIndex(index);

            Transfer(prize_pool_address, caller_address, prize_ammount);

            //SetWinner(crypt, index, mailbox);

            return true;
        }
        #endregion

        #region SUBSCRIPTION API
        // returns how many subscriptions for a specified box
        // eg: GetMailCount("hello@phantasma.io") => 4
        private static BigInteger GetSubscriptionCount(byte[] mailbox)
        {
            // get mailbox current size
            var key = subscription_size_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);

            return value.AsBigInteger();
        }

        // removes all mails from the mailbox
        private static bool RemoveSubscription(byte[] mailbox, BigInteger index)
        {
            if (index < 0) return false;

            var caller_address = GetAddressFromMailbox(mailbox);
            if (caller_address == null) return false;

            // only contract owner can generate crypts
            if (!Runtime.CheckWitness(caller_address)) return false;

            var count = GetSubscriptionCount(mailbox);
            if (index >= count) return false;

            var value = index.AsByteArray();

            if (count>0) // copy the last value in list to the "index" slot
            {
                var last = count - 1;
                var temp = GetSubscriptionByIndex(mailbox, last);

                var new_key = subscription_content_prefix.Concat(mailbox);
                new_key = new_key.Concat(value);

                Storage.Put(Storage.CurrentContext, new_key, 0);
            }

            var old_key = subscription_content_prefix.Concat(mailbox);
            old_key = old_key.Concat(value);
            Storage.Delete(Storage.CurrentContext, old_key);

            //count--;
            var count_key = subscription_size_prefix.Concat(mailbox);
            Storage.Put(Storage.CurrentContext, count_key, count);

            return true;
        }

        // returns index of crypts subscruption in a specified box
        private static byte[] GetSubscriptionByIndex(byte[] mailbox, BigInteger index)
        {
            if (index <= 0)
            {
                return null;
            }

            var count = GetSubscriptionCount(mailbox);

            if (index >= count)
            {
                return null;
            }

            var sub_key = subscription_content_prefix.Concat(mailbox);
            var value = index.AsByteArray();
            sub_key = sub_key.Concat(value);

            // get subscription (crypt index )
            value = Storage.Get(Storage.CurrentContext, sub_key);
            return value;
        }

        private static bool SubscribeTo(byte[] mailbox, BigInteger index)
        {
            var caller_address = GetAddressFromMailbox(mailbox);
            if (caller_address == null) return false;

            // only mailbox owner can subscript to crypts
            if (!Runtime.CheckWitness(caller_address)) return false;

            var status = GetCryptStatus(index);
            if (status != STATUS_OPERATING)
            {
                return false;
            }

            var price = GetCryptPrice(index);
            var balance = BalanceOf(caller_address);

            // check if have enough SOUL to subscribe
            if (balance < price) return false;

            return true;
        }
        #endregion

    }
}
