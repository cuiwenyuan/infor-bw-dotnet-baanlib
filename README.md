# BaanLib

[![CI](https://github.com/cuiwenyuan/infor-bw-dotnet-baanlib/actions/workflows/ci.yml/badge.svg)](https://github.com/cuiwenyuan/infor-bw-dotnet-baanlib/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A .NET port of [mfussenegger/baanlib](https://github.com/mfussenegger/baanlib) — a simple
wrapper around the Baan/Infor LN OLE automation object (`Baan.Application.*`) that makes OLE
automation easier to use from .NET.

Instead of having to build the call string by hand:

```csharp
// The "raw" OLE automation equivalent
dynamic baan = Activator.CreateInstance(Type.GetTypeFromProgID("Baan.Application.erpln"));
baan.Timeout = 3600;
baan.ParseExecFunction("odll_name", "some.function.name(\"with\", \"a\", \"few\", \"arguments\")");
baan.Quit();
```

…`BaanLib` lets you write it the natural way, with the DLL/object name, function path and
arguments expressed as chained member access:

```csharp
using BaanLib;

using (var b = new Baan("Baan.Application.erpln"))
{
    b.odll_name.some.function.name("with", "a", "few", "arguments");

    int var = 1;
    string foo = "test";
    b.odll_name.some.function.name(var, foo);
}
```

To further reduce typing, the API can also be used like this:

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

## Requirements

- **Windows only.** The library talks to the Baan/Infor LN OLE automation object via COM.
- One of the following runtimes:
  - .NET Framework **4.6.2** or later
  - .NET **6** (Windows), **.NET 8** (Windows) or **.NET 10** (Windows)
- The Baan/Infor LN client must be installed and its `Baan.Application.*` OLE class registered
  (only required at runtime, not for building or running the unit tests).

## Installation

```bash
dotnet add package BaanLib
```

## How it works

`Baan` derives from `System.Dynamic.DynamicObject`. Any attribute access that is not one of the
well-known COM properties (`Timeout`, `ReturnValue`, `FunctionCall`, `ReturnCall`, `Binary`)
starts a chain. When the chain is finally invoked, `BaanLib` builds the Baan function string
(numbers are emitted verbatim, everything else is wrapped in double quotes) and calls
`ParseExecFunction(dllName, methodName)`.

After the call it reads the OLE `Error` property:

| `Error` | Throws                       |
| ------- | ---------------------------- |
| `-1`    | `UnknownDllException`        |
| `-2`    | `UnknownMethodException`     |
| other   | returns `ReturnValue`        |

This mirrors the upstream Python implementation exactly.

## API

```csharp
public class Baan : DynamicObject, IDisposable
{
    public Baan(string progId);                 // creates the OLE object, Timeout = 3600
    public Baan(string progId, object baan);    // wraps an existing object (testing/advanced)
    public void Close();                        // calls Quit()
    public void Dispose();                      // -> Close()

    public object Timeout    { get; set; }      // OLE Timeout (seconds)
    public object ReturnValue { get; }          // last ReturnValue
    public object FunctionCall { get; }         // last function call string
    public object ReturnCall   { get; }         // last return call string
    public object Binary       { get; set; }    // binary mode flag
}

public class BaanWrapper : DynamicObject
{
    public string DllName;        // first segment of the chained path
    public string MethodName;     // remaining segments, joined by '.'
    public string GetCallingMethod(params object[] args); // builds the call string
}

public class UnknownDllException : Exception { }
public class UnknownMethodException : Exception { }
```

## Building and testing

You can build/test a single project or the whole solution (`BaanLib.sln`):

```bash
# Restore, build and test all target frameworks (via the solution)
dotnet build BaanLib.sln -c Release
dotnet test  BaanLib.sln -c Release

# Or target a single project / framework
dotnet build src/BaanLib/BaanLib.csproj -c Release -f net8.0-windows

# Produce a NuGet package
dotnet pack src/BaanLib/BaanLib.csproj -c Release
```

The unit tests use an in-memory fake of the OLE object, so they run without a real Baan/Infor LN
installation (and on CI).

## License

[MIT](LICENSE). This is an independent .NET port of
[mfussenegger/baanlib](https://github.com/mfussenegger/baanlib) (also MIT).
