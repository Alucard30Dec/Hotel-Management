using System.Windows.Forms;
using System.Drawing;

namespace HotelManagement.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel panelTop;
        private Panel panelLeft;
        private Panel panelFilter;
        private FlowLayoutPanel flowRooms;
        private Panel panelDetailHost;

        private Button btnAdmin;
        private Button btnExit;
        private Label lblTitle;
        private TextBox txtSearch;

        private Button btnSoDoPhong;
        private Button btnThongKe;
        private Button btnReports;
        private Label lblCurrentUser;

        private Button btnFilterAll;
        private Button btnFilterTrong;
        private Button btnFilterCoKhach;
        private Button btnFilterChuaDon;
        private Button btnFilterDaDat;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // Form
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 750);
            this.Text = "Quản lý khách sạn";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.Load += new System.EventHandler(this.MainForm_Load);

            // ===== Top bar =====
            panelTop = new Panel
            {
                BackColor = Color.FromArgb(63, 81, 181),
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10)
            };

            btnAdmin = new Button
            {
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "≡",
                Width = 40,
                Height = 40,
                Location = new Point(10, 10)
            };
            btnAdmin.Click += new System.EventHandler(this.btnAdmin_Click);

            lblTitle = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Location = new Point(60, 18),
                Text = "730 - ezCloudHotel"
            };

            txtSearch = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(280, 16),
                Width = 600,
                Text = ""
            };
            txtSearch.KeyDown += new KeyEventHandler(this.txtSearch_KeyDown);
            txtSearch.GotFocus += new System.EventHandler(this.txtSearch_GotFocus);
            txtSearch.LostFocus += new System.EventHandler(this.txtSearch_LostFocus);

            btnExit = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "Thoát",
                Width = 70,
                Height = 35
            };
            btnExit.Location = new Point(this.ClientSize.Width - 80, 12);
            btnExit.Click += new System.EventHandler(this.btnExit_Click);

            panelTop.Controls.Add(btnAdmin);
            panelTop.Controls.Add(lblTitle);
            panelTop.Controls.Add(txtSearch);
            panelTop.Controls.Add(btnExit);

            // ===== Left menu =====
            panelLeft = new Panel
            {
                BackColor = Color.FromArgb(248, 249, 252),
                Dock = DockStyle.Left,
                Width = 220,
                Padding = new Padding(0, 10, 0, 10)
            };

            btnSoDoPhong = MakeLeftButton("Sơ đồ phòng", 10);
            btnSoDoPhong.Click += new System.EventHandler(this.btnRooms_Click);

            btnThongKe = MakeLeftButton("Thống kê", 50);
            btnReports = MakeLeftButton("Báo cáo", 90);

            lblCurrentUser = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                Location = new Point(12, this.panelLeft.Height - 30),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Text = "Người dùng: (khách)"
            };

            panelLeft.Controls.Add(btnSoDoPhong);
            panelLeft.Controls.Add(btnThongKe);
            panelLeft.Controls.Add(btnReports);
            panelLeft.Controls.Add(lblCurrentUser);

            // ===== Filter row =====
            panelFilter = new Panel { BackColor = Color.White, Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };

            btnFilterAll = MakeFilter("Tất cả", Color.FromArgb(158, 158, 158), 10);
            btnFilterAll.Click += new System.EventHandler(this.btnFilterAll_Click);

            btnFilterTrong = MakeFilter("Trống", Color.FromArgb(76, 175, 80), 125);
            btnFilterTrong.Click += new System.EventHandler(this.btnFilterTrong_Click);

            btnFilterCoKhach = MakeFilter("Có khách", Color.FromArgb(33, 150, 243), 240);
            btnFilterCoKhach.Click += new System.EventHandler(this.btnFilterCoKhach_Click);

            btnFilterChuaDon = MakeFilter("Chưa dọn", Color.FromArgb(96, 125, 139), 355);
            btnFilterChuaDon.Click += new System.EventHandler(this.btnFilterChuaDon_Click);

            btnFilterDaDat = MakeFilter("Đã có khách đặt", Color.FromArgb(255, 152, 0), 470);
            btnFilterDaDat.Click += new System.EventHandler(this.btnFilterDaDat_Click);

            panelFilter.Controls.AddRange(new Control[] { btnFilterAll, btnFilterTrong, btnFilterCoKhach, btnFilterChuaDon, btnFilterDaDat });

            // ===== Host panels =====
            flowRooms = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            panelDetailHost = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.White };

            // order add: detail dưới cùng để có thể BringToFront khi dùng
            this.Controls.Add(panelDetailHost);
            this.Controls.Add(flowRooms);
            this.Controls.Add(panelFilter);
            this.Controls.Add(panelLeft);
            this.Controls.Add(panelTop);

            // helpers
            Button MakeLeftButton(string text, int top)
            {
                var btn = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(55, 71, 79),
                    BackColor = Color.Transparent,
                    Text = text,
                    Width = 220,
                    Height = 36,
                    Location = new Point(0, top),
                    Padding = new Padding(20, 0, 0, 0)
                };
                btn.FlatAppearance.BorderSize = 0;
                return btn;
            }
            Button MakeFilter(string text, Color color, int left)
            {
                var btn = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = color,
                    Text = text,
                    Height = 30,
                    Width = 110,
                    Location = new Point(left, 15)
                };
                btn.FlatAppearance.BorderSize = 0;
                return btn;
            }
        }
    }
}
