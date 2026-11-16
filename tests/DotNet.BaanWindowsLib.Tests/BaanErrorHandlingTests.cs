using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Wangcaisoft.DotNet.BaanWindowsLib;
using Xunit;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Tests
{
    public class BaanErrorHandlingTests
    {
        // Well-known RPC failure HResults mirrored from BaanHelper / BaanResilienceHelper.
        private const int RpcServerUnavailable = -2147023174; // 0x800706BA
        private const int RpcCallFailed = -2147023170;        // 0x800706BE
        private const int RpcDisconnected = -2147417848;      // 0x80010108

        [Fact]
        public void ComException_IsWrappedAsBaanComException_WithFullContext()
        {
            var comEx = new COMException("RPC server unavailable", RpcServerUnavailable);
            var fake = new FakeBaan { ReturnValue = "SERVER_SIDE_DETAIL", ParseExecException = comEx };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                var ex = Assert.Throws<BaanComException>(() => b.odll.process("x"));

                Assert.Equal("odll", ex.DllName);
                Assert.Equal("process(\"x\")", ex.Method);
                Assert.Equal("SERVER_SIDE_DETAIL", ex.ServerMessage);
                Assert.Equal(RpcServerUnavailable, ex.HResult);
                Assert.Same(comEx, ex.InnerException);
                Assert.Contains("process(\"x\")", ex.Message);
            }
        }

        [Fact]
        public void BaanComException_FallsBackToReturnCall_WhenReturnValueEmpty()
        {
            var comEx = new COMException("disconnected", RpcDisconnected);
            var fake = new FakeBaan { ReturnValue = "", ReturnCall = "RC_DETAIL", ParseExecException = comEx };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                var ex = Assert.Throws<BaanComException>(() => b.odll.process("x"));

                Assert.Equal("RC_DETAIL", ex.ServerMessage);
                Assert.Equal(RpcDisconnected, ex.HResult);
            }
        }

        [Fact]
        public void UnknownDllException_IsNotWrapped()
        {
            var fake = new FakeBaan { Error = -1 };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                Assert.Throws<UnknownDllException>(() => b.unknowndll.foo());
            }
        }

        [Fact]
        public void UnknownMethodException_IsNotWrapped()
        {
            var fake = new FakeBaan { Error = -2 };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                Assert.Throws<UnknownMethodException>(() => b.dll.unknown_method());
            }
        }

        [Fact]
        public void Close_DoesNotThrow_WhenQuitThrows_AndIsIdempotent()
        {
            var fake = new FakeBaan { ThrowOnQuit = true };
            var b = new Baan("Baan.Application.erpln", fake);

            var ex = Record.Exception(() => b.Close());

            Assert.Null(ex);
            Assert.True(fake.QuitCalled);

            // Second Close must be a no-op and not throw.
            var ex2 = Record.Exception(() => b.Close());
            Assert.Null(ex2);
        }

        [Fact]
        public async Task ConcurrentCalls_AreSerialized_WithoutCrossTalk()
        {
            var fake = new FakeBaan { EchoMethodAsReturnValue = true };
            dynamic b = new Baan("Baan.Application.erpln", fake);

            var results = new ConcurrentBag<string>();
            var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
            {
                var r = b.dll.process(i.ToString());
                results.Add((string)r);
            })).ToArray();

            await Task.WhenAll(tasks);

            // Every call must have received exactly its own method string (no interleaved reads).
            for (var i = 0; i < 50; i++)
            {
                Assert.Contains($"process(\"{i}\")", results);
            }

            b.Close();
        }

        [Fact]
        public async Task Close_WhileCallInFlight_CompletesWithoutDeadlock()
        {
            var fake = new FakeBaan { EchoMethodAsReturnValue = true };
            dynamic b = new Baan("Baan.Application.erpln", fake);

            var callTask = Task.Run(() =>
            {
                // Hold the internal lock briefly so Close must wait for the in-flight call.
                fake.SlowParseExecMs = 300;
                var r = b.dll.process("x");
                Assert.Equal("process(\"x\")", (string)r);
            });

            // Let the call enter and hold the internal lock before closing concurrently.
            await Task.Delay(50);
            b.Close();

            await callTask;

            Assert.True(fake.QuitCalled);
        }

        [Fact]
        public void IsRpcFailure_DetectsKnownCodes()
        {
            Assert.True(BaanResilienceHelper.IsRpcFailure(new COMException("x", RpcServerUnavailable)));
            Assert.True(BaanResilienceHelper.IsRpcFailure(new COMException("x", RpcCallFailed)));
            Assert.True(BaanResilienceHelper.IsRpcFailure(new COMException("x", RpcDisconnected)));
            Assert.False(BaanResilienceHelper.IsRpcFailure(new COMException("x", unchecked((int)0x80004005))));
            Assert.False(BaanResilienceHelper.IsRpcFailure(null));
        }

        [Fact]
        public void CheckServerAvailability_DoesNotThrow()
        {
            // On Windows this probes the environment; it must never throw.
            var status = BaanResilienceHelper.CheckServerAvailability("Baan.Application.doesnotexist");
            Assert.False(status.IsAvailable);
        }
    }
}
