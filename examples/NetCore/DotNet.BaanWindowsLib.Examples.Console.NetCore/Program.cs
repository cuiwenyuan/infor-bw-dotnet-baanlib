using System;
using static System.Console;
using System.Runtime.InteropServices;
using Wangcaisoft.DotNet.BaanWindowsLib;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Examples.Console.NetCore
{
    internal static class Program
    {
        private static void Main()
        {
            WriteLine("Wangcaisoft.DotNet.BaanWindowsLib — Console 示例 (.NET Core)");
            WriteLine("演示：连接 Baan/Infor LN → 调用 DLL 方法 → 用 BaanResult 解析返回值");
            WriteLine();

            // OLE 自动化类名（ProgID），对应 BECS 里注册的连接配置。
            string progId = "Baan.Application.erpln";

            try
            {
                // 建立连接。若本机未安装/未注册 BW 客户端，此处会抛出 COMException。
                // 变量必须声明为 dynamic，DynamicObject 才能拦截链式成员访问。
                using (dynamic baan = new Baan(progId))
                {
                    WriteLine($"已连接到 OLE 自动化对象：{progId}");

                    // 演示 1：单参数简化调用模式 + BaanResult 解析。
                    object raw = baan.zdalmain.process("200|CREATE|ORDER|1001|2026-07-11");
                    BaanResult result = BaanResult.Parse(raw);
                    if (result.IsSuccess)
                    {
                        ForegroundColor = ConsoleColor.Green;
                        WriteLine($"[成功] Code={result.Code}, Message={result.Message}");
                        ResetColor();
                    }
                    else
                    {
                        ForegroundColor = ConsoleColor.Red;
                        WriteLine($"[失败] Code={result.Code}, Message={result.Message}");
                        ResetColor();
                    }

                    // 演示 2：标准会话处理 API（ottstpapihand / stpapi）。
                    // f 必须是 dynamic，var put 会推断为 dynamic，因此 put(...) 可以直接调用。
                    WriteLine();
                    WriteLine("--- 演示 2：ottstpapihand / stpapi 会话处理 ---");

                    dynamic f = baan.ottstpapihand;
                    var put = f.stpapi.put.field;

                    put("sessioncode", "fieldname1", "value1");
                    put("sessioncode", "fieldname2", "value2");

                    object endResult = f.end.session("sessioncode");
                    WriteLine($"end.session 返回：{endResult}");
                }
            }
            catch (COMException ex)
            {
                // 没有 Baan Worktop 客户端的开发机上会走到这里——示例仍可正常编译运行。
                ForegroundColor = ConsoleColor.Yellow;
                WriteLine("⚠ 无法连接到 Baan/Infor LN 的 OLE 自动化对象。");
                WriteLine($"  原因：{ex.Message}");
                WriteLine("  请确认已安装 Baan Worktop (BW) 客户端，并在本机注册了对应的 OLE 自动化类。");
                ResetColor();
            }
            catch (UnknownDllException ex)
            {
                WriteLine($"[未知 DLL] {ex.Message}");
            }
            catch (UnknownMethodException ex)
            {
                WriteLine($"[未知方法] {ex.Message}");
            }
            catch (BaanResultException ex)
            {
                WriteLine($"[业务返回错误] Code={ex.Code}, Message={ex.Message}");
            }

            WriteLine();
            if (!IsInputRedirected)
            {
                WriteLine("按任意键退出...");
                ReadKey();
            }
        }
    }
}
