using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HotelManagement.Services
{
    public static class PerformanceTracker
    {
        private static readonly object SamplingSync = new object();
        private static readonly Dictionary<string, DateTime> LastSampleUtc =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly TimeSpan HighFrequencySampleWindow = TimeSpan.FromSeconds(20);

        public sealed class Scope : IDisposable
        {
            private readonly string _operation;
            private readonly Stopwatch _stopwatch;
            private readonly int _gc0Before;
            private readonly int _gc1Before;
            private readonly int _gc2Before;
            private readonly long _memoryBefore;
            private readonly Dictionary<string, object> _context;
            private bool _disposed;

            internal Scope(string operation, IDictionary<string, object> context)
            {
                _operation = string.IsNullOrWhiteSpace(operation) ? "unknown-operation" : operation.Trim();
                _gc0Before = GC.CollectionCount(0);
                _gc1Before = GC.CollectionCount(1);
                _gc2Before = GC.CollectionCount(2);
                _memoryBefore = GC.GetTotalMemory(false);
                _stopwatch = Stopwatch.StartNew();
                _context = context == null
                    ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, object>(context, StringComparer.OrdinalIgnoreCase);
            }

            public void AddContext(string key, object value)
            {
                if (string.IsNullOrWhiteSpace(key)) return;
                _context[key.Trim()] = value;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                _stopwatch.Stop();

                long memoryAfter = GC.GetTotalMemory(false);
                int gc0After = GC.CollectionCount(0);
                int gc1After = GC.CollectionCount(1);
                int gc2After = GC.CollectionCount(2);
                long memoryDelta = memoryAfter - _memoryBefore;
                int gc0Delta = gc0After - _gc0Before;
                int gc1Delta = gc1After - _gc1Before;
                int gc2Delta = gc2After - _gc2Before;
                long elapsedMs = _stopwatch.ElapsedMilliseconds;

                if (!ShouldEmitPerfScope(_operation, elapsedMs, memoryDelta, gc0Delta, gc1Delta, gc2Delta))
                    return;

                _context["ElapsedMs"] = elapsedMs;
                _context["MemoryDeltaBytes"] = memoryDelta;
                _context["Gc0Delta"] = gc0Delta;
                _context["Gc1Delta"] = gc1Delta;
                _context["Gc2Delta"] = gc2Delta;
                _context["ThreadId"] = Environment.CurrentManagedThreadId;

                AppLogger.Info("PerfScope: " + _operation, _context);
            }
        }

        public static Scope Measure(string operation, IDictionary<string, object> context = null)
        {
            return new Scope(operation, context);
        }

        private static bool ShouldEmitPerfScope(
            string operation,
            long elapsedMs,
            long memoryDelta,
            int gc0Delta,
            int gc1Delta,
            int gc2Delta)
        {
            if (IsVerbosePerfLogEnabled()) return true;
            if (!TryGetHighFrequencyThreshold(operation, out long thresholdMs))
                return true;

            if (elapsedMs >= thresholdMs) return true;
            if (gc0Delta > 0 || gc1Delta > 0 || gc2Delta > 0) return true;
            if (Math.Abs(memoryDelta) >= 1024 * 1024) return true;

            DateTime nowUtc = DateTime.UtcNow;
            lock (SamplingSync)
            {
                if (LastSampleUtc.TryGetValue(operation, out var lastUtc) &&
                    (nowUtc - lastUtc) < HighFrequencySampleWindow)
                {
                    return false;
                }

                LastSampleUtc[operation] = nowUtc;
            }

            return true;
        }

        private static bool IsVerbosePerfLogEnabled()
        {
            string raw = Environment.GetEnvironmentVariable("APP_PERF_LOG_VERBOSE");
            if (string.IsNullOrWhiteSpace(raw)) return false;
            raw = raw.Trim();
            return string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetHighFrequencyThreshold(string operation, out long thresholdMs)
        {
            thresholdMs = 0;
            if (string.IsNullOrWhiteSpace(operation)) return false;

            if (operation.StartsWith("MainForm.CheckRoomMapChanges", StringComparison.OrdinalIgnoreCase))
            {
                thresholdMs = 350;
                return true;
            }

            if (operation.StartsWith("MainForm.CheckBookingStatisticsChanges", StringComparison.OrdinalIgnoreCase))
            {
                thresholdMs = 500;
                return true;
            }

            if (operation.StartsWith("MainForm.RefreshRoomBillingSnapshots", StringComparison.OrdinalIgnoreCase))
            {
                thresholdMs = 250;
                return true;
            }

            return false;
        }
    }
}
