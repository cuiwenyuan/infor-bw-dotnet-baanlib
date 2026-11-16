using System;
using System.Threading;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Tests
{
    /// <summary>
    /// A lightweight in-memory stand-in for the Baan/Infor LN OLE automation object, used so the
    /// test suite can exercise the chaining/dispatch logic without a real Baan client installed.
    /// </summary>
    public class FakeBaan
    {
        public object Timeout { get; set; }

        public object ReturnValue { get; set; }

        public object FunctionCall { get; set; }

        public object ReturnCall { get; set; }

        public object Binary { get; set; }

        /// <summary>Error code inspected by <see cref="BaanWrapper"/> after a call (-1, -2 or 0).</summary>
        public int Error { get; set; }

        /// <summary>If set, <see cref="ParseExecFunction"/> throws this exception (simulating a COM failure).</summary>
        public Exception ParseExecException { get; set; }

        /// <summary>If true, <see cref="ParseExecFunction"/> stores the method string in <see cref="ReturnValue"/>.</summary>
        public bool EchoMethodAsReturnValue { get; set; }

        /// <summary>If true, <see cref="Quit"/> throws (simulating a broken COM server on shutdown).</summary>
        public bool ThrowOnQuit { get; set; }

        /// <summary>If greater than zero, <see cref="ParseExecFunction"/> sleeps for this many milliseconds
        /// (used to simulate a slow/hung call that holds the internal lock).</summary>
        public int SlowParseExecMs { get; set; }

        public string LastDllName { get; private set; }

        public string LastMethod { get; private set; }

        public bool QuitCalled { get; private set; }

        public void ParseExecFunction(string dllName, string method)
        {
            if (SlowParseExecMs > 0)
            {
                Thread.Sleep(SlowParseExecMs);
            }

            LastDllName = dllName;
            LastMethod = method;

            if (ParseExecException != null)
            {
                throw ParseExecException;
            }

            if (EchoMethodAsReturnValue)
            {
                ReturnValue = method;
            }
        }

        public void Quit()
        {
            QuitCalled = true;

            if (ThrowOnQuit)
            {
                throw new InvalidOperationException("simulated broken COM server on Quit");
            }
        }
    }
}
