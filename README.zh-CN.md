# Wangcaisoft.DotNet.BaanWindowsLib

[![CI](https://github.com/cuiwenyuan/infor-bw-dotnet-baanlib/actions/workflows/ci.yml/badge.svg)](https://github.com/cuiwenyuan/infor-bw-dotnet-baanlib/actions/workflows/ci.yml)
[![许可证: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> 🌐 Language / 语言: [English](README.md) · [中文](README.zh-CN.md)

[mfussenegger/baanlib](https://github.com/mfussenegger/baanlib) 的 .NET 移植版本——对 Baan / Infor LN
的 OLE 自动化对象（`Baan.Application.*`）做了轻量封装，让你可以用更自然的 .NET 代码调用 ERP 中的业务逻辑。

不用再手工拼装调用字符串：

```csharp
// 等价的“原始” OLE 自动化写法
dynamic baan = Activator.CreateInstance(Type.GetTypeFromProgID("Baan.Application.erpln"));
baan.Timeout = 3600;
baan.ParseExecFunction("odll_name", "some.function.name(\"with\", \"a\", \"few\", \"arguments\")");
baan.Quit();
```

……使用 `Wangcaisoft.DotNet.BaanWindowsLib`，可以用更自然的方式书写：DLL/对象名、函数路径与参数都以链式成员访问表达：

```csharp
using Wangcaisoft.DotNet.BaanWindowsLib;

using (var b = new Baan("Baan.Application.erpln"))
{
    b.odll_name.some.function.name("with", "a", "few", "arguments");

    int var = 1;
    string foo = "test";
    b.odll_name.some.function.name(var, foo);
}
```

## 用途

本类库用于把 .NET 应用程序（Web API、Windows 服务、桌面程序、定时任务等）与 **Infor LN（原 Baan）ERP**
集成。你只需在 Infor LN / Baan 中把业务逻辑开发成 **4GL DLL（对象）**，并通过 OLE 自动化对象暴露其函数，
即可直接从 .NET 调用这些 DLL 函数——无需手工拼装 OLE 调用字符串，也无需关心底层 COM 细节。

## 典型应用场景

- 从外部系统触发 Infor LN 中的业务动作（创建订单、过账、状态变更等）。
- 批量数据集成 / 接口同步，由 .NET 侧驱动 ERP 逻辑。
- 在 .NET 中编排多个 DLL 函数调用，组合成跨系统的业务流程。

## 快速开始

```bash
dotnet add package Wangcaisoft.DotNet.BaanWindowsLib
```

```csharp
using Wangcaisoft.DotNet.BaanWindowsLib;

// 连接到 Infor LN / Baan 的 OLE 自动化对象
using (var b = new Baan("Baan.Application.erpln"))
{
    // DLL/对象名、函数路径、参数都以链式成员访问表达
    b.odll_name.some.function.name("with", "a", "few", "arguments");

    int var = 1;
    string foo = "test";
    b.odll_name.some.function.name(var, foo);
}
```

可以进一步减少重复输入，像下面这样使用：

```csharp
using (var b = new Baan("Baan.Application.erpln"))
{
    dynamic f = b.ottstpapihand;
    var put = f.stpapi.put.field;

    put("sessioncode", "fieldname1", "value1");
    put("sessioncode", "fieldname2", "value2");

    f.end.session("sessioncode");
}
```

## 单参数简化调用模式

当 4GL DLL 函数的参数较多或经常变化时，可以把“动作 + 数据”打包成**一个字符串参数**，在 DLL 函数内部再
拆解并执行对应的逻辑。这样 .NET 侧调用保持稳定，ERP 侧的逻辑调整不需要改动接口签名。

```csharp
using Wangcaisoft.DotNet.BaanWindowsLib;

using (var b = new Baan("Baan.Application.erpln"))
{
    // 只传一个参数：动作与数据用分隔符打包
    var result = b.odll_name.process("CREATE|ORDER|1001|2026-07-11");
}
```

对应 Infor LN / Baan 4GL DLL 侧（示意）：

```4gl
function extern long process(string packed)
{
    string action(10), entity(10), key(20), dt(20)
    long   ret

    action = substr$(packed, 1, pos(packed, "|") - 1)
    packed = substr$(packed, pos(packed, "|") + 1)
    entity = substr$(packed, 1, pos(packed, "|") - 1)
    | ... 继续拆解并执行对应逻辑
    if (action = "CREATE") {
        ret = dal.create(entity, key, dt)
    }
    return(ret)
}
```

## 公司号配置（BSE_COMPNR）

本类库只透传 `ParseExecFunction(dllName, method)`，**本身不传递公司号**。公司号绑定在 ProgID 所指向的
OLE 连接 / 环境上，即它由 **Baan Worktop（BW）客户端** 一侧决定，而不是在 .NET 代码里。

**设置公司号的官方方式**，是在 BW 环境的 `set` 命令里使用该环境变量：

```
-set BSE_COMPNR=<compnr>
```

- **Infor ERP Baan 5.0c 及以后版本**（含 Infor LN）：写作 `-set BSE_COMPNR=200`
- **Infor ERP Baan IVc4**：第一次出现必须写作 `"-- -set BSE_COMPNR=200"`（注意前面的 `--`）

该命令放在 BW 客户端的 **环境（配置）的 command / arguments** 中。在 **BECS（BW Environment and
Configuration Selector）** 里为每个环境注册一个 OLE 自动化名，该环境的公司号由 `BSE_COMPNR` 固定。

> 官方参考 —— Infor LN 10.4 BW 安装，[set command](https://docs.infor.com/ln/10.4/en-us/lnolh/help/install/bw/in000510.html)

同一位置还可使用的相关 `set` 变量：

- `-set PACKAGE_COMB=<packcomb>` —— 覆盖包组合（覆盖用户数据）
- `set USER=<username>` —— 更改启动 bshell 的用户

### 方式 A —— 一个公司 = 一个 OLE 自动化名（推荐）

在 BECS 中为每个公司注册独立的 OLE 自动化名，每个环境用各自的 `-set BSE_COMPNR=...` 配置公司号。
.NET 侧只需按公司选择 ProgID：

```csharp
string progId = companyNo == 100
    ? "Baan.Application.erpln.c100"
    : "Baan.Application.erpln.c200";

using (var b = new Baan(progId))
{
    b.odll_name.some.function.name("arg1", "arg2");
}
```

### 方式 B —— 运行时切换公司（单参数模式）

若不想为每个公司建环境，可在单一环境下运行，并把公司号作为第一个参数传给自己的 4GL DLL，
由 DLL 内部先切换公司再执行逻辑：

```csharp
using (var b = new Baan("Baan.Application.erpln"))
{
    // 第一个参数 = 公司号，其余为业务数据
    var result = b.odll_name.process("200|CREATE|ORDER|1001|2026-07-11");
}
```

> 注意：`BSE_COMPNR` 只能在 bshell 启动时设置，因此它必须在 BW 侧配置（方式 A），或在 4GL DLL 内部
> 处理（方式 B）。在 `Baan` 对象创建之后，无法从 .NET 再设置公司号。

## 环境要求

- **仅支持 Windows。** 类库通过 COM 与 Baan / Infor LN 的 OLE 自动化对象通信。
- 需要以下运行时之一：
  - .NET Framework **4.6.2** 或更高版本
  - .NET **6**（Windows）、**.NET 8**（Windows）或 **.NET 10**（Windows）
- 运行时所在的机器需安装 Baan / Infor LN 客户端，并注册 `Baan.Application.*` OLE 类
  （仅运行时需要，构建与运行单元测试不要求安装）。

## 使用限制

- **仅支持 Windows 平台。** 本类库通过 COM 与 Baan / Infor LN 的 OLE 自动化对象（`Baan.Application.*`）通信，该能力仅在 Windows 上可用。
- **仅适用于本地部署版（On-premise），Infor LN Cloud 不适用。** 本类库依赖 **Baan Worktop（BW）客户端** 提供的 `Baan.Application.*` OLE 自动化对象。**Infor LN Cloud（云端多租户部署）不提供、也不支持 BW 客户端**，因此 `Baan.Application.*` 这个 OLE 类在云端不可用。**对于 LN Cloud 的集成，请改用 Infor ION 或 LN 的 SOAP / REST Web Service。**

## 安装

```bash
dotnet add package Wangcaisoft.DotNet.BaanWindowsLib
```

## 工作原理

`Baan` 继承自 `System.Dynamic.DynamicObject`。任何不属于已知 COM 属性（`Timeout`、`ReturnValue`、
`FunctionCall`、`ReturnCall`、`Binary`）的成员访问都会开启一条调用链。当该链最终被调用时，
`Wangcaisoft.DotNet.BaanWindowsLib` 会拼出 Baan 函数字符串（数字原样输出，其余内容用双引号包裹），
并调用 `ParseExecFunction(dllName, methodName)`。

调用结束后读取 OLE 的 `Error` 属性：

| `Error` | 抛出异常                       |
| ------- | ------------------------------ |
| `-1`    | `UnknownDllException`          |
| `-2`    | `UnknownMethodException`       |
| 其他    | 返回 `ReturnValue`             |

这与上游 Python 实现完全一致。

## API

```csharp
public class Baan : DynamicObject, IDisposable
{
    public Baan(string progId);                 // 创建 OLE 对象，Timeout = 3600
    public Baan(string progId, object baan);    // 包装已有对象（测试/高级用法）
    public void Close();                        // 调用 Quit()
    public void Dispose();                      // -> Close()

    public object Timeout    { get; set; }      // OLE 超时（秒）
    public object ReturnValue { get; }          // 上一次 ReturnValue
    public object FunctionCall { get; }         // 上一次函数调用字符串
    public object ReturnCall   { get; }         // 上一次返回调用字符串
    public object Binary       { get; set; }    // 二进制模式标志
}

public class BaanWrapper : DynamicObject
{
    public string DllName;        // 调用链的第一段（DLL/对象名）
    public string MethodName;     // 其余片段，用 '.' 连接
    public string GetCallingMethod(params object[] args); // 拼出调用字符串
}

public class UnknownDllException : Exception { }
public class UnknownMethodException : Exception { }
```

## 构建与测试

可以构建/测试单个项目，也可以构建整个解决方案（`DotNet.BaanWindowsLib.sln`）：

```bash
# 还原、构建并测试所有目标框架（通过解决方案）
dotnet build DotNet.BaanWindowsLib.sln -c Release
dotnet test  DotNet.BaanWindowsLib.sln -c Release

# 或只针对单个项目 / 框架
dotnet build src/DotNet.BaanWindowsLib/DotNet.BaanWindowsLib.csproj -c Release -f net8.0-windows

# 生成 NuGet 包
dotnet pack src/DotNet.BaanWindowsLib/DotNet.BaanWindowsLib.csproj -c Release
```

单元测试使用内存中的 OLE 对象替身（fake），因此无需安装真实的 Baan / Infor LN 即可运行（CI 上亦可）。

## 许可证

[MIT](LICENSE)。本库为 [mfussenegger/baanlib](https://github.com/mfussenegger/baanlib)（同为 MIT）独立的 .NET 移植版本。
