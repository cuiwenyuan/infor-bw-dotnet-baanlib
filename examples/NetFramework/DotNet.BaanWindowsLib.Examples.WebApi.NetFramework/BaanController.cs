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

        /// <summary>单参数简化调用模式 + BaanResult 解析。</summary>
        [HttpGet]
        public IHttpActionResult Process(string payload = "200|CREATE|ORDER|1001|2026-07-11",
                                         string progId = DefaultProgId)
        {
            try
            {
                using (dynamic baan = new Baan(progId))
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
                return Content(System.Net.HttpStatusCode.ServiceUnavailable, new
                {
                    error = "无法连接到 Baan/Infor LN 的 OLE 自动化对象。",
                    reason = ex.Message,
                    hint = "请确认已安装 Baan Worktop (BW) 客户端并注册了对应的 OLE 自动化类。"
                });
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
                using (dynamic baan = new Baan(progId))
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
                return Content(System.Net.HttpStatusCode.ServiceUnavailable, new
                {
                    error = "无法连接到 Baan/Infor LN 的 OLE 自动化对象。",
                    reason = ex.Message,
                    hint = "请确认已安装 Baan Worktop (BW) 客户端并注册了对应的 OLE 自动化类。"
                });
            }
        }
    }
}
