# Plan: .NET port of `mfussenegger/baanlib` — repo `infor-bw-dotnet-baanlib`

> A .NET (Windows-only) port of the Python [`mfussenegger/baanlib`](https://github.com/mfussenegger/baanlib)
> OLE automation wrapper for **Baan / Infor LN**, with API semantics kept 1:1 with the upstream.

## Targets & status

- **Target Frameworks (TFM):** `net462;net6.0-windows;net8.0-windows;net10.0-windows`
  (added `net10.0-windows` on 2026-07-11).
- **Unit tests:** xUnit, mirroring upstream `tests.py` (fake-based, no Baan client required).
- **CI/CD:** GitHub Actions on `windows-latest` (build + test on .NET 6 / 8 / 10 SDKs).
- **License:** MIT.
- **Repo:** `https://github.com/cuiwenyuan/infor-bw-dotnet-baanlib` (public).

## What we are porting

`baanlib` (Python/win32com) core semantics, from upstream `baanlib.py` v0.1.5:

- `Baan(progId)` — creates the OLE object (`Baan.Application.erpln`), sets `Timeout = 3600`.
- Context-manager (`using`) → on exit calls `Close()` → `Quit()`.
- Known COM properties return their value, **not** a wrapper:
  `Timeout` (get/set), `ReturnValue`, `FunctionCall`, `ReturnCall`, `Binary` (get/set).
- Any other attribute starts a chain: `b.dll.func.name` builds a `BaanWrapper`.
- On final call `b.dll.func.name(a, 1)` it builds `func.name("a", 1)`
  (**numbers unquoted, strings wrapped in double quotes**), then calls
  `ParseExecFunction(dllName, methodString)`.
- After the call it reads `Error`: `-1 → UnknownDllException`, `-2 → UnknownMethodException`,
  otherwise returns `ReturnValue`.
- Exceptions: `UnknownDllException`, `UnknownMethodException`.

### .NET approach

A `DynamicObject` (`TryGetMember` for chaining, `TryInvoke` for the call) wrapping a COM object
created via `Type.GetTypeFromProgID(progId)` + `Activator.CreateInstance`. This works identically
on all four TFMs with **no `#if` branching**, so the whole library is a single multi-targeted project.

## Repository structure

```
infor-bw-dotnet-baanlib/          (repo root)
├── .gitignore
├── LICENSE                       (MIT, full text)
├── README.md                     (English; mirrors upstream + .NET usage)
├── PLAN.md                       (this file)
├── DotNet.BaanWindowsLib.sln                   (classic .sln — understood by .NET 8 SDK)
├── .github/
│   └── workflows/ci.yml          (build + test on windows-latest, .NET 6/8/10)
├── src/
│   └── DotNet.BaanWindowsLib/
│       ├── DotNet.BaanWindowsLib.csproj        (multi-targeted, NuGet metadata)
│       ├── Baan.cs               (Baan : DynamicObject, IDisposable)
│       ├── BaanWrapper.cs        (BaanWrapper : DynamicObject)
│       └── Exceptions.cs         (UnknownDllException, UnknownMethodException)
└── tests/
    └── DotNet.BaanWindowsLib.Tests/
        ├── DotNet.BaanWindowsLib.Tests.csproj
        ├── FakeBaan.cs           (test double: Timeout/ParseExecFunction/Error/ReturnValue/...)
        └── BaanTests.cs          (mirror of upstream tests.py)
```

## API surface (semantic parity with upstream)

```csharp
namespace Wangcaisoft.DotNet.BaanWindowsLib
{
    public class Baan : DynamicObject, IDisposable
    {
        public Baan(string progId);                       // creates COM instance, Timeout=3600
        public Baan(string progId, object baanInstance);  // test/advanced hook
        public void Close();                              // calls Quit()
        public void Dispose();                            // -> Close()
        public object Timeout { get; set; }
        public object ReturnValue { get; }
        public object FunctionCall { get; }
        public object ReturnCall { get; }
        public object Binary { get; set; }
        // TryGetMember -> returns BaanWrapper for any unknown name
    }

    public class BaanWrapper : DynamicObject
    {
        public string DllName { get; }        // first segment of the chained path
        public string MethodName { get; }     // remaining segments, joined by '.'
        public string GetCallingMethod(params object[] args); // builds the call string
        // TryGetMember -> new BaanWrapper (extend chain)
        // TryInvoke   -> build string, call ParseExecFunction, check Error, return ReturnValue
    }

    public class UnknownDllException : Exception { }
    public class UnknownMethodException : Exception { }
}
```

## Build / test notes

- `Microsoft.CSharp` PackageReference is required for the `dynamic` keyword under SDK-style `net462`.
- `Microsoft.NETFramework.ReferenceAssemblies` lets `net462` build on a Core-SDK-only machine/CI.
- `Microsoft.NET.Test.Sdk` is pinned to a version that ships a .NET 10 test host (17.12.0+).
- Tests use an in-memory `FakeBaan`, so they run without a real Baan/Infor LN install (and on CI).

## Changelog

- **2026-07-11 (d)** — Naming split correction: the **NuGet package / assembly / namespaces** are
  `Wangcaisoft.DotNet.BaanWindowsLib`, while the **on-disk directories, `.sln` and `.csproj` filenames**
  are `DotNet.BaanWindowsLib` (the `Wangcaisoft.` prefix is dropped from file names). The `.sln`, CI,
  README and this PLAN tree were updated accordingly.
- **2026-07-11 (c)** — Renamed the repository directories and solution: `src/BaanLib` →
  `src/DotNet.BaanWindowsLib`, `tests/BaanLib.Tests` → `tests/DotNet.BaanWindowsLib.Tests`,
  `BaanLib.sln` → `DotNet.BaanWindowsLib.sln`, and the `.csproj` files renamed to match their folders.
- **2026-07-11 (b)** — Renamed the NuGet package / assembly / namespaces from `BaanLib` to
  `Wangcaisoft.DotNet.BaanWindowsLib` (`PackageId`/`AssemblyName`/`RootNamespace` in the lib csproj; source
  `namespace` declarations; test `using`/`RootNamespace`; README install/usage refs; added a
  `dotnet pack` step to CI). Repository URL unchanged (`cuiwenyuan/infor-bw-dotnet-baanlib`).
- **2026-07-11** — Added `net10.0-windows` to both projects; CI now also installs the .NET 10 SDK;
  repository URLs updated to `cuiwenyuan/infor-bw-dotnet-baanlib`.
- **2026-07-10** — Initial implementation: multi-targeted library, xUnit suite, CI, MIT license.
