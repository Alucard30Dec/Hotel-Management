using System;
using System.Windows.Forms;
using HotelManagement.Forms; // namespace chứa MainForm

namespace HotelManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // KHÔNG chạy LoginForm nữa
            // Application.Run(new LoginForm());

            // Chạy thẳng vào giao diện chính
            Application.Run(new MainForm());
        }
    }
}
