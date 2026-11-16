using System;

namespace Wangcaisoft.DotNet.BaanWindowsLib
{
    /// <summary>
    /// Raised when the underlying Baan/Infor LN OLE automation object reports that the
    /// referenced DLL (object) is unknown (<c>Error == -1</c>).
    /// </summary>
    public class UnknownDllException : Exception
    {
        public UnknownDllException()
        {
        }

        public UnknownDllException(string message)
            : base(message)
        {
        }

        public UnknownDllException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Raised when the underlying Baan/Infor LN OLE automation object reports that the
    /// referenced method is unknown (<c>Error == -2</c>).
    /// </summary>
    public class UnknownMethodException : Exception
    {
        public UnknownMethodException()
        {
        }

        public UnknownMethodException(string message)
            : base(message)
        {
        }

        public UnknownMethodException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Raised when <see cref="Baan.ThrowOnErrorResult"/> is enabled and a call returns a
    /// <c>"code|message"</c> value whose code is non-zero (i.e. not a success per the convention).
    /// </summary>
    public class BaanResultException : Exception
    {
        /// <summary>Gets the non-zero status code carried by the return value.</summary>
        public int Code { get; }

        /// <summary>
        /// Initializes a new <see cref="BaanResultException"/> from a parsed <see cref="BaanResult"/>.
        /// </summary>
        public BaanResultException(BaanResult result)
            : base(result.Message)
        {
            Code = result.Code;
        }

        /// <summary>
        /// Initializes a new <see cref="BaanResultException"/> from a code and message.
        /// </summary>
        public BaanResultException(int code, string message)
            : base(message)
        {
            Code = code;
        }

        /// <summary>
        /// Initializes a new <see cref="BaanResultException"/> from a code, message and inner exception.
        /// </summary>
        public BaanResultException(int code, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }
    }

    /// <summary>
    /// Raised when a call to the underlying Baan/Infor LN OLE automation object fails at the COM
    /// layer (for example a <see cref="System.Runtime.InteropServices.COMException"/> thrown by
    /// <c>ParseExecFunction</c>).
    /// </summary>
    /// <remarks>
    /// Unlike a bare <see cref="System.Runtime.InteropServices.COMException"/>, this exception
    /// preserves the context needed to diagnose the failure without losing information:
    /// <list type="bullet">
    ///   <item><description>The original COM <see cref="Exception.HResult"/> is kept on the base
    ///     <see cref="Exception"/> (so it can be matched against known RPC failure codes).</description></item>
    ///   <item><description>The <see cref="DllName"/> and <see cref="Method"/> identify the call.</description></item>
    ///   <item><description>Any server-supplied diagnostic text that could be recovered after the
    ///     failure (typically from <c>ReturnValue</c> / <c>ReturnCall</c>) is exposed via
    ///     <see cref="ServerMessage"/>; it may be empty if it could not be read.</description></item>
    ///   <item><description>The original exception is preserved as <see cref="Exception.InnerException"/>.</description></item>
    /// </list>
    /// </remarks>
    public class BaanComException : Exception
    {
        /// <summary>Gets the DLL (object) name that was being invoked when the failure occurred.</summary>
        public string DllName { get; }

        /// <summary>Gets the method that was being invoked when the failure occurred.</summary>
        public string Method { get; }

        /// <summary>
        /// Gets any server-supplied diagnostic message recovered after the failure
        /// (e.g. from <c>ReturnValue</c>). May be empty if it could not be read.
        /// </summary>
        public string ServerMessage { get; }

        /// <summary>
        /// Initializes a new <see cref="BaanComException"/>.
        /// </summary>
        /// <param name="message">A human-readable description of the failure.</param>
        /// <param name="hresult">The COM <see cref="Exception.HResult"/> of the underlying failure.</param>
        /// <param name="dllName">The DLL (object) name that was being invoked.</param>
        /// <param name="method">The method that was being invoked.</param>
        /// <param name="serverMessage">Any recovered server-supplied diagnostic message (may be null).</param>
        /// <param name="innerException">The original exception (e.g. the <see cref="System.Runtime.InteropServices.COMException"/>).</param>
        public BaanComException(string message, int hresult, string dllName, string method, string serverMessage, Exception innerException)
            : base(message, innerException)
        {
            // Keep the original COM HResult on the base Exception so callers can still inspect it.
            HResult = hresult;
            DllName = dllName ?? string.Empty;
            Method = method ?? string.Empty;
            ServerMessage = serverMessage ?? string.Empty;
        }
    }
}
