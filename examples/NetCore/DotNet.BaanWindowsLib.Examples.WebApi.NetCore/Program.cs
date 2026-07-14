using System.Runtime.InteropServices;
using Wangcaisoft.DotNet.BaanWindowsLib;

const string DefaultProgId = "Baan.Application.erpln";

// 弹性连接工厂（可选）：先探测环境，再用带重试的方式创建 OLE 对象。
// 把 BaanResilienceHelper 显式接线进来，由调用方决定何时/如何恢复。
static dynamic CreateResilientBaan(string progId)
{
    var status = BaanResilienceHelper.CheckServerAvailability(progId);
    if (!status.IsAvailable)
    {
        // 探测未通过：显式抛出，交由下方的 COMException 处理返回 503。
        throw new COMException($"Baan 环境探测未通过：{status.Message}");
    }

    // CreateInstanceWithRetry 在 RPC 失败时自动恢复 RPC 服务并重试；全部失败才抛 COMException。
    // 这里用较小的重试参数便于本地体验。
    object ole = BaanResilienceHelper.CreateInstanceWithRetry(progId, maxRetries: 2, initialDelayMs: 200);
    return new Baan(progId, ole);
}

// 统一的 RPC/连接故障响应（503）。
static IResult RpcFailureResult(COMException ex)
{
    bool rpc = BaanResilienceHelper.IsRpcFailure(ex);
    return Results.Json(new
    {
        error = rpc
            ? "Baan RPC/连接故障：已重试仍失败，可能是 BW 客户端或 Windows RPC 服务不可用。"
            : "无法连接到 Baan/Infor LN 的 OLE 自动化对象。",
        reason = ex.Message,
        hint = "请确认已安装 Baan Worktop (BW) 客户端并注册了对应的 OLE 自动化类；" +
               "若为 RPC 故障，可调用 BaanResilienceHelper.EnsureRpcRunning() / KillZombieBwProcesses() 进行恢复。"
    }, statusCode: StatusCodes.Status503ServiceUnavailable);
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 演示 1：单参数简化调用模式 + BaanResult 解析。
// GET /api/baan/process?payload=200|CREATE|ORDER|1001
app.MapGet("/api/baan/process", (string? payload, string? progId) =>
{
    payload ??= "200|CREATE|ORDER|1001|2026-07-11";
    progId ??= DefaultProgId;

    try
    {
        using dynamic baan = CreateResilientBaan(progId);
        object raw = baan.zdalmain.process(payload);
        BaanResult result = BaanResult.Parse(raw);
        return Results.Ok(new
        {
            success = result.IsSuccess,
            code = result.Code,
            message = result.Message,
            raw = result.Raw
        });
    }
    catch (COMException ex)
    {
        return RpcFailureResult(ex);
    }
    catch (UnknownDllException ex)
    {
        return Results.BadRequest("未知 DLL：" + ex.Message);
    }
    catch (UnknownMethodException ex)
    {
        return Results.BadRequest("未知方法：" + ex.Message);
    }
    catch (BaanResultException ex)
    {
        return Results.Json(new { code = ex.Code, message = ex.Message },
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }
});

// 演示 2：标准会话处理（ottstpapihand / stpapi）。
// GET /api/baan/session?sessionCode=sessioncode
app.MapGet("/api/baan/session", (string? sessionCode, string? progId) =>
{
    sessionCode ??= "sessioncode";
    progId ??= DefaultProgId;

    try
    {
        using dynamic baan = CreateResilientBaan(progId);
        dynamic f = baan.ottstpapihand;
        var put = f.stpapi.put.field;

        put(sessionCode, "fieldname1", "value1");
        put(sessionCode, "fieldname2", "value2");

        object endResult = f.end.session(sessionCode);
        return Results.Ok(new { session = sessionCode, endResult });
    }
    catch (COMException ex)
    {
        return RpcFailureResult(ex);
    }
});

app.MapGet("/", () => "Wangcaisoft.DotNet.BaanWindowsLib — Web API 示例 (.NET Core Minimal API)。"
    + "端点：/api/baan/process、/api/baan/session");

app.Run();
