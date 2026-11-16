using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Wangcaisoft.DotNet.BaanWindowsLib
{
    /// <summary>
    /// Wraps a single chained attribute path (for example
    /// <c>ottstpapihand.stpapi.put.field</c>) and, when invoked, builds the Baan function-call
    /// string and executes it through the underlying OLE automation object.
    /// </summary>
    /// <remarks>
    /// This mirrors the upstream Python <c>BaanWrapper</c>. Attribute access extends the chain;
    /// invocation (<c>__call__</c>) builds the method string, calls
    /// <c>ParseExecFunction(dllName, method)</c>, inspects <c>Error</c> and returns
    /// <c>ReturnValue</c>.
    /// </remarks>
    public class BaanWrapper : DynamicObject
    {
        private readonly dynamic _baanObject;
        private readonly string _name;
        private readonly bool _throwOnErrorResult;
        private readonly object _syncRoot;

        internal BaanWrapper(dynamic baanObject, string name)
        {
            _baanObject = baanObject;
            _name = name;
            _throwOnErrorResult = false;
            _syncRoot = new object();
        }

        internal BaanWrapper(dynamic baanObject, string name, bool throwOnErrorResult)
        {
            _baanObject = baanObject;
            _name = name;
            _throwOnErrorResult = throwOnErrorResult;
            _syncRoot = new object();
        }

        internal BaanWrapper(dynamic baanObject, string name, bool throwOnErrorResult, object syncRoot)
        {
            _baanObject = baanObject;
            _name = name;
            _throwOnErrorResult = throwOnErrorResult;
            _syncRoot = syncRoot ?? new object();
        }

        /// <summary>Gets the DLL (object) name, i.e. the first segment of the chained path.</summary>
        public string DllName => _name.Split('.')[0];

        /// <summary>Gets the method name, i.e. everything after the first segment, joined by dots.</summary>
        public string MethodName => string.Join(".", _name.Split('.').Skip(1));

        /// <summary>
        /// Builds the Baan function invocation string for the given arguments, mirroring the
        /// upstream Python <c>_get_calling_method</c>: numbers are emitted verbatim, everything
        /// else is wrapped in double quotes.
        /// </summary>
        public string GetCallingMethod(params object[] args)
        {
            var parts = args.Select(FormatArgument).ToArray();
            return MethodName + "(" + string.Join(", ", parts) + ")";
        }

        private static string FormatArgument(object arg)
        {
            switch (arg)
            {
                case null:
                    return "null";
                case bool b:
                    return b ? "True" : "False";
                case string s:
                    return "\"" + s + "\"";
                case float f:
                    return FormatNumber(f);
                case double d:
                    return FormatNumber(d);
                case decimal m:
                    return FormatNumber((double)m);
                case var _ when IsInteger(arg):
                    return Convert.ToString(arg, CultureInfo.InvariantCulture);
                default:
                    // Non-numeric object: wrap its string representation in quotes (matches Python).
                    return "\"" + arg + "\"";
            }
        }

        private static bool IsInteger(object arg)
        {
            return arg is sbyte or byte or short or ushort or int or uint
                or long or ulong;
        }

        private static string FormatNumber(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return value.ToString("R", CultureInfo.InvariantCulture);
            }

            // Whole numbers keep a trailing ".0" to mirror Python's str().
            if (value == Math.Floor(value))
            {
                return value.ToString("0.0", CultureInfo.InvariantCulture);
            }

            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new BaanWrapper(_baanObject, _name + "." + binder.Name, _throwOnErrorResult, _syncRoot);
            return true;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = null;

            var method = GetCallingMethod(args);
            var dllName = DllName;

            // Serialize the whole "invoke + read Error + read ReturnValue" sequence so that
            // concurrent threads cannot interleave and read each other's ReturnValue/Error.
            lock (_syncRoot)
            {
                try
                {
                    _baanObject.ParseExecFunction(dllName, method);

                    var error = (int)_baanObject.Error;
                    if (error == -1)
                    {
                        throw new UnknownDllException();
                    }

                    if (error == -2)
                    {
                        throw new UnknownMethodException();
                    }

                    result = _baanObject.ReturnValue;
                }
                catch (UnknownDllException)
                {
                    throw;
                }
                catch (UnknownMethodException)
                {
                    throw;
                }
                    catch (Exception ex)
                    {
                        // Capture any server-supplied diagnostic text (typically ReturnValue) so the
                        // failure context is not lost. Each read is guarded because a hung/broken COM
                        // server may throw again, which must never mask the original exception.
                        // An empty result is treated as "no message" and we fall back to ReturnCall.
                        string serverMessage = SafeRead(() => Convert.ToString(_baanObject.ReturnValue));
                        if (string.IsNullOrEmpty(serverMessage))
                        {
                            serverMessage = SafeRead(() => Convert.ToString(_baanObject.ReturnCall));
                        }

                        var hresult = ex is COMException comEx ? comEx.HResult : ex.HResult;

                        throw new BaanComException(
                            $"Call to {dllName}.{method} failed: {ex.Message}",
                            hresult,
                            dllName,
                            method,
                            serverMessage,
                            ex);
                    }
            }

            if (_throwOnErrorResult && result != null)
            {
                var parsed = BaanResult.Parse(result);
                if (!parsed.IsSuccess)
                {
                    throw new BaanResultException(parsed);
                }
            }

            return true;
        }

        /// <summary>
        /// Runs <paramref name="reader"/> and returns its result, or <c>null</c> if it throws
        /// (e.g. because the COM server is unreachable). Used to recover diagnostic information
        /// after a failure without ever masking the original exception.
        /// </summary>
        private static T SafeRead<T>(Func<T> reader)
        {
            try
            {
                return reader();
            }
            catch
            {
                return default;
            }
        }
    }
}
