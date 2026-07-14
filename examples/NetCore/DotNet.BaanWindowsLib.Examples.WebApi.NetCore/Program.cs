using System.Runtime.InteropServices;
using Wangcaisoft.DotNet.BaanWindowsLib;

const string DefaultProgId = "Baan.Application.erpln";

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
        using dynamic baan = new Baan(progId);
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
        return Results.Json(new
        {
            error = "无法连接到 Baan/Infor LN 的 OLE 自动化对象。",
            reason = ex.Message,
            hint = "请确认已安装 Baan Worktop (BW) 客户端并注册了对应的 OLE 自动化类。"
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
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
        using dynamic baan = new Baan(progId);
        dynamic f = baan.ottstpapihand;
        var put = f.stpapi.put.field;

        put(sessionCode, "fieldname1", "value1");
        put(sessionCode, "fieldname2", "value2");

        object endResult = f.end.session(sessionCode);
        return Results.Ok(new { session = sessionCode, endResult });
    }
    catch (COMException ex)
    {
        return Results.Json(new
        {
            error = "无法连接到 Baan/Infor LN 的 OLE 自动化对象。",
            reason = ex.Message,
            hint = "请确认已安装 Baan Worktop (BW) 客户端并注册了对应的 OLE 自动化类。"
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/", () => "Wangcaisoft.DotNet.BaanWindowsLib — Web API 示例 (.NET Core Minimal API)。"
    + "端点：/api/baan/process、/api/baan/session");

app.Run();
