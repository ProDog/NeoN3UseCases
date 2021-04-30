using Neo.Network.RPC;
using System;

namespace Neo3Cases
{
    internal class PolicyAPIDemo
    {
        private RpcClient rpcClient;
        private PolicyAPI policyAPI;

        public PolicyAPIDemo(RpcClient rpcClient)
        {
            this.rpcClient = rpcClient;
            this.policyAPI = new PolicyAPI(rpcClient);
        }

        internal void Run()
        {
            Console.WriteLine("Get Fee Factor: " + policyAPI.GetExecFeeFactorAsync().Result);
            Console.WriteLine("Get Storage Price: " + policyAPI.GetStoragePriceAsync().Result);
            Console.WriteLine("Get Network Fee Per Byte: " + policyAPI.GetFeePerByteAsync().Result);
            Console.WriteLine("Get Ploicy Blocked Accounts: " + policyAPI.IsBlockedAsync(Neo.SmartContract.Native.GasToken.GAS.Hash).Result);
        }
    }
}