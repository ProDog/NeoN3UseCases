using Neo;
using Neo.IO;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Linq;
using System.Text;
using Neo.Network.RPC;
using System.Security.Cryptography;
using Neo3Cases.RpcClientTest;

namespace Neo3Cases
{
    class Program
    {
        static void Main()
        {
            RpcClient rpcClient = new RpcClient(new Uri("http://localhost:20332"), null, null, ProtocolSettings.Load("config.json", true));

            Console.WriteLine("Open wallet: " + rpcClient.OpenWalletAsync(@"D:\Work\neo_code\neo-node\neo-cli\bin\Debug\net5.0\1.json", "1").Result);

            ContractAPIDemo contractAPIDemo = new ContractAPIDemo(rpcClient);
            contractAPIDemo.Run();

            PolicyAPIDemo policyAPIDemo = new PolicyAPIDemo(rpcClient);
            policyAPIDemo.Run();

            RpcClientDemo rpcClientDemo = new RpcClientDemo(rpcClient);
            rpcClientDemo.Run();

            Console.ReadKey();
        }        

        public static void Signtest()
        {
            Console.Write("wif:");
            string wif = Console.ReadLine();

            byte[] prikey = Wallet.GetPrivateKeyFromWIF(wif);
            KeyPair keyPair = new KeyPair(prikey);
            string message = "Thesekindofcontrolsaregoodweverecommendedbeforebuttheyarespecificto";

            byte[] byteMessage = Encoding.UTF8.GetBytes(message);
            var signData = Neo.Cryptography.Crypto.Sign(byteMessage, keyPair.PrivateKey, keyPair.PublicKey.EncodePoint(false)[1..]);

            Console.WriteLine("message:" + Convert.ToBase64String(byteMessage));
            Console.WriteLine("public key:" + Convert.ToBase64String(keyPair.PublicKey.ToArray()));
            Console.WriteLine("sign data:"+ Convert.ToBase64String(signData));
        }

        private static void CreateAccount()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);

            var publicKey = key.PublicKey.ToString();
            var wif = key.Export();
            var contract = Contract.CreateSignatureContract(key.PublicKey);
            var address = contract.ScriptHash.ToAddress(ProtocolSettings.Default.AddressVersion);
            var scriptHash = contract.ScriptHash;

        }

    }
}
