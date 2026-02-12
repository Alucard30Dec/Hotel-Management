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

            // ===== FORM CHÍNH =====
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 750);
            this.Text = "Quản lý khách sạn";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.MinimumSize = new Size(1000, 600);
            this.Load += new System.EventHandler(this.MainForm_Load);
            // gọi lại mỗi khi resize để co giãn khung tầng
            this.Resize += new System.EventHandler(this.MainForm_Resize);

            // ===== THANH TRÊN (TOP BAR) =====
            panelTop = new Panel
            {
                BackColor = Color.FromArgb(63, 81, 181),
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10, 10, 10, 10)
            };

            var topLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var panelTopLeft = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            btnAdmin = new Button
            {
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "≡",
                Width = 40,
                Height = 40,
                Margin = new Padding(0, 0, 10, 0),
                TabStop = false
            };
            btnAdmin.FlatAppearance.BorderSize = 0;
            btnAdmin.Click += new System.EventHandler(this.btnAdmin_Click);

            lblTitle = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Text = "Thanh Long Hotel",
                Margin = new Padding(0, 8, 0, 0)
            };

            panelTopLeft.Controls.Add(btnAdmin);
            panelTopLeft.Controls.Add(lblTitle);

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(10, 8, 10, 8),
                Text = ""
            };
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.KeyDown += new KeyEventHandler(this.txtSearch_KeyDown);
            txtSearch.GotFocus += new System.EventHandler(this.txtSearch_GotFocus);
            txtSearch.LostFocus += new System.EventHandler(this.txtSearch_LostFocus);

            btnExit = new Button
            {
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "Thoát",
                Width = 75,
                Height = 35,
                Margin = new Padding(0, 8, 0, 8)
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += new System.EventHandler(this.btnExit_Click);

            topLayout.Controls.Add(panelTopLeft, 0, 0);
            topLayout.Controls.Add(txtSearch, 1, 0);
            topLayout.Controls.Add(btnExit, 2, 0);

            panelTop.Controls.Add(topLayout);

            // ===== MENU TRÁI =====
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
                Dock = DockStyle.Bottom,
                Height = 30,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Text = "Người dùng: (khách)"
            };

            panelLeft.Controls.Add(btnSoDoPhong);
            panelLeft.Controls.Add(btnThongKe);
            panelLeft.Controls.Add(btnReports);
            panelLeft.Controls.Add(lblCurrentUser);

            // ===== PANEL MAIN =====
            var panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // ----- THANH FILTER -----
            panelFilter = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10)
            };

            var panelFilterInner = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            btnFilterAll = MakeFilter("Tất cả", Color.FromArgb(158, 158, 158));
            btnFilterAll.Click += new System.EventHandler(this.btnFilterAll_Click);

            btnFilterTrong = MakeFilter("Trống", Color.FromArgb(76, 175, 80));
            btnFilterTrong.Click += new System.EventHandler(this.btnFilterTrong_Click);

            btnFilterCoKhach = MakeFilter("Có khách", Color.FromArgb(33, 150, 243));
            btnFilterCoKhach.Click += new System.EventHandler(this.btnFilterCoKhach_Click);

            btnFilterChuaDon = MakeFilter("Chưa dọn", Color.FromArgb(96, 125, 139));
            btnFilterChuaDon.Click += new System.EventHandler(this.btnFilterChuaDon_Click);

            btnFilterDaDat = MakeFilter("Đã có khách đặt", Color.FromArgb(255, 152, 0));
            btnFilterDaDat.Click += new System.EventHandler(this.btnFilterDaDat_Click);

            panelFilterInner.Controls.Add(btnFilterAll);
            panelFilterInner.Controls.Add(btnFilterTrong);
            panelFilterInner.Controls.Add(btnFilterCoKhach);
            panelFilterInner.Controls.Add(btnFilterChuaDon);
            panelFilterInner.Controls.Add(btnFilterDaDat);

            panelFilter.Controls.Add(panelFilterInner);

            // ----- CENTER: DANH SÁCH PHÒNG + CHI TIẾT -----
            var panelCenter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // FlowLayoutPanel chứa các "khung tầng"
            flowRooms = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 245),

                // mỗi tầng 1 dòng, full width, xếp từ trên xuống dưới
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown
            };
            // khi thêm khối tầng mới thì tự chỉnh lại width
            flowRooms.ControlAdded += new ControlEventHandler(this.flowRooms_ControlAdded);

            // Panel chi tiết phòng (bên phải, nếu bạn dùng side panel)
            panelDetailHost = new Panel
            {
                Dock = DockStyle.Right,
                Width = 420,
                MinimumSize = new global::System.Drawing.Size(360, 0),
                Visible = false,
                BackColor = Color.White
            };

            panelCenter.Controls.Add(flowRooms);
            panelCenter.Controls.Add(panelDetailHost);

            panelMain.Controls.Add(panelCenter);
            panelMain.Controls.Add(panelFilter);

            // ===== ADD VÀO FORM =====
            this.Controls.Add(panelMain);
            this.Controls.Add(panelLeft);
            this.Controls.Add(panelTop);

            // ===== Helpers =====
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
                    Padding = new Padding(20, 0, 0, 0),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                btn.FlatAppearance.BorderSize = 0;
                return btn;
            }

            Button MakeFilter(string text, Color color)
            {
                var btn = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = color,
                    Text = text,
                    Height = 30,
                    Width = 130,
                    Margin = new Padding(5, 5, 5, 5)
                };
                btn.FlatAppearance.BorderSize = 0;
                return btn;
            }
        }
    }
}
