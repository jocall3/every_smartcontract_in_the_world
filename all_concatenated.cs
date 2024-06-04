using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.ABI.Decoders;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.XUnitEthereumClients;
using SolidityCallAnotherContract.Contracts.Test.CQS;
using SolidityCallAnotherContract.Contracts.TheOther.CQS;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ABIFixedArrayWithDynamicAndDynamicArraysTest
    {
        /*
pragma solidity ^0.4.24;
pragma experimental "ABIEncoderV2";

contract Test {

    function callManyContractsVariableReturn(address[] destination, bytes[] data) public view returns (bytes[] result){
       result = new bytes[](destination.length);
       for(uint i = 0; i < destination.length; i++){
           result[i] = callContract(destination[i], data[i]);
       }
       return result;
   }

   function callManyContractsSameQuery(address[] destination, bytes data) public view returns (bytes[] result){
       result = new bytes[](destination.length);
       for(uint i = 0; i < destination.length; i++) {
           result[i] = callContract(destination[i], data);
       }
       return result;
   }

   function callManyOtherContractsVariableArrayReturn(address theOther) public view returns (bytes[] result){
       result = new bytes[](3);
       result[0] = CallAnotherContract(theOther);
       result[1] = CallAnotherContract(theOther);
       result[2] = CallAnotherContract(theOther);
       return result;
   }

   function callManyOtherContractsFixedArrayReturn(address theOther) public view returns (bytes[10] result){
       result[0] = CallAnotherContract(theOther);
       result[1] = CallAnotherContract(theOther);
       result[2] = CallAnotherContract(theOther);
       return result;
   }

   function CallAnotherContract(address theOther) public view returns(bytes result) 
   {
       string memory name = "Solidity";
       string memory greeting = "Welcome something much much biggger jlkjfslkfjslkdfjsldfjasdflkjsafdlkjasdfljsadfljasdfkljasdkfljsadfljasdfldsfaj booh!";

       bytes memory callData = abi.encodeWithSignature("CallMe(string,string)", name, greeting);
       return callContract(theOther, callData);
   }

   //thanks to gonzalo and alex 
    function  callContract(address contractAddress, bytes memory data)  internal view returns(bytes memory answer) {

        uint256 length = data.length;
        uint256 size = 0;
        
        assembly {
            answer := mload(0x40)

            let result := staticcall(gas(),
                contractAddress, 
                add(data, 0x20), 
                length, 
                0, 
                0)
                
            //todo return some error if result is 0

            size := returndatasize
            returndatacopy(answer, 0, size)
            mstore(answer, size)
            mstore(0x40, add(answer,size))
        }

        return answer;
     
    }
}

contract TheOther
{
   function CallMe(string name, string greeting) public view returns(bytes test)
   {
       return abi.encodePacked("Hello ", name ," ", greeting);
   }
}
*/

        /// This test, decodes a bytes fixed array [10] and a bytes array.
        ///Bytes is a dynamic byte[] so we have fixed with dynamic and bytes[] is a byte[][]

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ABIFixedArrayWithDynamicAndDynamicArraysTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDoSimpleMultipleQueries()
        {

        }

        [Fact]
        public async void ShouldCallDifferentContractsUsingDataBytesArraysFixedAndVariable()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();

                var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TheOtherDeployment>();
                var deploymentReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

                var deploymentCallerHandler = web3.Eth.GetContractDeploymentHandler<TestDeployment>();
                var deploymentReceiptCaller = await deploymentCallerHandler.SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

                var callMeFunction1 = new CallMeFunction()
                {
                    Name = "Hi",
                    Greeting = "From the other contract"
                };

                var contracthandler = web3.Eth.GetContractHandler(deploymentReceiptCaller.ContractAddress);

                var callManyOthersFunctionMessage = new CallManyContractsSameQueryFunction()
                {
                    Destination = new string[]
                    {
                        deploymentReceipt.ContractAddress, deploymentReceipt.ContractAddress,
                        deploymentReceipt.ContractAddress
                    }.ToList(),
                    Data = callMeFunction1.GetCallData()
                };

                var returnVarByteArray = await contracthandler
                    .QueryAsync<CallManyContractsSameQueryFunction, List<byte[]>>(callManyOthersFunctionMessage)
                    .ConfigureAwait(false);


                var expected = "Hello Hi From the other contract";

                var firstVar = new StringTypeDecoder().Decode(returnVarByteArray[0]);
                var secondVar = new StringTypeDecoder().Decode(returnVarByteArray[1]);
                var thirdVar = new StringTypeDecoder().Decode(returnVarByteArray[2]);

                Assert.Equal(expected, firstVar);
                Assert.Equal(expected, secondVar);
                Assert.Equal(expected, thirdVar);

                callMeFunction1.Name = "";
                callMeFunction1.Greeting = "";

                var expectedShort = "Hello  ";
                callManyOthersFunctionMessage = new CallManyContractsSameQueryFunction()
                {
                    Destination = new string[]
                    {
                        deploymentReceipt.ContractAddress, deploymentReceipt.ContractAddress,
                        deploymentReceipt.ContractAddress
                    }.ToList(),
                    Data = callMeFunction1.GetCallData()
                };

                returnVarByteArray = await contracthandler
                    .QueryAsync<CallManyContractsSameQueryFunction, List<byte[]>>(callManyOthersFunctionMessage)
                    .ConfigureAwait(false);

                firstVar = new StringTypeDecoder().Decode(returnVarByteArray[0]);
                secondVar = new StringTypeDecoder().Decode(returnVarByteArray[1]);
                thirdVar = new StringTypeDecoder().Decode(returnVarByteArray[2]);

                Assert.Equal(expectedShort, firstVar);
                Assert.Equal(expectedShort, secondVar);
                Assert.Equal(expectedShort, thirdVar);
            }

        }


        [Fact]
        public async void ShouldDecodeFixedWithVariableElementsAndVariableElements()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                //also should be able to call another contract and get the output as bytes and bytes arrays
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();

                var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TheOtherDeployment>();
                var deploymentReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

                var deploymentCallerHandler =
                    web3.Eth
                        .GetContractDeploymentHandler<SolidityCallAnotherContract.Contracts.Test.CQS.TestDeployment>();
                var deploymentReceiptCaller = await deploymentCallerHandler.SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);
                ;

                var contracthandler = web3.Eth.GetContractHandler(deploymentReceiptCaller.ContractAddress);

                var callManyOthersFunctionMessage = new CallManyOtherContractsFixedArrayReturnFunction()
                {
                    TheOther = deploymentReceipt.ContractAddress
                };

                var callOtherFunctionMessage = new CallAnotherContractFunction()
                {
                    TheOther = deploymentReceipt.ContractAddress
                };

                var returnValue = await contracthandler.QueryRawAsync(callManyOthersFunctionMessage).ConfigureAwait(false);
                var inHex = returnValue.ToHex();

                var expected =
                    "Hello Solidity Welcome something much much biggger jlkjfslkfjslkdfjsldfjasdflkjsafdlkjasdfljsadfljasdfkljasdkfljsadfljasdfldsfaj booh!";

                var returnByteArray =
                    await contracthandler.QueryAsync<CallManyOtherContractsFixedArrayReturnFunction, List<Byte[]>>(
                        callManyOthersFunctionMessage).ConfigureAwait(false);
                //var inHex = returnValue.ToHex();
                var first = new StringTypeDecoder().Decode(returnByteArray[0]);
                var second = new StringTypeDecoder().Decode(returnByteArray[1]);
                var third = new StringTypeDecoder().Decode(returnByteArray[2]);
                Assert.Equal(expected, first);
                Assert.Equal(expected, second);
                Assert.Equal(expected, third);

                var callManyOthersVariableFunctionMessage = new CallManyOtherContractsVariableArrayReturnFunction()
                {
                    TheOther = deploymentReceipt.ContractAddress
                };

                var returnVarByteArray =
                    await contracthandler.QueryAsync<CallManyOtherContractsVariableArrayReturnFunction, List<Byte[]>>(
                        callManyOthersVariableFunctionMessage).ConfigureAwait(false);
                //var inHex = returnValue.ToHex();
                var firstVar = new StringTypeDecoder().Decode(returnVarByteArray[0]);
                var secondVar = new StringTypeDecoder().Decode(returnVarByteArray[1]);
                var thirdVar = new StringTypeDecoder().Decode(returnVarByteArray[2]);

                Assert.Equal(expected, firstVar);
                Assert.Equal(expected, secondVar);
                Assert.Equal(expected, thirdVar);

                var returnValue1Call =
                    await contracthandler.QueryAsync<CallAnotherContractFunction, byte[]>(callOtherFunctionMessage).ConfigureAwait(false);

                var return1ValueString = new StringTypeDecoder().Decode(returnValue1Call);
                Assert.Equal(expected, return1ValueString);
            }
        }
    }
}﻿using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.CQS;
using Nethereum.JsonRpc.Client;
using Nethereum.XUnitEthereumClients;
using Xunit;
using static Nethereum.Accounts.IntegrationTests.ABIIntegerTests;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ABIIntegerTests
    {

        //Smart contract for testing
        /*
        contract Test{
    
    function MinInt256() view public returns (int256){
        return int256((uint256(1) << 255));
    }
    
    function MaxUint() view public returns (uint256) {
        return 2**256 - 1;
    }
    
    function MaxInt256() view public returns (int256) {
        return int256(~((uint256(1) << 255)));
    }
    
    //Pass a value, what is left to be the Max, and will return -1
    function OverflowInt256ByQuantity(int256 value, int256 valueToAddToMaxInt256) view public returns (int256) {
        return (value + valueToAddToMaxInt256 + 1) + MaxInt256();
    }
    
    //Pass a value, what is left to be the Min, and will return -1
    function UnderflowInt256ByQuantity(int256 value, int256 valueToAddToMinInt256) view public returns (int256) {
        return (value + valueToAddToMinInt256 - 1) + MinInt256();
    }
    
    //This is -1
    function OverflowInt256() view public returns (int256) {
        return (MaxInt256() + 1) + MaxInt256();
    }
    
    //This is -1
    function UnderflowInt256() view public returns (int256) {
        return (MinInt256() - 1) + MinInt256();
    }
    
    function OverflowUInt256() view public returns (uint256) {
        return MaxUint() + 1;
    }
    
    //Pass a value, what is left to be the Max, and will return 0
    function OverflowUInt256ByQuantity(uint256 value, uint256 valueToAddToMaxUInt256) view public returns (uint256) {
        return value + valueToAddToMaxUInt256 + 1;
    }

}
*/


        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ABIIntegerTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        public class TestDeployment : ContractDeploymentMessage
        {

            public static string BYTECODE = "608060405234801561001057600080fd5b50610279806100206000396000f3006080604052600436106100985763ffffffff7c0100000000000000000000000000000000000000000000000000000000600035041663345bd9a3811461009d5780634b562c36146100ca57806371041b0a146100df57806372679287146100fa578063840c54d61461010f578063ab9df2e614610124578063ac6dc0801461013f578063cbf412f314610154578063d28da58014610169575b600080fd5b3480156100a957600080fd5b506100b860043560243561017e565b60408051918252519081900360200190f35b3480156100d657600080fd5b506100b8610197565b3480156100eb57600080fd5b506100b86004356024356101b2565b34801561010657600080fd5b506100b86101b9565b34801561011b57600080fd5b506100b86101cb565b34801561013057600080fd5b506100b86004356024356101ef565b34801561014b57600080fd5b506100b8610208565b34801561016057600080fd5b506100b8610223565b34801561017557600080fd5b506100b8610247565b6000610188610223565b82840160010101905092915050565b60006101a1610223565b6101a9610223565b60010101905090565b0160010190565b60006101c3610247565b600101905090565b7f800000000000000000000000000000000000000000000000000000000000000090565b60006101f96101cb565b60018385010301905092915050565b60006102126101cb565b600161021c6101cb565b0301905090565b7f7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff90565b600019905600a165627a7a723058202c39408cbf8485f567816db744d5263cbf1f5fb891f795bed766ebef54807c360029";

            public TestDeployment() : base(BYTECODE) { }

            public TestDeployment(string byteCode) : base(byteCode) { }


        }

        [Function("MaxUint", "uint256")]
        public class MaxFunction : FunctionMessage
        {

        }

        [Function("MaxInt256", "int256")]
        public class MaxInt256Function : FunctionMessage
        {

        }

        [Function("MinInt256", "int256")]
        public class MinInt256Function : FunctionMessage
        {

        }

        [Function("UnderflowInt256ByQuantity", "int256")]
        public class UnderflowInt256ByQuantityFunction : FunctionMessage
        {
            [Parameter("int256", "value", 1)]
            public BigInteger Value { get; set; }
            [Parameter("int256", "valueToAddToMinInt256", 2)]
            public BigInteger ValueToAddToMinInt256 { get; set; }
        }

        [Function("OverflowInt256ByQuantity", "int256")]
        public class OverflowInt256ByQuantityFunction : FunctionMessage
        {
            [Parameter("int256", "value", 1)]
            public BigInteger Value { get; set; }
            [Parameter("int256", "valueToAddToMaxInt256", 2)]
            public BigInteger ValueToAddToMaxInt256 { get; set; }
        }

        [Function("OverflowUInt256ByQuantity", "uint256")]
        public class OverflowUInt256ByQuantityFunction : FunctionMessage
        {
            [Parameter("uint256", "value", 1)]
            public BigInteger Value { get; set; }
            [Parameter("uint256", "valueToAddToMaxUInt256", 2)]
            public BigInteger ValueToAddToMaxUInt256 { get; set; }
        }

        [Fact]
        public async Task MinInt256()
        {
            var loggerMock = new Mock<ILogger<RpcClient>>();
            var web3 = GetWeb3(loggerMock.Object);
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestDeployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);
            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);
            var result = await contractHandler.QueryAsync<MinInt256Function, BigInteger>().ConfigureAwait(false);
            Assert.Equal(result, BigInteger.Parse("-57896044618658097711785492504343953926634992332820282019728792003956564819968"));
            loggerMock.VerifyLog(logger => logger.LogTrace("*RPC Response: 0x8000000000000000000000000000000000000000000000000000000000000000*"));
        }

        [Fact]
        public async Task MaxInt256()
        {
            var loggerMock = new Mock<ILogger<RpcClient>>();

            var web3 = GetWeb3(loggerMock.Object);
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestDeployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);
            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);

            var result = await contractHandler.QueryAsync<MaxInt256Function, BigInteger>().ConfigureAwait(false);
            Assert.Equal(result, BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819967"));
            loggerMock.VerifyLog(logger => logger.LogTrace("*RPC Response: 0x7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff*"));

        }

        //This test forces an overflow to test the encoding of the values for different values on the limits.
        //If the encodign is correct the overflow will occur and the result will be -1
        /*
         function OverflowInt256ByQuantity(int256 value, int256 valueToAddToMaxInt256) view public returns (int256) {
            return (value + valueToAddToMaxInt256 + 1) + MaxInt256();
          }
         */
        [Fact]
        public async Task OverflowInt256TestingEncoding()
        {
            var loggerMock = new Mock<ILogger<RpcClient>>();

            var web3 = GetWeb3(loggerMock.Object);
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestDeployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);

            for (var i = 1; i < 1000000; i = i + 100000)
            {
                for (var x = 0; x < 100; x++)
                {
                    var testAmount = i + x;

                    var result = await contractHandler.QueryAsync<OverflowInt256ByQuantityFunction, BigInteger>(
                        new OverflowInt256ByQuantityFunction()
                        {
                            ValueToAddToMaxInt256 = testAmount,
                            Value = IntType.MAX_INT256_VALUE - testAmount
                        }
                    ).ConfigureAwait(false);

                    loggerMock.Invocations.Last().Arguments.Contains("*RPC Response: 0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff*");


                    Assert.Equal(-1, result);
                }
            }
        }


        [Fact]
        public async Task OverflowUInt256TestingEncoding()
        {
            var loggerMock = new Mock<ILogger<RpcClient>>();

            var web3 = GetWeb3(loggerMock.Object);
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestDeployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);

            for (var i = 1; i < 1000000; i = i + 100000)
            {
                for (var x = 0; x < 100; x++)
                {
                    var testAmount = i + x;

                    var result = await contractHandler.QueryAsync<OverflowUInt256ByQuantityFunction, BigInteger>(
                        new OverflowUInt256ByQuantityFunction()
                        {
                            ValueToAddToMaxUInt256 = testAmount,
                            Value = IntType.MAX_UINT256_VALUE - testAmount
                        }
                    ).ConfigureAwait(false);
                    loggerMock.Invocations.Last().Arguments.Contains("*RPC Response: 0x0000000000000000000000000000000000000000000000000000000000000000*");


                    Assert.Equal(0, result);
                }
            }
        }

        //This test forces an underflow to test the encoding of the values for different values on the limits.
        //If the encodign is correct the underflow will occur and the result will be -1

        /*
        function UnderflowInt256ByQuantity(int256 value, int256 valueToAddToMinInt256) view public returns(int256)
        {
            return (value + valueToAddToMinInt256 - 1) + MinInt256();
        }
        */

        [Fact]
        public async Task UnderflowInt256TestingEncoding()
        {
            var loggerMock = new Mock<ILogger<RpcClient>>();

            var web3 = GetWeb3(loggerMock.Object);
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestDeployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);

            for (var i = 1; i < 1000000; i = i + 100000)
            {
                for (var x = 0; x < 100; x++)
                {
                    var testAmount = i + x;
                    var result = await contractHandler.QueryAsync<UnderflowInt256ByQuantityFunction, BigInteger>(
                        new UnderflowInt256ByQuantityFunction()
                        {
                            ValueToAddToMinInt256 = testAmount * -1,
                            Value = IntType.MIN_INT256_VALUE + testAmount
                        }
                    ).ConfigureAwait(false);
                    loggerMock.Invocations.Last().Arguments.Contains("*RPC Response: 0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff*");
                   
                    Assert.Equal(-1, result);
                }
            }
        }

        [Fact]
        public async Task UMaxInt256()
        {
            var loggerMock = new Mock<ILogger<RpcClient>>();

            var web3 = GetWeb3(loggerMock.Object);
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestDeployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);
            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);
            var result = await contractHandler.QueryAsync<MaxFunction, BigInteger>().ConfigureAwait(false);
            loggerMock.VerifyLog(logger => logger.LogTrace("*RPC Response: 0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff*"));
            
            Assert.Equal(result, BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935"));
        }

        public Web3.Web3 GetWeb3(ILogger logger)
        {
            var web3 = new Nethereum.Web3.Web3(_ethereumClientIntegrationFixture.GetWeb3().TransactionManager.Account,
                _ethereumClientIntegrationFixture.GetHttpUrl(), logger);
            return web3;
        }
       
    }
}﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Contracts.CQS;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ABIStructsTests
    {
        /*
     pragma solidity "0.4.25";
pragma experimental ABIEncoderV2;

contract TestV2
{
   
        uint256 public id1 = 1;
        uint256 public id2;
        uint256 public id3;
        string  public id4;
        TestStruct public testStructStorage;


        event TestStructStorageChanged(address sender, TestStruct testStruct);

        struct SubSubStruct {
            uint256 id;
        }

        struct SubStruct {
            uint256 id;
            SubSubStruct sub;
            string id2;
        }

        struct TestStruct {
            uint256 id;
            SubStruct subStruct1;
            SubStruct subStruct2;
            string id2;
        }

        struct SimpleStruct{
            uint256 id;
            uint256 id2;
        }

        function TestArray() pure public returns (SimpleStruct[2] structArray) {
            structArray[0] = (SimpleStruct(1, 100));
            structArray[1] = (SimpleStruct(2, 200));
            return structArray;
        }

        function Test(TestStruct testScrut) public {
            id1 = testScrut.id;
            id2 = testScrut.subStruct1.id;
            id3 = testScrut.subStruct2.sub.id;
            id4 = testScrut.subStruct2.id2;
    
        }

        function SetStorageStruct(TestStruct testStruct) public {
            testStructStorage = testStruct;
            emit TestStructStorageChanged(msg.sender, testStruct);
        }

        function GetTest() public view returns(TestStruct testStruct, int test1, int test2){
            testStruct.id = 1;
            testStruct.id2 = "hello";
            testStruct.subStruct1.id = 200;
            testStruct.subStruct1.id2 = "Giraffe";
            testStruct.subStruct1.sub.id = 20;
            testStruct.subStruct2.id = 300;
            testStruct.subStruct2.id2 = "Elephant";
            testStruct.subStruct2.sub.id = 30000;
            test1 = 5;
            test2 = 6;
        }
    
        struct Empty{
    
        }

        function TestEmpty(Empty empty) public {
    
        }
}
        
}
        */


        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ABIStructsTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task StructTests()
        {
            await SolidityV2StructTests().ConfigureAwait(false);
        }


        [Function("id1", "uint256")]
        public class Id1Function : FunctionMessage
        {

        }

        [Function("id2", "uint256")]
        public class Id2Function : FunctionMessage
        {

        }

        [Function("id3", "uint256")]
        public class Id3Function : FunctionMessage
        {

        }

        [Function("id4", "string")]
        public class Id4Function : FunctionMessage
        {

        }

        [Function("GetTest")]
        public class GetTestFunction : FunctionMessage
        {
            
        }

        [Function("testStructStorage")]
        public class GetTestStructStorageFunction : FunctionMessage
        {

        }

        public class SimpleStruct
        {
            [Parameter("uint256", "id1", 1)]
            public BigInteger Id1 { get; set; }

            [Parameter("uint256", "id2", 2)]
            public BigInteger Id2 { get; set; }
        }

        [Function("TestArray", typeof(TestArrayOuputDTO))]
        public class TestArray : FunctionMessage
        {
        }

        [FunctionOutput]
        public class TestArrayOuputDTO:IFunctionOutputDTO
        {
            [Parameter("tuple[2]", "simpleStruct", 1)]
            public List<SimpleStruct> SimpleStructs { get; set;}
        }

        [FunctionOutput]
        public class GetTestFunctionOuptputDTO:IFunctionOutputDTO
        {
            [Parameter("tuple")]
            public TestStructStrings TestStruct { get; set; }


            [Parameter("int256", "test1", 2)]
            public BigInteger Test1 { get; set; }


            [Parameter("int256", "test2", 3)]
            public BigInteger Test2 { get; set; }
        }

        [Function("Test")]
        public class TestFunction : FunctionMessage
        {
            [Parameter("tuple", "testStruct")]
            public TestStructStrings TestStruct { get; set; }
        }

        [Function("SetStorageStruct")]
        public class SetStorageStructFunction : FunctionMessage
        {
            [Parameter("tuple", "testStruct")]
            public TestStructStrings TestStruct { get; set; }
        }

        [Event("TestStructStorageChanged")]
        public class TestStructStorageChangedEvent: IEventDTO
        {
            [Parameter("address", "sender", 1)]
            public string Address { get; set; }

            [Parameter("tuple", "testStruct", 2)]
            public TestStructStrings TestStruct { get; set; }
        }


        [FunctionOutput]
        public class TestStructStrings: IFunctionOutputDTO
        {
            [Parameter("uint256", "id", 1)]
            public BigInteger Id { get; set; }

            [Parameter("tuple", "subStruct1", 2)]
            public SubStructUintString SubStruct1 { get; set; }

            [Parameter("tuple", "subStruct2", 3)]
            public SubStructUintString SubStruct2 { get; set; }

            [Parameter("string", "id2", 4)]
            public string Id2 { get; set; }
        }


        public class SubStructUintString
        {
            [Parameter("uint256", "id", 1)]
            public BigInteger Id { get; set; }

            [Parameter("tuple", "sub", 2)]
            public SubStructUInt Sub { get; set; }

            [Parameter("string", "id2", 3)]
            public String Id2 { get; set; }
        }

        public class SubStructUInt
        {
            [Parameter("uint256", "id", 1)]
            public BigInteger Id { get; set; }
        }

        public class TestContractDeployment : ContractDeploymentMessage
        {
            public const string BYTE_CODE = "0x6080604052600160005534801561001557600080fd5b50610d7a806100256000396000f3006080604052600436106100a35763ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166319dcec3d81146100a857806326145698146100d55780632abda41e146100f75780634d8f11fa14610117578063517999bc1461013957806363845ba31461015b578063a55001a614610180578063e5207eaa146101a0578063e9159d64146101b5578063f6ee7d9f146101d7575b600080fd5b3480156100b457600080fd5b506100bd6101ec565b6040516100cc93929190610bf9565b60405180910390f35b3480156100e157600080fd5b506100f56100f0366004610a06565b6102d1565b005b34801561010357600080fd5b506100f56101123660046109e1565b61039c565b34801561012357600080fd5b5061012c61039f565b6040516100cc9190610bd4565b34801561014557600080fd5b5061014e6103db565b6040516100cc9190610c26565b34801561016757600080fd5b506101706103e1565b6040516100cc9493929190610c34565b34801561018c57600080fd5b506100f561019b366004610a06565b6105f5565b3480156101ac57600080fd5b5061014e610628565b3480156101c157600080fd5b506101ca61062e565b6040516100cc9190610be8565b3480156101e357600080fd5b5061014e6106bc565b6101f46106c2565b6001815260408051808201825260058082527f68656c6c6f0000000000000000000000000000000000000000000000000000006020808401919091526060850192909252818401805160c8905283518085018552600781527f47697261666665000000000000000000000000000000000000000000000000008185015281518501525182015160149052828401805161012c905283518085018552600881527f456c657068616e74000000000000000000000000000000000000000000000000818501528151909401939093529151015161753090529091600690565b8051600490815560208083015180516005908155818301515160065560408201518051869594610306926007929101906106f8565b5050506040828101518051600484019081556020808301515160058601559282015180519293919261033e92600687019201906106f8565b5050506060820151805161035c9160078401916020909101906106f8565b509050507fc4948cf046f20c08b2b7f5b0b6de7bdbe767d009d512c8440b98eb424bdb9ad83382604051610391929190610bb4565b60405180910390a150565b50565b6103a7610776565b60408051808201825260018152606460208083019190915290835281518083019092526002825260c8828201528201525b90565b60005481565b6004805460408051606081018252600580548252825160208181018552600654825280840191909152600780548551601f6002600019610100600186161502019093169290920491820184900484028101840187528181529697969495939493860193928301828280156104965780601f1061046b57610100808354040283529160200191610496565b820191906000526020600020905b81548152906001019060200180831161047957829003601f168201915b5050509190925250506040805160608101825260048501805482528251602080820185526005880154825280840191909152600687018054855160026001831615610100026000190190921691909104601f81018490048402820184018752808252979897949650929486019390918301828280156105565780601f1061052b57610100808354040283529160200191610556565b820191906000526020600020905b81548152906001019060200180831161053957829003601f168201915b5050509190925250505060078201805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815293949392918301828280156105eb5780601f106105c0576101008083540402835291602001916105eb565b820191906000526020600020905b8154815290600101906020018083116105ce57829003601f168201915b5050505050905084565b8051600055602080820151516001556040808301518083015151600255015180516106249260039201906106f8565b5050565b60025481565b6003805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815292918301828280156106b45780601f10610689576101008083540402835291602001916106b4565b820191906000526020600020905b81548152906001019060200180831161069757829003601f168201915b505050505081565b60015481565b61010060405190810160405280600081526020016106de6107a4565b81526020016106eb6107a4565b8152602001606081525090565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061073957805160ff1916838001178555610766565b82800160010185558215610766579182015b8281111561076657825182559160200191906001019061074b565b506107729291506107bf565b5090565b6080604051908101604052806002905b61078e6107d9565b8152602001906001900390816107865790505090565b606060405190810160405280600081526020016106eb6107f0565b6103d891905b8082111561077257600081556001016107c5565b604080518082019091526000808252602082015290565b60408051602081019091526000815290565b6000601f8201831361081357600080fd5b813561082661082182610cad565b610c86565b9150808252602083016020830185838301111561084257600080fd5b61084d838284610cfe565b50505092915050565b600080828403121561086757600080fd5b6108716000610c86565b9392505050565b60006060828403121561088a57600080fd5b6108946060610c86565b905060006108a284846109d5565b82525060206108b3848483016108eb565b602083015250604082013567ffffffffffffffff8111156108d357600080fd5b6108df84828501610802565b60408301525092915050565b6000602082840312156108fd57600080fd5b6109076020610c86565b9050600061091584846109d5565b82525092915050565b60006080828403121561093057600080fd5b61093a6080610c86565b9050600061094884846109d5565b825250602082013567ffffffffffffffff81111561096557600080fd5b61097184828501610878565b602083015250604082013567ffffffffffffffff81111561099157600080fd5b61099d84828501610878565b604083015250606082013567ffffffffffffffff8111156109bd57600080fd5b6109c984828501610802565b60608301525092915050565b600061087182356103d8565b60008082840312156109f257600080fd5b60006109fe8484610856565b949350505050565b600060208284031215610a1857600080fd5b813567ffffffffffffffff811115610a2f57600080fd5b6109fe8482850161091e565b610a4481610ce5565b82525050565b610a5381610cd5565b610a5c826103d8565b60005b82811015610a8c57610a72858351610ad1565b610a7b82610cdf565b604095909501949150600101610a5f565b5050505050565b610a44816103d8565b6000610aa782610cdb565b808452610abb816020860160208601610d0a565b610ac481610d36565b9093016020019392505050565b80516040830190610ae28482610a93565b506020820151610af56020850182610a93565b50505050565b80516000906060840190610b0f8582610a93565b506020830151610b226020860182610b43565b5060408301518482036040860152610b3a8282610a9c565b95945050505050565b80516020830190610af58482610a93565b80516000906080840190610b688582610a93565b5060208301518482036020860152610b808282610afb565b91505060408301518482036040860152610b9a8282610afb565b91505060608301518482036060860152610b3a8282610a9c565b60408101610bc28285610a3b565b81810360208301526109fe8184610b54565b60808101610be28284610a4a565b92915050565b602080825281016108718184610a9c565b60608082528101610c0a8186610b54565b9050610c196020830185610a93565b6109fe6040830184610a93565b60208101610be28284610a93565b60808101610c428287610a93565b8181036020830152610c548186610afb565b90508181036040830152610c688185610afb565b90508181036060830152610c7c8184610a9c565b9695505050505050565b60405181810167ffffffffffffffff81118282101715610ca557600080fd5b604052919050565b600067ffffffffffffffff821115610cc457600080fd5b506020601f91909101601f19160190565b50600290565b5190565b60200190565b73ffffffffffffffffffffffffffffffffffffffff1690565b82818337506000910152565b60005b83811015610d25578181015183820152602001610d0d565b83811115610af55750506000910152565b601f01601f1916905600a265627a7a723058201a204c8fd11b9facac01a86aaac24ebbc6159e540ae80a3dfe6fa745070a73516c6578706572696d656e74616cf50037";

            public TestContractDeployment() : base(BYTE_CODE)
            {
            }
        }


        public async Task SolidityV2StructTests()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestContractDeployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);
            
            var functionTest = new TestFunction();
            var input = new TestStructStrings()
            {
                Id = 1,
                Id2 = "hello",
                SubStruct1 = new SubStructUintString()
                {
                    Id = 200,
                    Id2 = "Giraffe",
                    Sub = new SubStructUInt()
                    {
                        Id = 20
                    }
                },
                SubStruct2 = new SubStructUintString()
                {
                    Id = 300,
                    Id2 = "Elephant",
                    Sub = new SubStructUInt()
                    {
                        Id = 30000
                    }
                },
            };

            functionTest.TestStruct = input;

            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);

            var testStructArrayResult =
                await contractHandler.QueryDeserializingToObjectAsync<TestArray, TestArrayOuputDTO>().ConfigureAwait(false);

            Assert.Equal(1, testStructArrayResult.SimpleStructs[0].Id1);
            Assert.Equal(2, testStructArrayResult.SimpleStructs[1].Id1);

            var id1Before = await contractHandler.QueryAsync<Id1Function, BigInteger>().ConfigureAwait(false);
            Assert.Equal(1, id1Before);

            var receiptTransaction = await contractHandler.SendRequestAndWaitForReceiptAsync(functionTest).ConfigureAwait(false);

            var id1After = await contractHandler.QueryAsync<Id1Function, BigInteger>().ConfigureAwait(false);
            Assert.Equal(1, id1After);
            var id2After = await contractHandler.QueryAsync<Id2Function, BigInteger>().ConfigureAwait(false);
            Assert.Equal(200, id2After);
            var id3After = await contractHandler.QueryAsync<Id3Function, BigInteger>().ConfigureAwait(false);
            Assert.Equal(30000, id3After);
            var id4After = await contractHandler.QueryAsync<Id4Function, string>().ConfigureAwait(false);
            Assert.Equal("Elephant", id4After);
            var testDataFromContract = await contractHandler.QueryDeserializingToObjectAsync<GetTestFunction, GetTestFunctionOuptputDTO>().ConfigureAwait(false);
            Assert.Equal(5, testDataFromContract.Test1);
            var functionStorage = new SetStorageStructFunction {TestStruct = input};
            var receiptSending = await contractHandler.SendRequestAndWaitForReceiptAsync(functionStorage).ConfigureAwait(false);

            var storageData =  await contractHandler.QueryDeserializingToObjectAsync<GetTestStructStorageFunction, TestStructStrings>().ConfigureAwait(false);
            Assert.Equal("hello", storageData.Id2);
            var eventStorage = contractHandler.GetEvent<TestStructStorageChangedEvent>();
            var eventOutputs = eventStorage.DecodeAllEventsForEvent(receiptSending.Logs);
            Assert.Equal(1, eventOutputs[0].Event.TestStruct.Id);

            var eventUntyped = new Event(web3.Client, deploymentReceipt.ContractAddress, eventStorage.EventABI);
            var eventOutputs2 = eventUntyped.DecodeAllEventsDefaultForEvent(receiptSending.Logs);
            Assert.True("0x12890D2cce102216644c59daE5baed380d84830c".IsTheSameAddress(eventOutputs2[0].Event[0].Result.ToString()));
            Assert.Equal("sender", eventOutputs2[0].Event[0].Parameter.Name);

        }
    }
}﻿using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using Nethereum.Web3.Accounts.Managed;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{

    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class AccountTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public AccountTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task ShouldBeAbleToDeployAContractLoadingEncryptedPrivateKey()
        {
            var senderAddress = "0x12890d2cce102216644c59daE5baed380d84830c";
            var password = "password";
            //this is your wallet key file which can be found on

            //Linux: ~/.ethereum/keystore
            //Mac: /Library/Ethereum/keystore
            //Windows: %APPDATA%/Ethereum

            var keyStoreEncryptedJson =
                @"{""crypto"":{""cipher"":""aes-128-ctr"",""ciphertext"":""b4f42e48903879b16239cd5508bc5278e5d3e02307deccbec25b3f5638b85f91"",""cipherparams"":{""iv"":""dc3f37d304047997aa4ef85f044feb45""},""kdf"":""scrypt"",""mac"":""ada930e08702b89c852759bac80533bd71fc4c1ef502291e802232b74bd0081a"",""kdfparams"":{""n"":65536,""r"":1,""p"":8,""dklen"":32,""salt"":""2c39648840b3a59903352b20386f8c41d5146ab88627eaed7c0f2cc8d5d95bd4""}},""id"":""19883438-6d67-4ab8-84b9-76a846ce544b"",""address"":""12890d2cce102216644c59dae5baed380d84830c"",""version"":3}";

            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""val"",""type"":""int256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""int256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""int256""}],""type"":""constructor""}]";
            var byteCode =
                "0x60606040526040516020806052833950608060405251600081905550602b8060276000396000f3606060405260e060020a60003504631df4f1448114601a575b005b600054600435026060908152602090f3";

            var multiplier = 7;

            //if not using portable or netstandard (^net45) you can use LoadFromKeyStoreFile to load the file from the file system.

            var acccount = Account.LoadFromKeyStore(keyStoreEncryptedJson, password);

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt = await
                web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, byteCode, senderAddress,
                    new HexBigInteger(900000), null, multiplier).ConfigureAwait(false);

            var contractAddress = receipt.ContractAddress;

            var contract = web3.Eth.GetContract(abi, contractAddress);

            var multiplyFunction = contract.GetFunction("multiply");

            var result = await multiplyFunction.CallAsync<int>(7).ConfigureAwait(false);

            Assert.Equal(49, result);
        }

        [Fact]
        public async Task ShouldBeAbleToDeployAContractUsingPersonalUnlock()
        {

            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var password = EthereumClientIntegrationFixture.ManagedAccountPassword;
            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""val"",""type"":""int256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""int256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""int256""}],""type"":""constructor""}]";
            var byteCode =
                "0x60606040526040516020806052833950608060405251600081905550602b8060276000396000f3606060405260e060020a60003504631df4f1448114601a575b005b600054600435026060908152602090f3";

            var multiplier = 7;

            var web3 = _ethereumClientIntegrationFixture.GetWeb3Managed();

            var receipt = await
                web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, byteCode, senderAddress,
                    new HexBigInteger(900000), null, multiplier).ConfigureAwait(false);

            var contractAddress = receipt.ContractAddress;

            var contract = web3.Eth.GetContract(abi, contractAddress);

            var multiplyFunction = contract.GetFunction("multiply");

            var result = await multiplyFunction.CallAsync<int>(7).ConfigureAwait(false);

            Assert.Equal(49, result);
        }

        [Fact]
        public async Task ShouldBeAbleToDeployAContractUsingPrivateKey()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""val"",""type"":""int256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""int256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""int256""}],""type"":""constructor""}]";
            var byteCode =
                "0x60606040526040516020806052833950608060405251600081905550602b8060276000396000f3606060405260e060020a60003504631df4f1448114601a575b005b600054600435026060908152602090f3";

            var multiplier = 7;

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt = await
                web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, byteCode, senderAddress,
                    new HexBigInteger(900000), null, multiplier).ConfigureAwait(false);

            var contractAddress = receipt.ContractAddress;

            var contract = web3.Eth.GetContract(abi, contractAddress);

            var multiplyFunction = contract.GetFunction("multiply");

            var result = await multiplyFunction.CallAsync<int>(7).ConfigureAwait(false);

            Assert.Equal(49, result);
        }

        [Fact]
        public async Task ShouldBeAbleToTransferBetweenAccountsUsingManagedAccount()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var password = EthereumClientIntegrationFixture.ManagedAccountPassword;
            
            var addressTo = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";

            // A managed account is an account which is maintained by the client (Geth / Parity)
            var web3 = _ethereumClientIntegrationFixture.GetWeb3Managed();

            //The transaction receipt polling service is a simple utility service to poll for receipts until mined
            var transactionPolling = (TransactionReceiptPollingService)web3.TransactionManager.TransactionReceiptService;

            var currentBalance = await web3.Eth.GetBalance.SendRequestAsync(addressTo).ConfigureAwait(false);
            //assumed client is mining already

            //When sending the transaction using the transaction manager for a managed account, personal_sendTransaction is used.

            var txnHash =
                await web3.TransactionManager.SendTransactionAsync(senderAddress, addressTo, new HexBigInteger(20)).ConfigureAwait(false);
            var receipt = await transactionPolling.PollForReceiptAsync(txnHash).ConfigureAwait(false);         

            var newBalance = await web3.Eth.GetBalance.SendRequestAsync(addressTo).ConfigureAwait(false);

            Assert.Equal(currentBalance.Value + 20, newBalance.Value);
        }

    }
}using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class AnonymousEventFilterTest
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public AnonymousEventFilterTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task TestEvent()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new TestAnonymousEventContractDeployment {FromAddress = senderAddress};
            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TestAnonymousEventContractDeployment>();
            var deploymentTransactionReceipt =
                await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage).ConfigureAwait(false);
            var contractHandler = web3.Eth.GetContractHandler(deploymentTransactionReceipt.ContractAddress);

            var eventFilter = contractHandler.GetEvent<ItemCreatedEventDTO>();
            var filterId = await eventFilter.CreateFilterAsync(1).ConfigureAwait(false);

            var transactionReceiptSend = await contractHandler.SendRequestAndWaitForReceiptAsync(
                new NewItemFunction()
                {
                    FromAddress = senderAddress,
                    Id = 1,
                    Price = 100
                }).ConfigureAwait(false);

            var result = await eventFilter.GetFilterChangesAsync(filterId).ConfigureAwait(false);

            Assert.Single(result);
        }

        public class TestAnonymousEventContractDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "608060405234801561001057600080fd5b5033600160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506101d7806100616000396000f3fe608060405234801561001057600080fd5b5060043610610048576000357c01000000000000000000000000000000000000000000000000000000009004806329b856881461004d575b600080fd5b6100836004803603604081101561006357600080fd5b810190808035906020019092919080359060200190929190505050610085565b005b6000606060405190810160405280848152602001838152602001600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681525090806001815401808255809150509060018203906000526020600020906003020160009091929091909150600082015181600001556020820151816001015560408201518160020160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505050508133604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390a1505056fea165627a7a72305820c62dadf2e61a3d98a2b9997af83269a815a4166e6610807642db0b3771896a220029";

            public TestAnonymousEventContractDeployment() : base(BYTECODE)
            {
            }

            public TestAnonymousEventContractDeployment(string byteCode) : base(byteCode)
            {
            }
        }

        [Event("ItemCreated", true)]
        public class ItemCreatedEventDTO : IEventDTO
        {
            [Parameter("uint256", "itemId", 1, true)]
            public BigInteger ItemId { get; set; }

            [Parameter("address", "result", 2, false)]
            public string Result { get; set; }
        }

        [Function("newItem")]
        public class NewItemFunction : FunctionMessage
        {
            [Parameter("uint256", "id", 1)] public BigInteger Id { get; set; }

            [Parameter("uint256", "price", 2)] public BigInteger Price { get; set; }
        }

/* Contract
contract TestAnonymousEventContract {
    struct Item {
        uint id;
        uint price;
        address manager;
    }

    Item[] items;
    address manager;

    constructor() public {
        manager = msg.sender;
    }
    
    event ItemCreated(uint indexed itemId, address result) anonymous;

    function newItem(uint id, uint price) public {
        items.push(Item(id, price, manager));
        emit ItemCreated(id, msg.sender);
    }
} 
*/
    }
}using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class AnonymousEventFilterTopicTest
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public AnonymousEventFilterTopicTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task TestEvent()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new TestAnonymousEventContractDeployment {FromAddress = senderAddress};
            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TestAnonymousEventContractDeployment>();
            var deploymentTransactionReceipt =
                await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage).ConfigureAwait(false);
            var contractHandler = web3.Eth.GetContractHandler(deploymentTransactionReceipt.ContractAddress);

            var itemCreatedEvent = contractHandler.GetEvent<ItemCreatedEventDTO>();

            var eventFilter1 = await itemCreatedEvent.CreateFilterAsync(1).ConfigureAwait(false);
            var eventFilter2 = await itemCreatedEvent.CreateFilterAsync(2).ConfigureAwait(false);
            var eventFilter12 = await itemCreatedEvent.CreateFilterAsync(new[] { 1, 2 }).ConfigureAwait(false);

            var newItem1FunctionTransactionReceipt = await contractHandler.SendRequestAndWaitForReceiptAsync(
                new NewItemFunction
                {
                    FromAddress = senderAddress,
                    Id = 1,
                    Price = 100
                }).ConfigureAwait(false);
            var newItem2FunctionTransactionReceipt = await contractHandler.SendRequestAndWaitForReceiptAsync(
                new NewItemFunction
                {
                    FromAddress = senderAddress,
                    Id = 2,
                    Price = 100
                }).ConfigureAwait(false);

            var logs1Result = await itemCreatedEvent.GetFilterChangesAsync(eventFilter1).ConfigureAwait(false);
            Assert.Single(logs1Result);
            Assert.Equal(1, logs1Result[0].Event.ItemId);

            var logs2Result = await itemCreatedEvent.GetFilterChangesAsync(eventFilter2).ConfigureAwait(false);
            Assert.Single(logs2Result);
            Assert.Equal(2, logs2Result[0].Event.ItemId);

            var logs12Result = await itemCreatedEvent.GetFilterChangesAsync(eventFilter12).ConfigureAwait(false);
            Assert.Equal(2, logs12Result.Count);
            Assert.Contains(logs12Result, el => el.Event.ItemId == 1);
            Assert.Contains(logs12Result, el => el.Event.ItemId == 2);
        }

        public class TestAnonymousEventContractDeployment : ContractDeploymentMessage
        {
            public const string BYTECODE =
                "608060405234801561001057600080fd5b5033600160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506101d7806100616000396000f3fe608060405234801561001057600080fd5b5060043610610048576000357c01000000000000000000000000000000000000000000000000000000009004806329b856881461004d575b600080fd5b6100836004803603604081101561006357600080fd5b810190808035906020019092919080359060200190929190505050610085565b005b6000606060405190810160405280848152602001838152602001600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681525090806001815401808255809150509060018203906000526020600020906003020160009091929091909150600082015181600001556020820151816001015560408201518160020160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505050508133604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390a1505056fea165627a7a72305820c62dadf2e61a3d98a2b9997af83269a815a4166e6610807642db0b3771896a220029";

            public TestAnonymousEventContractDeployment() : base(BYTECODE)
            {
            }

            public TestAnonymousEventContractDeployment(string byteCode) : base(byteCode)
            {
            }
        }

        [Event("ItemCreated", true)]
        public class ItemCreatedEventDTO : IEventDTO
        {
            [Parameter("uint256", "itemId", 1, true)]
            public BigInteger ItemId { get; set; }

            [Parameter("address", "result", 2, false)]
            public string Result { get; set; }
        }

        [Function("newItem")]
        public class NewItemFunction : FunctionMessage
        {
            [Parameter("uint256", "id", 1)] public BigInteger Id { get; set; }

            [Parameter("uint256", "price", 2)] public BigInteger Price { get; set; }
        }

/* Contract 
contract TestAnonymousEventContract {
    struct Item {
        uint id;
        uint price;
        address manager;
    }

    Item[] items;
    address manager;

    constructor() public {
        manager = msg.sender;
    }
    
    event ItemCreated(uint indexed itemId, address result) anonymous;

    function newItem(uint id, uint price) public {
        items.push(Item(id, price, manager));
        emit ItemCreated(id, msg.sender);
    }
}
*/
    }
}using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable ConsiderUsingConfigureAwait
// ReSharper disable InconsistentNaming
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class AnonymousEventFilterWith2Topics
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public AnonymousEventFilterWith2Topics(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task TestEvent()
        {
            var bytecode =
                "608060405234801561001057600080fd5b5033600160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506101d8806100616000396000f3fe608060405234801561001057600080fd5b5060043610610048576000357c01000000000000000000000000000000000000000000000000000000009004806329b856881461004d575b600080fd5b6100836004803603604081101561006357600080fd5b810190808035906020019092919080359060200190929190505050610085565b005b6000606060405190810160405280848152602001838152602001600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681525090806001815401808255809150509060018203906000526020600020906003020160009091929091909150600082015181600001556020820151816001015560408201518160020160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff160217905550505050808233604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390a2505056fea165627a7a7230582091585526610b2382b1d3830ee346d3e852dab6dc3f9cefb8fe4b450988e126d10029";
            const string abi =
                @"[{""constant"":false,""inputs"":[{""name"":""id"",""type"":""uint256""},{""name"":""price"",""type"":""uint256""}],""name"":""newItem"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""constructor""},{""anonymous"":true,""inputs"":[{""indexed"":true,""name"":""itemId"",""type"":""uint256""},{""indexed"":true,""name"":""price"",""type"":""uint256""},{""indexed"":false,""name"":""result"",""type"":""address""}],""name"":""ItemCreated"",""type"":""event""}]";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var addressFrom = EthereumClientIntegrationFixture.AccountAddress;
            var deploymentTransactionReceipt = await DoTransactionAndWaitForReceiptAsync(web3,
                () => web3.Eth.DeployContract.SendRequestAsync(abi, bytecode, addressFrom, new HexBigInteger(900000))).ConfigureAwait(false);

            var code = await web3.Eth.GetCode.SendRequestAsync(deploymentTransactionReceipt.ContractAddress).ConfigureAwait(false);

            if (string.IsNullOrEmpty(code))
            {
                throw new Exception(
                    "Code was not deployed correctly, verify bytecode or enough gas was uto deploy the contract");
            }

            var contract = web3.Eth.GetContract(abi, deploymentTransactionReceipt.ContractAddress);

            var itemCreatedEvent = contract.GetEvent("ItemCreated");

            var filter1_ = await itemCreatedEvent.CreateFilterAsync(1).ConfigureAwait(false);
            var filter_22 = await itemCreatedEvent.CreateFilterAsync<object, int>(null, 22).ConfigureAwait(false);
            var filter1_22 = await itemCreatedEvent.CreateFilterAsync(1, 22).ConfigureAwait(false);
            var filter1_11 = await itemCreatedEvent.CreateFilterAsync(1, 11).ConfigureAwait(false);

            var newItemFunction = contract.GetFunction("newItem");

            var gas1_11 = await newItemFunction.EstimateGasAsync(1, 11).ConfigureAwait(false);
            var newItem1_11TransactionReceipt = await DoTransactionAndWaitForReceiptAsync(web3,
                () => newItemFunction.SendTransactionAsync(addressFrom, gas1_11, null, 1, 11)).ConfigureAwait(false);
            var gas2_22 = await newItemFunction.EstimateGasAsync(2, 22).ConfigureAwait(false);
            var newItem2_22TransactionReceipt = await DoTransactionAndWaitForReceiptAsync(web3,
                () => newItemFunction.SendTransactionAsync(addressFrom, gas2_22, null, 2, 22)).ConfigureAwait(false);

            var logs1_Result = await itemCreatedEvent.GetFilterChangesAsync<ItemCreatedEvent>(filter1_).ConfigureAwait(false);
            Assert.Single(logs1_Result);
            Assert.Equal(1, logs1_Result[0].Event.ItemId);
            Assert.Equal(11, logs1_Result[0].Event.Price);

            var logs_22Result = await itemCreatedEvent.GetFilterChangesAsync<ItemCreatedEvent>(filter_22).ConfigureAwait(false);
            Assert.Single(logs_22Result);
            Assert.Equal(2, logs_22Result[0].Event.ItemId);
            Assert.Equal(22, logs_22Result[0].Event.Price);

            var logs1_22Result = await itemCreatedEvent.GetFilterChangesAsync<ItemCreatedEvent>(filter1_22).ConfigureAwait(false);
            Assert.Empty(logs1_22Result);

            var logs1_11Result = await itemCreatedEvent.GetFilterChangesAsync<ItemCreatedEvent>(filter1_11).ConfigureAwait(false);
            Assert.Single(logs1_11Result);
            Assert.Equal(1, logs1_11Result[0].Event.ItemId);
            Assert.Equal(11, logs1_11Result[0].Event.Price);
        }

        private async Task<TransactionReceipt> DoTransactionAndWaitForReceiptAsync(Web3.Web3 web3,
            Func<Task<string>> transactionFunc)
        {
            var transactionHash = await transactionFunc().ConfigureAwait(false);

            TransactionReceipt receipt = null;

            while (receipt == null)
            {
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).ConfigureAwait(false);

                if (receipt != null)
                {
                    break;
                }

                await Task.Delay(100).ConfigureAwait(false);
            }

            return receipt;
        }

        [Event("ItemCreated", true)]
        public class ItemCreatedEvent
        {
            [Parameter("uint256", "itemId", 1, true)]
            public BigInteger ItemId { get; set; }

            [Parameter("uint256", "price", 2, true)]
            public BigInteger Price { get; set; }

            [Parameter("address", "result", 3, false)]
            public string Result { get; set; }
        }

/* Contract 
contract TestAnonymousEventContract {
    struct Item {
        uint id;
        uint price;
        address manager;
    }

    Item[] items;
    address manager;

    constructor() public {
        manager = msg.sender;
    }
    
    event ItemCreated(uint indexed itemId, uint indexed price, address result) anonymous;

    function newItem(uint id, uint price) public {
        items.push(Item(id, price, manager));
        emit ItemCreated(id, price, msg.sender);
    }
}
*/
    }
}using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable ConsiderUsingConfigureAwait
// ReSharper disable InconsistentNaming

namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class AnonymousEventFilterWith3Topics
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public AnonymousEventFilterWith3Topics(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task TestEvent()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new TestAnonymousEventContractDeployment {FromAddress = senderAddress};
            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TestAnonymousEventContractDeployment>();
            var deploymentTransactionReceipt =
                await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage).ConfigureAwait(false);
            var contractHandler = web3.Eth.GetContractHandler(deploymentTransactionReceipt.ContractAddress);

            var itemCreatedEvent = contractHandler.GetEvent<ItemCreatedEventDTO>();


            var eventFilter1__ = await itemCreatedEvent.CreateFilterAsync(1).ConfigureAwait(false);
            var eventFilter__SenderAddress =
                await itemCreatedEvent.CreateFilterAsync<object, object, string>(null, null, senderAddress).ConfigureAwait(false);

            var newItem1FunctionTransactionReceipt = await contractHandler.SendRequestAndWaitForReceiptAsync(
                new NewItemFunction
                {
                    Id = 1,
                    Price = 100
                }).ConfigureAwait(false);
            var newItem2FunctionTransactionReceipt = await contractHandler.SendRequestAndWaitForReceiptAsync(
                new NewItemFunction
                {
                    Id = 2,
                    Price = 100
                }).ConfigureAwait(false);

            var logs1__Result = await itemCreatedEvent.GetAllChangesAsync(eventFilter1__).ConfigureAwait(false);
            Assert.Single(logs1__Result);
            Assert.Equal(1, logs1__Result[0].Event.ItemId);
            Assert.Equal(100, logs1__Result[0].Event.Price);
            Assert.Equal(senderAddress.ToLower(), logs1__Result[0].Event.Result.ToLower());

            var logs__SenderAddress = await itemCreatedEvent.GetAllChangesAsync(eventFilter__SenderAddress).ConfigureAwait(false);
            Assert.Equal(2, logs__SenderAddress.Count);
            Assert.Contains(logs__SenderAddress, el => el.Event.ItemId == 1);
            Assert.Contains(logs__SenderAddress, el => el.Event.ItemId == 2);
        }

        public class TestAnonymousEventContractDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "608060405234801561001057600080fd5b5033600160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506101b8806100616000396000f3fe608060405234801561001057600080fd5b5060043610610048576000357c01000000000000000000000000000000000000000000000000000000009004806329b856881461004d575b600080fd5b6100836004803603604081101561006357600080fd5b810190808035906020019092919080359060200190929190505050610085565b005b6000606060405190810160405280848152602001838152602001600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681525090806001815401808255809150509060018203906000526020600020906003020160009091929091909150600082015181600001556020820151816001015560408201518160020160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505050503373ffffffffffffffffffffffffffffffffffffffff16818360405160405180910390a3505056fea165627a7a72305820b762f7e2a9d04b5fbe6c2437c0471b855ad19c17c4a3fedb2cbe7d74a9d79cca0029";

            public TestAnonymousEventContractDeployment() : base(BYTECODE)
            {
            }

            public TestAnonymousEventContractDeployment(string byteCode) : base(byteCode)
            {
            }
        }

        [Event("ItemCreated", true)]
        public class ItemCreatedEventDTO : IEventDTO
        {
            [Parameter("uint256", "itemId", 1, true)]
            public BigInteger ItemId { get; set; }

            [Parameter("uint256", "price", 2, true)]
            public BigInteger Price { get; set; }

            [Parameter("address", "result", 3, true)]
            public string Result { get; set; }
        }

        [Function("newItem")]
        public class NewItemFunction : FunctionMessage
        {
            [Parameter("uint256", "id", 1)] public BigInteger Id { get; set; }

            [Parameter("uint256", "price", 2)] public BigInteger Price { get; set; }
        }

/* Contract 
contract TestAnonymousEventContract {
    struct Item {
        uint id;
        uint price;
        address manager;
    }

    Item[] items;
    address manager;

    constructor() public {
        manager = msg.sender;
    }
    
    event ItemCreated(uint indexed itemId, uint indexed price, address indexed result) anonymous;

    function newItem(uint id, uint price) public {
        items.push(Item(id, price, manager));
        emit ItemCreated(id, price, msg.sender);
    }
}
*/
    }
}﻿using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Blocks;
using Nethereum.XUnitEthereumClients;
using Xunit;
using System.Linq;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class BatchTests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public BatchTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }
        
        [Fact]
        public async void ShouldBatchGetBalances()
        {

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var batchRequest = new RpcRequestResponseBatch();
            var batchItem1 = new RpcRequestResponseBatchItem<EthGetBalance, HexBigInteger>((EthGetBalance)web3.Eth.GetBalance, web3.Eth.GetBalance.BuildRequest(EthereumClientIntegrationFixture.AccountAddress, BlockParameter.CreateLatest(), 1));
            var batchItem2 = new RpcRequestResponseBatchItem<EthGetBalance, HexBigInteger>((EthGetBalance)web3.Eth.GetBalance, web3.Eth.GetBalance.BuildRequest(EthereumClientIntegrationFixture.AccountAddress, BlockParameter.CreateLatest(), 2));
            batchRequest.BatchItems.Add(batchItem1);
            batchRequest.BatchItems.Add(batchItem2);
            var response = await web3.Client.SendBatchRequestAsync(batchRequest);
            Assert.Equal(batchItem1.Response.Value, batchItem2.Response.Value);
        }

        [Fact]
        public async void ShouldBatchGetBalancesRpc()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var balances = await web3.Eth.GetBalance.SendBatchRequestAsync(EthereumClientIntegrationFixture.AccountAddress, EthereumClientIntegrationFixture.AccountAddress);
            Assert.Equal(balances[0], balances[1]);
        }

        [Fact]
        public async void ShouldBatchGetBlocksWithTransactionHashesRpc()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var blocks = await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendBatchRequestAsync(
                new HexBigInteger(1000000), new HexBigInteger(1000001), new HexBigInteger(1000002));
            Assert.Equal(3, blocks.Count);
        }

        [Fact]
        public async void ShouldBatchGetBlocksWithTransactionsRpc()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var blocks = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendBatchRequestAsync(
                new HexBigInteger(1000000), new HexBigInteger(1000001), new HexBigInteger(1000002));
            Assert.Equal(3, blocks.Count);
        }

        [Fact]
        public async void ShouldBatchGetTransactionReceiptsRpc()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(
                new HexBigInteger(1000000));
            var receipts = await web3.Eth.Transactions.GetTransactionReceipt.SendBatchRequestAsync(block.Transactions.Select(x => x.TransactionHash).ToArray());
            Assert.Equal(2, receipts.Count);
        }

        [Fact]
        public async void ShouldBatchGetBlocks()
        {

            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var batchRequest = new RpcRequestResponseBatch();

            for (int i = 0; i < 10; i++)
            {

                var batchItem1 = new RpcRequestResponseBatchItem<EthGetBlockWithTransactionsHashesByNumber, BlockWithTransactionHashes>((EthGetBlockWithTransactionsHashesByNumber)web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber, web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.BuildRequest(new BlockParameter(new HexBigInteger(i)), i));
                batchRequest.BatchItems.Add(batchItem1);
            }
            var response = await web3.Client.SendBatchRequestAsync(batchRequest);
            Assert.Equal(1438270115, ((RpcRequestResponseBatchItem<EthGetBlockWithTransactionsHashesByNumber, BlockWithTransactionHashes>)response.BatchItems[9]).Response.Timestamp.Value);
        }
    }
}﻿using System.Linq;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Model;
using Nethereum.RPC.ModelFactories;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    //TODO:This needs to be moved to a custom testing library
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class BlockHeaderIntegrationTests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public BlockHeaderIntegrationTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        //[Fact]
        //public async void ShouldDecodeCliqueAuthor()
        //{
     
        //    var web3 = _ethereumClientIntegrationFixture.GetWeb3();
        //    var block =
        //        await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(1)).ConfigureAwait(false);
        //    var blockHeader = BlockHeaderRPCFactory.FromRPC(block, true);
        //    var account = new CliqueBlockHeaderRecovery().RecoverCliqueSigner(blockHeader, false);
        //    Assert.True(EthereumClientIntegrationFixture.AccountAddress.IsTheSameAddress(account));

        //}

        //[Fact]
        //public async void ShouldDecodeGoerliCliqueAuthor()
        //{

        //    var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Goerli);
        //    var block =
        //        await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(5514521)).ConfigureAwait(false);
        //    var blockHeader = BlockHeaderRPCFactory.FromRPC(block, true);
        //    var account = new CliqueBlockHeaderRecovery().RecoverCliqueSigner(blockHeader, false);
        //    Assert.True("0x000000568b9b5a365eaa767d42e74ed88915c204".IsTheSameAddress(account));

        //}

        


        [Fact]
        public async void ShouldEncodeDecode()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var block =
                    await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(1)).ConfigureAwait(false);
            var blockHeader = BlockHeaderRPCFactory.FromRPC(block);

            var encoded = BlockHeaderEncoder.Current.Encode(blockHeader);
            var decoded = BlockHeaderEncoder.Current.Decode(encoded);

            Assert.Equal(blockHeader.StateRoot.ToHex(), decoded.StateRoot.ToHex());
            Assert.Equal(blockHeader.LogsBloom.ToHex(), decoded.LogsBloom.ToHex());
            Assert.Equal(blockHeader.MixHash.ToHex(), decoded.MixHash.ToHex());
            Assert.Equal(blockHeader.ReceiptHash.ToHex(), decoded.ReceiptHash.ToHex());
            Assert.Equal(blockHeader.Difficulty, decoded.Difficulty);
            Assert.Equal(blockHeader.BaseFee, decoded.BaseFee);
        }

    }
}﻿using System.Collections.Generic;
using Nethereum.Hex.HexTypes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EncodingInputOutput
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class Bytes1FixedArraySupport
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public Bytes1FixedArraySupport(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        /*
         contract TestBytes1Array {
        byte[50] public testArray;
    
        function initTestArray(){
            uint i = 0;
            while (i < 50) {
                testArray[i] = 0x01;
                i++;
            }
        }
    
         function getTestArray() constant  returns (byte[50]) {
            return testArray;
        }

        function initTestArrayExternally(byte[50] array){
            testArray = array;
        }
    }
*/

        //NOTE the contract above is very old (byte does not exist anymore)
        [Fact]
        public async void ShouldEncodeDecodeAnArrayOfBytes1ToASingleArray()
        {
            var ABI =
@"function initTestArray()
function getTestArray() constant  returns (bytes1[50])
function initTestArrayExternally(bytes1[50] array)
";
            var BYTE_CODE =
                "0x6060604052341561000f57600080fd5b5b6103738061001f6000396000f300606060405263ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166304640132811461005e578063463f5c30146100ab578063c4dd6a55146100f6578063e14f4e2714610134575b600080fd5b341561006957600080fd5b610071610149565b604051808261064080838360005b838110156100985780820151818401525b60200161007f565b5050505090500191505060405180910390f35b34156100b657600080fd5b6100c16004356101d1565b6040517fff00000000000000000000000000000000000000000000000000000000000000909116815260200160405180910390f35b341561010157600080fd5b61013260046106448160326106406040519081016040529190828261064080828437509395506101fc945050505050565b005b341561013f57600080fd5b61013261020e565b005b610151610262565b6000603261064060405190810160405291906106408301826000855b82829054906101000a900460f860020a027effffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff19168152602001906001019060208260000104928301926001038202915080841161016d5790505b505050505090505b90565b600081603281106101de57fe5b602091828204019190065b915054906101000a900460f860020a0281565b610209600082603261028b565b505b50565b60005b603281101561020b5760f860020a6000826032811061022c57fe5b602091828204019190065b6101000a81548160ff021916908360f860020a900402179055508080600101915050610211565b5b50565b6106406040519081016040526032815b6000815260001990910190602001816102725790505090565b6002830191839082156103125791602002820160005b838211156102e357835183826101000a81548160ff021916908360f860020a9004021790555092602001926001016020816000010492830192600103026102a1565b80156103105782816101000a81549060ff02191690556001016020816000010492830192600103026102e3565b505b5061031e929150610322565b5090565b6101ce91905b8082111561031e57805460ff19168155600101610328565b5090565b905600a165627a7a72305820369a31471cb54acda1cc23c0c6df419a6ed121f46f2845e30057c758b8249c4d0029";
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(BYTE_CODE, senderAddress,
                    new HexBigInteger(900000), null);
            var contract = web3.Eth.GetContract(ABI, receipt.ContractAddress);

            var function = contract.GetFunction("getTestArray");
            var result = await function.CallAsync<List<byte>>();

            for (var i = 0; i < 50; i++)
                Assert.Equal(0, result[i]);

            var listByteArray = new List<byte>();

            for (var i = 0; i < 50; i++)
                listByteArray.Add(1);


            var functionInit = contract.GetFunction("initTestArrayExternally");
            receipt = await functionInit.SendTransactionAndWaitForReceiptAsync(senderAddress, new HexBigInteger(900000),
                null, null, listByteArray);

            result = await function.CallAsync<List<byte>>();

            for (var i = 0; i < 50; i++)
                Assert.Equal(1, result[i]);
        }


        [Fact]
        public async void ShouldEncodeDecodeAnArrayOfBytes1ToASingleArrayUsingStringSignatures()
        {
            var ABI =
                @"[{'constant':true,'inputs':[],'name':'getTestArray','outputs':[{'name':'','type':'bytes1[50]'}],'payable':false,'type':'function'},{'constant':true,'inputs':[{'name':'','type':'uint256'}],'name':'testArray','outputs':[{'name':'','type':'bytes1'}],'payable':false,'type':'function'},{'constant':false,'inputs':[{'name':'array','type':'bytes1[50]'}],'name':'initTestArrayExternally','outputs':[],'payable':false,'type':'function'},{'constant':false,'inputs':[],'name':'initTestArray','outputs':[],'payable':false,'type':'function'}]";
            var BYTE_CODE =
                "0x6060604052341561000f57600080fd5b5b6103738061001f6000396000f300606060405263ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166304640132811461005e578063463f5c30146100ab578063c4dd6a55146100f6578063e14f4e2714610134575b600080fd5b341561006957600080fd5b610071610149565b604051808261064080838360005b838110156100985780820151818401525b60200161007f565b5050505090500191505060405180910390f35b34156100b657600080fd5b6100c16004356101d1565b6040517fff00000000000000000000000000000000000000000000000000000000000000909116815260200160405180910390f35b341561010157600080fd5b61013260046106448160326106406040519081016040529190828261064080828437509395506101fc945050505050565b005b341561013f57600080fd5b61013261020e565b005b610151610262565b6000603261064060405190810160405291906106408301826000855b82829054906101000a900460f860020a027effffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff19168152602001906001019060208260000104928301926001038202915080841161016d5790505b505050505090505b90565b600081603281106101de57fe5b602091828204019190065b915054906101000a900460f860020a0281565b610209600082603261028b565b505b50565b60005b603281101561020b5760f860020a6000826032811061022c57fe5b602091828204019190065b6101000a81548160ff021916908360f860020a900402179055508080600101915050610211565b5b50565b6106406040519081016040526032815b6000815260001990910190602001816102725790505090565b6002830191839082156103125791602002820160005b838211156102e357835183826101000a81548160ff021916908360f860020a9004021790555092602001926001016020816000010492830192600103026102a1565b80156103105782816101000a81549060ff02191690556001016020816000010492830192600103026102e3565b505b5061031e929150610322565b5090565b6101ce91905b8082111561031e57805460ff19168155600101610328565b5090565b905600a165627a7a72305820369a31471cb54acda1cc23c0c6df419a6ed121f46f2845e30057c758b8249c4d0029";
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(BYTE_CODE, senderAddress,
                    new HexBigInteger(900000), null);
            var contract = web3.Eth.GetContract(ABI, receipt.ContractAddress);

            var function = contract.GetFunction("getTestArray");
            var result = await function.CallAsync<List<byte>>();

            for (var i = 0; i < 50; i++)
                Assert.Equal(0, result[i]);

            var listByteArray = new List<byte>();

            for (var i = 0; i < 50; i++)
                listByteArray.Add(1);


            var functionInit = contract.GetFunction("initTestArrayExternally");
            receipt = await functionInit.SendTransactionAndWaitForReceiptAsync(senderAddress, new HexBigInteger(900000),
                null, null, listByteArray);

            result = await function.CallAsync<List<byte>>();

            for (var i = 0; i < 50; i++)
                Assert.Equal(1, result[i]);
        }
    }
}﻿using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.SmartContracts
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class CallStateFromPreviousBlock
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public CallStateFromPreviousBlock(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldTransferAndGetStateFromPreviousBlock()
        {
            var contractByteCode =
                "0x60606040526040516020806106f5833981016040528080519060200190919050505b80600160005060003373ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005081905550806000600050819055505b506106868061006f6000396000f360606040523615610074576000357c010000000000000000000000000000000000000000000000000000000090048063095ea7b31461008157806318160ddd146100b657806323b872dd146100d957806370a0823114610117578063a9059cbb14610143578063dd62ed3e1461017857610074565b61007f5b610002565b565b005b6100a060048080359060200190919080359060200190919050506101ad565b6040518082815260200191505060405180910390f35b6100c36004805050610674565b6040518082815260200191505060405180910390f35b6101016004808035906020019091908035906020019091908035906020019091905050610281565b6040518082815260200191505060405180910390f35b61012d600480803590602001909190505061048d565b6040518082815260200191505060405180910390f35b61016260048080359060200190919080359060200190919050506104cb565b6040518082815260200191505060405180910390f35b610197600480803590602001909190803590602001909190505061060b565b6040518082815260200191505060405180910390f35b600081600260005060003373ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005060008573ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925846040518082815260200191505060405180910390a36001905061027b565b92915050565b600081600160005060008673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600050541015801561031b575081600260005060008673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005060003373ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000505410155b80156103275750600082115b1561047c5781600160005060008573ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828282505401925050819055508273ffffffffffffffffffffffffffffffffffffffff168473ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040518082815260200191505060405180910390a381600160005060008673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008282825054039250508190555081600260005060008673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005060003373ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828282505403925050819055506001905061048656610485565b60009050610486565b5b9392505050565b6000600160005060008373ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000505490506104c6565b919050565b600081600160005060003373ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600050541015801561050c5750600082115b156105fb5781600160005060003373ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008282825054039250508190555081600160005060008573ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828282505401925050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040518082815260200191505060405180910390a36001905061060556610604565b60009050610605565b5b92915050565b6000600260005060008473ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005060008373ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005054905061066e565b92915050565b60006000600050549050610683565b9056";
            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""totalSupply"",""outputs"":[{""name"":""supply"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_from"",""type"":""address""},{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transferFrom"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""},{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":""balance"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""},{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""},{""name"":""_spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":""remaining"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""_initialAmount"",""type"":""uint256""}],""type"":""constructor""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""_from"",""type"":""address""},{""indexed"":true,""name"":""_to"",""type"":""address""},{""indexed"":false,""name"":""_value"",""type"":""uint256""}],""name"":""Transfer"",""type"":""event""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""_owner"",""type"":""address""},{""indexed"":true,""name"":""_spender"",""type"":""address""},{""indexed"":false,""name"":""_value"",""type"":""uint256""}],""name"":""Approval"",""type"":""event""}]";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            ulong totalSupply = 1000000;
            var address = EthereumClientIntegrationFixture.AccountAddress;
            var newAddress = "0x12890d2cce102216644c59dae5baed380d848301";

            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, contractByteCode,
                address, new HexBigInteger(900000), null, null, null, totalSupply).ConfigureAwait(false);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);
            var transferFunction = contract.GetFunction("transfer");
            var balanceFunction = contract.GetFunction("balanceOf");

            var gas = await transferFunction.EstimateGasAsync(address, null, null, newAddress, 1000).ConfigureAwait(false);
            var receiptFirstBlock =
                await transferFunction.SendTransactionAndWaitForReceiptAsync(address, gas, null, null, newAddress,
                    1000).ConfigureAwait(false);
            var balanceFirstBlock = await balanceFunction.CallAsync<int>(newAddress).ConfigureAwait(false);
            var receiptSecondBlock =
                await transferFunction.SendTransactionAndWaitForReceiptAsync(address, gas, null, null, newAddress,
                    1000).ConfigureAwait(false);
            var balanceSecondBlock = await balanceFunction.CallAsync<int>(newAddress).ConfigureAwait(false);
            var balanceOldBlock =
                await
                    balanceFunction.CallAsync<int>(
                        new BlockParameter(receiptFirstBlock.BlockNumber), newAddress).ConfigureAwait(false);

            Assert.Equal(2000, balanceSecondBlock);
            Assert.Equal(1000, balanceOldBlock);
            Assert.Equal(1000, balanceFirstBlock);
        }
    }
}﻿using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Contracts.IntegrationTests.Patricia
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ChainProofValidationTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ChainProofValidationTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldValidateBalanceOfEOA()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
           
            var accountProof = await web3.Eth.ChainProofValidation.GetAndValidateAccountProof("0xde0b295669a9fd93d5f28d9ec85e40f4cb697bae", null, null, new BlockParameter(16483161));
            Assert.NotNull(accountProof);
        }



        [Fact]
        public async void ShouldValidateStorage()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
   
            var value = await web3.Eth.ChainProofValidation.GetAndValidateValueFromStorage("0x5E4e65926BA27467555EB562121fac00D24E9dD2", "0x0", null, new BlockParameter(16648900));
            var libAddressManager = "0xdE1FCfB0851916CA5101820A69b13a4E276bd81F";
            //de1fcfb0851916ca5101820a69b13a4e276bd81f
            //libAddressManager is at slot 0 as the contract inherits from Lib_AddressResolver
            Assert.True(value.ToHex().IsTheSameHex(libAddressManager));
        }

        [Fact]
        public async void ShouldValidateTransactions()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var transactions = await web3.Eth.ChainProofValidation.GetAndValidateTransactions(new BlockParameter(16503723));
            Assert.Equal(103, transactions.Length);

        }

       
    }
}
using System.Threading;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting
// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.Deployment
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ContractConstructorDeploymentAndCall
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ContractConstructorDeploymentAndCall(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDeployAContractWithConstructor()
        {
            //The compiled solidity contract to be deployed
            /*
               contract test { 

               uint _multiplier;

               function test(uint multiplier){
                   _multiplier = multiplier;
               }

               function getMultiplier() constant returns(uint d){
                    return _multiplier;
               }

               function multiply(uint a) returns(uint d) { return a * _multiplier; }

               string public contractName = "Multiplier";
           }
           */

            var contractByteCode =
                "0x6060604052604060405190810160405280600a81526020017f4d756c7469706c6965720000000000000000000000000000000000000000000081526020015060016000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061008c57805160ff19168380011785556100bd565b828001600101855582156100bd579182015b828111156100bc57825182600050559160200191906001019061009e565b5b5090506100e891906100ca565b808211156100e457600081815060009055506001016100ca565b5090565b5050604051602080610303833981016040528080519060200190919050505b806000600050819055505b506101e2806101216000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806340490a901461004f57806375d0c0dc14610072578063c6888fa1146100ed5761004d565b005b61005c6004805050610119565b6040518082815260200191505060405180910390f35b61007f6004805050610141565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600f02600301f150905090810190601f1680156100df5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b610103600480803590602001909190505061012b565b6040518082815260200191505060405180910390f35b60006000600050549050610128565b90565b60006000600050548202905061013c565b919050565b60016000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156101da5780601f106101af576101008083540402835291602001916101da565b820191906000526020600020905b8154815290600101906020018083116101bd57829003601f168201915b50505050508156";

            var abi =
                @"[{""constant"":true,""inputs"":[],""name"":""getMultiplier"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""contractName"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""}]";

            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            //deploy the contract, including abi and a paramter of 7. 
            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(abi, contractByteCode, senderAddress,
                new HexBigInteger(900000), 7);

            Assert.NotNull(transactionHash);

            //get the contract address 
            TransactionReceipt receipt = null;
            //wait for the contract to be mined to the address
            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            Assert.NotNull(receipt.ContractAddress);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            //get the function by name
            var multiplyFunction = contract.GetFunction("multiply");

            //do a function call (not transaction) and get the result
            var callResult = await multiplyFunction.CallAsync<int>(69);
            Assert.Equal(483, callResult);

            var multiplierFunction = contract.GetFunction("getMultiplier");

            var multiplier = await multiplierFunction.CallAsync<int>();

            Assert.Equal(7, multiplier);

            var contractNameFunction = contract.GetFunction("contractName");

            var name = await contractNameFunction.CallAsync<string>();

            Assert.Equal("Multiplier", name);
        }


        [Fact]
        public async void ShouldDeployAContractWithConstructorProvidingGasPrice()
        {
            //The compiled solidity contract to be deployed
            /*
               contract test { 

               uint _multiplier;

               function test(uint multiplier){
                   _multiplier = multiplier;
               }

               function getMultiplier() constant returns(uint d){
                    return _multiplier;
               }

               function multiply(uint a) returns(uint d) { return a * _multiplier; }

               string public contractName = "Multiplier";
           }
           */

            var contractByteCode =
                "0x6060604052604060405190810160405280600a81526020017f4d756c7469706c6965720000000000000000000000000000000000000000000081526020015060016000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061008c57805160ff19168380011785556100bd565b828001600101855582156100bd579182015b828111156100bc57825182600050559160200191906001019061009e565b5b5090506100e891906100ca565b808211156100e457600081815060009055506001016100ca565b5090565b5050604051602080610303833981016040528080519060200190919050505b806000600050819055505b506101e2806101216000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806340490a901461004f57806375d0c0dc14610072578063c6888fa1146100ed5761004d565b005b61005c6004805050610119565b6040518082815260200191505060405180910390f35b61007f6004805050610141565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600f02600301f150905090810190601f1680156100df5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b610103600480803590602001909190505061012b565b6040518082815260200191505060405180910390f35b60006000600050549050610128565b90565b60006000600050548202905061013c565b919050565b60016000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156101da5780601f106101af576101008083540402835291602001916101da565b820191906000526020600020905b8154815290600101906020018083116101bd57829003601f168201915b50505050508156";

            var abi =
                @"[{""constant"":true,""inputs"":[],""name"":""getMultiplier"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""contractName"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""}]";


            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture
                .GetWeb3(); //deploy the contract, including abi and a paramter of 7. 

            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(abi, contractByteCode, senderAddress,
                new HexBigInteger(900000), new HexBigInteger(1000), new HexBigInteger(0), 7);

            Assert.NotNull(transactionHash);

            //get the contract address 
            TransactionReceipt receipt = null;
            //wait for the contract to be mined to the address
            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            Assert.NotNull(receipt.ContractAddress);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            //get the function by name
            var multiplyFunction = contract.GetFunction("multiply");

            //do a function call (not transaction) and get the result
            var callResult = await multiplyFunction.CallAsync<int>(69);
            Assert.Equal(483, callResult);

            var multiplierFunction = contract.GetFunction("getMultiplier");

            var multiplier = await multiplierFunction.CallAsync<int>();

            Assert.Equal(7, multiplier);

            var contractNameFunction = contract.GetFunction("contractName");

            var name = await contractNameFunction.CallAsync<string>();

            Assert.Equal("Multiplier", name);
        }


        [Fact]
        public async void ShouldDeployAContractWithConstructorProvidingFees()
        {
            //The compiled solidity contract to be deployed
            /*
               contract test { 

               uint _multiplier;

               function test(uint multiplier){
                   _multiplier = multiplier;
               }

               function getMultiplier() constant returns(uint d){
                    return _multiplier;
               }

               function multiply(uint a) returns(uint d) { return a * _multiplier; }

               string public contractName = "Multiplier";
           }
           */

            var contractByteCode =
                "0x6060604052604060405190810160405280600a81526020017f4d756c7469706c6965720000000000000000000000000000000000000000000081526020015060016000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061008c57805160ff19168380011785556100bd565b828001600101855582156100bd579182015b828111156100bc57825182600050559160200191906001019061009e565b5b5090506100e891906100ca565b808211156100e457600081815060009055506001016100ca565b5090565b5050604051602080610303833981016040528080519060200190919050505b806000600050819055505b506101e2806101216000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806340490a901461004f57806375d0c0dc14610072578063c6888fa1146100ed5761004d565b005b61005c6004805050610119565b6040518082815260200191505060405180910390f35b61007f6004805050610141565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600f02600301f150905090810190601f1680156100df5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b610103600480803590602001909190505061012b565b6040518082815260200191505060405180910390f35b60006000600050549050610128565b90565b60006000600050548202905061013c565b919050565b60016000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156101da5780601f106101af576101008083540402835291602001916101da565b820191906000526020600020905b8154815290600101906020018083116101bd57829003601f168201915b50505050508156";

            var abi =
                @"[{""constant"":true,""inputs"":[],""name"":""getMultiplier"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""contractName"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""}]";


            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture
                .GetWeb3(); //deploy the contract, including abi and a paramter of 7. 

            var feeEstimate = web3.FeeSuggestion.GetTimePreferenceFeeSuggestionStrategy();

            var fees = await feeEstimate.SuggestFeesAsync();

            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(
                abi: abi,
                contractByteCode: contractByteCode,
                from: senderAddress,
                gas: new HexBigInteger(900000),
                maxFeePerGas: fees[0].MaxFeePerGas.Value.ToHexBigInteger(),
                maxPriorityFeePerGas: fees[0].MaxPriorityFeePerGas.Value.ToHexBigInteger(),
                value: new HexBigInteger(1),
                nonce: null,
                7);


            Assert.NotNull(transactionHash);

            var transaction = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash);
            Assert.True(transaction.From.IsTheSameAddress(senderAddress));
            Assert.Equal(900000, transaction.Gas.Value);
            Assert.Equal(1, transaction.Value.Value);
            Assert.Equal(fees[0].MaxFeePerGas, transaction.MaxFeePerGas);
            Assert.Equal(fees[0].MaxPriorityFeePerGas, transaction.MaxPriorityFeePerGas);


            //get the contract address 
            TransactionReceipt receipt = null;
            //wait for the contract to be mined to the address
            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            Assert.NotNull(receipt.ContractAddress);


            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            //get the function by name
            var multiplyFunction = contract.GetFunction("multiply");

            //do a function call (not transaction) and get the result
            var callResult = await multiplyFunction.CallAsync<int>(69);
            Assert.Equal(483, callResult);

            var multiplierFunction = contract.GetFunction("getMultiplier");

            var multiplier = await multiplierFunction.CallAsync<int>();

            Assert.Equal(7, multiplier);

            var contractNameFunction = contract.GetFunction("contractName");

            var name = await contractNameFunction.CallAsync<string>();

            Assert.Equal("Multiplier", name);
        }

        [Fact]
        public async void ShouldEstimateContactDeployment()
        {
            var contractByteCode =
                "0x6060604052604060405190810160405280600a81526020017f4d756c7469706c6965720000000000000000000000000000000000000000000081526020015060016000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061008c57805160ff19168380011785556100bd565b828001600101855582156100bd579182015b828111156100bc57825182600050559160200191906001019061009e565b5b5090506100e891906100ca565b808211156100e457600081815060009055506001016100ca565b5090565b5050604051602080610303833981016040528080519060200190919050505b806000600050819055505b506101e2806101216000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806340490a901461004f57806375d0c0dc14610072578063c6888fa1146100ed5761004d565b005b61005c6004805050610119565b6040518082815260200191505060405180910390f35b61007f6004805050610141565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600f02600301f150905090810190601f1680156100df5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b610103600480803590602001909190505061012b565b6040518082815260200191505060405180910390f35b60006000600050549050610128565b90565b60006000600050548202905061013c565b919050565b60016000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156101da5780601f106101af576101008083540402835291602001916101da565b820191906000526020600020905b8154815290600101906020018083116101bd57829003601f168201915b50505050508156";

            var abi =
                @"[{""constant"":true,""inputs"":[],""name"":""getMultiplier"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""contractName"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""}]";

            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var estimate = await web3.Eth.DeployContract.EstimateGasAsync(abi, contractByteCode, senderAddress, 7);

            Assert.NotNull(estimate);
        }
    }
}using Nethereum.Hex.HexTypes;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.Deployment
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ContractDeploymentAndCall
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ContractDeploymentAndCall(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDeployAContractAndPerformACall()
        {
            //The compiled solidity contract to be deployed
            //contract test { function multiply(uint a) returns(uint d) { return a * 7; } }
            var contractByteCode =
                "0x606060405260728060106000396000f360606040526000357c010000000000000000000000000000000000000000000000000000000090048063c6888fa1146037576035565b005b604b60048080359060200190919050506061565b6040518082815260200191505060405180910390f35b6000600782029050606d565b91905056";

            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""}]";

            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(contractByteCode,
                senderAddress, new HexBigInteger(900000), null, null, null);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            //get the function by name
            var multiplyFunction = contract.GetFunction("multiply");
            //do a function call (not transaction) and get the result
            var callResult = await multiplyFunction.CallAsync<int>(69);
            Assert.Equal(483, callResult);
        }


        [Fact]
        public async void ShouldDeployAContractWithValueAndSendAValue()
        {
            var contractByteCode =
                "0x6060604052600180546c0100000000000000000000000033810204600160a060020a03199091161790556002340460008190556002023414603e576002565b6103258061004c6000396000f3606060405236156100615760e060020a600035046308551a53811461006657806335a063b41461007d5780633fa4f245146100a05780637150d8ae146100ae57806373fac6f0146100c5578063c19d93fb146100e8578063d696069714610101575b610002565b346100025761011f600154600160a060020a031681565b346100025761013b60015433600160a060020a0390811691161461014f57610002565b346100025761013d60005481565b346100025761011f600254600160a060020a031681565b346100025761013b60025433600160a060020a039081169116146101e457610002565b346100025761013d60025460ff60a060020a9091041681565b61013b60025460009060ff60a060020a90910416156102a457610002565b60408051600160a060020a039092168252519081900360200190f35b005b60408051918252519081900360200190f35b60025460009060a060020a900460ff161561016957610002565b6040517f80b62b7017bb13cf105e22749ee2a06a417ffba8c7f57b665057e0f3c2e925d990600090a16040516002805460a160020a60a060020a60ff0219909116179055600154600160a060020a0390811691309091163180156108fc02916000818181858888f1935050505015156101e157610002565b50565b60025460019060a060020a900460ff1681146101ff57610002565b6040517f64ea507aa320f07ae13c28b5e9bf6b4833ab544315f5f2aa67308e21c252d47d90600090a16040516002805460a060020a60ff02191660a160020a179081905560008054600160a060020a03909216926108fc8315029291818181858888f19350505050158061029a5750600154604051600160a060020a039182169130163180156108fc02916000818181858888f19350505050155b156101e157610002565b6000546002023414806102b657610002565b6040517f764326667cab2f2f13cad5f7b7665c704653bd1acc250dcb7b422bce726896b490600090a150506002805460a060020a73ffffffffffffffffffffffffffffffffffffffff199091166c01000000000000000000000000338102041760a060020a60ff02191617905556";

            var abi =
                "[{'constant':true,'inputs':[],'name':'seller','outputs':[{'name':'','type':'address'}],'payable':false,'type':'function'},{'constant':false,'inputs':[],'name':'abort','outputs':[],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'value','outputs':[{'name':'','type':'uint256'}],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'buyer','outputs':[{'name':'','type':'address'}],'payable':false,'type':'function'},{'constant':false,'inputs':[],'name':'confirmReceived','outputs':[],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'state','outputs':[{'name':'','type':'uint8'}],'payable':false,'type':'function'},{'constant':false,'inputs':[],'name':'confirmPurchase','outputs':[],'payable':true,'type':'function'},{'inputs':[],'type':'constructor'},{'anonymous':false,'inputs':[],'name':'aborted','type':'event'},{'anonymous':false,'inputs':[],'name':'purchaseConfirmed','type':'event'},{'anonymous':false,'inputs':[],'name':'itemReceived','type':'event'}]";

            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var transaction =
                await
                    web3.Eth.DeployContract.SendRequestAsync(abi, contractByteCode,
                        senderAddress, new HexBigInteger(900000), new HexBigInteger(10000));

            var pollingService = new TransactionReceiptPollingService(web3.TransactionManager);
            var receipt = await pollingService.PollForReceiptAsync(transaction);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            //get the function by name
            var valueFuntion = contract.GetFunction("value");

            //do a function call (not transaction) and get the result
            var callResult = await valueFuntion.CallAsync<int>();
            Assert.Equal(5000, callResult);

            var confirmPurchaseFunction = contract.GetFunction("confirmPurchase");
            var tx = await confirmPurchaseFunction.SendTransactionAsync(senderAddress,
                new HexBigInteger(900000), new HexBigInteger(10000));

            receipt = await pollingService.PollForReceiptAsync(tx);

            var stateFunction = contract.GetFunction("state");
            callResult = await stateFunction.CallAsync<int>();
            Assert.Equal(1, callResult);
        }

        /*
          * pragma solidity ^0.4.0;

contract Purchase {
    uint public value;
    address public seller;
    address public buyer;
    enum State { Created, Locked, Inactive }
    State public state;

    function Purchase() payable {
        seller = msg.sender;
        value = msg.value / 2;
        if (2 * value != msg.value) throw;
    }

    modifier require(bool _condition) {
        if (!_condition) throw;
        _;
    }

    modifier onlyBuyer() {
        if (msg.sender != buyer) throw;
        _;
    }

    modifier onlySeller() {
        if (msg.sender != seller) throw;
        _;
    }

    modifier inState(State _state) {
        if (state != _state) throw;
        _;
    }

    event aborted();
    event purchaseConfirmed();
    event itemReceived();

    /// Abort the purchase and reclaim the ether.
    /// Can only be called by the seller before
    /// the contract is locked.
    function abort()
        onlySeller
        inState(State.Created)
    {
        aborted();
        state = State.Inactive;
        if (!seller.send(this.balance))
            throw;
    }

    /// Confirm the purchase as buyer.
    /// Transaction has to include `2 * value` ether.
    /// The ether will be locked until confirmReceived
    /// is called.
    function confirmPurchase()
        inState(State.Created)
        require(msg.value == 2 * value)
        payable
    {
        purchaseConfirmed();
        buyer = msg.sender;
        state = State.Locked;
    }

    /// Confirm that you (the buyer) received the item.
    /// This will release the locked ether.
    function confirmReceived()
        onlyBuyer
        inState(State.Locked)
    {
        itemReceived();
        // It is important to change the state first because
        // otherwise, the contracts called using `send` below
        // can call in again here.
        state = State.Inactive;
        // This actually allows both the buyer and the seller to
        // block the refund.
        if (!buyer.send(value) || !seller.send(this.balance))
            throw;
    }
}*/

        [Fact]
        public async void ShouldDeployAContractWithValueAndSendAValueUsingSignAndSend()
        {
            var contractByteCode =
                "0x6060604052600180546c0100000000000000000000000033810204600160a060020a03199091161790556002340460008190556002023414603e576002565b6103258061004c6000396000f3606060405236156100615760e060020a600035046308551a53811461006657806335a063b41461007d5780633fa4f245146100a05780637150d8ae146100ae57806373fac6f0146100c5578063c19d93fb146100e8578063d696069714610101575b610002565b346100025761011f600154600160a060020a031681565b346100025761013b60015433600160a060020a0390811691161461014f57610002565b346100025761013d60005481565b346100025761011f600254600160a060020a031681565b346100025761013b60025433600160a060020a039081169116146101e457610002565b346100025761013d60025460ff60a060020a9091041681565b61013b60025460009060ff60a060020a90910416156102a457610002565b60408051600160a060020a039092168252519081900360200190f35b005b60408051918252519081900360200190f35b60025460009060a060020a900460ff161561016957610002565b6040517f80b62b7017bb13cf105e22749ee2a06a417ffba8c7f57b665057e0f3c2e925d990600090a16040516002805460a160020a60a060020a60ff0219909116179055600154600160a060020a0390811691309091163180156108fc02916000818181858888f1935050505015156101e157610002565b50565b60025460019060a060020a900460ff1681146101ff57610002565b6040517f64ea507aa320f07ae13c28b5e9bf6b4833ab544315f5f2aa67308e21c252d47d90600090a16040516002805460a060020a60ff02191660a160020a179081905560008054600160a060020a03909216926108fc8315029291818181858888f19350505050158061029a5750600154604051600160a060020a039182169130163180156108fc02916000818181858888f19350505050155b156101e157610002565b6000546002023414806102b657610002565b6040517f764326667cab2f2f13cad5f7b7665c704653bd1acc250dcb7b422bce726896b490600090a150506002805460a060020a73ffffffffffffffffffffffffffffffffffffffff199091166c01000000000000000000000000338102041760a060020a60ff02191617905556";

            var abi =
                "[{'constant':true,'inputs':[],'name':'seller','outputs':[{'name':'','type':'address'}],'payable':false,'type':'function'},{'constant':false,'inputs':[],'name':'abort','outputs':[],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'value','outputs':[{'name':'','type':'uint256'}],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'buyer','outputs':[{'name':'','type':'address'}],'payable':false,'type':'function'},{'constant':false,'inputs':[],'name':'confirmReceived','outputs':[],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'state','outputs':[{'name':'','type':'uint8'}],'payable':false,'type':'function'},{'constant':false,'inputs':[],'name':'confirmPurchase','outputs':[],'payable':true,'type':'function'},{'inputs':[],'type':'constructor'},{'anonymous':false,'inputs':[],'name':'aborted','type':'event'},{'anonymous':false,'inputs':[],'name':'purchaseConfirmed','type':'event'},{'anonymous':false,'inputs':[],'name':'itemReceived','type':'event'}]";


            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var transaction =
                await
                    web3.Eth.DeployContract.SendRequestAsync(abi, contractByteCode,
                        senderAddress, new HexBigInteger(900000), new HexBigInteger(10000));
            var pollingService = new TransactionReceiptPollingService(web3.TransactionManager);
            var receipt = await pollingService.PollForReceiptAsync(transaction);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            //get the function by name
            var valueFuntion = contract.GetFunction("value");

            //do a function call (not transaction) and get the result
            var callResult = await valueFuntion.CallAsync<int>();
            Assert.Equal(5000, callResult);

            var confirmPurchaseFunction = contract.GetFunction("confirmPurchase");
            var tx = await confirmPurchaseFunction.SendTransactionAsync(senderAddress,
                new HexBigInteger(900000), new HexBigInteger(10000));

            receipt = await pollingService.PollForReceiptAsync(tx);

            var stateFunction = contract.GetFunction("state");
            callResult = await stateFunction.CallAsync<int>();
            Assert.Equal(1, callResult);
        }

        [Fact]
        public async void ShouldDeployUsingMultipleParameters()
        {
            var contractByteCode =
                "0x606060408181528060bd833960a090525160805160009182556001556095908190602890396000f3606060405260e060020a60003504631df4f1448114601c575b6002565b34600257608360043560015460005460408051918402909202808252915173ffffffffffffffffffffffffffffffffffffffff33169184917f841774c8b4d8511a3974d7040b5bc3c603d304c926ad25d168dacd04e25c4bed9181900360200190a3919050565b60408051918252519081900360200190f3";

            var abi =
                @"[{'constant':false,'inputs':[{'name':'a','type':'int256'}],'name':'multiply','outputs':[{'name':'r','type':'int256'}],'payable':false,'type':'function'},{'inputs':[{'name':'multiplier','type':'int256'},{'name':'another','type':'int256'}],'type':'constructor'},{'anonymous':false,'inputs':[{'indexed':true,'name':'a','type':'int256'},{'indexed':true,'name':'sender','type':'address'},{'indexed':false,'name':'result','type':'int256'}],'name':'Multiplied','type':'event'}]";

            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt =
                await
                    web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, contractByteCode,
                        senderAddress, new HexBigInteger(900000), null, 7, 8);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            //get the function by name
            var multiplyFunction = contract.GetFunction("multiply");

            //do a function call (not transaction) and get the result
            var callResult = await multiplyFunction.CallAsync<int>(69);

            Assert.Equal(3864, callResult);
        }
    }
}﻿using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting 



namespace Nethereum.Contracts.IntegrationTests.CQS
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ContractHandlers
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ContractHandlers(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDecodeTransactionDeployment()
        {
            //EthereumClientIntegrationFixture.AccountAddress
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new StandardTokenDeployment
            {
                TotalSupply = 10000,
                FromAddress = senderAddress,
                Gas = new HexBigInteger(900000)
            };

            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<StandardTokenDeployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage).ConfigureAwait(false);

            var transaction =
                await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionReceipt.TransactionHash).ConfigureAwait(false);

            var deploymentMessageDecoded = transaction.DecodeTransactionToDeploymentMessage<StandardTokenDeployment>();

            Assert.Equal(deploymentMessage.TotalSupply, deploymentMessageDecoded.TotalSupply);
            Assert.Equal(deploymentMessage.FromAddress.ToLower(), deploymentMessageDecoded.FromAddress.ToLower());
            Assert.Equal(deploymentMessage.Gas, deploymentMessageDecoded.Gas);
        }


        [Fact]
        public async void ShouldDecodeTransactionInput()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new StandardTokenDeployment
            {
                TotalSupply = 10000,
                FromAddress = senderAddress,
                Gas = new HexBigInteger(900000)
            };

            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<StandardTokenDeployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage);

            var contractAddress = transactionReceipt.ContractAddress;
            var newAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";


            var transactionMessage = new TransferFunction
            {
                FromAddress = senderAddress,
                To = newAddress,
                TokenAmount = 1000
            };

            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
            var transferReceipt =
                await transferHandler.SendRequestAndWaitForReceiptAsync(contractAddress, transactionMessage);

            var transaction =
                await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transferReceipt.TransactionHash);

            var transferDecoded = transaction.DecodeTransactionToFunctionMessage<TransferFunction>();

            Assert.Equal(transactionMessage.To.ToLower(), transferDecoded.To.ToLower());
            Assert.Equal(transactionMessage.FromAddress.ToLower(), transferDecoded.FromAddress.ToLower());
            Assert.Equal(transactionMessage.TokenAmount, transferDecoded.TokenAmount);
        }

        [Fact]
        public async void Test()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new StandardTokenDeployment
            {
                TotalSupply = 10000,
                FromAddress = senderAddress,
                Gas = new HexBigInteger(900000)
            };

            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<StandardTokenDeployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage);

            var contractAddress = transactionReceipt.ContractAddress;
            var newAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";


            var transactionMessage = new TransferFunction
            {
                FromAddress = senderAddress,
                To = newAddress,
                TokenAmount = 1000
            };

            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();

            var estimatedGas = await transferHandler.EstimateGasAsync(contractAddress, transactionMessage);

            // for demo purpouses gas estimation it is done in the background so we don't set it
            transactionMessage.Gas = estimatedGas.Value;

            var transferReceipt =
                await transferHandler.SendRequestAndWaitForReceiptAsync(contractAddress, transactionMessage);

            var balanceOfFunctionMessage = new BalanceOfFunction
            {
                Owner = newAddress,
                FromAddress = senderAddress
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balanceFirstTransaction =
                await balanceHandler.QueryAsync<int>(contractAddress, balanceOfFunctionMessage);


            Assert.Equal(1000, balanceFirstTransaction);

            var transferReceipt2 =
                await transferHandler.SendRequestAndWaitForReceiptAsync(contractAddress, transactionMessage);
            var balanceSecondTransaction =
                await balanceHandler.QueryDeserializingToObjectAsync<BalanceOfFunctionOutput>(balanceOfFunctionMessage,
                    contractAddress);

            Assert.Equal(2000, balanceSecondTransaction.Balance);

            var balanceFirstTransactionHistory =
                await balanceHandler.QueryDeserializingToObjectAsync<BalanceOfFunctionOutput>(balanceOfFunctionMessage,
                    contractAddress, new BlockParameter(transferReceipt.BlockNumber));

            Assert.Equal(1000, balanceFirstTransactionHistory.Balance);
        }
    }

    [Function("transfer", "bool")]
    public class TransferFunction : FunctionMessage
    {
        [Parameter("address", "_to", 1)] public string To { get; set; }

        [Parameter("uint256", "_value", 2)] public int TokenAmount { get; set; }
    }

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunction : FunctionMessage
    {
        [Parameter("address", "_owner", 1)] public string Owner { get; set; }
    }

    [FunctionOutput]
    public class BalanceOfFunctionOutput : IFunctionOutputDTO
    {
        [Parameter("uint256", 1)] public int Balance { get; set; }
    }

    public class StandardTokenDeployment : ContractDeploymentMessage
    {
        public static string BYTECODE =
            "0x60606040526040516020806106f5833981016040528080519060200190919050505b80600160005060003373ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005081905550806000600050819055505b506106868061006f6000396000f360606040523615610074576000357c010000000000000000000000000000000000000000000000000000000090048063095ea7b31461008157806318160ddd146100b657806323b872dd146100d957806370a0823114610117578063a9059cbb14610143578063dd62ed3e1461017857610074565b61007f5b610002565b565b005b6100a060048080359060200190919080359060200190919050506101ad565b6040518082815260200191505060405180910390f35b6100c36004805050610674565b6040518082815260200191505060405180910390f35b6101016004808035906020019091908035906020019091908035906020019091905050610281565b6040518082815260200191505060405180910390f35b61012d600480803590602001909190505061048d565b6040518082815260200191505060405180910390f35b61016260048080359060200190919080359060200190919050506104cb565b6040518082815260200191505060405180910390f35b610197600480803590602001909190803590602001909190505061060b565b6040518082815260200191505060405180910390f35b600081600260005060003373ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005060008573ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925846040518082815260200191505060405180910390a36001905061027b565b92915050565b600081600160005060008673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600050541015801561031b575081600260005060008673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005060003373ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000505410155b80156103275750600082115b1561047c5781600160005060008573ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828282505401925050819055508273ffffffffffffffffffffffffffffffffffffffff168473ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040518082815260200191505060405180910390a381600160005060008673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008282825054039250508190555081600260005060008673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005060003373ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828282505403925050819055506001905061048656610485565b60009050610486565b5b9392505050565b6000600160005060008373ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000505490506104c6565b919050565b600081600160005060003373ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600050541015801561050c5750600082115b156105fb5781600160005060003373ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008282825054039250508190555081600160005060008573ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828282505401925050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040518082815260200191505060405180910390a36001905061060556610604565b60009050610605565b5b92915050565b6000600260005060008473ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005060008373ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060005054905061066e565b92915050565b60006000600050549050610683565b9056";

        public StandardTokenDeployment() : base(BYTECODE)
        {
        }

        [Parameter("uint256", "totalSupply")] public int TotalSupply { get; set; }
    }

    [Event("Transfer")]
    public class TransferEventDTOBase : IEventDTO
    {
        [Parameter("address", "_from", 1, true)]
        public string From { get; set; }

        [Parameter("address", "_to", 2, true)] public string To { get; set; }

        [Parameter("uint256", "_value", 3, false)]
        public BigInteger Value { get; set; }
    }

    public partial class TransferEventDTO : TransferEventDTOBase
    {
    }
}﻿using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.XUnitEthereumClients;
using Newtonsoft.Json.Linq;
using System.Numerics;
using Xunit;
// ReSharper disable ConsiderUsingConfigureAwait  
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.SmartContracts
{
    /*
      pragma solidity ^0.8.4;

error InsufficientBalance(uint256 available, uint256 required);

contract TestToken {
    mapping(address => uint) balance;
    function transfer(address to, uint256 amount) public {
        if (amount > balance[msg.sender])
            // Error call using named parameters. Equivalent to
            // revert InsufficientBalance(balance[msg.sender], amount);
            revert InsufficientBalance({
                available: balance[msg.sender],
                required: amount
            });
        balance[msg.sender] -= amount;
        balance[to] += amount;
    }
    // ...
}
     */

    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class CustomErrorTest
    {
        public partial class TestTokenDeployment: ContractDeploymentMessage
        {
            public static string BYTECODE = "608060405234801561001057600080fd5b5061019b806100206000396000f3fe608060405234801561001057600080fd5b506004361061002b5760003560e01c8063a9059cbb14610030575b600080fd5b61004361003e3660046100ea565b610045565b005b3360009081526020819052604090205481111561009557336000908152602081905260409081902054905163cf47918160e01b815260048101919091526024810182905260440160405180910390fd5b33600090815260208190526040812080548392906100b4908490610138565b90915550506001600160a01b038216600090815260208190526040812080548392906100e1908490610120565b90915550505050565b600080604083850312156100fc578182fd5b82356001600160a01b0381168114610112578283fd5b946020939093013593505050565b600082198211156101335761013361014f565b500190565b60008282101561014a5761014a61014f565b500390565b634e487b7160e01b600052601160045260246000fdfea2646970667358221220036d01bbac8615b9779f8355c03bd4da1057c57188f047db3a3190e81f894f7964736f6c63430008040033";

            public TestTokenDeployment() : base(BYTECODE) { }
            public TestTokenDeployment(string byteCode) : base(byteCode) { }
        }

        public partial class TransferFunction : TransferFunctionBase { }

        [Function("transfer")]
        public class TransferFunctionBase : FunctionMessage
        {
            [Parameter("address", "to", 1)]
            public virtual string To { get; set; }
            [Parameter("uint256", "amount", 2)]
            public virtual BigInteger Amount { get; set; }
        }

        [Error("InsufficientBalance")]
        public class InsufficientBalance
        {
            [Parameter("uint256", "available", 1)]
            public virtual BigInteger Available { get; set; }

            [Parameter("uint256", "required", 1)]
            public virtual BigInteger Required { get; set; }
        }

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public CustomErrorTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact] //estimates are done when making a transaction
        public async void ShouldRetrieveErrorReasonMakingAnEstimateForTransaction()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new TestTokenDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<TestTokenDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;

                var contractHandler = web3.Eth.GetContractHandler(contractAddress);

                var error = await Assert.ThrowsAsync<SmartContractCustomErrorRevertException>(async () =>
                   await contractHandler.EstimateGasAsync(new TransferFunction() { Amount = 100, To = EthereumClientIntegrationFixture.AccountAddress }).ConfigureAwait(false)).ConfigureAwait(false);
                
                Assert.True(error.IsCustomErrorFor<InsufficientBalance>());
                var insufficientBalance = error.DecodeError<InsufficientBalance>();
                Assert.Equal(100, insufficientBalance.Required);
                Assert.Equal(0, insufficientBalance.Available);

            }
        }

        [Fact]
        public async void ShouldRetrieveErrorReasonMakingAQuery()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new TestTokenDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<TestTokenDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;

                var contractHandler = web3.Eth.GetContractHandler(contractAddress);


                var error = await Assert.ThrowsAsync<SmartContractCustomErrorRevertException>(async () =>
                   //random return value as it is going to error
                   await contractHandler.QueryAsync<TransferFunction, int>(new TransferFunction() { Amount = 100, To = EthereumClientIntegrationFixture.AccountAddress }).ConfigureAwait(false)).ConfigureAwait(false);

                Assert.True(error.IsCustomErrorFor<InsufficientBalance>());
                var insufficientBalance = error.DecodeError<InsufficientBalance>();
                Assert.Equal(100, insufficientBalance.Required);
                Assert.Equal(0, insufficientBalance.Available);

            }
        }

        [Fact]
        public async void ShouldFindError()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new TestTokenDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<TestTokenDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;
                var contractHandler = web3.Eth.GetContractHandler(contractAddress);

                var customErrorException = await Assert.ThrowsAsync<SmartContractCustomErrorRevertException>(async () =>
                   //random return value as it is going to error
                   await contractHandler.QueryAsync<TransferFunction, int>(new TransferFunction() { Amount = 100, To = EthereumClientIntegrationFixture.AccountAddress }).ConfigureAwait(false)).ConfigureAwait(false);

                var contract = web3.Eth.GetContract("[{'inputs':[{'internalType':'uint256','name':'available','type':'uint256'},{'internalType':'uint256','name':'required','type':'uint256'}],'name':'InsufficientBalance','type':'error'},{'inputs':[{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'transfer','outputs':[],'stateMutability':'nonpayable','type':'function'}]", contractAddress);
                var error = contract.FindError(customErrorException.ExceptionEncodedData);
                Assert.NotNull(error);
                var errorJObject = error.DecodeExceptionEncodedDataToDefault(customErrorException.ExceptionEncodedData).ConvertToJObject();
                var expectedJson = JToken.Parse(@"{'available': '0','required': '100'}");
                Assert.True(JObject.DeepEquals(expectedJson, errorJObject));
            }
        }

        [Fact]
        public async void ShouldRetrieveErrorReasonMakingACall()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new TestTokenDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<TestTokenDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;

                var contract = web3.Eth.GetContract("[{'inputs':[{'internalType':'uint256','name':'available','type':'uint256'},{'internalType':'uint256','name':'required','type':'uint256'}],'name':'InsufficientBalance','type':'error'},{'inputs':[{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'transfer','outputs':[],'stateMutability':'nonpayable','type':'function'}]", contractAddress);
                var function = contract.GetFunction("transfer");

                var error = await Assert.ThrowsAsync<SmartContractCustomErrorRevertException>(async () =>
                   //random return value as it is going to error
                   await function.CallAsync<int>(EthereumClientIntegrationFixture.AccountAddress, 100).ConfigureAwait(false)).ConfigureAwait(false);

                Assert.True(error.IsCustomErrorFor<InsufficientBalance>());
                var insufficientBalance = error.DecodeError<InsufficientBalance>();
                Assert.Equal(100, insufficientBalance.Required);
                Assert.Equal(0, insufficientBalance.Available);

            }
        }

        [Fact]
        public async void ShouldRetrieveErrorReasonMakingAnEstimateCall()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new TestTokenDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<TestTokenDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;

                var contract = web3.Eth.GetContract("[{'inputs':[{'internalType':'uint256','name':'available','type':'uint256'},{'internalType':'uint256','name':'required','type':'uint256'}],'name':'InsufficientBalance','type':'error'},{'inputs':[{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'transfer','outputs':[],'stateMutability':'nonpayable','type':'function'}]", contractAddress);
                var function = contract.GetFunction("transfer");

                var error = await Assert.ThrowsAsync<SmartContractCustomErrorRevertException>(async () =>
                   //random return value as it is going to error
                   await function.EstimateGasAsync(EthereumClientIntegrationFixture.AccountAddress, 100).ConfigureAwait(false)).ConfigureAwait(false);

                Assert.True(error.IsCustomErrorFor<InsufficientBalance>());
                var insufficientBalance = error.DecodeError<InsufficientBalance>();
                Assert.Equal(100, insufficientBalance.Required);
                Assert.Equal(0, insufficientBalance.Available);
            }
        }
    }
}using System.Linq;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EncodingInputOutput
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class DefaultTypeIntegrationTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public DefaultTypeIntegrationTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        [Fact]
        public async Task<string> Test()
        {
            //The compiled solidity contract to be deployed
            /*
              contract test { 
    
    
                function test1() returns(int) { 
                   int d = 3457987492347979798742;
                   return d;
                }
    
                  function test2(int d) returns(int) { 
                   return d;
                }
    
                function test3(int d)returns(int){
                    int x = d + 1 -1;
                    return x;
                }
    
                function test4(int d)returns(bool){
                    return d == 3457987492347979798742;
                }
    
                function test5(int d)returns(bool){
                    return d == -3457987492347979798742;
                }
    
                function test6(int d)returns(bool){
                    return d == 500;
                }
    
                function test7(int256 d)returns(bool){
                    return d == 74923479797565;
                }
    
                function test8(int256 d)returns(bool){
                    return d == 9223372036854775808;
                }
            }
           }
           */

            var contractByteCode =
                "60606040526102b7806100126000396000f36060604052361561008a576000357c01000000000000000000000000000000000000000000000000000000009004806311da9d8c1461008c5780631c2a1101146100b857806363798981146100e45780636b59084d146101105780639e71212514610133578063a605861c1461015f578063e42d455b1461018b578063e92b09da146101b75761008a565b005b6100a26004808035906020019091905050610243565b6040518082815260200191505060405180910390f35b6100ce600480803590602001909190505061020e565b6040518082815260200191505060405180910390f35b6100fa60048080359060200190919050506101ff565b6040518082815260200191505060405180910390f35b61011d60048050506101e3565b6040518082815260200191505060405180910390f35b6101496004808035906020019091905050610229565b6040518082815260200191505060405180910390f35b6101756004808035906020019091905050610274565b6040518082815260200191505060405180910390f35b6101a1600480803590602001909190505061029e565b6040518082815260200191505060405180910390f35b6101cd6004808035906020019091905050610287565b6040518082815260200191505060405180910390f35b6000600068bb75377716692498d690508091506101fb565b5090565b6000819050610209565b919050565b60006000600160018401039050809150610223565b50919050565b600068bb75377716692498d68214905061023e565b919050565b60007fffffffffffffffffffffffffffffffffffffffffffffff448ac888e996db672a8214905061026f565b919050565b60006101f482149050610282565b919050565b60006544247b660f3d82149050610299565b919050565b6000678000000000000000821490506102b2565b91905056";

            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test5"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test3"",""outputs"":[{""name"":"""",""type"":""int256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test2"",""outputs"":[{""name"":"""",""type"":""int256""}],""type"":""function""},{""constant"":false,""inputs"":[],""name"":""test1"",""outputs"":[{""name"":"""",""type"":""int256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test4"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test6"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test8"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test7"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""}]";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            //deploy the contract, including abi and a paramter of 7. 
            var transactionHash =
                await
                    web3.Eth.DeployContract.SendRequestAsync(contractByteCode, senderAddress,
                        new HexBigInteger(900000));

            //get the contract address 
            TransactionReceipt receipt = null;
            //wait for the contract to be mined to the address
            while (receipt == null)
            {
                await Task.Delay(500);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);
            var test1 = contract.GetFunction("test1");
            Assert.Equal("3457987492347979798742",
                (await test1.CallDecodingToDefaultAsync()).First().Result.ToString());
            Assert.Equal("3457987492347979798742",
                (await test1.CallDecodingToDefaultAsync(@from: senderAddress, gas: null, value: null)).First().Result
                .ToString());

            return "OK";
        }
    }
}﻿using Nethereum.Hex.HexTypes;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.Deployment
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class DeploymentNullIssue
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public DeploymentNullIssue(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDeployWithNoConstructorParameters()
        {
            var ABI =
                "[{'constant':false,'inputs':[],'name':'createID','outputs':[{'name':'','type':'address'}],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'owner','outputs':[{'name':'','type':'address'}],'payable':false,'type':'function'},{'anonymous':false,'inputs':[{'indexed':true,'name':'_from','type':'address'},{'indexed':false,'name':'_controllerAddress','type':'address'}],'name':'ReturnIDController','type':'event'}]";
            var BYTE_CODE =
                "0x60606040525b33600060006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b5b612891806100576000396000f30060606040526000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff16806348573542146100465780638da5cb5b14610098575bfe5b341561004e57fe5b6100566100ea565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b34156100a057fe5b6100a861019f565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b6000600060006100f8610376565b809050604051809103906000f080151561010e57fe5b915061011a82336101c5565b90503373ffffffffffffffffffffffffffffffffffffffff167f94083f4ecce35399252890d68b14d19ad0419a427258864ef16558218d7bb87d82604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390a28092505b505090565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b60006000836101d2610386565b808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001915050604051809103906000f080151561021b57fe5b90508373ffffffffffffffffffffffffffffffffffffffff1663a6f9dae1826040518263ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001915050600060405180830381600087803b15156102b457fe5b60325a03f115156102c157fe5b5050508073ffffffffffffffffffffffffffffffffffffffff1663a6f9dae1846040518263ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001915050600060405180830381600087803b151561035b57fe5b60325a03f1151561036857fe5b5050508091505b5092915050565b6040516114b08061039783390190565b60405161101f8061184783390190560060606040525b33600060006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b5b611459806100576000396000f300606060405236156100b8576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff16806302859d60146100ba5780631ab807f814610115578063277c94671461015857806341c0e1b5146101945780634f23eb86146101a65780635f893bfa146102a2578063781dd722146102d85780638da5cb5b1461032d578063a6f9dae11461037f578063b115ce0d146103b5578063e7996f0714610419578063eb43e0331461043d575bfe5b34156100c257fe5b6100fb60048080356000191690602001909190803573ffffffffffffffffffffffffffffffffffffffff169060200190919050506104a1565b604051808215151515815260200191505060405180910390f35b341561011d57fe5b61015660048080356000191690602001909190803573ffffffffffffffffffffffffffffffffffffffff169060200190919050506106b4565b005b341561016057fe5b61017660048080359060200190919050506106cb565b60405180826000191660001916815260200191505060405180910390f35b341561019c57fe5b6101a46106f0565b005b34156101ae57fe5b610260600480803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509190803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509190803573ffffffffffffffffffffffffffffffffffffffff169060200190919050506107dc565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b34156102aa57fe5b6102d6600480803573ffffffffffffffffffffffffffffffffffffffff16906020019091905050610988565b005b34156102e057fe5b61032b600480803573ffffffffffffffffffffffffffffffffffffffff1690602001909190803573ffffffffffffffffffffffffffffffffffffffff169060200190919050506109ff565b005b341561033557fe5b61033d610b67565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b341561038757fe5b6103b3600480803573ffffffffffffffffffffffffffffffffffffffff16906020019091905050610b8d565b005b34156103bd57fe5b6103d7600480803560001916906020019091905050610c2a565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b341561042157fe5b61043b600480803560001916906020019091905050610c5d565b005b341561044557fe5b61045f600480803560001916906020019091905050610cf7565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b6000600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614156106ad573073ffffffffffffffffffffffffffffffffffffffff168273ffffffffffffffffffffffffffffffffffffffff16638da5cb5b6000604051602001526040518163ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401809050602060405180830381600087803b151561057c57fe5b60325a03f1151561058957fe5b5050506040518051905073ffffffffffffffffffffffffffffffffffffffff161415156105b557610000565b8160016000856000191660001916815260200190815260200160002060006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff160217905550600280548060010182816106239190610d3d565b916000526020600020900160005b85909190915090600019169055508173ffffffffffffffffffffffffffffffffffffffff1660016000856000191660001916815260200190815260200160002060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff161490505b5b5b92915050565b6106c66106c083610cf7565b826109ff565b5b5050565b6002818154811015156106da57fe5b906000526020600020900160005b915090505481565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614156107d957600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614156107d757600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16ff5b5b5b5b565b6000600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614156109805783838361083f610d69565b8080602001806020018473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020018381038352868181518152602001915080519060200190808383600083146108c1575b8051825260208311156108c15760208201915060208101905060208303925061089d565b505050905090810190601f1680156108ed5780820380516001836020036101000a031916815260200191505b50838103825285818151815260200191508051906020019080838360008314610935575b80518252602083111561093557602082019150602081019050602083039250610911565b505050905090810190601f1680156109615780820380516001836020036101000a031916815260200191505b5095505050505050604051809103906000f080151561097c57fe5b90505b5b5b9392505050565b8073ffffffffffffffffffffffffffffffffffffffff1663b6549f756040518163ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401809050600060405180830381600087803b15156109eb57fe5b60325a03f115156109f857fe5b5050505b50565b3073ffffffffffffffffffffffffffffffffffffffff168273ffffffffffffffffffffffffffffffffffffffff16638da5cb5b6000604051602001526040518163ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401809050602060405180830381600087803b1515610a8257fe5b60325a03f11515610a8f57fe5b5050506040518051905073ffffffffffffffffffffffffffffffffffffffff16141515610abb57610000565b8173ffffffffffffffffffffffffffffffffffffffff16637c6ebde9826040518263ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001915050600060405180830381600087803b1515610b5257fe5b60325a03f11515610b5f57fe5b5050505b5050565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161415610c265780600060006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b5b5b50565b60016020528060005260406000206000915054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161415610cf35760016000826000191660001916815260200190815260200160002060006101000a81549073ffffffffffffffffffffffffffffffffffffffff02191690555b5b5b50565b600060016000836000191660001916815260200190815260200160002060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1690505b919050565b815481835581811511610d6457818360005260206000209182019101610d639190610d79565b5b505050565b60405161068f80610d9f83390190565b610d9b91905b80821115610d97576000816000905550600101610d7f565b5090565b90560060606040526000600360006101000a81548160ff021916908315150217905550341561002757fe5b60405161068f38038061068f833981016040528080518201919060200180518201919060200180519060200190919050505b5b33600060006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b82600190805190602001906100b2929190610114565b5081600290805190602001906100c9929190610114565b5080600360016101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b5050506101b9565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061015557805160ff1916838001178555610183565b82800160010185558215610183579182015b82811115610182578251825591602001919060010190610167565b5b5090506101909190610194565b5090565b6101b691905b808211156101b257600081600090555060010161019a565b5090565b90565b6104c7806101c86000396000f30060606040523615610076576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff16806309bd5a6014610078578063232533b214610111578063516f279e1461016357806363d256ce146101fc5780638da5cb5b14610226578063b6549f7514610278575bfe5b341561008057fe5b61008861028a565b60405180806020018281038252838181518152602001915080519060200190808383600083146100d7575b8051825260208311156100d7576020820191506020810190506020830392506100b3565b505050905090810190601f1680156101035780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b341561011957fe5b610121610328565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b341561016b57fe5b61017361034e565b60405180806020018281038252838181518152602001915080519060200190808383600083146101c2575b8051825260208311156101c25760208201915060208101905060208303925061019e565b505050905090810190601f1680156101ee5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b341561020457fe5b61020c6103ec565b604051808215151515815260200191505060405180910390f35b341561022e57fe5b6102366103ff565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b341561028057fe5b610288610425565b005b60028054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156103205780601f106102f557610100808354040283529160200191610320565b820191906000526020600020905b81548152906001019060200180831161030357829003601f168201915b505050505081565b600360019054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b60018054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156103e45780601f106103b9576101008083540402835291602001916103e4565b820191906000526020600020905b8154815290600101906020018083116103c757829003601f168201915b505050505081565b600360009054906101000a900460ff1681565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161415610498576001600360006101000a81548160ff0219169083151502179055505b5b5b5600a165627a7a72305820ca63b024d1e5e546695f10f3a1280cf6d95cd35dcb7c112aa9f86f18298b01ac0029a165627a7a72305820d10ec5dbd904c95211e74b47091a491c696569914f6f925e8024c989053a400c00296060604052341561000c57fe5b60405160208061101f833981016040528080519060200190919050505b5b33600060006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b80600160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b505b610f60806100bf6000396000f300606060405236156100ad576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff16806302859d60146100af5780631ff8df681461010a5780634f23eb861461015c5780635f893bfa146102585780637ca307b41461028e5780638da5cb5b146102a0578063a54c0810146102f2578063a6f9dae114610328578063ab9dbd071461035e578063e7996f07146103b0578063eb43e033146103d4575bfe5b34156100b757fe5b6100f060048080356000191690602001909190803573ffffffffffffffffffffffffffffffffffffffff16906020019091905050610438565b604051808215151515815260200191505060405180910390f35b341561011257fe5b61011a6105dc565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b341561016457fe5b610216600480803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509190803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509190803573ffffffffffffffffffffffffffffffffffffffff16906020019091905050610607565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b341561026057fe5b61028c600480803573ffffffffffffffffffffffffffffffffffffffff16906020019091905050610892565b005b341561029657fe5b61029e61095f565b005b34156102a857fe5b6102b0610ae1565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b34156102fa57fe5b610326600480803573ffffffffffffffffffffffffffffffffffffffff16906020019091905050610b07565b005b341561033057fe5b61035c600480803573ffffffffffffffffffffffffffffffffffffffff16906020019091905050610bfc565b005b341561036657fe5b61036e610cf1565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b34156103b857fe5b6103d2600480803560001916906020019091905050610d1c565b005b34156103dc57fe5b6103f6600480803560001916906020019091905050610e75565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b6000600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614806104e35750600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16145b156105d557600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff166302859d6084846000604051602001526040518363ffffffff167c01000000000000000000000000000000000000000000000000000000000281526004018083600019166000191681526020018273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200192505050602060405180830381600087803b15156105ba57fe5b60325a03f115156105c757fe5b5050506040518051905090505b5b5b92915050565b6000600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1690505b90565b6000600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614806106b25750600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16145b1561088a57600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16634f23eb868585856000604051602001526040518463ffffffff167c01000000000000000000000000000000000000000000000000000000000281526004018080602001806020018473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020018381038352868181518152602001915080519060200190808383600083146107b1575b8051825260208311156107b15760208201915060208101905060208303925061078d565b505050905090810190601f1680156107dd5780820380516001836020036101000a031916815260200191505b50838103825285818151815260200191508051906020019080838360008314610825575b80518252602083111561082557602082019150602081019050602083039250610801565b505050905090810190601f1680156108515780820380516001836020036101000a031916815260200191505b5095505050505050602060405180830381600087803b151561086f57fe5b60325a03f1151561087c57fe5b5050506040518051905090505b5b5b9392505050565b600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16635f893bfa826040518263ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001915050600060405180830381600087803b151561094b57fe5b60325a03f1151561095857fe5b5050505b50565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161480610a085750600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16145b15610ade57600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff166341c0e1b56040518163ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401809050600060405180830381600087803b1515610a9257fe5b60325a03f11515610a9f57fe5b505050600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16ff5b5b5b565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161480610bb05750600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16145b15610bf85780600260006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b5b5b50565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161480610ca55750600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16145b15610ced5780600060006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055505b5b5b50565b6000600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1690505b90565b600060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161480610dc55750600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16145b15610e7157600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1663e7996f07826040518263ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401808260001916600019168152602001915050600060405180830381600087803b1515610e5f57fe5b60325a03f11515610e6c57fe5b5050505b5b5b50565b6000600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1663eb43e033836000604051602001526040518263ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401808260001916600019168152602001915050602060405180830381600087803b1515610f1557fe5b60325a03f11515610f2257fe5b5050506040518051905090505b9190505600a165627a7a72305820a1b4722dacd2e6d25ba83549efe9c66b18b5799be2a59687cf5738279cf8766e0029a165627a7a72305820c721968970e1abea3089152dfdd4a6ec4647e051088773f1db229f94fc42b0530029";
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var gas = await web3.Eth.DeployContract.EstimateGasAsync(ABI, BYTE_CODE, senderAddress);
            var transactionReceiptService = new TransactionReceiptPollingService(web3.TransactionManager);
            var receipt = await transactionReceiptService.DeployContractAndWaitForReceiptAsync(
                () =>
                    web3.Eth.DeployContract.SendRequestAsync(ABI, BYTE_CODE, senderAddress,
                        new HexBigInteger(3905820)));
            Assert.NotNull(receipt.ContractAddress);
        }
    }
}﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Fee1559Suggestions;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.RPC.Web3;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Signer.IntegrationTests
{
    public class EIP1559Test
    {

        [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
        public class SignedEIP155
        {
            private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

            public SignedEIP155(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
            {
                _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
            }

            //[Fact]
            public async void ShouldCheckFeeHistory()
            {
                //besu
                // var web3 = new Nethereum.Web3.Web3("http://18.116.30.130:8545/");
                //calavera
                var web3 = new Nethereum.Web3.Web3("http://18.224.51.102:8545/");
                //var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Goerli);
                var version = await new Web3ClientVersion(web3.Client).SendRequestAsync().ConfigureAwait(false);

                var x = new TimePreferenceFeeSuggestionStrategy(web3.Client);
                var fees = await x.SuggestFeesAsync().ConfigureAwait(false);

                //var block =
                //    await web3.Eth.FeeHistory.SendRequestAsync(7, new BlockParameter(10), new []{10,20, 30}
                //         );
                var count = fees.Length;

            }

            [Fact]
            public async void ShouldSendTransactionWithAccessLists()
            {
                var chainId = 444444444500;

                var accessLists = new List<AccessListItem>();
                accessLists.Add(new AccessListItem("0x527306090abaB3A6e1400e9345bC60c78a8BEf57",
                    new List<byte[]>
                    {
                        "0x3fd54831f488a22b28398de0c567a3b064b937f54f81739ae9bd545967f3abab".HexToByteArray(),
                        "0x3fd54831f488a22b28398de0c567a3b064b937f54f81739ae9bd545967f3abac".HexToByteArray()
                    }
                ));
                accessLists.Add(new AccessListItem("0x427306090abaB3A6e1400e9345bC60c78a8BEf5c",
                    new List<byte[]>
                    {
                        "0x3fd54831f488a22b28398de0c567a3b064b937f54f81739ae9bd545967f3abaa".HexToByteArray(),
                        "0x3fd54831f488a22b28398de0c567a3b064b937f54f81739ae9bd545967f3abad".HexToByteArray()
                    }
                ));

                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var nonce = await web3.Eth.TransactionManager.Account.NonceService.GetNextNonceAsync().ConfigureAwait(false);
                var lastBlock =
                    await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(
                        BlockParameter.CreateLatest()).ConfigureAwait(false);
                var baseFee = lastBlock.BaseFeePerGas;
                var maxPriorityFeePerGas = 2000000000;
                var maxFeePerGas = baseFee.Value * 2 + 2000000000;

                var transaction1559 = new Transaction1559(chainId, nonce, maxPriorityFeePerGas, maxFeePerGas, 45000,
                    "0x1ad91ee08f21be3de0ba2ba6918e714da6b45836", 10, "", accessLists);
                var signer = new Transaction1559Signer();
                signer.SignTransaction(new EthECKey(EthereumClientIntegrationFixture.AccountPrivateKey), transaction1559);


                var txnHash =
                    await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(transaction1559.GetRLPEncoded()
                        .ToHex()).ConfigureAwait(false);
                // create recover signature
                var txn = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txnHash).ConfigureAwait(false);

                Assert.True(txn.To.IsTheSameAddress("0x1ad91ee08f21be3de0ba2ba6918e714da6b45836"));
                Assert.Equal(10, txn.Value.Value);

            }


            [Fact]
            public async void ShouldSendTransactionCalculatingTheDefaultFees()
            {
                var chainId = 444444444500;
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var nonce = await web3.Eth.TransactionManager.Account.NonceService.GetNextNonceAsync().ConfigureAwait(false);
                var lastBlock =
                    await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(
                        BlockParameter.CreateLatest()).ConfigureAwait(false);
                var baseFee = lastBlock.BaseFeePerGas;
                var maxPriorityFeePerGas = 2000000000;
                var maxFeePerGas = baseFee.Value * 2 + 2000000000;

                var transaction1559 = new Transaction1559(chainId, nonce.Value, maxPriorityFeePerGas, maxFeePerGas,
                    45000,
                    "0x1ad91ee08f21be3de0ba2ba6918e714da6b45836", 10, "", null);
                var signer = new Transaction1559Signer();
                signer.SignTransaction(new EthECKey(EthereumClientIntegrationFixture.AccountPrivateKey), transaction1559);


                var txnHash =
                    await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(transaction1559.GetRLPEncoded()
                        .ToHex()).ConfigureAwait(false);
                // create recover signature
                var txn = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txnHash).ConfigureAwait(false);
                //what I want is to get the right Transaction checking the type or chainid etc and do a recovery
                Assert.True(txn.To.IsTheSameAddress("0x1ad91ee08f21be3de0ba2ba6918e714da6b45836"));
                Assert.Equal(10, txn.Value.Value);

                var transaction1559FromChain = TransactionFactory.CreateTransaction(chainId, (byte) txn.Type.Value,
                    txn.Nonce, txn.MaxPriorityFeePerGas,
                    txn.MaxFeePerGas, null, txn.Gas, txn.To, txn.Value, txn.Input, null, txn.R, txn.S, txn.V);

                Assert.True(transaction1559FromChain.GetSenderAddress()
                    .IsTheSameAddress("0x12890D2cce102216644c59daE5baed380d84830c"));

                var transactionReceipt =
                    await new TransactionReceiptPollingService(web3.TransactionManager).PollForReceiptAsync(txnHash,
                        new CancellationTokenSource().Token).ConfigureAwait(false);

            }

        }
    }
}﻿using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EncodingInputOutput
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EncodingIssueGeth1_7
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EncodingIssueGeth1_7(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        /*
         ﻿[{"constant":true,"inputs":[],"name":"InvestmentsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"type":"function"},{"constant":true,"inputs":[],"name":"GetAllInvestments","outputs":[{"name":"ids","type":"uint256[]"},{"name":"addresses","type":"address[]"},{"name":"chargerIds","type":"uint256[]"},{"name":"balances","type":"uint256[]"},{"name":"states","type":"bool[]"}],"payable":false,"type":"function"},{"constant":false,"inputs":[{"name":"_from","type":"address"},{"name":"_charger","type":"uint256"}],"name":"investInQueue","outputs":[{"name":"success","type":"bool"}],"payable":true,"type":"function"},{"constant":false,"inputs":[{"name":"_newCharge","type":"uint256"}],"name":"addCharge","outputs":[],"payable":false,"type":"function"},{"constant":true,"inputs":[],"name":"getChargers","outputs":[{"name":"chargers","type":"uint256[]"}],"payable":false,"type":"function"}]
        60606040526108f2806100126000396000f3606060405260e060020a6000350463472ad331811461004a5780637996c88714610058578063b4821203146102ff578063dbda4c0814610400578063fa82518514610449575b610002565b34610002576104af60005481565b346100025760408051602080820183526000808352835180830185528181528451808401865282815285518085018752838152865180860188528481528751808701895285815288518088018a5286815289518089018b528781528a51808a018c528881528b51998a018c52888a5288549b516104c19c989a979996989597959694959394929391929087908059106100ee5750595b908082528060200260200182016040528015610105575b509550866040518059106101165750595b90808252806020026020018201604052801561012d575b5094508660405180591061013e5750595b908082528060200260200182016040528015610155575b509350866040518059106101665750595b90808252806020026020018201604052801561017d575b5092508660405180591061018e5750595b9080825280602002602001820160405280156101a5575b509150600090505b60005481101561065757600180548290811015610002579060005260206000209060050201600050548651879083908110156100025760209081029091010152600180548290811015610002579060005260206000209060050201600050600101548551600160a060020a03909116908690839081101561000257600160a060020a03909216602092830290910190910152600180548290811015610002579060005260206000209060050201600050600201600050548482815181101561000257602090810290910101526001805482908110156100025790600052602060002090600502016000506003016000505483828151811015610002576020908102909101015260018054829081101561000257906000526020600020906005020160005060040154825160ff9091169083908390811015610002579115156020928302909101909101526001016101ad565b6105f760043560243560008082151561032f57600280546000908110156100025760009182526020909120015492505b61066a84846040805160a081018252600080825260208201819052918101829052606081018290526080810182905281905b6000548210156106c05784600160a060020a0316600160005083815481101561000257906000526020600020906005020160005060010154600160a060020a03161480156103d1575083600160005083815481101561000257906000526020600020906005020160005060020154145b156107a7576001805483908110156100025790600052602060002090600502016000505492505b505092915050565b346100025761060b600435600280546001810180835582818380158290116106a6576000838152602090206106a69181019083015b808211156106bc5760008155600101610435565b34610002576040805160208082018352600082526002805484518184028101840190955280855261060d94928301828280156104a557602002820191906000526020600020905b81548152600190910190602001808311610490575b5050505050905090565b60408051918252519081900360200190f35b60405180806020018060200180602001806020018060200186810386528b8181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f15090500186810385528a8181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038452898181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038352888181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038252878181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050019a505050505050505050505060405180910390f35b604080519115158252519081900360200190f35b005b60405180806020018281038252838181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050019250505060405180910390f35b50939a9299509097509550909350915050565b905034600160005060018303815481101561000257906000526020600020906005020160005060030180549091019055600191505b5092915050565b5050506000928352506020909120018190555b50565b5090565b6107b2858560a06040519081016040528060008152602001600081526020016000815260200160008152602001600081526020015060a0604051908101604052806000815260200160008152602001600081526020016000815260200160008152602001506107bd8360008181526003602052604090205460ff60a060020a9091041615156106b9576000818152600360205260409020805474ff0000000000000000000000000000000000000000191660a060020a179055600280546001810180835582818380158290116106a6576000838152602090206106a6918101908301610435565b600190910190610361565b8051935090506103f8565b60008054600190810191829055600160a060020a0386166020840152604083018590529082528054808201808355828183801582901161085e5760050281600502836000526020600020918201910161085e91905b808211156106bc57600080825560018201805473ffffffffffffffffffffffffffffffffffffffff1916905560028201819055600382015560048101805460ff19169055600501610812565b50505060009283525060209182902083516005909202019081559082015160018201805473ffffffffffffffffffffffffffffffffffffffff19166c0100000000000000000000000092830292909204919091179055604082015160028201556060820151600382015560808201516004909101805460ff191660f860020a9283029290920491909117905590508061069f56
        0xc74c12278aa650f41c20cc66dc488c583ab780cf
        0x1dC0Cdd495d2Fd22aaf216b82D14936e1fCD8b40 
        */
        [Fact]
        public async void Test()
        {
            var abi =
                "[{'constant':true,'inputs':[],'name':'InvestmentsCount','outputs':[{'name':'','type':'uint256'}],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'GetAllInvestments','outputs':[{'name':'ids','type':'uint256[]'},{'name':'addresses','type':'address[]'},{'name':'chargerIds','type':'uint256[]'},{'name':'balances','type':'uint256[]'},{'name':'states','type':'bool[]'}],'payable':false,'type':'function'},{'constant':false,'inputs':[{'name':'_from','type':'address'},{'name':'_charger','type':'uint256'}],'name':'investInQueue','outputs':[{'name':'success','type':'bool'}],'payable':true,'type':'function'},{'constant':false,'inputs':[{'name':'_newCharge','type':'uint256'}],'name':'addCharge','outputs':[],'payable':false,'type':'function'},{'constant':true,'inputs':[],'name':'getChargers','outputs':[{'name':'chargers','type':'uint256[]'}],'payable':false,'type':'function'}]";

            var byteCode =
                "0x60606040526108f2806100126000396000f3606060405260e060020a6000350463472ad331811461004a5780637996c88714610058578063b4821203146102ff578063dbda4c0814610400578063fa82518514610449575b610002565b34610002576104af60005481565b346100025760408051602080820183526000808352835180830185528181528451808401865282815285518085018752838152865180860188528481528751808701895285815288518088018a5286815289518089018b528781528a51808a018c528881528b51998a018c52888a5288549b516104c19c989a979996989597959694959394929391929087908059106100ee5750595b908082528060200260200182016040528015610105575b509550866040518059106101165750595b90808252806020026020018201604052801561012d575b5094508660405180591061013e5750595b908082528060200260200182016040528015610155575b509350866040518059106101665750595b90808252806020026020018201604052801561017d575b5092508660405180591061018e5750595b9080825280602002602001820160405280156101a5575b509150600090505b60005481101561065757600180548290811015610002579060005260206000209060050201600050548651879083908110156100025760209081029091010152600180548290811015610002579060005260206000209060050201600050600101548551600160a060020a03909116908690839081101561000257600160a060020a03909216602092830290910190910152600180548290811015610002579060005260206000209060050201600050600201600050548482815181101561000257602090810290910101526001805482908110156100025790600052602060002090600502016000506003016000505483828151811015610002576020908102909101015260018054829081101561000257906000526020600020906005020160005060040154825160ff9091169083908390811015610002579115156020928302909101909101526001016101ad565b6105f760043560243560008082151561032f57600280546000908110156100025760009182526020909120015492505b61066a84846040805160a081018252600080825260208201819052918101829052606081018290526080810182905281905b6000548210156106c05784600160a060020a0316600160005083815481101561000257906000526020600020906005020160005060010154600160a060020a03161480156103d1575083600160005083815481101561000257906000526020600020906005020160005060020154145b156107a7576001805483908110156100025790600052602060002090600502016000505492505b505092915050565b346100025761060b600435600280546001810180835582818380158290116106a6576000838152602090206106a69181019083015b808211156106bc5760008155600101610435565b34610002576040805160208082018352600082526002805484518184028101840190955280855261060d94928301828280156104a557602002820191906000526020600020905b81548152600190910190602001808311610490575b5050505050905090565b60408051918252519081900360200190f35b60405180806020018060200180602001806020018060200186810386528b8181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f15090500186810385528a8181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038452898181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038352888181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038252878181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050019a505050505050505050505060405180910390f35b604080519115158252519081900360200190f35b005b60405180806020018281038252838181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050019250505060405180910390f35b50939a9299509097509550909350915050565b905034600160005060018303815481101561000257906000526020600020906005020160005060030180549091019055600191505b5092915050565b5050506000928352506020909120018190555b50565b5090565b6107b2858560a06040519081016040528060008152602001600081526020016000815260200160008152602001600081526020015060a0604051908101604052806000815260200160008152602001600081526020016000815260200160008152602001506107bd8360008181526003602052604090205460ff60a060020a9091041615156106b9576000818152600360205260409020805474ff0000000000000000000000000000000000000000191660a060020a179055600280546001810180835582818380158290116106a6576000838152602090206106a6918101908301610435565b600190910190610361565b8051935090506103f8565b60008054600190810191829055600160a060020a0386166020840152604083018590529082528054808201808355828183801582901161085e5760050281600502836000526020600020918201910161085e91905b808211156106bc57600080825560018201805473ffffffffffffffffffffffffffffffffffffffff1916905560028201819055600382015560048101805460ff19169055600501610812565b50505060009283525060209182902083516005909202019081559082015160018201805473ffffffffffffffffffffffffffffffffffffffff19166c0100000000000000000000000092830292909204919091179055604082015160028201556060820151600382015560808201516004909101805460ff191660f860020a9283029290920491909117905590508061069f56";

            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(byteCode, senderAddress,
                new HexBigInteger(900000), null, null, null);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);
            var addChargeFunction = contract.GetFunction("addCharge");

            var gas = await addChargeFunction.EstimateGasAsync(senderAddress, null, null, 20);
            var tx = await addChargeFunction.SendTransactionAsync(senderAddress, gas, null, 20);
            tx = await addChargeFunction.SendTransactionAsync(senderAddress, gas, null, 30);
            var pollingService = (TransactionReceiptPollingService) web3.TransactionManager.TransactionReceiptService;
            //CI is too slow
            receipt = await pollingService.PollForReceiptAsync(tx, new CancellationTokenSource(30000).Token);

            var chargers = contract.GetFunction("getChargers");

            var result = await chargers.CallAsync<List<BigInteger>>();

            Assert.Equal(20, result[0]);
            Assert.Equal(30, result[1]);
        }
    }
}using Nethereum.Contracts.Standards.ENS;
using Nethereum.ENS.ENSRegistry.ContractDefinition;
using Nethereum.ENS.FIFSRegistrar.ContractDefinition;
using Nethereum.ENS.PublicResolver.ContractDefinition;
using Nethereum.Geth;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.Web3.Accounts;
using Nethereum.Web3.Accounts.Managed;
using Nethereum.XUnitEthereumClients;
using Xunit;
// ReSharper disable ConsiderUsingConfigureAwait
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.ENS.IntegrationTests.ENS
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ENSLocalTest
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ENSLocalTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        [Fact]
        public async void ShouldCreateEnsRegistarResolverAndRegiterandResolveANewAddress()
        {
            //Ignoring parity due to https://github.com/paritytech/parity-ethereum/issues/8675
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                //The address we want to resolve when using "test.eth"
                var addressToResolve = "0x12890D2cce102216644c59daE5baed380d84830c";


                var addressFrom = "0x12890D2cce102216644c59daE5baed380d84830c";

                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                //CI: slowing polling for CI
                web3.TransactionManager.TransactionReceiptService =
                    new TransactionReceiptPollingService(web3.TransactionManager, 500);


                //deploy ENS contract
                var ensDeploymentReceipt =
                    await ENSRegistryService.DeployContractAndWaitForReceiptAsync(web3, new ENSRegistryDeployment()).ConfigureAwait(false);

                var ensUtil = new EnsUtil();
                var ethNode = ensUtil.GetNameHash("eth");

                //create a new First in First service registrar for "eth"
                var fifsDeploymentReceipt = await FIFSRegistrarService.DeployContractAndWaitForReceiptAsync(web3,
                    new FIFSRegistrarDeployment()
                    {
                        EnsAddr = ensDeploymentReceipt.ContractAddress,
                        Node = ethNode.HexToByteArray()
                    }).ConfigureAwait(false);


                var publicResolverDeploymentReceipt = await PublicResolverService.DeployContractAndWaitForReceiptAsync(
                    web3,
                    new PublicResolverDeployment() { Ens = ensDeploymentReceipt.ContractAddress }
                ).ConfigureAwait(false);


                var ensRegistryService = new ENSRegistryService(web3, ensDeploymentReceipt.ContractAddress);

                //set ownership of "eth" to the fifs service
                //we are owners of "", so a subnode label "eth" will now be owned by the FIFS registar, which will allow to also to set ownership in Ens of further subnodes of Eth.
                var ethLabel = ensUtil.GetLabelHash("eth");

                var receipt = await ensRegistryService.SetSubnodeOwnerRequestAndWaitForReceiptAsync(
                    ensUtil.GetNameHash("").HexToByteArray(),
                    ethLabel.HexToByteArray(),
                    fifsDeploymentReceipt.ContractAddress
                ).ConfigureAwait(false);


                //Now the owner of Eth is the FIFS
                var ownerOfEth =
                    await ensRegistryService.OwnerQueryAsync(ethNode.HexToByteArray()).ConfigureAwait(false);
                Assert.Equal(fifsDeploymentReceipt.ContractAddress.ToLower(), ownerOfEth.ToLower());
                /**** setup done **/

                //registration of "myname"

                //create a service for the registrar
                var fifsService = new FIFSRegistrarService(web3, fifsDeploymentReceipt.ContractAddress);

                //create a label
                var testLabel = ensUtil.GetLabelHash("myname");
                //submit the registration using the label bytes, and set ourselves as the owner
                await fifsService.RegisterRequestAndWaitForReceiptAsync(new RegisterFunction()
                {
                    Owner = addressFrom,
                    Subnode = testLabel.HexToByteArray()
                }).ConfigureAwait(false);

                

                //now using the the full name
                var fullNameNode = ensUtil.GetNameHash("myname.eth");

                var ownerOfMyName =
                    await ensRegistryService.OwnerQueryAsync(fullNameNode.HexToByteArray()).ConfigureAwait(false);
                //set the resolver (the public one)
                await ensRegistryService.SetResolverRequestAndWaitForReceiptAsync(
                    new SetResolverFunction()
                    {

                        Resolver = publicResolverDeploymentReceipt.ContractAddress,
                        Node = fullNameNode.HexToByteArray()
                    }).ConfigureAwait(false);


                var publicResolverService =
                    new PublicResolverService(web3, publicResolverDeploymentReceipt.ContractAddress);
                // set the address in the resolver which we want to resolve, ownership is validated using ENS in the background

                //Fails here
                await publicResolverService.SetAddrRequestAndWaitForReceiptAsync(fullNameNode.HexToByteArray(),
                    addressToResolve
                ).ConfigureAwait(false);


                //Now as "end user" we can start resolving... 

                //get the resolver address from ENS
                var resolverAddress = await ensRegistryService.ResolverQueryAsync(
                    fullNameNode.HexToByteArray()).ConfigureAwait(false);

                //using the resolver address we can create our service (should be an abstract / interface based on abi as we can have many)
                var resolverService = new PublicResolverService(web3, resolverAddress);

                //and get the address from the resolver
                var theAddress =
                    await resolverService.AddrQueryAsync(fullNameNode.HexToByteArray()).ConfigureAwait(false);

                Assert.Equal(addressToResolve.ToLower(), theAddress.ToLower());
            }

        }
    }
}
using System.Linq;
using Multiformats.Codec;
using Multiformats.Hash;
using Nethereum.Contracts.Standards.ENS;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.ENS.IntegrationTests.ENS
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ENSMainNetTest
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ENSMainNetTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }
        public async void ShouldBeAbleToRegisterExample()
        {
            var durationInDays = 365;
            var ourName = "lllalalalal"; //enter owner name
            var tls = "eth";
            var owner = "0x111F530216fBB0377B4bDd4d303a465a1090d09d";
            var secret = "Today is gonna be the day That theyre gonna throw it back to you"; //make your own


            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ethTLSService = new EthTLSService(web3);
            await ethTLSService.InitialiseAsync().ConfigureAwait(false);

            var price = await ethTLSService.CalculateRentPriceInEtherAsync(ourName, durationInDays).ConfigureAwait(false);
            Assert.True(price > 0);

            var commitment = await ethTLSService.CalculateCommitmentAsync(ourName, owner, secret).ConfigureAwait(false);
            var commitTransactionReceipt = await ethTLSService.CommitRequestAndWaitForReceiptAsync(commitment).ConfigureAwait(false);
            var txnHash = await ethTLSService.RegisterRequestAsync(ourName, owner, durationInDays, secret, price).ConfigureAwait(false);
        }

        public async void ShouldBeAbleToSetTextExample()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = new ENSService(web3.Eth);
            var txn = await ensService.SetTextRequestAsync("nethereum.eth", (Contracts.Standards.ENS.TextDataKey)TextDataKey.url, "https://nethereum.com").ConfigureAwait(false);
        }

        [Fact]
        public async void ShouldBeAbleToResolveText()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = new ENSService(web3.Eth);
            var url = await ensService.ResolveTextAsync("nethereum.eth", (Contracts.Standards.ENS.TextDataKey)TextDataKey.url).ConfigureAwait(false);
            Assert.Equal("https://nethereum.com", url);
        }

        [Fact]
        public async void ShouldBeAbleToCalculateRentPriceAndCommitment()
        {
            var durationInDays = 365;
            var ourName = "supersillynameformonkeys";
            var tls = "eth";
            var owner = "0x12890D2cce102216644c59daE5baed380d84830c";
            var secret = "animals in the forest";


            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ethTLSService = new EthTLSService(web3);
            await ethTLSService.InitialiseAsync().ConfigureAwait(false);

            var price = await ethTLSService.CalculateRentPriceInEtherAsync(ourName, durationInDays).ConfigureAwait(false);
            Assert.True(price > 0);

            var commitment = await ethTLSService.CalculateCommitmentAsync(ourName, owner, secret).ConfigureAwait(false);
            Assert.Equal("0x546d078db03381f4a33a33600cf1b91e00815b572c944f4a19624c8d9aaa9c14", commitment.ToHex(true));
        }


        [Fact]
        public async void ShouldFindEthControllerFromMainnet()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ethTLSService = new EthTLSService(web3);
            await ethTLSService.InitialiseAsync().ConfigureAwait(false);
            var controllerAddress = ethTLSService.TLSControllerAddress;
            Assert.True("0x283Af0B28c62C092C9727F1Ee09c02CA627EB7F5".IsTheSameAddress(controllerAddress));
            
        }

        [Fact]
        public async void ShouldResolveAddressFromMainnet()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = new ENSService(web3.Eth);
            var theAddress = await ensService.ResolveAddressAsync("nick.eth").ConfigureAwait(false);     
            var expectedAddress = "0xb8c2C29ee19D8307cb7255e1Cd9CbDE883A267d5";
            Assert.True(expectedAddress.IsTheSameAddress(theAddress));   
        }

        //Food for thought, a simple CID just using IPFS Base58 Defaulting all other values / Swarm
        [Fact]
        public async void ShouldRetrieveTheContentHashAndDecodeIt()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = new ENSService(web3.Eth);
            var content = await ensService.GetContentHashAsync("3-7-0-0.web3.nethereum.dotnet.netdapps.eth").ConfigureAwait(false);
            var storage = content[0];
            //This depends on IPLD.ContentIdentifier, Multiformats Hash and Codec
            if (storage == 0xe3) // if storage is IPFS 
            {
                //We skip 2 storage ++
                var cid = IPLD.ContentIdentifier.Cid.Cast(content.Skip(2).ToArray());
                var decoded = cid.Hash.B58String();
                Assert.Equal("QmRZiL8WbAVQMF1715fhG3b4x9tfGS6hgBLPQ6KYfKzcYL", decoded);
            }
          
        }

        [Fact]
        public async void ShouldCreateContentIPFSHash()
        {
            var multihash = Multihash.FromB58String("QmRZiL8WbAVQMF1715fhG3b4x9tfGS6hgBLPQ6KYfKzcYL");
            var cid = new IPLD.ContentIdentifier.Cid(MulticodecCode.MerkleDAGProtobuf, multihash);
            var ipfsStoragePrefix = new byte[] { 0xe3, 0x01 };
            var fullContentHash = ipfsStoragePrefix.Concat(cid.ToBytes()).ToArray();
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = new ENSService(web3.Eth);
            var content = await ensService.GetContentHashAsync("3-7-0-0.web3.nethereum.dotnet.netdapps.eth").ConfigureAwait(false);
            //e301017012202febb4a7c84c8079f78844e50150d97ad33e2a3a0d680d54e7211e30ef13f08d
            Assert.Equal(content.ToHex(), fullContentHash.ToHex());
        }

        //[Fact]
        public async void ShouldSetSubnodeExample()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = new ENSService(web3.Eth);
            var txn = await ensService.SetSubnodeOwnerRequestAsync("yoursupername.eth", "subdomainName", "addressOwner").ConfigureAwait(false);
        }

        //[Fact]
        public async void ShouldReverseResolveAddressFromMainnet()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = new ENSService(web3.Eth);
            var name = await ensService.ReverseResolveAsync("0xd1220a0cf47c7b9be7a2e6ba89f429762e7b9adb").ConfigureAwait(false);
            var expectedName = "alex.vandesande.eth";
            Assert.Equal(expectedName, name);
        }

    }
}﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ADRaffy.ENSNormalize;
using Multiformats.Codec;
using Multiformats.Hash;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.Services;
using Nethereum.Contracts.Standards.ENS;

using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Contracts.IntegrationTests.SmartContracts.Standards
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ENSMainNetTest
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ENSMainNetTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        public async void ShouldBeAbleToRegisterExample()
        {
            var durationInDays = 365;
            var ourName = "lllalalalal"; //enter owner name
            var tls = "eth";
            var owner = "0x111F530216fBB0377B4bDd4d303a465a1090d09d";
            var secret = "Today is gonna be the day That theyre gonna throw it back to you"; //make your own


            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ethTLSService = web3.Eth.GetEnsEthTlsService();
            await ethTLSService.InitialiseAsync().ConfigureAwait(false);

            var price = await ethTLSService.CalculateRentPriceInEtherAsync(ourName, durationInDays).ConfigureAwait(false);
            Assert.True(price > 0);

            var commitment = await ethTLSService.CalculateCommitmentAsync(ourName, owner, secret).ConfigureAwait(false);
            var commitTransactionReceipt = await ethTLSService.CommitRequestAndWaitForReceiptAsync(commitment).ConfigureAwait(false);
            var txnHash = await ethTLSService.RegisterRequestAsync(ourName, owner, durationInDays, secret, price).ConfigureAwait(false);
        }

        public async void ShouldBeAbleToSetTextExample()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var txn = await ensService.SetTextRequestAsync("nethereum.eth", TextDataKey.url, "https://nethereum.com").ConfigureAwait(false);
        }

        //[Fact]
        //public async void ShouldBeAbleToResolveText()
        //{
        //    var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
        //    var ensService = web3.Eth.GetEnsService();
        //    var url = await ensService.ResolveTextAsync("nethereum.eth", TextDataKey.url).ConfigureAwait(false);
        //    Assert.Equal("https://nethereum.com", url);
        //}

        [Fact]
        public async void ShouldBeAbleToCalculateRentPriceAndCommitment()
        {
            var durationInDays = 365;
            var ourName = "supersillynameformonkeys";
            var tls = "eth";
            var owner = "0x12890D2cce102216644c59daE5baed380d84830c";
            var secret = "animals in the forest";


            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ethTLSService = web3.Eth.GetEnsEthTlsService();
            await ethTLSService.InitialiseAsync().ConfigureAwait(false);

            var price = await ethTLSService.CalculateRentPriceInEtherAsync(ourName, durationInDays).ConfigureAwait(false);
            Assert.True(price > 0);

            var commitment = await ethTLSService.CalculateCommitmentAsync(ourName, owner, secret).ConfigureAwait(false);
            Assert.Equal("0x546d078db03381f4a33a33600cf1b91e00815b572c944f4a19624c8d9aaa9c14", commitment.ToHex(true));
        }


        [Fact]
        public async void ShouldFindEthControllerFromMainnet()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ethTLSService = web3.Eth.GetEnsEthTlsService();
            await ethTLSService.InitialiseAsync().ConfigureAwait(false);
            var controllerAddress = ethTLSService.TLSControllerAddress;
            Assert.True("0x283Af0B28c62C092C9727F1Ee09c02CA627EB7F5".IsTheSameAddress(controllerAddress));

        }

        [Fact]
        public async void ShouldResolveAddressFromMainnet()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var theAddress = await ensService.ResolveAddressAsync("nick.eth").ConfigureAwait(false);
            var expectedAddress = "0xb8c2C29ee19D8307cb7255e1Cd9CbDE883A267d5";
            Assert.True(expectedAddress.IsTheSameAddress(theAddress));
        }

        //Food for thought, a simple CID just using IPFS Base58 Defaulting all other values / Swarm
        [Fact]
        public async void ShouldRetrieveTheContentHashAndDecodeIt()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var content = await ensService.GetContentHashAsync("3-7-0-0.web3.nethereum.dotnet.netdapps.eth").ConfigureAwait(false);
            var storage = content[0];
            //This depends on IPLD.ContentIdentifier, Multiformats Hash and Codec
            if (storage == 0xe3) // if storage is IPFS 
            {
                //We skip 2 storage ++
                var cid = IPLD.ContentIdentifier.Cid.Cast(content.Skip(2).ToArray());
                var decoded = cid.Hash.B58String();
                Assert.Equal("QmRZiL8WbAVQMF1715fhG3b4x9tfGS6hgBLPQ6KYfKzcYL", decoded);
            }

        }

        [Fact]
        public async void ShouldCreateContentIPFSHash()
        {
            var multihash = Multihash.FromB58String("QmRZiL8WbAVQMF1715fhG3b4x9tfGS6hgBLPQ6KYfKzcYL");
            var cid = new IPLD.ContentIdentifier.Cid(MulticodecCode.MerkleDAGProtobuf, multihash);
            var ipfsStoragePrefix = new byte[] {0xe3, 0x01};
            var fullContentHash = ipfsStoragePrefix.Concat(cid.ToBytes()).ToArray();
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var content = await ensService.GetContentHashAsync("3-7-0-0.web3.nethereum.dotnet.netdapps.eth").ConfigureAwait(false);
            //e301017012202febb4a7c84c8079f78844e50150d97ad33e2a3a0d680d54e7211e30ef13f08d
            Assert.Equal(content.ToHex(), fullContentHash.ToHex());
        }

        //[Fact]
        public async void ShouldSetSubnodeExample()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var txn = await ensService.SetSubnodeOwnerRequestAsync("yoursupername.eth", "subdomainName",
                "addressOwner").ConfigureAwait(false);
        }

        [Fact]
        public async void ShouldReverseResolveAddressFromMainnet()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var name = await ensService.ReverseResolveAsync("0xd1220a0cf47c7b9be7a2e6ba89f429762e7b9adb").ConfigureAwait(false);
            var expectedName = "alex.vandesande.eth";
            Assert.Equal(expectedName, name);
        }


        [Fact]
        public async void ShouldResolveAddressFromMainnetEmoji()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var theAddress = await ensService.ResolveAddressAsync("💩💩💩.eth").ConfigureAwait(false);
            var expectedAddress = "0x372973309f827B5c3864115cE121c96ef9cB1658";
            Assert.True(expectedAddress.IsTheSameAddress(theAddress));
        }


        [Fact]
        public async void ShouldNormaliseAsciiDomain()
        {
            var input = "foo.eth"; // latin chars only
            var expected = "foo.eth";
            var output = new EnsUtil().Normalise(input);
            Assert.Equal(expected, output);
        }


        [Fact]
        public void ShouldNotNormaliseMixtureOfCharactersDomain()
        {
            var input = "fоо.eth"; // with cyrillic 'o'
            var expected = "fоо.eth";

            Assert.Throws<InvalidLabelException>(() =>
                    new EnsUtil().Normalise(input));
            //Invalid label "fоо‎": illegal mixture: Latin + Cyrillic о‎ {43E}
        }

        [Fact]
        public void ShouldNormaliseToLowerDomain()
        {
            var input = "Foo.eth"; 
            var expected = "foo.eth";
            var output = new EnsUtil().Normalise(input);
            Assert.Equal(expected, output);
        }

        [Fact]
        public void ShouldNormaliseEmojiDomain()
        {
            var input = "🦚.eth";
            var expected = "🦚.eth";
            var output = new EnsUtil().Normalise(input);
            Assert.Equal(expected, output);
        }

        [Fact]
        public async void ShouldResolveAddressOffline()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);        
            var ensService = web3.Eth.GetEnsService();
            var theAddress = await ensService.ResolveAddressAsync("1.offchainexample.eth").ConfigureAwait(false);
            var expected = "0x41563129cDbbD0c5D3e1c86cf9563926b243834d";
            Assert.True(expected.IsTheSameAddress(theAddress));
        }


        [Fact]
        public async void ShouldResolveEmailOffline()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var theAddress = await ensService.ResolveTextAsync("1.offchainexample.eth", TextDataKey.email).ConfigureAwait(false);
            var expected = "nick@ens.domains";
            Assert.True(expected.IsTheSameAddress(theAddress));
        }

        [Fact]
        public async void ShouldResolveDescriptionOffline()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var description = await ensService.ResolveTextAsync("1.offchainexample.eth", TextDataKey.description).ConfigureAwait(false);
            var expected = "hello offchainresolver wildcard record";
            Assert.True(expected.IsTheSameAddress(description));
        }

        [Fact]
        public async void ShouldResolveAddressOfflineMatoken()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var theAddress = await ensService.ResolveAddressAsync("matoken.lens.xyz").ConfigureAwait(false);
            var expected = "0x5A384227B65FA093DEC03Ec34e111Db80A040615";
            Assert.True(expected.IsTheSameAddress(theAddress));
        }


        [Fact]
        public async void ShouldReverseResolveAddressMatoken()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var ensService = web3.Eth.GetEnsService();
            var addressToResolve = "0x5A384227B65FA093DEC03Ec34e111Db80A040615";
            var reverse = await ensService.ReverseResolveAsync(addressToResolve);
            var address = await ensService.ResolveAddressAsync(reverse).ConfigureAwait(false);
            Assert.True(address.IsTheSameAddress(addressToResolve));
        }


    }
}
﻿using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.DebugNode.Dtos;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.XUnitEthereumClients;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using System.Threading;
using Nethereum.ABI;
using System.Linq;
// ReSharper disable ConsiderUsingConfigureAwait  
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EVM
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class Erc20EVMContractSimulatorAndStorage
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public Erc20EVMContractSimulatorAndStorage(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        [Fact]
        public async void ShouldCalculateBalanceSlot()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var contractAddress = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var senderAddress = "0x0000000000000000000000000000000000000001";
            var simulator = new Nethereum.EVM.Contracts.ERC20.ERC20Simulator(web3, 1, contractAddress);
            var slot = await simulator.CalculateMappingBalanceSlotAsync(senderAddress, 100);
            Assert.Equal(9, slot);
        }

        [Fact]
        public async void ShouldSimulateTransferAndBalanceState()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var contractAddress = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var senderAddress = "0x0000000000000000000000000000000000000001";
            var receiverAddress = "0x0000000000000000000000000000000000000025";
            var simulator = new Nethereum.EVM.Contracts.ERC20.ERC20Simulator(web3, 1, contractAddress);
            var simulationResult = await simulator.SimulateTransferAndBalanceStateAsync(senderAddress, receiverAddress, 100);
            Assert.Equal(simulationResult.BalanceSenderAfter, simulationResult.BalanceSenderBefore - 100);
            Assert.Equal(simulationResult.BalanceSenderStorageAfter, simulationResult.BalanceSenderBefore - 100);
            Assert.Equal(simulationResult.BalanceReceiverAfter, simulationResult.BalanceReceiverBefore + 100);
            Assert.Equal(simulationResult.BalanceReceiverStorageAfter, simulationResult.BalanceReceiverBefore + 100);

        }



        [Fact]
        public async void ShouldBeAbleToGetBalanceFromStorage()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var contractAddress = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var senderAddress = "0x0000000000000000000000000000000000000001";
            var balanceStorage = await web3.Eth.ERC20.GetContractService(contractAddress).GetBalanceFromStorageAsync(senderAddress, 9);
            var balanceSmartContract = await web3.Eth.ERC20.GetContractService(contractAddress).BalanceOfQueryAsync(senderAddress);
            Assert.Equal(balanceSmartContract, balanceStorage);
        }

      
    }

}
       
﻿using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.Decoders;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Services;
using Nethereum.StandardTokenEIP20.ContractDefinition;
using Nethereum.StandardTokenEIP20.Events.DTO;
using Nethereum.XUnitEthereumClients;
using Xunit;
// ReSharper disable once ConsiderUsingConfigureAwait

namespace Nethereum.StandardTokenEIP20.IntegrationTests
{

    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class Erc20TokenTester
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public Erc20TokenTester(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        private static async Task<TransactionReceipt> GetTransactionReceiptAsync(
            EthApiTransactionsService transactionService, string transactionHash)
        {
            TransactionReceipt receipt = null;
            //wait for the contract to be mined to the address
            while (receipt == null)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                receipt = await transactionService.GetTransactionReceipt.SendRequestAsync(transactionHash).ConfigureAwait(false);
            }
            return receipt;
        }

        [Fact]
        public async void ShouldGetTheDaiFromMainnet()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var contractHandler = web3.Eth.GetContractHandler("0x89d24A6b4CcB1B6fAA2625fE562bDD9a23260359");
            var stringBytes32Decoder = new StringBytes32Decoder();
            var symbol = await contractHandler.QueryRawAsync<SymbolFunction, StringBytes32Decoder, string>().ConfigureAwait(false);
            var token = await contractHandler.QueryRawAsync<NameFunction, StringBytes32Decoder, string>().ConfigureAwait(false);
            
        }

        [Fact]
        public async void ShouldReturnData()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentHandler =  web3.Eth.GetContractDeploymentHandler<EIP20Deployment>();
            var receipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(new EIP20Deployment()
            {
                DecimalUnits = 18,
                InitialAmount = BigInteger.Parse("10000000000000000000000000"),
                TokenSymbol = "XST",
                TokenName = "XomeStandardToken"
            }).ConfigureAwait(false);

            var contractHandler = web3.Eth.GetContractHandler(receipt.ContractAddress);
            var symbol = await contractHandler.QueryRawAsync<SymbolFunction, StringBytes32Decoder, string>().ConfigureAwait(false);
            var token = await contractHandler.QueryRawAsync<NameFunction, StringBytes32Decoder, string>().ConfigureAwait(false);

            Assert.Equal("XST", symbol);
            Assert.Equal("XomeStandardToken", token);
        }

        [Fact]
        public async void Test()
        {
            var addressOwner = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
     
            ulong totalSupply = 1000000;
            var newAddress = "0x12890d2cce102216644c59daE5baed380d84830e";

            var deploymentContract = new EIP20Deployment()
            {
                InitialAmount = totalSupply,
                TokenName = "TestToken",
                TokenSymbol = "TST"
            };

            var tokenService = await StandardTokenService.DeployContractAndGetServiceAsync(web3, deploymentContract).ConfigureAwait(false);
            
            var transfersEvent = tokenService.GetTransferEvent();

            var totalSupplyDeployed = await tokenService.TotalSupplyQueryAsync().ConfigureAwait(false);
            Assert.Equal(totalSupply, totalSupplyDeployed);

            var tokenName = await tokenService.NameQueryAsync().ConfigureAwait(false);
            Assert.Equal("TestToken", tokenName);

            var tokenSymbol = await tokenService.SymbolQueryAsync().ConfigureAwait(false);
            Assert.Equal("TST", tokenSymbol);

            var ownerBalance = await tokenService.BalanceOfQueryAsync(addressOwner).ConfigureAwait(false);
            Assert.Equal(totalSupply, ownerBalance);

            var transferReceipt =
                await tokenService.TransferRequestAndWaitForReceiptAsync(newAddress, 1000).ConfigureAwait(false);

            ownerBalance = await tokenService.BalanceOfQueryAsync(addressOwner).ConfigureAwait(false);
            Assert.Equal(totalSupply - 1000, ownerBalance);

            var newAddressBalance = await tokenService.BalanceOfQueryAsync(newAddress).ConfigureAwait(false);
            Assert.Equal(1000, newAddressBalance);

            var allTransfersFilter =
                await transfersEvent.CreateFilterAsync(new BlockParameter(transferReceipt.BlockNumber)).ConfigureAwait(false);
            var eventLogsAll = await transfersEvent.GetAllChangesAsync(allTransfersFilter).ConfigureAwait(false);
            Assert.Single(eventLogsAll);
            var transferLog = eventLogsAll.First();
            Assert.Equal(transferLog.Log.TransactionIndex.HexValue, transferReceipt.TransactionIndex.HexValue);
            Assert.Equal(transferLog.Log.BlockNumber.HexValue, transferReceipt.BlockNumber.HexValue);
            Assert.Equal(transferLog.Event.To.ToLower(), newAddress.ToLower());
            Assert.Equal(transferLog.Event.Value, (ulong) 1000);

            var approveTransactionReceipt = await tokenService.ApproveRequestAndWaitForReceiptAsync(newAddress, 1000).ConfigureAwait(false);
            var allowanceAmount = await tokenService.AllowanceQueryAsync(addressOwner, newAddress).ConfigureAwait(false);
            Assert.Equal(1000, allowanceAmount);
        }
    }
}﻿using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.Standards.ERC721.ContractDefinition;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
//using System.Threading.Tasks.Dataflow;
using Xunit;

namespace Nethereum.Contracts.IntegrationTests.SmartContracts.Standards
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ERC721Tests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ERC721Tests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        public partial class MyERC721Deployment : MyERC721DeploymentBase
        {
            public MyERC721Deployment() : base(BYTECODE)
            {
            }

            public MyERC721Deployment(string byteCode) : base(byteCode)
            {
            }
        }

        public class MyERC721DeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "6101406040523480156200001257600080fd5b506040516200332a3803806200332a8339810160408190526200003591620002ff565b81604051806040016040528060018152602001603160f81b815250838381600090805190602001906200006a9291906200018c565b508051620000809060019060208401906200018c565b5050600b805460ff1916905550620000983362000132565b815160208084019190912082518383012060e08290526101008190524660a0818152604080517f8b73c3c69bb8fe3d512ecc4cf759cc79239f7b179b0ffacaa9a75d522b39400f81880181905281830187905260608201869052608082019490945230818401528151808203909301835260c00190528051940193909320919290916080523060c0526101205250620003a5945050505050565b600b80546001600160a01b03838116610100818102610100600160a81b031985161790945560405193909204169182907f8be0079c531659141344cd1fd0a4f28419497f9722a3daafe3b4186f6b6457e090600090a35050565b8280546200019a9062000369565b90600052602060002090601f016020900481019282620001be576000855562000209565b82601f10620001d957805160ff191683800117855562000209565b8280016001018555821562000209579182015b8281111562000209578251825591602001919060010190620001ec565b50620002179291506200021b565b5090565b5b808211156200021757600081556001016200021c565b634e487b7160e01b600052604160045260246000fd5b600082601f8301126200025a57600080fd5b81516001600160401b038082111562000277576200027762000232565b604051601f8301601f19908116603f01168101908282118183101715620002a257620002a262000232565b81604052838152602092508683858801011115620002bf57600080fd5b600091505b83821015620002e35785820183015181830184015290820190620002c4565b83821115620002f55760008385830101525b9695505050505050565b600080604083850312156200031357600080fd5b82516001600160401b03808211156200032b57600080fd5b620003398683870162000248565b935060208501519150808211156200035057600080fd5b506200035f8582860162000248565b9150509250929050565b600181811c908216806200037e57607f821691505b6020821081036200039f57634e487b7160e01b600052602260045260246000fd5b50919050565b60805160a05160c05160e0516101005161012051612f35620003f560003960006111a4015260006111f3015260006111ce01526000611127015260006111510152600061117b0152612f356000f3fe608060405234801561001057600080fd5b50600436106101f05760003560e01c80636352211e1161010f5780639ab24eb0116100a2578063c87b56dd11610071578063c87b56dd14610416578063d204c45e14610429578063e985e9c51461043c578063f2fde38b1461047857600080fd5b80639ab24eb0146103ca578063a22cb465146103dd578063b88d4fde146103f0578063c3cda5201461040357600080fd5b80638456cb59116100de5780638456cb59146103915780638da5cb5b146103995780638e539e8c146103af57806395d89b41146103c257600080fd5b80636352211e1461035057806370a0823114610363578063715018a6146103765780637ecebe001461037e57600080fd5b80633a46b1a8116101875780634f6ccce7116101565780634f6ccce7146102f3578063587cde1e146103065780635c19a95c146103325780635c975abb1461034557600080fd5b80633a46b1a8146102b25780633f4ba83a146102c557806342842e0e146102cd57806342966c68146102e057600080fd5b806318160ddd116101c357806318160ddd1461027257806323b872dd146102845780632f745c59146102975780633644e515146102aa57600080fd5b806301ffc9a7146101f557806306fdde031461021d578063081812fc14610232578063095ea7b31461025d575b600080fd5b610208610203366004612915565b61048b565b60405190151581526020015b60405180910390f35b61022561049c565b604051610214919061298a565b61024561024036600461299d565b61052e565b6040516001600160a01b039091168152602001610214565b61027061026b3660046129d2565b6105bb565b005b6008545b604051908152602001610214565b6102706102923660046129fc565b6106d0565b6102766102a53660046129d2565b610702565b610276610798565b6102766102c03660046129d2565b6107a7565b6102706107d0565b6102706102db3660046129fc565b61080a565b6102706102ee36600461299d565b610825565b61027661030136600461299d565b61089f565b610245610314366004612a38565b6001600160a01b039081166000908152600c60205260409020541690565b610270610340366004612a38565b610932565b600b5460ff16610208565b61024561035e36600461299d565b610941565b610276610371366004612a38565b6109b8565b610270610a3f565b61027661038c366004612a38565b610a79565b610270610a97565b600b5461010090046001600160a01b0316610245565b6102766103bd36600461299d565b610acf565b610225610b2b565b6102766103d8366004612a38565b610b3a565b6102706103eb366004612a53565b610b5b565b6102706103fe366004612b1b565b610b66565b610270610411366004612b97565b610b9e565b61022561042436600461299d565b610ccb565b610270610437366004612bf7565b610cd6565b61020861044a366004612c59565b6001600160a01b03918216600090815260056020908152604080832093909416825291909152205460ff1690565b610270610486366004612a38565b610d35565b600061049682610dd3565b92915050565b6060600080546104ab90612c8c565b80601f01602080910402602001604051908101604052809291908181526020018280546104d790612c8c565b80156105245780601f106104f957610100808354040283529160200191610524565b820191906000526020600020905b81548152906001019060200180831161050757829003601f168201915b5050505050905090565b600061053982610df8565b61059f5760405162461bcd60e51b815260206004820152602c60248201527f4552433732313a20617070726f76656420717565727920666f72206e6f6e657860448201526b34b9ba32b73a103a37b5b2b760a11b60648201526084015b60405180910390fd5b506000908152600460205260409020546001600160a01b031690565b60006105c682610941565b9050806001600160a01b0316836001600160a01b0316036106335760405162461bcd60e51b815260206004820152602160248201527f4552433732313a20617070726f76616c20746f2063757272656e74206f776e656044820152603960f91b6064820152608401610596565b336001600160a01b038216148061064f575061064f813361044a565b6106c15760405162461bcd60e51b815260206004820152603860248201527f4552433732313a20617070726f76652063616c6c6572206973206e6f74206f7760448201527f6e6572206e6f7220617070726f76656420666f7220616c6c00000000000000006064820152608401610596565b6106cb8383610e15565b505050565b6106db335b82610e83565b6106f75760405162461bcd60e51b815260040161059690612cc0565b6106cb838383610f6d565b600061070d836109b8565b821061076f5760405162461bcd60e51b815260206004820152602b60248201527f455243373231456e756d657261626c653a206f776e657220696e646578206f7560448201526a74206f6620626f756e647360a81b6064820152608401610596565b506001600160a01b03919091166000908152600660209081526040808320938352929052205490565b60006107a261111a565b905090565b6001600160a01b0382166000908152600d602052604081206107c99083611241565b9392505050565b600b546001600160a01b036101009091041633146108005760405162461bcd60e51b815260040161059690612d11565b610808611350565b565b6106cb83838360405180602001604052806000815250610b66565b61082e336106d5565b6108935760405162461bcd60e51b815260206004820152603060248201527f4552433732314275726e61626c653a2063616c6c6572206973206e6f74206f7760448201526f1b995c881b9bdc88185c1c1c9bdd995960821b6064820152608401610596565b61089c816113e3565b50565b60006108aa60085490565b821061090d5760405162461bcd60e51b815260206004820152602c60248201527f455243373231456e756d657261626c653a20676c6f62616c20696e646578206f60448201526b7574206f6620626f756e647360a01b6064820152608401610596565b6008828154811061092057610920612d46565b90600052602060002001549050919050565b3361093d81836113ec565b5050565b6000818152600260205260408120546001600160a01b0316806104965760405162461bcd60e51b815260206004820152602960248201527f4552433732313a206f776e657220717565727920666f72206e6f6e657869737460448201526832b73a103a37b5b2b760b91b6064820152608401610596565b60006001600160a01b038216610a235760405162461bcd60e51b815260206004820152602a60248201527f4552433732313a2062616c616e636520717565727920666f7220746865207a65604482015269726f206164647265737360b01b6064820152608401610596565b506001600160a01b031660009081526003602052604090205490565b600b546001600160a01b03610100909104163314610a6f5760405162461bcd60e51b815260040161059690612d11565b610808600061145e565b6001600160a01b0381166000908152600f6020526040812054610496565b600b546001600160a01b03610100909104163314610ac75760405162461bcd60e51b815260040161059690612d11565b6108086114b8565b6000438210610b205760405162461bcd60e51b815260206004820152601a60248201527f566f7465733a20626c6f636b206e6f7420796574206d696e65640000000000006044820152606401610596565b610496600e83611241565b6060600180546104ab90612c8c565b6001600160a01b0381166000908152600d6020526040812061049690611533565b61093d33838361158f565b610b703383610e83565b610b8c5760405162461bcd60e51b815260040161059690612cc0565b610b988484848461165d565b50505050565b83421115610bee5760405162461bcd60e51b815260206004820152601860248201527f566f7465733a207369676e6174757265206578706972656400000000000000006044820152606401610596565b604080517fe48329057bfd03d55e49b547132e39cffd9c1820ad7b9d4c5307691425d15adf60208201526001600160a01b038816918101919091526060810186905260808101859052600090610c6890610c609060a00160405160208183030381529060405280519060200120611690565b8585856116de565b9050610c7381611706565b8614610cb85760405162461bcd60e51b8152602060048201526014602482015273566f7465733a20696e76616c6964206e6f6e636560601b6044820152606401610596565b610cc281886113ec565b50505050505050565b60606104968261172e565b600b546001600160a01b03610100909104163314610d065760405162461bcd60e51b815260040161059690612d11565b6000610d1160105490565b9050610d21601080546001019055565b610d2b838261189c565b6106cb81836118b6565b600b546001600160a01b03610100909104163314610d655760405162461bcd60e51b815260040161059690612d11565b6001600160a01b038116610dca5760405162461bcd60e51b815260206004820152602660248201527f4f776e61626c653a206e6577206f776e657220697320746865207a65726f206160448201526564647265737360d01b6064820152608401610596565b61089c8161145e565b60006001600160e01b0319821663780e9d6360e01b1480610496575061049682611941565b6000908152600260205260409020546001600160a01b0316151590565b600081815260046020526040902080546001600160a01b0319166001600160a01b0384169081179091558190610e4a82610941565b6001600160a01b03167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b92560405160405180910390a45050565b6000610e8e82610df8565b610eef5760405162461bcd60e51b815260206004820152602c60248201527f4552433732313a206f70657261746f7220717565727920666f72206e6f6e657860448201526b34b9ba32b73a103a37b5b2b760a11b6064820152608401610596565b6000610efa83610941565b9050806001600160a01b0316846001600160a01b03161480610f355750836001600160a01b0316610f2a8461052e565b6001600160a01b0316145b80610f6557506001600160a01b0380821660009081526005602090815260408083209388168352929052205460ff165b949350505050565b826001600160a01b0316610f8082610941565b6001600160a01b031614610fe45760405162461bcd60e51b815260206004820152602560248201527f4552433732313a207472616e736665722066726f6d20696e636f72726563742060448201526437bbb732b960d91b6064820152608401610596565b6001600160a01b0382166110465760405162461bcd60e51b8152602060048201526024808201527f4552433732313a207472616e7366657220746f20746865207a65726f206164646044820152637265737360e01b6064820152608401610596565b611051838383611991565b61105c600082610e15565b6001600160a01b0383166000908152600360205260408120805460019290611085908490612d72565b90915550506001600160a01b03821660009081526003602052604081208054600192906110b3908490612d89565b909155505060008181526002602052604080822080546001600160a01b0319166001600160a01b0386811691821790925591518493918716917fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef91a46106cb8383836119e2565b6000306001600160a01b037f00000000000000000000000000000000000000000000000000000000000000001614801561117357507f000000000000000000000000000000000000000000000000000000000000000046145b1561119d57507f000000000000000000000000000000000000000000000000000000000000000090565b50604080517f00000000000000000000000000000000000000000000000000000000000000006020808301919091527f0000000000000000000000000000000000000000000000000000000000000000828401527f000000000000000000000000000000000000000000000000000000000000000060608301524660808301523060a0808401919091528351808403909101815260c0909201909252805191012090565b60004382106112925760405162461bcd60e51b815260206004820181905260248201527f436865636b706f696e74733a20626c6f636b206e6f7420796574206d696e65646044820152606401610596565b825460005b818110156112f75760006112ab82846119ed565b9050848660000182815481106112c3576112c3612d46565b60009182526020909120015463ffffffff1611156112e3578092506112f1565b6112ee816001612d89565b91505b50611297565b811561133b5784611309600184612d72565b8154811061131957611319612d46565b60009182526020909120015464010000000090046001600160e01b031661133e565b60005b6001600160e01b031695945050505050565b600b5460ff166113995760405162461bcd60e51b815260206004820152601460248201527314185d5cd8589b194e881b9bdd081c185d5cd95960621b6044820152606401610596565b600b805460ff191690557f5db9ee0a495bf2e6ff9c91a7834c1ba4fdd244a5e8aa4e537bd38aeae4b073aa335b6040516001600160a01b03909116815260200160405180910390a1565b61089c81611a08565b6001600160a01b038281166000818152600c602052604080822080548686166001600160a01b0319821681179092559151919094169392849290917f3134e8a2e6d97e929a7e54011ea5485d7d196dd5f0ba4d4ef95803e8e3fc257f9190a46106cb818361145986611a48565b611a53565b600b80546001600160a01b03838116610100818102610100600160a81b031985161790945560405193909204169182907f8be0079c531659141344cd1fd0a4f28419497f9722a3daafe3b4186f6b6457e090600090a35050565b600b5460ff16156114fe5760405162461bcd60e51b815260206004820152601060248201526f14185d5cd8589b194e881c185d5cd95960821b6044820152606401610596565b600b805460ff191660011790557f62e78cea01bee320cd4e420270b5ea74000d11b0c9f74754ebdbfc544b05a2586113c63390565b8054600090801561157c578261154a600183612d72565b8154811061155a5761155a612d46565b60009182526020909120015464010000000090046001600160e01b031661157f565b60005b6001600160e01b03169392505050565b816001600160a01b0316836001600160a01b0316036115f05760405162461bcd60e51b815260206004820152601960248201527f4552433732313a20617070726f766520746f2063616c6c6572000000000000006044820152606401610596565b6001600160a01b03838116600081815260056020908152604080832094871680845294825291829020805460ff191686151590811790915591519182527f17307eab39ab6107e8899845ad3d59bd9653f200f220920489ca2b5937696c31910160405180910390a3505050565b611668848484610f6d565b61167484848484611b90565b610b985760405162461bcd60e51b815260040161059690612da1565b600061049661169d61111a565b8360405161190160f01b6020820152602281018390526042810182905260009060620160405160208183030381529060405280519060200120905092915050565b60008060006116ef87878787611c8e565b915091506116fc81611d7b565b5095945050505050565b6001600160a01b0381166000908152600f602052604090208054600181018255905b50919050565b606061173982610df8565b61179f5760405162461bcd60e51b815260206004820152603160248201527f45524337323155524953746f726167653a2055524920717565727920666f72206044820152703737b732bc34b9ba32b73a103a37b5b2b760791b6064820152608401610596565b6000828152600a6020526040812080546117b890612c8c565b80601f01602080910402602001604051908101604052809291908181526020018280546117e490612c8c565b80156118315780601f1061180657610100808354040283529160200191611831565b820191906000526020600020905b81548152906001019060200180831161181457829003601f168201915b50505050509050600061184f60408051602081019091526000815290565b90508051600003611861575092915050565b81511561189357808260405160200161187b929190612df3565b60405160208183030381529060405292505050919050565b610f6584611f31565b61093d828260405180602001604052806000815250612008565b6118bf82610df8565b6119225760405162461bcd60e51b815260206004820152602e60248201527f45524337323155524953746f726167653a2055524920736574206f66206e6f6e60448201526d32bc34b9ba32b73a103a37b5b2b760911b6064820152608401610596565b6000828152600a6020908152604090912082516106cb92840190612834565b60006001600160e01b031982166380ac58cd60e01b148061197257506001600160e01b03198216635b5e139f60e01b145b8061049657506301ffc9a760e01b6001600160e01b0319831614610496565b600b5460ff16156119d75760405162461bcd60e51b815260206004820152601060248201526f14185d5cd8589b194e881c185d5cd95960821b6044820152606401610596565b6106cb83838361203b565b6106cb8383836120f3565b60006119fc6002848418612e38565b6107c990848416612d89565b611a11816120ff565b6000818152600a602052604090208054611a2a90612c8c565b15905061089c576000818152600a6020526040812061089c916128b4565b6000610496826109b8565b816001600160a01b0316836001600160a01b031614158015611a755750600081115b156106cb576001600160a01b03831615611b03576001600160a01b0383166000908152600d602052604081208190611ab0906121ae856121ba565b91509150846001600160a01b03167fdec2bacdd2f05b59de34da9b523dff8be42e5e38e818c82fdb0bae774387a7248383604051611af8929190918252602082015260400190565b60405180910390a250505b6001600160a01b038216156106cb576001600160a01b0382166000908152600d602052604081208190611b39906121e8856121ba565b91509150836001600160a01b03167fdec2bacdd2f05b59de34da9b523dff8be42e5e38e818c82fdb0bae774387a7248383604051611b81929190918252602082015260400190565b60405180910390a25050505050565b60006001600160a01b0384163b15611c8657604051630a85bd0160e11b81526001600160a01b0385169063150b7a0290611bd4903390899088908890600401612e4c565b6020604051808303816000875af1925050508015611c0f575060408051601f3d908101601f19168201909252611c0c91810190612e89565b60015b611c6c573d808015611c3d576040519150601f19603f3d011682016040523d82523d6000602084013e611c42565b606091505b508051600003611c645760405162461bcd60e51b815260040161059690612da1565b805181602001fd5b6001600160e01b031916630a85bd0160e11b149050610f65565b506001610f65565b6000807f7fffffffffffffffffffffffffffffff5d576e7357a4501ddfe92f46681b20a0831115611cc55750600090506003611d72565b8460ff16601b14158015611cdd57508460ff16601c14155b15611cee5750600090506004611d72565b6040805160008082526020820180845289905260ff881692820192909252606081018690526080810185905260019060a0016020604051602081039080840390855afa158015611d42573d6000803e3d6000fd5b5050604051601f1901519150506001600160a01b038116611d6b57600060019250925050611d72565b9150600090505b94509492505050565b6000816004811115611d8f57611d8f612ea6565b03611d975750565b6001816004811115611dab57611dab612ea6565b03611df85760405162461bcd60e51b815260206004820152601860248201527f45434453413a20696e76616c6964207369676e617475726500000000000000006044820152606401610596565b6002816004811115611e0c57611e0c612ea6565b03611e595760405162461bcd60e51b815260206004820152601f60248201527f45434453413a20696e76616c6964207369676e6174757265206c656e677468006044820152606401610596565b6003816004811115611e6d57611e6d612ea6565b03611ec55760405162461bcd60e51b815260206004820152602260248201527f45434453413a20696e76616c6964207369676e6174757265202773272076616c604482015261756560f01b6064820152608401610596565b6004816004811115611ed957611ed9612ea6565b0361089c5760405162461bcd60e51b815260206004820152602260248201527f45434453413a20696e76616c6964207369676e6174757265202776272076616c604482015261756560f01b6064820152608401610596565b6060611f3c82610df8565b611fa05760405162461bcd60e51b815260206004820152602f60248201527f4552433732314d657461646174613a2055524920717565727920666f72206e6f60448201526e3732bc34b9ba32b73a103a37b5b2b760891b6064820152608401610596565b6000611fb760408051602081019091526000815290565b90506000815111611fd757604051806020016040528060008152506107c9565b80611fe1846121f4565b604051602001611ff2929190612df3565b6040516020818303038152906040529392505050565b61201283836122f5565b61201f6000848484611b90565b6106cb5760405162461bcd60e51b815260040161059690612da1565b6001600160a01b0383166120965761209181600880546000838152600960205260408120829055600182018355919091527ff3f7a9fe364faab93b216da50a3214154f22a0a2b415b23a84c8169e8b636ee30155565b6120b9565b816001600160a01b0316836001600160a01b0316146120b9576120b9838261243c565b6001600160a01b0382166120d0576106cb816124d9565b826001600160a01b0316826001600160a01b0316146106cb576106cb8282612588565b6106cb838360016125cc565b600061210a82610941565b905061211881600084611991565b612123600083610e15565b6001600160a01b038116600090815260036020526040812080546001929061214c908490612d72565b909155505060008281526002602052604080822080546001600160a01b0319169055518391906001600160a01b038416907fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef908390a461093d816000846119e2565b60006107c98284612d72565b6000806121dc856121d76121cd88611533565b868863ffffffff16565b61263c565b91509150935093915050565b60006107c98284612d89565b60608160000361221b5750506040805180820190915260018152600360fc1b602082015290565b8160005b8115612245578061222f81612ebc565b915061223e9050600a83612e38565b915061221f565b60008167ffffffffffffffff81111561226057612260612a8f565b6040519080825280601f01601f19166020018201604052801561228a576020820181803683370190505b5090505b8415610f655761229f600183612d72565b91506122ac600a86612ed5565b6122b7906030612d89565b60f81b8183815181106122cc576122cc612d46565b60200101906001600160f81b031916908160001a9053506122ee600a86612e38565b945061228e565b6001600160a01b03821661234b5760405162461bcd60e51b815260206004820181905260248201527f4552433732313a206d696e7420746f20746865207a65726f20616464726573736044820152606401610596565b61235481610df8565b156123a15760405162461bcd60e51b815260206004820152601c60248201527f4552433732313a20746f6b656e20616c7265616479206d696e746564000000006044820152606401610596565b6123ad60008383611991565b6001600160a01b03821660009081526003602052604081208054600192906123d6908490612d89565b909155505060008181526002602052604080822080546001600160a01b0319166001600160a01b03861690811790915590518392907fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef908290a461093d600083836119e2565b60006001612449846109b8565b6124539190612d72565b6000838152600760205260409020549091508082146124a6576001600160a01b03841660009081526006602090815260408083208584528252808320548484528184208190558352600790915290208190555b5060009182526007602090815260408084208490556001600160a01b039094168352600681528383209183525290812055565b6008546000906124eb90600190612d72565b6000838152600960205260408120546008805493945090928490811061251357612513612d46565b90600052602060002001549050806008838154811061253457612534612d46565b600091825260208083209091019290925582815260099091526040808220849055858252812055600880548061256c5761256c612ee9565b6001900381819060005260206000200160009055905550505050565b6000612593836109b8565b6001600160a01b039093166000908152600660209081526040808320868452825280832085905593825260079052919091209190915550565b6001600160a01b0383166125eb576125e8600e6121e8836121ba565b50505b6001600160a01b03821661260a57612607600e6121ae836121ba565b50505b6001600160a01b038381166000908152600c60205260408082205485841683529120546106cb92918216911683611a53565b815460009081908161264d86611533565b905060008211801561268b57504386612667600185612d72565b8154811061267757612677612d46565b60009182526020909120015463ffffffff16145b156126eb5761269985612762565b866126a5600185612d72565b815481106126b5576126b5612d46565b9060005260206000200160000160046101000a8154816001600160e01b0302191690836001600160e01b03160217905550612759565b856000016040518060400160405280612703436127cf565b63ffffffff16815260200161271788612762565b6001600160e01b0390811690915282546001810184556000938452602093849020835194909301519091166401000000000263ffffffff909316929092179101555b95939450505050565b60006001600160e01b038211156127cb5760405162461bcd60e51b815260206004820152602760248201527f53616665436173743a2076616c756520646f65736e27742066697420696e20326044820152663234206269747360c81b6064820152608401610596565b5090565b600063ffffffff8211156127cb5760405162461bcd60e51b815260206004820152602660248201527f53616665436173743a2076616c756520646f65736e27742066697420696e203360448201526532206269747360d01b6064820152608401610596565b82805461284090612c8c565b90600052602060002090601f01602090048101928261286257600085556128a8565b82601f1061287b57805160ff19168380011785556128a8565b828001600101855582156128a8579182015b828111156128a857825182559160200191906001019061288d565b506127cb9291506128ea565b5080546128c090612c8c565b6000825580601f106128d0575050565b601f01602090049060005260206000209081019061089c91905b5b808211156127cb57600081556001016128eb565b6001600160e01b03198116811461089c57600080fd5b60006020828403121561292757600080fd5b81356107c9816128ff565b60005b8381101561294d578181015183820152602001612935565b83811115610b985750506000910152565b60008151808452612976816020860160208601612932565b601f01601f19169290920160200192915050565b6020815260006107c9602083018461295e565b6000602082840312156129af57600080fd5b5035919050565b80356001600160a01b03811681146129cd57600080fd5b919050565b600080604083850312156129e557600080fd5b6129ee836129b6565b946020939093013593505050565b600080600060608486031215612a1157600080fd5b612a1a846129b6565b9250612a28602085016129b6565b9150604084013590509250925092565b600060208284031215612a4a57600080fd5b6107c9826129b6565b60008060408385031215612a6657600080fd5b612a6f836129b6565b915060208301358015158114612a8457600080fd5b809150509250929050565b634e487b7160e01b600052604160045260246000fd5b600067ffffffffffffffff80841115612ac057612ac0612a8f565b604051601f8501601f19908116603f01168101908282118183101715612ae857612ae8612a8f565b81604052809350858152868686011115612b0157600080fd5b858560208301376000602087830101525050509392505050565b60008060008060808587031215612b3157600080fd5b612b3a856129b6565b9350612b48602086016129b6565b925060408501359150606085013567ffffffffffffffff811115612b6b57600080fd5b8501601f81018713612b7c57600080fd5b612b8b87823560208401612aa5565b91505092959194509250565b60008060008060008060c08789031215612bb057600080fd5b612bb9876129b6565b95506020870135945060408701359350606087013560ff81168114612bdd57600080fd5b9598949750929560808101359460a0909101359350915050565b60008060408385031215612c0a57600080fd5b612c13836129b6565b9150602083013567ffffffffffffffff811115612c2f57600080fd5b8301601f81018513612c4057600080fd5b612c4f85823560208401612aa5565b9150509250929050565b60008060408385031215612c6c57600080fd5b612c75836129b6565b9150612c83602084016129b6565b90509250929050565b600181811c90821680612ca057607f821691505b60208210810361172857634e487b7160e01b600052602260045260246000fd5b60208082526031908201527f4552433732313a207472616e736665722063616c6c6572206973206e6f74206f6040820152701ddb995c881b9bdc88185c1c1c9bdd9959607a1b606082015260800190565b6020808252818101527f4f776e61626c653a2063616c6c6572206973206e6f7420746865206f776e6572604082015260600190565b634e487b7160e01b600052603260045260246000fd5b634e487b7160e01b600052601160045260246000fd5b600082821015612d8457612d84612d5c565b500390565b60008219821115612d9c57612d9c612d5c565b500190565b60208082526032908201527f4552433732313a207472616e7366657220746f206e6f6e20455243373231526560408201527131b2b4bb32b91034b6b83632b6b2b73a32b960711b606082015260800190565b60008351612e05818460208801612932565b835190830190612e19818360208801612932565b01949350505050565b634e487b7160e01b600052601260045260246000fd5b600082612e4757612e47612e22565b500490565b6001600160a01b0385811682528416602082015260408101839052608060608201819052600090612e7f9083018461295e565b9695505050505050565b600060208284031215612e9b57600080fd5b81516107c9816128ff565b634e487b7160e01b600052602160045260246000fd5b600060018201612ece57612ece612d5c565b5060010190565b600082612ee457612ee4612e22565b500690565b634e487b7160e01b600052603160045260246000fdfea2646970667358221220820f375dbdbbacb4510f8a05836cdd93fc977f757ea9e6b6c50fe455cb6190e964736f6c634300080d0033";

            public MyERC721DeploymentBase() : base(BYTECODE)
            {
            }

            public MyERC721DeploymentBase(string byteCode) : base(byteCode)
            {
            }

            [Parameter("string", "name", 1)] public virtual string Name { get; set; }
            [Parameter("string", "symbol", 2)] public virtual string Symbol { get; set; }
        }



        [Fact]
        public async void ShouldDeployCustomAndQueryInteractWithGenericService()
        {

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            web3.Eth.TransactionManager.UseLegacyAsDefault = true;


            var erc721Deployment = new MyERC721Deployment() {Name = "Property Registry", Symbol = "PR"};

            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<MyERC721Deployment>()
                .SendRequestAndWaitForReceiptAsync(erc721Deployment).ConfigureAwait(false);

            //creating a new service with the new contract address
            var erc721Service = web3.Eth.ERC721.GetContractService(deploymentReceipt.ContractAddress);
            var url = "ipfs://bafkreiblz4ltiepapdhqhjiurmfpov7extmxwcntqskvx2zqisoftlmk7a";

            var addressToRegisterOwnership = "0xe612205919814b1995D861Bdf6C2fE2f20cDBd68";

            var mintReceipt =
                await erc721Service.SafeMintRequestAndWaitForReceiptAsync(web3.TransactionManager.Account.Address, url).ConfigureAwait(false);

            var ownerOfToken = await erc721Service.OwnerOfQueryAsync(0).ConfigureAwait(false);

            Assert.True(ownerOfToken.IsTheSameAddress(web3.TransactionManager.Account.Address));

            var addressOfToken = await erc721Service.TokenURIQueryAsync(0).ConfigureAwait(false);

            Assert.Equal(url, addressOfToken);

            var transfer =
                await erc721Service.TransferFromRequestAndWaitForReceiptAsync(ownerOfToken, addressToRegisterOwnership,
                    0).ConfigureAwait(false);
            Assert.False(transfer.HasErrors());

            ownerOfToken = await erc721Service.OwnerOfQueryAsync(0).ConfigureAwait(false);
            Assert.True(ownerOfToken.IsTheSameAddress(addressToRegisterOwnership));

        }

        [Fact] //
        public async void ShouldRetriveAllTheMetadataUrls()
        {
            //e2e test of using multicall to retrieve all the ipfs metadata urls of a user
            //var web3 = new Web3.Web3("https://optimism-mainnet.infura.io/v3/<<infuraId>>");
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var contractAddress =
                "0x31385d3520bced94f77aae104b406994d8f2168c";
            var erc721Service = web3.Eth.ERC721.GetContractService(contractAddress);
            var ownerToCheck = await erc721Service.OwnerOfQueryAsync(1).ConfigureAwait(false);
            //var erc721Service = web3.Eth.ERC721.GetContractService("0xB8Df6Cc3050cC02F967Db1eE48330bA23276A492"); //optimism
            var urls = await erc721Service.GetAllTokenUrlsOfOwnerUsingTokenOfOwnerByIndexAndMultiCallAsync(
                ownerToCheck).ConfigureAwait(false); //Pick an owner with some balance
            Assert.True(urls.Any());


        }

        // [Fact]
        ///This is slow as it uses bactches of 3000 calls to get all owned tokens of 11305
        public async void ShouldRetrieveAllOwnersUsingIdRangeAndMulticallAsync()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            //creating a new service with the new contract address
            var contractAddress =
                "0x31385d3520bced94f77aae104b406994d8f2168c";
            var erc721Service = web3.Eth.ERC721.GetContractService(contractAddress);
            var owners = await web3.Eth.ERC721.GetContractService(contractAddress)
                .GetAllOwnersUsingIdRangeAndMultiCallAsync(0, 11304).ConfigureAwait(false);
            Assert.Equal(11305, owners.Count);

        }

       
        [Fact]
        public async void ShouldRetriveAllOwnnedTokensUsingProcessor()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);

            //creating a new service with the new contract address
            var contractAddresses = new string[]
                {"0xd24a7c412f2279b1901e591898c1e96c140be8c5", "0x31385d3520bced94f77aae104b406994d8f2168c"};
            var erc721Service = web3.Eth.ERC721.GetContractService(contractAddresses[0]);
            var ownerToCheck = await erc721Service.OwnerOfQueryAsync(1).ConfigureAwait(false);

            var cancellationToken = new CancellationToken();
            //// this is 143 calls as we hit the limit
            //var allOwners =
            //    await GetAllCurrentOwnersProcessingTransferEvents(web3, contractAddresses, null, null,
            //        cancellationToken);

            var ownedByAccount = await web3.Processing.Logs.ERC721.GetErc721OwnedByAccountUsingAllTransfersForContract(
                contractAddresses[0],
                ownerToCheck, null, null, cancellationToken).ConfigureAwait(false);
            Assert.True(ownedByAccount.Count > 0);
            var ownedByAccount2 =
                await web3.Processing.Logs.ERC721.GetErc721OwnedByAccountUsingAllTransfersForContracts(
                    contractAddresses,
                    ownerToCheck, null, null, cancellationToken).ConfigureAwait(false);
            Assert.True(ownedByAccount2.Count >= ownedByAccount.Count);
        }

        //[Fact]
        public async void ShouldGetAllApeOwners()
        {
            var web3 = new Web3.Web3("http://fullnode.dappnode:8545");
            Nethereum.JsonRpc.Client.ClientBase.ConnectionTimeout = TimeSpan.FromSeconds(190.0);
            var erc721TokenContractAddress = "0xBC4CA0EdA7647A8aB7C2061c2E118A18a936f13D";


            var tokensOwned = await web3.Processing.Logs.ERC721.GetAllCurrentOwnersProcessingAllTransferEvents(erc721TokenContractAddress, 12287507, null, default(CancellationToken), 50000).ConfigureAwait(false);

            foreach (var token in tokensOwned)
            {
                System.Diagnostics.Debug.WriteLine(token.Owner);
                System.Diagnostics.Debug.WriteLine(token.TokenId);
            }
        }


        //[Fact]
    //    public async void ShouldGetAllApeOwners2()
    //    {
    //        var web3 = new Web3.Web3("http://fullnode.dappnode:8545");
    //        Nethereum.JsonRpc.Client.ClientBase.ConnectionTimeout = TimeSpan.FromSeconds(190.0);
    //        var erc721TokenContractAddress = "0xBC4CA0EdA7647A8aB7C2061c2E118A18a936f13D";
    //        var currentBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
    //        var numberOfBlocks = 50000;
    //        var blockRanges = GetBlockRanges(12287507, currentBlockNumber.Value, numberOfBlocks);
    //        var items = new ConcurrentBag<EventLog<TransferEventDTO>>();
    //        await blockRanges
    //            .ParallelForEachAsync(
    //                async item =>
    //                {
    //                    var transfers = await web3.Processing.Logs.ERC721.GetAllTransferEventsForContract(erc721TokenContractAddress, item.StartBlockNumber, item.EndBlockNumber, default(CancellationToken), numberOfBlocks); ;
    //                    foreach(var transfer in transfers)
    //                    {
    //                        items.Add(transfer);
    //                    };
    //                },
    //                3
    //                //Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0))
    //            );

    //        //foreach (var token in tokensOwned)
    //        //{
    //        //    //System.Diagnostics.Debug.WriteLine(token.Owner);
    //        //   // System.Diagnostics.Debug.WriteLine(token.TokenId);
    //        //}
    //    }

       

    //    public List<BlockRange> GetBlockRanges(BigInteger startBlockNumber, BigInteger endBlockNumber, int pageSize)
    //    {
    //        var currentBlockNumber = startBlockNumber;
    //        var blockRanges = new List<BlockRange>();
            
    //        while (currentBlockNumber < endBlockNumber)
    //        {
    //            blockRanges.Add(new BlockRange() { StartBlockNumber = currentBlockNumber, EndBlockNumber = currentBlockNumber + pageSize });
    //            currentBlockNumber = currentBlockNumber + pageSize;
    //        }

    //        return blockRanges;
    //    }


    //    public class BlockRange
    //    {
    //        public BigInteger StartBlockNumber { get; set; }
    //        public BigInteger EndBlockNumber { get; set; }
    //    }
    //}


    //public static class AsyncExtensions
    //{
    //    public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int maxDop = DataflowBlockOptions.Unbounded, TaskScheduler scheduler = null)
    //    {
    //        var options = new ExecutionDataflowBlockOptions
    //        {
    //            MaxDegreeOfParallelism = maxDop
    //        };
    //        if (scheduler != null)
    //            options.TaskScheduler = scheduler;

    //        var block = new ActionBlock<T>(body, options);

    //        foreach (var item in source)
    //            block.Post(item);

    //        block.Complete();
    //        return block.Completion;
    //    }
    }
}﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Contracts.IntegrationTests.SmartContracts.Standards
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ERC1155Tests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ERC1155Tests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        public partial class MyERC1155Deployment : MyERC1155DeploymentBase
        {
            public MyERC1155Deployment() : base(BYTECODE) { }
            public MyERC1155Deployment(string byteCode) : base(byteCode) { }
        }

        public class MyERC1155DeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "60806040523480156200001157600080fd5b506040805160208101909152600081526200002c816200004b565b50620000383362000064565b6003805460ff60a01b1916905562000198565b805162000060906002906020840190620000b6565b5050565b600380546001600160a01b038381166001600160a01b0319831681179093556040519116919082907f8be0079c531659141344cd1fd0a4f28419497f9722a3daafe3b4186f6b6457e090600090a35050565b828054620000c4906200015c565b90600052602060002090601f016020900481019282620000e8576000855562000133565b82601f106200010357805160ff191683800117855562000133565b8280016001018555821562000133579182015b828111156200013357825182559160200191906001019062000116565b506200014192915062000145565b5090565b5b8082111562000141576000815560010162000146565b600181811c908216806200017157607f821691505b6020821081036200019257634e487b7160e01b600052602260045260246000fd5b50919050565b6123c780620001a86000396000f3fe608060405234801561001057600080fd5b50600436106101415760003560e01c80636b20c454116100b8578063a22cb4651161007c578063a22cb465146102aa578063bd85b039146102bd578063e985e9c5146102dd578063f242432a14610319578063f2fde38b1461032c578063f5298aca1461033f57600080fd5b80636b20c45414610259578063715018a61461026c578063731133e9146102745780638456cb59146102875780638da5cb5b1461028f57600080fd5b80632eb2c2d61161010a5780632eb2c2d6146101d75780633f4ba83a146101ea5780634e1273f4146101f25780634f558e791461021257806357f7789e146102345780635c975abb1461024757600080fd5b8062fdd58e1461014657806301ffc9a71461016c57806302fe53051461018f5780630e89341c146101a45780631f7fdffa146101c4575b600080fd5b61015961015436600461180c565b610352565b6040519081526020015b60405180910390f35b61017f61017a36600461184c565b6103e9565b6040519015158152602001610163565b6101a261019d366004611925565b61043b565b005b6101b76101b2366004611961565b610471565b60405161016391906119c7565b6101a26101d2366004611a6e565b610513565b6101a26101e5366004611b06565b61054f565b6101a26105e6565b610205610200366004611baf565b61061a565b6040516101639190611cb4565b61017f610220366004611961565b600090815260046020526040902054151590565b6101a2610242366004611cc7565b610743565b600354600160a01b900460ff1661017f565b6101a2610267366004611d03565b610791565b6101a26107d4565b6101a2610282366004611d76565b610808565b6101a261083e565b6003546040516001600160a01b039091168152602001610163565b6101a26102b8366004611dca565b610870565b6101596102cb366004611961565b60009081526004602052604090205490565b61017f6102eb366004611e06565b6001600160a01b03918216600090815260016020908152604080832093909416825291909152205460ff1690565b6101a2610327366004611e39565b61087f565b6101a261033a366004611e9d565b6108c4565b6101a261034d366004611eb8565b61095c565b60006001600160a01b0383166103c35760405162461bcd60e51b815260206004820152602b60248201527f455243313135353a2062616c616e636520717565727920666f7220746865207a60448201526a65726f206164647265737360a81b60648201526084015b60405180910390fd5b506000908152602081815260408083206001600160a01b03949094168352929052205490565b60006001600160e01b03198216636cdb3d1360e11b148061041a57506001600160e01b031982166303a24d0760e21b145b8061043557506301ffc9a760e01b6001600160e01b03198316145b92915050565b6003546001600160a01b031633146104655760405162461bcd60e51b81526004016103ba90611eeb565b61046e8161099f565b50565b600081815260056020526040902080546060919061048e90611f20565b80601f01602080910402602001604051908101604052809291908181526020018280546104ba90611f20565b80156105075780601f106104dc57610100808354040283529160200191610507565b820191906000526020600020905b8154815290600101906020018083116104ea57829003601f168201915b50505050509050919050565b6003546001600160a01b0316331461053d5760405162461bcd60e51b81526004016103ba90611eeb565b610549848484846109b2565b50505050565b6001600160a01b03851633148061056b575061056b85336102eb565b6105d25760405162461bcd60e51b815260206004820152603260248201527f455243313135353a207472616e736665722063616c6c6572206973206e6f74206044820152711bdddb995c881b9bdc88185c1c1c9bdd995960721b60648201526084016103ba565b6105df8585858585610b0c565b5050505050565b6003546001600160a01b031633146106105760405162461bcd60e51b81526004016103ba90611eeb565b610618610cb6565b565b6060815183511461067f5760405162461bcd60e51b815260206004820152602960248201527f455243313135353a206163636f756e747320616e6420696473206c656e677468604482015268040dad2e6dac2e8c6d60bb1b60648201526084016103ba565b600083516001600160401b0381111561069a5761069a611870565b6040519080825280602002602001820160405280156106c3578160200160208202803683370190505b50905060005b845181101561073b5761070e8582815181106106e7576106e7611f5a565b602002602001015185838151811061070157610701611f5a565b6020026020010151610352565b82828151811061072057610720611f5a565b602090810291909101015261073481611f86565b90506106c9565b509392505050565b6003546001600160a01b0316331461076d5760405162461bcd60e51b81526004016103ba90611eeb565b6000828152600560209081526040909120825161078c92840190611757565b505050565b6001600160a01b0383163314806107ad57506107ad83336102eb565b6107c95760405162461bcd60e51b81526004016103ba90611f9f565b61078c838383610d53565b6003546001600160a01b031633146107fe5760405162461bcd60e51b81526004016103ba90611eeb565b6106186000610ee1565b6003546001600160a01b031633146108325760405162461bcd60e51b81526004016103ba90611eeb565b61054984848484610f33565b6003546001600160a01b031633146108685760405162461bcd60e51b81526004016103ba90611eeb565b610618611009565b61087b338383611091565b5050565b6001600160a01b03851633148061089b575061089b85336102eb565b6108b75760405162461bcd60e51b81526004016103ba90611f9f565b6105df8585858585611171565b6003546001600160a01b031633146108ee5760405162461bcd60e51b81526004016103ba90611eeb565b6001600160a01b0381166109535760405162461bcd60e51b815260206004820152602660248201527f4f776e61626c653a206e6577206f776e657220697320746865207a65726f206160448201526564647265737360d01b60648201526084016103ba565b61046e81610ee1565b6001600160a01b038316331480610978575061097883336102eb565b6109945760405162461bcd60e51b81526004016103ba90611f9f565b61078c83838361128e565b805161087b906002906020840190611757565b6001600160a01b0384166109d85760405162461bcd60e51b81526004016103ba90611fe8565b81518351146109f95760405162461bcd60e51b81526004016103ba90612029565b33610a098160008787878761138f565b60005b8451811015610aa457838181518110610a2757610a27611f5a565b6020026020010151600080878481518110610a4457610a44611f5a565b602002602001015181526020019081526020016000206000886001600160a01b03166001600160a01b031681526020019081526020016000206000828254610a8c9190612071565b90915550819050610a9c81611f86565b915050610a0c565b50846001600160a01b031660006001600160a01b0316826001600160a01b03167f4a39dc06d4c0dbc64b70af90fd698a233a518aa5d07e595d983b8c0526c8f7fb8787604051610af5929190612089565b60405180910390a46105df816000878787876113ea565b8151835114610b2d5760405162461bcd60e51b81526004016103ba90612029565b6001600160a01b038416610b535760405162461bcd60e51b81526004016103ba906120b7565b33610b6281878787878761138f565b60005b8451811015610c48576000858281518110610b8257610b82611f5a565b602002602001015190506000858381518110610ba057610ba0611f5a565b602090810291909101810151600084815280835260408082206001600160a01b038e168352909352919091205490915081811015610bf05760405162461bcd60e51b81526004016103ba906120fc565b6000838152602081815260408083206001600160a01b038e8116855292528083208585039055908b16825281208054849290610c2d908490612071565b9250508190555050505080610c4190611f86565b9050610b65565b50846001600160a01b0316866001600160a01b0316826001600160a01b03167f4a39dc06d4c0dbc64b70af90fd698a233a518aa5d07e595d983b8c0526c8f7fb8787604051610c98929190612089565b60405180910390a4610cae8187878787876113ea565b505050505050565b600354600160a01b900460ff16610d065760405162461bcd60e51b815260206004820152601460248201527314185d5cd8589b194e881b9bdd081c185d5cd95960621b60448201526064016103ba565b6003805460ff60a01b191690557f5db9ee0a495bf2e6ff9c91a7834c1ba4fdd244a5e8aa4e537bd38aeae4b073aa335b6040516001600160a01b03909116815260200160405180910390a1565b6001600160a01b038316610d795760405162461bcd60e51b81526004016103ba90612146565b8051825114610d9a5760405162461bcd60e51b81526004016103ba90612029565b6000339050610dbd8185600086866040518060200160405280600081525061138f565b60005b8351811015610e82576000848281518110610ddd57610ddd611f5a565b602002602001015190506000848381518110610dfb57610dfb611f5a565b602090810291909101810151600084815280835260408082206001600160a01b038c168352909352919091205490915081811015610e4b5760405162461bcd60e51b81526004016103ba90612189565b6000928352602083815260408085206001600160a01b038b1686529091529092209103905580610e7a81611f86565b915050610dc0565b5060006001600160a01b0316846001600160a01b0316826001600160a01b03167f4a39dc06d4c0dbc64b70af90fd698a233a518aa5d07e595d983b8c0526c8f7fb8686604051610ed3929190612089565b60405180910390a450505050565b600380546001600160a01b038381166001600160a01b0319831681179093556040519116919082907f8be0079c531659141344cd1fd0a4f28419497f9722a3daafe3b4186f6b6457e090600090a35050565b6001600160a01b038416610f595760405162461bcd60e51b81526004016103ba90611fe8565b33610f7981600087610f6a88611545565b610f7388611545565b8761138f565b6000848152602081815260408083206001600160a01b038916845290915281208054859290610fa9908490612071565b909155505060408051858152602081018590526001600160a01b0380881692600092918516917fc3d58168c5ae7397731d063d5bbf3d657854427343f4c083240f7aacaa2d0f62910160405180910390a46105df81600087878787611590565b600354600160a01b900460ff16156110565760405162461bcd60e51b815260206004820152601060248201526f14185d5cd8589b194e881c185d5cd95960821b60448201526064016103ba565b6003805460ff60a01b1916600160a01b1790557f62e78cea01bee320cd4e420270b5ea74000d11b0c9f74754ebdbfc544b05a258610d363390565b816001600160a01b0316836001600160a01b0316036111045760405162461bcd60e51b815260206004820152602960248201527f455243313135353a2073657474696e6720617070726f76616c20737461747573604482015268103337b91039b2b63360b91b60648201526084016103ba565b6001600160a01b03838116600081815260016020908152604080832094871680845294825291829020805460ff191686151590811790915591519182527f17307eab39ab6107e8899845ad3d59bd9653f200f220920489ca2b5937696c31910160405180910390a3505050565b6001600160a01b0384166111975760405162461bcd60e51b81526004016103ba906120b7565b336111a7818787610f6a88611545565b6000848152602081815260408083206001600160a01b038a168452909152902054838110156111e85760405162461bcd60e51b81526004016103ba906120fc565b6000858152602081815260408083206001600160a01b038b8116855292528083208785039055908816825281208054869290611225908490612071565b909155505060408051868152602081018690526001600160a01b03808916928a821692918616917fc3d58168c5ae7397731d063d5bbf3d657854427343f4c083240f7aacaa2d0f62910160405180910390a4611285828888888888611590565b50505050505050565b6001600160a01b0383166112b45760405162461bcd60e51b81526004016103ba90612146565b336112e3818560006112c587611545565b6112ce87611545565b6040518060200160405280600081525061138f565b6000838152602081815260408083206001600160a01b0388168452909152902054828110156113245760405162461bcd60e51b81526004016103ba90612189565b6000848152602081815260408083206001600160a01b03898116808652918452828520888703905582518981529384018890529092908616917fc3d58168c5ae7397731d063d5bbf3d657854427343f4c083240f7aacaa2d0f62910160405180910390a45050505050565b600354600160a01b900460ff16156113dc5760405162461bcd60e51b815260206004820152601060248201526f14185d5cd8589b194e881c185d5cd95960821b60448201526064016103ba565b610cae86868686868661164b565b6001600160a01b0384163b15610cae5760405163bc197c8160e01b81526001600160a01b0385169063bc197c819061142e90899089908890889088906004016121cd565b6020604051808303816000875af1925050508015611469575060408051601f3d908101601f191682019092526114669181019061222b565b60015b61151557611475612248565b806308c379a0036114ae5750611489612264565b8061149457506114b0565b8060405162461bcd60e51b81526004016103ba91906119c7565b505b60405162461bcd60e51b815260206004820152603460248201527f455243313135353a207472616e7366657220746f206e6f6e20455243313135356044820152732932b1b2b4bb32b91034b6b83632b6b2b73a32b960611b60648201526084016103ba565b6001600160e01b0319811663bc197c8160e01b146112855760405162461bcd60e51b81526004016103ba906122ed565b6040805160018082528183019092526060916000919060208083019080368337019050509050828160008151811061157f5761157f611f5a565b602090810291909101015292915050565b6001600160a01b0384163b15610cae5760405163f23a6e6160e01b81526001600160a01b0385169063f23a6e61906115d49089908990889088908890600401612335565b6020604051808303816000875af192505050801561160f575060408051601f3d908101601f1916820190925261160c9181019061222b565b60015b61161b57611475612248565b6001600160e01b0319811663f23a6e6160e01b146112855760405162461bcd60e51b81526004016103ba906122ed565b6001600160a01b0385166116d25760005b83518110156116d05782818151811061167757611677611f5a565b60200260200101516004600086848151811061169557611695611f5a565b6020026020010151815260200190815260200160002060008282546116ba9190612071565b909155506116c9905081611f86565b905061165c565b505b6001600160a01b038416610cae5760005b8351811015611285578281815181106116fe576116fe611f5a565b60200260200101516004600086848151811061171c5761171c611f5a565b602002602001015181526020019081526020016000206000828254611741919061237a565b90915550611750905081611f86565b90506116e3565b82805461176390611f20565b90600052602060002090601f01602090048101928261178557600085556117cb565b82601f1061179e57805160ff19168380011785556117cb565b828001600101855582156117cb579182015b828111156117cb5782518255916020019190600101906117b0565b506117d79291506117db565b5090565b5b808211156117d757600081556001016117dc565b80356001600160a01b038116811461180757600080fd5b919050565b6000806040838503121561181f57600080fd5b611828836117f0565b946020939093013593505050565b6001600160e01b03198116811461046e57600080fd5b60006020828403121561185e57600080fd5b813561186981611836565b9392505050565b634e487b7160e01b600052604160045260246000fd5b601f8201601f191681016001600160401b03811182821017156118ab576118ab611870565b6040525050565b600082601f8301126118c357600080fd5b81356001600160401b038111156118dc576118dc611870565b6040516118f3601f8301601f191660200182611886565b81815284602083860101111561190857600080fd5b816020850160208301376000918101602001919091529392505050565b60006020828403121561193757600080fd5b81356001600160401b0381111561194d57600080fd5b611959848285016118b2565b949350505050565b60006020828403121561197357600080fd5b5035919050565b6000815180845260005b818110156119a057602081850181015186830182015201611984565b818111156119b2576000602083870101525b50601f01601f19169290920160200192915050565b602081526000611869602083018461197a565b60006001600160401b038211156119f3576119f3611870565b5060051b60200190565b600082601f830112611a0e57600080fd5b81356020611a1b826119da565b604051611a288282611886565b83815260059390931b8501820192828101915086841115611a4857600080fd5b8286015b84811015611a635780358352918301918301611a4c565b509695505050505050565b60008060008060808587031215611a8457600080fd5b611a8d856117f0565b935060208501356001600160401b0380821115611aa957600080fd5b611ab5888389016119fd565b94506040870135915080821115611acb57600080fd5b611ad7888389016119fd565b93506060870135915080821115611aed57600080fd5b50611afa878288016118b2565b91505092959194509250565b600080600080600060a08688031215611b1e57600080fd5b611b27866117f0565b9450611b35602087016117f0565b935060408601356001600160401b0380821115611b5157600080fd5b611b5d89838a016119fd565b94506060880135915080821115611b7357600080fd5b611b7f89838a016119fd565b93506080880135915080821115611b9557600080fd5b50611ba2888289016118b2565b9150509295509295909350565b60008060408385031215611bc257600080fd5b82356001600160401b0380821115611bd957600080fd5b818501915085601f830112611bed57600080fd5b81356020611bfa826119da565b604051611c078282611886565b83815260059390931b8501820192828101915089841115611c2757600080fd5b948201945b83861015611c4c57611c3d866117f0565b82529482019490820190611c2c565b96505086013592505080821115611c6257600080fd5b50611c6f858286016119fd565b9150509250929050565b600081518084526020808501945080840160005b83811015611ca957815187529582019590820190600101611c8d565b509495945050505050565b6020815260006118696020830184611c79565b60008060408385031215611cda57600080fd5b8235915060208301356001600160401b03811115611cf757600080fd5b611c6f858286016118b2565b600080600060608486031215611d1857600080fd5b611d21846117f0565b925060208401356001600160401b0380821115611d3d57600080fd5b611d49878388016119fd565b93506040860135915080821115611d5f57600080fd5b50611d6c868287016119fd565b9150509250925092565b60008060008060808587031215611d8c57600080fd5b611d95856117f0565b9350602085013592506040850135915060608501356001600160401b03811115611dbe57600080fd5b611afa878288016118b2565b60008060408385031215611ddd57600080fd5b611de6836117f0565b915060208301358015158114611dfb57600080fd5b809150509250929050565b60008060408385031215611e1957600080fd5b611e22836117f0565b9150611e30602084016117f0565b90509250929050565b600080600080600060a08688031215611e5157600080fd5b611e5a866117f0565b9450611e68602087016117f0565b9350604086013592506060860135915060808601356001600160401b03811115611e9157600080fd5b611ba2888289016118b2565b600060208284031215611eaf57600080fd5b611869826117f0565b600080600060608486031215611ecd57600080fd5b611ed6846117f0565b95602085013595506040909401359392505050565b6020808252818101527f4f776e61626c653a2063616c6c6572206973206e6f7420746865206f776e6572604082015260600190565b600181811c90821680611f3457607f821691505b602082108103611f5457634e487b7160e01b600052602260045260246000fd5b50919050565b634e487b7160e01b600052603260045260246000fd5b634e487b7160e01b600052601160045260246000fd5b600060018201611f9857611f98611f70565b5060010190565b60208082526029908201527f455243313135353a2063616c6c6572206973206e6f74206f776e6572206e6f7260408201526808185c1c1c9bdd995960ba1b606082015260800190565b60208082526021908201527f455243313135353a206d696e7420746f20746865207a65726f206164647265736040820152607360f81b606082015260800190565b60208082526028908201527f455243313135353a2069647320616e6420616d6f756e7473206c656e677468206040820152670dad2e6dac2e8c6d60c31b606082015260800190565b6000821982111561208457612084611f70565b500190565b60408152600061209c6040830185611c79565b82810360208401526120ae8185611c79565b95945050505050565b60208082526025908201527f455243313135353a207472616e7366657220746f20746865207a65726f206164604082015264647265737360d81b606082015260800190565b6020808252602a908201527f455243313135353a20696e73756666696369656e742062616c616e636520666f60408201526939103a3930b739b332b960b11b606082015260800190565b60208082526023908201527f455243313135353a206275726e2066726f6d20746865207a65726f206164647260408201526265737360e81b606082015260800190565b60208082526024908201527f455243313135353a206275726e20616d6f756e7420657863656564732062616c604082015263616e636560e01b606082015260800190565b6001600160a01b0386811682528516602082015260a0604082018190526000906121f990830186611c79565b828103606084015261220b8186611c79565b9050828103608084015261221f818561197a565b98975050505050505050565b60006020828403121561223d57600080fd5b815161186981611836565b600060033d11156122615760046000803e5060005160e01c5b90565b600060443d10156122725790565b6040516003193d81016004833e81513d6001600160401b0381602484011181841117156122a157505050505090565b82850191508151818111156122b95750505050505090565b843d87010160208285010111156122d35750505050505090565b6122e260208286010187611886565b509095945050505050565b60208082526028908201527f455243313135353a204552433131353552656365697665722072656a656374656040820152676420746f6b656e7360c01b606082015260800190565b6001600160a01b03868116825285166020820152604081018490526060810183905260a06080820181905260009061236f9083018461197a565b979650505050505050565b60008282101561238c5761238c611f70565b50039056fea2646970667358221220e6345901894672cd148af97d11a0da586cfd7d5796c0959cc84b4af8eee993d164736f6c634300080d0033";
            public MyERC1155DeploymentBase() : base(BYTECODE) { }
            public MyERC1155DeploymentBase(string byteCode) : base(byteCode) { }

        }

        [Fact]
        public async void ShouldDeployCustomAndQueryInteractWithGenericService()
        {
         
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            web3.Eth.TransactionManager.UseLegacyAsDefault = true;


            var ercERC1155Deployment = new MyERC1155Deployment();
            //Deploy the 1155 contract (shop)
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<MyERC1155Deployment>().SendRequestAndWaitForReceiptAsync(ercERC1155Deployment).ConfigureAwait(false);

            //creating a new service with the new contract address
            var erc1155Service = web3.Eth.ERC1155.GetContractService(deploymentReceipt.ContractAddress);
            var id = 123456789;
            var url = "ipfs://bafkreiblz4ltiepapdhqhjiurmfpov7extmxwcntqskvx2zqisoftlmk7a";
            var amount = 100;
            var addressToRegisterOwnership = "0xe612205919814b1995D861Bdf6C2fE2f20cDBd68";

            //Adding the product information
            var tokenUriReceipt = await erc1155Service.SetTokenUriRequestAndWaitForReceiptAsync(id,
                 url).ConfigureAwait(false);

            var mintReceipt = await erc1155Service.MintRequestAndWaitForReceiptAsync(web3.TransactionManager.Account.Address, id, amount, new byte[] { }).ConfigureAwait(false);


            // the balance should be 
            var balance = await erc1155Service.BalanceOfQueryAsync(web3.TransactionManager.Account.Address, id).ConfigureAwait(false);

            Assert.Equal(amount, balance);

            var addressOfToken = await erc1155Service.UriQueryAsync(id).ConfigureAwait(false);

            Assert.Equal(url, addressOfToken);

            //lets sell 2
            var transfer = await erc1155Service.SafeTransferFromRequestAndWaitForReceiptAsync(web3.TransactionManager.Account.Address, addressToRegisterOwnership, id, 2, new byte[] { }).ConfigureAwait(false);
            Assert.False(transfer.HasErrors());

            var balance2 = await erc1155Service.BalanceOfQueryAsync(addressToRegisterOwnership, id).ConfigureAwait(false);
            Assert.Equal(2, balance2);
        }

    }
}
﻿using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.JsonRpc.Client;
using Nethereum.XUnitEthereumClients;
using System.Numerics;
using Xunit;
using static Nethereum.Contracts.IntegrationTests.SmartContracts.ErrorReasonTest;
// ReSharper disable ConsiderUsingConfigureAwait  
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.SmartContracts
{

    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ErrorReasonTest
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ErrorReasonTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        //[Fact] //Ignoring as Infura does not support this old block
        //public async void ShouldRetrieveErrorReason()
        //{
        //    var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
        //    //var errorReason =
        //    // await web3.Eth.GetContractTransactionErrorReason.SendRequestAsync("0x9e2831c81b4e3f29a9fb420d50a288d3424b1dd53f7de6bdc423f5429e3be4fc");
        //    // trying to call it will throw an error now

        //    //RpcResponseException error = await Assert.ThrowsAsync<RpcResponseException>(async () =>
        //    await web3.Eth.GetContractTransactionErrorReason.SendRequestAsync("0x9e2831c81b4e3f29a9fb420d50a288d3424b1dd53f7de6bdc423f5429e3be4fc");
        //        //);

        //  //  Assert.Equal("execution reverted: ERC20: transfer amount exceeds balance: eth_call", error.Message);
        //}


        //Solidity 
        /*
          pragma solidity ^0.5.0;

        contract Error { 
                function throwIt() view public returns (bool result) {
                    require(false, "An error message");
                return false;
                }
        }
         */

        [Fact]
        public async void ShouldThrowErrorDecodingCall()
        {
            //Parity does throw an RPC exception if the call is reverted, no info included
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new ErrorDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<ErrorDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;

                var contractHandler = web3.Eth.GetContractHandler(contractAddress);
                var error = await Assert.ThrowsAsync<SmartContractRevertException>(async () =>
                    await contractHandler.QueryAsync<ThrowItQueryFunction, bool>().ConfigureAwait(false)).ConfigureAwait(false);
                Assert.Equal("An error message", error.RevertMessage);
            }
            else // parity throws Rpc exception : "VM execution error."
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new ErrorDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<ErrorDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;

                var contractHandler = web3.Eth.GetContractHandler(contractAddress);
                var error = await Assert.ThrowsAsync<RpcResponseException>(async () =>
                    await contractHandler.QueryAsync<ThrowItQueryFunction, bool>().ConfigureAwait(false)).ConfigureAwait(false);
            }
        }


        [Fact]
        public async void ShouldThrowErrorOnEstimation()
        {
            //Parity does throw an RPC exception if the call is reverted, no info included
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new ErrorDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<ErrorDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;

                var contractHandler = web3.Eth.GetContractHandler(contractAddress);
                var error = await Assert.ThrowsAsync<SmartContractRevertException>(async () =>
                    await contractHandler.SendRequestAndWaitForReceiptAsync<ThrowItTxnFunction>().ConfigureAwait(false)).ConfigureAwait(false);
                Assert.Equal("An error message", error.RevertMessage);
            }
            else // parity throws Rpc exception : "VM execution error."
            {
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                var errorThrowDeployment = new ErrorDeployment();

                var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<ErrorDeployment>()
                    .SendRequestAndWaitForReceiptAsync(errorThrowDeployment).ConfigureAwait(false);
                var contractAddress = transactionReceiptDeployment.ContractAddress;

                var contractHandler = web3.Eth.GetContractHandler(contractAddress);
                var error = await Assert.ThrowsAsync<RpcResponseException>(async () =>
                    await contractHandler.SendRequestAndWaitForReceiptAsync<ThrowItTxnFunction>().ConfigureAwait(false)).ConfigureAwait(false);
            }
        }


        public partial class ErrorDeployment : ErrorDeploymentBase
        {
            public ErrorDeployment() : base(BYTECODE) { }
            public ErrorDeployment(string byteCode) : base(byteCode) { }
        }

        public class ErrorDeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "608060405234801561001057600080fd5b5061010c806100206000396000f3fe6080604052348015600f57600080fd5b506004361060325760003560e01c806351246563146037578063f88b5631146051575b600080fd5b603d6059565b604051901515815260200160405180910390f35b6057609c565b005b60405162461bcd60e51b815260206004820152601060248201526f416e206572726f72206d65737361676560801b60448201526000906064015b60405180910390fd5b60405162461bcd60e51b815260206004820152601060248201526f416e206572726f72206d65737361676560801b6044820152606401609356fea2646970667358221220f26c20ede67912c665386ca928fab378febc6fa989f204d778b967f6bed6f48264736f6c63430008110033";
            public ErrorDeploymentBase() : base(BYTECODE) { }
            public ErrorDeploymentBase(string byteCode) : base(byteCode) { }

        }

        public partial class ThrowItQueryFunction : ThrowItQueryFunctionBase { }

        [Function("throwItQuery", "bool")]
        public class ThrowItQueryFunctionBase : FunctionMessage
        {

        }

        public partial class ThrowItTxnFunction : ThrowItTxnFunctionBase { }

        [Function("throwItTxn")]
        public class ThrowItTxnFunctionBase : FunctionMessage
        {

        }

        public partial class ThrowItQueryOutputDTO : ThrowItQueryOutputDTOBase { }

        [FunctionOutput]
        public class ThrowItQueryOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("bool", "result", 1)]
            public virtual bool Result { get; set; }
        }

    }

}﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    [CollectionDefinition(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EthereumClientFixtureCollection : ICollectionFixture<EthereumClientIntegrationFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

}
﻿using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.StandardTokenEIP20.IntegrationTests
{
    [CollectionDefinition(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EthereumClientFixtureCollection : ICollectionFixture<EthereumClientIntegrationFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

}
﻿using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.StandardTokenEIP20.IntegrationTests
{
    [CollectionDefinition(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EthereumClientFixtureCollection : ICollectionFixture<EthereumClientIntegrationFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

}
﻿using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Signer.IntegrationTests
{
    [CollectionDefinition(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EthereumClientFixtureCollection : ICollectionFixture<EthereumClientIntegrationFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

}
﻿using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.StandardTokenEIP20.IntegrationTests
{
    [CollectionDefinition(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EthereumClientFixtureCollection : ICollectionFixture<EthereumClientIntegrationFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

}
﻿using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EncodingInputOutput
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EventAddressIntString
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EventAddressIntString(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        /*
         pragma solidity ^0.4.14;

contract Coin {
    // The keyword "public" makes those variables
    // readable from outside.
    address public minter;
    mapping (address => uint) public balances;

    // Events allow light clients to react on
    // changes efficiently.
    event Sent(address from, uint amount, address to );

    // This is the constructor whose code is
    // run only when the contract is created.
    function Coin() {
        minter = msg.sender;
    }

    function mint(address receiver, uint amount) {
        if (msg.sender != minter) return;
        balances[receiver] += amount;
    }

    function send(address receiver, uint amount) {
        if (balances[msg.sender] < amount) return;
        balances[msg.sender] -= amount;
        balances[receiver] += amount;
        Sent(msg.sender, amount, receiver);
    }

    event MetadataEvent(address creator, int id, string description, string metadata);

    function RaiseEventMetadata(address creator, int id, string description, string metadata ){
        MetadataEvent(creator, id, description, metadata);
    }
}

        */

        public class CoinService
        {
            public const string ABI =
                @"[{'constant':true,'inputs':[],'name':'minter','outputs':[{'name':'','type':'address'}],'payable':false,'type':'function'},{'constant':true,'inputs':[{'name':'','type':'address'}],'name':'balances','outputs':[{'name':'','type':'uint256'}],'payable':false,'type':'function'},{'constant':false,'inputs':[{'name':'receiver','type':'address'},{'name':'amount','type':'uint256'}],'name':'mint','outputs':[],'payable':false,'type':'function'},{'constant':false,'inputs':[{'name':'creator','type':'address'},{'name':'id','type':'int256'},{'name':'description','type':'string'},{'name':'metadata','type':'string'}],'name':'RaiseEventMetadata','outputs':[],'payable':false,'type':'function'},{'constant':false,'inputs':[{'name':'receiver','type':'address'},{'name':'amount','type':'uint256'}],'name':'send','outputs':[],'payable':false,'type':'function'},{'inputs':[],'payable':false,'type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'from','type':'address'},{'indexed':false,'name':'amount','type':'uint256'},{'indexed':false,'name':'to','type':'address'}],'name':'Sent','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'creator','type':'address'},{'indexed':false,'name':'id','type':'int256'},{'indexed':false,'name':'description','type':'string'},{'indexed':false,'name':'metadata','type':'string'}],'name':'MetadataEvent','type':'event'}]";

            public const string BYTE_CODE =
                "0x6060604052341561000f57600080fd5b5b60008054600160a060020a03191633600160a060020a03161790555b5b61041a8061003c6000396000f300606060405263ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166307546172811461006957806327e235e31461009857806340c10f19146100c9578063c0cafcbb146100ed578063d0679d3414610196575b600080fd5b341561007457600080fd5b61007c6101ba565b604051600160a060020a03909116815260200160405180910390f35b34156100a357600080fd5b6100b7600160a060020a03600435166101c9565b60405190815260200160405180910390f35b34156100d457600080fd5b6100eb600160a060020a03600435166024356101db565b005b34156100f857600080fd5b6100eb60048035600160a060020a03169060248035919060649060443590810190830135806020601f8201819004810201604051908101604052818152929190602084018383808284378201915050505050509190803590602001908201803590602001908080601f01602080910402602001604051908101604052818152929190602084018383808284375094965061021995505050505050565b005b34156101a157600080fd5b6100eb600160a060020a036004351660243561033f565b005b600054600160a060020a031681565b60016020526000908152604090205481565b60005433600160a060020a039081169116146101f657610215565b600160a060020a03821660009081526001602052604090208054820190555b5050565b7f088e204f1e4b42de930e87cb772757d4fe2dac2efa50bb4b9d6b6c8669c31d4d84848484604051600160a060020a038516815260208101849052608060408201818152906060830190830185818151815260200191508051906020019080838360005b838110156102965780820151818401525b60200161027d565b50505050905090810190601f1680156102c35780820380516001836020036101000a031916815260200191505b50838103825284818151815260200191508051906020019080838360005b838110156102fa5780820151818401525b6020016102e1565b50505050905090810190601f1680156103275780820380516001836020036101000a031916815260200191505b50965050505050505060405180910390a15b50505050565b600160a060020a0333166000908152600160205260409020548190101561036557610215565b600160a060020a03338181166000908152600160205260408082208054869003905592851681528290208054840190557f197260fb0c64c295dfe7074f7a13f7d1dee6f994b2be2f1c70d2332a64526e38918390859051600160a060020a03938416815260208101929092529091166040808301919091526060909101905180910390a15b50505600a165627a7a72305820fb59e26777a80c533713392891786e6db6d3e60117c2cd734a1e45a1f26c3ed90029";

            private readonly Web3.Web3 _web3;

            private readonly Contract _contract;

            public CoinService(Web3.Web3 web3, string address)
            {
                this._web3 = web3;
                _contract = web3.Eth.GetContract(ABI, address);
            }

            public static Task<string> DeployContractAsync(Web3.Web3 web3, string addressFrom, HexBigInteger gas = null,
                HexBigInteger valueAmount = null)
            {
                return web3.Eth.DeployContract.SendRequestAsync(ABI, BYTE_CODE, addressFrom, gas, valueAmount);
            }

            public Function GetFunctionMinter()
            {
                return _contract.GetFunction("minter");
            }

            public Function GetFunctionBalances()
            {
                return _contract.GetFunction("balances");
            }

            public Function GetFunctionMint()
            {
                return _contract.GetFunction("mint");
            }

            public Function GetFunctionRaiseEventMetadata()
            {
                return _contract.GetFunction("RaiseEventMetadata");
            }

            public Function GetFunctionSend()
            {
                return _contract.GetFunction("send");
            }

            public Event GetEventSent()
            {
                return _contract.GetEvent("Sent");
            }

            public Event GetEventMetadataEvent()
            {
                return _contract.GetEvent("MetadataEvent");
            }

            public Task<string> MinterAsyncCall()
            {
                var function = GetFunctionMinter();
                return function.CallAsync<string>();
            }

            public Task<BigInteger> BalancesAsyncCall(string a)
            {
                var function = GetFunctionBalances();
                return function.CallAsync<BigInteger>(a);
            }

            public Task<string> MintAsync(string addressFrom, string receiver, BigInteger amount,
                HexBigInteger gas = null, HexBigInteger valueAmount = null)
            {
                var function = GetFunctionMint();
                return function.SendTransactionAsync(addressFrom, gas, valueAmount, receiver, amount);
            }

            public Task<string> RaiseEventMetadataAsync(string addressFrom, string creator, BigInteger id,
                string description, string metadata, HexBigInteger gas = null, HexBigInteger valueAmount = null)
            {
                var function = GetFunctionRaiseEventMetadata();
                return function.SendTransactionAsync(addressFrom, gas, valueAmount, creator, id, description, metadata);
            }

            public Task<string> RaiseEventMetadataAsync(string addressFrom, RaiseEventMetadataInput input,
                HexBigInteger gas = null, HexBigInteger valueAmount = null)
            {
                var function = _contract.GetFunction<RaiseEventMetadataInput>();
                return function.SendTransactionAsync(input, addressFrom, gas, valueAmount);
            }

            public Task<string> SendAsync(string addressFrom, string receiver, BigInteger amount,
                HexBigInteger gas = null, HexBigInteger valueAmount = null)
            {
                var function = GetFunctionSend();
                return function.SendTransactionAsync(addressFrom, gas, valueAmount, receiver, amount);
            }
        }

        [Function("RaiseEventMetadata")]
        public class RaiseEventMetadataInput
        {
            [Parameter("address", "creator", 1, false)]
            public string Creator { get; set; }

            [Parameter("int256", "id", 2, false)] public int Id { get; set; }

            [Parameter("string", "description", 3, false)]
            public string Description { get; set; }

            [Parameter("string", "metadata", 4, false)]
            public string Metadata { get; set; }
        }

        [Event("Sent")]
        public class SentEventDTO
        {
            [Parameter("address", "from", 1, false)]
            public string From { get; set; }

            [Parameter("uint256", "amount", 2, false)]
            public BigInteger Amount { get; set; }

            [Parameter("address", "to", 3, false)] public string To { get; set; }
        }

        [Event("MetadataEvent")]
        public class MetadataEventEventDTO
        {
            [Parameter("address", "creator", 1, false)]
            public string Creator { get; set; }

            [Parameter("int256", "id", 2, false)] public int Id { get; set; }

            [Parameter("string", "description", 3, false)]
            public string Description { get; set; }

            [Parameter("string", "metadata", 4, false)]
            public string Metadata { get; set; }
        }

        [Fact]
        public async void Test()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var accountAddresss = EthereumClientIntegrationFixture.AccountAddress;
            var pollingService = new TransactionReceiptPollingService(web3.TransactionManager);
            var contractAddress = await pollingService.DeployContractAndGetAddressAsync(() =>
                    CoinService.DeployContractAsync(web3, accountAddresss, new HexBigInteger(4000000)))
                .ConfigureAwait(false);
            var coinService = new CoinService(web3, contractAddress);
            var txn = await coinService.MintAsync(accountAddresss, accountAddresss, 100, new HexBigInteger(4000000))
                .ConfigureAwait(false);
            var receipt = await pollingService.PollForReceiptAsync(txn).ConfigureAwait(false);
            var eventSent = coinService.GetEventSent();
            var sent = await eventSent.GetAllChangesAsync<SentEventDTO>(eventSent.CreateFilterInput())
                .ConfigureAwait(false);

            txn = await coinService.RaiseEventMetadataAsync(accountAddresss, accountAddresss, 100, "Description",
                "The metadata created here blah blah blah", new HexBigInteger(4000000)).ConfigureAwait(false);
            receipt = await pollingService.PollForReceiptAsync(txn).ConfigureAwait(false);

            var metadataEvent = coinService.GetEventMetadataEvent();
            var metadata =
                await metadataEvent.GetAllChangesAsync<MetadataEventEventDTO>(
                        metadataEvent.CreateFilterInput(new BlockParameter(receipt.BlockNumber), null))
                    .ConfigureAwait(false);
            var result = metadata[0].Event;
            Assert.Equal(result.Creator.ToLower(), accountAddresss.ToLower());
            Assert.Equal(100, result.Id);
            Assert.Equal("The metadata created here blah blah blah", result.Metadata);
            Assert.Equal("Description", result.Description);
        }

        [Fact]
        public async void TestChinese()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var accountAddresss = EthereumClientIntegrationFixture.AccountAddress;
            var pollingService = new TransactionReceiptPollingService(web3.TransactionManager);
            var contractAddress = await pollingService.DeployContractAndGetAddressAsync(() =>
                    CoinService.DeployContractAsync(web3, accountAddresss, new HexBigInteger(4000000)))
                .ConfigureAwait(false);
            var coinService = new CoinService(web3, contractAddress);

            var input = new RaiseEventMetadataInput
            {
                Creator = accountAddresss,
                Id = 101,
                Description = @"中国，China",
                Metadata = @"中国，China"
            };

            var txn = await coinService.RaiseEventMetadataAsync(accountAddresss, input, new HexBigInteger(4000000))
                .ConfigureAwait(false);
            var receipt = await pollingService.PollForReceiptAsync(txn).ConfigureAwait(false);

            var metadataEvent = coinService.GetEventMetadataEvent();
            var metadata = await metadataEvent
                .GetAllChangesAsync<MetadataEventEventDTO>(metadataEvent.CreateFilterInput()).ConfigureAwait(false);
            var result = metadata[0].Event;
            Assert.Equal(result.Creator.ToLower(), accountAddresss.ToLower());
            Assert.Equal(101, result.Id);
            Assert.Equal(@"中国，China", result.Metadata);
            Assert.Equal(@"中国，China", result.Description);
        }
    }
}using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.IntegrationTests.CQS;
using Nethereum.JsonRpc.Client;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable  ConsiderUsingConfigureAwait
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting
namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EventFilterNewFilterInputTests
    {
        public partial class EIP20Deployment : EIP20DeploymentBase
        {
            public EIP20Deployment() : base(BYTECODE)
            {
            }

            public EIP20Deployment(string byteCode) : base(byteCode)
            {
            }
        }

        public class EIP20DeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "608060405234801561001057600080fd5b506040516107843803806107848339810160409081528151602080840151838501516060860151336000908152808552959095208490556002849055908501805193959094919391019161006991600391860190610096565b506004805460ff191660ff8416179055805161008c906005906020840190610096565b5050505050610131565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f106100d757805160ff1916838001178555610104565b82800160010185558215610104579182015b828111156101045782518255916020019190600101906100e9565b50610110929150610114565b5090565b61012e91905b80821115610110576000815560010161011a565b90565b610644806101406000396000f3006080604052600436106100ae5763ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166306fdde0381146100b3578063095ea7b31461013d57806318160ddd1461017557806323b872dd1461019c57806327e235e3146101c6578063313ce567146101e75780635c6581651461021257806370a082311461023957806395d89b411461025a578063a9059cbb1461026f578063dd62ed3e14610293575b600080fd5b3480156100bf57600080fd5b506100c86102ba565b6040805160208082528351818301528351919283929083019185019080838360005b838110156101025781810151838201526020016100ea565b50505050905090810190601f16801561012f5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561014957600080fd5b50610161600160a060020a0360043516602435610348565b604080519115158252519081900360200190f35b34801561018157600080fd5b5061018a6103ae565b60408051918252519081900360200190f35b3480156101a857600080fd5b50610161600160a060020a03600435811690602435166044356103b4565b3480156101d257600080fd5b5061018a600160a060020a03600435166104b7565b3480156101f357600080fd5b506101fc6104c9565b6040805160ff9092168252519081900360200190f35b34801561021e57600080fd5b5061018a600160a060020a03600435811690602435166104d2565b34801561024557600080fd5b5061018a600160a060020a03600435166104ef565b34801561026657600080fd5b506100c861050a565b34801561027b57600080fd5b50610161600160a060020a0360043516602435610565565b34801561029f57600080fd5b5061018a600160a060020a03600435811690602435166105ed565b6003805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815292918301828280156103405780601f1061031557610100808354040283529160200191610340565b820191906000526020600020905b81548152906001019060200180831161032357829003601f168201915b505050505081565b336000818152600160209081526040808320600160a060020a038716808552908352818420869055815186815291519394909390927f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925928290030190a350600192915050565b60025481565b600160a060020a03831660008181526001602090815260408083203384528252808320549383529082905281205490919083118015906103f45750828110155b15156103ff57600080fd5b600160a060020a038085166000908152602081905260408082208054870190559187168152208054849003905560001981101561046157600160a060020a03851660009081526001602090815260408083203384529091529020805484900390555b83600160a060020a031685600160a060020a03167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef856040518082815260200191505060405180910390a3506001949350505050565b60006020819052908152604090205481565b60045460ff1681565b600160209081526000928352604080842090915290825290205481565b600160a060020a031660009081526020819052604090205490565b6005805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815292918301828280156103405780601f1061031557610100808354040283529160200191610340565b3360009081526020819052604081205482111561058157600080fd5b3360008181526020818152604080832080548790039055600160a060020a03871680845292819020805487019055805186815290519293927fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef929181900390910190a350600192915050565b600160a060020a039182166000908152600160209081526040808320939094168252919091522054905600a165627a7a7230582084c618322109054a21a57e27075384a6172ab854e4b2c2d35062a964a6bf593f0029";

            public EIP20DeploymentBase() : base(BYTECODE)
            {
            }

            public EIP20DeploymentBase(string byteCode) : base(byteCode)
            {
            }

            [Parameter("uint256", "_initialAmount", 1)]
            public BigInteger InitialAmount { get; set; }

            [Parameter("string", "_tokenName", 2)] public string TokenName { get; set; }

            [Parameter("uint8", "_decimalUnits", 3)]
            public byte DecimalUnits { get; set; }

            [Parameter("string", "_tokenSymbol", 4)]
            public string TokenSymbol { get; set; }
        }

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EventFilterNewFilterInputTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void Test()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new EIP20Deployment
            {
                InitialAmount = 10000,
                FromAddress = senderAddress,
                TokenName = "TST",
                TokenSymbol = "TST"
            };

            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<EIP20Deployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage)
                .ConfigureAwait(false);

            var contractAddress = transactionReceipt.ContractAddress;
            var newAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";


            var transactionMessage = new TransferFunction
            {
                FromAddress = senderAddress,
                To = newAddress,
                TokenAmount = 1000,
            };

            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();

            var transferReceipt =
                await transferHandler.SendRequestAndWaitForReceiptAsync(contractAddress, transactionMessage)
                    .ConfigureAwait(false);


            await EventAssertionsAsync(contractAddress, web3.Client, senderAddress, newAddress).ConfigureAwait(false);
            await EventAssertionsAsync(null, web3.Client, senderAddress, newAddress).ConfigureAwait(false);
        }

        public async Task EventAssertionsAsync(string contractAddresses,
            IClient client,
            string senderAddress,
            string newAddress)
        {
            Event<TransferEventDTO> eventForAnyContract = null;

            if (contractAddresses == null)
            {
                eventForAnyContract = new Event<TransferEventDTO>(client);
            }
            else
            {
                eventForAnyContract = new Event<TransferEventDTO>(client, contractAddresses);
            }

            var filterInputForAllContracts = eventForAnyContract.CreateFilterInput();

            var event1 = await eventForAnyContract.GetAllChangesAsync(filterInputForAllContracts).ConfigureAwait(false);

            Assert.True(event1.Any());

            var filterInputForFromAddress = eventForAnyContract.CreateFilterInput(senderAddress);

            var event2 = await eventForAnyContract.GetAllChangesAsync(filterInputForFromAddress).ConfigureAwait(false);

            Assert.True(event2.Any());


            var filterInputForToAddress = eventForAnyContract.CreateFilterInput<string, string>(null, newAddress);

            var event3 = await eventForAnyContract.GetAllChangesAsync(filterInputForToAddress).ConfigureAwait(false);

            Assert.True(event3.Any());


            var filterInputForToAndFromAddress = eventForAnyContract.CreateFilterInput(senderAddress, newAddress);

            var event4 = await eventForAnyContract.GetAllChangesAsync(filterInputForToAndFromAddress).ConfigureAwait(false);
            Assert.True(event4.Any());


            var filterInputForFromAddressArray = eventForAnyContract.CreateFilterInput(new[] {senderAddress});


            var event5 = await eventForAnyContract.GetAllChangesAsync(filterInputForFromAddressArray).ConfigureAwait(false);

            Assert.True(event5.Any());


            var filterInputForToAddressArray = eventForAnyContract.CreateFilterInput(null, new[] {newAddress});

            var event6 = await eventForAnyContract.GetAllChangesAsync(filterInputForToAddressArray).ConfigureAwait(false);

            Assert.True(event6.Any());


            var filterInputForToAndFromAddressArray =
                eventForAnyContract.CreateFilterInput(new[] {senderAddress}, new[] {newAddress});

            var event7 = await eventForAnyContract.GetAllChangesAsync(filterInputForToAndFromAddressArray).ConfigureAwait(false);
            Assert.True(event5.Any());
        }
    }
}using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EventFilterTest
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EventFilterTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task TestEvent()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new TestContract3Deployment() {FromAddress = senderAddress};
            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TestContract3Deployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage).ConfigureAwait(false);
            var contractHandler = web3.Eth.GetContractHandler(transactionReceipt.ContractAddress);

            var eventFilter = contractHandler.GetEvent<ItemCreatedEventDTO>();
            var filterId = await eventFilter.CreateFilterAsync(1).ConfigureAwait(false);

            var transactionReceiptSend = await contractHandler.SendRequestAndWaitForReceiptAsync(
                new NewItemFunction()
                {
                    FromAddress = senderAddress,
                    Id = 1,
                    Price = 100
                }).ConfigureAwait(false);

            var result = await eventFilter.GetFilterChangesAsync(filterId).ConfigureAwait(false);

            Assert.Single(result);
        }

        [Event("ItemCreated")]
        public class ItemCreatedEventDTO : IEventDTO
        {
            [Parameter("uint256", "itemId", 1, true)]
            public BigInteger ItemId { get; set; }

            [Parameter("address", "result", 2, false)]
            public string Result { get; set; }
        }

        public class TestContract3Deployment : ContractDeploymentMessage
        {
            public const string BYTECODE =
                "6060604052341561000f57600080fd5b60018054600160a060020a03191633600160a060020a03161790556101fe806100396000396000f3006060604052600436106100405763ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166329b856888114610045575b600080fd5b341561005057600080fd5b61005e600435602435610060565b005b6000805460018101610072838261015b565b91600052602060002090600302016000606060405190810160409081528682526020820186905260015473ffffffffffffffffffffffffffffffffffffffff169082015291905081518155602082015181600101556040820151600291909101805473ffffffffffffffffffffffffffffffffffffffff191673ffffffffffffffffffffffffffffffffffffffff909216919091179055508290507f1c78b9707d8ddf8078f46413765b0e73d250ffc795526eeb39c6889ea8efafd03360405173ffffffffffffffffffffffffffffffffffffffff909116815260200160405180910390a25050565b81548183558181151161018757600302816003028360005260206000209182019101610187919061018c565b505050565b6101cf91905b808211156101cb576000808255600182015560028101805473ffffffffffffffffffffffffffffffffffffffff19169055600301610192565b5090565b905600a165627a7a723058203753f72c36b1db5a70e27526c245d50858edb379405dddc78dea7dc6ff8ecee00029";

            public TestContract3Deployment() : base(BYTECODE)
            {
            }

            public TestContract3Deployment(string byteCode) : base(byteCode)
            {
            }
        }


        /*Contract 
         contract TestContract3 {

    struct Item {
        uint id;
        uint price;
        address manager;
    }

    Item[] items;
    address manager;

    function TestContract3() public {
        manager = msg.sender;
    }
    event ItemCreated(uint indexed itemId, address result);

    function newItem(uint id, uint price) public {
        items.push(Item(id, price, manager));
        emit ItemCreated(id, msg.sender);
    }
}
*/

        [Function("newItem")]
        public class NewItemFunction : FunctionMessage
        {
            [Parameter("uint256", "id", 1)] public BigInteger Id { get; set; }
            [Parameter("uint256", "price", 2)] public BigInteger Price { get; set; }
        }
    }
}using System.Linq;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable ConsiderUsingConfigureAwait
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EventFilterTopic
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EventFilterTopic(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        private readonly string _privateKey = "0x00b5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";

        [Fact]
        public async Task StoreAndRetrieveStructs()
        {
            var abi =
                @"[{'constant':true,'inputs':[{'name':'','type':'bytes32'},{'name':'','type':'uint256'}],'name':'documents','outputs':[{'name':'name','type':'string'},{'name':'description','type':'string'},{'name':'sender','type':'address'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'key','type':'bytes32'},{'name':'name','type':'string'},{'name':'description','type':'string'}],'name':'storeDocument','outputs':[{'name':'success','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'}]";

            var smartContractByteCode =
                "6060604052341561000f57600080fd5b6105408061001e6000396000f30060606040526004361061004b5763ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166379c17cc581146100505780638553139c14610189575b600080fd5b341561005b57600080fd5b610069600435602435610235565b60405173ffffffffffffffffffffffffffffffffffffffff821660408201526060808252845460026000196101006001841615020190911604908201819052819060208201906080830190879080156101035780601f106100d857610100808354040283529160200191610103565b820191906000526020600020905b8154815290600101906020018083116100e657829003601f168201915b50508381038252855460026000196101006001841615020190911604808252602090910190869080156101775780601f1061014c57610100808354040283529160200191610177565b820191906000526020600020905b81548152906001019060200180831161015a57829003601f168201915b50509550505050505060405180910390f35b341561019457600080fd5b610221600480359060446024803590810190830135806020601f8201819004810201604051908101604052818152929190602084018383808284378201915050505050509190803590602001908201803590602001908080601f01602080910402602001604051908101604052818152929190602084018383808284375094965061028795505050505050565b604051901515815260200160405180910390f35b60006020528160005260406000208181548110151561025057fe5b60009182526020909120600390910201600281015490925060018301915073ffffffffffffffffffffffffffffffffffffffff1683565b6000610291610371565b60606040519081016040908152858252602080830186905273ffffffffffffffffffffffffffffffffffffffff33168284015260008881529081905220805491925090600181016102e2838261039f565b600092835260209092208391600302018151819080516103069291602001906103d0565b506020820151816001019080516103219291602001906103d0565b506040820151600291909101805473ffffffffffffffffffffffffffffffffffffffff191673ffffffffffffffffffffffffffffffffffffffff9092169190911790555060019695505050505050565b60606040519081016040528061038561044e565b815260200161039261044e565b8152600060209091015290565b8154818355818115116103cb576003028160030283600052602060002091820191016103cb9190610460565b505050565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061041157805160ff191683800117855561043e565b8280016001018555821561043e579182015b8281111561043e578251825591602001919060010190610423565b5061044a9291506104b3565b5090565b60206040519081016040526000815290565b6104b091905b8082111561044a57600061047a82826104cd565b6104886001830160006104cd565b5060028101805473ffffffffffffffffffffffffffffffffffffffff19169055600301610466565b90565b6104b091905b8082111561044a57600081556001016104b9565b50805460018160011615610100020316600290046000825580601f106104f35750610511565b601f01602090049060005260206000209081019061051191906104b3565b505600a165627a7a72305820049f1f3ad86cf097dd9c5de014d2e718b5b6b9a05b091d4daebcf60dd3e1213c0029";

            var account = new Account(_privateKey);
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var accountBalance = await web3.Eth.GetBalance.SendRequestAsync(account.Address).ConfigureAwait(false);

            Assert.True(accountBalance.Value > 0);

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                    abi,
                    smartContractByteCode,
                    account.Address,
                    new HexBigInteger(900000)).ConfigureAwait(false);

            var contractAddress = receipt.ContractAddress;

            var contract = web3.Eth.GetContract(abi, contractAddress);
            var storeDocumentFunction = contract.GetFunction("storeDocument");

            var receipt1 = await storeDocumentFunction.SendTransactionAndWaitForReceiptAsync(account.Address,
                new HexBigInteger(900000), null, null, "k1", "doc1", "Document 1").ConfigureAwait(false);
            Assert.Equal(1, receipt1.Status?.Value);
            var receipt2 = await storeDocumentFunction.SendTransactionAndWaitForReceiptAsync(account.Address,
                new HexBigInteger(900000), null, null, "k2", "doc2", "Document 2").ConfigureAwait(false);
            Assert.Equal(1, receipt2.Status?.Value);

            var documentsFunction = contract.GetFunction("documents");
            var document1 = await documentsFunction.CallDeserializingToObjectAsync<Document>("k1", 0).ConfigureAwait(false);
            var document2 = await documentsFunction.CallDeserializingToObjectAsync<Document>("k2", 0).ConfigureAwait(false);

            Assert.Equal("doc1", document1.Name);
            Assert.Equal("doc2", document2.Name);
        }

        [FunctionOutput]
        public class Document
        {
            [Parameter("string", "name", 1)] public string Name { get; set; }

            [Parameter("string", "description", 2)]
            public string Description { get; set; }

            [Parameter("address", "sender", 3)] public string Sender { get; set; }
        }

        [Fact]
        public async Task DeployAndCallContract_WithEvents()
        {
            var abi =
                @"[{'constant':false,'inputs':[{'name':'val','type':'int256'}],'name':'multiply','outputs':[{'name':'','type':'int256'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[{'name':'multiplier','type':'int256'}],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'from','type':'address'},{'indexed':false,'name':'val','type':'int256'},{'indexed':false,'name':'result','type':'int256'}],'name':'Multiplied','type':'event'}]";

            var smartContractByteCode =
                "6060604052341561000f57600080fd5b604051602080610149833981016040528080516000555050610113806100366000396000f300606060405260043610603e5763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416631df4f14481146043575b600080fd5b3415604d57600080fd5b60566004356068565b60405190815260200160405180910390f35b6000805482027fd01bc414178a5d1578a8b9611adebfeda577e53e89287df879d5ab2c29dfa56a338483604051808473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001838152602001828152602001935050505060405180910390a1929150505600a165627a7a723058201bd2fbd3fb58686ed61df3e636dc4cc7c95b864aa1654bc02b0136e6eca9e9ef0029";

            var accountAddresss = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var multiplier = 2;

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                    abi,
                    smartContractByteCode,
                    accountAddresss,
                    new HexBigInteger(900000),
                    null,
                    multiplier).ConfigureAwait(false);

            var contractAddress = receipt.ContractAddress;

            var contract = web3.Eth.GetContract(abi, contractAddress);
            var multiplyFunction = contract.GetFunction("multiply");

            var multipliedEvent = contract.GetEvent("Multiplied");
            var filterForAll = await multipliedEvent.CreateFilterAsync().ConfigureAwait(false);

            var estimatedGas = await multiplyFunction.EstimateGasAsync(7).ConfigureAwait(false);

            var receipt1 = await multiplyFunction.SendTransactionAndWaitForReceiptAsync(accountAddresss,
                new HexBigInteger(estimatedGas.Value), null, null, 5).ConfigureAwait(false);
            var receipt2 = await multiplyFunction.SendTransactionAndWaitForReceiptAsync(accountAddresss,
                new HexBigInteger(estimatedGas.Value), null, null, 7).ConfigureAwait(false);

            Assert.Equal(1, receipt1.Status.Value);
            Assert.Equal(1, receipt2.Status.Value);

            Assert.False(receipt1.HasErrors());
            Assert.False(receipt2.HasErrors());

            var logsForAll = await multipliedEvent.GetFilterChangesAsync<MultipliedEvent>(filterForAll).ConfigureAwait(false);

            Assert.Equal(2, logsForAll.Count());
        }

        [Event("Multiplied")]
        public class MultipliedEvent
        {
            [Parameter("address", "from", 1)] public string Sender { get; set; }

            [Parameter("int", "val", 2)] public int InputValue { get; set; }

            [Parameter("int", "result", 3)] public int Result { get; set; }
        }

        public async Task<string> Test()
        {
            //The compiled solidity contract to be deployed
            /*
            contract test { 
    
                uint _multiplier;
    
                event Multiplied(uint indexed a);
    
                function test(uint multiplier){
                    _multiplier = multiplier;
                }
    
                function multiply(uint a, uint id) returns(uint d) { 
        
                    Multiplied(a);
        
                    return a * _multiplier; 
        
                }
    
            }
           
           */

            var contractByteCode =
                "606060405260405160208060de833981016040528080519060200190919050505b806000600050819055505b5060a68060386000396000f360606040526000357c010000000000000000000000000000000000000000000000000000000090048063c6888fa1146037576035565b005b604b60048080359060200190919050506061565b6040518082815260200191505060405180910390f35b6000817f61aa1562c4ed1a53026a57ad595b672e1b7c648166127b904365b44401821b7960405180905060405180910390a26000600050548202905060a1565b91905056";

            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""a"",""type"":""uint256""}],""name"":""Multiplied"",""type"":""event""}]
";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var addressFrom = EthereumClientIntegrationFixture.AccountAddress;

            //deploy the contract, including abi and a paramter of 7. 
            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(abi, contractByteCode, addressFrom, 7)
                .ConfigureAwait(false);


            //the contract should be mining now

            //get the contract address 
            TransactionReceipt receipt = null;
            //wait for the contract to be mined to the address
            while (receipt == null)
            {
                await Task.Delay(5000).ConfigureAwait(false);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).ConfigureAwait(false);
            }

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            var multipliedEvent = contract.GetEvent("Multiplied");
            var filterAll = await multipliedEvent.CreateFilterAsync().ConfigureAwait(false);
            //filter first indexed parameter
            var filter69 = await multipliedEvent.CreateFilterAsync(new object[] { 69 }).ConfigureAwait(false);


            await Task.Delay(2000).ConfigureAwait(false);

            //get the function by name
            var multiplyFunction = contract.GetFunction("multiply");

            var transaction69 = await multiplyFunction.SendTransactionAsync(addressFrom, null, 69).ConfigureAwait(false);
            var transaction18 = await multiplyFunction.SendTransactionAsync(addressFrom, null, 18).ConfigureAwait(false);
            var transaction7 = await multiplyFunction.SendTransactionAsync(addressFrom, null, 7).ConfigureAwait(false);


            TransactionReceipt receiptTransaction = null;

            while (receiptTransaction == null)
            {
                await Task.Delay(5000).ConfigureAwait(false);
                receiptTransaction = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction7).ConfigureAwait(false);
            }

            var logsAll = await web3.Eth.Filters.GetFilterChangesForEthNewFilter.SendRequestAsync(filterAll).ConfigureAwait(false);
            var logs69 = await web3.Eth.Filters.GetFilterChangesForEthNewFilter.SendRequestAsync(filter69).ConfigureAwait(false);


            return "All logs :" + logsAll.Length + " Logs for 69 " + logs69.Length;
        }
    }
}using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EventFilterWith2Topics
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EventFilterWith2Topics(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Event("Multiplied")]
        public class EventMultiplied
        {
            [Parameter("uint", "a", 1, true)] public int A { get; set; }

            [Parameter("uint", "result", 2, true)] public int Result { get; set; }
        }

        [Event("MultipliedLog")]
        public class EventMultipliedSenderLog
        {
            [Parameter("uint", "a", 1, true)] public int A { get; set; }

            [Parameter("uint", "result", 2, true)] public int Result { get; set; }

            [Parameter("address", "sender", 4, false)]
            public string Sender { get; set; }


            [Parameter("string", "hello", 3, true)]
            public string Hello { get; set; }
        }

        [Fact]
        public async Task Test()
        {
            //The compiled solidity contract to be deployed
            /*
          contract test { 
    
                uint _multiplier;
    
                event Multiplied(uint indexed a, uint indexed result);
    
                event MultipliedLog(uint indexed a, uint indexed result, string indexed hello, address sender );
    
                function test(uint multiplier){
                    _multiplier = multiplier;
                }
    
                function multiply(uint a) returns(uint d) {
                    d = a * _multiplier;
                    Multiplied(a, d);
                    MultipliedLog(a, d, "Hello world", msg.sender);
                    return d;
                }
    
                function multiply1(uint a) returns(uint d) {
                    return a * _multiplier;
                }
    
                function multiply2(uint a, uint b) returns(uint d){
                    return a * b;
                }
    
            }
           
           */

            var contractByteCode =
                "0x6060604052604051602080610213833981016040528080519060200190919050505b806000600050819055505b506101d88061003b6000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806361325dbc1461004f578063c23f4e3e1461007b578063c6888fa1146100b05761004d565b005b61006560048080359060200190919050506100dc565b6040518082815260200191505060405180910390f35b61009a60048080359060200190919080359060200190919050506100f2565b6040518082815260200191505060405180910390f35b6100c66004808035906020019091905050610104565b6040518082815260200191505060405180910390f35b6000600060005054820290506100ed565b919050565b600081830290506100fe565b92915050565b600060006000505482029050805080827f51ae5c4fa89d1aa731ff280d425357e6e5c838c6fc8ed6ca0139ea31716bbd5760405180905060405180910390a360405180807f48656c6c6f20776f726c64000000000000000000000000000000000000000000815260200150600b019050604051809103902081837f74053123e4f45ba0f8cbf86301034a4ab00cdc75cd155a0df7c5d815bd97dcb533604051808273ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390a48090506101d3565b91905056";

            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply1"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""},{""name"":""b"",""type"":""uint256""}],""name"":""multiply2"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""a"",""type"":""uint256""},{""indexed"":true,""name"":""result"",""type"":""uint256""}],""name"":""Multiplied"",""type"":""event""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""a"",""type"":""uint256""},{""indexed"":true,""name"":""result"",""type"":""uint256""},{""indexed"":true,""name"":""sender"",""type"":""string""},{""indexed"":false,""name"":""hello"",""type"":""address""}],""name"":""MultipliedLog"",""type"":""event""}]";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var addressFrom = EthereumClientIntegrationFixture.AccountAddress;
            //deploy the contract, including abi and a paramter of 7. 
            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(abi, contractByteCode, addressFrom,
                new HexBigInteger(900000), 7).ConfigureAwait(false);

            Assert.NotNull(transactionHash);

            //the contract should be mining now

            //get the contract address 
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).ConfigureAwait(false);
            //wait for the contract to be mined to the address
            while (receipt == null)
            {
                Thread.Sleep(100);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).ConfigureAwait(false);
            }

            var code = await web3.Eth.GetCode.SendRequestAsync(receipt.ContractAddress).ConfigureAwait(false);

            if (string.IsNullOrEmpty(code))
                throw new Exception(
                    "Code was not deployed correctly, verify bytecode or enough gas was uto deploy the contract");


            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            var multipliedEvent = contract.GetEvent("Multiplied");
            var filterAllContract = await contract.CreateFilterAsync().ConfigureAwait(false);
            var filterAll = await multipliedEvent.CreateFilterAsync().ConfigureAwait(false);
            //filter on the first indexed parameter
            var filter69 = await multipliedEvent.CreateFilterAsync(69).ConfigureAwait(false);

            HexBigInteger filter49 = null;


            //filter on the second indexed parameter
            filter49 = await multipliedEvent.CreateFilterAsync<object, int>(null, 49).ConfigureAwait(false);


            //filter OR on the first indexed parameter
            var filter69And18 = await multipliedEvent.CreateFilterAsync(new[] { 69, 18 }).ConfigureAwait(false);


            var multipliedEventLog = contract.GetEvent("MultipliedLog");
            var filterAllLog = await multipliedEventLog.CreateFilterAsync().ConfigureAwait(false);

            //get the function by name
            var multiplyFunction = contract.GetFunction("multiply");

            var gas = await multiplyFunction.EstimateGasAsync(69).ConfigureAwait(false);
            var transaction69 = await multiplyFunction.SendTransactionAsync(addressFrom, gas, null, 69).ConfigureAwait(false);
            var transaction18 = await multiplyFunction.SendTransactionAsync(addressFrom, gas, null, 18).ConfigureAwait(false);
            var transaction7 = await multiplyFunction.SendTransactionAsync(addressFrom, gas, null, 7).ConfigureAwait(false);

            var multiplyFunction2 = contract.GetFunction("multiply2");
            var callResult = await multiplyFunction2.CallAsync<int>(7, 7).ConfigureAwait(false);

            TransactionReceipt receiptTransaction = null;

            while (receiptTransaction == null)
            {
                Thread.Sleep(100);
                receiptTransaction = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction7).ConfigureAwait(false);
            }

            var logs = await web3.Eth.Filters.GetFilterChangesForEthNewFilter.SendRequestAsync(filterAllContract).ConfigureAwait(false);
            var eventLogsAll = await multipliedEvent.GetFilterChangesAsync<EventMultiplied>(filterAll).ConfigureAwait(false);
            var eventLogs69 = await multipliedEvent.GetFilterChangesAsync<EventMultiplied>(filter69).ConfigureAwait(false);


            //Parity does not accept null values for filter
            var eventLogsResult49 = await multipliedEvent.GetFilterChangesAsync<EventMultiplied>(filter49).ConfigureAwait(false);


            var eventLogsFor69And18 = await multipliedEvent.GetFilterChangesAsync<EventMultiplied>(filter69And18).ConfigureAwait(false);


            var multipliedLogEvents =
                await multipliedEventLog.GetFilterChangesAsync<EventMultipliedSenderLog>(filterAllLog).ConfigureAwait(false);

            Assert.Equal(483, eventLogs69.First().Event.Result);
            Assert.Equal("0xed6c11b0b5b808960df26f5bfc471d04c1995b0ffd2055925ad1be28d6baadfd",
                multipliedLogEvents.First().Event.Hello); //The sha3 keccak of "Hello world" as it is an indexed string
            Assert.Equal(multipliedLogEvents.First().Event.Sender.ToLower(), addressFrom.ToLower());
        }
    }
}using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.FiltersEvents
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EventFilterWith3Topics
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EventFilterWith3Topics(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        [Event("Pushed")]
        public class PushedEventDTO : IEventDTO
        {
            [Parameter("address", "first", 1, true)]
            public string First { get; set; }

            [Parameter("address", "second", 2, true)]
            public string Second { get; set; }

            [Parameter("bytes", "third", 3, false)]
            public byte[] Third { get; set; }

            [Parameter("bytes", "fourth", 4, false)]
            public byte[] Fourth { get; set; }

            [Parameter("bytes32", "fifth", 5, true)]
            public byte[] Fifth { get; set; }
        }


        [Event("PushedResult")]
        public class PushedResultEventDTO : IEventDTO
        {
            [Parameter("address", "first", 1, true)]
            public string First { get; set; }

            [Parameter("address", "second", 2, true)]
            public string Second { get; set; }

            [Parameter("bytes32", "third", 3, false)]
            public byte[] Third { get; set; }

            [Parameter("bytes32", "fourth", 4, false)]
            public byte[] Fourth { get; set; }

            [Parameter("bytes32", "fifth", 5, true)]
            public byte[] Fifth { get; set; }
        }


        [Event("Pushed2")]
        public class Pushed2EventDTO : IEventDTO
        {
            [Parameter("address", "first", 1, true)]
            public string First { get; set; }

            [Parameter("address", "second", 2, true)]
            public string Second { get; set; }

            [Parameter("bytes32", "third", 3, true)]
            public byte[] Third { get; set; }

            [Parameter("bytes", "fourth", 4, false)]
            public byte[] Fourth { get; set; }

            [Parameter("bytes", "fifth", 5, false)]
            public byte[] Fifth { get; set; }
        }

        [Function("PushEvent")]
        public class PushEventFunction : FunctionMessage
        {
        }

        public class TestEventDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "608060405234801561001057600080fd5b506103f0806100206000396000f3006080604052600436106100405763ffffffff7c0100000000000000000000000000000000000000000000000000000000600035041663b28dcc648114610045575b600080fd5b34801561005157600080fd5b5061005a61005c565b005b604080517f30313032303330340000000000000000000000000000000000000000000000008082526020820181905282519092606092849273ffffffffffffffffffffffffffffffffffffffff33169283927f5f7b4ef412a1639bb2acc0141a66f923c92070040b2c90d8a79c6d0966c03bfd929081900390910190a46040805160208082528183019092529080820161040080388339505081519192507f010000000000000000000000000000000000000000000000000000000000000091839150600090811061012a57fe5b9060200101907effffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff1916908160001a90535081600019163373ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167f53a4d306e452259eb29df0d47380abbde3f61e0f2ca4516e4d592aa40ced9afd8485604051808060200180602001838103835285818151815260200191508051906020019080838360005b838110156101f15781810151838201526020016101d9565b50505050905090810190601f16801561021e5780820380516001836020036101000a031916815260200191505b50838103825284518152845160209182019186019080838360005b83811015610251578181015183820152602001610239565b50505050905090810190601f16801561027e5780820380516001836020036101000a031916815260200191505b5094505050505060405180910390a481600019163373ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167fc4c107300ce6c6db32c2acd7c37b5c44c5fbdc3c065c6332b7234a66ed8686498485604051808060200180602001838103835285818151815260200191508051906020019080838360005b8381101561032457818101518382015260200161030c565b50505050905090810190601f1680156103515780820380516001836020036101000a031916815260200191505b50838103825284518152845160209182019186019080838360005b8381101561038457818101518382015260200161036c565b50505050905090810190601f1680156103b15780820380516001836020036101000a031916815260200191505b5094505050505060405180910390a450505600a165627a7a723058206506e94e4f63baa256b37d2ccb52196e7e2419b43b26fba54ec96427211fd3bb0029";

            public TestEventDeployment() : base(BYTECODE)
            {
            }

            public TestEventDeployment(string byteCode) : base(byteCode)
            {
            }
        }

        /*
         pragma solidity 0.4.23;
        contract TestEvent {

            //this is ok all 32 bytes
            event PushedResult(address indexed first, address indexed second, bytes32 third, bytes32 fourth, bytes32 indexed fifth);
    
            //This fails to create a filter
            event Pushed(
                address indexed first,
                address indexed second,
                bytes third,            
                bytes fourth,
                bytes32 indexed fifth 
            );


            //This is ok creating a filter
            event Pushed2(
                address indexed first,
                address indexed second,
                bytes32 indexed third,
                bytes fourth,            
                bytes fifth
            );

            function PushEvent() public
            {
                //0x3031303230333034000000000000000000000000000000000000000000000000
                bytes32 thing = bytes32("01020304");
                emit PushedResult(msg.sender, msg.sender, thing, thing, thing);
                bytes memory bytesArray = new bytes(32);
                bytesArray[0] = 0x01;
                emit Pushed(msg.sender, msg.sender, bytesArray, bytesArray, thing);    
                emit Pushed2(msg.sender, msg.sender, thing, bytesArray, bytesArray);
            }
        }

        */

        [Fact]
        public async Task Test()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var addressFrom = EthereumClientIntegrationFixture.AccountAddress;
            var receipt = await web3.Eth.GetContractDeploymentHandler<TestEventDeployment>()
                .SendRequestAndWaitForReceiptAsync(new TestEventDeployment() { FromAddress = addressFrom }).ConfigureAwait(false);


            var contractHandler = web3.Eth.GetContractHandler(receipt.ContractAddress);

            var bytes = "0x3031303230333034000000000000000000000000000000000000000000000000".HexToByteArray();

            //Event with all parameters fixed 32 bytes 2 addresses indexed and last indexed bytes 32
            var eventAllBytes32 = contractHandler.GetEvent<PushedResultEventDTO>();
            var filterAllBytes32 = await eventAllBytes32.CreateFilterAsync(addressFrom, addressFrom, bytes,
                new BlockParameter(receipt.BlockNumber), BlockParameter.CreateLatest()).ConfigureAwait(false);


            //Event with dynamic and last indexed bytes32
            var eventPushed = contractHandler.GetEvent<PushedEventDTO>();

            //ERROR creating filter
            //var filter2 = await eventPushed.CreateFilterAsync(addressFrom, addressFrom, bytes,
            //    new BlockParameter(receipt.BlockNumber), BlockParameter.CreateLatest());

            //Event with dynamic bytes all indexed values at the front
            var eventIndexedAtTheFront = contractHandler.GetEvent<Pushed2EventDTO>();
            var filterIndexedAtTheFront = await eventIndexedAtTheFront.CreateFilterAsync(addressFrom, addressFrom,
                bytes,
                new BlockParameter(receipt.BlockNumber), BlockParameter.CreateLatest()).ConfigureAwait(false);


            var pushReceipt =
                await contractHandler.SendRequestAndWaitForReceiptAsync(new PushEventFunction()
                { FromAddress = addressFrom }).ConfigureAwait(false);

            // Getting changes from the event with all bytes32
            var filterChangesAllBytes32 = await eventAllBytes32.GetFilterChangesAsync(filterAllBytes32).ConfigureAwait(false);

            Assert.NotEmpty(filterChangesAllBytes32);

            Assert.Equal(addressFrom.ToLower(), filterChangesAllBytes32[0].Event.First.ToLower());

            //Decoding the event (that we cannot create a filter) from the transaction receipt
            var eventsPushed = eventPushed.DecodeAllEventsForEvent(pushReceipt.Logs);

            Assert.NotEmpty(eventsPushed);

            Assert.Equal(addressFrom.ToLower(), eventsPushed[0].Event.First.ToLower());


            // Getting changes from the event with indexed at the front
            var filterChangesIndexedAtTheFront =
                await eventIndexedAtTheFront.GetFilterChangesAsync(filterIndexedAtTheFront).ConfigureAwait(false);

            Assert.NotEmpty(filterChangesIndexedAtTheFront);

            Assert.Equal(addressFrom.ToLower(), filterChangesIndexedAtTheFront[0].Event.First.ToLower());
        }
    }
}﻿using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.EVM;
using Nethereum.EVM.BlockchainState;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using System;
using System.Numerics;
using Xunit;
// ReSharper disable ConsiderUsingConfigureAwait  
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EVM
{


    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EvmSimulatorERC20Tests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EvmSimulatorERC20Tests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDeployToChain_CheckBalanceEvmSim_TransferSim()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var tokenDeployment = new TokenDeployment();
            tokenDeployment.InitialSupply = 10000;
            var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<TokenDeployment>().SendRequestAndWaitForReceiptAsync(tokenDeployment);
            var contractAddress = transactionReceiptDeployment.ContractAddress;
            var contractHandler = web3.Eth.GetContractHandler(contractAddress);

            var balanceOfFunction = new BalanceOfFunction();
            balanceOfFunction.Owner = EthereumClientIntegrationFixture.AccountAddress;
            var balanceOfFunctionReturn = await contractHandler.QueryAsync<BalanceOfFunction, BigInteger>(balanceOfFunction);
            Console.WriteLine(balanceOfFunctionReturn);

            //current block number
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var code = await web3.Eth.GetCode.SendRequestAsync(contractAddress); // runtime code;

            var callInput = balanceOfFunction.CreateCallInput(contractAddress);
            callInput.From = EthereumClientIntegrationFixture.AccountAddress;
            callInput.ChainId = new HexBigInteger(EthereumClientIntegrationFixture.ChainId);

            var nodeDataService = new RpcNodeDataService(web3.Eth, new BlockParameter(blockNumber));
            var executionStateService = new ExecutionStateService(nodeDataService);
            var programContext = new ProgramContext(callInput, executionStateService);
            var program = new Program(code.HexToByteArray(), programContext);
            var evmSimulator = new EVMSimulator();
            await evmSimulator.ExecuteAsync(program);
            var resultEncoded = program.ProgramResult.Result;
            var result = new BalanceOfOutputDTO().DecodeOutput(resultEncoded.ToHex());

            var transferFunction = new TransferFunction();
            transferFunction.FromAddress = EthereumClientIntegrationFixture.AccountAddress;
            transferFunction.To = "0xd8da6bf26964af9d7eed9e03e53415d37aa96045";
            transferFunction.Value = 500;

            callInput = transferFunction.CreateCallInput(contractAddress);
            programContext = new ProgramContext(callInput, executionStateService);
            program = new Program(code.HexToByteArray(), programContext);
            await evmSimulator.ExecuteAsync(program);

            balanceOfFunction.Owner = "0xd8da6bf26964af9d7eed9e03e53415d37aa96045";
            callInput = balanceOfFunction.CreateCallInput(contractAddress);
            callInput.From = EthereumClientIntegrationFixture.AccountAddress;

            programContext = new ProgramContext(callInput, executionStateService);
            program = new Program(code.HexToByteArray(), programContext);
            await evmSimulator.ExecuteAsync(program);
            resultEncoded = program.ProgramResult.Result;
            result = new BalanceOfOutputDTO().DecodeOutput(resultEncoded.ToHex());

            Assert.Equal(500, result.ReturnValue1);

        }


        public partial class TokenDeployment : TokenDeploymentBase
        {
            public TokenDeployment() : base(BYTECODE) { }
            public TokenDeployment(string byteCode) : base(byteCode) { }
        }

        public class TokenDeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "608060405234801561001057600080fd5b5060405161061238038061061283398181016040528101906100329190610098565b8060008190555080600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002081905550506100eb565b600081519050610092816100d4565b92915050565b6000602082840312156100ae576100ad6100cf565b5b60006100bc84828501610083565b91505092915050565b6000819050919050565b600080fd5b6100dd816100c5565b81146100e857600080fd5b50565b610518806100fa6000396000f3fe608060405234801561001057600080fd5b50600436106100415760003560e01c806318160ddd1461004657806370a0823114610064578063a9059cbb14610094575b600080fd5b61004e6100c4565b60405161005b9190610393565b60405180910390f35b61007e600480360381019061007991906102ed565b6100cd565b60405161008b9190610393565b60405180910390f35b6100ae60048036038101906100a9919061031a565b610116565b6040516100bb9190610378565b60405180910390f35b60008054905090565b6000600160008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050919050565b60008073ffffffffffffffffffffffffffffffffffffffff168373ffffffffffffffffffffffffffffffffffffffff16141561015157600080fd5b600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000205482111561019d57600080fd5b81600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020546101e89190610404565b600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000208190555081600160008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000205461027691906103ae565b600160008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020819055506001905092915050565b6000813590506102d2816104b4565b92915050565b6000813590506102e7816104cb565b92915050565b600060208284031215610303576103026104af565b5b6000610311848285016102c3565b91505092915050565b60008060408385031215610331576103306104af565b5b600061033f858286016102c3565b9250506020610350858286016102d8565b9150509250929050565b6103638161044a565b82525050565b61037281610476565b82525050565b600060208201905061038d600083018461035a565b92915050565b60006020820190506103a86000830184610369565b92915050565b60006103b982610476565b91506103c483610476565b9250827fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff038211156103f9576103f8610480565b5b828201905092915050565b600061040f82610476565b915061041a83610476565b92508282101561042d5761042c610480565b5b828203905092915050565b600061044382610456565b9050919050565b60008115159050919050565b600073ffffffffffffffffffffffffffffffffffffffff82169050919050565b6000819050919050565b7f4e487b7100000000000000000000000000000000000000000000000000000000600052601160045260246000fd5b600080fd5b6104bd81610438565b81146104c857600080fd5b50565b6104d481610476565b81146104df57600080fd5b5056fea26469706673582212200d8da631c5caac28cf4381f8bc52fa949c90fd86d5d7f3ad9fc86adfaced5b5764736f6c63430008070033";
            public TokenDeploymentBase() : base(BYTECODE) { }
            public TokenDeploymentBase(string byteCode) : base(byteCode) { }
            [Parameter("uint256", "_initialSupply", 1)]
            public virtual BigInteger InitialSupply { get; set; }
        }

        public partial class BalanceOfFunction : BalanceOfFunctionBase { }

        [Function("balanceOf", "uint256")]
        public class BalanceOfFunctionBase : FunctionMessage
        {
            [Parameter("address", "_owner", 1)]
            public virtual string Owner { get; set; }
        }

        public partial class TotalSupplyFunction : TotalSupplyFunctionBase { }

        [Function("totalSupply", "uint256")]
        public class TotalSupplyFunctionBase : FunctionMessage
        {

        }

        public partial class TransferFunction : TransferFunctionBase { }

        [Function("transfer", "bool")]
        public class TransferFunctionBase : FunctionMessage
        {
            [Parameter("address", "_to", 1)]
            public virtual string To { get; set; }
            [Parameter("uint256", "_value", 2)]
            public virtual BigInteger Value { get; set; }
        }

        public partial class BalanceOfOutputDTO : BalanceOfOutputDTOBase { }

        [FunctionOutput]
        public class BalanceOfOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("uint256", "", 1)]
            public virtual BigInteger ReturnValue1 { get; set; }
        }

        public partial class TotalSupplyOutputDTO : TotalSupplyOutputDTOBase { }

        [FunctionOutput]
        public class TotalSupplyOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("uint256", "", 1)]
            public virtual BigInteger ReturnValue1 { get; set; }
        }
    }
}﻿using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.EVM;
using Nethereum.EVM.BlockchainState;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Nethereum.Util;
using System;
using System.Globalization;
using System.Numerics;
using Xunit;
using System.Xml.Linq;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
// ReSharper disable ConsiderUsingConfigureAwait  
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EVM
{




    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EvmSimulatorERC20Tests820PushZero
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EvmSimulatorERC20Tests820PushZero(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDeployToChain_CheckBalanceEvmSim_TransferSim()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var tokenDeployment = new TokenDeployment();
            tokenDeployment.InitialSupply = 10000;
            var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<TokenDeployment>().SendRequestAndWaitForReceiptAsync(tokenDeployment);
            var contractAddress = transactionReceiptDeployment.ContractAddress;
            var contractHandler = web3.Eth.GetContractHandler(contractAddress);

            var balanceOfFunction = new BalanceOfFunction();
            balanceOfFunction.Owner = EthereumClientIntegrationFixture.AccountAddress;
            var balanceOfFunctionReturn = await contractHandler.QueryAsync<BalanceOfFunction, BigInteger>(balanceOfFunction);
            Console.WriteLine(balanceOfFunctionReturn);

            //current block number
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var code = await web3.Eth.GetCode.SendRequestAsync(contractAddress); // runtime code;

            var callInput = balanceOfFunction.CreateCallInput(contractAddress);
            callInput.From = EthereumClientIntegrationFixture.AccountAddress;
            callInput.ChainId = new HexBigInteger(EthereumClientIntegrationFixture.ChainId);

            var nodeDataService = new RpcNodeDataService(web3.Eth, new BlockParameter(blockNumber));
            var executionStateService = new ExecutionStateService(nodeDataService);
            var programContext = new ProgramContext(callInput, executionStateService);
            var program = new Program(code.HexToByteArray(), programContext);
            var evmSimulator = new EVMSimulator();
            await evmSimulator.ExecuteAsync(program);
            var resultEncoded = program.ProgramResult.Result;
            var result = new BalanceOfOutputDTO().DecodeOutput(resultEncoded.ToHex());

            var transferFunction = new TransferFunction();
            transferFunction.FromAddress = EthereumClientIntegrationFixture.AccountAddress;
            transferFunction.To = "0xd8da6bf26964af9d7eed9e03e53415d37aa96045";
            transferFunction.Value = 500;

            callInput = transferFunction.CreateCallInput(contractAddress);
            programContext = new ProgramContext(callInput, executionStateService);
            program = new Program(code.HexToByteArray(), programContext);
            await evmSimulator.ExecuteAsync(program);

            balanceOfFunction.Owner = "0xd8da6bf26964af9d7eed9e03e53415d37aa96045";
            callInput = balanceOfFunction.CreateCallInput(contractAddress);
            callInput.From = EthereumClientIntegrationFixture.AccountAddress;

            programContext = new ProgramContext(callInput, executionStateService);
            program = new Program(code.HexToByteArray(), programContext);
            await evmSimulator.ExecuteAsync(program);
            resultEncoded = program.ProgramResult.Result;
            result = new BalanceOfOutputDTO().DecodeOutput(resultEncoded.ToHex());

            Assert.Equal(500, result.ReturnValue1);

        }


        public partial class TokenDeployment : TokenDeploymentBase
        {
            public TokenDeployment() : base(BYTECODE) { }
            public TokenDeployment(string byteCode) : base(byteCode) { }
        }

        public class TokenDeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "60a060405234801561000f575f80fd5b506040516104df3803806104df83398101604081905261002e91610047565b6080819052335f9081526020819052604090205561005e565b5f60208284031215610057575f80fd5b5051919050565b60805161046a6100755f395f60bd015261046a5ff3fe608060405234801561000f575f80fd5b506004361061007a575f3560e01c8063313ce56711610058578063313ce5671461011057806370a082311461012a57806395d89b4114610152578063a9059cbb14610174575f80fd5b806306fdde031461007e57806318160ddd146100b857806323b872dd146100ed575b5f80fd5b6100a260405180604001604052806005815260200164045524332360dc1b81525081565b6040516100af9190610313565b60405180910390f35b6100df7f000000000000000000000000000000000000000000000000000000000000000081565b6040519081526020016100af565b6101006100fb366004610379565b610187565b60405190151581526020016100af565b610118601281565b60405160ff90911681526020016100af565b6100df6101383660046103b2565b6001600160a01b03165f9081526020819052604090205490565b6100a26040518060400160405280600381526020016245524360e81b81525081565b6101006101823660046103d2565b610259565b6001600160a01b0383165f908152602081905260408120548211156101aa575f80fd5b6001600160a01b0384165f908152602081905260409020546101cd90839061040e565b6001600160a01b038086165f9081526020819052604080822093909355908516815220546101fc908390610421565b6001600160a01b038481165f818152602081815260409182902094909455518581529092918716917fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef910160405180910390a35060019392505050565b335f90815260208190526040812054821115610273575f80fd5b335f9081526020819052604090205461028d90839061040e565b335f90815260208190526040808220929092556001600160a01b038516815220546102b9908390610421565b6001600160a01b0384165f81815260208181526040918290209390935551848152909133917fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef910160405180910390a35060015b92915050565b5f6020808352835180828501525f5b8181101561033e57858101830151858201604001528201610322565b505f604082860101526040601f19601f8301168501019250505092915050565b80356001600160a01b0381168114610374575f80fd5b919050565b5f805f6060848603121561038b575f80fd5b6103948461035e565b92506103a26020850161035e565b9150604084013590509250925092565b5f602082840312156103c2575f80fd5b6103cb8261035e565b9392505050565b5f80604083850312156103e3575f80fd5b6103ec8361035e565b946020939093013593505050565b634e487b7160e01b5f52601160045260245ffd5b8181038181111561030d5761030d6103fa565b8082018082111561030d5761030d6103fa56fea264697066735822122001e3cb289326f5ec5fe4f3bb1e9f2250bd6a5926c1e0d624d683270888f930a664736f6c63430008140033";
            public TokenDeploymentBase() : base(BYTECODE) { }
            public TokenDeploymentBase(string byteCode) : base(byteCode) { }
            [Parameter("uint256", "total", 1)]
            public virtual BigInteger InitialSupply { get; set; }
        }

        public partial class BalanceOfFunction : BalanceOfFunctionBase { }

        [Function("balanceOf", "uint256")]
        public class BalanceOfFunctionBase : FunctionMessage
        {
            [Parameter("address", "_owner", 1)]
            public virtual string Owner { get; set; }
        }

        public partial class TotalSupplyFunction : TotalSupplyFunctionBase { }

        [Function("totalSupply", "uint256")]
        public class TotalSupplyFunctionBase : FunctionMessage
        {

        }

        public partial class TransferFunction : TransferFunctionBase { }

        [Function("transfer", "bool")]
        public class TransferFunctionBase : FunctionMessage
        {
            [Parameter("address", "_to", 1)]
            public virtual string To { get; set; }
            [Parameter("uint256", "_value", 2)]
            public virtual BigInteger Value { get; set; }
        }

        public partial class BalanceOfOutputDTO : BalanceOfOutputDTOBase { }

        [FunctionOutput]
        public class BalanceOfOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("uint256", "", 1)]
            public virtual BigInteger ReturnValue1 { get; set; }
        }

        public partial class TotalSupplyOutputDTO : TotalSupplyOutputDTOBase { }

        [FunctionOutput]
        public class TotalSupplyOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("uint256", "", 1)]
            public virtual BigInteger ReturnValue1 { get; set; }
        }
    }
}﻿using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.EVM;
using Nethereum.EVM.BlockchainState;
using Nethereum.EVM.SourceInfo;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Xunit;
// ReSharper disable ConsiderUsingConfigureAwait  
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EVM
{



    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class EvmSimulatorPayableMultiContractTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public EvmSimulatorPayableMultiContractTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDeployToChain_TransferEtherToContract_AndTransferSameToAnotherContract()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<PayableTestSenderDeployment>().SendRequestAndWaitForReceiptAsync();
            var payableTestSenderContractAddress = transactionReceiptDeployment.ContractAddress;
            transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<PayableReceiverContractDeployment>().SendRequestAndWaitForReceiptAsync();
            var payableReceiverContractAddress = transactionReceiptDeployment.ContractAddress;

            //current block number
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var code = await web3.Eth.GetCode.SendRequestAsync(payableTestSenderContractAddress); // runtime code;

            var payMeAndSendFunction = new PayMeAndSendFunction();
            payMeAndSendFunction.AmountToSend = 5000;
            payMeAndSendFunction.RecieverContract = payableReceiverContractAddress;
            payMeAndSendFunction.FromAddress = EthereumClientIntegrationFixture.AccountAddress;

            var callInput = payMeAndSendFunction.CreateCallInput(payableTestSenderContractAddress);
            callInput.From = EthereumClientIntegrationFixture.AccountAddress;
            callInput.ChainId = new HexBigInteger(EthereumClientIntegrationFixture.ChainId);

            var nodeDataService = new RpcNodeDataService(web3.Eth, new BlockParameter(blockNumber));
            var executionStateService = new ExecutionStateService(nodeDataService);
            var programContext = new ProgramContext(callInput, executionStateService);
            var program = new Program(code.HexToByteArray(), programContext);
            var evmSimulator = new EVMSimulator();
            program = await evmSimulator.ExecuteAsync(program);
            var totalBalanceReceiver = programContext.ExecutionStateService.CreateOrGetAccountExecutionState(payableReceiverContractAddress).Balance.ExecutionBalance;
            var totalBalanceSender = programContext.ExecutionStateService.CreateOrGetAccountExecutionState(payableTestSenderContractAddress).Balance.ExecutionBalance;
            Assert.Equal(5000, totalBalanceReceiver);
            Assert.Equal(0, totalBalanceSender); //Sender sends the amount sent to the receiver..

            var paidAmountFunction = new PaidAmountFunction();
            callInput = paidAmountFunction.CreateCallInput(payableTestSenderContractAddress);
            callInput.From = EthereumClientIntegrationFixture.AccountAddress;

            programContext = new ProgramContext(callInput, executionStateService);
            var program2 = new Program(code.HexToByteArray(), programContext);
            await evmSimulator.ExecuteAsync(program2);
            var resultEncoded = program.ProgramResult.Result;
            var result = new PaidAmountOutputDTO().DecodeOutput(resultEncoded.ToHex());
            Assert.Equal(5000, result.ReturnValue1);

        }


        [Fact]
        public async void ShouldSourceMapTheTrace()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<PayableTestSenderDeployment>().SendRequestAndWaitForReceiptAsync();
            var payableTestSenderContractAddress = transactionReceiptDeployment.ContractAddress;
            transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<PayableReceiverContractDeployment>().SendRequestAndWaitForReceiptAsync();
            var payableReceiverContractAddress = transactionReceiptDeployment.ContractAddress;

            //current block number
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var code = await web3.Eth.GetCode.SendRequestAsync(payableTestSenderContractAddress); // runtime code;

            var payMeAndSendFunction = new PayMeAndSendFunction();
            payMeAndSendFunction.AmountToSend = 5000;
            payMeAndSendFunction.RecieverContract = payableReceiverContractAddress;
            payMeAndSendFunction.FromAddress = EthereumClientIntegrationFixture.AccountAddress;

            var callInput = payMeAndSendFunction.CreateCallInput(payableTestSenderContractAddress);
            callInput.From = EthereumClientIntegrationFixture.AccountAddress;
            callInput.ChainId = new HexBigInteger(EthereumClientIntegrationFixture.ChainId);

            var nodeDataService = new RpcNodeDataService(web3.Eth, new BlockParameter(blockNumber));
            var executionStateService = new ExecutionStateService(nodeDataService);
            var programContext = new ProgramContext(callInput, executionStateService);
            var program = new Program(code.HexToByteArray(), programContext);
            var evmSimulator = new EVMSimulator();
            program = await evmSimulator.ExecuteAsync(program);
           

            var sourceMapUtil = new SourceMapUtil();
            var sourceMaps = new Dictionary<string, List<SourceMap>>
            {
                { AddressUtil.Current.ConvertToValid20ByteAddress(payableTestSenderContractAddress).ToLower(), sourceMapUtil.UnCompressSourceMap(sourceMapPayableTestSender) },
                { AddressUtil.Current.ConvertToValid20ByteAddress(payableReceiverContractAddress).ToLower(), sourceMapUtil.UnCompressSourceMap(sourceMapPayableReceiverContract) }
            };

            var programAddressAsKey = AddressUtil.Current.ConvertToValid20ByteAddress(program.ProgramContext.AddressContract).ToLower();
            if (sourceMaps.ContainsKey(programAddressAsKey))
            {
                var sourceMap = sourceMaps[programAddressAsKey];
                for (var i = 0; i < sourceMap.Count; i++)
                {
                    program.Instructions[i].SourceMap = sourceMap[i];
                }
            }


            foreach (var programCode in program.ProgramResult.InnerContractCodeCalls)
            {
                if (sourceMaps.ContainsKey(programCode.Key))
                {
                    var sourceMap = sourceMaps[programCode.Key];
                    for (var i = 0; i < sourceMap.Count; i++)
                    {
                        programCode.Value[i].SourceMap = sourceMap[i];
                    }
                }
            }

            foreach (var trace in program.Trace)
            {
                Debug.WriteLine(trace.VMTraceStep);
                Debug.WriteLine(trace.Instruction.Instruction.ToString());
                Debug.WriteLine(trace.CodeAddress);
                if ((trace.Instruction.SourceMap.Position + trace.Instruction.SourceMap.Length) < source.Length && trace.Instruction.SourceMap.Position > 0)
                {
                    Debug.WriteLine(source.Substring(trace.Instruction.SourceMap.Position, trace.Instruction.SourceMap.Length));
                }
            }
        }

        string sourceMapPayableReceiverContract = "35:323:0:-:0;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;77:25;;;;;;;;;;;;;;;;;;;160::1;;;148:2;133:18;77:25:0;;;;;;;250:105;;;;;;;;;;-1:-1:-1;326:21:0;250:105;;109:133;185:21;152:7;172:34;;;109:133;";
        string sourceMapPayableTestSender = "362:229:0:-:0;;;;;;;;;;;;;;;;;;;;;;;;;;396:25;;;;;;;;;;;;;;;;;;;160::1;;;148:2;133:18;396:25:0;;;;;;;430:158;;;;;;:::i;:::-;;:::i;:::-;;;535:16;-1:-1:-1;;;;;535:22:0;;567:9;535:45;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;:::i;:::-;522:10;:58;-1:-1:-1;430:158:0:o;196:316:1:-;285:6;338:2;326:9;317:7;313:23;309:32;306:52;;;354:1;351;344:12;306:52;380:23;;-1:-1:-1;;;;;432:31:1;;422:42;;412:70;;478:1;475;468:12;412:70;501:5;196:316;-1:-1:-1;;;196:316:1:o;517:184::-;587:6;640:2;628:9;619:7;615:23;611:32;608:52;;;656:1;653;646:12;608:52;-1:-1:-1;679:16:1;;517:184;-1:-1:-1;517:184:1:o";

        string source = 
@"pragma solidity >=0.7.0 <0.9.0;

contract PayableReceiverContract {

    uint256 public paidAmount;
    function payMe() external payable returns (uint256) {
        paidAmount = address(this).balance;
        return paidAmount;
    }

    function balanceContract() external view returns (uint256){
        return address(this).balance;
    }
}

contract PayableTestSender {
    uint256 public paidAmount;

    function payMeAndSend(PayableReceiverContract recieverContract) external payable {
        paidAmount = recieverContract.payMe { value: msg.value }();
    }
}";

        public partial class PayableTestSenderDeployment : PayableTestSenderDeploymentBase
        {
            public PayableTestSenderDeployment() : base(BYTECODE) { }
            public PayableTestSenderDeployment(string byteCode) : base(byteCode) { }
        }

        public class PayableTestSenderDeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "608060405234801561001057600080fd5b50610164806100206000396000f3fe6080604052600436106100295760003560e01c806312fa769f1461002e578063ebfcd76014610056575b600080fd5b34801561003a57600080fd5b5061004460005481565b60405190815260200160405180910390f35b6100696100643660046100e5565b61006b565b005b806001600160a01b031663d997ccb3346040518263ffffffff1660e01b81526004016020604051808303818588803b1580156100a657600080fd5b505af11580156100ba573d6000803e3d6000fd5b50505050506040513d601f19601f820116820180604052508101906100df9190610115565b60005550565b6000602082840312156100f757600080fd5b81356001600160a01b038116811461010e57600080fd5b9392505050565b60006020828403121561012757600080fd5b505191905056fea2646970667358221220c7e7d58c7a60f94540e1792c0cdb89172d74fcdf0f2b0f4d5646046eb1473a1464736f6c63430008090033";
            public PayableTestSenderDeploymentBase() : base(BYTECODE) { }
            public PayableTestSenderDeploymentBase(string byteCode) : base(byteCode) { }

        }

        public partial class PayMeAndSendFunction : PayMeAndSendFunctionBase { }

        [Function("payMeAndSend")]
        public class PayMeAndSendFunctionBase : FunctionMessage
        {
            [Parameter("address", "recieverContract", 1)]
            public virtual string RecieverContract { get; set; }
        }

        public partial class PaidAmountFunction : PaidAmountFunctionBase { }

        [Function("paidAmount", "uint256")]
        public class PaidAmountFunctionBase : FunctionMessage
        {

        }



        public partial class PaidAmountOutputDTO : PaidAmountOutputDTOBase { }

        [FunctionOutput]
        public class PaidAmountOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("uint256", "", 1)]
            public virtual BigInteger ReturnValue1 { get; set; }
        }

        public partial class PayableReceiverContractDeployment : PayableReceiverContractDeploymentBase
        {
            public PayableReceiverContractDeployment() : base(BYTECODE) { }
            public PayableReceiverContractDeployment(string byteCode) : base(byteCode) { }
        }

        public class PayableReceiverContractDeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "6080604052348015600f57600080fd5b5060ac8061001e6000396000f3fe60806040526004361060305760003560e01c806312fa769f146035578063322a5e5f14605b578063d997ccb314606c575b600080fd5b348015604057600080fd5b50604960005481565b60405190815260200160405180910390f35b348015606657600080fd5b50476049565b476000819055604956fea2646970667358221220e730dbb658a9da6b18d44e355856fb5aef90abdd725d954dffb23c9120cabfe964736f6c63430008090033";
            public PayableReceiverContractDeploymentBase() : base(BYTECODE) { }
            public PayableReceiverContractDeploymentBase(string byteCode) : base(byteCode) { }

        }

        public partial class PayMeFunction : PayMeFunctionBase { }

        [Function("payMe", "uint256")]
        public class PayMeFunctionBase : FunctionMessage
        {

        }

        public partial class BalanceContractFunction : BalanceContractFunctionBase { }

        [Function("balanceContract", "uint256")]
        public class BalanceContractFunctionBase : FunctionMessage
        {

        }

        public partial class BalanceContractOutputDTO : BalanceContractOutputDTOBase { }

        [FunctionOutput]
        public class BalanceContractOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("uint256", "", 1)]
            public virtual BigInteger ReturnValue1 { get; set; }
        }
    }
}
﻿using System.Collections.Generic;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting
// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.CQS
{


        [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class FixedArrayQuery
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public FixedArrayQuery(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void MultiDimensionShouldUseSolidityNestedNotationToDescribeReturnButDecodeToCSharp()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var contractAddress = "0x5EF1009b9FCD4fec3094a5564047e190D72Bd511";
            var contractHandler = web3.Eth.GetContractHandler(contractAddress);

            var getPairsByIndexRangeFunction = new GetPairsByIndexRangeFunction();
            getPairsByIndexRangeFunction.UniswapFactory = "0x5C69bEe701ef814a2B6a3EDD4B1652CB9cc5aA6f";
            getPairsByIndexRangeFunction.Start = 0;
            getPairsByIndexRangeFunction.Stop = 10;

            var getPairsByIndexRangeFunctionReturn = await contractHandler.QueryAsync<GetPairsByIndexRangeFunction, List<List<string>>>(getPairsByIndexRangeFunction);

            Assert.Equal(10, getPairsByIndexRangeFunctionReturn.Count);
            Assert.Equal(3, getPairsByIndexRangeFunctionReturn[0].Count);
        }


        public partial class GetPairsByIndexRangeFunction : GetPairsByIndexRangeFunctionBase { }

        [Function("getPairsByIndexRange", "address[3][]")]
        public class GetPairsByIndexRangeFunctionBase : FunctionMessage
        {
            [Parameter("address", "_uniswapFactory", 1)]
            public virtual string UniswapFactory { get; set; }
            [Parameter("uint256", "_start", 2)]
            public virtual BigInteger Start { get; set; }
            [Parameter("uint256", "_stop", 3)]
            public virtual BigInteger Stop { get; set; }
        }

        [Fact]
        public async void TestCQS()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new TestContractDeployment() {FromAddress = senderAddress};
            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TestContractDeployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage);
            var contractHandler = web3.Eth.GetContractHandler(transactionReceipt.ContractAddress);
            var result = await contractHandler.QueryAsync<ReturnArrayFunction, List<string>>();
            Assert.Equal(10, result.Count);
        }

        public class TestContractDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "0x6060604052341561000f57600080fd5b6101f48061001e6000396000f3006060604052600436106100405763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416633cac14c88114610045575b600080fd5b341561005057600080fd5b610058610091565b604051808261014080838360005b8381101561007e578082015183820152602001610066565b5050505090500191505060405180910390f35b61009961019f565b7312890d2cce102216644c59dae5baed380d84830c81527312890d2cce102216644c59dae5baed380d84830160208201527312890d2cce102216644c59dae5baed380d84830260408201527312890d2cce102216644c59dae5baed380d84830360608201527312890d2cce102216644c59dae5baed380d84830460808201527312890d2cce102216644c59dae5baed380d84830560a08201527312890d2cce102216644c59dae5baed380d84830660c08201527312890d2cce102216644c59dae5baed380d84830760e08201527312890d2cce102216644c59dae5baed380d8483086101008201527312890d2cce102216644c59dae5baed380d84830961012082015290565b610140604051908101604052600a815b6000815260001990910190602001816101af57905050905600a165627a7a723058208d1ac3fcf253acee694131c355fd6eada9fec4570b122d449fe950cc09d4a5490029";

            public TestContractDeployment() : base(BYTECODE)
            {
            }

            public TestContractDeployment(string byteCode) : base(byteCode)
            {
            }
        }

        [Function("returnArray", "address[10]")]
        public class ReturnArrayFunction : FunctionMessage
        {
        }

        //*Smart contract
        /*
         contract TestContrac2{
        function returnArray() public view returns(address[10] memory addresses){
            addresses[0] = 0x12890D2cce102216644c59daE5baed380d84830c;
            addresses[1] = 0x12890D2cCe102216644c59DaE5Baed380D848301;
            addresses[2] = 0x12890D2cce102216644c59daE5baed380d848302;
            addresses[3] = 0x12890d2cce102216644c59daE5baed380d848303;
            addresses[4] = 0x12890d2cce102216644c59daE5baed380d848304;
            addresses[5] = 0x12890d2cce102216644c59daE5baed380d848305;
            addresses[6] = 0x12890d2cce102216644c59daE5baed380d848306;
            addresses[7] = 0x12890d2cce102216644c59daE5baed380d848307;
            addresses[8] = 0x12890d2cce102216644c59daE5baed380d848308;
            addresses[9] = 0x12890d2cce102216644c59daE5baed380d848309;
            return addresses;
        }
        }
        */
    }
}﻿using System.Collections.Generic;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting
// ReSharper disable ConsiderUsingConfigureAwait
namespace Nethereum.Contracts.IntegrationTests.CQS
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class FixedMultipleArrayQuery
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public FixedMultipleArrayQuery(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void TestCQS()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new TestContractDeployment() {FromAddress = senderAddress};
            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TestContractDeployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage);
            var contractHandler = web3.Eth.GetContractHandler(transactionReceipt.ContractAddress);
            var result = await contractHandler.QueryAsync<ReturnArrayFunction, List<List<int>>>();
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0].Count);
        }

        [Function("returnArray", "int256[2][2]")]
        public class ReturnArrayFunction : FunctionMessage
        {
        }

        public class TestContractDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "0x6060604052341561000f57600080fd5b6101618061001e6000396000f3006060604052600436106100405763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416633cac14c88114610045575b600080fd5b341561005057600080fd5b6100586100b2565b6040516000826002835b818410156100a25782846020020151604080838360005b83811015610091578082015183820152602001610079565b505050509050019260010192610062565b9250505091505060405180910390f35b6100ba6100e2565b6001815152600281516001602002015260018181602002015152600260208201516020015290565b60806040519081016040526002815b6100f961010f565b8152602001906001900390816100f15790505090565b604080519081016040526002815b600081526020019060019003908161011d57905050905600a165627a7a72305820d6fbdcd20aa2df88d4cb7700f4abe8b955740ec5ba3c4101ba0e8819677de5810029";

            public TestContractDeployment() : base(BYTECODE)
            {
            }

            public TestContractDeployment(string byteCode) : base(byteCode)
            {
            }
        }

        /*contract
         * contract TestContract3{
            function returnArray() public view returns(int[2][2] memory array){
                array[0][0] = 1;
                array[0][1] = 2;
                array[1][0] = 1;
                array[1][1] = 2;
                return array;
                }
            }
        */
    }
}﻿using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EncodingInputOutput
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class FunctionOutputDTOTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public FunctionOutputDTOTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        public class TestOutputService
        {
            public static string ABI =
                @"[{'constant':false,'inputs':[],'name':'getData','outputs':[{'name':'birthTime','type':'uint64'},{'name':'userName','type':'string'},{'name':'starterId','type':'uint16'},{'name':'currLocation','type':'uint16'},{'name':'isBusy','type':'bool'},{'name':'owner','type':'address'}],'payable':false,'stateMutability':'nonpayable','type':'function'}]";

            public static string BYTE_CODE =
                "0x6060604052341561000f57600080fd5b6101c88061001e6000396000f3006060604052600436106100405763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416633bc5de308114610045575b600080fd5b341561005057600080fd5b61005861011a565b60405167ffffffffffffffff8716815261ffff808616604083015284166060820152821515608082015273ffffffffffffffffffffffffffffffffffffffff821660a082015260c06020820181815290820187818151815260200191508051906020019080838360005b838110156100da5780820151838201526020016100c2565b50505050905090810190601f1680156101075780820380516001836020036101000a031916815260200191505b5097505050505050505060405180910390f35b600061012461018a565b6000806000806001955060408051908101604052600481527f6a75616e0000000000000000000000000000000000000000000000000000000060208201529596600195508594506000935073de0b295669a9fd93d5f28d9ec85e40f4cb697bae92509050565b602060405190810160405260008152905600a165627a7a72305820ba7625d1c6f0f2844d32ad76e28729e80979f69cbd32d0589995f24cb969a6850029"; /*
            pragma solidity ^0.4.19;

            contract TestOutput {

                function getData() returns (uint64 birthTime, string userName, uint16 starterId, uint16 currLocation, bool isBusy, address owner ) {
                    birthTime = 1;
                    userName = "juan";
                    starterId = 1;
                    currLocation = 1;
                    isBusy = false;
                    owner = 0xde0b295669a9fd93d5f28d9ec85e40f4cb697bae;
                }
            }

            */

            private readonly Web3.Web3 web3;

            private readonly Contract contract;

            public TestOutputService(Web3.Web3 web3, string address)
            {
                this.web3 = web3;
                contract = web3.Eth.GetContract(ABI, address);
            }

            public Function GetFunctionGetData()
            {
                return contract.GetFunction("getData");
            }


            public Task<string> GetDataAsync(string addressFrom, HexBigInteger gas = null,
                HexBigInteger valueAmount = null)
            {
                var function = GetFunctionGetData();
                return function.SendTransactionAsync(addressFrom, gas, valueAmount);
            }

            public Task<GetDataDTO> GetDataAsyncCall()
            {
                var function = GetFunctionGetData();
                return function.CallDeserializingToObjectAsync<GetDataDTO>();
            }
        }

        [FunctionOutput]
        public class GetDataDTO
        {
            [Parameter("uint64", "birthTime", 1)] public ulong BirthTime { get; set; }

            [Parameter("string", "userName", 2)] public string UserName { get; set; }

            [Parameter("uint16", "starterId", 3)] public int StarterId { get; set; }

            [Parameter("uint16", "currLocation", 4)]
            public int CurrLocation { get; set; }

            [Parameter("bool", "isBusy", 5)] public bool IsBusy { get; set; }

            [Parameter("address", "owner", 6)] public string Owner { get; set; }
        }

        [Fact]
        public async void ShouldReturnFunctionOutputDTO()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            var contractReceipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(TestOutputService.ABI,
                TestOutputService.BYTE_CODE, senderAddress, new HexBigInteger(900000));
            var service = new TestOutputService(web3, contractReceipt.ContractAddress);
            var message = await service.GetDataAsyncCall();
            Assert.Equal(1, (int) message.BirthTime);
            Assert.Equal(1, message.CurrLocation);
            Assert.Equal(1, message.StarterId);
            Assert.False(message.IsBusy);
            Assert.Equal("juan", message.UserName);
            Assert.Equal("0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe", message.Owner);
        }
    }
}﻿using System.Collections.Generic;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Geth;
using Nethereum.Geth.RPC.DTOs;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Contracts.IntegrationTests.SmartContracts
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class GethCallTest
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public GethCallTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }
        //curl --data '{"method":"eth_call","params":[{"to":"0xd0828aeb00e4db6813e2f330318ef94d2bba2f60","input":"0x893d20e8"}, "latest", {"0xd0828aeb00e4db6813e2f330318ef94d2bba2f60": {"code":"0x6080604052348015600f57600080fd5b506004361060285760003560e01c8063893d20e814602d575b600080fd5b600054604080516001600160a01b039092168252519081900360200190f3fea2646970667358221220dbb42870b3edb8a876ba4948e51e4e2b8fe47ae467ed612734a355d3dcf676dc64736f6c63430008040033"}}],"id":1,"jsonrpc":"2.0"}' -H "Content-Type: application/json" -X POST 192.168.2.153:8545
        [Fact]
        public async void ShouldBeAbleToReplaceContractToAccessState()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var account = EthereumClientIntegrationFixture.GetAccount();
                var web3 = new Web3Geth(account, _ethereumClientIntegrationFixture.GetHttpUrl());

                var deploymentMessage = new SimpleStorageDeployment()
                    {Owner = EthereumClientIntegrationFixture.AccountAddress};

                var deploymentHandler = web3.Eth.GetContractDeploymentHandler<SimpleStorageDeployment>();
                var deploymentReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage).ConfigureAwait(false);

                var stateChanges = new Dictionary<string, StateChange>();
                stateChanges.Add(deploymentReceipt.ContractAddress,
                    new StateChange() {Code = SimpleStorage2DeployedByteCode.EnsureHexPrefix()});
                var result = await web3.GethEth.Call.SendRequestAsync(
                    new GetOwnerFunction().CreateTransactionInput(deploymentReceipt.ContractAddress),
                    BlockParameter.CreateLatest(), stateChanges).ConfigureAwait(false);
                var output = new GetOwnerFunctionOutput();
                output = output.DecodeOutput(result);
                Assert.True(output.Owner.IsTheSameAddress(EthereumClientIntegrationFixture.AccountAddress));
            }
        }
        //Original contract
        /*
            pragma solidity >=0.5.0 <0.9.0;
            contract SimpleStorage {
                address private owner;
                constructor(address _owner)
                {
                    owner = _owner;
                }
            }

        */

        public class SimpleStorageDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE = "6080604052348015600f57600080fd5b5060405160c638038060c6833981016040819052602a91604e565b600080546001600160a01b0319166001600160a01b0392909216919091179055607a565b600060208284031215605e578081fd5b81516001600160a01b03811681146073578182fd5b9392505050565b603f8060876000396000f3fe6080604052600080fdfea26469706673582212206baffd7b6dc8e96c3a57d1dad9d24e01daa26556489015858d60e5ffdceaeead64736f6c63430008040033";
            public SimpleStorageDeployment() : base(BYTECODE) { }
            public SimpleStorageDeployment(string byteCode) : base(byteCode) { }
            [Parameter("address", "_owner", 1)]
            public virtual string Owner { get; set; }
        }

        //Replacement contract

        /*
        pragma solidity >=0.5.0 <0.9.0;
        contract SimpleStorage2 {
            address private owner;
            function getOwner() public view returns(address _owner) {
                return owner;
            }
        }

        */
        public static string SimpleStorage2DeployedByteCode =
            "6080604052348015600f57600080fd5b506004361060285760003560e01c8063893d20e814602d575b600080fd5b600054604080516001600160a01b039092168252519081900360200190f3fea2646970667358221220dbb42870b3edb8a876ba4948e51e4e2b8fe47ae467ed612734a355d3dcf676dc64736f6c63430008040033";

        [Function("getOwner", "address")]
        public class GetOwnerFunction : FunctionMessage
        {

        }

        [FunctionOutput]
        public class GetOwnerFunctionOutput : IFunctionOutputDTO
        {
            [Parameter("address", "_owner", 1)]
            public virtual string Owner { get; set; }
        }
    }
}﻿using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EncodingInputOutput
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class IndexedEvents
    {
        /*
        contract EventTest {
           event Event(uint first, uint indexed second, uint third, uint indexed fourth);
           function sendEvent() {
               Event(1,2,3,4);
           }
        }
        */

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public IndexedEvents(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        public static string ABI =
            @"[{'constant':false,'inputs':[],'name':'sendEvent','outputs':[],'payable':false,'type':'function'},{'anonymous':false,'inputs':[{'indexed':false,'name':'first','type':'uint256'},{'indexed':true,'name':'second','type':'uint256'},{'indexed':false,'name':'third','type':'uint256'},{'indexed':true,'name':'fourth','type':'uint256'}],'name':'Event','type':'event'}]";

        public static string BYTE_CODE =
            "0x6060604052341561000f57600080fd5b5b60bd8061001e6000396000f300606060405263ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166332b7a7618114603c575b600080fd5b3415604657600080fd5b604c604e565b005b600460027f07392b89121f4d481601a3db92f2daf8c73cc086a942b4522c58abcbdeac4d016001600360405191825260208201526040908101905180910390a35b5600a165627a7a723058202af7567cbbe622cac1fcce7d7f4aa0be6c879974474c7f7580a9fea9d4dfa5850029";

        [Event("Event")]
        public class EventEventDTO : IEventDTO
        {
            [Parameter("uint256", "first", 1, false)]
            public BigInteger First { get; set; }

            [Parameter("uint256", "second", 2, true)]
            public BigInteger Second { get; set; }

            [Parameter("uint256", "third", 3, false)]
            public BigInteger Third { get; set; }

            [Parameter("uint256", "fourth", 4, true)]
            public BigInteger Fourth { get; set; }
        }

        [Fact]
        public async void ShouldBeParsedInAnyOrder()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(BYTE_CODE, senderAddress,
                    new HexBigInteger(900000), null).ConfigureAwait(false);
            var contract = web3.Eth.GetContract(ABI, receipt.ContractAddress);

            var function = contract.GetFunction("sendEvent");
            receipt = await function.SendTransactionAndWaitForReceiptAsync(senderAddress, new HexBigInteger(900000),
                null, null).ConfigureAwait(false);


            var eventLog = contract.GetEvent("Event");
            var events = eventLog.DecodeAllEventsForEvent<EventEventDTO>(receipt.Logs);

            Assert.Equal(1, events[0].Event.First);
            Assert.Equal(2, events[0].Event.Second);
            Assert.Equal(3, events[0].Event.Third);
            Assert.Equal(4, events[0].Event.Fourth);
        }

        [Fact]
        public async void ShouldBeParsedInAnyOrderUsingExtensions()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(BYTE_CODE, senderAddress,
                    new HexBigInteger(900000), null).ConfigureAwait(false);
            var contract = web3.Eth.GetContract(ABI, receipt.ContractAddress);

            var function = contract.GetFunction("sendEvent");
            receipt = await function.SendTransactionAndWaitForReceiptAsync(senderAddress, new HexBigInteger(900000),
                null, null).ConfigureAwait(false);

            var events = receipt.Logs.DecodeAllEvents<EventEventDTO>();

            Assert.Equal(1, events[0].Event.First);
            Assert.Equal(2, events[0].Event.Second);
            Assert.Equal(3, events[0].Event.Third);
            Assert.Equal(4, events[0].Event.Fourth);
        }
    }
}using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EncodingInputOutput
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class IntTypeIntegrationTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public IntTypeIntegrationTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        public async Task<string> Test()
        {
            //The compiled solidity contract to be deployed
            /*
              contract test { 
    
    
                function test1() returns(int) { 
                   int d = 3457987492347979798742;
                   return d;
                }
    
                  function test2(int d) returns(int) { 
                   return d;
                }
    
                function test3(int d)returns(int){
                    int x = d + 1 -1;
                    return x;
                }
    
                function test4(int d)returns(bool){
                    return d == 3457987492347979798742;
                }
    
                function test5(int d)returns(bool){
                    return d == -3457987492347979798742;
                }
    
                function test6(int d)returns(bool){
                    return d == 500;
                }
    
                function test7(int256 d)returns(bool){
                    return d == 74923479797565;
                }
    
                function test8(int256 d)returns(bool){
                    return d == 9223372036854775808;
                }
            }
           }
           */

            var contractByteCode =
                "60606040526102b7806100126000396000f36060604052361561008a576000357c01000000000000000000000000000000000000000000000000000000009004806311da9d8c1461008c5780631c2a1101146100b857806363798981146100e45780636b59084d146101105780639e71212514610133578063a605861c1461015f578063e42d455b1461018b578063e92b09da146101b75761008a565b005b6100a26004808035906020019091905050610243565b6040518082815260200191505060405180910390f35b6100ce600480803590602001909190505061020e565b6040518082815260200191505060405180910390f35b6100fa60048080359060200190919050506101ff565b6040518082815260200191505060405180910390f35b61011d60048050506101e3565b6040518082815260200191505060405180910390f35b6101496004808035906020019091905050610229565b6040518082815260200191505060405180910390f35b6101756004808035906020019091905050610274565b6040518082815260200191505060405180910390f35b6101a1600480803590602001909190505061029e565b6040518082815260200191505060405180910390f35b6101cd6004808035906020019091905050610287565b6040518082815260200191505060405180910390f35b6000600068bb75377716692498d690508091506101fb565b5090565b6000819050610209565b919050565b60006000600160018401039050809150610223565b50919050565b600068bb75377716692498d68214905061023e565b919050565b60007fffffffffffffffffffffffffffffffffffffffffffffff448ac888e996db672a8214905061026f565b919050565b60006101f482149050610282565b919050565b60006544247b660f3d82149050610299565b919050565b6000678000000000000000821490506102b2565b91905056";

            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test5"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test3"",""outputs"":[{""name"":"""",""type"":""int256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test2"",""outputs"":[{""name"":"""",""type"":""int256""}],""type"":""function""},{""constant"":false,""inputs"":[],""name"":""test1"",""outputs"":[{""name"":"""",""type"":""int256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test4"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test6"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test8"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""d"",""type"":""int256""}],""name"":""test7"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""}]";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            //deploy the contract, including abi and a paramter of 7. 
            var transactionHash =
                await
                    web3.Eth.DeployContract.SendRequestAsync(contractByteCode, senderAddress,
                        new HexBigInteger(900000)).ConfigureAwait(false);

            //get the contract address 
            TransactionReceipt receipt = null;
            //wait for the contract to be mined to the address
            while (receipt == null)
            {
                await Task.Delay(500).ConfigureAwait(false);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).ConfigureAwait(false);
            }

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);
            var test1 = contract.GetFunction("test1");
            Assert.Equal("3457987492347979798742", (await test1.CallAsync<BigInteger>().ConfigureAwait(false)).ToString());
            var test2 = contract.GetFunction("test2");
            Assert.Equal("3457987492347979798742",
                (await test2.CallAsync<BigInteger>(BigInteger.Parse("3457987492347979798742")).ConfigureAwait(false)).ToString());

            var test3 = contract.GetFunction("test3");
            Assert.Equal("3457987492347979798742",
                (await test3.CallAsync<BigInteger>(BigInteger.Parse("3457987492347979798742")).ConfigureAwait(false)).ToString());

            var test4 = contract.GetFunction("test4");
            Assert.True(await test4.CallAsync<bool>(BigInteger.Parse("3457987492347979798742")).ConfigureAwait(false));

            var test5 = contract.GetFunction("test5");
            Assert.True(await test5.CallAsync<bool>(BigInteger.Parse("-3457987492347979798742")).ConfigureAwait(false));

            var test6 = contract.GetFunction("test6");
            Assert.True(await test6.CallAsync<bool>(BigInteger.Parse("500")).ConfigureAwait(false));

            var test8 = contract.GetFunction("test8");
            Assert.True(await test8.CallAsync<bool>(BigInteger.Parse("9223372036854775808")).ConfigureAwait(false));

            return "OK";
        }
    }
}﻿using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

// ReSharper disable AsyncConverter.AsyncWait

namespace Nethereum.Contracts.IntegrationTests.Issues
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class Issue24
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public Issue24(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        //Issue on the original Event speficifing a string instead of an address type
        /*
          
          public class EventBatchUploaded
        {
            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", 1)]
            public string address { get; set; }

            [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("bytes", 2)]
            public byte[] hashBytes { get; set; }
        }
        
        */

        [Event(("BatchUploaded"))]
        public class EventBatchUploaded : IEventDTO
        {
            [Parameter("address", 1)] public string address { get; set; }

            [Parameter("bytes", 2)] public string hashBytes { get; set; }
        }


        [Fact]
        public void BytesTest()
        {
            var bytes =
                "000000000000000000000000000000000000000000000000000000000000002e516d5074633431505661375945585a7359524448586a6332525753474c47794b396774787a524e6543354b4e5641000000000000000000000000000000000000";

            var bytesType = new BytesType();
            var bytesArray = bytes.HexToByteArray();
            var decoded = (byte[]) bytesType.Decode(bytesArray, typeof(byte[]));
            var stringValue = (string) bytesType.Decode(bytesArray, typeof(string));
        }

        //This was a problem on event type declaration, see the Mordern test
        [Fact]
        public void MordenTest()
        {
            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""multihash"",""type"":""bytes""}],""name"":""uploadBatch"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_blockHeight"",""type"":""uint256""},{""name"":""_contentBy"",""type"":""address""},{""name"":""_changeCount"",""type"":""uint256""},{""name"":""_totalRexRewarded"",""type"":""uint256""}],""name"":""issueContentReward"",""outputs"":[],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_address"",""type"":""address""}],""name"":""updateCoordinator"",""outputs"":[],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""uploadCount"",""outputs"":[{""name"":"""",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""_dataFeedCoordinatorAddress"",""type"":""address""},{""name"":""feedCode"",""type"":""bytes4""}],""type"":""constructor""},{""anonymous"":false,""inputs"":[{""indexed"":false,""name"":"""",""type"":""address""},{""indexed"":false,""name"":"""",""type"":""bytes""}],""name"":""BatchUploaded"",""type"":""event""},{""anonymous"":false,""inputs"":[{""indexed"":false,""name"":""blockHeight"",""type"":""uint256""},{""indexed"":false,""name"":""contentBy"",""type"":""address""},{""indexed"":false,""name"":""changeCount"",""type"":""uint256""},{""indexed"":false,""name"":""totalRewards"",""type"":""uint256""}],""name"":""ContentRewarded"",""type"":""event""}]";
            var contractAddress = "0xe8d75008917c6a460473e62d5d4cefd3bbe4d85b";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var dataFeedContract = web3.Eth.GetContract(abi, contractAddress);

            var e = dataFeedContract.GetEvent("BatchUploaded");
            var filterId = e.CreateFilterAsync(new BlockParameter(500000)).Result;
            var changes = e.GetAllChangesAsync<EventBatchUploaded>(filterId).Result;
        }
    }
}﻿using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.Issues
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class Issue78
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public Issue78(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task ProdContract()
        {
            var byteCode =
                "0x606060405260405161069d38038061069d833981016040528080518201919060200150505b33600160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908302179055508060036000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061009e57805160ff19168380011785556100cf565b828001600101855582156100cf579182015b828111156100ce5782518260005055916020019190600101906100b0565b5b5090506100fa91906100dc565b808211156100f657600081815060009055506001016100dc565b5090565b50505b506105918061010c6000396000f360606040526000357c0100000000000000000000000000000000000000000000000000000000900480632d202d24146100685780634ba200501461009b578063893d20e81461011b578063a0e67e2b14610159578063d5d1e770146101b557610063565b610002565b346100025761008360048080359060200190919050506101df565b60405180821515815260200191505060405180910390f35b34610002576100ad600480505061027a565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600302600f01f150905090810190601f16801561010d5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b346100025761012d6004805050610336565b604051808273ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b346100025761016b6004805050610365565b60405180806020018281038252838181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050019250505060405180910390f35b34610002576101c760048050506103f7565b60405180821515815260200191505060405180910390f35b60003373ffffffffffffffffffffffffffffffffffffffff16600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff161415156102415760009050610275565b81600260006101000a81548173ffffffffffffffffffffffffffffffffffffffff0219169083021790555060019050610275565b919050565b602060405190810160405280600081526020015060036000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156103275780601f106102fc57610100808354040283529160200191610327565b820191906000526020600020905b81548152906001019060200180831161030a57829003601f168201915b50505050509050610333565b90565b6000600160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff169050610362565b90565b602060405190810160405280600081526020015060006000508054806020026020016040519081016040528092919081815260200182805480156103e857602002820191906000526020600020905b8160009054906101000a900473ffffffffffffffffffffffffffffffffffffffff16815260200190600101908083116103b4575b505050505090506103f4565b90565b60003373ffffffffffffffffffffffffffffffffffffffff16600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16141515610459576000905061058e565b600060005080548060010182818154818355818115116104ab578183600052602060002091820191016104aa919061048c565b808211156104a6576000818150600090555060010161048c565b5090565b5b5050509190906000526020600020900160005b600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff16909190916101000a81548173ffffffffffffffffffffffffffffffffffffffff0219169083021790555050600260009054906101000a900473ffffffffffffffffffffffffffffffffffffffff16600160006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908302179055506000600260006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908302179055506001905061058e565b9056";
            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""nextOwner"",""type"":""address""}],""name"":""setNextOwner"",""outputs"":[{""name"":""set"",""type"":""bool""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getProduct"",""outputs"":[{""name"":""product"",""type"":""string""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getOwner"",""outputs"":[{""name"":""owner"",""type"":""address""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getOwners"",""outputs"":[{""name"":""owners"",""type"":""address[]""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""confirmOwnership"",""outputs"":[{""name"":""confirmed"",""type"":""bool""}],""payable"":false,""type"":""function""},{""inputs"":[{""name"":""productDigest"",""type"":""string""}],""type"":""constructor""}]";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = EthereumClientIntegrationFixture.AccountAddress;

            var contractHash = await web3.Eth.DeployContract.SendRequestAsync(abi, byteCode, account,
                new HexBigInteger(900 * 1000), "My product").ConfigureAwait(false);

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(contractHash).ConfigureAwait(false);
            while (receipt == null)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(contractHash).ConfigureAwait(false);
            }

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);
            var code = await web3.Eth.GetCode.SendRequestAsync(receipt.ContractAddress).ConfigureAwait(false);

            Assert.True(!string.IsNullOrEmpty(code) && code.Length > 3);

            var function = contract.GetFunction("getProduct");
            var result = await function.CallAsync<string>().ConfigureAwait(false);

            Assert.Equal("My product", result);
        }
    }
}﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Moq;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.XUnitEthereumClients;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable ConsiderUsingConfigureAwait  
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.Logging
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class LoggingTest
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public LoggingTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
     
        }

        [Fact]
        public async void TestLogging()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var loggerMock = new Mock<ILogger<RpcClient>>();
               

                var contractByteCode =
                    "0x6060604052604060405190810160405280600a81526020017f4d756c7469706c6965720000000000000000000000000000000000000000000081526020015060016000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061008c57805160ff19168380011785556100bd565b828001600101855582156100bd579182015b828111156100bc57825182600050559160200191906001019061009e565b5b5090506100e891906100ca565b808211156100e457600081815060009055506001016100ca565b5090565b5050604051602080610303833981016040528080519060200190919050505b806000600050819055505b506101e2806101216000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806340490a901461004f57806375d0c0dc14610072578063c6888fa1146100ed5761004d565b005b61005c6004805050610119565b6040518082815260200191505060405180910390f35b61007f6004805050610141565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600f02600301f150905090810190601f1680156100df5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b610103600480803590602001909190505061012b565b6040518082815260200191505060405180910390f35b60006000600050549050610128565b90565b60006000600050548202905061013c565b919050565b60016000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156101da5780601f106101af576101008083540402835291602001916101da565b820191906000526020600020905b8154815290600101906020018083116101bd57829003601f168201915b50505050508156";

                var abi =
                    @"[{""constant"":true,""inputs"":[],""name"":""getMultiplier"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""contractName"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""}]";

                var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
                var web3 = new Web3.Web3(_ethereumClientIntegrationFixture.GetWeb3().TransactionManager.Account,
                    client: new RpcClient(
                        new Uri("http://localhost:8545"), authHeaderValue: null, null, null,
                        loggerMock.Object));
                web3.TransactionManager.UseLegacyAsDefault = true;
               
                BigInteger nonce = 0;

                try
                {
                    var transactionHash2 = await web3.Eth.DeployContract.SendRequestAsync(abi, contractByteCode,
                        senderAddress, // lower gas
                        new HexBigInteger(900), 7).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                }

                //Parity is:
                //RPC Response Error: Transaction gas is too low. 
                
                loggerMock.VerifyLog(logger => logger.LogTrace("*eth_getTransactionCount*"));
                loggerMock.VerifyLog(logger => logger.LogTrace("*eth_gasPrice*"));
                loggerMock.VerifyLog(logger => logger.LogTrace("*eth_sendRawTransaction*")); 
                loggerMock.VerifyLog(logger => logger.LogError("RPC Response Error: intrinsic gas too low"));

            }
        }
    }
}﻿using System.Threading;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Fee1559Suggestions;
using Nethereum.XUnitEthereumClients;
using Xunit;
// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Signer.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class MedianPriorityFeeHistorySuggestionStrategy1559Tests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public MedianPriorityFeeHistorySuggestionStrategy1559Tests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        [Fact]
        public async void ShouldBeAbleToCalculateHistoryMedium()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {

                var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Goerli);

               
                var feeStrategy = new MedianPriorityFeeHistorySuggestionStrategy(web3.Client);
                for (var x = 0; x < 10; x++)
                {
                    Thread.Sleep(500);
                    var fee = await feeStrategy.SuggestFeeAsync().ConfigureAwait(false);
                }
            }
        }


        [Fact]
        public async void ShouldBeAbleToCalculateHistoryAndSend1000sOfTransactions2()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var receiveAddress = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";

                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
#if NETCOREAPP3_1_OR_GREATER || NET50
                EthECKey.SignRecoverable = true;
#endif
                var feeStrategy = new MedianPriorityFeeHistorySuggestionStrategy(web3.Client);
                for (var x = 0; x < 10; x++)
                {
                    Thread.Sleep(500);
                    var fee = await feeStrategy.SuggestFeeAsync().ConfigureAwait(false);
                    for (int i = 0; i < 50; i++)
                    {
                        var encoded = await web3.TransactionManager.SendTransactionAsync(
                            new TransactionInput()
                            {
                                Type = new HexBigInteger(2),
                                From = web3.TransactionManager.Account.Address,
                                MaxFeePerGas = new HexBigInteger(fee.MaxFeePerGas.Value),
                                MaxPriorityFeePerGas = new HexBigInteger(fee.MaxPriorityFeePerGas.Value),
                                To = receiveAddress,
                                Value = new HexBigInteger(10)
                            }).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}﻿using Nethereum.ABI.ByteArrayConvertors;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.EVM;
using Nethereum.EVM.BlockchainState;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Mekle.Contracts.MerkleERC20Drop;
using Nethereum.Mekle.Contracts.MerkleERC20Drop.ContractDefinition;
using Nethereum.Merkle;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util.HashProviders;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;
// ReSharper disable ConsiderUsingConfigureAwait  
// ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.MerkleDropTests
{

    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class MerkleDropERC20Tests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public MerkleDropERC20Tests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Struct("MerklePaymentDropItem")]
        public class MerklePaymentItem
        {
            [Parameter("address", "sender", 1)]
            public string Sender { get; set; }

            [Parameter("address", "receiver", 2)]
            public string Receiver { get; set; }

            [Parameter("uint256", "amount", 3)]
            public BigInteger Amount { get; set; }
        }

        [Fact]
        public async void ShouldCreateAMerkleDropDeployContractAndClaimIt()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            
            var account1 = new Account("2ddc04030d24227de118e0e525d886f0b3429bf0302962ae7bea79a297473e04", EthereumClientIntegrationFixture.ChainId);
            var account2 = new Account("2ddc04030d24227de118e0e525d886f0b3429bf0302962ae7bea79a297473e0c", EthereumClientIntegrationFixture.ChainId);
            var account3 = new Account("2ddc04030d24227de118e0e525d886f0b3429bf0302962ae7bea79a297473e27", EthereumClientIntegrationFixture.ChainId);

            var merkleDropAccount1 = new MerkleDropItem() { Address = account1.Address, Amount = 100 };
            var merkleDropAccount2 = new MerkleDropItem() { Address = account2.Address, Amount = 60 };

            var merkleDropAccounts = new List<MerkleDropItem>
            {
                merkleDropAccount1, merkleDropAccount2
            };

            var merkleDropTree = new MerkleDropMerkleTree();
            merkleDropTree.BuildTree(merkleDropAccounts);
            var rootMerkleDrop = merkleDropTree.Root;
          
            var account1MerkleDropProof = merkleDropTree.GetProof(merkleDropAccount1);
            var account2MerkleDropProof = merkleDropTree.GetProof(merkleDropAccount2);

            //Another trie using the generic Abi Struct
            //just for demo the contract includes the proof of the merkle of the sender(s)
            //sender any address..
            var merklePaymentDropItem1 = new MerklePaymentItem() { Sender= EthereumClientIntegrationFixture.AccountAddress,  Receiver = account1.Address, Amount = 100 };
           

            var merklePaymentList = new List<MerklePaymentItem>
            {
                merklePaymentDropItem1
            };

            var merklePaymentTree = new AbiStructMerkleTree<MerklePaymentItem>();
            merklePaymentTree.BuildTree(merklePaymentList);
            var rootMerklePayment = merklePaymentTree.Root;

            var merklePayment1Proof = merklePaymentTree.GetProof(merklePaymentDropItem1);
        

            var deployment = new MerkleERC20DropDeployment();
            deployment.Decimals = 2;
            deployment.RootMerkleDrop = rootMerkleDrop.Hash;
            deployment.RootMerklePayment = rootMerklePayment.Hash;
            deployment.InitialSupply = 200;
            deployment.Name = "Merkle";
            deployment.Symbol = "MKD";
            
            //less calls
            web3.TransactionManager.UseLegacyAsDefault = true;


            var transactionReceiptDeployment = await web3.Eth.GetContractDeploymentHandler<MerkleERC20DropDeployment>().SendRequestAndWaitForReceiptAsync(deployment);
            var contractAddress = transactionReceiptDeployment.ContractAddress;

            var service = new MerkleERC20DropService(web3, contractAddress);

            //Validate we are encoding packing
            var encoder = new AbiStructEncoderPackedByteConvertor<MerkleDropItem>();
            var encodedPacked = encoder.ConvertToByteArray(merkleDropAccount1);
            var encodedPackedContract = await service.ComputeEncodedPackedDropQueryAsync(merkleDropAccount1.Address, merkleDropAccount1.Amount);
            Assert.True(encodedPacked.ToHex().IsTheSameHex(encodedPackedContract.ToHex()));

            //Validate we are using the right hash
            var leafAccount1 = new Sha3KeccackHashProvider().ComputeHash(encodedPacked);
            var leafAccount1Contract = await service.ComputeLeafDropQueryAsync(merkleDropAccount1.Address, merkleDropAccount1.Amount);

            Assert.True(leafAccount1.ToHex().IsTheSameHex(leafAccount1Contract.ToHex()));

            //Validate hash pairing is the same
            var rootComputed = await service.HashPairQueryAsync(account1MerkleDropProof.First(), leafAccount1);
            Assert.True(rootMerkleDrop.Hash.ToHex().IsTheSameHex(rootComputed.ToHex()));

            //Validate we are assigning the root
            var rootInContract = await service.RootMerkleDropQueryAsync();
            Assert.True(rootMerkleDrop.Hash.ToHex().IsTheSameHex(rootInContract.ToHex()));

            //check if is valid account 1
            var validAccount1 = await service.VerifyClaimQueryAsync(merkleDropAccount1.Address, merkleDropAccount1.Amount, account1MerkleDropProof);
            Assert.True(validAccount1);

            //check invalid  account 1
            var invalidAccount1 = await service.VerifyClaimQueryAsync(merkleDropAccount1.Address, merkleDropAccount1.Amount + 10, account1MerkleDropProof);
            Assert.False(invalidAccount1);

            var claimReceipt = await service.ClaimRequestAndWaitForReceiptAsync(merkleDropAccount1.Address, merkleDropAccount1.Amount, account1MerkleDropProof);
            var balanceAccount1 = await service.BalanceOfQueryAsync(merkleDropAccount1.Address);
            Assert.Equal(merkleDropAccount1.Amount, balanceAccount1);


            //Finally check that the payment is included using the proof and amounts
            var validPaymentIncluded = await service.VerifyPaymentIncludedQueryAsync(merklePaymentDropItem1.Sender, merklePaymentDropItem1.Receiver, merklePaymentDropItem1.Amount, merklePayment1Proof);
            Assert.True(validPaymentIncluded);
        }


      
    }
}﻿using System;
using System.Collections.Generic;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting
using System.Numerics;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using Nethereum.XUnitEthereumClients;
// ReSharper disable ConsiderUsingConfigureAwait
namespace Nethereum.Contracts.IntegrationTests.CQS
{
    [FunctionOutput]
    public class BalanceOfOutputDTO : IFunctionOutputDTO
    {
        [Parameter("uint256", "balance", 1)] public BigInteger Balance { get; set; }
    }

    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class MultiCallTest
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public MultiCallTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldCheckBalanceOfMultipleAccounts()
        {
            //Connecting to Ethereum mainnet using Infura
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);

            //Setting the owner https://etherscan.io/tokenholdings?a=0x8ee7d9235e01e6b42345120b5d270bdb763624c7
            var balanceOfMessage1 = new BalanceOfFunction()
                {Owner = "0x5d3a536e4d6dbd6114cc1ead35777bab948e3643"}; //compound
            var call1 = new MulticallInputOutput<BalanceOfFunction, BalanceOfOutputDTO>(balanceOfMessage1,
                "0x6b175474e89094c44da98b954eedeac495271d0f"); //dai

            var balanceOfMessage2 = new BalanceOfFunction()
                {Owner = "0x6c6bc977e13df9b0de53b251522280bb72383700"}; //uni
            var call2 = new MulticallInputOutput<BalanceOfFunction, BalanceOfOutputDTO>(balanceOfMessage2,
                "0x6b175474e89094c44da98b954eedeac495271d0f"); //dai

            await web3.Eth.GetMultiQueryHandler().MultiCallAsync(call1, call2);
            Assert.True(call1.Output.Balance > 0);
            Assert.True(call2.Output.Balance > 0);
        }


        [Fact]
        public async void ShouldCheckBalanceOfMultipleAccountsUsingRpcBatch()
        {
            //Connecting to Ethereum mainnet using Infura
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);

            //Setting the owner https://etherscan.io/tokenholdings?a=0x8ee7d9235e01e6b42345120b5d270bdb763624c7
            var balanceOfMessage1 = new BalanceOfFunction()
            { Owner = "0x5d3a536e4d6dbd6114cc1ead35777bab948e3643" }; //compound
            var call1 = new MulticallInputOutput<BalanceOfFunction, BalanceOfOutputDTO>(balanceOfMessage1,
                "0x6b175474e89094c44da98b954eedeac495271d0f"); //dai

            var balanceOfMessage2 = new BalanceOfFunction()
            { Owner = "0x6c6bc977e13df9b0de53b251522280bb72383700" }; //uni
            var call2 = new MulticallInputOutput<BalanceOfFunction, BalanceOfOutputDTO>(balanceOfMessage2,
                "0x6b175474e89094c44da98b954eedeac495271d0f"); //dai

            await web3.Eth.GetMultiQueryBatchRpcHandler().MultiCallAsync(call1, call2);
            Assert.True(call1.Output.Balance > 0);
            Assert.True(call2.Output.Balance > 0);
        }


        /*
pragma solidity "0.4.25";
pragma experimental ABIEncoderV2;

contract TestV2
{

uint256 public id1 = 1;
uint256 public id2;
uint256 public id3;
string  public id4;
TestStruct public testStructStorage;


event TestStructStorageChanged(address sender, TestStruct testStruct);

struct SubSubStruct {
    uint256 id;
}

struct SubStruct {
    uint256 id;
    SubSubStruct sub;
    string id2;
}

struct TestStruct {
    uint256 id;
    SubStruct subStruct1;
    SubStruct subStruct2;
    string id2;
}

struct SimpleStruct{
    uint256 id;
    uint256 id2;
}

function TestArray() pure public returns (SimpleStruct[2] structArray) {
    structArray[0] = (SimpleStruct(1, 100));
    structArray[1] = (SimpleStruct(2, 200));
    return structArray;
}

function Test(TestStruct testScrut) public {
    id1 = testScrut.id;
    id2 = testScrut.subStruct1.id;
    id3 = testScrut.subStruct2.sub.id;
    id4 = testScrut.subStruct2.id2;

}

function SetStorageStruct(TestStruct testStruct) public {
    testStructStorage = testStruct;
    emit TestStructStorageChanged(msg.sender, testStruct);
}

function GetTest() public view returns(TestStruct testStruct, int test1, int test2){
    testStruct.id = 1;
    testStruct.id2 = "hello";
    testStruct.subStruct1.id = 200;
    testStruct.subStruct1.id2 = "Giraffe";
    testStruct.subStruct1.sub.id = 20;
    testStruct.subStruct2.id = 300;
    testStruct.subStruct2.id2 = "Elephant";
    testStruct.subStruct2.sub.id = 30000;
    test1 = 5;
    test2 = 6;
}

struct Empty{

}

function TestEmpty(Empty empty) public {

}
}

}
*/

        public class MulticallDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "608060405234801561001057600080fd5b506105ee806100206000396000f3fe608060405234801561001057600080fd5b50600436106100885760003560e01c806372425d9d1161005b57806372425d9d146100e657806386d516e8146100ec578063a8b0574e146100f2578063ee82ac5e1461010057600080fd5b80630f28c97d1461008d578063252dba42146100a257806327e86d6e146100c35780634d2301cc146100cb575b600080fd5b425b6040519081526020015b60405180910390f35b6100b56100b03660046102a3565b610112565b604051610099929190610438565b61008f610252565b61008f6100d9366004610281565b6001600160a01b03163190565b4461008f565b4561008f565b604051418152602001610099565b61008f61010e366004610403565b4090565b8051439060609067ffffffffffffffff811115610131576101316105a2565b60405190808252806020026020018201604052801561016457816020015b606081526020019060019003908161014f5790505b50905060005b835181101561024c576000808583815181106101885761018861058c565b6020026020010151600001516001600160a01b03168684815181106101af576101af61058c565b6020026020010151602001516040516101c8919061041c565b6000604051808303816000865af19150503d8060008114610205576040519150601f19603f3d011682016040523d82523d6000602084013e61020a565b606091505b50915091508161021957600080fd5b8084848151811061022c5761022c61058c565b6020026020010181905250505080806102449061055b565b91505061016a565b50915091565b600061025f600143610514565b40905090565b80356001600160a01b038116811461027c57600080fd5b919050565b60006020828403121561029357600080fd5b61029c82610265565b9392505050565b600060208083850312156102b657600080fd5b823567ffffffffffffffff808211156102ce57600080fd5b818501915085601f8301126102e257600080fd5b8135818111156102f4576102f46105a2565b8060051b6103038582016104e3565b8281528581019085870183870188018b101561031e57600080fd5b600093505b848410156103f55780358681111561033a57600080fd5b8701601f196040828e038201121561035157600080fd5b6103596104ba565b6103648b8401610265565b815260408301358981111561037857600080fd5b8084019350508d603f84011261038d57600080fd5b8a830135898111156103a1576103a16105a2565b6103b18c84601f840116016104e3565b92508083528e60408286010111156103c857600080fd5b80604085018d85013760009083018c0152808b019190915284525060019390930192918701918701610323565b509998505050505050505050565b60006020828403121561041557600080fd5b5035919050565b6000825161042e81846020870161052b565b9190910192915050565b600060408201848352602060408185015281855180845260608601915060608160051b870101935082870160005b828110156104ac57878603605f190184528151805180885261048d81888a0189850161052b565b601f01601f191696909601850195509284019290840190600101610466565b509398975050505050505050565b6040805190810167ffffffffffffffff811182821017156104dd576104dd6105a2565b60405290565b604051601f8201601f1916810167ffffffffffffffff8111828210171561050c5761050c6105a2565b604052919050565b60008282101561052657610526610576565b500390565b60005b8381101561054657818101518382015260200161052e565b83811115610555576000848401525b50505050565b600060001982141561056f5761056f610576565b5060010190565b634e487b7160e01b600052601160045260246000fd5b634e487b7160e01b600052603260045260246000fd5b634e487b7160e01b600052604160045260246000fdfea2646970667358221220d29b08a734a0942c71b50a9d55db7448beb4f7a73fceb9f54f09c19f5dda3dcb64736f6c63430008060033";

            public MulticallDeployment() : base(BYTECODE)
            {
            }

            public MulticallDeployment(string byteCode) : base(byteCode)
            {
            }
        }


        [Function("id1", "uint256")]
        public class Id1Function : FunctionMessage
        {
        }

        [Function("id2", "uint256")]
        public class Id2Function : FunctionMessage
        {
        }

        [Function("id3", "uint256")]
        public class Id3Function : FunctionMessage
        {
        }

        [Function("id4", "string")]
        public class Id4Function : FunctionMessage
        {
        }

        [Function("GetTest")]
        public class GetTestFunction : FunctionMessage
        {
        }

        [Function("testStructStorage")]
        public class GetTestStructStorageFunction : FunctionMessage
        {
        }

        public class SimpleStruct
        {
            [Parameter("uint256", "id1", 1)] public BigInteger Id1 { get; set; }

            [Parameter("uint256", "id2", 2)] public BigInteger Id2 { get; set; }
        }

        [Function("TestArray", typeof(TestArrayOuputDTO))]
        public class TestArray : FunctionMessage
        {
        }

        [FunctionOutput]
        public class TestArrayOuputDTO : IFunctionOutputDTO
        {
            [Parameter("tuple[2]", "simpleStruct", 1)]
            public List<SimpleStruct> SimpleStructs { get; set; }
        }

        [FunctionOutput]
        public class GetTestFunctionOuptputDTO : IFunctionOutputDTO
        {
            [Parameter("tuple")] public TestStructStrings TestStruct { get; set; }


            [Parameter("int256", "test1", 2)] public BigInteger Test1 { get; set; }


            [Parameter("int256", "test2", 3)] public BigInteger Test2 { get; set; }
        }

        [Function("Test")]
        public class TestFunction : FunctionMessage
        {
            [Parameter("tuple", "testStruct")] public TestStructStrings TestStruct { get; set; }
        }

        [Function("SetStorageStruct")]
        public class SetStorageStructFunction : FunctionMessage
        {
            [Parameter("tuple", "testStruct")] public TestStructStrings TestStruct { get; set; }
        }

        [Event("TestStructStorageChanged")]
        public class TestStructStorageChangedEvent : IEventDTO
        {
            [Parameter("address", "sender", 1)] public string Address { get; set; }

            [Parameter("tuple", "testStruct", 2)] public TestStructStrings TestStruct { get; set; }
        }


        [FunctionOutput]
        public class TestStructStrings : IFunctionOutputDTO
        {
            [Parameter("uint256", "id", 1)] public BigInteger Id { get; set; }

            [Parameter("tuple", "subStruct1", 2)] public SubStructUintString SubStruct1 { get; set; }

            [Parameter("tuple", "subStruct2", 3)] public SubStructUintString SubStruct2 { get; set; }

            [Parameter("string", "id2", 4)] public string Id2 { get; set; }
        }


        public class SubStructUintString
        {
            [Parameter("uint256", "id", 1)] public BigInteger Id { get; set; }

            [Parameter("tuple", "sub", 2)] public SubStructUInt Sub { get; set; }

            [Parameter("string", "id2", 3)] public String Id2 { get; set; }
        }

        public class SubStructUInt
        {
            [Parameter("uint256", "id", 1)] public BigInteger Id { get; set; }
        }

        public class TestContractDeployment : ContractDeploymentMessage
        {
            public const string BYTE_CODE =
                "0x6080604052600160005534801561001557600080fd5b50610d7a806100256000396000f3006080604052600436106100a35763ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166319dcec3d81146100a857806326145698146100d55780632abda41e146100f75780634d8f11fa14610117578063517999bc1461013957806363845ba31461015b578063a55001a614610180578063e5207eaa146101a0578063e9159d64146101b5578063f6ee7d9f146101d7575b600080fd5b3480156100b457600080fd5b506100bd6101ec565b6040516100cc93929190610bf9565b60405180910390f35b3480156100e157600080fd5b506100f56100f0366004610a06565b6102d1565b005b34801561010357600080fd5b506100f56101123660046109e1565b61039c565b34801561012357600080fd5b5061012c61039f565b6040516100cc9190610bd4565b34801561014557600080fd5b5061014e6103db565b6040516100cc9190610c26565b34801561016757600080fd5b506101706103e1565b6040516100cc9493929190610c34565b34801561018c57600080fd5b506100f561019b366004610a06565b6105f5565b3480156101ac57600080fd5b5061014e610628565b3480156101c157600080fd5b506101ca61062e565b6040516100cc9190610be8565b3480156101e357600080fd5b5061014e6106bc565b6101f46106c2565b6001815260408051808201825260058082527f68656c6c6f0000000000000000000000000000000000000000000000000000006020808401919091526060850192909252818401805160c8905283518085018552600781527f47697261666665000000000000000000000000000000000000000000000000008185015281518501525182015160149052828401805161012c905283518085018552600881527f456c657068616e74000000000000000000000000000000000000000000000000818501528151909401939093529151015161753090529091600690565b8051600490815560208083015180516005908155818301515160065560408201518051869594610306926007929101906106f8565b5050506040828101518051600484019081556020808301515160058601559282015180519293919261033e92600687019201906106f8565b5050506060820151805161035c9160078401916020909101906106f8565b509050507fc4948cf046f20c08b2b7f5b0b6de7bdbe767d009d512c8440b98eb424bdb9ad83382604051610391929190610bb4565b60405180910390a150565b50565b6103a7610776565b60408051808201825260018152606460208083019190915290835281518083019092526002825260c8828201528201525b90565b60005481565b6004805460408051606081018252600580548252825160208181018552600654825280840191909152600780548551601f6002600019610100600186161502019093169290920491820184900484028101840187528181529697969495939493860193928301828280156104965780601f1061046b57610100808354040283529160200191610496565b820191906000526020600020905b81548152906001019060200180831161047957829003601f168201915b5050509190925250506040805160608101825260048501805482528251602080820185526005880154825280840191909152600687018054855160026001831615610100026000190190921691909104601f81018490048402820184018752808252979897949650929486019390918301828280156105565780601f1061052b57610100808354040283529160200191610556565b820191906000526020600020905b81548152906001019060200180831161053957829003601f168201915b5050509190925250505060078201805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815293949392918301828280156105eb5780601f106105c0576101008083540402835291602001916105eb565b820191906000526020600020905b8154815290600101906020018083116105ce57829003601f168201915b5050505050905084565b8051600055602080820151516001556040808301518083015151600255015180516106249260039201906106f8565b5050565b60025481565b6003805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815292918301828280156106b45780601f10610689576101008083540402835291602001916106b4565b820191906000526020600020905b81548152906001019060200180831161069757829003601f168201915b505050505081565b60015481565b61010060405190810160405280600081526020016106de6107a4565b81526020016106eb6107a4565b8152602001606081525090565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061073957805160ff1916838001178555610766565b82800160010185558215610766579182015b8281111561076657825182559160200191906001019061074b565b506107729291506107bf565b5090565b6080604051908101604052806002905b61078e6107d9565b8152602001906001900390816107865790505090565b606060405190810160405280600081526020016106eb6107f0565b6103d891905b8082111561077257600081556001016107c5565b604080518082019091526000808252602082015290565b60408051602081019091526000815290565b6000601f8201831361081357600080fd5b813561082661082182610cad565b610c86565b9150808252602083016020830185838301111561084257600080fd5b61084d838284610cfe565b50505092915050565b600080828403121561086757600080fd5b6108716000610c86565b9392505050565b60006060828403121561088a57600080fd5b6108946060610c86565b905060006108a284846109d5565b82525060206108b3848483016108eb565b602083015250604082013567ffffffffffffffff8111156108d357600080fd5b6108df84828501610802565b60408301525092915050565b6000602082840312156108fd57600080fd5b6109076020610c86565b9050600061091584846109d5565b82525092915050565b60006080828403121561093057600080fd5b61093a6080610c86565b9050600061094884846109d5565b825250602082013567ffffffffffffffff81111561096557600080fd5b61097184828501610878565b602083015250604082013567ffffffffffffffff81111561099157600080fd5b61099d84828501610878565b604083015250606082013567ffffffffffffffff8111156109bd57600080fd5b6109c984828501610802565b60608301525092915050565b600061087182356103d8565b60008082840312156109f257600080fd5b60006109fe8484610856565b949350505050565b600060208284031215610a1857600080fd5b813567ffffffffffffffff811115610a2f57600080fd5b6109fe8482850161091e565b610a4481610ce5565b82525050565b610a5381610cd5565b610a5c826103d8565b60005b82811015610a8c57610a72858351610ad1565b610a7b82610cdf565b604095909501949150600101610a5f565b5050505050565b610a44816103d8565b6000610aa782610cdb565b808452610abb816020860160208601610d0a565b610ac481610d36565b9093016020019392505050565b80516040830190610ae28482610a93565b506020820151610af56020850182610a93565b50505050565b80516000906060840190610b0f8582610a93565b506020830151610b226020860182610b43565b5060408301518482036040860152610b3a8282610a9c565b95945050505050565b80516020830190610af58482610a93565b80516000906080840190610b688582610a93565b5060208301518482036020860152610b808282610afb565b91505060408301518482036040860152610b9a8282610afb565b91505060608301518482036060860152610b3a8282610a9c565b60408101610bc28285610a3b565b81810360208301526109fe8184610b54565b60808101610be28284610a4a565b92915050565b602080825281016108718184610a9c565b60608082528101610c0a8186610b54565b9050610c196020830185610a93565b6109fe6040830184610a93565b60208101610be28284610a93565b60808101610c428287610a93565b8181036020830152610c548186610afb565b90508181036040830152610c688185610afb565b90508181036060830152610c7c8184610a9c565b9695505050505050565b60405181810167ffffffffffffffff81118282101715610ca557600080fd5b604052919050565b600067ffffffffffffffff821115610cc457600080fd5b506020601f91909101601f19160190565b50600290565b5190565b60200190565b73ffffffffffffffffffffffffffffffffffffffff1690565b82818337506000910152565b60005b83811015610d25578181015183820152602001610d0d565b83811115610af55750506000910152565b601f01601f1916905600a265627a7a723058201a204c8fd11b9facac01a86aaac24ebbc6159e540ae80a3dfe6fa745070a73516c6578706572696d656e74616cf50037";

            public TestContractDeployment() : base(BYTE_CODE)
            {
            }
        }

        [Fact]
        public async void MulticallTestStruct()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestContractDeployment>()
                .SendRequestAndWaitForReceiptAsync();

            var deploymentReceiptMulticall = await web3.Eth.GetContractDeploymentHandler<MulticallDeployment>()
                .SendRequestAndWaitForReceiptAsync();

            var getTestFunction1 = new GetTestFunction();
            var call1 = new MulticallInputOutput<GetTestFunction, GetTestFunctionOuptputDTO>(getTestFunction1,
                deploymentReceipt.ContractAddress);

            var getTestFunction2 = new GetTestFunction();
            var call2 = new MulticallInputOutput<GetTestFunction, GetTestFunctionOuptputDTO>(getTestFunction2,
                deploymentReceipt.ContractAddress);

            await web3.Eth.GetMultiQueryHandler(deploymentReceiptMulticall.ContractAddress)
                .MultiCallV1Async(call1, call2);

            Assert.Equal(5, call1.Output.Test1);
            Assert.Equal(5, call2.Output.Test1);
        }
    }
}﻿using System.Collections.Generic;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting
// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.CQS
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class MultipleByteArray
    {
        /*
         pragma solidity ^0.4.24;
pragma experimental ABIEncoderV2;

contract Test {
  
    function Foo() public returns (bytes[][]) {
        bytes[][] memory blist = new bytes[][](1);
        blist[0] = new bytes[](1);
        blist[0][0] = new bytes(4);
        return blist;
    }
}
         */
        public class TestDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "608060405234801561001057600080fd5b506102c4806100206000396000f3006080604052600436106100405763ffffffff7c0100000000000000000000000000000000000000000000000000000000600035041663bfb4ebcf8114610045575b600080fd5b34801561005157600080fd5b5061005a610070565b604051610067919061022e565b60405180910390f35b604080516001808252818301909252606091829190816020015b606081526020019060019003908161008a575050604080516001808252818301909252919250602082015b60608152602001906001900390816100b55750508151829060009081106100d857fe5b602090810290910101526040805160048082528183019092529081602001602082028038833950508251839150600090811061011057fe5b90602001906020020151600081518110151561012857fe5b60209081029091010152905090565b60006101428261024c565b8084526020840193508360208202850161015b85610246565b60005b8481101561019257838303885261017683835161019e565b925061018182610246565b60209890980197915060010161015e565b50909695505050505050565b60006101a98261024c565b808452602084019350836020820285016101c285610246565b60005b848110156101925783830388526101dd8383516101f9565b92506101e882610246565b6020989098019791506001016101c5565b60006102048261024c565b808452610218816020860160208601610250565b61022181610280565b9093016020019392505050565b6020808252810161023f8184610137565b9392505050565b60200190565b5190565b60005b8381101561026b578181015183820152602001610253565b8381111561027a576000848401525b50505050565b601f01601f1916905600a265627a7a723058204990d69439208797efea4464539382a3abd33dd4bcfea3e376d78fc4b96408576c6578706572696d656e74616cf50037";

            public TestDeployment() : base(BYTECODE)
            {
            }

            public TestDeployment(string byteCode) : base(byteCode)
            {
            }
        }

        [Function("Foo", "bytes[][]")]
        public class FooFunction : FunctionMessage
        {
        }

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public MultipleByteArray(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldReturnMultiDimensionalArray()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TestDeployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync();
            var contractHandler = web3.Eth.GetContractHandler(transactionReceipt.ContractAddress);
            var result = await contractHandler.QueryAsync<FooFunction, List<List<byte[]>>>();
            Assert.True(result[0][0].Length == 4);
        }
    }
}﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.NonceServices;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class NonceTests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public NonceTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldBeAbleToHandleNoncesOfMultipleTxnMultipleWeb3sMultithreaded()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var privateKey = EthereumClientIntegrationFixture.AccountPrivateKey;
            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""val"",""type"":""int256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""int256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""int256""}],""type"":""constructor""}]";
            var byteCode =
                "0x60606040526040516020806052833950608060405251600081905550602b8060276000396000f3606060405260e060020a60003504631df4f1448114601a575b005b600054600435026060908152602090f3";
            JsonRpc.Client.RpcClient.ConnectionTimeout = TimeSpan.FromSeconds(30.0);
            var multiplier = 7;

            var client = _ethereumClientIntegrationFixture.GetClient();
            var nonceProvider = new InMemoryNonceService(senderAddress, client);
            //tested with 1000
            var listTasks = 10;
            var taskItems = new List<int>();
            for (var i = 0; i < listTasks; i++)
                taskItems.Add(i);

            var numProcs = Environment.ProcessorCount;
            var concurrencyLevel = numProcs * 2;
            var concurrentDictionary = new ConcurrentDictionary<int, string>(concurrencyLevel, listTasks * 2);


            Parallel.ForEach(taskItems, (item, state) =>
            {
                var account = new Account(privateKey, EthereumClientIntegrationFixture.ChainId);
                account.NonceService = nonceProvider;
                var web3 = new Web3.Web3(account, client);
                // Wait for task completion synchronously in order to Parallel.ForEach work correctly
                var txn = web3.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress, new HexBigInteger(900000),
                        null, multiplier).Result;
                concurrentDictionary.TryAdd(item, txn);
            });

            var web31 = new Web3.Web3(new Account(privateKey), client);
            var pollService = new TransactionReceiptPollingService(web31.TransactionManager);

            for (var i = 0; i < listTasks; i++)
            {
                string txn = null;
                concurrentDictionary.TryGetValue(i, out txn);
                var receipt = await pollService.PollForReceiptAsync(txn).ConfigureAwait(false);
                Assert.NotNull(receipt);
            }
        }


        [Fact]
        public async void ShouldBeAbleToHandleNoncesOfMultipleTxnMultipleWeb3sSingleThreaded()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var privateKey = EthereumClientIntegrationFixture.AccountPrivateKey;

            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""val"",""type"":""int256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""int256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""int256""}],""type"":""constructor""}]";
            var byteCode =
                "0x60606040526040516020806052833950608060405251600081905550602b8060276000396000f3606060405260e060020a60003504631df4f1448114601a575b005b600054600435026060908152602090f3";

            var multiplier = 7;

            var client = _ethereumClientIntegrationFixture.GetClient();
            var nonceProvider = new InMemoryNonceService(senderAddress, client);
            var account = new Account(privateKey, EthereumClientIntegrationFixture.ChainId) {NonceService = nonceProvider};
            var web31 = new Web3.Web3(account, client);

            var txn1 = await
                web31.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress, new HexBigInteger(900000), null,
                    multiplier).ConfigureAwait(false);

            var web32 = new Web3.Web3(account, client);


            var txn2 = await
                web32.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress, new HexBigInteger(900000), null,
                    multiplier).ConfigureAwait(false);

            var web33 = new Web3.Web3(account, client);

            var txn3 = await
                web33.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress, new HexBigInteger(900000), null,
                    multiplier).ConfigureAwait(false);

            var pollService = new TransactionReceiptPollingService(web31.TransactionManager);

            var receipt1 = await pollService.PollForReceiptAsync(txn1).ConfigureAwait(false);
            var receipt2 = await pollService.PollForReceiptAsync(txn2).ConfigureAwait(false);
            var receipt3 = await pollService.PollForReceiptAsync(txn3).ConfigureAwait(false);

            Assert.NotNull(receipt1);
            Assert.NotNull(receipt2);
            Assert.NotNull(receipt3);
        }


        [Fact]
        public async void ShouldBeAbleToHandleNoncesOfMultipleTxnSingleWeb3SingleThreaded()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var privateKey = EthereumClientIntegrationFixture.AccountPrivateKey;
            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""val"",""type"":""int256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""int256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""int256""}],""type"":""constructor""}]";
            var byteCode =
                "0x60606040526040516020806052833950608060405251600081905550602b8060276000396000f3606060405260e060020a60003504631df4f1448114601a575b005b600054600435026060908152602090f3";

            var multiplier = 7;

            var web3 = new Web3.Web3(new Account(privateKey, EthereumClientIntegrationFixture.ChainId), _ethereumClientIntegrationFixture.GetClient());

            var txn1 = await
                web3.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress, new HexBigInteger(900000), null,
                    multiplier).ConfigureAwait(false);

            var txn2 = await
                web3.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress, new HexBigInteger(900000), null,
                    multiplier).ConfigureAwait(false);

            var txn3 = await
                web3.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress, new HexBigInteger(900000), null,
                    multiplier).ConfigureAwait(false);

            var pollService = new TransactionReceiptPollingService(web3.TransactionManager);

            var receipt1 = await pollService.PollForReceiptAsync(txn1).ConfigureAwait(false);
            var receipt2 = await pollService.PollForReceiptAsync(txn2).ConfigureAwait(false);
            var receipt3 = await pollService.PollForReceiptAsync(txn3).ConfigureAwait(false);

            Assert.NotNull(receipt1);
            Assert.NotNull(receipt2);
            Assert.NotNull(receipt3);
        }
    }
}using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ReceiptStatusTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ReceiptStatusTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        //note ensure geth  "byzantiumBlock": 0 is set in the genesis.json file to enable status
        [Fact]
        public async Task ShouldReportNoErrorsWhenValid()
        {
            var abi =
                @"[{'constant':false,'inputs':[{'name':'val','type':'int256'}],'name':'multiply','outputs':[{'name':'','type':'int256'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[{'name':'multiplier','type':'int256'}],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'from','type':'address'},{'indexed':false,'name':'val','type':'int256'},{'indexed':false,'name':'result','type':'int256'}],'name':'Multiplied','type':'event'}]";

            var smartContractByteCode =
                "6060604052341561000f57600080fd5b604051602080610149833981016040528080516000555050610113806100366000396000f300606060405260043610603e5763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416631df4f14481146043575b600080fd5b3415604d57600080fd5b60566004356068565b60405190815260200160405180910390f35b6000805482027fd01bc414178a5d1578a8b9611adebfeda577e53e89287df879d5ab2c29dfa56a338483604051808473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001838152602001828152602001935050505060405180910390a1929150505600a165627a7a723058201bd2fbd3fb58686ed61df3e636dc4cc7c95b864aa1654bc02b0136e6eca9e9ef0029";

            var accountAddresss = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var multiplier = 2;

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                    abi,
                    smartContractByteCode,
                    accountAddresss,
                    new HexBigInteger(900000),
                    null,
                    multiplier).ConfigureAwait(false);

            var contractAddress = receipt.ContractAddress;

            var contract = web3.Eth.GetContract(abi, contractAddress);
            var multiplyFunction = contract.GetFunction("multiply");

            //correct gas estimation with a parameter
            var estimatedGas = await multiplyFunction.EstimateGasAsync(7).ConfigureAwait(false);

            var receipt1 = await multiplyFunction.SendTransactionAndWaitForReceiptAsync(accountAddresss,
                new HexBigInteger(estimatedGas.Value), null, null, 5).ConfigureAwait(false);

            Assert.Equal(1, receipt1.Status.Value);

            Assert.False(receipt1.HasErrors());
        }


        [Fact]
        public async Task ShouldReportErrorsWhenInValid()
        {
            var abi =
                @"[{'constant':false,'inputs':[{'name':'val','type':'int256'}],'name':'multiply','outputs':[{'name':'','type':'int256'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[{'name':'multiplier','type':'int256'}],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'from','type':'address'},{'indexed':false,'name':'val','type':'int256'},{'indexed':false,'name':'result','type':'int256'}],'name':'Multiplied','type':'event'}]";

            var smartContractByteCode =
                "6060604052341561000f57600080fd5b604051602080610149833981016040528080516000555050610113806100366000396000f300606060405260043610603e5763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416631df4f14481146043575b600080fd5b3415604d57600080fd5b60566004356068565b60405190815260200160405180910390f35b6000805482027fd01bc414178a5d1578a8b9611adebfeda577e53e89287df879d5ab2c29dfa56a338483604051808473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001838152602001828152602001935050505060405180910390a1929150505600a165627a7a723058201bd2fbd3fb58686ed61df3e636dc4cc7c95b864aa1654bc02b0136e6eca9e9ef0029";

            var accountAddresss = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();


            var multiplier = 2;

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                    abi,
                    smartContractByteCode,
                    accountAddresss,
                    new HexBigInteger(900000),
                    null,
                    multiplier).ConfigureAwait(false);

            var contractAddress = receipt.ContractAddress;

            var contract = web3.Eth.GetContract(abi, contractAddress);
            var multiplyFunction = contract.GetFunction("multiply");

            //incorrect gas estimation without a parameter
            //it will ran out of gas
            var estimatedGas = await multiplyFunction.EstimateGasAsync().ConfigureAwait(false);

            var receipt1 = await multiplyFunction.SendTransactionAndWaitForReceiptAsync(accountAddresss,
                new HexBigInteger(estimatedGas.Value), null, null, 5).ConfigureAwait(false);

            Assert.Equal(0, receipt1.Status.Value);

            Assert.True(receipt1.HasErrors());
        }
    }
}﻿using System;
using System.Collections.Generic;
using System.Text;
using Nethereum.ABI.EIP712;
using Nethereum.Contracts.TransactionHandlers.MultiSend;
using Nethereum.GnosisSafe.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.GnosisSafe.IntegrationTests
{
       [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
        public class SafeFunctionalTests
        {
            private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

            public SafeFunctionalTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
            {
                _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
            }
            
            
            [Fact]
        public async void ShouldBeAbleToEncodeTheSameAsTheSmartContract()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Rinkeby);
            var gnosisSafeAddress = "0xa9C09412C1d93DAc6eE9254A51E97454588D3B88";
            var chainId = (int)Chain.Rinkeby;
            var service = new GnosisSafeService(web3, gnosisSafeAddress);
            var param = new EncodeTransactionDataFunction
            {
                To = "0x40A2aCCbd92BCA938b02010E17A5b8929b49130D",
                Value = 0,
                Data = "0x40A2aCCbd92BCA938b02010E17A5b8929b49130D".HexToByteArray(),
                Operation = (byte)ContractOperationType.Call,
                SafeTxGas = 0,
                BaseGas = 0,
                GasPrice = 0,
                GasToken = AddressUtil.AddressEmptyAsHex,
                RefundReceiver = AddressUtil.AddressEmptyAsHex,
                Nonce = 1
            };
            var encoded = await service.EncodeTransactionDataQueryAsync(param).ConfigureAwait(false);

            var domain = new GnosisSafeEIP712Domain
            {
                VerifyingContract = gnosisSafeAddress,
                ChainId = chainId
            };

            var encodedMessage = Eip712TypedDataEncoder.Current.EncodeTypedData(param, domain, "SafeTx");
            Assert.Equal(encoded.ToHex(), encodedMessage.ToHex());

        }
    }
}
﻿using System;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Signer.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class SignedEIP155
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public SignedEIP155(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        [Fact]
        public async Task ShouldSendSignTransaction()
        {
            var contractByteCode =
                "0x6060604052604060405190810160405280600a81526020017f4d756c7469706c6965720000000000000000000000000000000000000000000081526020015060016000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061008c57805160ff19168380011785556100bd565b828001600101855582156100bd579182015b828111156100bc57825182600050559160200191906001019061009e565b5b5090506100e891906100ca565b808211156100e457600081815060009055506001016100ca565b5090565b5050604051602080610303833981016040528080519060200190919050505b806000600050819055505b506101e2806101216000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806340490a901461004f57806375d0c0dc14610072578063c6888fa1146100ed5761004d565b005b61005c6004805050610119565b6040518082815260200191505060405180910390f35b61007f6004805050610141565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600f02600301f150905090810190601f1680156100df5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b610103600480803590602001909190505061012b565b6040518082815260200191505060405180910390f35b60006000600050549050610128565b90565b60006000600050548202905061013c565b919050565b60016000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156101da5780601f106101af576101008083540402835291602001916101da565b820191906000526020600020905b8154815290600101906020018083116101bd57829003601f168201915b50505050508156";

            var abi =
                @"[{""constant"":true,""inputs"":[],""name"":""getMultiplier"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""contractName"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""}]";


            var privateKey = EthereumClientIntegrationFixture.AccountPrivateKey;
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;


            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            //GetWeb3 includes the chain Id

            var x = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().ConfigureAwait(false);
            web3.Eth.TransactionManager.UseLegacyAsDefault = true;

            var receipt = await
                web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, contractByteCode, senderAddress,
                    new HexBigInteger(900000), null, 7).ConfigureAwait(false);
            var contractAddress = receipt.ContractAddress;
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var multiplyFunction = contract.GetFunction("multiply");

            var transactions = new Func<Task<string>>[]
            {
                () => multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                    new HexBigInteger(0), 69),
                () => multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                    new HexBigInteger(0), 7),
                () => multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                    new HexBigInteger(0), 8),
                () => multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                    new HexBigInteger(0), 8)
            };

            var transactionsReceipts =
                await web3.TransactionManager.TransactionReceiptService
                    .SendRequestsAndWaitForReceiptAsync(transactions).ConfigureAwait(false);

            Assert.Equal(4, transactionsReceipts.Count);

            web3.Eth.TransactionManager.UseLegacyAsDefault = false;
        }
    }
}﻿using System;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer.IntegrationTests;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Signer.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class SignedTransactionManagerTests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public SignedTransactionManagerTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }
     
        [Fact]
        public async Task ShouldSendSignTransaction()
        {
            var contractByteCode =
                "0x6060604052604060405190810160405280600a81526020017f4d756c7469706c6965720000000000000000000000000000000000000000000081526020015060016000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061008c57805160ff19168380011785556100bd565b828001600101855582156100bd579182015b828111156100bc57825182600050559160200191906001019061009e565b5b5090506100e891906100ca565b808211156100e457600081815060009055506001016100ca565b5090565b5050604051602080610303833981016040528080519060200190919050505b806000600050819055505b506101e2806101216000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806340490a901461004f57806375d0c0dc14610072578063c6888fa1146100ed5761004d565b005b61005c6004805050610119565b6040518082815260200191505060405180910390f35b61007f6004805050610141565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600f02600301f150905090810190601f1680156100df5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b610103600480803590602001909190505061012b565b6040518082815260200191505060405180910390f35b60006000600050549050610128565b90565b60006000600050548202905061013c565b919050565b60016000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156101da5780601f106101af576101008083540402835291602001916101da565b820191906000526020600020905b8154815290600101906020018083116101bd57829003601f168201915b50505050508156";

            var abi =
                @"[{""constant"":true,""inputs"":[],""name"":""getMultiplier"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""contractName"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""}]";

            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt = await
                web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, contractByteCode, senderAddress,
                    new HexBigInteger(900000), null, 7).ConfigureAwait(false);
            var contractAddress = receipt.ContractAddress;
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var multiplyFunction = contract.GetFunction("multiply");

            var transactions = new Func<Task<string>>[]
            {
                () => multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                    new HexBigInteger(0), 69),
                () => multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                    new HexBigInteger(0), 7),
                () => multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                    new HexBigInteger(0), 8),
                () => multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                    new HexBigInteger(0), 8)
            };

            var transactionsReceipts =
                await web3.TransactionManager.TransactionReceiptService
                    .SendRequestsAndWaitForReceiptAsync(transactions).ConfigureAwait(false);

            Assert.Equal(4, transactionsReceipts.Count);
        }
    }
}﻿using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting
// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Contracts.IntegrationTests.CQS
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class SimpleMessage
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public SimpleMessage(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void TestCQS()
        {
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var deploymentMessage = new TestContractDeployment() {FromAddress = senderAddress};

            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<TestContractDeployment>();
            var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage);

            var contractHandler = web3.Eth.GetContractHandler(transactionReceipt.ContractAddress);

            var returnSenderMessage = new ReturnSenderFunction() {FromAddress = senderAddress};

            var returnAddress = await contractHandler.QueryAsync<ReturnSenderFunction, string>(returnSenderMessage);

            Assert.Equal(senderAddress.ToLower(), returnAddress.ToLower());
        }

        [Fact]
        public async void TestOriginal()
        {
            var byteCode =
                "0x6060604052341561000f57600080fd5b60ac8061001d6000396000f300606060405260043610603e5763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416635170a9d081146043575b600080fd5b3415604d57600080fd5b6053607c565b60405173ffffffffffffffffffffffffffffffffffffffff909116815260200160405180910390f35b33905600a165627a7a72305820ad71c73577f8423259abb92d0e9aad1a0e98ef0c93a1a1aeee4c4407c9b85c320029";
            var abi =
                @"[ { ""constant"": true, ""inputs"": [], ""name"": ""returnSender"", ""outputs"": [ { ""name"": """", ""type"": ""address"", ""value"": ""0x108b08336f8890a3f5d091b1f696c67b13b19c4d"" } ], ""payable"": false, ""stateMutability"": ""view"", ""type"": ""function"" } ]";
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, byteCode, senderAddress,
                    new HexBigInteger(900000));

            var contractAddress = receipt.ContractAddress;
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var function = contract.GetFunction("returnSender");
            var returnAddress = await function.CallAsync<string>(senderAddress, new HexBigInteger(900000), null,
                BlockParameter.CreateLatest());
            Assert.Equal(senderAddress.ToLower(), returnAddress.ToLower());
        }

        [Fact]
        public async void TestOriginalStringSignature()
        {
            var byteCode =
                "0x6060604052341561000f57600080fd5b60ac8061001d6000396000f300606060405260043610603e5763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416635170a9d081146043575b600080fd5b3415604d57600080fd5b6053607c565b60405173ffffffffffffffffffffffffffffffffffffffff909116815260200160405180910390f35b33905600a165627a7a72305820ad71c73577f8423259abb92d0e9aad1a0e98ef0c93a1a1aeee4c4407c9b85c320029";
            var abi =
                @"function returnSender() public view returns (address)";
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var receipt =
                await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, byteCode, senderAddress,
                    new HexBigInteger(900000));

            var contractAddress = receipt.ContractAddress;
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var function = contract.GetFunction("returnSender");
            var returnAddress = await function.CallAsync<string>(senderAddress, new HexBigInteger(900000), null,
                BlockParameter.CreateLatest());
            Assert.Equal(senderAddress.ToLower(), returnAddress.ToLower());
        }

        //Smart contract
        /*pragma solidity ^0.4.4;
        contract TestContract{
            function returnSender() public view returns (address) {
                return msg.sender;
            }
        }*/

        public class TestContractDeployment : ContractDeploymentMessage
        {
            public static string BYTECODE =
                "0x6060604052341561000f57600080fd5b60ac8061001d6000396000f300606060405260043610603e5763ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416635170a9d081146043575b600080fd5b3415604d57600080fd5b6053607c565b60405173ffffffffffffffffffffffffffffffffffffffff909116815260200160405180910390f35b33905600a165627a7a72305820ad71c73577f8423259abb92d0e9aad1a0e98ef0c93a1a1aeee4c4407c9b85c320029";

            public TestContractDeployment() : base(BYTECODE)
            {
            }

            public TestContractDeployment(string byteCode) : base(byteCode)
            {
            }
        }

        [Function("returnSender", "address")]
        public class ReturnSenderFunction : FunctionMessage
        {
        }
    }
}﻿using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.EncodingInputOutput
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class SmartContractSha3Hashes
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public SmartContractSha3Hashes(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void Test()
        {
            /* 
             contract Hashes{
                
                function sha3Test(string _myvalue) returns (bytes32 val){
                    return sha3(_myvalue);
                }
    
                bytes32 public myHash;
    
                function storeMyHash(bytes32 _myHash){
                    myHash = _myHash;    
                }
            }
            */

            var text = "code monkeys are great";
            var hash = "0x1c21348936d43dc62d853ff6238cff94e361f8dcee9fde6fd5fbfed9ff663150";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            var sha3Hello = Web3.Web3.Sha3(text);
            Assert.Equal(hash, "0x" + sha3Hello);

            var contractByteCode =
                "0x6060604052610154806100126000396000f360606040526000357c0100000000000000000000000000000000000000000000000000000000900480632bb49eb71461004f5780637c886096146100bd578063b6f61649146100d55761004d565b005b6100a36004808035906020019082018035906020019191908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509090919050506100fc565b604051808260001916815260200191505060405180910390f35b6100d3600480803590602001909190505061013d565b005b6100e2600480505061014b565b604051808260001916815260200191505060405180910390f35b600081604051808280519060200190808383829060006004602084601f0104600f02600301f15090500191505060405180910390209050610138565b919050565b806000600050819055505b50565b6000600050548156";

            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""_myvalue"",""type"":""string""}],""name"":""sha3Test"",""outputs"":[{""name"":""val"",""type"":""bytes32""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_myHash"",""type"":""bytes32""}],""name"":""storeMyHash"",""outputs"":[],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""myHash"",""outputs"":[{""name"":"""",""type"":""bytes32""}],""type"":""function""}]";

            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(contractByteCode,
                senderAddress, new HexBigInteger(900000), null, null, null).ConfigureAwait(false);

            //"0x350b79547251fdb18b64ec17cf3783e7d854bd30" (prev deployed contract)

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);

            var sha3Function = contract.GetFunction("sha3Test");
            var result = await sha3Function.CallAsync<byte[]>(text).ConfigureAwait(false);
            Assert.Equal(hash, "0x" + result.ToHex());

            var storeMyHash = contract.GetFunction("storeMyHash");
            var gas = await storeMyHash.EstimateGasAsync(senderAddress, null, null, hash.HexToByteArray()).ConfigureAwait(false);
            var receiptTxn =
                await storeMyHash.SendTransactionAndWaitForReceiptAsync(senderAddress, gas, null, null,
                    hash.HexToByteArray()).ConfigureAwait(false);

            var myHashFuction = contract.GetFunction("myHash");
            result = await myHashFuction.CallAsync<byte[]>().ConfigureAwait(false);
            Assert.Equal(hash, "0x" + result.ToHex());
        }
    }
}﻿using System.Collections.Generic;
using System.Numerics;
using Nethereum.Contracts;
using Nethereum.XUnitEthereumClients;
using Xunit;
using Nethereum.ABI.FunctionEncoding;
using Newtonsoft.Json.Linq;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TestDecodingDefaultValuesToPropertyOutputsAndConversion
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TestDecodingDefaultValuesToPropertyOutputsAndConversion(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldDecodeDefaultArrayAndConvertToObjectDictionary()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestInternalDynamicArrayOfDynamicStructs.StructsSample2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contract = web3.Eth.GetContract("[{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'name':'SetPurchaseOrders','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder2','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'id','type':'uint256'}],'name':'GetPurchaseOrder','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder3','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'}],'name':'AddLineItems','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'name':'SetPurchaseOrder','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple'}],'name':'PurchaseOrderChanged','type':'event'},{'anonymous':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple[]'}],'name':'PurchaseOrdersChanged','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'indexed':false,'name':'purchaseOrderId','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'indexed':false,'name':'lineItem','type':'tuple[]'}],'name':'LineItemsAdded','type':'event'}]", deploymentReceipt.ContractAddress);
            var functionPO3 = contract.GetFunction("GetPurchaseOrder3");
            var result = await functionPO3.CallDecodingToDefaultAsync().ConfigureAwait(false);

            var decoded = result.ConvertToObjectDictionary();
            var purchaseOrdersDefaultResult = ((IList<object>)decoded["purchaseOrder"]);
            var purchaseOrderDefaultFirst = (Dictionary<string, object>)purchaseOrdersDefaultResult[0];

            Assert.Equal(1, (BigInteger)purchaseOrderDefaultFirst["id"]);
            Assert.Equal(1000, (BigInteger)purchaseOrderDefaultFirst["customerId"]);

            var lineItemsDefaultPo1 = (IList<object>)purchaseOrderDefaultFirst["lineItem"];
            var lineItemsDefaultPo1First = (Dictionary<string, object>)lineItemsDefaultPo1[0];

            Assert.Equal(1, (BigInteger)lineItemsDefaultPo1First["id"]);
            Assert.Equal(100, (BigInteger)lineItemsDefaultPo1First["productId"]);
            Assert.Equal(2, (BigInteger)lineItemsDefaultPo1First["quantity"]);
            Assert.Equal("hello1", lineItemsDefaultPo1First["description"]);
        }

        [Fact]
        public async void ShouldDecodeDefaultArrayAndConvertToDynamicDictionary()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestInternalDynamicArrayOfDynamicStructs.StructsSample2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contract = web3.Eth.GetContract("[{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'name':'SetPurchaseOrders','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder2','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'id','type':'uint256'}],'name':'GetPurchaseOrder','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder3','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'}],'name':'AddLineItems','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'name':'SetPurchaseOrder','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple'}],'name':'PurchaseOrderChanged','type':'event'},{'anonymous':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple[]'}],'name':'PurchaseOrdersChanged','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'indexed':false,'name':'purchaseOrderId','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'indexed':false,'name':'lineItem','type':'tuple[]'}],'name':'LineItemsAdded','type':'event'}]", deploymentReceipt.ContractAddress);
            var functionPO3 = contract.GetFunction("GetPurchaseOrder3");
            var result = await functionPO3.CallDecodingToDefaultAsync().ConfigureAwait(false);

            var dynamicDecoded = result.ConvertToDynamicDictionary();
            Assert.Equal(1, (BigInteger)dynamicDecoded["purchaseOrder"][0]["id"]);
            Assert.Equal(1000, (BigInteger)dynamicDecoded["purchaseOrder"][0]["customerId"]);
            Assert.Equal(1, (BigInteger)dynamicDecoded["purchaseOrder"][0]["lineItem"][0]["id"]);
            Assert.Equal(100, (BigInteger)dynamicDecoded["purchaseOrder"][0]["lineItem"][0]["productId"]);
            Assert.Equal(2, (BigInteger)dynamicDecoded["purchaseOrder"][0]["lineItem"][0]["quantity"]);
            Assert.Equal("hello1", dynamicDecoded["purchaseOrder"][0]["lineItem"][0]["description"]);
        }
        
        [Fact]
        public async void ShouldDecodeDefaultArrayAndConvertToJObject()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestInternalDynamicArrayOfDynamicStructs.StructsSample2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contract = web3.Eth.GetContract("[{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'name':'SetPurchaseOrders','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder2','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'id','type':'uint256'}],'name':'GetPurchaseOrder','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder3','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'}],'name':'AddLineItems','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'name':'SetPurchaseOrder','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple'}],'name':'PurchaseOrderChanged','type':'event'},{'anonymous':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple[]'}],'name':'PurchaseOrdersChanged','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'indexed':false,'name':'purchaseOrderId','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'indexed':false,'name':'lineItem','type':'tuple[]'}],'name':'LineItemsAdded','type':'event'}]", deploymentReceipt.ContractAddress);
            var functionPO3 = contract.GetFunction("GetPurchaseOrder3");
            var result = await functionPO3.CallDecodingToDefaultAsync().ConfigureAwait(false);

          
            var expected = JToken.Parse(
                @"{
  ""purchaseOrder"": [
    {
                ""id"": '1',
      ""lineItem"": [
        {
                    ""id"": '1',
          ""productId"": '100',
          ""quantity"": '2',
          ""description"": ""hello1""
        },
        {
                    ""id"": '2',
          ""productId"": '200',
          ""quantity"": '3',
          ""description"": ""hello2""
        },
        {
                    ""id"": '3',
          ""productId"": '300',
          ""quantity"": '4',
          ""description"": ""hello3""
        }
      ],
      ""customerId"": '1000'
    }
  ]
}");
            Assert.True(JObject.DeepEquals(expected, result.ConvertToJObject()));
        }
        
        [Fact]
        public async void ShouldDecodeDefaultArrayAndConvertToJObjectUsingABIStringSignature()
        {

            /*
             struct PurchaseOrder
            {
                uint256 id;
                LineItem[] lineItem;
                uint256 customerId;
            }

            struct LineItem
            {
                uint256 id;
                uint256 productId;
                uint256 quantity;
                string description;
            }*/

        var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<TestInternalDynamicArrayOfDynamicStructs.StructsSample2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);
            var abiLineItem = "tuple(uint256 id, uint256 productId, uint256 quantity, string description)";
            var abiPurchaseOrder = $"tuple(uint256 id,{abiLineItem}[] lineItem, uint256 customerId)";
            var abi = $@"
function GetPurchaseOrder3() public view returns({abiPurchaseOrder}[] purchaseOrder)";

            var contract = web3.Eth.GetContract(abi, deploymentReceipt.ContractAddress);
            var functionPO3 = contract.GetFunction("GetPurchaseOrder3");
            var result = await functionPO3.CallDecodingToDefaultAsync().ConfigureAwait(false);


            var expected = JToken.Parse(
                @"{
  ""purchaseOrder"": [
    {
                ""id"": '1',
      ""lineItem"": [
        {
                    ""id"": '1',
          ""productId"": '100',
          ""quantity"": '2',
          ""description"": ""hello1""
        },
        {
                    ""id"": '2',
          ""productId"": '200',
          ""quantity"": '3',
          ""description"": ""hello2""
        },
        {
                    ""id"": '3',
          ""productId"": '300',
          ""quantity"": '4',
          ""description"": ""hello3""
        }
      ],
      ""customerId"": '1000'
    }
  ]
}");
            Assert.True(JObject.DeepEquals(expected, result.ConvertToJObject()));
        }

        public partial class StructsSample3Deployment : StructsSample3DeploymentBase
        {
            public StructsSample3Deployment() : base(BYTECODE) { }
            public StructsSample3Deployment(string byteCode) : base(byteCode) { }
        }

        public class StructsSample3DeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "60806040523480156200001157600080fd5b5060018080556103e86003557f63344455000000000000000000000000000000000000000000000000124344406004819055600580546001600160a01b0319167312890d2cce102216644c59dae5baed380d84830c17905560068054808401825560008290527ff652222313e28459528d920b65115c16c04f3efc82aaedc97be59f3f377c0d3f90810183905581548085018355810183905581548085018355810183905581548085018355810183905581549384019091559190910155620000d96200045a565b506040805160808101825260018082526064602080840191825260028486018181528651808801909752600687527f68656c6c6f310000000000000000000000000000000000000000000000000000878401526060860196875281549485018083556000929092528551600490950260008051602062001cb68339815191528101958655935160008051602062001cd68339815191528501555160008051602062001cf683398151915284015594518051949594869493620001b19360008051602062001d1683398151915290910192019062000482565b50505050620001bf6200045a565b5060408051608081018252600280825260c8602080840191825260038486019081528551808701909652600686527f68656c6c6f32000000000000000000000000000000000000000000000000000086830152606085019586528354600181018086556000959095528551600490910260008051602062001cb68339815191528101918255935160008051602062001cd6833981519152850155905160008051602062001cf683398151915284015594518051949593948694936200029a9360008051602062001d1683398151915290910192019062000482565b50505050620002a86200045a565b50604080516080810182526003815261012c602080830191825260048385018181528551808701909652600686527f68656c6c6f330000000000000000000000000000000000000000000000000000868401526060850195865260028054600181018083556000929092528651930260008051602062001cb68339815191528101938455945160008051602062001cd6833981519152860155905160008051602062001cf68339815191528501559451805194959486949293620003809360008051602062001d168339815191520192019062000482565b5050600780546001818101808455600093909352805460069092027fa66cc928b5edb82af9bd49922954155ab7b0942694bea4ce44661d9a8736c6888101928355600280549496509194509192620003fc927fa66cc928b5edb82af9bd49922954155ab7b0942694bea4ce44661d9a8736c68901919062000507565b50600282810154908201556003808301549082015560048083015490820180546001600160a01b0319166001600160a01b03909216919091179055600580830180546200044d9284019190620005a6565b5050505050505062000709565b6040518060800160405280600081526020016000815260200160008152602001606081525090565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f10620004c557805160ff1916838001178555620004f5565b82800160010185558215620004f5579182015b82811115620004f5578251825591602001919060010190620004d8565b5062000503929150620005e9565b5090565b828054828255906000526020600020906004028101928215620005985760005260206000209160040282015b8281111562000598578282600082015481600001556001820154816001015560028201548160020155600382018160030190805460018160011615610100020316600290046200058592919062000609565b5050509160040191906004019062000533565b506200050392915062000682565b828054828255906000526020600020908101928215620004f55760005260206000209182015b82811115620004f5578254825591600101919060010190620005cc565b6200060691905b80821115620005035760008155600101620005f0565b90565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f10620006445780548555620004f5565b82800160010185558215620004f557600052602060002091601f0160209004820182811115620004f5578254825591600101919060010190620005cc565b6200060691905b80821115620005035760008082556001820181905560028201819055620006b46003830182620006be565b5060040162000689565b50805460018160011615610100020316600290046000825580601f10620006e6575062000706565b601f016020900490600052602060002090810190620007069190620005e9565b50565b61159d80620007196000396000f3fe608060405234801561001057600080fd5b50600436106100625760003560e01c8063357a45ec14610067578063793ce7601461007c57806381519ba81461009a578063815c844d146100ad578063a08f28cc146100c0578063cc0b4b02146100d5575b600080fd5b61007a610075366004610db4565b6100e8565b005b6100846101ce565b6040516100919190611413565b60405180910390f35b61007a6100a8366004610d77565b6103c7565b6100846100bb366004610de9565b610432565b6100c8610616565b6040516100919190611402565b61007a6100e3366004610e07565b610839565b805160009081526020819052604080822083518155908301516002820155905b82602001515181101561019057816001018360200151828151811061012957fe5b6020908102919091018101518254600181810180865560009586529484902083516004909302019182558284015190820155604082015160028201556060820151805192939192610180926003850192019061094b565b5050600190920191506101089050565b507f9989e7b45071a3a51625f3275ab6ab355999fed98a1d147b1bd25459df69493e33836040516101c2929190611395565b60405180910390a15050565b6101d66109c9565b60076000815481106101e457fe5b90600052602060002090600602016040518060c00160405290816000820154815260200160018201805480602002602001604051908101604052809291908181526020016000905b8282101561031e5783829060005260206000209060040201604051806080016040529081600082015481526020016001820154815260200160028201548152602001600382018054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156103065780601f106102db57610100808354040283529160200191610306565b820191906000526020600020905b8154815290600101906020018083116102e957829003601f168201915b5050505050815250508152602001906001019061022c565b50505050815260200160028201548152602001600382015481526020016004820160009054906101000a90046001600160a01b03166001600160a01b03166001600160a01b03168152602001600582018054806020026020016040519081016040528092919081815260200182805480156103b857602002820191906000526020600020905b8154815260200190600101908083116103a4575b50505050508152505090505b90565b60005b81518110156103f7576103ef8282815181106103e257fe5b60200260200101516100e8565b6001016103ca565b507f030bd4cd6feb982193c060be2e7179a7154e82d9a3dcb385d949f97cb1fdbeba816040516104279190611402565b60405180910390a150565b61043a6109c9565b600082815260208181526040808320815160c081018352815481526001820180548451818702810187019095528085529195929486810194939192919084015b8282101561056c5783829060005260206000209060040201604051806080016040529081600082015481526020016001820154815260200160028201548152602001600382018054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156105545780601f1061052957610100808354040283529160200191610554565b820191906000526020600020905b81548152906001019060200180831161053757829003601f168201915b5050505050815250508152602001906001019061047a565b50505050815260200160028201548152602001600382015481526020016004820160009054906101000a90046001600160a01b03166001600160a01b03166001600160a01b031681526020016005820180548060200260200160405190810160405280929190818152602001828054801561060657602002820191906000526020600020905b8154815260200190600101908083116105f2575b5050505050815250509050919050565b60606007805480602002602001604051908101604052809291908181526020016000905b8282101561083057838290600052602060002090600602016040518060c00160405290816000820154815260200160018201805480602002602001604051908101604052809291908181526020016000905b8282101561077e5783829060005260206000209060040201604051806080016040529081600082015481526020016001820154815260200160028201548152602001600382018054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156107665780601f1061073b57610100808354040283529160200191610766565b820191906000526020600020905b81548152906001019060200180831161074957829003601f168201915b5050505050815250508152602001906001019061068c565b50505050815260200160028201548152602001600382015481526020016004820160009054906101000a90046001600160a01b03166001600160a01b03166001600160a01b031681526020016005820180548060200260200160405190810160405280929190818152602001828054801561081857602002820191906000526020600020905b815481526020019060010190808311610804575b5050505050815250508152602001906001019061063a565b50505050905090565b60005b81518110156108cd5760008084815260200190815260200160002060010182828151811061086657fe5b60209081029190910181015182546001818101808655600095865294849020835160049093020191825582840151908201556040820151600282015560608201518051929391926108bd926003850192019061094b565b50506001909201915061083c9050565b507f13fdaebbac9da33d495b4bd32c83e33786a010730713d20c5a8ef70ca576be65338383604051610901939291906113d5565b60405180910390a17f9989e7b45071a3a51625f3275ab6ab355999fed98a1d147b1bd25459df69493e336000808581526020019081526020016000206040516101c29291906113b5565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061098c57805160ff19168380011785556109b9565b828001600101855582156109b9579182015b828111156109b957825182559160200191906001019061099e565b506109c5929150610a0b565b5090565b6040518060c001604052806000815260200160608152602001600081526020016000801916815260200160006001600160a01b03168152602001606081525090565b6103c491905b808211156109c55760008155600101610a11565b6000610a3182356114bd565b9392505050565b600082601f830112610a4957600080fd5b8135610a5c610a578261144b565b611424565b91508181835260208401935060208101905083856020840282011115610a8157600080fd5b60005b83811015610aad5781610a978882610bce565b8452506020928301929190910190600101610a84565b5050505092915050565b600082601f830112610ac857600080fd5b8135610ad6610a578261144b565b81815260209384019390925082018360005b83811015610aad5781358601610afe8882610c29565b8452506020928301929190910190600101610ae8565b600082601f830112610b2557600080fd5b8135610b33610a578261144b565b81815260209384019390925082018360005b83811015610aad5781358601610b5b8882610c29565b8452506020928301929190910190600101610b45565b600082601f830112610b8257600080fd5b8135610b90610a578261144b565b81815260209384019390925082018360005b83811015610aad5781358601610bb88882610cb0565b8452506020928301929190910190600101610ba2565b6000610a3182356103c4565b600082601f830112610beb57600080fd5b8135610bf9610a578261146c565b91508082526020830160208301858383011115610c1557600080fd5b610c208382846114eb565b50505092915050565b600060808284031215610c3b57600080fd5b610c456080611424565b90506000610c538484610bce565b8252506020610c6484848301610bce565b6020830152506040610c7884828501610bce565b604083015250606082013567ffffffffffffffff811115610c9857600080fd5b610ca484828501610bda565b60608301525092915050565b600060c08284031215610cc257600080fd5b610ccc60c0611424565b90506000610cda8484610bce565b825250602082013567ffffffffffffffff811115610cf757600080fd5b610d0384828501610ab7565b6020830152506040610d1784828501610bce565b6040830152506060610d2b84828501610bce565b6060830152506080610d3f84828501610a25565b60808301525060a082013567ffffffffffffffff811115610d5f57600080fd5b610d6b84828501610a38565b60a08301525092915050565b600060208284031215610d8957600080fd5b813567ffffffffffffffff811115610da057600080fd5b610dac84828501610b71565b949350505050565b600060208284031215610dc657600080fd5b813567ffffffffffffffff811115610ddd57600080fd5b610dac84828501610cb0565b600060208284031215610dfb57600080fd5b6000610dac8484610bce565b60008060408385031215610e1a57600080fd5b6000610e268585610bce565b925050602083013567ffffffffffffffff811115610e4357600080fd5b610e4f85828601610b14565b9150509250929050565b6000610e6583836110d3565b505060200190565b6000610a31838361119e565b6000610a3183836111f9565b6000610a318383611271565b610e9a816114da565b82525050565b610e9a816114bd565b6000610eb4826114a6565b610ebe81856114b4565b9350610ec983611494565b60005b82811015610ef457610edf868351610e59565b9550610eea82611494565b9150600101610ecc565b5093949350505050565b6000610f09826114aa565b610f1381856114b4565b9350610f1e8361149a565b60005b82811015610ef457610f3b86610f368461154d565b610e59565b9550610f46826114ae565b9150600101610f21565b6000610f5b826114a6565b610f6581856114b4565b935083602082028501610f7785611494565b60005b84811015610fae578383038852610f92838351610e6d565b9250610f9d82611494565b602098909801979150600101610f7a565b50909695505050505050565b6000610fc5826114a6565b610fcf81856114b4565b935083602082028501610fe185611494565b60005b84811015610fae578383038852610ffc838351610e6d565b925061100782611494565b602098909801979150600101610fe4565b6000611023826114aa565b61102d81856114b4565b93508360208202850161103f8561149a565b60005b84811015610fae5783830388526110598383610e79565b9250611064826114ae565b602098909801979150600101611042565b6000611080826114a6565b61108a81856114b4565b93508360208202850161109c85611494565b60005b84811015610fae5783830388526110b7838351610e85565b92506110c282611494565b60209890980197915060010161109f565b610e9a816103c4565b60006110e7826114a6565b6110f181856114b4565b93506111018185602086016114f7565b61110a81611559565b9093019392505050565b600081546001811660008114611131576001811461115757611196565b607f600283041661114281876114b4565b60ff1984168152955050602085019250611196565b6002820461116581876114b4565b95506111708561149a565b60005b8281101561118f57815488820152600190910190602001611173565b8701945050505b505092915050565b805160009060808401906111b285826110d3565b5060208301516111c560208601826110d3565b5060408301516111d860408601826110d3565b50606083015184820360608601526111f082826110dc565b95945050505050565b8054600090608084019061120c8161153a565b61121686826110d3565b505060018301546112268161153a565b61123360208701826110d3565b505060028301546112438161153a565b61125060408701826110d3565b506003840185830360608701526112678382611114565b9695505050505050565b805160009060c084019061128585826110d3565b506020830151848203602086015261129d8282610fba565b91505060408301516112b260408601826110d3565b5060608301516112c560608601826110d3565b5060808301516112d86080860182610ea0565b5060a083015184820360a08601526111f08282610ea9565b805460009060c08401906113038161153a565b61130d86826110d3565b506001840185830360208701526113248382611018565b925050600284015490506113378161153a565b61134460408701826110d3565b505060038301546113548161153a565b61136160608701826110d3565b5050600483015461137181611527565b61137e6080870182610ea0565b506005840185830360a08701526112678382610efe565b604081016113a38285610e91565b8181036020830152610dac8184611271565b604081016113c38285610e91565b8181036020830152610dac81846112f0565b606081016113e38286610e91565b6113f060208301856110d3565b81810360408301526111f08184610f50565b60208082528101610a318184611075565b60208082528101610a318184611271565b60405181810167ffffffffffffffff8111828210171561144357600080fd5b604052919050565b600067ffffffffffffffff82111561146257600080fd5b5060209081020190565b600067ffffffffffffffff82111561148357600080fd5b506020601f91909101601f19160190565b60200190565b60009081526020902090565b5190565b5490565b60010190565b90815260200190565b60006114c8826114ce565b92915050565b6001600160a01b031690565b60006114c88260006114c8826114bd565b82818337506000910152565b60005b838110156115125781810151838201526020016114fa565b83811115611521576000848401525b50505050565b60006114c8611535836103c4565b6114ce565b60006114c8611548836103c4565b6103c4565b60006114c8825461153a565b601f01601f19169056fea265627a7a7230582067491039c4738f74dd4c2305eed783b636a17b2ab15fa7a656497dbf2b38288a6c6578706572696d656e74616cf50037405787fa12a823e0f2b7631cc41b3ba8828b3321ca811111fa75cd3aa3bb5ace405787fa12a823e0f2b7631cc41b3ba8828b3321ca811111fa75cd3aa3bb5acf405787fa12a823e0f2b7631cc41b3ba8828b3321ca811111fa75cd3aa3bb5ad0405787fa12a823e0f2b7631cc41b3ba8828b3321ca811111fa75cd3aa3bb5ad1";
            public StructsSample3DeploymentBase() : base(BYTECODE) { }
            public StructsSample3DeploymentBase(string byteCode) : base(byteCode) { }

        }

        

        [Fact]
        public async void ShouldDecodeDefaultArrayAndConvertToJObject2()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<StructsSample3Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contract = web3.Eth.GetContract("[{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'},{'name':'id2','type':'bytes32'},{'name':'id3','type':'address'},{'name':'id5','type':'bytes32[]'}],'name':'purchaseOrder','type':'tuple'}],'name':'SetPurchaseOrder','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder2','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'},{'name':'id2','type':'bytes32'},{'name':'id3','type':'address'},{'name':'id5','type':'bytes32[]'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'},{'name':'id2','type':'bytes32'},{'name':'id3','type':'address'},{'name':'id5','type':'bytes32[]'}],'name':'purchaseOrder','type':'tuple[]'}],'name':'SetPurchaseOrders','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[{'name':'id','type':'uint256'}],'name':'GetPurchaseOrder','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'},{'name':'id2','type':'bytes32'},{'name':'id3','type':'address'},{'name':'id5','type':'bytes32[]'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder3','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'},{'name':'id2','type':'bytes32'},{'name':'id3','type':'address'},{'name':'id5','type':'bytes32[]'}],'name':'purchaseOrder','type':'tuple[]'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'}],'name':'AddLineItems','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'},{'name':'id2','type':'bytes32'},{'name':'id3','type':'address'},{'name':'id5','type':'bytes32[]'}],'indexed':false,'name':'purchaseOrder','type':'tuple'}],'name':'PurchaseOrderChanged','type':'event'},{'anonymous':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'},{'name':'id2','type':'bytes32'},{'name':'id3','type':'address'},{'name':'id5','type':'bytes32[]'}],'indexed':false,'name':'purchaseOrder','type':'tuple[]'}],'name':'PurchaseOrdersChanged','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'indexed':false,'name':'purchaseOrderId','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'indexed':false,'name':'lineItem','type':'tuple[]'}],'name':'LineItemsAdded','type':'event'}]", deploymentReceipt.ContractAddress);
            var functionPO3 = contract.GetFunction("GetPurchaseOrder3");
            var result = await functionPO3.CallDecodingToDefaultAsync().ConfigureAwait(false);


            var expected = JToken.Parse(
                @"{
  'purchaseOrder': [
    {
      'id': '1',
      'lineItem': [
        {
          'id': '1',
          'productId': '100',
          'quantity': '2',
          'description': 'hello1'
        },
        {
          'id': '2',
          'productId': '200',
          'quantity': '3',
          'description': 'hello2'
        },
        {
          'id': '3',
          'productId': '300',
          'quantity': '4',
          'description': 'hello3'
        }
      ],
      'customerId': '1000',
      'id2': '6334445500000000000000000000000000000000000000000000000012434440',
      'id3': '0x12890D2cce102216644c59daE5baed380d84830c',
      'id5': [
        '6334445500000000000000000000000000000000000000000000000012434440',
        '6334445500000000000000000000000000000000000000000000000012434440',
        '6334445500000000000000000000000000000000000000000000000012434440',
        '6334445500000000000000000000000000000000000000000000000012434440',
        '6334445500000000000000000000000000000000000000000000000012434440'
      ]
    }
  ]
}");
            Assert.True(JObject.DeepEquals(expected, result.ConvertToJObject()));
        }
    }

    /*

    pragma solidity "0.5.7";
pragma experimental ABIEncoderV2;

contract StructsSample3
    {
        mapping(uint => PurchaseOrder) purchaseOrders;
        PurchaseOrder po;
        PurchaseOrder []
        purchaseOrders2;
        event PurchaseOrderChanged(address sender, PurchaseOrder purchaseOrder);
        event PurchaseOrdersChanged(PurchaseOrder []
        purchaseOrder);
        event LineItemsAdded(address sender, uint purchaseOrderId, LineItem []
        lineItem);

        constructor() public {
 
            po.id = 1 ;
            po.customerId = 1000;
            po.id2 = 0x6334445500000000000000000000000000000000000000000000000012434440;
            po.id3 = 0x12890D2cce102216644c59daE5baed380d84830c;
            po.id5.push(po.id2);
            po.id5.push(po.id2);
            po.id5.push(po.id2);
            po.id5.push(po.id2);
            po.id5.push(po.id2);
            LineItem memory lineItem = LineItem(1,100,2, "hello1");
    po.lineItem.push(lineItem);
           
            
            LineItem memory lineItem2 = LineItem(2,200,3, "hello2");
    po.lineItem.push(lineItem2);
            
             LineItem memory lineItem3 = LineItem(3,300,4, "hello3");
    po.lineItem.push(lineItem3);
            purchaseOrders2.push(po);
           
           
        }

struct PurchaseOrder
{
    uint256 id;
    LineItem[] lineItem;
    uint256 customerId;
    bytes32 id2;
    address id3;
    bytes32[] id5;
}

struct LineItem
{
    uint256 id;
    uint256 productId;
    uint256 quantity;
    string description;
}

struct Test
{
    uint256 id;
    string[] strings;
}

function SetPurchaseOrder(PurchaseOrder memory purchaseOrder) public
{
    PurchaseOrder storage purchaseOrderTemp = purchaseOrders[purchaseOrder.id];
    purchaseOrderTemp.id = purchaseOrder.id;
    purchaseOrderTemp.customerId = purchaseOrder.customerId;


    for (uint x = 0; x < purchaseOrder.lineItem.length; x++)
    {
        purchaseOrderTemp.lineItem.push(purchaseOrder.lineItem[x]);
    }

    emit PurchaseOrderChanged(msg.sender, purchaseOrder);
}

function SetPurchaseOrders(PurchaseOrder[] memory purchaseOrder) public
{
    for (uint i = 0; i < purchaseOrder.length; i++)
    {
        SetPurchaseOrder(purchaseOrder[i]);
    }
    emit PurchaseOrdersChanged(purchaseOrder);
}

function GetPurchaseOrder(uint id) view public returns (PurchaseOrder memory purchaseOrder)
{
    return purchaseOrders[id];
}

function GetPurchaseOrder2() public view returns(PurchaseOrder memory purchaseOrder)
{
    return purchaseOrders2[0];
}

function GetPurchaseOrder3() public view returns(PurchaseOrder[] memory purchaseOrder)
{
    return purchaseOrders2;
}

function AddLineItems(uint id, LineItem[] memory lineItem) public
{
    for (uint x = 0; x < lineItem.length; x++)
    {
        purchaseOrders[id].lineItem.push(lineItem[x]);
    }
    emit LineItemsAdded(msg.sender, id, lineItem);
    emit PurchaseOrderChanged(msg.sender, purchaseOrders[id]);
}
        
}
*/
}﻿using System.Collections.Generic;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Nethereum.Hex.HexConvertors.Extensions;
using Xunit;
using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System;
using Newtonsoft.Json.Linq;
using Nethereum.Hex.HexTypes;
using System.Linq;


namespace Nethereum.Accounts.IntegrationTests
{

    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TestInternalDynamicArrayOfDynamicStructs
    {

        //Struct with dynamic array of structs containing strings

        /*
pragma solidity "0.5.7";
pragma experimental ABIEncoderV2;

contract StructsSample2
{
        mapping(uint => PurchaseOrder) purchaseOrders;
        PurchaseOrder po;
        PurchaseOrder[] purchaseOrders2;
        event PurchaseOrderChanged(address sender, PurchaseOrder purchaseOrder);
        event PurchaseOrdersChanged(PurchaseOrder[] purchaseOrder);
        event LineItemsAdded(address sender, uint purchaseOrderId, LineItem[] lineItem);
        
        constructor() public {
 
            po.id = 1 ;
            po.customerId = 1000;
            LineItem memory lineItem = LineItem(1,100,2, "hello1");
            po.lineItem.push(lineItem);
           
            
            LineItem memory lineItem2 = LineItem(2,200,3, "hello2");
            po.lineItem.push(lineItem2);
            
             LineItem memory lineItem3 = LineItem(3,300,4, "hello3");
            po.lineItem.push(lineItem3);
            purchaseOrders2.push(po);
           
           
        }
        
        struct PurchaseOrder {
            uint256 id;
            LineItem[] lineItem;
            uint256 customerId;
        }

        struct LineItem {
            uint256 id;
            uint256 productId;
            uint256 quantity;
            string description;
        }
        
        struct Test{
            uint256 id;
            string[] strings;    
        }

        function SetPurchaseOrder(PurchaseOrder memory purchaseOrder) public {
            PurchaseOrder storage purchaseOrderTemp = purchaseOrders[purchaseOrder.id];
            purchaseOrderTemp.id = purchaseOrder.id;
            purchaseOrderTemp.customerId = purchaseOrder.customerId;
            
          
            for (uint x = 0; x < purchaseOrder.lineItem.length; x++)
            {
                purchaseOrderTemp.lineItem.push(purchaseOrder.lineItem[x]);
            }
            
            emit PurchaseOrderChanged(msg.sender, purchaseOrder);
        }

        function SetPurchaseOrders(PurchaseOrder[] memory purchaseOrder) public {
            for (uint i = 0; i < purchaseOrder.length; i ++)
            {
                SetPurchaseOrder(purchaseOrder[i]);
            }
             emit PurchaseOrdersChanged(purchaseOrder);
        }

        function GetPurchaseOrder(uint id) view public returns (PurchaseOrder memory purchaseOrder) {
           return purchaseOrders[id];
        }

        function GetPurchaseOrder2() public view returns (PurchaseOrder memory purchaseOrder) {
           return purchaseOrders2[0];
        }
        
        function GetPurchaseOrder3() public view returns (PurchaseOrder[] memory purchaseOrder) {
            return purchaseOrders2;
        }
        
        function AddLineItems(uint id, LineItem[] memory lineItem) public {
            for (uint x = 0; x < lineItem.length; x++)
            {
                purchaseOrders[id].lineItem.push(lineItem[x]);
            }
            emit LineItemsAdded(msg.sender, id, lineItem);
            emit PurchaseOrderChanged(msg.sender, purchaseOrders[id]);
        }
        
}

*/

    private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TestInternalDynamicArrayOfDynamicStructs(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public void ShouldEncodeSignatureWithStructArrays()
        {
            var functionAbi = ABITypedRegistry.GetFunctionABI<SetPurchaseOrderFunction>();
            Assert.Equal("f79eb4a2", functionAbi.Sha3Signature);

            functionAbi = ABITypedRegistry.GetFunctionABI<SetPurchaseOrdersFunction>();
            Assert.Equal("1a9542af", functionAbi.Sha3Signature);
        }

        [Fact]
        public void ShouldEncodeStructContainingStructArrayWithDynamicTuple()
        {
            var purchaseOrder = new PurchaseOrder();
            purchaseOrder.CustomerId = 2;
            purchaseOrder.Id = 1;
            purchaseOrder.LineItem = new List<LineItem>();
            purchaseOrder.LineItem.Add(new LineItem() { Id = 1, ProductId = 2, Quantity = 3, Description = "hello" });

            var func = new SetPurchaseOrderFunction() { PurchaseOrder = purchaseOrder };
            var data = func.GetCallData();
            var expected = "0000000000000000000000000000000000000000000000000000000000000020000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000600000000000000000000000000000000000000000000000000000000000000002000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000030000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000568656c6c6f000000000000000000000000000000000000000000000000000000";
            Assert.Equal(expected, data.ToHex().Substring(8));
        }

        /*
        constructor() public {

          po.id = 1 ;
          po.customerId = 1000;
          LineItem memory lineItem = LineItem(1,100,2, "hello1");
          po.lineItem.push(lineItem);


          LineItem memory lineItem2 = LineItem(2,200,3, "hello2");
          po.lineItem.push(lineItem2);
      }
      */

        [Fact]
        public void ShouldEncodeStructContainingStructArrayWithDynamicTuple2()
        {
            var purchaseOrder = new PurchaseOrder();
            purchaseOrder.CustomerId = 1000;
            purchaseOrder.Id = 1;
            purchaseOrder.LineItem = new List<LineItem>();
            purchaseOrder.LineItem.Add(new LineItem() { Id = 1, ProductId = 100, Quantity = 2, Description = "hello1" });
            purchaseOrder.LineItem.Add(new LineItem() { Id = 2, ProductId = 200, Quantity = 3, Description = "hello2" });

            var func = new SetPurchaseOrderFunction() { PurchaseOrder = purchaseOrder };
            var data = func.GetCallData();
            var expected = "00000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000006000000000000000000000000000000000000000000000000000000000000003e80000000000000000000000000000000000000000000000000000000000000002000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000006400000000000000000000000000000000000000000000000000000000000000020000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000668656c6c6f310000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000c800000000000000000000000000000000000000000000000000000000000000030000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000668656c6c6f320000000000000000000000000000000000000000000000000000";
            Assert.Equal(expected, data.ToHex().Substring(8));
        }

        /*
        constructor() public {

          po.id = 1 ;
          po.customerId = 1000;
          LineItem memory lineItem = LineItem(1,100,2, "hello1");
          po.lineItem.push(lineItem);


          LineItem memory lineItem2 = LineItem(2,200,3, "hello2");
          po.lineItem.push(lineItem2);

          LineItem memory lineItem3 = LineItem(3,300,4, "hello3");
          po.lineItem.push(lineItem3);
          purchaseOrders2.push(po);


      }
      */

        [Fact]
        public void ShouldEncodeStructContainingStructArrayWithDynamicTuple3()
        {
            var purchaseOrder = new PurchaseOrder();
            purchaseOrder.CustomerId = 1000;
            purchaseOrder.Id = 1;
            purchaseOrder.LineItem = new List<LineItem>();
            purchaseOrder.LineItem.Add(new LineItem() { Id = 1, ProductId = 100, Quantity = 2, Description = "hello1" });
            purchaseOrder.LineItem.Add(new LineItem() { Id = 2, ProductId = 200, Quantity = 3, Description = "hello2" });
            purchaseOrder.LineItem.Add(new LineItem() { Id = 3, ProductId = 300, Quantity = 4, Description = "hello3" });


            var func = new SetPurchaseOrderFunction() { PurchaseOrder = purchaseOrder };
            var data = func.GetCallData();
            var expected = "00000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000006000000000000000000000000000000000000000000000000000000000000003e800000000000000000000000000000000000000000000000000000000000000030000000000000000000000000000000000000000000000000000000000000060000000000000000000000000000000000000000000000000000000000000012000000000000000000000000000000000000000000000000000000000000001e00000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000006400000000000000000000000000000000000000000000000000000000000000020000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000668656c6c6f310000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000c800000000000000000000000000000000000000000000000000000000000000030000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000668656c6c6f3200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003000000000000000000000000000000000000000000000000000000000000012c00000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000668656c6c6f330000000000000000000000000000000000000000000000000000";
            Assert.Equal(expected, data.ToHex().Substring(8));
        }


        [Fact]
        public async void ShouldEncodeStructContainingArrayUsingJson()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<StructsSample2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contract = web3.Eth.GetContract("[{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'name':'SetPurchaseOrders','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder2','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'id','type':'uint256'}],'name':'GetPurchaseOrder','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder3','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'}],'name':'AddLineItems','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'name':'SetPurchaseOrder','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple'}],'name':'PurchaseOrderChanged','type':'event'},{'anonymous':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple[]'}],'name':'PurchaseOrdersChanged','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'indexed':false,'name':'purchaseOrderId','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'indexed':false,'name':'lineItem','type':'tuple[]'}],'name':'LineItemsAdded','type':'event'}]", deploymentReceipt.ContractAddress);

            var json = @"{'purchaseOrder':{
  'id': 1,
  'lineItem': [
    {
      'id': 1,
      'productId': 100,
      'quantity': 2,
      'description': 'hello1'
    },
    {
      'id': 2,
      'productId': 200,
      'quantity': 3,
      'description': 'hello2'
    }],
    'customerId': 1000
}}";
            
            var functionPurchaseOrder = contract.GetFunction("SetPurchaseOrder");
            var values = functionPurchaseOrder.ConvertJsonToObjectInputParameters(json);
            var receiptSending = await functionPurchaseOrder.
                                           SendTransactionAndWaitForReceiptAsync(
                                               EthereumClientIntegrationFixture.AccountAddress,
                                               new HexBigInteger(900000), null, null,
                                                values.ToArray()).ConfigureAwait(false);


            var eventPurchaseOrder = contract.GetEvent("PurchaseOrderChanged");
            var eventOutputs = eventPurchaseOrder.DecodeAllEventsDefaultForEvent(receiptSending.Logs);

            var jObjectEvent = eventOutputs[0].Event.ConvertToJObject();

            var expectedJObject = JObject.Parse(@"{
  'sender': '0x12890D2cce102216644c59daE5baed380d84830c',
  'purchaseOrder':{
  'id': '1',
  'lineItem': [
    {
      'id': '1',
      'productId': '100',
      'quantity': '2',
      'description': 'hello1'
    },
    {
      'id': '2',
      'productId': '200',
      'quantity': '3',
      'description': 'hello2'
    }],
    'customerId': '1000'
    }
}");
            Assert.True(JObject.DeepEquals(expectedJObject, jObjectEvent));
        }

        

        [Fact]
        public async void ShouldEncodeDecodeStructContainingStructsArrayOnlyUsingObjects()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<StructsSample2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contract = web3.Eth.GetContract("[{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'name':'SetPurchaseOrders','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder2','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'id','type':'uint256'}],'name':'GetPurchaseOrder','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder3','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'}],'name':'AddLineItems','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'name':'SetPurchaseOrder','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple'}],'name':'PurchaseOrderChanged','type':'event'},{'anonymous':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple[]'}],'name':'PurchaseOrdersChanged','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'indexed':false,'name':'purchaseOrderId','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'indexed':false,'name':'lineItem','type':'tuple[]'}],'name':'LineItemsAdded','type':'event'}]", deploymentReceipt.ContractAddress);

            /*
              struct PurchaseOrder {
            uint256 id;
            LineItem[] lineItem;
            uint256 customerId;
        }

        struct LineItem {
            uint256 id;
            uint256 productId;
            uint256 quantity;
            string description;
        }
         
             */
            var purchaseOrder = new List<object>();
            purchaseOrder.Add(1); // id
            
            var lineItem1 = new List<object>();
            lineItem1.Add(1); //id
            lineItem1.Add(100); //productId
            lineItem1.Add(2); //quantity
            lineItem1.Add("hello1"); //description

            var lineItem2 = new List<object>();
            lineItem2.Add(2); //id
            lineItem2.Add(200); //productId
            lineItem2.Add(3); //quantity
            lineItem2.Add("hello2"); //description

            var lineItems = new List<object>();
            lineItems.Add(lineItem1.ToArray());
            lineItems.Add(lineItem2.ToArray());

            purchaseOrder.Add(lineItems); // lineItems

            purchaseOrder.Add(1000); // customerId



            var functionPurchaseOrder = contract.GetFunction("SetPurchaseOrder");
            var receiptSending = await functionPurchaseOrder.
                                            SendTransactionAndWaitForReceiptAsync(
                                                EthereumClientIntegrationFixture.AccountAddress, 
                                                new HexBigInteger(900000), null, null,
                                                new object[] { purchaseOrder.ToArray() }).ConfigureAwait(false);


            var eventPurchaseOrder = contract.GetEvent<PurchaseOrderChangedEventDTO>();
            var eventOutputs = eventPurchaseOrder.DecodeAllEventsForEvent(receiptSending.Logs);
            var purchaseOrderResult = eventOutputs[0].Event.PurchaseOrder;
      
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal("hello1", purchaseOrderResult.LineItem[0].Description);

            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);
            Assert.Equal("hello2", purchaseOrderResult.LineItem[1].Description);
           
        }


        [Fact]
        public async void ShouldEncodeDecodeStructContainingStructsArrayOnlyUsingTypedStructs()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<StructsSample2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var contract = web3.Eth.GetContract("[{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'name':'SetPurchaseOrders','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder2','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'id','type':'uint256'}],'name':'GetPurchaseOrder','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'GetPurchaseOrder3','outputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple[]'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'}],'name':'AddLineItems','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'name':'purchaseOrder','type':'tuple'}],'name':'SetPurchaseOrder','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple'}],'name':'PurchaseOrderChanged','type':'event'},{'anonymous':false,'inputs':[{'components':[{'name':'id','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'name':'lineItem','type':'tuple[]'},{'name':'customerId','type':'uint256'}],'indexed':false,'name':'purchaseOrder','type':'tuple[]'}],'name':'PurchaseOrdersChanged','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'sender','type':'address'},{'indexed':false,'name':'purchaseOrderId','type':'uint256'},{'components':[{'name':'id','type':'uint256'},{'name':'productId','type':'uint256'},{'name':'quantity','type':'uint256'},{'name':'description','type':'string'}],'indexed':false,'name':'lineItem','type':'tuple[]'}],'name':'LineItemsAdded','type':'event'}]", deploymentReceipt.ContractAddress);

            var purchaseOrder = new PurchaseOrder();
            purchaseOrder.CustomerId = 1000;
            purchaseOrder.Id = 1;
            purchaseOrder.LineItem = new List<LineItem>();
            purchaseOrder.LineItem.Add(new LineItem() { Id = 1, ProductId = 100, Quantity = 2, Description = "hello1" });
            purchaseOrder.LineItem.Add(new LineItem() { Id = 2, ProductId = 200, Quantity = 3, Description = "hello2" });

            var functionPurchaseOrder = contract.GetFunction("SetPurchaseOrder");
            var receiptSending = await functionPurchaseOrder.SendTransactionAndWaitForReceiptAsync(EthereumClientIntegrationFixture.AccountAddress, new HexBigInteger(900000), null, null,
                purchaseOrder).ConfigureAwait(false);
            
            
            var eventPurchaseOrder = contract.GetEvent<PurchaseOrderChangedEventDTO>();
            var eventOutputs = eventPurchaseOrder.DecodeAllEventsForEvent(receiptSending.Logs);
            var purchaseOrderResult = eventOutputs[0].Event.PurchaseOrder;
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal("hello1", purchaseOrderResult.LineItem[0].Description);

            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);
            Assert.Equal("hello2", purchaseOrderResult.LineItem[1].Description);


            var lineItems = new List<LineItem>();
            lineItems.Add(new LineItem() { Id = 3, ProductId = 300, Quantity = 2, Description = "hello3" });
            lineItems.Add(new LineItem() { Id = 4, ProductId = 400, Quantity = 3, Description = "hello4" });

            var addLineItemsUntypedFunction = contract.GetFunction("AddLineItems");
            
            receiptSending = await addLineItemsUntypedFunction.SendTransactionAndWaitForReceiptAsync(EthereumClientIntegrationFixture.AccountAddress, new HexBigInteger(900000), null,
                null, 1, lineItems);

            var eventLineItemsAdded = contract.GetEvent("LineItemsAdded");

            var x = eventLineItemsAdded.DecodeAllEventsDefaultForEvent(receiptSending.Logs);

            var expectedJObject = JObject.Parse(@"{
  'sender': '0x12890D2cce102216644c59daE5baed380d84830c',
  'purchaseOrderId': '1',
  'lineItem': [
    {
      'id': '3',
      'productId': '300',
      'quantity': '2',
      'description': 'hello3'
    },
    {
      'id': '4',
      'productId': '400',
      'quantity': '3',
      'description': 'hello4'
    }
  ]
}");
            Assert.True(JObject.DeepEquals(expectedJObject, x[0].Event.ConvertToJObject()));

            Assert.True(JToken.DeepEquals(expectedJObject,
                ABITypedRegistry.GetEvent<LineItemsAddedEventDTO>()
                                .DecodeEventDefaultTopics(receiptSending.Logs[0])
                                .Event.ConvertToJObject()));

            var getPurchaseOrderFunction = contract.GetFunction("GetPurchaseOrder");
            //Not deserialising to DTO so just simple CallAsync
            purchaseOrderResult = await getPurchaseOrderFunction.CallAsync<PurchaseOrder>(1).ConfigureAwait(false);

            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);
            Assert.Equal(3, purchaseOrderResult.LineItem[2].Id);
            Assert.Equal(300, purchaseOrderResult.LineItem[2].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[2].Quantity);
            Assert.Equal(4, purchaseOrderResult.LineItem[3].Id);
            Assert.Equal(400, purchaseOrderResult.LineItem[3].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[3].Quantity);


            var purchaseOrderResultOutput = await getPurchaseOrderFunction.CallAsync<GetPurchaseOrderOutputDTO>(1).ConfigureAwait(false);

            purchaseOrderResult = purchaseOrderResultOutput.PurchaseOrder;

            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);
            Assert.Equal(3, purchaseOrderResult.LineItem[2].Id);
            Assert.Equal(300, purchaseOrderResult.LineItem[2].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[2].Quantity);
            Assert.Equal(4, purchaseOrderResult.LineItem[3].Id);
            Assert.Equal(400, purchaseOrderResult.LineItem[3].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[3].Quantity);


            var listPurchaseOrder = new List<PurchaseOrder>();
            listPurchaseOrder.Add(purchaseOrder);

            var setPurchaseOrdersFunction = contract.GetFunction("SetPurchaseOrders");

            receiptSending = await setPurchaseOrdersFunction.SendTransactionAndWaitForReceiptAsync(EthereumClientIntegrationFixture.AccountAddress, new HexBigInteger(900000), null,
                null, listPurchaseOrder);
            

            var eventPurchaseOrders = contract.GetEvent("PurchaseOrdersChanged");
            var eventPurchaseOrdersOutputs = eventPurchaseOrders.DecodeAllEventsForEvent<PurchaseOrdersChangedEventDTO>(receiptSending.Logs);
            purchaseOrderResult = eventPurchaseOrdersOutputs[0].Event.PurchaseOrder[0];

            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);


            //Stored array on constructor
            var getPurchaseOrder3Function = contract.GetFunction("GetPurchaseOrder3");

            //Not deserialising to DTO so just simple CallAsync
            var purchaseOrderResults = await getPurchaseOrder3Function.CallAsync<List<PurchaseOrder>>();
            /*
              constructor() public {
            _purchaseOrder.id = 1;
            _purchaseOrder.customerId = 2;
            LineItem memory lineItem = LineItem(1,2,3);
            _purchaseOrder.lineItem.push(lineItem);
            purchaseOrdersArray.push(_purchaseOrder); 
        }
        */
            purchaseOrderResult = purchaseOrderResults[0];
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
        }

        [Fact]
        public async void ShouldEncodeDecodeStructContainingStructsArray()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<StructsSample2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var purchaseOrder = new PurchaseOrder();
            purchaseOrder.CustomerId = 1000;
            purchaseOrder.Id = 1;
            purchaseOrder.LineItem = new List<LineItem>();
            purchaseOrder.LineItem.Add(new LineItem() { Id = 1, ProductId = 100, Quantity = 2, Description = "hello1" });
            purchaseOrder.LineItem.Add(new LineItem() { Id = 2, ProductId = 200, Quantity = 3, Description = "hello2"});

            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);

            var receiptSending = await contractHandler.SendRequestAndWaitForReceiptAsync(new SetPurchaseOrderFunction() { PurchaseOrder = purchaseOrder }).ConfigureAwait(false);
            var eventPurchaseOrder = contractHandler.GetEvent<PurchaseOrderChangedEventDTO>();
            var eventOutputs = eventPurchaseOrder.DecodeAllEventsForEvent(receiptSending.Logs);
            var purchaseOrderResult = eventOutputs[0].Event.PurchaseOrder;
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal("hello1", purchaseOrderResult.LineItem[0].Description);

            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);
            Assert.Equal("hello2", purchaseOrderResult.LineItem[1].Description);


            var query = await contractHandler.QueryDeserializingToObjectAsync<GetPurchaseOrderFunction, GetPurchaseOrderOutputDTO>(new GetPurchaseOrderFunction() { Id = 1 }).ConfigureAwait(false);

            purchaseOrderResult = query.PurchaseOrder;
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal("hello1", purchaseOrderResult.LineItem[0].Description);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);
            Assert.Equal("hello2", purchaseOrderResult.LineItem[1].Description);
       
            var lineItems = new List<LineItem>();
            lineItems.Add(new LineItem() { Id = 3, ProductId = 300, Quantity = 2, Description = "hello3" });
            lineItems.Add(new LineItem() { Id = 4, ProductId = 400, Quantity = 3, Description = "hello4" });

            var lineItemsFunction = new AddLineItemsFunction() { Id = 1, LineItem = lineItems };
            var data = lineItemsFunction.GetCallData().ToHex();

            
            receiptSending = await contractHandler.SendRequestAndWaitForReceiptAsync(new AddLineItemsFunction() { Id = 1, LineItem = lineItems }).ConfigureAwait(false);

            var lineItemsEvent = contractHandler.GetEvent<LineItemsAddedEventDTO>();
            var lineItemsLogs = lineItemsEvent.DecodeAllEventsForEvent(receiptSending.Logs);


            

            query = await contractHandler.QueryDeserializingToObjectAsync<GetPurchaseOrderFunction, GetPurchaseOrderOutputDTO>(new GetPurchaseOrderFunction() { Id = 1 }).ConfigureAwait(false);
            purchaseOrderResult = query.PurchaseOrder;
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);
            Assert.Equal(3, purchaseOrderResult.LineItem[2].Id);
            Assert.Equal(300, purchaseOrderResult.LineItem[2].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[2].Quantity);
            Assert.Equal(4, purchaseOrderResult.LineItem[3].Id);
            Assert.Equal(400, purchaseOrderResult.LineItem[3].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[3].Quantity);



            //Purchase Orders

            var listPurchaseOrder = new List<PurchaseOrder>();
            listPurchaseOrder.Add(purchaseOrder);
            var func = new SetPurchaseOrdersFunction() { PurchaseOrder = listPurchaseOrder };
            receiptSending = await contractHandler.SendRequestAndWaitForReceiptAsync(func).ConfigureAwait(false);
            var eventPurchaseOrders = contractHandler.GetEvent<PurchaseOrdersChangedEventDTO>();
            var eventPurchaseOrdersOutputs = eventPurchaseOrders.DecodeAllEventsForEvent(receiptSending.Logs);
            purchaseOrderResult = eventPurchaseOrdersOutputs[0].Event.PurchaseOrder[0];

            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);

            //Stored array on constructor
            var query2 = await contractHandler.QueryDeserializingToObjectAsync<GetPurchaseOrder3Function, GetPurchaseOrder3OutputDTO>().ConfigureAwait(false);
            /*
              constructor() public {
            _purchaseOrder.id = 1;
            _purchaseOrder.customerId = 2;
            LineItem memory lineItem = LineItem(1,2,3);
            _purchaseOrder.lineItem.push(lineItem);
            purchaseOrdersArray.push(_purchaseOrder); 
        }
        */

        purchaseOrderResult = query2.PurchaseOrder[0];
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);

           
        }

        

        public partial class PurchaseOrder : PurchaseOrderBase { }

        public class PurchaseOrderBase
        {
            [Parameter("uint256", "id", 1)]
            public virtual BigInteger Id { get; set; }
            [Parameter("tuple[]", "lineItem", 2)]
            public virtual List<LineItem> LineItem { get; set; }
            [Parameter("uint256", "customerId", 3)]
            public virtual BigInteger CustomerId { get; set; }
        }

        public partial class LineItem : LineItemBase { }

        public class LineItemBase
        {
            [Parameter("uint256", "id", 1)]
            public virtual BigInteger Id { get; set; }
            [Parameter("uint256", "productId", 2)]
            public virtual BigInteger ProductId { get; set; }
            [Parameter("uint256", "quantity", 3)]
            public virtual BigInteger Quantity { get; set; }
            [Parameter("string", "description", 4)]
            public virtual string Description { get; set; }
        }
        public partial class StructsSample2Deployment : StructsSample2DeploymentBase
        {
            public StructsSample2Deployment() : base(BYTECODE) { }
            public StructsSample2Deployment(string byteCode) : base(byteCode) { }
        }

        public class StructsSample2DeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "60806040523480156200001157600080fd5b50600180556103e8600355620000266200035e565b506040805160808101825260018082526064602080840191825260028486018181528651808801909752600687527f68656c6c6f310000000000000000000000000000000000000000000000000000878401526060860196875281549485018083556000929092528551600490950260008051602062001774833981519152810195865593516000805160206200179483398151915285015551600080516020620017b483398151915284015594518051949594869493620000fe93600080516020620017d483398151915290910192019062000386565b505050506200010c6200035e565b5060408051608081018252600280825260c8602080840191825260038486019081528551808701909652600686527f68656c6c6f3200000000000000000000000000000000000000000000000000008683015260608501958652835460018101808655600095909552855160049091026000805160206200177483398151915281019182559351600080516020620017948339815191528501559051600080516020620017b48339815191528401559451805194959394869493620001e793600080516020620017d483398151915290910192019062000386565b50505050620001f56200035e565b50604080516080810182526003815261012c602080830191825260048385018181528551808701909652600686527f68656c6c6f33000000000000000000000000000000000000000000000000000086840152606085019586526002805460018101808355600092909252865193026000805160206200177483398151915281019384559451600080516020620017948339815191528601559051600080516020620017b48339815191528501559451805194959486949293620002cd93600080516020620017d48339815191520192019062000386565b5050600480546001818101808455600093909352805460039092027f8a35acfbc15ff81a39ae7d344fd709f28e8600b4aa8c65c6b64bfe7fe36bd19b810192835560028054949650919450919262000349927f8a35acfbc15ff81a39ae7d344fd709f28e8600b4aa8c65c6b64bfe7fe36bd19c0191906200040b565b5060029182015491015550620005cb92505050565b6040518060800160405280600081526020016000815260200160008152602001606081525090565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f10620003c957805160ff1916838001178555620003f9565b82800160010185558215620003f9579182015b82811115620003f9578251825591602001919060010190620003dc565b5062000407929150620004aa565b5090565b8280548282559060005260206000209060040281019282156200049c5760005260206000209160040282015b828111156200049c5782826000820154816000015560018201548160010155600282015481600201556003820181600301908054600181600116156101000203166002900462000489929190620004ca565b5050509160040191906004019062000437565b506200040792915062000544565b620004c791905b80821115620004075760008155600101620004b1565b90565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f10620005055780548555620003f9565b82800160010185558215620003f957600052602060002091601f016020900482015b82811115620003f957825482559160010191906001019062000527565b620004c791905b8082111562000407576000808255600182018190556002820181905562000576600383018262000580565b506004016200054b565b50805460018160011615610100020316600290046000825580601f10620005a85750620005c8565b601f016020900490600052602060002090810190620005c89190620004aa565b50565b61119980620005db6000396000f3fe608060405234801561001057600080fd5b50600436106100625760003560e01c80631a9542af14610067578063793ce7601461007c578063815c844d1461009a578063a08f28cc146100ad578063cc0b4b02146100c2578063f79eb4a2146100d5575b600080fd5b61007a610075366004610ad3565b6100e8565b005b610084610153565b604051610091919061102e565b60405180910390f35b6100846100a8366004610b45565b6102bb565b6100b561040e565b604051610091919061101d565b61007a6100d0366004610b63565b6105a0565b61007a6100e3366004610b10565b6106be565b60005b81518110156101185761011082828151811061010357fe5b60200260200101516106be565b6001016100eb565b507f63d0df058c364c605130a4550879b03d3814f0ba56c550569be936f3c2d7a2f581604051610148919061101d565b60405180910390a150565b61015b610798565b600460008154811061016957fe5b90600052602060002090600302016040518060600160405290816000820154815260200160018201805480602002602001604051908101604052809291908181526020016000905b828210156102a35783829060005260206000209060040201604051806080016040529081600082015481526020016001820154815260200160028201548152602001600382018054600181600116156101000203166002900480601f01602080910402602001604051908101604052809291908181526020018280546001816001161561010002031660029004801561028b5780601f106102605761010080835404028352916020019161028b565b820191906000526020600020905b81548152906001019060200180831161026e57829003601f168201915b505050505081525050815260200190600101906101b1565b50505050815260200160028201548152505090505b90565b6102c3610798565b6000828152602081815260408083208151606081018352815481526001820180548451818702810187019095528085529195929486810194939192919084015b828210156103f55783829060005260206000209060040201604051806080016040529081600082015481526020016001820154815260200160028201548152602001600382018054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156103dd5780601f106103b2576101008083540402835291602001916103dd565b820191906000526020600020905b8154815290600101906020018083116103c057829003601f168201915b50505050508152505081526020019060010190610303565b5050505081526020016002820154815250509050919050565b60606004805480602002602001604051908101604052809291908181526020016000905b8282101561059757838290600052602060002090600302016040518060600160405290816000820154815260200160018201805480602002602001604051908101604052809291908181526020016000905b828210156105765783829060005260206000209060040201604051806080016040529081600082015481526020016001820154815260200160028201548152602001600382018054600181600116156101000203166002900480601f01602080910402602001604051908101604052809291908181526020018280546001816001161561010002031660029004801561055e5780601f106105335761010080835404028352916020019161055e565b820191906000526020600020905b81548152906001019060200180831161054157829003601f168201915b50505050508152505081526020019060010190610484565b50505050815260200160028201548152505081526020019060010190610432565b50505050905090565b60005b8151811015610634576000808481526020019081526020016000206001018282815181106105cd57fe5b602090810291909101810151825460018181018086556000958652948490208351600490930201918255828401519082015560408201516002820155606082015180519293919261062492600385019201906107b9565b5050600190920191506105a39050565b507f13fdaebbac9da33d495b4bd32c83e33786a010730713d20c5a8ef70ca576be6533838360405161066893929190610ff0565b60405180910390a17f88ab28750130223a530a1325799e7ef636cd4c7a60d350c38c45316082fdbbf8336000808581526020019081526020016000206040516106b2929190610fd0565b60405180910390a15050565b805160009081526020819052604080822083518155908301516002820155905b8260200151518110156107665781600101836020015182815181106106ff57fe5b602090810291909101810151825460018181018086556000958652948490208351600490930201918255828401519082015560408201516002820155606082015180519293919261075692600385019201906107b9565b5050600190920191506106de9050565b507f88ab28750130223a530a1325799e7ef636cd4c7a60d350c38c45316082fdbbf833836040516106b2929190610fb0565b60405180606001604052806000815260200160608152602001600081525090565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f106107fa57805160ff1916838001178555610827565b82800160010185558215610827579182015b8281111561082757825182559160200191906001019061080c565b50610833929150610837565b5090565b6102b891905b80821115610833576000815560010161083d565b600082601f83011261086257600080fd5b813561087561087082611066565b61103f565b81815260209384019390925082018360005b838110156108b3578135860161089d88826109c6565b8452506020928301929190910190600101610887565b5050505092915050565b600082601f8301126108ce57600080fd5b81356108dc61087082611066565b81815260209384019390925082018360005b838110156108b3578135860161090488826109c6565b84525060209283019291909101906001016108ee565b600082601f83011261092b57600080fd5b813561093961087082611066565b81815260209384019390925082018360005b838110156108b357813586016109618882610a4d565b845250602092830192919091019060010161094b565b600082601f83011261098857600080fd5b813561099661087082611087565b915080825260208301602083018583830111156109b257600080fd5b6109bd838284611106565b50505092915050565b6000608082840312156109d857600080fd5b6109e2608061103f565b905060006109f08484610ac0565b8252506020610a0184848301610ac0565b6020830152506040610a1584828501610ac0565b604083015250606082013567ffffffffffffffff811115610a3557600080fd5b610a4184828501610977565b60608301525092915050565b600060608284031215610a5f57600080fd5b610a69606061103f565b90506000610a778484610ac0565b825250602082013567ffffffffffffffff811115610a9457600080fd5b610aa084828501610851565b6020830152506040610ab484828501610ac0565b60408301525092915050565b6000610acc82356102b8565b9392505050565b600060208284031215610ae557600080fd5b813567ffffffffffffffff811115610afc57600080fd5b610b088482850161091a565b949350505050565b600060208284031215610b2257600080fd5b813567ffffffffffffffff811115610b3957600080fd5b610b0884828501610a4d565b600060208284031215610b5757600080fd5b6000610b088484610ac0565b60008060408385031215610b7657600080fd5b6000610b828585610ac0565b925050602083013567ffffffffffffffff811115610b9f57600080fd5b610bab858286016108bd565b9150509250929050565b6000610acc8383610e2d565b6000610acc8383610e88565b6000610acc8383610f00565b610be2816110e4565b82525050565b6000610bf3826110c1565b610bfd81856110cf565b935083602082028501610c0f856110af565b60005b84811015610c46578383038852610c2a838351610bb5565b9250610c35826110af565b602098909801979150600101610c12565b50909695505050505050565b6000610c5d826110c1565b610c6781856110cf565b935083602082028501610c79856110af565b60005b84811015610c46578383038852610c94838351610bb5565b9250610c9f826110af565b602098909801979150600101610c7c565b6000610cbb826110c5565b610cc581856110cf565b935083602082028501610cd7856110b5565b60005b84811015610c46578383038852610cf18383610bc1565b9250610cfc826110c9565b602098909801979150600101610cda565b6000610d18826110c1565b610d2281856110cf565b935083602082028501610d34856110af565b60005b84811015610c46578383038852610d4f838351610bcd565b9250610d5a826110af565b602098909801979150600101610d37565b6000610d76826110c1565b610d8081856110cf565b9350610d90818560208601611112565b610d9981611155565b9093019392505050565b600081546001811660008114610dc05760018114610de657610e25565b607f6002830416610dd181876110cf565b60ff1984168152955050602085019250610e25565b60028204610df481876110cf565b9550610dff856110b5565b60005b82811015610e1e57815488820152600190910190602001610e02565b8701945050505b505092915050565b80516000906080840190610e418582610fa7565b506020830151610e546020860182610fa7565b506040830151610e676040860182610fa7565b5060608301518482036060860152610e7f8282610d6b565b95945050505050565b80546000906080840190610e9b81611142565b610ea58682610fa7565b50506001830154610eb581611142565b610ec26020870182610fa7565b50506002830154610ed281611142565b610edf6040870182610fa7565b50600384018583036060870152610ef68382610da3565b9695505050505050565b80516000906060840190610f148582610fa7565b5060208301518482036020860152610f2c8282610c52565b9150506040830151610f416040860182610fa7565b509392505050565b80546000906060840190610f5c81611142565b610f668682610fa7565b50600184018583036020870152610f7d8382610cb0565b92505060028401549050610f9081611142565b610f9d6040870182610fa7565b5090949350505050565b610be2816102b8565b60408101610fbe8285610bd9565b8181036020830152610b088184610f00565b60408101610fde8285610bd9565b8181036020830152610b088184610f49565b60608101610ffe8286610bd9565b61100b6020830185610fa7565b8181036040830152610e7f8184610be8565b60208082528101610acc8184610d0d565b60208082528101610acc8184610f00565b60405181810167ffffffffffffffff8111828210171561105e57600080fd5b604052919050565b600067ffffffffffffffff82111561107d57600080fd5b5060209081020190565b600067ffffffffffffffff82111561109e57600080fd5b506020601f91909101601f19160190565b60200190565b60009081526020902090565b5190565b5490565b60010190565b90815260200190565b6001600160a01b031690565b60006110ef826110f5565b92915050565b60006110ef8260006110ef826110d8565b82818337506000910152565b60005b8381101561112d578181015183820152602001611115565b8381111561113c576000848401525b50505050565b60006110ef611150836102b8565b6102b8565b601f01601f19169056fea265627a7a72305820d86be1a4bba1bef3e684f798e114d9c22018925c426cdb9ca60314aec45501f46c6578706572696d656e74616cf50037405787fa12a823e0f2b7631cc41b3ba8828b3321ca811111fa75cd3aa3bb5ace405787fa12a823e0f2b7631cc41b3ba8828b3321ca811111fa75cd3aa3bb5acf405787fa12a823e0f2b7631cc41b3ba8828b3321ca811111fa75cd3aa3bb5ad0405787fa12a823e0f2b7631cc41b3ba8828b3321ca811111fa75cd3aa3bb5ad1";
            public StructsSample2DeploymentBase() : base(BYTECODE) { }
            public StructsSample2DeploymentBase(string byteCode) : base(byteCode) { }

        }

        public partial class SetPurchaseOrdersFunction : SetPurchaseOrdersFunctionBase { }

        [Function("SetPurchaseOrders")]
        public class SetPurchaseOrdersFunctionBase : FunctionMessage
        {
            [Parameter("tuple[]", "purchaseOrder", 1)]
            public virtual List<PurchaseOrder> PurchaseOrder { get; set; }
        }

        public partial class GetPurchaseOrder2Function : GetPurchaseOrder2FunctionBase { }

        [Function("GetPurchaseOrder2", "tuple")]
        public class GetPurchaseOrder2FunctionBase : FunctionMessage
        {

        }

        public partial class GetPurchaseOrderFunction : GetPurchaseOrderFunctionBase { }

        [Function("GetPurchaseOrder", "tuple")]
        public class GetPurchaseOrderFunctionBase : FunctionMessage
        {
            [Parameter("uint256", "id", 1)]
            public virtual BigInteger Id { get; set; }
        }

        public partial class GetPurchaseOrder3Function : GetPurchaseOrder3FunctionBase { }

        [Function("GetPurchaseOrder3", "tuple[]")]
        public class GetPurchaseOrder3FunctionBase : FunctionMessage
        {

        }

        public partial class AddLineItemsFunction : AddLineItemsFunctionBase { }

        [Function("AddLineItems")]
        public class AddLineItemsFunctionBase : FunctionMessage
        {
            [Parameter("uint256", "id", 1)]
            public virtual BigInteger Id { get; set; }
            [Parameter("tuple[]", "lineItem", 2)]
            public virtual List<LineItem> LineItem { get; set; }
        }

        public partial class SetPurchaseOrderFunction : SetPurchaseOrderFunctionBase { }

        [Function("SetPurchaseOrder")]
        public class SetPurchaseOrderFunctionBase : FunctionMessage
        {
            [Parameter("tuple", "purchaseOrder", 1)]
            public virtual PurchaseOrder PurchaseOrder { get; set; }
        }

        public partial class PurchaseOrderChangedEventDTO : PurchaseOrderChangedEventDTOBase { }

        [Event("PurchaseOrderChanged")]
        public class PurchaseOrderChangedEventDTOBase : IEventDTO
        {
            [Parameter("address", "sender", 1, false)]
            public virtual string Sender { get; set; }
            [Parameter("tuple", "purchaseOrder", 2, false)]
            public virtual PurchaseOrder PurchaseOrder { get; set; }
        }

        public partial class PurchaseOrdersChangedEventDTO : PurchaseOrdersChangedEventDTOBase { }

        [Event("PurchaseOrdersChanged")]
        public class PurchaseOrdersChangedEventDTOBase : IEventDTO
        {
            [Parameter("tuple[]", "purchaseOrder", 1, false)]
            public virtual List<PurchaseOrder> PurchaseOrder { get; set; }
        }

        public partial class LineItemsAddedEventDTO : LineItemsAddedEventDTOBase { }

        [Event("LineItemsAdded")]
        public class LineItemsAddedEventDTOBase : IEventDTO
        {
            [Parameter("address", "sender", 1, false)]
            public virtual string Sender { get; set; }
            [Parameter("uint256", "purchaseOrderId", 2, false)]
            public virtual BigInteger PurchaseOrderId { get; set; }
            [Parameter("tuple[]", "lineItem", 3, false)]
            public virtual List<LineItem> LineItem { get; set; }
        }



        public partial class GetPurchaseOrder2OutputDTO : GetPurchaseOrder2OutputDTOBase { }

        [FunctionOutput]
        public class GetPurchaseOrder2OutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("tuple", "purchaseOrder", 1)]
            public virtual PurchaseOrder PurchaseOrder { get; set; }
        }

        public partial class GetPurchaseOrderOutputDTO : GetPurchaseOrderOutputDTOBase { }

        [FunctionOutput]
        public class GetPurchaseOrderOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("tuple", "purchaseOrder", 1)]
            public virtual PurchaseOrder PurchaseOrder { get; set; }
        }

        public partial class GetPurchaseOrder3OutputDTO : GetPurchaseOrder3OutputDTOBase { }

        [FunctionOutput]
        public class GetPurchaseOrder3OutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("tuple[]", "purchaseOrder", 1)]
            public virtual List<PurchaseOrder> PurchaseOrder { get; set; }
        }

    }
}﻿using System.Collections.Generic;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Nethereum.Hex.HexConvertors.Extensions;
using Xunit;
using Nethereum.ABI.FunctionEncoding;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TestInternalDynamicArrayOfNonDynamicStructs
    {

        /*
pragma solidity "0.5.7";
pragma experimental ABIEncoderV2;

contract StructsSample
{
        mapping(uint => PurchaseOrder) purchaseOrders;
        event PurchaseOrderChanged(address sender, PurchaseOrder purchaseOrder);
        event PurchaseOrdersChanged(PurchaseOrder[] purchaseOrder);
        event LineItemsAdded(address sender, uint purchaseOrderId, LineItem[] lineItem);

        //array
        PurchaseOrder[] purchaseOrdersArray;
        PurchaseOrder _purchaseOrder;

         constructor() public {
            _purchaseOrder.id = 1;
            _purchaseOrder.customerId = 2;
            LineItem memory lineItem = LineItem(1,2,3);
            _purchaseOrder.lineItem.push(lineItem);
            purchaseOrdersArray.push(_purchaseOrder); 
        }
        

        struct PurchaseOrder {
            uint256 id;
            LineItem[] lineItem;
            uint256 customerId;
        }

        struct LineItem {
            uint256 id;
            uint256 productId;
            uint256 quantity;
        }

        function SetPurchaseOrder(PurchaseOrder memory purchaseOrder) public {
            PurchaseOrder storage purchaseOrderTemp = purchaseOrders[purchaseOrder.id];
            purchaseOrderTemp.id = purchaseOrder.id;
            purchaseOrderTemp.customerId = purchaseOrder.customerId;
            
            for (uint x = 0; x < purchaseOrder.lineItem.length; x++)
            {
                purchaseOrderTemp.lineItem.push(purchaseOrder.lineItem[x]);
            }
            
            emit PurchaseOrderChanged(msg.sender, purchaseOrder);
        }

        function SetPurchaseOrders(PurchaseOrder[] memory purchaseOrder) public {
            for (uint i = 0; i < purchaseOrder.length; i ++)
            {
                SetPurchaseOrder(purchaseOrder[i]);
            }

            emit PurchaseOrdersChanged(purchaseOrder);
        }

        function GetPurchaseOrder(uint id) view public returns (PurchaseOrder memory purchaseOrder) {
           return purchaseOrders[id];
        }

        function GetPurchaseOrder2() public returns (PurchaseOrder memory purchaseOrder) {
           // return storedPurchaseOrder;
        }

         function GetPurchaseOrders() public view returns (PurchaseOrder[] memory purchaseOrder) {
            return purchaseOrdersArray;
        }
        
        function AddLineItems(uint id, LineItem[] memory lineItem) public {
            for (uint x = 0; x < lineItem.length; x++)
            {
                purchaseOrders[id].lineItem.push(lineItem[x]);
            }
            emit LineItemsAdded(msg.sender, id, lineItem);
            emit PurchaseOrderChanged(msg.sender, purchaseOrders[id]);
        }
}

*/

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TestInternalDynamicArrayOfNonDynamicStructs(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public void ShouldEncodeSignatureWithStructArrays()
        {
            var functionAbi = ABITypedRegistry.GetFunctionABI<SetPurchaseOrderFunction>();
            Assert.Equal("0cc400bd", functionAbi.Sha3Signature);

            functionAbi = ABITypedRegistry.GetFunctionABI<SetPurchaseOrdersFunction>();
            Assert.Equal("cfca7768", functionAbi.Sha3Signature);   
        }


        [Fact]
        public async void ShouldEncodeDecodeStructContainingStructsArray()
        
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<StructsSampleDeployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);

            var purchaseOrder = new PurchaseOrder();
            purchaseOrder.CustomerId = 1000;
            purchaseOrder.Id = 1;
            purchaseOrder.LineItem = new List<LineItem>();
            purchaseOrder.LineItem.Add(new LineItem() { Id = 1, ProductId = 100, Quantity = 2 });
            purchaseOrder.LineItem.Add(new LineItem() { Id = 2, ProductId = 200, Quantity = 3 });

            var contractHandler = web3.Eth.GetContractHandler(deploymentReceipt.ContractAddress);

            var receiptSending = await contractHandler.SendRequestAndWaitForReceiptAsync(new SetPurchaseOrderFunction() { PurchaseOrder = purchaseOrder }).ConfigureAwait(false);
            var eventPurchaseOrder = contractHandler.GetEvent<PurchaseOrderChangedEventDTO>();
            var eventOutputs = eventPurchaseOrder.DecodeAllEventsForEvent(receiptSending.Logs);
            var purchaseOrderResult = eventOutputs[0].Event.PurchaseOrder;
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);

          

            var query = await contractHandler.QueryAsync<GetPurchaseOrderFunction, GetPurchaseOrderOutputDTO>(new GetPurchaseOrderFunction() { Id = 1 }).ConfigureAwait(false);

            purchaseOrderResult = query.PurchaseOrder;
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);

 
      
            var lineItems = new List<LineItem>();
            lineItems.Add(new LineItem() { Id = 3, ProductId = 300, Quantity = 2 });
            lineItems.Add(new LineItem() { Id = 4, ProductId = 400, Quantity = 3 });

            var lineItemsFunction = new AddLineItemsFunction() { Id = 1, LineItem = lineItems };
            var data = lineItemsFunction.GetCallData().ToHex();

            receiptSending = await contractHandler.SendRequestAndWaitForReceiptAsync(new AddLineItemsFunction() { Id =1, LineItem = lineItems }).ConfigureAwait(false);

            var lineItemsEvent = contractHandler.GetEvent<LineItemsAddedEventDTO>();
            var lineItemsLogs = lineItemsEvent.DecodeAllEventsForEvent(receiptSending.Logs);
            query = await contractHandler.QueryAsync<GetPurchaseOrderFunction, GetPurchaseOrderOutputDTO>(new GetPurchaseOrderFunction() { Id = 1 }).ConfigureAwait(false);
            purchaseOrderResult = query.PurchaseOrder;
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);
            Assert.Equal(3, purchaseOrderResult.LineItem[2].Id);
            Assert.Equal(300, purchaseOrderResult.LineItem[2].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[2].Quantity);
            Assert.Equal(4, purchaseOrderResult.LineItem[3].Id);
            Assert.Equal(400, purchaseOrderResult.LineItem[3].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[3].Quantity);



            //Purchase Orders

            var listPurchaseOrder = new List<PurchaseOrder>();
            listPurchaseOrder.Add(purchaseOrder);
            var func = new SetPurchaseOrdersFunction() { PurchaseOrder = listPurchaseOrder };
            receiptSending = await contractHandler.SendRequestAndWaitForReceiptAsync(func).ConfigureAwait(false);
            var eventPurchaseOrders = contractHandler.GetEvent<PurchaseOrdersChangedEventDTO>();
            var eventPurchaseOrdersOutputs = eventPurchaseOrders.DecodeAllEventsForEvent(receiptSending.Logs);
            purchaseOrderResult = eventPurchaseOrdersOutputs[0].Event.PurchaseOrder[0];

            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(1000, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(100, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].Quantity);
            Assert.Equal(2, purchaseOrderResult.LineItem[1].Id);
            Assert.Equal(200, purchaseOrderResult.LineItem[1].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[1].Quantity);

            //Stored array on constructor
            var query2 = await contractHandler.QueryAsync<GetPurchaseOrdersFunction, GetPurchaseOrdersOutputDTO>().ConfigureAwait(false);
            /*
              constructor() public {
            _purchaseOrder.id = 1;
            _purchaseOrder.customerId = 2;
            LineItem memory lineItem = LineItem(1,2,3);
            _purchaseOrder.lineItem.push(lineItem);
            purchaseOrdersArray.push(_purchaseOrder); 
        }
        */

            purchaseOrderResult = query2.PurchaseOrder[0];
            Assert.Equal(1, purchaseOrderResult.Id);
            Assert.Equal(2, purchaseOrderResult.CustomerId);
            Assert.Equal(1, purchaseOrderResult.LineItem[0].Id);
            Assert.Equal(2, purchaseOrderResult.LineItem[0].ProductId);
            Assert.Equal(3, purchaseOrderResult.LineItem[0].Quantity);

        }


        public partial class PurchaseOrder : PurchaseOrderBase { }

        public class PurchaseOrderBase
        {
            [Parameter("uint256", "id", 1)]
            public virtual BigInteger Id { get; set; }
            [Parameter("tuple[]", "lineItem", 2)]
            public virtual List<LineItem> LineItem { get; set; }
            [Parameter("uint256", "customerId", 3)]
            public virtual BigInteger CustomerId { get; set; }
        }

        public partial class LineItem : LineItemBase { }

        public class LineItemBase
        {
            [Parameter("uint256", "id", 1)]
            public virtual BigInteger Id { get; set; }
            [Parameter("uint256", "productId", 2)]
            public virtual BigInteger ProductId { get; set; }
            [Parameter("uint256", "quantity", 3)]
            public virtual BigInteger Quantity { get; set; }
        }


        public partial class StructsSampleDeployment : StructsSampleDeploymentBase
        {
            public StructsSampleDeployment() : base(BYTECODE) { }
            public StructsSampleDeployment(string byteCode) : base(byteCode) { }
        }

        public class StructsSampleDeploymentBase : ContractDeploymentMessage
        {
            public static string BYTECODE = "608060405234801561001057600080fd5b5060016002908155600455610023610148565b50604080516060810182526001808252600260208301818152600394840185815285548085018755600087815286517fc2575a0e9e593c00f959f8c92f12db2869c3395a3b0502d05e2516446f71f85b9289029283015592517fc2575a0e9e593c00f959f8c92f12db2869c3395a3b0502d05e2516446f71f85c82015590517fc2575a0e9e593c00f959f8c92f12db2869c3395a3b0502d05e2516446f71f85d9091015582548084018085559390915281547fb10e2d527612073b26eecdfd717e6a320cf44b4afac2b0732d9fcbe2b7fa0cf691860291820190815585549495939492939092610136927fb10e2d527612073b26eecdfd717e6a320cf44b4afac2b0732d9fcbe2b7fa0cf7019190610169565b50600291820154910155506101fd9050565b60405180606001604052806000815260200160008152602001600081525090565b8280548282559060005260206000209060030281019282156101c35760005260206000209160030282015b828111156101c35782548255600180840154908301556002808401549083015560039283019290910190610194565b506101cf9291506101d3565b5090565b6101fa91905b808211156101cf5760008082556001820181905560028201556003016101d9565b90565b610c1a8061020c6000396000f3fe608060405234801561001057600080fd5b50600436106100625760003560e01c80630cc400bd14610067578063793ce7601461007c578063815c844d1461009a578063bde3a813146100ad578063cd21ca74146100c2578063cfca7768146100d5575b600080fd5b61007a61007536600461071a565b6100e8565b005b6100846101a8565b6040516100919190610b1d565b60405180910390f35b6100846100a836600461074f565b6101b3565b6100b5610264565b6040516100919190610b0c565b61007a6100d036600461076d565b610354565b61007a6100e33660046106dd565b610440565b805160009081526020819052604080822083518155908301516002820155905b82602001515181101561016a57816001018360200151828151811061012957fe5b602090810291909101810151825460018181018555600094855293839020825160039092020190815591810151828401556040015160029091015501610108565b507f0dc5f52079349ee37fd5e887dcc0f4f87fd42b39457373a22bd627d904b512d3338360405161019c929190610a96565b60405180910390a15050565b6101b06104ab565b90565b6101bb6104ab565b6000828152602081815260408083208151606081018352815481526001820180548451818702810187019095528085529195929486810194939192919084015b8282101561024b57838290600052602060002090600302016040518060600160405290816000820154815260200160018201548152602001600282015481525050815260200190600101906101fb565b5050505081526020016002820154815250509050919050565b60606001805480602002602001604051908101604052809291908181526020016000905b8282101561034b57838290600052602060002090600302016040518060600160405290816000820154815260200160018201805480602002602001604051908101604052809291908181526020016000905b8282101561032a57838290600052602060002090600302016040518060600160405290816000820154815260200160018201548152602001600282015481525050815260200190600101906102da565b50505050815260200160028201548152505081526020019060010190610288565b50505050905090565b60005b81518110156103c25760008084815260200190815260200160002060010182828151811061038157fe5b602090810291909101810151825460018181018555600094855293839020825160039092020190815591810151828401556040015160029091015501610357565b507f82aa45b3f2e54dab763d30d887917f42ea610ef707f2d22c9f8c13dda3edff8f3383836040516103f693929190610ad6565b60405180910390a17f0dc5f52079349ee37fd5e887dcc0f4f87fd42b39457373a22bd627d904b512d33360008085815260200190815260200160002060405161019c929190610ab6565b60005b81518110156104705761046882828151811061045b57fe5b60200260200101516100e8565b600101610443565b507fb14fa81a9e0940109985cca0ffab2aa902d841ae78b578e9e0c28763fce8bd6e816040516104a09190610b0c565b60405180910390a150565b60405180606001604052806000815260200160608152602001600081525090565b600082601f8301126104dd57600080fd5b81356104f06104eb82610b55565b610b2e565b9150818183526020840193506020810190508385606084028201111561051557600080fd5b60005b83811015610543578161052b888261061c565b84525060209092019160609190910190600101610518565b5050505092915050565b600082601f83011261055e57600080fd5b813561056c6104eb82610b55565b9150818183526020840193506020810190508385606084028201111561059157600080fd5b60005b8381101561054357816105a7888261061c565b84525060209092019160609190910190600101610594565b600082601f8301126105d057600080fd5b81356105de6104eb82610b55565b81815260209384019390925082018360005b8381101561054357813586016106068882610677565b84525060209283019291909101906001016105f0565b60006060828403121561062e57600080fd5b6106386060610b2e565b9050600061064684846106ca565b8252506020610657848483016106ca565b602083015250604061066b848285016106ca565b60408301525092915050565b60006060828403121561068957600080fd5b6106936060610b2e565b905060006106a184846106ca565b825250602082013567ffffffffffffffff8111156106be57600080fd5b610657848285016104cc565b60006106d682356101b0565b9392505050565b6000602082840312156106ef57600080fd5b813567ffffffffffffffff81111561070657600080fd5b610712848285016105bf565b949350505050565b60006020828403121561072c57600080fd5b813567ffffffffffffffff81111561074357600080fd5b61071284828501610677565b60006020828403121561076157600080fd5b600061071284846106ca565b6000806040838503121561078057600080fd5b600061078c85856106ca565b925050602083013567ffffffffffffffff8111156107a957600080fd5b6107b58582860161054d565b9150509250929050565b60006107cb838361094e565b505060600190565b60006107cb838361098b565b60006106d683836109e6565b6107f481610bab565b82525050565b600061080582610b88565b61080f8185610b96565b935061081a83610b76565b60005b82811015610845576108308683516107bf565b955061083b82610b76565b915060010161081d565b5093949350505050565b600061085a82610b88565b6108648185610b96565b935061086f83610b76565b60005b82811015610845576108858683516107bf565b955061089082610b76565b9150600101610872565b60006108a582610b8c565b6108af8185610b96565b93506108ba83610b7c565b60005b82811015610845576108cf86836107d3565b95506108da82610b90565b91506001016108bd565b60006108ef82610b88565b6108f98185610b96565b93508360208202850161090b85610b76565b60005b848110156109425783830388526109268383516107df565b925061093182610b76565b60209890980197915060010161090e565b50909695505050505050565b8051606083019061095f8482610a8d565b5060208201516109726020850182610a8d565b5060408201516109856040850182610a8d565b50505050565b8054606083019061099b81610bcd565b6109a58582610a8d565b505060018201546109b581610bcd565b6109c26020860182610a8d565b505060028201546109d281610bcd565b6109df6040860182610a8d565b5050505050565b805160009060608401906109fa8582610a8d565b5060208301518482036020860152610a12828261084f565b9150506040830151610a276040860182610a8d565b509392505050565b80546000906060840190610a4281610bcd565b610a4c8682610a8d565b50600184018583036020870152610a63838261089a565b92505060028401549050610a7681610bcd565b610a836040870182610a8d565b5090949350505050565b6107f4816101b0565b60408101610aa482856107eb565b818103602083015261071281846109e6565b60408101610ac482856107eb565b81810360208301526107128184610a2f565b60608101610ae482866107eb565b610af16020830185610a8d565b8181036040830152610b0381846107fa565b95945050505050565b602080825281016106d681846108e4565b602080825281016106d681846109e6565b60405181810167ffffffffffffffff81118282101715610b4d57600080fd5b604052919050565b600067ffffffffffffffff821115610b6c57600080fd5b5060209081020190565b60200190565b60009081526020902090565b5190565b5490565b60010190565b90815260200190565b6001600160a01b031690565b6000610bb682610bbc565b92915050565b6000610bb6826000610bb682610b9f565b6000610bb6610bdb836101b0565b6101b056fea265627a7a7230582096df729aef8b0d0a5f1e087647bffe76396ead26a673478e4eecafdf97687e2a6c6578706572696d656e74616cf50037";
            public StructsSampleDeploymentBase() : base(BYTECODE) { }
            public StructsSampleDeploymentBase(string byteCode) : base(byteCode) { }

        }

        public partial class SetPurchaseOrderFunction : SetPurchaseOrderFunctionBase { }

        [Function("SetPurchaseOrder")]
        public class SetPurchaseOrderFunctionBase : FunctionMessage
        {
            [Parameter("tuple", "purchaseOrder", 1)]
            public virtual PurchaseOrder PurchaseOrder { get; set; }
        }

        public partial class GetPurchaseOrder2Function : GetPurchaseOrder2FunctionBase { }

        [Function("GetPurchaseOrder2", "tuple")]
        public class GetPurchaseOrder2FunctionBase : FunctionMessage
        {

        }

        public partial class GetPurchaseOrderFunction : GetPurchaseOrderFunctionBase { }

        [Function("GetPurchaseOrder", "tuple")]
        public class GetPurchaseOrderFunctionBase : FunctionMessage
        {
            [Parameter("uint256", "id", 1)]
            public virtual BigInteger Id { get; set; }
        }

        public partial class GetPurchaseOrdersFunction : GetPurchaseOrdersFunctionBase { }

        [Function("GetPurchaseOrders", "tuple[]")]
        public class GetPurchaseOrdersFunctionBase : FunctionMessage
        {

        }

        public partial class AddLineItemsFunction : AddLineItemsFunctionBase { }

        [Function("AddLineItems")]
        public class AddLineItemsFunctionBase : FunctionMessage
        {
            [Parameter("uint256", "id", 1)]
            public virtual BigInteger Id { get; set; }
            [Parameter("tuple[]", "lineItem", 2)]
            public virtual List<LineItem> LineItem { get; set; }
        }

        public partial class SetPurchaseOrdersFunction : SetPurchaseOrdersFunctionBase { }

        [Function("SetPurchaseOrders")]
        public class SetPurchaseOrdersFunctionBase : FunctionMessage
        {
            [Parameter("tuple[]", "purchaseOrder", 1)]
            public virtual List<PurchaseOrder> PurchaseOrder { get; set; }
        }

        public partial class PurchaseOrderChangedEventDTO : PurchaseOrderChangedEventDTOBase { }

        [Event("PurchaseOrderChanged")]
        public class PurchaseOrderChangedEventDTOBase : IEventDTO
        {
            [Parameter("address", "sender", 1, false)]
            public virtual string Sender { get; set; }
            [Parameter("tuple", "purchaseOrder", 2, false)]
            public virtual PurchaseOrder PurchaseOrder { get; set; }
        }

        public partial class PurchaseOrdersChangedEventDTO : PurchaseOrdersChangedEventDTOBase { }

        [Event("PurchaseOrdersChanged")]
        public class PurchaseOrdersChangedEventDTOBase : IEventDTO
        {
            [Parameter("tuple[]", "purchaseOrder", 1, false)]
            public virtual List<PurchaseOrder> PurchaseOrder { get; set; }
        }

        public partial class LineItemsAddedEventDTO : LineItemsAddedEventDTOBase { }

        [Event("LineItemsAdded")]
        public class LineItemsAddedEventDTOBase : IEventDTO
        {
            [Parameter("address", "sender", 1, false)]
            public virtual string Sender { get; set; }
            [Parameter("uint256", "purchaseOrderId", 2, false)]
            public virtual BigInteger PurchaseOrderId { get; set; }
            [Parameter("tuple[]", "lineItem", 3, false)]
            public virtual List<LineItem> LineItem { get; set; }
        }


        public partial class GetPurchaseOrderOutputDTO : GetPurchaseOrderOutputDTOBase { }

        [FunctionOutput]
        public class GetPurchaseOrderOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("tuple", "purchaseOrder", 1)]
            public virtual PurchaseOrder PurchaseOrder { get; set; }
        }

        public partial class GetPurchaseOrdersOutputDTO : GetPurchaseOrdersOutputDTOBase { }

        [FunctionOutput]
        public class GetPurchaseOrdersOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("tuple[]", "purchaseOrder", 1)]
            public virtual List<PurchaseOrder> PurchaseOrder { get; set; }
        }

    }
}
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.XUnitEthereumClients;
using Xunit; 
 // ReSharper disable ConsiderUsingConfigureAwait  
 // ReSharper disable AsyncConverter.ConfigureAwaitHighlighting

namespace Nethereum.Contracts.IntegrationTests.Issues
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TestIssueGasAllDataOutput
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TestIssueGasAllDataOutput(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        public async Task<TransactionReceipt> WaitForReceiptAsync(Web3.Web3 web3, string transactionHash)
        {
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).ConfigureAwait(false);

            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).ConfigureAwait(false);
            }

            return receipt;
        }

        [FunctionOutput]
        public class CustomerData
        {
            [Parameter("int256", "mobile", 1)] public int mobile { get; set; }

            [Parameter("bytes32", "customerName", 2)]
            public string customerName { get; set; }

            [Parameter("bytes32", "serviceProvider", 3)]
            public string serviceProvider { get; set; }
        }

        [FunctionOutput]
        public class AllCustomerData
        {
            [Parameter("int256[]", "mobile", 1)] public List<BigInteger> mobile { get; set; }

            [Parameter("bytes32[]", "customerName", 2)]
            public List<string> customerName { get; set; }

            [Parameter("bytes32[]", "serviceProvider", 3)]
            public List<string> serviceProvider { get; set; }
        }

        [Fact]
        public async Task ShouldOutputAllData()
        {
            var abi =
                @"[{""constant"":false,""inputs"":[{""name"":""val"",""type"":""int256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""int256""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[{""name"":""customerName"",""type"":""string""},{""name"":""mobileNumber"",""type"":""int256""},{""name"":""serviceProvider"",""type"":""string""}],""name"":""addCustomer"",""outputs"":[{""name"":"""",""type"":""bool""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getAllCustomers"",""outputs"":[{""name"":""mobile"",""type"":""int256[]""},{""name"":""customerName"",""type"":""bytes32[]""},{""name"":""serviceProvider"",""type"":""bytes32[]""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[{""name"":""mobileNumber"",""type"":""int256""}],""name"":""get"",""outputs"":[{""name"":""mobile"",""type"":""int256""},{""name"":""customerName"",""type"":""bytes32""},{""name"":""serviceProvider"",""type"":""bytes32""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[{""name"":""mobileNumber"",""type"":""int256""}],""name"":""getCustomersByMobileNumber"",""outputs"":[{""name"":""mobile"",""type"":""uint256""},{""name"":""customerName"",""type"":""bytes32""},{""name"":""serviceProvider"",""type"":""bytes32""}],""payable"":false,""type"":""function""},{""inputs"":[],""type"":""constructor""}]";
            var byteCode =
                "606060405260006001556104f1806100176000396000f3606060405260e060020a60003504631df4f144811461004a5780634f5d64ce146100675780637da67ba814610121578063846719e0146102e15780638babdb681461034a575b610002565b346100025760076004350260408051918252519081900360200190f35b34610002576103746004808035906020019082018035906020019191908080601f0160208091040260200160405190810160405280939291908181526020018383808284375050604080516020604435808b0135601f81018390048302840183019094528383529799893599909860649850929650919091019350909150819084018382808284375094965050505050505060408051606081018252600060208201819052918101829052838152610448855b6020015190565b34610002576040805160208082018352600080835283518083018552818152845180840186528281528551808501875283815286518086018852848152875180870189528581528851606081018a5286815296870186905286890186905285549851610388999597949693949293919290869080591061019e5750595b9080825280602002602001820160405280156101b5575b509450856040518059106101c65750595b9080825280602002602001820160405280156101dd575b509350856040518059106101ee5750595b908082528060200260200182016040528015610205575b509250600091505b858260ff1610156104e3576002600050600060006000508460ff168154811015610002579060005260206000209001600050548152602080820192909252604090810160002081516060810183528154808252600183015494820194909452600290910154918101919091528651909250869060ff8516908110156100025760209081029091018101919091528101518451859060ff851690811015610002576020908102909101015260408101518351849060ff851690811015610002576020908102909101015260019091019061020d565b34610002576004357f61626300000000000000000000000000000000000000000000000000000000007f53330000000000000000000000000000000000000000000000000000000000005b60408051938452602084019290925282820152519081900360600190f35b3461000257600080546004358252600260208190526040909220600181015492015490919061032c565b604080519115158252519081900360200190f35b604051808060200180602001806020018481038452878181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018481038352868181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018481038252858181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f150905001965050505050505060405180910390f35b60208201526104568361011a565b604082015260008054600181018083558281838015829011610499576000838152602090206104999181019083015b808211156104df5760008155600101610485565b5050506000928352506020808320909101869055948152600280865260409182902083518155958301516001808801919091559290910151940193909355509092915050565b5090565b50929791965094509250505056";

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            var transactionHash =
                await web3.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress,
                    new HexBigInteger(1999990)).ConfigureAwait(false);

            var receipt = await WaitForReceiptAsync(web3, transactionHash).ConfigureAwait(false);

            var contractAddress = receipt.ContractAddress;

            var contract = web3.Eth.GetContract(abi, contractAddress);

            var addCustomerFunction = contract.GetFunction("addCustomer");

            var getCustomersByMobileNumberFunction = contract.GetFunction("getCustomersByMobileNumber");

            var resultHash = await addCustomerFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                new HexBigInteger(0), "Mahesh", 111, "Airtel").ConfigureAwait(false);

            receipt = await WaitForReceiptAsync(web3, resultHash).ConfigureAwait(false);

            var results = await getCustomersByMobileNumberFunction.CallDeserializingToObjectAsync<CustomerData>(111).ConfigureAwait(false);

            Assert.Equal("Mahesh", results.customerName);

            var getAllCustomers = contract.GetFunction("getAllCustomers");
            var results2 = await getAllCustomers.CallDeserializingToObjectAsync<AllCustomerData>().ConfigureAwait(false);
        }
    }
}﻿using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.XUnitEthereumClients;
using Xunit;
using Nethereum.ABI.FunctionEncoding;
using Newtonsoft.Json.Linq;


namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TestJsonAnonymousReturn
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TestJsonAnonymousReturn(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldReturnJsonWithEncodedDefaultNameWithOrderAndTypeIfNotIncluded()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentReceipt = await web3.Eth.GetContractDeploymentHandler<SimpleOwner2Deployment>()
                .SendRequestAndWaitForReceiptAsync().ConfigureAwait(false);
            var contract = web3.Eth.GetContract("[{\"inputs\":[],\"name\":\"getOwner\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]", deploymentReceipt.ContractAddress);
            var functionGetOwner = contract.GetFunction("getOwner");
            var result = await functionGetOwner.CallDecodingToDefaultAsync(EthereumClientIntegrationFixture.AccountAddress, null, null, null, null);
            var jObjectResult = result.ConvertToJObject();
            var expectedJson = JObject.Parse("{\r\n  \"param_1_address\": \"0x12890D2cce102216644c59daE5baed380d84830c\",\r\n  \"param_2_uint256\": \"1\"\r\n}");
            Assert.True(JObject.DeepEquals(expectedJson, jObjectResult));
        }


        /*contract SimpleOwner2 {

            function getOwner() public view returns(address, uint) {
                return (msg.sender, 1);
            }
        }*/
        public class SimpleOwner2Deployment : ContractDeploymentMessage
        {
            public static string BYTECODE = "6080604052348015600f57600080fd5b50607d80601d6000396000f3fe6080604052348015600f57600080fd5b506004361060285760003560e01c8063893d20e814602d575b600080fd5b604080513381526001602082015281519081900390910190f3fea264697066735822122047ba7eb98d57a3784b68591a5cbfccdb8c58fe369ccb067bd70b18da89e52c0964736f6c63430008110033";
            public SimpleOwner2Deployment() : base(BYTECODE) { }
            public SimpleOwner2Deployment(string byteCode) : base(byteCode) { }

        }

        public partial class GetOwnerFunction : GetOwnerFunctionBase { }

        [Function("getOwner", typeof(GetOwnerOutputDTO))]
        public class GetOwnerFunctionBase : FunctionMessage
        {

        }

        public partial class GetOwnerOutputDTO : GetOwnerOutputDTOBase { }

        [FunctionOutput]
        public class GetOwnerOutputDTOBase : IFunctionOutputDTO
        {
            [Parameter("address", "", 1)]
            public virtual string ReturnValue1 { get; set; }
            [Parameter("uint256", "", 2)]
            public virtual BigInteger ReturnValue2 { get; set; }
        }
    }
}using System.Collections.Generic;
using System.Threading;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Fee1559Suggestions;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Xunit;
// ReSharper disable ConsiderUsingConfigureAwait

namespace Nethereum.Signer.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TimePreferenceSuggestionStrategy1559Tests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TimePreferenceSuggestionStrategy1559Tests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }
        
        [Fact]
        public async void ShouldBeAbleToCalculateHistoryAndSend1000sOfTransactions()
        {
            if (_ethereumClientIntegrationFixture.EthereumClient == EthereumClient.Geth)
            {
                var receiveAddress = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";
                var listTransactionHashes = new List<string>();
                var web3 = _ethereumClientIntegrationFixture.GetWeb3();
                web3.TransactionReceiptPolling.SetPollingRetryIntervalInMilliseconds(2000);
#if NETCOREAPP3_1_OR_GREATER || NET50
                EthECKey.SignRecoverable = true;
#endif
                var feeStrategy = new TimePreferenceFeeSuggestionStrategy(web3.Client);
                for (var x = 0; x < 10; x++)
                {
                    Thread.Sleep(500);
                    var fee = await feeStrategy.SuggestFeeAsync().ConfigureAwait(false);
                    for (int i = 0; i < 50; i++)
                    {
                        var encoded = await web3.TransactionManager.SendTransactionAsync(
                            new TransactionInput()
                            {
                                Type = new HexBigInteger(2),
                                From = web3.TransactionManager.Account.Address,
                                MaxFeePerGas = new HexBigInteger(fee.MaxFeePerGas.Value),
                                MaxPriorityFeePerGas = new HexBigInteger(fee.MaxPriorityFeePerGas.Value),
                                To = receiveAddress,
                                Value = new HexBigInteger(10)
                            }).ConfigureAwait(false);
                        listTransactionHashes.Add(encoded);
                   }
                }

                foreach (var tx in listTransactionHashes)
                {   
                    
                    var receipt = await web3.TransactionReceiptPolling.PollForReceiptAsync(tx);
                    Assert.True(receipt.Succeeded());
                }
            }
        }
    }
}using Nethereum.Hex.HexTypes;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.StandardTokenEIP20;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.HdWallet.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TokenTransferTest
    {
        public const string Words =
            "ripple scissors kick mammal hire column oak again sun offer wealth tomorrow wagon turn fatal";

        public const string Password = "TREZOR";

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TokenTransferTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldBeAbleTransferTokensUsingTheHdWallet()
        {
            var wallet = new Wallet(Words, Password);
            var account = wallet.GetAccount(0, EthereumClientIntegrationFixture.ChainId);
            
            var web3 = new Web3.Web3(account, _ethereumClientIntegrationFixture.GetWeb3().Client);

            ulong totalSupply = 1000000;
            var contractByteCode =
                "0x6060604052341561000f57600080fd5b604051602080610711833981016040528080519150505b60018054600160a060020a03191633600160a060020a0390811691909117918290556000838155911681526002602052604090208190555b505b6106a28061006f6000396000f300606060405236156100a15763ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166306fdde0381146100a6578063095ea7b31461013157806318160ddd1461016757806323b872dd1461018c578063313ce567146101c857806370a08231146101f15780638da5cb5b1461022257806395d89b4114610251578063a9059cbb146102dc578063dd62ed3e14610312575b600080fd5b34156100b157600080fd5b6100b9610349565b60405160208082528190810183818151815260200191508051906020019080838360005b838110156100f65780820151818401525b6020016100dd565b50505050905090810190601f1680156101235780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b341561013c57600080fd5b610153600160a060020a0360043516602435610380565b604051901515815260200160405180910390f35b341561017257600080fd5b61017a6103ed565b60405190815260200160405180910390f35b341561019757600080fd5b610153600160a060020a03600435811690602435166044356103f4565b604051901515815260200160405180910390f35b34156101d357600080fd5b6101db610510565b60405160ff909116815260200160405180910390f35b34156101fc57600080fd5b61017a600160a060020a0360043516610515565b60405190815260200160405180910390f35b341561022d57600080fd5b610235610534565b604051600160a060020a03909116815260200160405180910390f35b341561025c57600080fd5b6100b9610543565b60405160208082528190810183818151815260200191508051906020019080838360005b838110156100f65780820151818401525b6020016100dd565b50505050905090810190601f1680156101235780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34156102e757600080fd5b610153600160a060020a036004351660243561057a565b604051901515815260200160405180910390f35b341561031d57600080fd5b61017a600160a060020a0360043581169060243516610649565b60405190815260200160405180910390f35b60408051908101604052601a81527f4578616d706c6520466978656420537570706c7920546f6b656e000000000000602082015281565b600160a060020a03338116600081815260036020908152604080832094871680845294909152808220859055909291907f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b9259085905190815260200160405180910390a35060015b92915050565b6000545b90565b600160a060020a0383166000908152600260205260408120548290108015906104445750600160a060020a0380851660009081526003602090815260408083203390941683529290522054829010155b80156104505750600082115b80156104755750600160a060020a038316600090815260026020526040902054828101115b1561050457600160a060020a0380851660008181526002602081815260408084208054899003905560038252808420338716855282528084208054899003905594881680845291905290839020805486019055917fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef9085905190815260200160405180910390a3506001610508565b5060005b5b9392505050565b601281565b600160a060020a0381166000908152600260205260409020545b919050565b600154600160a060020a031681565b60408051908101604052600581527f4649584544000000000000000000000000000000000000000000000000000000602082015281565b600160a060020a0333166000908152600260205260408120548290108015906105a35750600082115b80156105c85750600160a060020a038316600090815260026020526040902054828101115b1561063a57600160a060020a033381166000818152600260205260408082208054879003905592861680825290839020805486019055917fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef9085905190815260200160405180910390a35060016103e7565b5060006103e7565b5b92915050565b600160a060020a038083166000908152600360209081526040808320938516835292905220545b929150505600a165627a7a72305820ec01add6c7f9d88976180c397e2a5b2e9f8fc1f95f5abb00e2a4c9dbf7bcfaf20029";
            var abi =
                "[{\"constant\":true,\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_spender\",\"type\":\"address\"},{\"name\":\"_amount\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"totalSupply\",\"outputs\":[{\"name\":\"totalSupply\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_amount\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"owner\",\"outputs\":[{\"name\":\"\",\"type\":\"address\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_amount\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"},{\"name\":\"_spender\",\"type\":\"address\"}],\"name\":\"allowance\",\"outputs\":[{\"name\":\"remaining\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"inputs\":[{\"name\":\"totalSupply\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"constructor\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"_from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"_to\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"_owner\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"_spender\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"}]";

            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, contractByteCode,
                account.Address, new HexBigInteger(3000000), null, totalSupply).ConfigureAwait(false);

            var standarErc20Service = web3.Eth.ERC20.GetContractService(receipt.ContractAddress);

            var pollingService = new TransactionReceiptPollingService(web3.TransactionManager);

            var transactionHash = await standarErc20Service.TransferRequestAsync(
                "0x98f5438cDE3F0Ff6E11aE47236e93481899d1C47", 10).ConfigureAwait(false);

            var receiptSend = await pollingService.PollForReceiptAsync(transactionHash).ConfigureAwait(false);

            var balance =
                await standarErc20Service.BalanceOfQueryAsync("0x98f5438cDE3F0Ff6E11aE47236e93481899d1C47").ConfigureAwait(false);

            Assert.Equal(10, balance);
        }
    }
}﻿using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TransactionInterceptorTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TransactionInterceptorTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task ShouldIntereceptAndSendRawTransaction()
        {
            var privateKey = EthereumClientIntegrationFixture.AccountPrivateKey;
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;
            var receiveAddress = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var transactionInterceptor = new AccountTransactionSigningInterceptor(privateKey, EthereumClientIntegrationFixture.ChainId ,web3.Client);
            web3.Client.OverridingRequestInterceptor = transactionInterceptor;

            var txId = await web3.Eth.Transactions.SendTransaction.SendRequestAsync(
                new TransactionInput {From = senderAddress, To = receiveAddress, Value = new HexBigInteger(10)}).ConfigureAwait(false);

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId).ConfigureAwait(false);
            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId).ConfigureAwait(false);
            }
            Assert.Equal(txId, receipt.TransactionHash);
        }

        [Fact]
        public async Task ShouldIntereceptContractDeploymentAndContractTrasanctionWithRawTransaction()
        {
            var contractByteCode =
                "0x6060604052604060405190810160405280600a81526020017f4d756c7469706c6965720000000000000000000000000000000000000000000081526020015060016000509080519060200190828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061008c57805160ff19168380011785556100bd565b828001600101855582156100bd579182015b828111156100bc57825182600050559160200191906001019061009e565b5b5090506100e891906100ca565b808211156100e457600081815060009055506001016100ca565b5090565b5050604051602080610303833981016040528080519060200190919050505b806000600050819055505b506101e2806101216000396000f360606040526000357c01000000000000000000000000000000000000000000000000000000009004806340490a901461004f57806375d0c0dc14610072578063c6888fa1146100ed5761004d565b005b61005c6004805050610119565b6040518082815260200191505060405180910390f35b61007f6004805050610141565b60405180806020018281038252838181518152602001915080519060200190808383829060006004602084601f0104600f02600301f150905090810190601f1680156100df5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b610103600480803590602001909190505061012b565b6040518082815260200191505060405180910390f35b60006000600050549050610128565b90565b60006000600050548202905061013c565b919050565b60016000508054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156101da5780601f106101af576101008083540402835291602001916101da565b820191906000526020600020905b8154815290600101906020018083116101bd57829003601f168201915b50505050508156";

            var abi =
                @"[{""constant"":true,""inputs"":[],""name"":""getMultiplier"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""contractName"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""}]";


            var privateKey = EthereumClientIntegrationFixture.AccountPrivateKey;
            var senderAddress = EthereumClientIntegrationFixture.AccountAddress;

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var transactionInterceptor = new AccountTransactionSigningInterceptor(privateKey, EthereumClientIntegrationFixture.ChainId, web3.Client);
            web3.Client.OverridingRequestInterceptor = transactionInterceptor;

            var txId = await web3.Eth.DeployContract.SendRequestAsync(abi, contractByteCode, senderAddress,
                new HexBigInteger(900000), 7).ConfigureAwait(false);

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId).ConfigureAwait(false);
            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId).ConfigureAwait(false);
            }

            Assert.Equal(txId, receipt.TransactionHash);
            Assert.NotNull(receipt.ContractAddress);

            var contract = web3.Eth.GetContract(abi, receipt.ContractAddress);
            var multiplyFunction = contract.GetFunction("multiply");

            txId = await multiplyFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000),
                new HexBigInteger(0), 69).ConfigureAwait(false);

            receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId).ConfigureAwait(false);
            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId).ConfigureAwait(false);
            }

            Assert.Equal(txId, receipt.TransactionHash);
        }
    }
}﻿using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.RPC.TransactionManagers;
using Nethereum.Signer;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TransactionRawRecovery
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TransactionRawRecovery(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldRecoverRawTransactionFromRPCTransactionAndAccountSender()
        {

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var toAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";
           
            var transactionManager = web3.TransactionManager;
            var fromAddress = transactionManager?.Account?.Address;

            //Sending transaction
            var transactionInput = EtherTransferTransactionInputBuilder.CreateTransactionInput(fromAddress, toAddress, 1.11m, 2);
            //Raw transaction signed
            var rawTransaction = await transactionManager.SignTransactionAsync(transactionInput).ConfigureAwait(false);
            var txnHash = await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(rawTransaction).ConfigureAwait(false);
            //Getting the transaction from the chain
            var transactionRpc = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txnHash).ConfigureAwait(false);
            
            //Using the transanction from RPC to build a txn for signing / signed
            var transaction = TransactionFactory.CreateLegacyTransaction(transactionRpc.To, transactionRpc.Gas, transactionRpc.GasPrice, transactionRpc.Value, transactionRpc.Input, transactionRpc.Nonce,
                transactionRpc.R, transactionRpc.S, transactionRpc.V);
            
            //Get the raw signed rlp recovered
            var rawTransactionRecovered = transaction.GetRLPEncoded().ToHex();
            
            //Get the account sender recovered
            var accountSenderRecovered = string.Empty;
            if (transaction is LegacyTransactionChainId)
            {
                var txnChainId = transaction as LegacyTransactionChainId;
                accountSenderRecovered = EthECKey.RecoverFromSignature(transaction.Signature.ToEthECDSASignature(), transaction.RawHash, txnChainId.GetChainIdAsBigInteger()).GetPublicAddress();
            }
            else
            {
                accountSenderRecovered = EthECKey.RecoverFromSignature(transaction.Signature.ToEthECDSASignature(), transaction.RawHash).GetPublicAddress();
            }

            Assert.Equal(rawTransaction, rawTransactionRecovered);
            Assert.Equal(web3.TransactionManager.Account.Address, accountSenderRecovered);
        }
    }
}using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Fee1559Suggestions;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Signer.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TransactionSigningTests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TransactionSigningTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }
 
        [Fact]
        public async Task<bool> ShouldSignAndSendRawTransaction()
        {
            var receiveAddress = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

           
            
            var feeStrategy = new SimpleFeeSuggestionStrategy(web3.Client);
         
            var fee = await feeStrategy.SuggestFeeAsync().ConfigureAwait(false);
            var encoded = await web3.TransactionManager.SignTransactionAsync(
                new TransactionInput()
                {
                    Type = new HexBigInteger(2),
                    From = web3.TransactionManager.Account.Address,
                    MaxFeePerGas = new HexBigInteger(fee.MaxFeePerGas.Value),
                    MaxPriorityFeePerGas = new HexBigInteger(fee.MaxPriorityFeePerGas.Value),
                    Nonce = await web3.Eth.TransactionManager.Account.NonceService.GetNextNonceAsync().ConfigureAwait(false),
                    To = receiveAddress,
                    Value = new HexBigInteger(10)
                }).ConfigureAwait(false);
            
            Assert.True(TransactionVerificationAndRecovery.VerifyTransaction(encoded));

           
            Assert.Equal(web3.TransactionManager.Account.Address.EnsureHexPrefix().ToLower(),
                TransactionVerificationAndRecovery.GetSenderAddress(encoded).EnsureHexPrefix().ToLower());

            var txId = await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + encoded).ConfigureAwait(false);
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId).ConfigureAwait(false);
            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId).ConfigureAwait(false);
            }

            Assert.Equal(txId, receipt.TransactionHash);
            return true;
        }

      
    }
}﻿using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Chain;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.TransactionTypes;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TransactionTests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TransactionTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldReceiveTheTransactionHash()
        {

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var toAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";
            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, 1.11m, gasPriceGwei: 2).ConfigureAwait(false);
            var tran = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(receipt.TransactionHash);
            Assert.NotNull(tran.TransactionHash);
            var blockWithTransactions =
                await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(receipt.BlockNumber);
            foreach (var transaction in blockWithTransactions.Transactions)
            {
                Assert.NotNull(transaction.TransactionHash);
            }
        }

        [Fact]
        public async void ShouldReceiveTheTransactionByHashPendingAndNullValuesDependingOnTrasactionType()
        {

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var toAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";
            var transactionHash = await web3.Eth.GetEtherTransferService()
                .TransferEtherAsync(toAddress, 1.11m, gasPriceGwei: 2).ConfigureAwait(false);
            var tran = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash);
            Assert.NotNull(tran.TransactionHash);
            Assert.Null(tran.MaxFeePerGas);
            Assert.Null(tran.MaxPriorityFeePerGas);
            Assert.NotNull(tran.GasPrice);

            var transactionHash2 = await web3.Eth.GetEtherTransferService()
                .TransferEtherAsync(toAddress, 1.11m, maxFeePerGas: Web3.Web3.Convert.ToWei(2, Util.UnitConversion.EthUnit.Gwei), maxPriorityFee: Web3.Web3.Convert.ToWei(2, Util.UnitConversion.EthUnit.Gwei)).ConfigureAwait(false);
            var tran2 = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash2);
            Assert.NotNull(tran2.TransactionHash);
            Assert.NotNull(tran2.MaxFeePerGas);
            Assert.NotNull(tran2.MaxPriorityFeePerGas);
            Assert.NotNull(tran2.GasPrice);

        }


        [Fact]
        public async void ShouldSendTrasactionBasedOnChainFeature()
        {

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            ChainFeaturesService.Current.UpsertChainFeature(
                new ChainFeature()
                {
                    ChainName = "Nethereum Test Chain",
                    ChainId = 444444444500,
                    SupportEIP1559 = false
                });

            var toAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";
            var tranHash = await web3.Eth.TransactionManager.SendTransactionAsync(new TransactionInput()
            {
                From = EthereumClientIntegrationFixture.AccountAddress,
                To = toAddress,
                Value = new HexBigInteger(100),
            }
            );
                
            var tran = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(tranHash);
            Assert.NotNull(tran.TransactionHash);
            Assert.Null(tran.MaxFeePerGas);
            Assert.Null(tran.MaxPriorityFeePerGas);
            Assert.NotNull(tran.GasPrice);

            ChainFeaturesService.Current.UpsertChainFeature(
                new ChainFeature()
                {
                    ChainName = "Nethereum Test Chain",
                    ChainId = 444444444500,
                    SupportEIP1559 = true
                });

            var tranHash2 = await web3.Eth.TransactionManager.SendTransactionAsync(new TransactionInput()
            {
                From = EthereumClientIntegrationFixture.AccountAddress,
                To = toAddress,
                Value = new HexBigInteger(100),
            }
            );

            var tran2 = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(tranHash2);
            Assert.NotNull(tran2.TransactionHash);
            Assert.NotNull(tran2.MaxFeePerGas);
            Assert.NotNull(tran2.MaxPriorityFeePerGas);
            Assert.NotNull(tran2.GasPrice);

            ChainFeaturesService.Current.TryRemoveChainFeature(444444444500);
            //Should default to 1559 when not feature is set

            var tranHash3 = await web3.Eth.TransactionManager.SendTransactionAsync(new TransactionInput()
            {
                From = EthereumClientIntegrationFixture.AccountAddress,
                To = toAddress,
                Value = new HexBigInteger(100),
            }
           );

            var tran3 = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(tranHash3);
            Assert.NotNull(tran3.TransactionHash);
            Assert.NotNull(tran3.MaxFeePerGas);
            Assert.NotNull(tran3.MaxPriorityFeePerGas);
            Assert.NotNull(tran3.GasPrice);
        }

        [Fact]
        public async void ShouldGetTransactionByHash()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var txnType2 = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync("0xe7bab1a12b9234a27a0f53f71d19bc0595f1ea2c8148f5d45edac76a4566e15b");
            var txnLegacy = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync("0x8751032c189f44478b13ca77834b6af3567ec3e014069450f17209ed0fd1a3c1");
            Assert.True(txnType2.Type.ToTransactionType() == TransactionType.EIP1559);
            Assert.True(txnLegacy.Type.ToTransactionType() == TransactionType.Legacy);

            Assert.True(txnType2.Is1559());
            Assert.True(txnLegacy.IsLegacy());
        }

    }
}﻿using System.Numerics;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using Nethereum.XUnitEthereumClients;
using Xunit;

namespace Nethereum.Accounts.IntegrationTests
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class TransferEtherTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public TransferEtherTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldTransferEtherWithGasPrice()
        {
            
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            
            var toAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe";
            var balanceOriginal = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            var balanceOriginalEther = Web3.Web3.Convert.FromWei(balanceOriginal.Value);
            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, 1.11m, gasPriceGwei:2).ConfigureAwait(false);

            var balance = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            Assert.Equal(balanceOriginalEther + 1.11m, Web3.Web3.Convert.FromWei(balance));
        }

        [Fact]
        public async void ShouldTransferEther()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var toAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BA1";
            var balanceOriginal = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            var balanceOriginalEther = Web3.Web3.Convert.FromWei(balanceOriginal.Value);

            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, 1.11m).ConfigureAwait(false);

            var balance = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            Assert.Equal(balanceOriginalEther + 1.11m, Web3.Web3.Convert.FromWei(balance));
        }

        [Fact]
        public async void ShouldTransferEtherWithGasPriceAndGasAmount()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var toAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BA1";
            var balanceOriginal = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            var balanceOriginalEther = Web3.Web3.Convert.FromWei(balanceOriginal.Value);

            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, 1.11m, gasPriceGwei: 2, new BigInteger(25000)).ConfigureAwait(false);

            var balance = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            Assert.Equal(balanceOriginalEther + 1.11m, Web3.Web3.Convert.FromWei(balance));
        }

        [Fact]
        public async void ShouldTransferEtherEstimatingAmount()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var toAddress = "0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BA1";
            var balanceOriginal = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            var balanceOriginalEther = Web3.Web3.Convert.FromWei(balanceOriginal.Value);
            var transferService = web3.Eth.GetEtherTransferService();
            var estimate = await transferService.EstimateGasAsync(toAddress, 1.11m).ConfigureAwait(false);
            var receipt = await transferService
                .TransferEtherAndWaitForReceiptAsync(toAddress, 1.11m, gasPriceGwei: 2, estimate).ConfigureAwait(false);

            var balance = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            Assert.Equal(balanceOriginalEther + 1.11m, Web3.Web3.Convert.FromWei(balance));
        }

        [Fact]
        public async void ShouldTransferWholeBalanceInEther()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var privateKey = EthECKey.GenerateKey();
            var newAccount = new Account(privateKey.GetPrivateKey(), EthereumClientIntegrationFixture.ChainId);
            var toAddress = newAccount.Address;
            var transferService = web3.Eth.GetEtherTransferService();

            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, 0.1m, gasPriceGwei: 2).ConfigureAwait(false);

            var balance = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            Assert.Equal(0.1m, Web3.Web3.Convert.FromWei(balance));

            var totalAmountBack =
                await transferService.CalculateTotalAmountToTransferWholeBalanceInEtherAsync(toAddress, 2m).ConfigureAwait(false);

            var web32 = new Web3.Web3(newAccount, web3.Client);
            var receiptBack = await web32.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(EthereumClientIntegrationFixture.AccountAddress, totalAmountBack, gasPriceGwei: 2).ConfigureAwait(false);

            var balanceFrom = await web32.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            Assert.Equal(0, Web3.Web3.Convert.FromWei(balanceFrom));
        }

        [Fact]
        public async void ShouldTransferWholeBalanceInEtherEIP1599()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var privateKey = EthECKey.GenerateKey();
            var newAccount = new Account(privateKey.GetPrivateKey(), EthereumClientIntegrationFixture.ChainId);
            var toAddress = newAccount.Address;
            var transferService = web3.Eth.GetEtherTransferService();

            var fee = await transferService.SuggestFeeToTransferWholeBalanceInEtherAsync().ConfigureAwait(false);
            var receipt = await transferService
                .TransferEtherAndWaitForReceiptAsync(toAddress, 0.1m, maxPriorityFee: fee.MaxPriorityFeePerGas.Value, maxFeePerGas: fee.MaxFeePerGas.Value).ConfigureAwait(false);

            var balance = await web3.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            Assert.Equal(0.1m, Web3.Web3.Convert.FromWei(balance));


            var web32 = new Web3.Web3(newAccount, web3.Client);

            var feeWhole =
                  await web32.Eth.GetEtherTransferService().SuggestFeeToTransferWholeBalanceInEtherAsync().ConfigureAwait(false);
            
            var amount = await web32.Eth.GetEtherTransferService()
                .CalculateTotalAmountToTransferWholeBalanceInEtherAsync(toAddress, maxFeePerGas: feeWhole.MaxFeePerGas.Value).ConfigureAwait(false);
            
            var receiptBack = await web32.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(EthereumClientIntegrationFixture.AccountAddress, amount, feeWhole.MaxPriorityFeePerGas.Value, feeWhole.MaxFeePerGas.Value).ConfigureAwait(false);

            var balanceFrom = await web32.Eth.GetBalance.SendRequestAsync(toAddress).ConfigureAwait(false);
            Assert.Equal(0, Web3.Web3.Convert.FromWei(balanceFrom));
        }
    }
}