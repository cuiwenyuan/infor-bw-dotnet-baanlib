using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace Wangcaisoft.DotNet.BaanWindowsLib
{
    /// <summary>
    /// Optional, standalone resilience helpers for the Baan/Infor LN OLE automation scenario.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is <b>not</b> used by the core <see cref="Baan"/> wrapper automatically. It
    /// contains higher-level, business-oriented recovery logic (RPC service recovery, instance
    /// creation retries, zombie <c>bw</c> process cleanup) that is useful in production hosts but
    /// would be out of place inside a dependency-free OLE wrapper. Wire it in from your own code
    /// (for example, call <see cref="CreateInstanceWithRetry"/> before constructing a
    /// <see cref="Baan"/>, or run <see cref="EnsureRpcRunning"/> / <see cref="KillZombieBwProcesses"/>
    /// from a watchdog).
    /// </para>
    /// <para>Windows only (depends on <c>System.ServiceProcess</c> and process management).</para>
    /// </remarks>
    public static class BaanResilienceHelper
    {
        // Well-known COM/RPC failure codes that indicate the Baan client (bw) or the RPC
        // infrastructure is unavailable rather than a genuine application error.
        // 0x800706BA = RPC_S_SERVER_UNAVAILABLE
        // 0x800706BE = RPC_S_CALL_FAILED
        // 0x80010108 = RPC_E_DISCONNECTED
        private const int RpcServerUnavailable = -2147023174;
        private const int RpcCallFailed = -2147023170;
        private const int RpcDisconnected = -2147417848;

        private static bool IsWindows() => Environment.OSVersion.Platform == PlatformID.Win32NT;

        /// <summary>
        /// Returns a value indicating whether the given exception represents an RPC/connectivity
        /// failure (as opposed to a genuine Baan application error).
        /// </summary>
        public static bool IsRpcFailure(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            int hr = ex is COMException com ? com.HResult : ex.HResult;
            return hr == RpcServerUnavailable || hr == RpcCallFailed || hr == RpcDisconnected;
        }

        /// <summary>
        /// A snapshot of the Baan client environment, returned by <see cref="CheckServerAvailability"/>.
        /// </summary>
        public readonly struct BaanServerStatus
        {
            /// <summary>Gets a value indicating whether the ProgID could be resolved.</summary>
            public bool ProgIdResolved { get; }

            /// <summary>Gets a value indicating whether the Windows RPC service (RpcSs) is running.</summary>
            public bool RpcRunning { get; }

            /// <summary>Gets the number of running <c>bw</c> processes.</summary>
            public int BwProcessCount { get; }

            /// <summary>Gets a human-readable diagnostic message.</summary>
            public string Message { get; }

            /// <summary>
            /// Gets a value indicating whether the server is considered available
            /// (ProgID resolved and RPC running).
            /// </summary>
            public bool IsAvailable => ProgIdResolved && RpcRunning;

            public BaanServerStatus(bool progIdResolved, bool rpcRunning, int bwProcessCount, string message)
            {
                ProgIdResolved = progIdResolved;
                RpcRunning = rpcRunning;
                BwProcessCount = bwProcessCount;
                Message = message ?? string.Empty;
            }
        }

        /// <summary>
        /// Probes the Baan client environment: resolves the ProgID, checks the Windows RPC service
        /// (RpcSs), and counts running <c>bw</c> processes. Never throws.
        /// </summary>
        public static BaanServerStatus CheckServerAvailability(string progId)
        {
            if (!IsWindows())
            {
                return new BaanServerStatus(false, false, 0, "Not running on Windows.");
            }

            try
            {
                bool progIdResolved = Type.GetTypeFromProgID(progId) != null;
                if (!progIdResolved)
                {
                    return new BaanServerStatus(false, false, 0, $"Failed to resolve Baan ProgID: {progId}");
                }

                bool rpcRunning;
                try
                {
                    using var sc = new ServiceController("RpcSs");
                    rpcRunning = sc.Status == ServiceControllerStatus.Running;
                    if (!rpcRunning)
                    {
                        EnsureRpcRunning();
                    }
                }
                catch (Exception ex)
                {
                    return new BaanServerStatus(true, false, 0, $"Windows RPC service (RpcSs) check failed: {ex.Message}");
                }

                int bwCount = 0;
                try
                {
                    bwCount = Process.GetProcessesByName("bw").Length;
                }
                catch
                {
                    // Counting is best-effort; ignore failures.
                }

                return new BaanServerStatus(true, rpcRunning, bwCount, "Server available.");
            }
            catch (Exception ex)
            {
                return new BaanServerStatus(false, false, 0, $"Failed to check Baan server availability: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts the Windows RPC service (RpcSs) if it is not already running. Never throws.
        /// </summary>
        public static void EnsureRpcRunning()
        {
            try
            {
                using var sc = new ServiceController("RpcSs");
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
            }
            catch
            {
                // The caller decides how to react; failure here must not be fatal.
            }
        }

        /// <summary>
        /// Kills <c>bw</c> processes once their count exceeds <paramref name="threshold"/>. This is a
        /// destructive operation intended for cleaning up zombie processes after repeated RPC
        /// failures; call it only from a supervised watchdog. Stops at <paramref name="maxToKill"/>
        /// to avoid taking down legitimate sessions. Never throws.
        /// </summary>
        public static int KillZombieBwProcesses(int threshold = 10, int maxToKill = 5)
        {
            if (!IsWindows() || threshold <= 0 || maxToKill <= 0)
            {
                return 0;
            }

            int killed = 0;
            try
            {
                var processes = Process.GetProcessesByName("bw");
                if (processes.Length <= threshold)
                {
                    foreach (var p in processes)
                    {
                        p.Dispose();
                    }

                    return 0;
                }

                foreach (var process in processes)
                {
                    if (killed >= maxToKill)
                    {
                        process.Dispose();
                        continue;
                    }

                    try
                    {
                        process.Kill();
                        killed++;
                    }
                    catch
                    {
                        // Best effort: a process may have exited already.
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {
                // Ignore; cleanup is best-effort.
            }

            return killed;
        }

        /// <summary>
        /// Creates the OLE automation object for <paramref name="progId"/> with retry and exponential
        /// backoff. On an RPC/connectivity failure it attempts to recover the RPC service and retries.
        /// </summary>
        /// <param name="progId">The Baan ProgID (e.g. <c>Baan.Application.erpln</c>).</param>
        /// <param name="maxRetries">Maximum number of creation attempts. Defaults to 3.</param>
        /// <param name="initialDelayMs">Initial delay between retries; doubles after each attempt.</param>
        /// <returns>The created OLE object.</returns>
        /// <exception cref="COMException">Thrown (with the last failure as inner) if all attempts fail.</exception>
        public static object CreateInstanceWithRetry(string progId, int maxRetries = 3, int initialDelayMs = 1000)
        {
            if (progId == null)
            {
                throw new ArgumentNullException(nameof(progId));
            }

            Exception lastException = null;
            int delay = initialDelayMs;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var type = Type.GetTypeFromProgID(progId);
                    if (type == null)
                    {
                        throw new COMException($"Could not retrieve a type for ProgID '{progId}'.");
                    }

                    var instance = Activator.CreateInstance(type);
                    if (instance == null)
                    {
                        throw new COMException($"Activator.CreateInstance returned null for ProgID '{progId}'.");
                    }

                    return instance;
                }
                catch (COMException ex)
                {
                    lastException = ex;
                    if (attempt < maxRetries - 1)
                    {
                        EnsureRpcRunning();
                        Thread.Sleep(delay);
                        delay *= 2;
                    }
                }
            }

            throw new COMException(
                $"Failed to create Baan instance for ProgID '{progId}' after {maxRetries} attempt(s).",
                lastException);
        }
    }
}
