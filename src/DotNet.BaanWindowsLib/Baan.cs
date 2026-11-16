using System;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Threading;

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
    /// <para>
    /// All access to the underlying COM object is serialized through a dedicated lock
    /// (<c>_syncRoot</c>) so that concurrent threads cannot interleave calls and read each
    /// other's <c>ReturnValue</c>/<c>Error</c>. <see cref="Close"/> is fault-tolerant and uses a
    /// bounded wait so a hung/unreachable COM server cannot deadlock a caller or leak a zombie
    /// <c>bw</c> process.
    /// </para>
    /// </remarks>
    public class Baan : DynamicObject, IDisposable
    {
        private const int DefaultTimeout = 3600;

        // How long Close() will wait for the call lock before forcing a release. A hung COM
        // server must never block disposal indefinitely.
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

        private dynamic _baan;
        private bool _disposed;
        private bool _throwOnErrorResult;
        private readonly object _syncRoot = new object();

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
            get { lock (_syncRoot) return _baan.Timeout; }
            set { lock (_syncRoot) _baan.Timeout = value; }
        }

        /// <summary>Gets the return value of the last executed function.</summary>
        public object ReturnValue
        {
            get { lock (_syncRoot) return _baan.ReturnValue; }
        }

        /// <summary>Gets the last function call string.</summary>
        public object FunctionCall
        {
            get { lock (_syncRoot) return _baan.FunctionCall; }
        }

        /// <summary>Gets the last return call string.</summary>
        public object ReturnCall
        {
            get { lock (_syncRoot) return _baan.ReturnCall; }
        }

        /// <summary>Gets or sets the binary mode flag.</summary>
        public object Binary
        {
            get { lock (_syncRoot) return _baan.Binary; }
            set { lock (_syncRoot) _baan.Binary = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a call whose decoded return value has a non-zero
        /// code (the <c>"code|message"</c> convention) throws <see cref="BaanResultException"/>
        /// instead of returning the raw value. Default is <c>false</c> (non-breaking).
        /// </summary>
        public bool ThrowOnErrorResult
        {
            get => _throwOnErrorResult;
            set => _throwOnErrorResult = value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new BaanWrapper(_baan, binder.Name, _throwOnErrorResult, _syncRoot);
            return true;
        }

        /// <summary>
        /// Quits the underlying OLE automation object. Safe to call multiple times and from any
        /// thread. If the COM object cannot be reached (a hung call holds the lock, or the server
        /// is gone), the RCW is forcibly released so no zombie <c>bw</c> process or COM reference
        /// is leaked. Never throws.
        /// </summary>
        public void Close()
        {
            if (_disposed)
            {
                return;
            }

            // Bounded wait: if a call is in flight we wait briefly, but a hung server must not
            // block disposal forever.
            bool acquired = Monitor.TryEnter(_syncRoot, CloseTimeout);
            try
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                var baan = _baan;
                _baan = null;

                if (baan == null)
                {
                    return;
                }

                if (acquired)
                {
                    try
                    {
                        baan.Quit();
                    }
                    catch
                    {
                        // Best effort: a broken server may throw here; release the RCW below.
                    }
                }

                // Always release the RCW so a hung/unreachable server cannot leak a 'bw' process.
                ReleaseComObject(baan);
            }
            finally
            {
                if (acquired)
                {
                    Monitor.Exit(_syncRoot);
                }
            }
        }

        /// <summary>
        /// Releases the COM RCW for <paramref name="comObject"/> if it is a COM object. Swallows all
        /// errors so it is always safe to call from <see cref="Close"/>.
        /// </summary>
        private static void ReleaseComObject(object comObject)
        {
            try
            {
                if (comObject != null && Marshal.IsComObject(comObject))
                {
                    Marshal.FinalReleaseComObject(comObject);
                }
            }
            catch
            {
                // Ignore: there is nothing more we can do to release a broken RCW.
            }
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
