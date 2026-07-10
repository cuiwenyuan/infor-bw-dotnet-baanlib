using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace BaanLib
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

        internal BaanWrapper(dynamic baanObject, string name)
        {
            _baanObject = baanObject;
            _name = name;
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
            result = new BaanWrapper(_baanObject, _name + "." + binder.Name);
            return true;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            var method = GetCallingMethod(args);
            var dllName = DllName;

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
            return true;
        }
    }
}
