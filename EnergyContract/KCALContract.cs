using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Phantasma.Contracts
{
    public class EnergyContract : SmartContract
    {
        public static readonly byte[] Developers_Address = "AGUNSWYyZDVQpzL6YbuSYc5qqbG7fDcMuZ".ToScriptHash();

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                // param Owner must be script hash
                bool isOwner = Runtime.CheckWitness(Developers_Address);

                return isOwner;
            }
            else
            if (Runtime.Trigger == TriggerType.Application)
            {
                #region NEP5 METHODS
                if (operation == "totalSupply") return TotalSupply();
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

                else if (operation == "deploy")
                {
                    if (args.Length != 1) return false;
                    byte[] to = (byte[])args[0];
                    return Deploy(to);
                }
                #endregion

                #region NEP10
                else if (operation == "supportedStandards")
                {
                    return new string[] {"NEP-5" , "NEP-10"};
                }

                #endregion
            }

            return false;
        }


        #region UTILITY METHODS

        public static bool ValidateAddress(byte[] address)
        {
            if (address.Length != 20)
                return false;
            if (address.AsBigInteger() == 0)
                return false;
            return true;
        }

        #endregion


        #region NEP5
        //Token Settings
        public static string Name() => "Phantasma Energy";
        public static string Symbol() => "KCAL";
        public static byte Decimals() => 10;
        public static BigInteger TotalSupply() => 1000000000 * _decimals_helper;

        public const ulong _decimals_helper = 10000000000; //decided by Decimals()

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

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
            Transferred(from, to, value);
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
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(sender)) return false;
            if (!ValidateAddress(from)) return false;
            if (!ValidateAddress(to)) return false;

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

            Transferred(from, to, value);
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
            //OnApproved(from, to, value);
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

        // initialization parameters, only once
        public static bool Deploy(byte[] to)
        {
            // Only Team/Admmin/Owner can deploy
            if (!Runtime.CheckWitness(Developers_Address))
                return false;

            if (!ValidateAddress(to)) return false;

            var current_total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            if (current_total_supply != 0)
            {
                return false;
            }

            var supply = TotalSupply();

            Storage.Put(Storage.CurrentContext, "totalSupply", supply);

            Transferred(null, to, supply);
            Storage.Put(Storage.CurrentContext, to, supply);

            Runtime.Notify("deploy", supply);

            return true;
        }
        #endregion
    }
}
