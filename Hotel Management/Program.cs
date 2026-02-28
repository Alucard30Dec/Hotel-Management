using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data.Entity;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hotel_Management.Migrations;
using HotelManagement.Data;
using HotelManagement.Forms; // namespace chứa MainForm
using HotelManagement.Services;
using MySql.Data.MySqlClient;

namespace HotelManagement
{
    internal static class Program
    {
        private static int _fatalErrorShown;
        private static int _startupMaintenanceScheduled;
        private static bool _deferDatabaseInitialize;
        private static readonly TimeSpan StartupMaintenanceDelay = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan StartupMaintenanceInterval = TimeSpan.FromHours(12);
        private const string StartupMaintenanceStampFileName = "startup-maintenance.stamp";

        [STAThread]
        static void Main(string[] args)
        {
            using (PerformanceTracker.Measure("Program.Main.TotalStartup"))
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (sender, eventArgs) =>
                {
                    AppLogger.Error(eventArgs.Exception, "Unhandled UI thread exception.");
                    MessageBox.Show(
                        "Có lỗi phát sinh trong quá trình chạy. Thao tác hiện tại sẽ dừng, vui lòng thử lại.",
                        "Lỗi runtime",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                };
                AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    var ex = eventArgs.ExceptionObject as Exception
                        ?? new Exception(eventArgs.ExceptionObject?.ToString() ?? "Unknown unhandled exception.");
                    AppLogger.Error(ex, "Unhandled non-UI exception.");
                    ShowFatalStartupError(ex, "Loi runtime");
                };

                try
                {
                    using (AuditContext.BeginCorrelationScope("startup-" + Guid.NewGuid().ToString("N")))
                    {
                        using (PerformanceTracker.Measure("Program.Main.Bootstrap"))
                        {
                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);

                            if (TryRunLegacyMigration(args))
                                return;

                            Database.SetInitializer(new MigrateDatabaseToLatestVersion<HotelDbContext, Configuration>());
                            _deferDatabaseInitialize = CanDeferDatabaseInitialize();
                            if (!_deferDatabaseInitialize)
                            {
                                EnsureDatabaseInitialized();
                            }
                            EnsureStatisticsSchema(interactiveWarning: false);

                            var mainForm = new MainForm();
                            mainForm.Shown += MainForm_Shown;
                            Application.Run(mainForm);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowFatalStartupError(ex, "Loi khoi dong");
                }
            }
        }

        private static void ShowFatalStartupError(Exception ex, string title)
        {
            if (System.Threading.Interlocked.Exchange(ref _fatalErrorShown, 1) == 1)
                return;

            AppLogger.Error(ex, "Fatal startup/runtime error.", new Dictionary<string, object>
            {
                ["Title"] = title ?? string.Empty
            });

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-error.log");
            string details = ex?.ToString() ?? "Unknown exception.";

            try
            {
                File.AppendAllText(
                    logPath,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]\r\n" + details + "\r\n\r\n");
            }
            catch
            {
                AppLogger.Warn("Cannot append startup-error.log.");
            }

            MessageBox.Show(
                "Ứng dụng không thể tiếp tục do lỗi hệ thống.\n\n" +
                "Log da duoc ghi tai:\n" + logPath,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static bool TryRunLegacyMigration(string[] args)
        {
            if (args == null || args.Length == 0) return false;

            bool shouldRun = args.Any(a =>
                string.Equals(a, "--migrate-legacy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a, "/migrate-legacy", StringComparison.OrdinalIgnoreCase));
            if (!shouldRun) return false;

            try
            {
                var service = new LegacyDataMigrationService();
                var result = service.RunOnce();
                MessageBox.Show(
                    result.ToSummaryText(),
                    "Legacy Migration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Legacy migration failed.");
                MessageBox.Show(
                    "Legacy migration failed.\n\n" + ex.Message,
                    "Legacy Migration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return true;
        }

        private static void MainForm_Shown(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _startupMaintenanceScheduled, 1) == 1)
                return;

            _ = RunStartupMaintenanceAsync();
        }

        private static async Task RunStartupMaintenanceAsync()
        {
            try
            {
                await Task.Delay(StartupMaintenanceDelay).ConfigureAwait(false);

                if (_deferDatabaseInitialize)
                {
                    EnsureDatabaseInitialized();
                    _deferDatabaseInitialize = false;
                }

                if (!ShouldRunStartupMaintenance()) return;

                using (PerformanceTracker.Measure("Program.StartupMaintenance.Background"))
                {
                    EnsureStatisticsSchema(interactiveWarning: false);
                    EnsurePerformanceIndexes();
                }

                MarkStartupMaintenanceCompleted();
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Startup maintenance background task failed.", new Dictionary<string, object>
                {
                    ["Error"] = ex.Message
                });
            }
        }

        private static void EnsureDatabaseInitialized()
        {
            using (PerformanceTracker.Measure("Program.Main.DatabaseInitialize"))
            {
                using (var db = new HotelDbContext())
                {
                    db.Database.Initialize(false);
                }
            }
        }

        private static bool CanDeferDatabaseInitialize()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    const string sql = @"SELECT COUNT(1)
                                         FROM INFORMATION_SCHEMA.TABLES
                                         WHERE TABLE_SCHEMA = DATABASE()
                                           AND TABLE_NAME IN ('PHONG', 'DATPHONG', 'KHACHHANG', 'HOADON')";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        int readyTables = Convert.ToInt32(cmd.ExecuteScalar());
                        return readyTables >= 4;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Cannot determine whether database initialization can be deferred.", new Dictionary<string, object>
                {
                    ["Error"] = ex.Message
                });
                return false;
            }
        }

