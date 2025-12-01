using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class MainForm : Form
    {
        private User _currentUser;
        private readonly RoomDAL _roomDal = new RoomDAL();

        // null = tất cả, 0..3 = trạng thái phòng
        private int? _currentFilterStatus = null;

        // placeholder ô tìm kiếm
        private readonly Color _placeholderColor = Color.Gray;
        private readonly Color _normalColor = Color.Black;
        private const string _placeholderText = "Nhập từ khóa tìm kiếm";

        // Timer cập nhật đồng hồ phòng
        private Timer _roomTimer;

        // Lưu các label trong card phòng để Timer cập nhật
        private class RoomTileInfo
        {
            public Room Room { get; set; }
            public Label LblStartTime { get; set; } // dòng trên
            public Label LblCenter { get; set; }    // dòng giữa
            public Label LblElapsed { get; set; }   // dòng dưới
        }

        public MainForm()
        {
            InitializeComponent();

            // user mặc định khi mở app trực tiếp
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
            LoadRoomTiles();
            SetupRoomTimer();
        }

        #region UI người dùng

        private void UpdateUserUI()
        {
            lblCurrentUser.Text = $"Người dùng: {_currentUser.Username} ({_currentUser.Role})";
            btnReports.Enabled = _currentUser.Role == "Admin";
        }

        #endregion

        #region Placeholder ô tìm kiếm

        private void InitSearchPlaceholder()
        {
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
            if (txtSearch.ForeColor == _placeholderColor)
                return;

            if (e.KeyCode != Keys.Enter)
                return;

            string keyword = txtSearch.Text.Trim().ToLower();

            foreach (Control c in flowRooms.Controls)
            {
                if (c is Panel panelTang)
                {
                    bool anyVisible = false;

                    foreach (Control child in panelTang.Controls)
                    {
                        if (child is FlowLayoutPanel flp)
                        {
                            foreach (Control roomCard in flp.Controls)
                            {
                                if (roomCard is Panel roomPanel)
                                {
                                    string allText = "";
                                    foreach (Control lbl in roomPanel.Controls)
                                    {
                                        allText += lbl.Text.ToLower() + " ";
                                        if (lbl is Panel innerPanel)
                                        {
                                            foreach (Control innerChild in innerPanel.Controls)
                                                allText += innerChild.Text.ToLower() + " ";
                                        }
                                    }

                                    bool visible = string.IsNullOrEmpty(keyword) || allText.Contains(keyword);
                                    roomPanel.Visible = visible;
                                    if (visible) anyVisible = true;
                                }
                            }
                        }
                    }

                    panelTang.Visible = anyVisible || string.IsNullOrEmpty(keyword);
                }
            }
        }

        #endregion

        #region Vẽ sơ đồ phòng theo tầng + đơn / đôi

        private void LoadRoomTiles()
        {
            flowRooms.SuspendLayout();
            flowRooms.Controls.Clear();

            var rooms = _roomDal.GetAll();

            if (_currentFilterStatus.HasValue)
                rooms = rooms.FindAll(r => r.TrangThai == _currentFilterStatus.Value);

            var tangGroups = rooms.GroupBy(r => r.Tang)
                                  .OrderBy(g => g.Key);

            foreach (var group in tangGroups)
            {
                int tang = group.Key;

                Panel panelTang = new Panel();
                int containerWidth = flowRooms.ClientSize.Width;
                if (containerWidth <= 0)
                    containerWidth = this.ClientSize.Width - panelLeft.Width - 40;

                panelTang.Width = containerWidth - 40;
                panelTang.Height = 340;
                panelTang.Margin = new Padding(10, 5, 10, 10);
                panelTang.BackColor = Color.White;
                panelTang.BorderStyle = BorderStyle.None;

                // Header Tầng
                Panel header = new Panel();
                header.Height = 28;
                header.Width = panelTang.Width;
                header.Location = new Point(0, 0);
                header.BackColor = Color.White;

                Panel accent = new Panel();
                accent.Width = 4;
                accent.Height = 20;
                accent.BackColor = Color.FromArgb(63, 81, 181);
                accent.Location = new Point(0, 4);

                Label lblTang = new Label();
                lblTang.AutoSize = true;
                lblTang.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
                lblTang.ForeColor = Color.FromArgb(55, 71, 79);
                lblTang.Text = $"Tầng {tang}";
                lblTang.Location = new Point(10, 4);

                header.Controls.Add(accent);
                header.Controls.Add(lblTang);

                // Phòng đơn
                Label lblDon = new Label();
                lblDon.AutoSize = true;
                lblDon.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                lblDon.ForeColor = Color.Gray;
                lblDon.Text = "Phòng đơn";
                lblDon.Location = new Point(10, 32);

                FlowLayoutPanel flowDon = new FlowLayoutPanel();
                flowDon.Location = new Point(10, 52);
                flowDon.Width = panelTang.Width - 30;
                flowDon.Height = 130;
                flowDon.AutoScroll = false;
                flowDon.WrapContents = false;
                flowDon.FlowDirection = FlowDirection.LeftToRight;

                // Phòng đôi
                Label lblDoi = new Label();
                lblDoi.AutoSize = true;
                lblDoi.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                lblDoi.ForeColor = Color.Gray;
                lblDoi.Text = "Phòng đôi";
                lblDoi.Location = new Point(10, 195);

                FlowLayoutPanel flowDoi = new FlowLayoutPanel();
                flowDoi.Location = new Point(10, 215);
                flowDoi.Width = panelTang.Width - 30;
                flowDoi.Height = 130;
                flowDoi.AutoScroll = false;
                flowDoi.WrapContents = false;
                flowDoi.FlowDirection = FlowDirection.LeftToRight;

                foreach (var room in group.OrderBy(r => r.MaPhong))
                {
                    Panel tile = CreateRoomTile(room);

                    if (room.LoaiPhongID == 1)
                        flowDon.Controls.Add(tile);
                    else if (room.LoaiPhongID == 2)
                        flowDoi.Controls.Add(tile);
                    else
                        flowDoi.Controls.Add(tile);
                }

                panelTang.Controls.Add(header);
                panelTang.Controls.Add(lblDon);
                panelTang.Controls.Add(flowDon);
                panelTang.Controls.Add(lblDoi);
                panelTang.Controls.Add(flowDoi);

                flowRooms.Controls.Add(panelTang);
            }

            flowRooms.ResumeLayout();

            // cập nhật text / thời gian ban đầu
            if (_roomTimer != null)
                RoomTimer_Tick(this, EventArgs.Empty);
        }

        private Color GetRoomBackColor(int trangThai)
        {
            switch (trangThai)
            {
                case 0:  // Trống
                    return Color.FromArgb(76, 175, 80);
                case 1:  // Có khách
                    return Color.FromArgb(33, 150, 243);
                case 2:  // Chưa dọn
                    return Color.FromArgb(244, 67, 54);
                case 3:  // Đã có khách đặt
                    return Color.FromArgb(255, 152, 0);
                default:
                    return Color.FromArgb(158, 158, 158);
            }
        }

        private string GetStatusIcon(int status)
        {
            // bạn có thể thay bằng icon thật, tạm dùng ký tự
            switch (status)
            {
                case 0: return "✔";  // Trống
                case 1: return "🛏"; // Có khách
                case 2: return "🧹"; // Chưa dọn
                case 3: return "📅"; // Đã đặt
                default: return "";
            }
        }

        private Panel CreateRoomTile(Room room)
        {
            Color baseColor = GetRoomBackColor(room.TrangThai);
            Color lightColor = ControlPaint.Light(baseColor, 0.8f);
            Color textColor = baseColor;

            // ===== Card ngoài (trắng, viền nhẹ) =====
            var panel = new Panel();
            panel.Width = 260;
            panel.Height = 80;
            panel.Margin = new Padding(12, 8, 12, 8);
            panel.BackColor = Color.White;

            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 210, 210)))
                {
                    var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            // ============= CỘT TRÁI: STD + SỐ PHÒNG + ICON =============
            var leftPanel = new Panel();
            leftPanel.Width = 70;
            leftPanel.Dock = DockStyle.Left;
            leftPanel.BackColor = baseColor;
            leftPanel.Padding = new Padding(0, 4, 0, 4);

            var lblStd = new Label();
            lblStd.AutoSize = false;
            lblStd.Dock = DockStyle.Top;
            lblStd.Height = 18;
            lblStd.TextAlign = ContentAlignment.MiddleCenter;
            lblStd.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblStd.ForeColor = Color.White;
            lblStd.Text = "STD";

            var lblIcon = new Label();
            lblIcon.AutoSize = false;
            lblIcon.Dock = DockStyle.Bottom;
            lblIcon.Height = 20;
            lblIcon.TextAlign = ContentAlignment.MiddleCenter;
            lblIcon.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblIcon.ForeColor = Color.White;
            lblIcon.Text = GetStatusIcon(room.TrangThai);

            var lblCode = new Label();
            lblCode.AutoSize = false;
            lblCode.Dock = DockStyle.Fill;
            lblCode.TextAlign = ContentAlignment.MiddleCenter;
            lblCode.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            lblCode.ForeColor = Color.White;
            lblCode.Text = room.MaPhong;

            leftPanel.Controls.Add(lblCode);   // fill
            leftPanel.Controls.Add(lblIcon);   // bottom
            leftPanel.Controls.Add(lblStd);    // top

            // ================= CỘT PHẢI: TRẠNG THÁI =================
            var rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.BackColor = lightColor;
            rightPanel.Padding = new Padding(6, 6, 6, 6);

            // Dòng 1: ngày giờ bắt đầu (khi có khách)
            var lblStartTime = new Label();
            lblStartTime.AutoSize = false;
            lblStartTime.Height = 18;
            lblStartTime.Dock = DockStyle.Top;
            lblStartTime.TextAlign = ContentAlignment.MiddleCenter;
            lblStartTime.Font = new Font("Segoe UI", 8F);
            lblStartTime.ForeColor = Color.FromArgb(120, 0, 0, 0);
            lblStartTime.Name = "lblStartTime";

            // Dòng 2: chữ "Trống" / tên khách / trạng thái
            var lblCenter = new Label();
            lblCenter.AutoSize = false;
            lblCenter.Dock = DockStyle.Fill;
            lblCenter.TextAlign = ContentAlignment.MiddleCenter;
            lblCenter.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            lblCenter.ForeColor = textColor;
            lblCenter.Name = "lblCenter";

            // Dòng 3: đồng hồ HH : MM : SS
            var lblElapsed = new Label();
            lblElapsed.AutoSize = false;
            lblElapsed.Height = 20;
            lblElapsed.Dock = DockStyle.Bottom;
            lblElapsed.TextAlign = ContentAlignment.BottomCenter;
            lblElapsed.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblElapsed.ForeColor = Color.FromArgb(120, 0, 0, 0);
            lblElapsed.Name = "lblElapsed";

            rightPanel.Controls.Add(lblCenter);
            rightPanel.Controls.Add(lblElapsed);
            rightPanel.Controls.Add(lblStartTime);

            panel.Controls.Add(rightPanel);
            panel.Controls.Add(leftPanel);

            // Tooltip nếu có ghi chú
            if (!string.IsNullOrWhiteSpace(room.GhiChu))
            {
                var tooltip = new ToolTip();
                tooltip.SetToolTip(panel, room.GhiChu);
            }

            panel.MouseEnter += (s, e) => { panel.BackColor = Color.FromArgb(250, 250, 250); };
            panel.MouseLeave += (s, e) => { panel.BackColor = Color.White; };

            // Gắn info cho timer
            var info = new RoomTileInfo
            {
                Room = room,
                LblStartTime = lblStartTime,
                LblCenter = lblCenter,
                LblElapsed = lblElapsed
            };
            panel.Tag = info;

            // Gắn sự kiện Click cho panel và toàn bộ control con
            AttachRoomClick(panel);

            // Text ban đầu theo trạng thái
            switch (room.TrangThai)
            {
                case 0:
                    lblCenter.Text = "Trống";
                    break;
                case 2:
                    lblCenter.Text = "Chưa dọn";
                    break;
                case 3:
                    lblCenter.Text = "Đã có khách đặt";
                    break;
                case 1:
                    lblCenter.Text = room.TenKhachHienThi ?? "";
                    break;
            }

            return panel;
        }

        /// <summary>
        /// Gắn sự kiện click cho control và toàn bộ control con.
        /// Không đụng tới Tag đang lưu RoomTileInfo.
        /// </summary>
        private void AttachRoomClick(Control ctrl)
        {
            ctrl.Cursor = Cursors.Hand;
            ctrl.Click -= RoomTile_Click;
            ctrl.Click += RoomTile_Click;

            foreach (Control child in ctrl.Controls)
            {
                AttachRoomClick(child);
            }
        }

        /// <summary>
        /// Handler chung: tìm RoomTileInfo gần nhất rồi mở RoomDetailForm.
        /// </summary>
        private void RoomTile_Click(object sender, EventArgs e)
        {
            if (sender is Control c)
            {
                Control cur = c;
                RoomTileInfo info = null;

                // leo lên trên tới khi gặp control có Tag là RoomTileInfo
                while (cur != null && !(cur.Tag is RoomTileInfo))
                    cur = cur.Parent;

                if (cur != null && cur.Tag is RoomTileInfo ti)
                    info = ti;

                if (info != null && info.Room != null)
                {
                    ShowRoomDetail(info.Room);
                }
            }
        }

        #endregion

        #region Timer cập nhật đồng hồ phòng

        private void SetupRoomTimer()
        {
            if (_roomTimer == null)
            {
                _roomTimer = new Timer();
                _roomTimer.Interval = 1000; // 1 giây
                _roomTimer.Tick += RoomTimer_Tick;
                _roomTimer.Start();
            }
        }

        private void RoomTimer_Tick(object sender, EventArgs e)
        {
            foreach (Control tangPanel in flowRooms.Controls)
            {
                if (tangPanel is Panel panelTang)
                {
                    foreach (Control child in panelTang.Controls)
                    {
                        if (child is FlowLayoutPanel flp)
                        {
                            foreach (Control roomPanel in flp.Controls)
                            {
                                if (roomPanel is Panel p && p.Tag is RoomTileInfo info)
                                {
                                    UpdateRoomTileTime(info);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateRoomTileTime(RoomTileInfo info)
        {
            var room = info.Room;

            // Nếu có khách & có thời gian bắt đầu thì hiển thị đồng hồ
            if (room.TrangThai == 1 && room.ThoiGianBatDau.HasValue)
            {
                DateTime start = room.ThoiGianBatDau.Value;
                DateTime now = DateTime.Now;

                info.LblStartTime.Text = start.ToString("dd/MM/yyyy, HH:mm");

                if (room.KieuThue == 1 || room.KieuThue == 2)
                    info.LblCenter.Text = room.TenKhachHienThi ?? "";
                else
                    info.LblCenter.Text = ""; // phòng giờ không hiện tên

                TimeSpan diff = now - start;
                if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;

                info.LblElapsed.Text = string.Format("{0:00} : {1:00} : {2:00}",
                    (int)diff.TotalHours, diff.Minutes, diff.Seconds);
            }
            else
            {
                // Các trạng thái khác: không hiển thị thời gian
                info.LblStartTime.Text = "";
                info.LblElapsed.Text = "";

                switch (room.TrangThai)
                {
                    case 0:
                        info.LblCenter.Text = "Trống";
                        break;
                    case 2:
                        info.LblCenter.Text = "Chưa dọn";
                        break;
                    case 3:
                        info.LblCenter.Text = "Đã có khách đặt";
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region Mở chi tiết phòng trong MainForm

        private void ShowRoomDetail(Room room)
        {
            flowRooms.Visible = false;
            panelFilter.Visible = false;

            panelDetailHost.Controls.Clear();

            var detail = new RoomDetailForm(room);
            detail.TopLevel = false;
            detail.FormBorderStyle = FormBorderStyle.None;
            detail.Dock = DockStyle.Fill;

            detail.BackRequested += (s, e) =>
            {
                panelDetailHost.Visible = false;
                panelDetailHost.Controls.Clear();

                panelFilter.Visible = true;
                flowRooms.Visible = true;

                LoadRoomTiles();
            };

            detail.Saved += (s, e) =>
            {
                // reload dữ liệu phòng rồi cập nhật UI
                var updated = _roomDal.GetById(room.PhongID);
                if (updated != null)
                    room = updated;

                LoadRoomTiles();
            };

            panelDetailHost.Visible = true;
            panelDetailHost.Controls.Add(detail);
            panelDetailHost.BringToFront();

            detail.Show();
        }

        #endregion

        #region Các nút menu / filter

        private void btnRooms_Click(object sender, EventArgs e)
        {
            panelDetailHost.Visible = false;
            panelDetailHost.Controls.Clear();

            panelFilter.Visible = true;
            flowRooms.Visible = true;

            LoadRoomTiles();
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Chức năng báo cáo sẽ được bổ sung sau.", "Thông báo");
        }

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

        private void btnFilterAll_Click(object sender, EventArgs e)
        {
            _currentFilterStatus = null;
            LoadRoomTiles();
        }

        private void btnFilterTrong_Click(object sender, EventArgs e)
        {
            _currentFilterStatus = 0;
            LoadRoomTiles();
        }

        private void btnFilterCoKhach_Click(object sender, EventArgs e)
        {
            _currentFilterStatus = 1;
            LoadRoomTiles();
        }

        private void btnFilterChuaDon_Click(object sender, EventArgs e)
        {
            _currentFilterStatus = 2;
            LoadRoomTiles();
        }

        private void btnFilterDaDat_Click(object sender, EventArgs e)
        {
            _currentFilterStatus = 3;
            LoadRoomTiles();
        }

        #endregion
    }
}
