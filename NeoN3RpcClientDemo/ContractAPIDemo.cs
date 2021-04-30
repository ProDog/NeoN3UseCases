using Neo;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo3Cases
{
    public class ContractAPIDemo
    {
        ContractClient _contractClient;
        RpcClient _rpcClient;

        private static KeyPair keyPair0 = Neo.Network.RPC.Utility.GetKeyPair("KxuVa318GrRFhLvSvWfz4dZTP7JibKMTPkHFMPrBv2PdA2NvzEJZ");
        private static KeyPair keyPair1 = Neo.Network.RPC.Utility.GetKeyPair("L3A2sG2mr1afjXEcFuwiMnLnN9hXGLRD981t7ShMFZ3HVfgyPAEU");
        private static KeyPair keyPair2 = Neo.Network.RPC.Utility.GetKeyPair("KzYoUe85ixi82jpoTakpRv8pNCSgBtZ8tQAxZk9ik8BoJaqZPN19");
        private static UInt160 gasHash = Neo.SmartContract.Native.GasToken.GAS.Hash;
        private static UInt160 neoHash = Neo.SmartContract.Native.NeoToken.NEO.Hash;
        private static UInt160 nep17Hash = new();
        private static UInt160 multiAccount = new();

        public ContractAPIDemo(RpcClient rpcClient)
        {
            _contractClient = new ContractClient(rpcClient);

            _rpcClient = rpcClient;
        }

        public void Run()
        {
            multiAccount = Contract.CreateMultiSigContract(2, new ECPoint[] { keyPair0.PublicKey, keyPair1.PublicKey, keyPair2.PublicKey }).ScriptHash;

            DeployContract();

            InvokeContractTx0();

            InvokeContractTx1();

            InvokeContractTx2();

            InvokeScript();

            InvokeFunction1();

            InvokeFunction2();

            TransferNep17();

            TestNep17API();
        }

        private void DeployContract()
        {
            Console.WriteLine("deploy contract.");
            string path = @"D:\Work\TestCode\NeoN3Contract\Nep17Contract\bin\sc";
            string nefFilePath = path + "\\MyTestContract.nef";
            string manifestFilePath = path + "\\MyTestContract.manifest.json";

            NefFile nefFile;
            using (var stream = new BinaryReader(File.OpenRead(nefFilePath), Encoding.UTF8, false))
            {
                nefFile = stream.ReadSerializable<NefFile>();
            }

            var mani = File.ReadAllBytes(manifestFilePath);
            ContractManifest manifest = ContractManifest.Parse(mani);

            var tx = _contractClient.CreateDeployContractTxAsync(nefFile.ToArray(), manifest, keyPair0).Result;
            _rpcClient.SendRawTransactionAsync(tx);

            var contractHash = Neo.SmartContract.Helper.GetContractHash(tx.Sender, nefFile.CheckSum, manifest.Name);
            Console.WriteLine("contract hash:" + Neo.SmartContract.Helper.GetContractHash(tx.Sender, nefFile.CheckSum, manifest.Name));
            nep17Hash = contractHash;

            Console.WriteLine($"Transaction {tx.Hash} is broadcasted!");
        }

        public void InvokeContractTx0()
        {
            Console.WriteLine("send GAS.");            

            UInt160 sender = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash;
            byte[] script = gasHash.MakeScript("transfer", sender, Contract.CreateSignatureContract(keyPair1.PublicKey).ScriptHash, 10_12345678, "adada");

            Signer[] signers = new[] { new Signer { Scopes = WitnessScope.Global, Account = sender } };

            SendInvokeTx(script, signers, keyPair0);

            Console.WriteLine("send NEO.");            
            byte[] script1 = neoHash.MakeScript("transfer", sender, multiAccount, 10, "adada");

            SendInvokeTx(script1, signers, keyPair0);
        }

        public void InvokeContractTx1()
        {
            Console.WriteLine("send gas with sender.");

            UInt160 sender = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash;            
            UInt160 account = Contract.CreateSignatureContract(keyPair1.PublicKey).ScriptHash;

            byte[] script = gasHash.MakeScript("transfer", account, multiAccount, 5_12345678, "adada");

            Signer[] signers = new[] 
            { 
                new Signer { Scopes = WitnessScope.Global, Account = sender }, 
                new Signer { Scopes = WitnessScope.Global, Account = account } 
            };

            SendInvokeTx(script, signers, keyPair0, keyPair1);
        }

        public void InvokeContractTx2()
        {
            Console.WriteLine("send gas with ContractParameter.");

            UInt160 sender = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash;

            byte[] script = gasHash.MakeScript(
                "transfer",
                sender,
                new ContractParameter() 
                {                            
                    Type = ContractParameterType.Hash160,
                    Value = Contract.CreateSignatureContract(keyPair1.PublicKey).ScriptHash
                },                        
                100000,
                "data");

            Signer[] signers = new[] { new Signer { Scopes = WitnessScope.Global, Account = sender } };

            SendInvokeTx(script, signers, keyPair0);
        }

        public void InvokeScript()
        {
            Console.WriteLine("InvokeScript.");

            RpcInvokeResult invokeResult = _contractClient.TestInvokeAsync(
                gasHash,
                "balanceOf",
                new ContractParameter()
                {
                    Type = ContractParameterType.Hash160,
                    Value = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash
                }).Result;

            Console.WriteLine($"Invoke result: {invokeResult.ToJson()}");
        }

        private async Task InvokeFunction1()
        {
            Console.WriteLine("InvokeFunction balanceOf.");

            RpcStack param = new RpcStack()
            {
                Type = "Hash160",
                Value = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash.ToString()
            };

            RpcInvokeResult rpcInvokeResult = await _rpcClient.InvokeFunctionAsync(gasHash.ToString(), "balanceOf", new RpcStack[] { param }).ConfigureAwait(false);
            string script = rpcInvokeResult.Script;
            var engineState = rpcInvokeResult.State;
            long gasConsumed = rpcInvokeResult.GasConsumed;

            var balance = rpcInvokeResult.Stack[0].GetInteger();

            Console.WriteLine("balance:" + balance);
        }

        private async Task InvokeFunction2()
        {
            Console.WriteLine("InvokeFunction transfer.");           
            
            RpcStack from = new RpcStack()
            {
                Type = "Hash160",
                Value = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash.ToString()
            };
            RpcStack to = new RpcStack()
            {
                Type = "Hash160",
                Value = Contract.CreateSignatureContract(keyPair1.PublicKey).ScriptHash.ToString()
            };
            RpcStack amount = new RpcStack()
            {
                Type = "Integer",
                Value = "120000000"
            };
            RpcStack data = new RpcStack()
            {
                Type = "String",
                Value = "my data"
            };

            Signer signer0 = new Signer()
            {
                Account = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash
            };

            RpcInvokeResult rpcInvokeResult1 = await _rpcClient.InvokeFunctionAsync(gasHash.ToString(), "transfer", new RpcStack[] { from, to, amount, data }, signer0).ConfigureAwait(false);

            Console.WriteLine("1: " + rpcInvokeResult1.ToJson().ToString());
            var txHex1 = Convert.FromBase64String(rpcInvokeResult1.Tx);
            _ = _rpcClient.SendRawTransactionAsync(txHex1);
            Console.WriteLine($"Transaction {txHex1.AsSerializable<Transaction>().Hash} is broadcasted!");

            RpcInvokeResult rpcInvokeResult2 = await _rpcClient.InvokeScriptAsync(gasHash.MakeScript("transfer", Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash, Contract.CreateSignatureContract(keyPair1.PublicKey).ScriptHash, 1, "data"), signer0).ConfigureAwait(false);

            Console.WriteLine("2: " + rpcInvokeResult2.ToJson().ToString());
            var txHex2 = Convert.FromBase64String(rpcInvokeResult2.Tx);
            _ = _rpcClient.SendRawTransactionAsync(txHex2);
            Console.WriteLine($"Transaction {txHex2.AsSerializable<Transaction>().Hash} is broadcasted!");

        }

        private void SendInvokeTx(byte[] script, Signer[] signers, params KeyPair[] keyPair)
        {
            TransactionManagerFactory factory = new TransactionManagerFactory(_rpcClient);
            TransactionManager manager = factory.MakeTransactionAsync(script, signers).Result;

            foreach (var kp in keyPair)
            {
                manager.AddSignature(kp);
            }
            Transaction invokeTx = manager
                .SignAsync().Result;

            _rpcClient.SendRawTransactionAsync(invokeTx);

            Console.WriteLine($"Transaction {invokeTx.Hash} is broadcasted!");
        }

        public void TransferNep17()
        {
            Console.WriteLine("transfer nep17.");

            UInt160 sender = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash;
            byte[] script = nep17Hash.MakeScript("transfer", sender, multiAccount, 20_12345678, "adada");

            Signer[] signers = new[] { new Signer { Scopes = WitnessScope.Global, Account = sender } };

            SendInvokeTx(script, signers, keyPair0);
        }

        private void TestNep17API()
        {
            Nep17API nep17API = new Nep17API(_rpcClient);
            UInt160 account = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash;

            Console.WriteLine("balance: " + nep17API.BalanceOfAsync(nep17Hash, account).Result);
            Console.WriteLine("symbol: " + nep17API.SymbolAsync(nep17Hash).Result);
            Console.WriteLine("decimals: " + nep17API.DecimalsAsync(nep17Hash).Result);
            Console.WriteLine("totalSupply: " + nep17API.TotalSupplyAsync(nep17Hash).Result);
            Console.WriteLine("tokenInfo: " + nep17API.GetTokenInfoAsync(nep17Hash).Result);

            var tx = nep17API.CreateTransferTxAsync(nep17Hash, keyPair0, Contract.CreateSignatureContract(keyPair1.PublicKey).ScriptHash, 12345678, 123).Result;
            _ = _rpcClient.SendRawTransactionAsync(tx.ToArray());
            Console.WriteLine($"Transaction {tx.Hash} is broadcasted!");

            var tx1 = nep17API.CreateTransferTxAsync(nep17Hash, 2, new ECPoint[] { keyPair0.PublicKey, keyPair1.PublicKey, keyPair2.PublicKey }, new KeyPair[] { keyPair0, keyPair1 }, account, 12345678, 123).Result;
            _ = _rpcClient.SendRawTransactionAsync(tx1.ToArray());
            Console.WriteLine($"Multi Transaction {tx1.Hash} is broadcasted!");

            var tx2 = nep17API.CreateTransferTxAsync(gasHash, 2, new ECPoint[] { keyPair0.PublicKey, keyPair1.PublicKey, keyPair2.PublicKey }, new KeyPair[] { keyPair0, keyPair1 }, account, 12345678, 123).Result;
            _ = _rpcClient.SendRawTransactionAsync(tx2.ToArray());
            Console.WriteLine($"Multi Transaction GAS {tx2.Hash} is broadcasted!");

            var tx3 = nep17API.CreateTransferTxAsync(neoHash, 2, new ECPoint[] { keyPair0.PublicKey, keyPair1.PublicKey, keyPair2.PublicKey }, new KeyPair[] { keyPair0, keyPair1 }, account, 1, 123).Result;
            _ = _rpcClient.SendRawTransactionAsync(tx3.ToArray());
            Console.WriteLine($"Multi Transaction NEO {tx3.Hash} is broadcasted!");
        }
    }
}
