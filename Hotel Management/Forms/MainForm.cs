using System;
using System.Drawing;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class MainForm : Form
    {
        private User _currentUser;
        private readonly RoomDAL _roomDal = new RoomDAL();

        public MainForm()
        {
            InitializeComponent();

            // Người dùng mặc định (chưa đăng nhập)
            _currentUser = new User
            {
                Username = "Khách",
                Role = "Letan"
            };
        }

        public MainForm(User user) : this()
        {
            if (user != null)
                _currentUser = user;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateUserUI();
            InitSearchPlaceholder();
            LoadRoomTiles();   // Vẽ các ô phòng
        }

        private void UpdateUserUI()
        {
            lblCurrentUser.Text = $"Người dùng: {_currentUser.Username} ({_currentUser.Role})";

            // VD: chỉ Admin dùng được nút Báo cáo
            btnReports.Enabled = _currentUser.Role == "Admin";
        }

        #region Placeholder cho ô tìm kiếm

        private readonly Color _placeholderColor = Color.Gray;
        private readonly Color _normalColor = Color.Black;
        private const string _placeholderText = "Nhập từ khóa tìm kiếm";

        private void InitSearchPlaceholder()
        {
            // Thiết lập placeholder lần đầu
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = _placeholderText;
                txtSearch.ForeColor = _placeholderColor;
            }
        }

        private void txtSearch_GotFocus(object sender, EventArgs e)
        {
            if (txtSearch.ForeColor == _placeholderColor)
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = _normalColor;
            }
        }

        private void txtSearch_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = _placeholderText;
                txtSearch.ForeColor = _placeholderColor;
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            // Nếu vẫn đang là placeholder thì không tìm kiếm
            if (txtSearch.ForeColor == _placeholderColor)
                return;

            if (e.KeyCode == Keys.Enter)
            {
                string keyword = txtSearch.Text.Trim().ToLower();
                foreach (Control c in flowRooms.Controls)
                {
                    if (c is Panel p)
                    {
                        bool visible = true;
                        if (!string.IsNullOrEmpty(keyword))
                        {
                            string allText = "";
                            foreach (Control child in p.Controls)
                                allText += child.Text.ToLower() + " ";

                            visible = allText.Contains(keyword);
                        }
                        p.Visible = visible;
                    }
                }
            }
        }

        #endregion

        #region Vẽ tile phòng

        private void LoadRoomTiles()
        {
            flowRooms.SuspendLayout();
            flowRooms.Controls.Clear();

            var rooms = _roomDal.GetAll();

            foreach (var room in rooms)
            {
                var tile = CreateRoomTile(room);
                flowRooms.Controls.Add(tile);
            }

            flowRooms.ResumeLayout();
        }

        private Panel CreateRoomTile(Room room)
        {
            var panel = new Panel();
            panel.Width = 220;
            panel.Height = 120;
            panel.Margin = new Padding(8);
            panel.Padding = new Padding(8);
            panel.BorderStyle = BorderStyle.None;
            panel.BackColor = GetRoomBackColor(room.TrangThai);

            var lblCode = new Label();
            lblCode.AutoSize = false;
            lblCode.TextAlign = ContentAlignment.MiddleLeft;
            lblCode.Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold);
            lblCode.ForeColor = Color.White;
            lblCode.Text = room.MaPhong;
            lblCode.Location = new Point(8, 8);
            lblCode.Size = new Size(80, 30);

            var lblType = new Label();
            lblType.AutoSize = true;
            lblType.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblType.ForeColor = Color.WhiteSmoke;
            lblType.Text = "STD";
            lblType.Location = new Point(8, 40);

            var lblStatus = new Label();
            lblStatus.AutoSize = false;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblStatus.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold);
            lblStatus.ForeColor = Color.White;
            lblStatus.Text = GetRoomStatusText(room.TrangThai);
            lblStatus.Location = new Point(0, 70);
            lblStatus.Size = new Size(panel.Width - 16, 30);

            panel.Controls.Add(lblCode);
            panel.Controls.Add(lblType);
            panel.Controls.Add(lblStatus);

            if (!string.IsNullOrWhiteSpace(room.GhiChu))
            {
                var tooltip = new ToolTip();
                tooltip.SetToolTip(panel, room.GhiChu);
            }

            panel.MouseEnter += (s, e) =>
            {
                panel.BackColor = ControlPaint.Light(panel.BackColor);
            };
            panel.MouseLeave += (s, e) =>
            {
                panel.BackColor = GetRoomBackColor(room.TrangThai);
            };

            panel.Cursor = Cursors.Hand;
            panel.Click += (s, e) =>
            {
                var f = new RoomForm();
                f.ShowDialog(this);
            };

            return panel;
        }

        private Color GetRoomBackColor(int trangThai)
        {
            // 0 = Trống (xanh lá)
            // 1 = Có khách (xanh dương)
            // 2 = Chưa dọn (xám)
            // 3 = Đã đặt (cam)
            switch (trangThai)
            {
                case 0:
                    return Color.FromArgb(76, 175, 80);   // xanh lá
                case 1:
                    return Color.FromArgb(33, 150, 243);  // xanh dương
                case 2:
                    return Color.FromArgb(96, 125, 139);  // xám
                case 3:
                    return Color.FromArgb(255, 152, 0);   // cam
                default:
                    return Color.FromArgb(158, 158, 158); // xám nhạt
            }
        }


        private string GetRoomStatusText(int trangThai)
        {
            switch (trangThai)
            {
                case 0: return "Trống";
                case 1: return "Có khách";
                case 2: return "Chưa dọn";
                case 3: return "Đã có khách đặt";
                default: return "Không rõ";
            }
        }


        #endregion

        #region Menu trái – mở form quản lý

        private void btnRooms_Click(object sender, EventArgs e)
        {
            var f = new RoomForm();
            f.ShowDialog(this);
            LoadRoomTiles();
        }

        private void btnCustomers_Click(object sender, EventArgs e)
        {
            var f = new CustomerForm();
            f.ShowDialog(this);
        }

        private void btnBookings_Click(object sender, EventArgs e)
        {
            var f = new BookingForm();
            f.ShowDialog(this);
            LoadRoomTiles();
        }

        private void btnInvoices_Click(object sender, EventArgs e)
        {
            var f = new InvoiceForm();
            f.ShowDialog(this);
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Báo cáo sẽ được bổ sung sau.", "Thông báo");
        }

        #endregion

        #region Top bar – Admin, thoát

        private void btnAdmin_Click(object sender, EventArgs e)
        {
            using (var login = new LoginForm())
            {
                var result = login.ShowDialog(this);
                if (result == DialogResult.OK && login.LoggedInUser != null)
                {
                    _currentUser = login.LoggedInUser;
                    UpdateUserUI();
                    MessageBox.Show($"Xin chào: {_currentUser.Username}", "Đăng nhập thành công");
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
