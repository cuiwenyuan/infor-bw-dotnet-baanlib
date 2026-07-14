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

Each example demonstrates two patterns / 每个示例都演示两种模式：

1. **Single-parameter call + `BaanResult`** — pack "action + data" into one string, parse the
   `"<code>|<message>"` return value.
   **单参数调用 + `BaanResult`** —— 把"动作+数据"打包成一个字符串，解析 `"<code>|<message>"` 返回值。
2. **Session handling (`ottstpapihand` / stpapi)** — put fields into a standard session, then end it.
   **会话处理（`ottstpapihand` / stpapi）** —— 逐个字段写入标准会话，最后结束会话。

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
