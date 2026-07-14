using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Wangcaisoft.DotNet.BaanWindowsLib;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Examples.WinForms.NetFramework
{
    /// <summary>
    /// 纯代码构建的窗体（不依赖 Designer/resx），演示：
    /// 连接 Baan/Infor LN → 单参数调用 + BaanResult 解析 → ottstpapihand/stpapi 会话处理。
    /// </summary>
    public sealed class MainForm : Form
    {
        private readonly TextBox _progIdBox;
        private readonly Button _runButton;
        private readonly TextBox _outputBox;

        public MainForm()
        {
            Text = "Wangcaisoft.DotNet.BaanWindowsLib — WinForms 示例 (.NET Framework)";
            Width = 720;
            Height = 480;
            StartPosition = FormStartPosition.CenterScreen;

            var progIdLabel = new Label
            {
                Text = "OLE 自动化类名 (ProgID)：",
                Left = 12,
                Top = 16,
                Width = 160,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _progIdBox = new TextBox
            {
                Left = 176,
                Top = 14,
                Width = 360,
                Text = "Baan.Application.erpln"
            };

            _runButton = new Button
            {
                Text = "运行示例",
                Left = 548,
                Top = 12,
                Width = 140,
                Height = 26
            };
            _runButton.Click += RunButton_Click;

            _outputBox = new TextBox
            {
                Left = 12,
                Top = 52,
                Width = 676,
                Height = 380,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font(FontFamily.GenericMonospace, 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(progIdLabel);
            Controls.Add(_progIdBox);
            Controls.Add(_runButton);
            Controls.Add(_outputBox);
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            _outputBox.Clear();
            _runButton.Enabled = false;
            try
            {
                RunDemo(_progIdBox.Text.Trim());
            }
            finally
            {
                _runButton.Enabled = true;
            }
        }

        private void RunDemo(string progId)
        {
            Log("演示：连接 Baan/Infor LN → 调用 DLL 方法 → 用 BaanResult 解析返回值");
            Log(string.Empty);

            try
            {
                // 弹性演示（可选）：先探测环境，再用带重试的方式创建 OLE 对象。
                // 变量必须声明为 dynamic，DynamicObject 才能拦截链式成员访问。
                var status = BaanResilienceHelper.CheckServerAvailability(progId);
                Log($"环境探测：{status.Message}（ProgID={status.ProgIdResolved}, RPC={status.RpcRunning}, bw 进程={status.BwProcessCount}）");

                // CreateInstanceWithRetry 在 RPC 失败时会自动恢复 RPC 服务并重试；
                // 全部失败才抛出 COMException。这里用较小的重试参数便于本地体验。
                object ole = BaanResilienceHelper.CreateInstanceWithRetry(progId, maxRetries: 2, initialDelayMs: 200);

                using (dynamic baan = new Baan(progId, ole))
                {
                    Log("已连接到 OLE 自动化对象：" + progId);

                    // 演示 1：单参数简化调用模式 + BaanResult 解析。
                    object raw = baan.zdalmain.process("200|CREATE|ORDER|1001|2026-07-11");
                    BaanResult result = BaanResult.Parse(raw);
                    Log(result.IsSuccess
                        ? $"[成功] Code={result.Code}, Message={result.Message}"
                        : $"[失败] Code={result.Code}, Message={result.Message}");

                    // 演示 2：标准会话处理 API（ottstpapihand / stpapi）。
                    // f 必须是 dynamic，var put 会推断为 dynamic，因此 put(...) 可以直接调用。
                    Log(string.Empty);
                    Log("--- 演示 2：ottstpapihand / stpapi 会话处理 ---");

                    dynamic f = baan.ottstpapihand;
                    var put = f.stpapi.put.field;

                    put("sessioncode", "fieldname1", "value1");
                    put("sessioncode", "fieldname2", "value2");

                    object endResult = f.end.session("sessioncode");
                    Log("end.session 返回：" + endResult);
                }
            }
            catch (COMException ex) when (BaanResilienceHelper.IsRpcFailure(ex))
            {
                // RPC/连接类故障：即便重试后仍失败，通常说明 BW 客户端或 Windows RPC 服务不可用。
                Log("⚠ RPC/连接故障：Baan 客户端或 Windows RPC 服务不可用。");
                Log("  原因：" + ex.Message);
                Log("  可尝试 BaanResilienceHelper.EnsureRpcRunning() / KillZombieBwProcesses() 进行恢复。");
            }
            catch (COMException ex)
            {
                // 没有 Baan Worktop 客户端的开发机上会走到这里——示例仍可正常编译运行。
                Log("⚠ 无法连接到 Baan/Infor LN 的 OLE 自动化对象。");
                Log("  原因：" + ex.Message);
                Log("  请确认已安装 Baan Worktop (BW) 客户端，并在本机注册了对应的 OLE 自动化类。");
            }
            catch (UnknownDllException ex)
            {
                Log("[未知 DLL] " + ex.Message);
            }
            catch (UnknownMethodException ex)
            {
                Log("[未知方法] " + ex.Message);
            }
            catch (BaanResultException ex)
            {
                Log($"[业务返回错误] Code={ex.Code}, Message={ex.Message}");
            }
        }

        private void Log(string line)
        {
            _outputBox.AppendText(line + Environment.NewLine);
        }
    }
}
