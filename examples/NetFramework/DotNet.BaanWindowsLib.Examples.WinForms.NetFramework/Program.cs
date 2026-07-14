using System;
using System.Windows.Forms;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Examples.WinForms.NetFramework
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
