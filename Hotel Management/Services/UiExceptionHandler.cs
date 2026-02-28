using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManagement.Data;

namespace HotelManagement.Services
{
    public static class UiExceptionHandler
    {
        public static void Run(IWin32Window owner, string operationName, Action action)
        {
            if (action == null) return;

            using (AuditContext.BeginCorrelationScope())
            {
                try
                {
                    action();
                }
                catch (ValidationException ex)
                {
                    AppLogger.Warn("Validation failed at UI boundary.", BuildContext(operationName, ex.Message));
                    MessageBox.Show(
                        owner,
                        ex.Message,
                        "Dữ liệu không hợp lệ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch (DomainException ex)
                {
                    AppLogger.Warn("Domain rule failed at UI boundary.", BuildContext(operationName, ex.Message));
                    MessageBox.Show(
                        owner,
                        ex.Message,
                        "Không thể thực hiện",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch (InfrastructureException ex)
                {
                    AppLogger.Error(ex, "Infrastructure failure at UI boundary.", BuildContext(operationName, null));
                    MessageBox.Show(
                        owner,
                        "Không thể hoàn tất thao tác do lỗi hệ thống dữ liệu. Vui lòng thử lại.",
                        "Lỗi hệ thống",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Unhandled error at UI boundary.", BuildContext(operationName, null));
                    MessageBox.Show(
                        owner,
                        "Có lỗi không mong muốn. Vui lòng thử lại hoặc liên hệ kỹ thuật.",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        public static async Task RunAsync(IWin32Window owner, string operationName, Func<Task> action)
        {
            if (action == null) return;

            using (AuditContext.BeginCorrelationScope())
            {
                try
                {
                    await action().ConfigureAwait(true);
                }
                catch (ValidationException ex)
                {
                    AppLogger.Warn("Validation failed at UI boundary.", BuildContext(operationName, ex.Message));
                    MessageBox.Show(
                        owner,
                        ex.Message,
                        "Dữ liệu không hợp lệ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch (DomainException ex)
                {
                    AppLogger.Warn("Domain rule failed at UI boundary.", BuildContext(operationName, ex.Message));
                    MessageBox.Show(
                        owner,
                        ex.Message,
                        "Không thể thực hiện",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch (InfrastructureException ex)
                {
                    AppLogger.Error(ex, "Infrastructure failure at UI boundary.", BuildContext(operationName, null));
                    MessageBox.Show(
                        owner,
                        "Không thể hoàn tất thao tác do lỗi hệ thống dữ liệu. Vui lòng thử lại.",
                        "Lỗi hệ thống",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Unhandled error at UI boundary.", BuildContext(operationName, null));
                    MessageBox.Show(
                        owner,
                        "Có lỗi không mong muốn. Vui lòng thử lại hoặc liên hệ kỹ thuật.",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private static Dictionary<string, object> BuildContext(string operationName, string detail)
        {
            var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Operation"] = string.IsNullOrWhiteSpace(operationName) ? "Unknown" : operationName
            };

            if (!string.IsNullOrWhiteSpace(detail))
                context["Detail"] = detail;

            return context;
        }
    }
}
