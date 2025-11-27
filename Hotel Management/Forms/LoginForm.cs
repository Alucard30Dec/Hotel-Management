using System;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class LoginForm : Form
    {
        private readonly UserDAL userDal = new UserDAL();

        // Thuộc tính để MainForm đọc thông tin user sau khi đăng nhập
        public User LoggedInUser { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu");
                return;
            }

            User user = userDal.Login(username, password);
            if (user == null)
            {
                MessageBox.Show("Đăng nhập thất bại, sai tên đăng nhập hoặc mật khẩu");
                return;
            }

            // Đăng nhập thành công: lưu lại user & trả về DialogResult.OK
            LoggedInUser = user;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            // Chỉ đóng dialog, không tắt toàn bộ ứng dụng
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
