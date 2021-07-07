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
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Neo3Cases.RpcClientTest
{
    public class ContractAPIDemo
    {
        ContractClient _contractClient;
        RpcClient _rpcClient;
        private static KeyPair keyPair0 = Neo.Network.RPC.Utility.GetKeyPair("KwzT1VQqzyzKjdwGLbf5nJAKfRhHiap6SqPEkDkyqJN2WX5wcsuK");
        private static KeyPair keyPair1 = Neo.Network.RPC.Utility.GetKeyPair("Kzwwk3LVmET6PjHafDMmWg8TbvLLpcQbjZi2okpFLKkoaG2LuyJ1");
        private static KeyPair keyPair2 = Neo.Network.RPC.Utility.GetKeyPair("L3PR4aAwCfxEqghd39tjz6FU116G315L6NmkMtmNqhTYhkQmWrqg");
        private static UInt160 gasHash = Neo.SmartContract.Native.NativeContract.GAS.Hash;
        private static UInt160 neoHash = Neo.SmartContract.Native.NativeContract.NEO.Hash;
        private static UInt160 nep17Hash = new();
        private static UInt160 multiAccount = new();

        public ContractAPIDemo(RpcClient rpcClient)
        {
            _contractClient = new ContractClient(rpcClient);

            _rpcClient = rpcClient;
        }

        public void Run()
        {
            //multiAccount = Contract.CreateMultiSigContract(2, new ECPoint[] { keyPair0.PublicKey, keyPair1.PublicKey, keyPair2.PublicKey }).ScriptHash;

            //DeployContract();

            //InvokeContract();

            //InvokeContractTx0();

            //InvokeContractTx1();

            //InvokeContractTx2();

            //InvokeScript();

            //InvokeFunction1();

            //InvokeFunction2();

            //TransferNep17();

            //TestNep17API();

            TestInvokeTx();
        }


        //合约 Verify 方法带参数的交易构造示例
        private void TestInvokeTx()
        {
            var network = ProtocolSettings.Load("config.json", true).Network;

            UInt160 contractHash = UInt160.Parse("0xa9d1bc6952ec62516a84460dcca194efa2065a3d");
            Contract userContract = Contract.CreateSignatureContract(keyPair0.PublicKey);

            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitDynamicCall(gasHash, "transfer", contractHash, UInt160.Parse("0xc70f936eb813309acda06756781809052ff585b9"), (BigInteger)12345, 12);
                script = sb.ToArray();
            }

            UInt160 sender = userContract.ScriptHash;
            Signer[] signers = new[] { new Signer { Scopes = WitnessScope.Global, Account = contractHash }, new Signer { Scopes = WitnessScope.Global, Account = sender } };

            uint blockCount = _rpcClient.GetBlockCountAsync().Result - 1;
            RpcInvokeResult invokeResult = _rpcClient.InvokeScriptAsync(script, signers).Result;
            var tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)new Random().Next(),
                Script = script,
                Signers = signers ?? Array.Empty<Signer>(),
                ValidUntilBlock = blockCount - 1 + 100,
                SystemFee = invokeResult.GasConsumed,
                Attributes = Array.Empty<TransactionAttribute>(),
                Witnesses = Array.Empty<Witness>()
            };            

            var wScript = new byte[0];
            using (ScriptBuilder sb = new ScriptBuilder()) { sb.EmitPush(1); wScript = sb.ToArray(); }
            var contractWitness = new Witness { InvocationScript = wScript, VerificationScript = new byte[0] };

            tx.Witnesses = new Witness[] { contractWitness, new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = userContract.Script } };

            tx.NetworkFee = _rpcClient.CalculateNetworkFeeAsync(tx).Result;

            var context = new ContractParametersContext(null, tx, network);
            var signature = tx.Sign(keyPair0, network);
            context.AddSignature(Contract.CreateSignatureContract(keyPair0.PublicKey), keyPair0.PublicKey, signature);
            context.Add(Contract.Create(contractHash, ContractParameterType.Integer), 1);

            tx.Witnesses = context.GetWitnesses();

            var result = _rpcClient.RpcSendAsync("sendrawtransaction", Convert.ToBase64String(tx.ToArray())).Result;

            Console.WriteLine(result.AsString());
        }

        private void DeployContract()
        {
            Console.WriteLine("deploy contract.");

            //合约路径
            string path = @"D:\Work\TestCode\NeoN3Contract\Nep17Contract\bin\sc";
            string nefFilePath = path + "\\Nep17Contract.nef";
            string manifestFilePath = path + "\\Nep17Contract.manifest.json";

            //构造 contractClient
            RpcClient rpcClient = new RpcClient(new Uri("http://localhost:20332"), null, null, ProtocolSettings.Load("config.json", true));
            ContractClient contractClient = new ContractClient(rpcClient);

            //读取合约文件 nef 和 manifest
            NefFile nefFile;
            using (var stream = new BinaryReader(File.OpenRead(nefFilePath), Encoding.UTF8, false))
            {
                nefFile = stream.ReadSerializable<NefFile>();
            }
            var mani = File.ReadAllBytes(manifestFilePath);
            ContractManifest manifest = ContractManifest.Parse(mani);

            //构造交易
            var tx = contractClient.CreateDeployContractTxAsync(nefFile.ToArray(), manifest, keyPair0).Result;

            //发交易
            rpcClient.SendRawTransactionAsync(tx);

            //合约 hash
            var contractHash = Neo.SmartContract.Helper.GetContractHash(tx.Sender, nefFile.CheckSum, manifest.Name);
            Console.WriteLine("contract hash:" + contractHash);
            
            Console.WriteLine($"Transaction {tx.Hash} is broadcasted!");
        }

        public void InvokeContract()
        {
            Console.WriteLine("Invoke nep17 contract:");

            UInt160 sender = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash;
            byte[] script = nep17Hash.MakeScript("init", sender, 1000000_000000);

            Signer[] signers = new[] { new Signer { Scopes = WitnessScope.Global, Account = sender } };

            SendInvokeTx(script, signers, null, keyPair0);            
        }

        public void InvokeContractTx0()
        {
            Console.WriteLine("send GAS.");            

            UInt160 sender = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash;
            byte[] script = gasHash.MakeScript("transfer", sender, Contract.CreateSignatureContract(keyPair1.PublicKey).ScriptHash, 10_12345678, "adada");

            Signer[] signers = new[] { new Signer { Scopes = WitnessScope.Global, Account = sender } };

            SendInvokeTx(script, signers,null, keyPair0);

            Console.WriteLine("send NEO.");            
            byte[] script1 = neoHash.MakeScript("transfer", sender, multiAccount, 10, "adada");

            SendInvokeTx(script1, signers,null, keyPair0);
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

            SendInvokeTx(script, signers, null, keyPair0, keyPair1);
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

            Signer[] signers = new[]
            { 
                //new Signer 
                //{ Scopes = WitnessScope.Global, Account = sender }, 
                //new Signer()
                //{
                //    Account=UInt160.Parse("0x1c5917b2ccf717fafe059b11c7ee0e01e5bacc32"),
                ////Account = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash,
                ////AllowedContracts = new UInt160[] { UInt160.Parse("0xf3df263234eb1e913bd0c6e01465ef46e7ed0f98"), UInt160.Parse("0x782be0763d4da864c216d64737dc4aff88fd9b43"), UInt160.Parse("0x4638f10d3fdc2cce2ea14625dbc1672b2919d85c") },
                //Scopes = WitnessScope.Global
                //},
                new Signer()
                {
                Account = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash,
                AllowedGroups = new ECPoint[] { ECPoint.Parse("0248e7f50d17b4447d8b255677e8e1ab059d6169c012c7a4c60cc5d4bb5796595f", ECCurve.Secp256r1), ECPoint.Parse("02495c03730de9bbbd57edcd83a8dbfc8cd8aab30ef95ee0068f2fa4a3a6e4b7f6", ECCurve.Secp256r1), ECPoint.Parse("023aeccf6586927fbcb36eff3c142396e1315be7233f807952c1f7bc7f7e058aad", ECCurve.Secp256r1) },
                Scopes = WitnessScope.CustomGroups
                }
            };

            //TransactionAttribute[] transactionAttributes = new TransactionAttribute[] {  new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 0, Result = Array.Empty<byte>() }};
            TransactionAttribute[] transactionAttributes = new TransactionAttribute[] { new HighPriorityAttribute { } };

            SendInvokeTx(script, signers, transactionAttributes, keyPair0);
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
                Account = Contract.CreateSignatureContract(keyPair0.PublicKey).ScriptHash,
                AllowedContracts = new UInt160[] { UInt160.Parse("0xf3df263234eb1e913bd0c6e01465ef46e7ed0f98"), UInt160.Parse("0x782be0763d4da864c216d64737dc4aff88fd9b43"), UInt160.Parse("0x4638f10d3fdc2cce2ea14625dbc1672b2919d85c") },
                AllowedGroups = new ECPoint[] { ECPoint.Parse("0248e7f50d17b4447d8b255677e8e1ab059d6169c012c7a4c60cc5d4bb5796595f", ECCurve.Secp256k1), ECPoint.Parse("02495c03730de9bbbd57edcd83a8dbfc8cd8aab30ef95ee0068f2fa4a3a6e4b7f6", ECCurve.Secp256r1), ECPoint.Parse("023aeccf6586927fbcb36eff3c142396e1315be7233f807952c1f7bc7f7e058aad", ECCurve.Secp256r1) },
                Scopes = WitnessScope.CustomContracts
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

        private void SendInvokeTx(byte[] script, Signer[] signers, TransactionAttribute[] transactionAttributes = null, params KeyPair[] keyPair)
        {
            TransactionManagerFactory factory = new TransactionManagerFactory(_rpcClient);
            TransactionManager manager = factory.MakeTransactionAsync(script, signers, transactionAttributes).Result;

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

            SendInvokeTx(script, signers, null, keyPair0);
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

            var tx2 = nep17API.CreateTransferTxAsync(gasHash, 2, new ECPoint[] { keyPair0.PublicKey, keyPair1.PublicKey, keyPair2.PublicKey }, new KeyPair[] { keyPair0, keyPair1 }, account, 12345678, 123).Result;
            _ = _rpcClient.SendRawTransactionAsync(tx2.ToArray());
            Console.WriteLine($"Multi Transaction GAS {tx2.Hash} is broadcasted!");

            var tx3 = nep17API.CreateTransferTxAsync(neoHash, 2, new ECPoint[] { keyPair0.PublicKey, keyPair1.PublicKey, keyPair2.PublicKey }, new KeyPair[] { keyPair0, keyPair1 }, account, 1, 123).Result;
            _ = _rpcClient.SendRawTransactionAsync(tx3.ToArray());
            Console.WriteLine($"Multi Transaction NEO {tx3.Hash} is broadcasted!");
        }
    }
}
