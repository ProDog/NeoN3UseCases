using Neo;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;

namespace Neo3Cases.RpcClientTest
{
    internal class RpcClientDemo
    {
        private RpcClient rpcClient;

        public RpcClientDemo(RpcClient rpcClient)
        {
            this.rpcClient = rpcClient;
        }

        internal void Run()
        {
            var txid = "0x3e55711455b12e1765e43212197d0f9bce949a73b3e0a997b439144192e77385";
            var contractHash = "0xf3df263234eb1e913bd0c6e01465ef46e7ed0f98";
            var addr = "NUNtEBBbJkmPrmhiVSPN6JuM7AcE8FJ5sE";
            var blockHash = rpcClient.GetBestBlockHashAsync().Result;
            Console.WriteLine("GetBestBlockHash: " + blockHash);

            var blockHex = rpcClient.GetBlockHexAsync(blockHash).Result;
            Console.WriteLine("GetBlockHex: " + blockHex);
            Console.WriteLine("GetBlock: " + rpcClient.GetBlockAsync(blockHash).Result);
            Console.WriteLine("GetBlockHeaderCount: " + rpcClient.GetBlockHeaderCountAsync().Result);

            var blockCount = rpcClient.GetBlockCountAsync().Result;
            Console.WriteLine("GetBlockCount: " + blockCount);
            Console.WriteLine("GetBlockHash: " + rpcClient.GetBlockHashAsync((int)blockCount - 1).Result);
            Console.WriteLine("GetBlockHeaderHex: " + rpcClient.GetBlockHeaderHexAsync(blockHash).Result);

            Console.WriteLine("GetBlockHeader: " + rpcClient.GetBlockHeaderAsync(blockHash).Result.ToJson(ProtocolSettings.Default).ToString());

            Console.WriteLine("GetContractState: " + rpcClient.GetContractStateAsync(contractHash).Result.ToJson().ToString());
            Console.WriteLine("GetNativeContracts: " + rpcClient.GetNativeContractsAsync().Result[0].ToJson());

            Console.WriteLine("GetRawMempool: " + rpcClient.GetRawMempoolAsync().Result);
            Console.WriteLine("GetRawMempoolBoth: " + rpcClient.GetRawMempoolBothAsync().Result);

            var txHex = rpcClient.GetRawTransactionHexAsync(txid).Result;
            Console.WriteLine("GetRawTransactionHex: " + txHex);
            Console.WriteLine("GetRawTransaction: " + rpcClient.GetRawTransactionAsync(txid).Result.ToJson(ProtocolSettings.Default).ToString());

            var tx = Convert.FromBase64String(txHex).AsSerializable<Transaction>();
            Console.WriteLine("CalculateNetworkFee: " + rpcClient.CalculateNetworkFeeAsync(tx).Result);

            Console.WriteLine("GetStorage: " + rpcClient.GetStorageAsync(contractHash, Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("deploy"))).Result);
            Console.WriteLine("GetTransactionHeight: " + rpcClient.GetTransactionHeightAsync(txid).Result);

            Console.WriteLine("GetNextBlockValidators: " + rpcClient.GetNextBlockValidatorsAsync().Result.ToString());
            Console.WriteLine("GetCommittee: " + rpcClient.GetCommitteeAsync().Result.ToString());

            Console.WriteLine("GetConnectionCount: " + rpcClient.GetConnectionCountAsync().Result);
            Console.WriteLine("GetPeers: " + rpcClient.GetPeersAsync().Result.ToJson().ToString());
            Console.WriteLine("GetVersion: " + rpcClient.GetVersionAsync().Result);

            try
            {
                Console.WriteLine("SendRawTransaction: " + rpcClient.SendRawTransactionAsync(Convert.FromBase64String(txHex)).Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                Console.WriteLine("SendRawTransaction: " + rpcClient.SendRawTransactionAsync(tx).Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                Console.WriteLine("SubmitBlock: " + rpcClient.SubmitBlockAsync(Convert.FromBase64String(blockHex)).Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("GetUnclaimedGas: " + rpcClient.GetUnclaimedGasAsync(addr).Result.Unclaimed);
            Console.WriteLine("ValidateAddress: " + rpcClient.ValidateAddressAsync(addr).Result.IsValid);

            var key = rpcClient.DumpPrivKeyAsync(addr).Result;
            Console.WriteLine("DumpPrivKey: " + key);
            var newAddr = rpcClient.GetNewAddressAsync().Result;
            Console.WriteLine("GetNewAddress: " + newAddr);
            Console.WriteLine("GetWalletBalance: " + rpcClient.GetWalletBalanceAsync(contractHash).Result);
            Console.WriteLine("GetWalletUnclaimedGas: " + rpcClient.GetWalletUnclaimedGasAsync().Result);
            Console.WriteLine("ImportPrivKey: " + rpcClient.ImportPrivKeyAsync(key).Result.Address);
            Console.WriteLine("ListAddress: " + rpcClient.ListAddressAsync().Result.ToArray().ToString());

            Console.WriteLine("SendFrom: " + rpcClient.SendFromAsync(contractHash, addr, newAddr, "123456789").Result);
            Console.WriteLine("SendToAddress: " + rpcClient.SendToAddressAsync(contractHash, newAddr, "123456789").Result);

            RpcTransferOut[] outs = new RpcTransferOut[]
            {
                new RpcTransferOut
                {
                    Asset = UInt160.Parse(contractHash),
                    ScriptHash = newAddr.ToScriptHash(ProtocolSettings.Default.AddressVersion),
                    Value = "12345678"
                },
                new RpcTransferOut
                {
                    Asset = NativeContract.GAS.Hash,
                    ScriptHash = newAddr.ToScriptHash(ProtocolSettings.Default.AddressVersion),
                    Value = "12345678"
                },
                new RpcTransferOut
                {
                    Asset = NativeContract.NEO.Hash,
                    ScriptHash = newAddr.ToScriptHash(ProtocolSettings.Default.AddressVersion),
                    Value = "1"
                },
                new RpcTransferOut
                {
                    Asset = UInt160.Parse(contractHash),
                    ScriptHash = newAddr.ToScriptHash(ProtocolSettings.Default.AddressVersion),
                    Value = "22345687"
                },
            };
            Console.WriteLine("SendMany: " + rpcClient.SendManyAsync(addr, outs).Result);

            Console.WriteLine("GetApplicationLog: " + rpcClient.GetApplicationLogAsync(txid).Result.ToJson().ToString());
            Console.WriteLine("GetApplicationLog: " + rpcClient.GetApplicationLogAsync(txid, Neo.SmartContract.TriggerType.All).Result.ToJson().ToString());
            Console.WriteLine("GetNep17Transfers: " + rpcClient.GetNep17TransfersAsync(addr).Result.ToJson(ProtocolSettings.Default).ToString());
            Console.WriteLine("GetNep17Balances: " + rpcClient.GetNep17BalancesAsync(addr).Result.ToJson(ProtocolSettings.Default).ToString());
        }
    }
}