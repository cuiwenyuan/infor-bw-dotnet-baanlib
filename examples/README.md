# Examples / 示例程序

Sample applications for [`Wangcaisoft.DotNet.BaanWindowsLib`](../README.md), grouped by runtime
(.NET Framework / .NET Core), each with **Console**, **WinForms** and **Web API** flavors.

[`Wangcaisoft.DotNet.BaanWindowsLib`](../README.zh-CN.md) 的示例程序，按运行时（.NET Framework / .NET Core）
分组，每组包含 **Console**、**WinForms**、**Web API** 三种类型。

> All examples use **real** calls (`new Baan("Baan.Application.erpln")`). On a machine without the
> Baan Worktop (BW) client, they compile and run fine and simply show a friendly
> "OLE object unavailable" message — no Baan client is required to build or run them.
>
> 所有示例都使用**真实**调用（`new Baan("Baan.Application.erpln")`）。在没有安装 BW 客户端的机器上，
> 它们同样可以正常编译运行，只会打印友好的"OLE 对象不可用"提示——构建与运行都**不需要** Baan 客户端。

## Structure / 目录结构

```
examples/
├── NetFramework/                                            # net462
│   ├── DotNet.BaanWindowsLib.Examples.Console.NetFramework   # 控制台
│   ├── DotNet.BaanWindowsLib.Examples.WinForms.NetFramework  # WinForms
│   └── DotNet.BaanWindowsLib.Examples.WebApi.NetFramework    # OWIN 自宿主 Web API
└── NetCore/                                                 # net8.0-windows
    ├── DotNet.BaanWindowsLib.Examples.Console.NetCore        # 控制台
    ├── DotNet.BaanWindowsLib.Examples.WinForms.NetCore       # WinForms
    └── DotNet.BaanWindowsLib.Examples.WebApi.NetCore         # ASP.NET Core Minimal API
```

Each example demonstrates three patterns / 每个示例都演示三种模式：

1. **Single-parameter call + `BaanResult`** — pack "action + data" into one string, parse the
   `"<code>|<message>"` return value.
   **单参数调用 + `BaanResult`** —— 把"动作+数据"打包成一个字符串，解析 `"<code>|<message>"` 返回值。
2. **Session handling (`ottstpapihand` / stpapi)** — put fields into a standard session, then end it.
   **会话处理（`ottstpapihand` / stpapi）** —— 逐个字段写入标准会话，最后结束会话。
3. **Resilience (optional, `BaanResilienceHelper`)** — before connecting, probe the environment
   with `CheckServerAvailability` and create the OLE object with `CreateInstanceWithRetry`
   (auto-recovers the RPC service and retries on a transient RPC failure); RPC failures are
   distinguished in the error handler via `IsRpcFailure`.
   **弹性容错（可选，`BaanResilienceHelper`）** —— 连接前用 `CheckServerAvailability` 探测环境，
   并用 `CreateInstanceWithRetry` 创建 OLE 对象（RPC 故障时自动恢复 RPC 服务并重试）；错误处理里
   通过 `IsRpcFailure` 区分 RPC 故障。

The two **Console** examples additionally demonstrate the **raw call style**
(`b.ParseExecFunction("dll", "func(\"a\", \"b\")")`) — the call text is passed through verbatim,
which is handy when migrating existing code or when the call string is built dynamically.
两个 **Console** 示例还额外演示了**原始调用风格**
（`b.ParseExecFunction("dll", "func(\"a\", \"b\")")`）——调用串原样透传，适合从既有代码迁移，
或调用串需要动态拼接时使用。

## Target Frameworks / 目标框架

| Group / 组 | TFM |
|------------|-----|
| .NET Framework | `net462` |
| .NET Core | `net8.0-windows` |

## Running / 运行方式

Prerequisite: [.NET SDK](https://dotnet.microsoft.com/download) (with .NET 8 SDK). Run from the
repository root. 前置条件：安装 [.NET SDK](https://dotnet.microsoft.com/download)（含 .NET 8 SDK），
在仓库根目录执行。

### Console

```bash
# .NET Framework
dotnet run --project examples/NetFramework/DotNet.BaanWindowsLib.Examples.Console.NetFramework

# .NET Core
dotnet run --project examples/NetCore/DotNet.BaanWindowsLib.Examples.Console.NetCore
```

### WinForms

```bash
# .NET Framework
dotnet run --project examples/NetFramework/DotNet.BaanWindowsLib.Examples.WinForms.NetFramework

# .NET Core
dotnet run --project examples/NetCore/DotNet.BaanWindowsLib.Examples.WinForms.NetCore
```

Enter a ProgID and click **运行示例 (Run)** to see the output. 输入 ProgID 后点击**运行示例**查看输出。

### Web API

```bash
# .NET Framework (OWIN self-host, listens on http://localhost:9000/)
dotnet run --project examples/NetFramework/DotNet.BaanWindowsLib.Examples.WebApi.NetFramework

# .NET Core (ASP.NET Core Minimal API)
dotnet run --project examples/NetCore/DotNet.BaanWindowsLib.Examples.WebApi.NetCore
```

Endpoints / 端点：

- `GET /api/baan/process?payload=200|CREATE|ORDER|1001` — single-parameter call + `BaanResult`
- `GET /api/baan/session?sessionCode=sessioncode` — session handling (`ottstpapihand` / stpapi)

## Notes / 说明

- The example projects are included in the main solution `DotNet.BaanWindowsLib.sln` under the
  **Examples/NetFramework** and **Examples/NetCore** solution folders.
  示例项目已加入主解决方案 `DotNet.BaanWindowsLib.sln`，归类在 **Examples/NetFramework** 与
  **Examples/NetCore** 解决方案文件夹下。
- The dynamic call requires the variable to be declared as `dynamic` (e.g. `dynamic baan = new Baan(...)`),
  otherwise the chained member access will not compile.
  动态调用要求变量声明为 `dynamic`（如 `dynamic baan = new Baan(...)`），否则链式成员访问无法编译。
- `ParseExecFunction` is a **real member** (it is not part of the dynamic chain), so the raw style
  works on both `Baan` and `dynamic` variables.
  `ParseExecFunction` 是**真实成员**（不属于动态链），因此原始调用风格在 `Baan` 与 `dynamic` 变量上都可直接使用。
- `BaanResilienceHelper` is **optional** — the core `Baan` wrapper does not depend on it. The
  examples show how to wire it in for long-running / server-side hosts; for a single short-lived
  call `new Baan(progId)` is sufficient. See the README for "When to use / When NOT to use".
  `BaanResilienceHelper` 是**可选**的——核心 `Baan` 包装并不依赖它。示例展示了如何在长时间运行 /
  服务端宿主中接线；若只是单次短时调用，`new Baan(progId)` 已足够。详见 README 的"何时使用 / 不适用场景"。