        private static bool ShouldRunStartupMaintenance()
        {
            try
            {
                string stampPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StartupMaintenanceStampFileName);
                if (!File.Exists(stampPath)) return true;

                string text = File.ReadAllText(stampPath).Trim();
                if (text.Length == 0) return true;

                if (!DateTime.TryParseExact(
                        text,
                        "O",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var lastRunUtc))
                {
                    return true;
                }

                return (DateTime.UtcNow - lastRunUtc) >= StartupMaintenanceInterval;
            }
            catch
            {
                return true;
            }
        }

        private static void MarkStartupMaintenanceCompleted()
        {
            try
            {
                string stampPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StartupMaintenanceStampFileName);
                File.WriteAllText(stampPath, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            }
            catch
            {
                // Ignore stamp write failures.
            }
        }

        private static void EnsureStatisticsSchema(bool interactiveWarning = true)
        {
            using (PerformanceTracker.Measure("Program.EnsureStatisticsSchema"))
            {
                try
                {
                    SchemaMaintenanceService.EnsureStatisticsAndAuditSchema();
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Cannot sync statistics/audit schema.");
                    if (interactiveWarning)
                    {
                        MessageBox.Show(
                            "Không thể đồng bộ schema thống kê/audit.\nỨng dụng sẽ tiếp tục chạy nhưng có thể thiếu một số dữ liệu báo cáo.",
                            "Canh bao dong bo schema",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private static void EnsurePerformanceIndexes()
        {
            using (PerformanceTracker.Measure("Program.EnsurePerformanceIndexes"))
            {
                try
                {
                    using (var conn = DbHelper.GetConnection())
                    {
                        EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_NGAYDEN", "CREATE INDEX IDX_DATPHONG_NGAYDEN ON DATPHONG (NgayDen)");
                        EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_TRANGTHAI", "CREATE INDEX IDX_DATPHONG_TRANGTHAI ON DATPHONG (TrangThai)");
                        EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_PHONGID", "CREATE INDEX IDX_DATPHONG_PHONGID ON DATPHONG (PhongID)");
                        EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_RANGE_DATA_TYPE_STATUS_ID", "CREATE INDEX IDX_DATPHONG_RANGE_DATA_TYPE_STATUS_ID ON DATPHONG (NgayDen, DataStatus, BookingType, TrangThai, DatPhongID)");
                        EnsureIndex(conn, "DATPHONG", "IDX_DATPHONG_ROOM_STATUS_DATA_ID", "CREATE INDEX IDX_DATPHONG_ROOM_STATUS_DATA_ID ON DATPHONG (PhongID, TrangThai, DataStatus, DatPhongID)");
                        EnsureIndex(conn, "PHONG", "IDX_PHONG_TRANGTHAI", "CREATE INDEX IDX_PHONG_TRANGTHAI ON PHONG (TrangThai)");
                        EnsureIndex(conn, "HOADON", "IDX_HOADON_NGAYLAP", "CREATE INDEX IDX_HOADON_NGAYLAP ON HOADON (NgayLap)");
                        EnsureIndex(conn, "HOADON", "IDX_HOADON_BOOKING_STATUS_PAY_DATE", "CREATE INDEX IDX_HOADON_BOOKING_STATUS_PAY_DATE ON HOADON (DatPhongID, DataStatus, DaThanhToan, NgayLap)");
                        EnsureIndex(conn, "HOADON", "IDX_HOADON_DATE_STATUS_BOOKING", "CREATE INDEX IDX_HOADON_DATE_STATUS_BOOKING ON HOADON (NgayLap, DataStatus, DatPhongID)");
                        EnsureIndex(conn, "BOOKING_EXTRAS", "IDX_BOOKING_EXTRAS_BOOKING", "CREATE INDEX IDX_BOOKING_EXTRAS_BOOKING ON BOOKING_EXTRAS (DatPhongID)");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("Cannot ensure performance indexes.", new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message
                    });
                }
            }
        }

        private static void EnsureIndex(MySqlConnection conn, string tableName, string indexName, string createSql)
        {
            string checkSql = @"SELECT COUNT(1)
                                FROM INFORMATION_SCHEMA.STATISTICS
                                WHERE TABLE_SCHEMA = DATABASE()
                                  AND TABLE_NAME = @TableName
                                  AND INDEX_NAME = @IndexName";

            using (var checkCmd = new MySqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@TableName", tableName);
                checkCmd.Parameters.AddWithValue("@IndexName", indexName);
                bool exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                if (exists) return;
            }

            using (var createCmd = new MySqlCommand(createSql, conn))
            {
                createCmd.ExecuteNonQuery();
            }
        }
    }
}
