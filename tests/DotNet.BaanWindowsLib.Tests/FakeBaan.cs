using System;

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

        public string LastDllName { get; private set; }

        public string LastMethod { get; private set; }

        public bool QuitCalled { get; private set; }

        public void ParseExecFunction(string dllName, string method)
        {
            LastDllName = dllName;
            LastMethod = method;
        }

        public void Quit()
        {
            QuitCalled = true;
        }
    }
}
