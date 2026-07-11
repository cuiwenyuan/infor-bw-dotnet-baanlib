using System;
using Wangcaisoft.DotNet.BaanWindowsLib;
using Xunit;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Tests
{
    public class BaanTests
    {
        [Fact]
        public void DllName_IsFirstSegment()
        {
            dynamic b = new Baan("Baan.Application.erpln", new FakeBaan());

            Assert.Equal("ottstpapihand", ((BaanWrapper)b.ottstpapihand).DllName);
            Assert.Equal(
                "ottstpapihand",
                ((BaanWrapper)(b.ottstpapihand.stpapi.put.field)).DllName);

            b.Close();
        }

        [Fact]
        public void MethodName_IsRemainingSegments()
        {
            dynamic b = new Baan("Baan.Application.erpln", new FakeBaan());

            Assert.Equal("stpapi", ((BaanWrapper)b.ottstpapihand.stpapi).MethodName);
            Assert.Equal(
                "stpapi.put.field",
                ((BaanWrapper)(b.ottstpapihand.stpapi.put.field)).MethodName);

            b.Close();
        }

        [Fact]
        public void GetCallingMethod_BuildsQuotedString()
        {
            dynamic b = new Baan("Baan.Application.erpln", new FakeBaan());
            var wrapper = (BaanWrapper)(b.ottstpapihand.stpapi.put.field);

            var callingMethod = wrapper.GetCallingMethod("tcibd0501m000", "tcibd001.item", "TEST");

            Assert.Equal(
                "stpapi.put.field(\"tcibd0501m000\", \"tcibd001.item\", \"TEST\")",
                callingMethod);

            b.Close();
        }

        [Fact]
        public void Call_InvokesParseExecFunction_WithDllAndMethod()
        {
            var fake = new FakeBaan();
            dynamic b = new Baan("Baan.Application.erpln", fake);

            b.ottstpapihand.stpapi.put.field("tcibd0501m000", "tcibd001.item", "TEST");
            Assert.Equal("ottstpapihand", fake.LastDllName);
            Assert.Equal(
                "stpapi.put.field(\"tcibd0501m000\", \"tcibd001.item\", \"TEST\")",
                fake.LastMethod);

            b.test.some.foo("test", 10);
            Assert.Equal("test", fake.LastDllName);
            Assert.Equal("some.foo(\"test\", 10)", fake.LastMethod);

            b.test.some.foo("test", 10.0);
            Assert.Equal("test", fake.LastDllName);
            Assert.Equal("some.foo(\"test\", 10.0)", fake.LastMethod);

            b.Close();
        }

        [Fact]
        public void UsingStatement_DisposesAndQuits()
        {
            var fake = new FakeBaan();

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                b.test.foo(1);
            }

            Assert.True(fake.QuitCalled);
        }

        [Fact]
        public void WellKnownProperties_AreNotWrappers()
        {
            using (dynamic b = new Baan("Baan.Application.erpln", new FakeBaan()))
            {
                Assert.IsNotType<BaanWrapper>(b.Timeout);
                Assert.Equal(3600, b.Timeout);
                Assert.IsNotType<BaanWrapper>(b.ReturnValue);
                Assert.IsNotType<BaanWrapper>(b.FunctionCall);
                Assert.IsNotType<BaanWrapper>(b.ReturnCall);
                Assert.IsNotType<BaanWrapper>(b.Binary);
            }
        }

        [Fact]
        public void Setters_UpdateUnderlyingObject()
        {
            var fake = new FakeBaan();

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                b.Timeout = 3800;
                b.Binary = true;
            }

            Assert.Equal(3800, fake.Timeout);
            Assert.Equal(true, fake.Binary);
        }

        [Fact]
        public void Error_Minus1_ThrowsUnknownDllException()
        {
            var fake = new FakeBaan { Error = -1 };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                Assert.Throws<UnknownDllException>(() => b.unkowndll.foo());
            }
        }

        [Fact]
        public void Error_Minus2_ThrowsUnknownMethodException()
        {
            var fake = new FakeBaan { Error = -2 };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                Assert.Throws<UnknownMethodException>(() => b.dll.unknown_method());
            }
        }

        [Theory]
        [InlineData("0|Success", 0, "Success", true)]
        [InlineData("1|Error", 1, "Error", false)]
        [InlineData("1", 1, "", false)]
        [InlineData("Success", 0, "Success", true)]
        [InlineData("", 0, "", true)]
        public void BaanResult_Parse_DecodesConvention(
            string input, int expectedCode, string expectedMessage, bool expectedSuccess)
        {
            var r = BaanResult.Parse(input);

            Assert.Equal(expectedCode, r.Code);
            Assert.Equal(expectedMessage, r.Message);
            Assert.Equal(input, r.Raw);
            Assert.Equal(expectedSuccess, r.IsSuccess);
        }

        [Fact]
        public void BaanResult_Parse_Null_IsSuccess()
        {
            var r = BaanResult.Parse(null);

            Assert.Equal(0, r.Code);
            Assert.True(r.IsSuccess);
        }

        [Fact]
        public void BaanResult_Parse_CustomDelimiter()
        {
            var r = BaanResult.Parse("2;NotFound", ';');

            Assert.Equal(2, r.Code);
            Assert.Equal("NotFound", r.Message);
            Assert.False(r.IsSuccess);
        }

        [Fact]
        public void ThrowOnErrorResult_DefaultFalse_ReturnsRawValue()
        {
            var fake = new FakeBaan { ReturnValue = "1|Error" };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                var raw = b.odll.process("x");
                Assert.Equal("1|Error", raw);
            }
        }

        [Fact]
        public void ThrowOnErrorResult_True_NonZero_ThrowsBaanResultException()
        {
            var fake = new FakeBaan { ReturnValue = "1|Error" };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                b.ThrowOnErrorResult = true;

                var ex = Assert.Throws<BaanResultException>(() => b.odll.process("x"));
                Assert.Equal(1, ex.Code);
                Assert.Equal("Error", ex.Message);
            }
        }

        [Fact]
        public void ThrowOnErrorResult_True_Zero_DoesNotThrow()
        {
            var fake = new FakeBaan { ReturnValue = "0|Success" };

            using (dynamic b = new Baan("Baan.Application.erpln", fake))
            {
                b.ThrowOnErrorResult = true;

                var raw = b.odll.process("x");
                Assert.Equal("0|Success", raw);
            }
        }
    }
}
