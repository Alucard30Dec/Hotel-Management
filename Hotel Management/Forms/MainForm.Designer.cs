using System.Drawing;
using System.Windows.Forms;

namespace HotelManagement.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel panelTop;
        private Panel panelLeft;
        private Panel panelFilter;
        private Panel panelDetailHost;
        private FlowLayoutPanel flowRooms;

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
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelTop = new Panel();
            this.btnAdmin = new Button();
            this.lblTitle = new Label();
            this.txtSearch = new TextBox();
            this.btnExit = new Button();

            this.panelLeft = new Panel();
            this.btnSoDoPhong = new Button();
            this.btnThongKe = new Button();
            this.btnReports = new Button();
            this.lblCurrentUser = new Label();

            this.panelFilter = new Panel();
            this.btnFilterAll = new Button();
            this.btnFilterTrong = new Button();
            this.btnFilterCoKhach = new Button();
            this.btnFilterChuaDon = new Button();
            this.btnFilterDaDat = new Button();

            this.flowRooms = new FlowLayoutPanel();
            this.panelDetailHost = new Panel();

            // ===== MainForm =====
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Text = "Quản lý khách sạn";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.Load += new System.EventHandler(this.MainForm_Load);

            // ===== panelTop =====
            this.panelTop.BackColor = Color.FromArgb(63, 81, 181);
            this.panelTop.Dock = DockStyle.Top;
            this.panelTop.Height = 60;
            this.panelTop.Padding = new Padding(10, 10, 10, 10);

            // btnAdmin
            this.btnAdmin.FlatStyle = FlatStyle.Flat;
            this.btnAdmin.FlatAppearance.BorderSize = 0;
            this.btnAdmin.ForeColor = Color.White;
            this.btnAdmin.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnAdmin.Text = "≡";
            this.btnAdmin.Width = 40;
            this.btnAdmin.Height = 40;
            this.btnAdmin.Location = new Point(10, 10);
            this.btnAdmin.Click += new System.EventHandler(this.btnAdmin_Click);

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.ForeColor = Color.White;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            this.lblTitle.Location = new Point(60, 18);
            this.lblTitle.Text = "Thanh Long - Hotel";

            // txtSearch
            this.txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtSearch.Font = new Font("Segoe UI", 10F);
            this.txtSearch.Location = new Point(280, 16);
            this.txtSearch.Width = 650;
            this.txtSearch.Text = "";
            this.txtSearch.ForeColor = Color.Black;
            this.txtSearch.KeyDown += new KeyEventHandler(this.txtSearch_KeyDown);
            this.txtSearch.GotFocus += new System.EventHandler(this.txtSearch_GotFocus);
            this.txtSearch.LostFocus += new System.EventHandler(this.txtSearch_LostFocus);

            // btnExit
            this.btnExit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnExit.FlatStyle = FlatStyle.Flat;
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.ForeColor = Color.White;
            this.btnExit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnExit.Text = "Thoát";
            this.btnExit.Width = 70;
            this.btnExit.Height = 35;
            this.btnExit.Location = new Point(1110, 12); // 1200 - 90
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);

            this.panelTop.Controls.Add(this.btnAdmin);
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Controls.Add(this.txtSearch);
            this.panelTop.Controls.Add(this.btnExit);

            // ===== panelLeft =====
            this.panelLeft.BackColor = Color.FromArgb(248, 249, 252);
            this.panelLeft.Dock = DockStyle.Left;
            this.panelLeft.Width = 220;
            this.panelLeft.Padding = new Padding(0, 10, 0, 10);

            void StyleLeftMenu(Button btn, string text, int top)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Font = new Font("Segoe UI", 10F);
                btn.ForeColor = Color.FromArgb(55, 71, 79);
                btn.BackColor = Color.Transparent;
                btn.Text = text;
                btn.Width = this.panelLeft.Width;
                btn.Height = 36;
                btn.Location = new Point(0, top);
                btn.Padding = new Padding(20, 0, 0, 0);
            }

            int menuTop = 10;
            StyleLeftMenu(this.btnSoDoPhong, "Sơ đồ phòng", menuTop);
            menuTop += 40;
            StyleLeftMenu(this.btnThongKe, "Thống kê", menuTop);
            menuTop += 40;
            StyleLeftMenu(this.btnReports, "Báo cáo", menuTop);

            this.btnSoDoPhong.Click += new System.EventHandler(this.btnRooms_Click);
            this.btnThongKe.Click += (s, e) =>
            {
                MessageBox.Show("Chức năng thống kê sẽ được bổ sung sau.", "Thông báo");
            };
            this.btnReports.Click += new System.EventHandler(this.btnReports_Click);

            this.lblCurrentUser.AutoSize = true;
            this.lblCurrentUser.Font = new Font("Segoe UI", 9F);
            this.lblCurrentUser.ForeColor = Color.Gray;
            this.lblCurrentUser.Location = new Point(12, this.panelLeft.Height - 30);
            this.lblCurrentUser.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblCurrentUser.Text = "Người dùng: (khách)";

            this.panelLeft.Controls.Add(this.btnSoDoPhong);
            this.panelLeft.Controls.Add(this.btnThongKe);
            this.panelLeft.Controls.Add(this.btnReports);
            this.panelLeft.Controls.Add(this.lblCurrentUser);

            // ===== panelFilter =====
            this.panelFilter.BackColor = Color.White;
            this.panelFilter.Dock = DockStyle.Top;
            this.panelFilter.Height = 60;
            this.panelFilter.Padding = new Padding(10, 10, 10, 10);

            void StyleFilter(Button btn, string text, Color color, int left)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                btn.ForeColor = Color.White;
                btn.BackColor = color;
                btn.Text = text;
                btn.Height = 30;
                btn.Width = 130;
                btn.Location = new Point(left, 15);
            }

            int filterLeft = 10;
            StyleFilter(this.btnFilterAll, "Tất cả", Color.FromArgb(158, 158, 158), filterLeft);
            filterLeft += 135;
            StyleFilter(this.btnFilterTrong, "Trống", Color.FromArgb(76, 175, 80), filterLeft);
            filterLeft += 135;
            StyleFilter(this.btnFilterCoKhach, "Có khách", Color.FromArgb(33, 150, 243), filterLeft);
            filterLeft += 135;
            StyleFilter(this.btnFilterChuaDon, "Chưa dọn", Color.FromArgb(96, 125, 139), filterLeft);
            filterLeft += 135;
            StyleFilter(this.btnFilterDaDat, "Đã có khách đặt", Color.FromArgb(255, 152, 0), filterLeft);

            this.btnFilterAll.Click += new System.EventHandler(this.btnFilterAll_Click);
            this.btnFilterTrong.Click += new System.EventHandler(this.btnFilterTrong_Click);
            this.btnFilterCoKhach.Click += new System.EventHandler(this.btnFilterCoKhach_Click);
            this.btnFilterChuaDon.Click += new System.EventHandler(this.btnFilterChuaDon_Click);
            this.btnFilterDaDat.Click += new System.EventHandler(this.btnFilterDaDat_Click);

            this.panelFilter.Controls.Add(this.btnFilterAll);
            this.panelFilter.Controls.Add(this.btnFilterTrong);
            this.panelFilter.Controls.Add(this.btnFilterCoKhach);
            this.panelFilter.Controls.Add(this.btnFilterChuaDon);
            this.panelFilter.Controls.Add(this.btnFilterDaDat);

            // ===== flowRooms =====
            this.flowRooms.Dock = DockStyle.Fill;
            this.flowRooms.AutoScroll = true;
            this.flowRooms.Padding = new Padding(10);
            this.flowRooms.BackColor = Color.FromArgb(245, 245, 245);

            // ===== panelDetailHost =====
            this.panelDetailHost.Dock = DockStyle.Fill;
            this.panelDetailHost.BackColor = Color.White;
            this.panelDetailHost.Visible = false;

            // ===== Add controls vào Form =====
            this.Controls.Add(this.panelDetailHost);
            this.Controls.Add(this.flowRooms);
            this.Controls.Add(this.panelFilter);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelTop);
        }
    }
}
