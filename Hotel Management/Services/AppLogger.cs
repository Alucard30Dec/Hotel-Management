using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using HotelManagement.Data;

namespace HotelManagement.Services
{
    public static class AppLogger
    {
        private static readonly object Sync = new object();
        private static readonly string LogsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static DateTime _currentLogDate = DateTime.MinValue.Date;
        private static string _currentLogFilePath;

        public static void Info(string message, IDictionary<string, object> context = null)
        {
            Write("INFO", message, null, context);
        }

        public static void Warn(string message, IDictionary<string, object> context = null)
        {
            Write("WARN", message, null, context);
        }

        public static void Error(Exception ex, string message, IDictionary<string, object> context = null)
        {
            Write("ERROR", message, ex, context);
        }

        private static void Write(string level, string message, Exception ex, IDictionary<string, object> context)
        {
            try
            {
                DateTime now = DateTime.Now;
                string correlationId = AuditContext.ResolveCorrelationId(null) ?? string.Empty;

                var sb = new StringBuilder(512);
                sb.Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                  .Append(" [").Append(level ?? "INFO").Append("] ")
                  .Append("[corr=").Append(string.IsNullOrWhiteSpace(correlationId) ? "-" : correlationId).Append("] ")
                  .Append(message ?? string.Empty);

                if (context != null)
                {
                    foreach (var kv in context)
                    {
                        if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                        sb.Append(" | ").Append(kv.Key.Trim()).Append('=').Append(FormatValue(kv.Value));
                    }
                }

                if (ex != null)
                {
                    sb.AppendLine();
                    sb.Append(ex);
                }

                lock (Sync)
                {
                    string filePath = ResolveLogFilePath(now);
                    File.AppendAllText(filePath, sb.ToString() + Environment.NewLine, Encoding.UTF8);
                }

                Debug.WriteLine(sb.ToString());
            }
            catch
            {
                // Never throw from logger.
            }
        }

        private static string ResolveLogFilePath(DateTime now)
        {
            DateTime date = now.Date;
            if (string.IsNullOrWhiteSpace(_currentLogFilePath) || date != _currentLogDate)
            {
                Directory.CreateDirectory(LogsDir);
                _currentLogDate = date;
                _currentLogFilePath = Path.Combine(
                    LogsDir,
                    "app-" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
            }

            return _currentLogFilePath;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "null";
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            if (value is decimal dec)
                return dec.ToString("0.##", CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}
