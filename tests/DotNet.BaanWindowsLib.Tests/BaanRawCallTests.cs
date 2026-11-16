using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Wangcaisoft.DotNet.BaanWindowsLib;
using Xunit;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Tests
{
    /// <summary>
    /// Covers the original (raw) call style:
    /// <c>b.ParseExecFunction("dll", "func(\"arg1\", \"arg2\")")</c>.
    /// It must behave exactly like the chained style, only with the call text passed verbatim.
    /// </summary>
    public class BaanRawCallTests
    {
        [Fact]
        public void ParseExecFunction_PassesDllAndCallVerbatim()
        {
            var fake = new FakeBaan { EchoMethodAsReturnValue = true };

            using (var b = new Baan("Baan.Application.erpln", fake))
            {
                var result = b.ParseExecFunction(
                    "odll_name",
                    "some.function.name(\"with\", \"a\", \"few\", \"arguments\")");

                Assert.Equal("odll_name", fake.LastDllName);
                Assert.Equal(
                    "some.function.name(\"with\", \"a\", \"few\", \"arguments\")",
                    fake.LastMethod);
                Assert.Equal(fake.LastMethod, result);
            }
        }

        [Fact]
        public void ParseExecFunction_OnDynamicVariable_IsNotInterceptedByChain()
        {
            var fake = new FakeBaan { EchoMethodAsReturnValue = true };
            dynamic b = new Baan("Baan.Application.erpln", fake);

            // ParseExecFunction is a real member, so it must win over DynamicObject.TryGetMember;
            // otherwise the DLL name would wrongly be "ParseExecFunction".
            var result = (string)b.ParseExecFunction("odll_name", "fn(1)");

            Assert.Equal("odll_name", fake.LastDllName);
            Assert.Equal("fn(1)", fake.LastMethod);
            Assert.Equal("fn(1)", result);

            b.Close();
        }

        [Fact]
        public void ParseExecFunction_ErrorMinus1_ThrowsUnknownDllException()
        {
            var fake = new FakeBaan { Error = -1 };

            using (var b = new Baan("Baan.Application.erpln", fake))
            {
                Assert.Throws<UnknownDllException>(
                    () => b.ParseExecFunction("unknown_dll", "fn()"));
            }
        }

        [Fact]
        public void ParseExecFunction_ErrorMinus2_ThrowsUnknownMethodException()
        {
            var fake = new FakeBaan { Error = -2 };

            using (var b = new Baan("Baan.Application.erpln", fake))
            {
                Assert.Throws<UnknownMethodException>(
                    () => b.ParseExecFunction("odll", "unknown.method()"));
            }
        }

        [Fact]
        public void ParseExecFunction_ComFailure_IsWrappedWithServerMessage()
        {
            const int rpcCallFailed = unchecked((int)0x800706BA);

            var fake = new FakeBaan
            {
                ReturnValue = "server says no",
                ParseExecException = new COMException("boom", rpcCallFailed)
            };

            using (var b = new Baan("Baan.Application.erpln", fake))
            {
                var ex = Assert.Throws<BaanComException>(
                    () => b.ParseExecFunction("odll", "fn(\"x\")"));

                Assert.Equal("odll", ex.DllName);
                Assert.Equal("fn(\"x\")", ex.Method);
                Assert.Equal("server says no", ex.ServerMessage);
                Assert.Equal(rpcCallFailed, ex.HResult);
                Assert.IsType<COMException>(ex.InnerException);
            }
        }

        [Fact]
        public void ParseExecFunction_ThrowOnErrorResult_ThrowsBaanResultException()
        {
            var fake = new FakeBaan { ReturnValue = "1|Error" };

            using (var b = new Baan("Baan.Application.erpln", fake))
            {
                b.ThrowOnErrorResult = true;

                var ex = Assert.Throws<BaanResultException>(
                    () => b.ParseExecFunction("odll", "fn()"));

                Assert.Equal(1, ex.Code);
                Assert.Equal("Error", ex.Message);
            }
        }

        [Fact]
        public void ParseExecFunction_ThrowOnErrorResult_Zero_DoesNotThrow()
        {
            var fake = new FakeBaan { ReturnValue = "0|Success" };

            using (var b = new Baan("Baan.Application.erpln", fake))
            {
                b.ThrowOnErrorResult = true;

                var result = b.ParseExecFunction("odll", "fn()");
                Assert.Equal("0|Success", result);
            }
        }

        [Theory]
        [InlineData(null, "fn()")]
        [InlineData("", "fn()")]
        [InlineData("odll", null)]
        [InlineData("odll", "")]
        public void ParseExecFunction_NullOrEmptyArguments_Throw(string dllName, string functionCall)
        {
            using (var b = new Baan("Baan.Application.erpln", new FakeBaan()))
            {
                Assert.Throws<ArgumentException>(
                    () => b.ParseExecFunction(dllName, functionCall));
            }
        }

        [Fact]
        public void ParseExecFunction_AfterClose_ThrowsObjectDisposedException()
        {
            var b = new Baan("Baan.Application.erpln", new FakeBaan());
            b.Close();

            Assert.Throws<ObjectDisposedException>(() => b.ParseExecFunction("odll", "fn()"));
        }

        [Fact]
        public async Task ParseExecFunction_ConcurrentCalls_AreSerialized()
        {
            var fake = new FakeBaan { EchoMethodAsReturnValue = true };
            var b = new Baan("Baan.Application.erpln", fake);

            var results = new ConcurrentBag<string>();
            var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
            {
                // Each call must read back exactly its own method string (no interleaved reads).
                results.Add((string)b.ParseExecFunction("odll", "fn(" + i + ")"));
            })).ToArray();

            await Task.WhenAll(tasks);

            for (var i = 0; i < 50; i++)
            {
                Assert.Contains("fn(" + i + ")", results);
            }

            b.Close();
        }
    }
}
