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
}
