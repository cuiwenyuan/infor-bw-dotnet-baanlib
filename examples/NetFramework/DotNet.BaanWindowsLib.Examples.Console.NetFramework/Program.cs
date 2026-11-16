using System;
using static System.Console;
using System.Runtime.InteropServices;
using Wangcaisoft.DotNet.BaanWindowsLib;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Examples.Console.NetFramework
{
    internal static class Program
    {
        private static void Main()
        {
            WriteLine("Wangcaisoft.DotNet.BaanWindowsLib — Console 示例 (.NET Framework)");
            WriteLine("演示：连接 Baan/Infor LN → 调用 DLL 方法 → 用 BaanResult 解析返回值");
            WriteLine();

            // OLE 自动化类名（ProgID），对应 BECS 里注册的连接配置。
            // 多公司场景可为每个公司注册独立 ProgID，例如 Baan.Application.erpln.c200。
            string progId = "Baan.Application.erpln";

            try
            {
                // 弹性演示（可选）：先探测环境，再用带重试的方式创建 OLE 对象。
                // 变量必须声明为 dynamic，DynamicObject 才能拦截链式成员访问。
                var status = BaanResilienceHelper.CheckServerAvailability(progId);
                WriteLine($"环境探测：{status.Message}（ProgID={status.ProgIdResolved}, RPC={status.RpcRunning}, bw 进程={status.BwProcessCount}）");

                // CreateInstanceWithRetry 在 RPC 失败时会自动恢复 RPC 服务并重试；
                // 全部失败才抛出 COMException。这里用较小的重试参数便于本地体验。
                object ole = BaanResilienceHelper.CreateInstanceWithRetry(progId, maxRetries: 2, initialDelayMs: 200);

                using (dynamic baan = new Baan(progId, ole))
                {
                    WriteLine($"已连接到 OLE 自动化对象：{progId}");

                    // 单参数简化调用模式：把“动作 + 数据”打包成一个字符串，
                    // 由 4GL DLL 内部拆解为 company / action / entity / key / date 等。
                    object raw = baan.zdalmain.process("200|CREATE|ORDER|1001|2026-07-11");

                    // 用 BaanResult 解析 "<code>|<message>" 约定格式（默认分隔符 '|'）。
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
                    // 这是 Baan/Infor LN 里“驱动一个标准会话”的惯用模式：把字段逐个写入会话，
                    // 最后结束会话。下面等价于 4GL 里通过 ottstpapihand 维护一张会话表。
                    //
                    // 注意：f 必须是 dynamic，这样 f.stpapi.put.field 才能被 DynamicObject 拦截；
                    // var put 会推断为 dynamic，因此 put(...) 可以直接调用。
                    WriteLine();
                    WriteLine("--- 演示 2：ottstpapihand / stpapi 会话处理 ---");

                    dynamic f = baan.ottstpapihand;
                    var put = f.stpapi.put.field;

                    // 把字段逐条写入指定会话（sessioncode 换成你真实的会话名）。
                    put("sessioncode", "fieldname1", "value1");
                    put("sessioncode", "fieldname2", "value2");

                    // 结束会话（返回值通常可忽略，这里仅打印以便观察）。
                    object endResult = f.end.session("sessioncode");
                    WriteLine($"end.session 返回：{endResult}");

                    // 演示 3：原始调用风格（Raw）——跳过链式成员访问，直接把
                    // “DLL 名 + 完整调用串”传给 ParseExecFunction，调用串原样透传。
                    // 适合从既有代码/配置迁移；baan 是 dynamic，也能直接调用（真实成员优先于动态链）。
                    WriteLine();
                    WriteLine("--- 演示 3：原始调用风格 ParseExecFunction ---");

                    object rawCallResult = baan.ParseExecFunction(
                        "ottstpapihand",
                        "stpapi.get.field(\"sessioncode\", \"fieldname1\")");
                    WriteLine($"ParseExecFunction 返回：{rawCallResult}");
                }
            }
            catch (COMException ex) when (BaanResilienceHelper.IsRpcFailure(ex))
            {
                // RPC/连接类故障：即便重试后仍失败，通常说明 BW 客户端或 Windows RPC 服务不可用。
                ForegroundColor = ConsoleColor.Yellow;
                WriteLine("⚠ RPC/连接故障：Baan 客户端或 Windows RPC 服务不可用。");
                WriteLine($"  原因：{ex.Message}");
                WriteLine("  可尝试 BaanResilienceHelper.EnsureRpcRunning() / KillZombieBwProcesses() 进行恢复。");
                ResetColor();
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
