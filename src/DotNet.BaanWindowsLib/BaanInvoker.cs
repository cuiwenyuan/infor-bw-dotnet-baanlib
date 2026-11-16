using System;
using System.Runtime.InteropServices;

namespace Wangcaisoft.DotNet.BaanWindowsLib
{
    /// <summary>
    /// Shared execution engine for both call styles offered by this library:
    /// the chained style (<c>b.dll.func(args)</c>, see <see cref="BaanWrapper"/>) and the raw
    /// style (<c>b.ParseExecFunction("dll", "func(args)")</c>, see
    /// <see cref="Baan.ParseExecFunction"/>).
    /// </summary>
    /// <remarks>
    /// Keeping the whole "invoke + read <c>Error</c> + read <c>ReturnValue</c>" sequence in one
    /// place guarantees that both styles behave identically: the same serialization lock, the same
    /// <see cref="UnknownDllException"/>/<see cref="UnknownMethodException"/> mapping, the same
    /// <see cref="BaanComException"/> wrapping (with server diagnostics preserved) and the same
    /// <c>ThrowOnErrorResult</c> handling.
    /// </remarks>
    internal static class BaanInvoker
    {
        /// <summary>
        /// Executes <paramref name="functionCall"/> against <paramref name="dllName"/> on the
        /// underlying OLE automation object and returns its <c>ReturnValue</c>.
        /// </summary>
        /// <param name="baanObject">The underlying Baan/Infor LN OLE automation object.</param>
        /// <param name="dllName">The DLL (4GL object) name, e.g. <c>ottstpapihand</c>.</param>
        /// <param name="functionCall">
        /// The full Baan function call, e.g. <c>stpapi.put.field("session", "field", "value")</c>.
        /// </param>
        /// <param name="throwOnErrorResult">
        /// When <c>true</c>, a non-zero <c>"code|message"</c> return value throws
        /// <see cref="BaanResultException"/>.
        /// </param>
        /// <param name="syncRoot">The lock that serializes all access to <paramref name="baanObject"/>.</param>
        /// <returns>The raw <c>ReturnValue</c> produced by the call.</returns>
        internal static object Execute(
            dynamic baanObject,
            string dllName,
            string functionCall,
            bool throwOnErrorResult,
            object syncRoot)
        {
            object result;

            // Serialize the whole "invoke + read Error + read ReturnValue" sequence so that
            // concurrent threads cannot interleave and read each other's ReturnValue/Error.
            lock (syncRoot ?? new object())
            {
                try
                {
                    baanObject.ParseExecFunction(dllName, functionCall);

                    var error = (int)baanObject.Error;
                    if (error == -1)
                    {
                        throw new UnknownDllException();
                    }

                    if (error == -2)
                    {
                        throw new UnknownMethodException();
                    }

                    result = baanObject.ReturnValue;
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
                    string serverMessage = SafeRead(() => Convert.ToString(baanObject.ReturnValue));
                    if (string.IsNullOrEmpty(serverMessage))
                    {
                        serverMessage = SafeRead(() => Convert.ToString(baanObject.ReturnCall));
                    }

                    var hresult = ex is COMException comEx ? comEx.HResult : ex.HResult;

                    throw new BaanComException(
                        $"Call to {dllName}.{functionCall} failed: {ex.Message}",
                        hresult,
                        dllName,
                        functionCall,
                        serverMessage,
                        ex);
                }
            }

            if (throwOnErrorResult && result != null)
            {
                var parsed = BaanResult.Parse(result);
                if (!parsed.IsSuccess)
                {
                    throw new BaanResultException(parsed);
                }
            }

            return result;
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
