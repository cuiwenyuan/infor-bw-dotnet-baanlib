using System;
using Microsoft.Owin.Hosting;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Examples.WebApi.NetFramework
{
    internal static class Program
    {
        private static void Main()
        {
            const string baseAddress = "http://localhost:9000/";

            using (WebApp.Start<Startup>(baseAddress))
            {
                Console.WriteLine("Wangcaisoft.DotNet.BaanWindowsLib — Web API 示例 (.NET Framework, OWIN 自宿主)");
                Console.WriteLine("已启动，监听：" + baseAddress);
                Console.WriteLine();
                Console.WriteLine("可用端点：");
                Console.WriteLine("  GET " + baseAddress + "api/baan/process?payload=200|CREATE|ORDER|1001");
                Console.WriteLine("  GET " + baseAddress + "api/baan/session?sessionCode=sessioncode");
                Console.WriteLine();

                if (Console.IsInputRedirected)
                {
                    // CI/管道环境下不阻塞，短暂运行后退出。
                    Console.WriteLine("检测到输入被重定向，跳过等待并退出。");
                    return;
                }

                Console.WriteLine("按回车键停止服务...");
                Console.ReadLine();
            }
        }
    }
}
