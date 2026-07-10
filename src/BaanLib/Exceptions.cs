using System;

namespace BaanLib
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
}
