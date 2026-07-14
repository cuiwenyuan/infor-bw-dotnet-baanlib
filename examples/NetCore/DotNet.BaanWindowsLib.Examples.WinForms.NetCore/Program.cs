using System;
using System.Windows.Forms;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Examples.WinForms.NetCore
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
