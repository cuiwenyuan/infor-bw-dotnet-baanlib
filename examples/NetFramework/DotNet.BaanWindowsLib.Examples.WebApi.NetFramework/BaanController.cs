using System.Runtime.InteropServices;
using System.Web.Http;
using Wangcaisoft.DotNet.BaanWindowsLib;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Examples.WebApi.NetFramework
{
    /// <summary>
    /// 演示如何在 Web API 里调用 Baan/Infor LN：
    /// GET api/baan/process 走单参数模式 + BaanResult；
    /// GET api/baan/session 走 ottstpapihand/stpapi 会话处理。
    /// </summary>
    public class BaanController : ApiController
    {
        private const string DefaultProgId = "Baan.Application.erpln";

        /// <summary>
        /// 弹性连接工厂（可选）：先探测环境，再用带重试的方式创建 OLE 对象。
        /// 把 BaanResilienceHelper 显式接线进来，由调用方决定何时/如何恢复。
        /// </summary>
        private dynamic CreateResilientBaan(string progId)
        {
            var status = BaanResilienceHelper.CheckServerAvailability(progId);
            if (!status.IsAvailable)
            {
                throw new COMException($"Baan 环境探测未通过：{status.Message}");
            }

            // CreateInstanceWithRetry 在 RPC 失败时自动恢复 RPC 服务并重试；全部失败才抛 COMException。
            object ole = BaanResilienceHelper.CreateInstanceWithRetry(progId, maxRetries: 2, initialDelayMs: 200);
            return new Baan(progId, ole);
        }

        /// <summary>统一的 RPC/连接故障响应（503）。</summary>
        private IHttpActionResult RpcFailure(COMException ex)
        {
            bool rpc = BaanResilienceHelper.IsRpcFailure(ex);
            return Content(System.Net.HttpStatusCode.ServiceUnavailable, new
            {
                error = rpc
                    ? "Baan RPC/连接故障：已重试仍失败，可能是 BW 客户端或 Windows RPC 服务不可用。"
                    : "无法连接到 Baan/Infor LN 的 OLE 自动化对象。",
                reason = ex.Message,
                hint = "请确认已安装 Baan Worktop (BW) 客户端并注册了对应的 OLE 自动化类；" +
                       "若为 RPC 故障，可调用 BaanResilienceHelper.EnsureRpcRunning() / KillZombieBwProcesses() 进行恢复。"
            });
        }

        /// <summary>单参数简化调用模式 + BaanResult 解析。</summary>
        [HttpGet]
        public IHttpActionResult Process(string payload = "200|CREATE|ORDER|1001|2026-07-11",
                                         string progId = DefaultProgId)
        {
            try
            {
                using (dynamic baan = CreateResilientBaan(progId))
                {
                    object raw = baan.zdalmain.process(payload);
                    BaanResult result = BaanResult.Parse(raw);
                    return Ok(new
                    {
                        success = result.IsSuccess,
                        code = result.Code,
                        message = result.Message,
                        raw = result.Raw
                    });
                }
            }
            catch (COMException ex)
            {
                return RpcFailure(ex);
            }
            catch (UnknownDllException ex)
            {
                return BadRequest("未知 DLL：" + ex.Message);
            }
            catch (UnknownMethodException ex)
            {
                return BadRequest("未知方法：" + ex.Message);
            }
            catch (BaanResultException ex)
            {
                // net462 的 HttpStatusCode 枚举没有 UnprocessableEntity(422)，用数值转换。
                return Content((System.Net.HttpStatusCode)422, new
                {
                    code = ex.Code,
                    message = ex.Message
                });
            }
        }

        /// <summary>标准会话处理（ottstpapihand / stpapi）。</summary>
        [HttpGet]
        public IHttpActionResult Session(string sessionCode = "sessioncode",
                                         string progId = DefaultProgId)
        {
            try
            {
                using (dynamic baan = CreateResilientBaan(progId))
                {
                    dynamic f = baan.ottstpapihand;
                    var put = f.stpapi.put.field;

                    put(sessionCode, "fieldname1", "value1");
                    put(sessionCode, "fieldname2", "value2");

                    object endResult = f.end.session(sessionCode);
                    return Ok(new { session = sessionCode, endResult });
                }
            }
            catch (COMException ex)
            {
                return RpcFailure(ex);
            }
        }
    }
}
