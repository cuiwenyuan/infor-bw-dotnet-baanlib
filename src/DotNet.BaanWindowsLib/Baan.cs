using System;
using System.Dynamic;
using System.Runtime.InteropServices;

namespace Wangcaisoft.DotNet.BaanWindowsLib
{
    /// <summary>
    /// A simple wrapper around the Baan/Infor LN OLE automation object
    /// (<c>Baan.Application.*</c>) that makes OLE automation easier to use from .NET.
    /// </summary>
    /// <remarks>
    /// Attribute access for any name that is not one of the well-known COM properties
    /// (<see cref="Timeout"/>, <see cref="ReturnValue"/>, <see cref="FunctionCall"/>,
    /// <see cref="ReturnCall"/>, <see cref="Binary"/>) starts a chain that is finally executed
    /// when invoked, for example:
    /// <c>baan.ottstpapihand.stpapi.put.field("session", "field", "value")</c>.
    /// </remarks>
    public class Baan : DynamicObject, IDisposable
    {
        private const int DefaultTimeout = 3600;

        private dynamic _baan;
        private bool _disposed;

        /// <summary>
        /// Creates a new Baan/Infor LN OLE automation object for the given ProgID
        /// (e.g. <c>Baan.Application.erpln</c>) and sets the default timeout.
        /// </summary>
        public Baan(string progId)
            : this(progId, CreateBaanObject(progId))
        {
        }

        /// <summary>
        /// Creates a <see cref="Baan"/> that wraps an already-created OLE object. Primarily
        /// intended for testing and advanced scenarios.
        /// </summary>
        public Baan(string progId, object baanInstance)
        {
            _baan = baanInstance;
            _baan.Timeout = DefaultTimeout;
        }

        private static object CreateBaanObject(string progId)
        {
            var type = Type.GetTypeFromProgID(progId);
            if (type == null)
            {
                throw new COMException(
                    $"Could not retrieve a type for ProgID '{progId}'. " +
                    "Make sure the Baan/Infor LN client is installed and registered.");
            }

            return Activator.CreateInstance(type);
        }

        /// <summary>Gets or sets the OLE automation timeout (seconds).</summary>
        public object Timeout
        {
            get => _baan.Timeout;
            set => _baan.Timeout = value;
        }

        /// <summary>Gets the return value of the last executed function.</summary>
        public object ReturnValue => _baan.ReturnValue;

        /// <summary>Gets the last function call string.</summary>
        public object FunctionCall => _baan.FunctionCall;

        /// <summary>Gets the last return call string.</summary>
        public object ReturnCall => _baan.ReturnCall;

        /// <summary>Gets or sets the binary mode flag.</summary>
        public object Binary
        {
            get => _baan.Binary;
            set => _baan.Binary = value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new BaanWrapper(_baan, binder.Name);
            return true;
        }

        /// <summary>
        /// Quits the underlying OLE automation object. Safe to call multiple times.
        /// </summary>
        public void Close()
        {
            if (_disposed)
            {
                return;
            }

            if (_baan != null)
            {
                _baan.Quit();
                _baan = null;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        ~Baan()
        {
            Close();
        }
    }
}
