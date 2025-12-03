using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class MainForm : Form
    {
        private User _currentUser;
        private readonly RoomDAL _roomDal = new RoomDAL();

        // null = tất cả; 0..3 = theo trạng thái
        private int? _currentFilterStatus = null;

        // placeholder search
        private readonly Color _placeholderColor = Color.Gray;
        private readonly Color _normalColor = Color.Black;
        private const string _placeholderText = "Nhập từ khóa tìm kiếm";

        // Timer update đồng hồ thời gian
        private Timer _roomTimer;

        // Lưu các nhãn trong card phòng để tick timer cập nhật
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

            // user mặc định (khách)
            _currentUser = new User { Username = "Khách", Role = "Letan" };

            // co giãn tile khi đổi kích thước cửa sổ
            this.Resize += (s, e) => LoadRoomTiles();
        }

        public MainForm(User user) : this()
        {
            if (user != null) _currentUser = user;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateUserUI();
            InitSearchPlaceholder();
            LoadRoomTiles();
            SetupRoomTimer();
        }

        private void UpdateUserUI()
        {
            lblCurrentUser.Text = $"Người dùng: {_currentUser.Username} ({_currentUser.Role})";
            btnReports.Enabled = _currentUser.Role == "Admin";
        }

        #region Search placeholder

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
            if (txtSearch.ForeColor == _placeholderColor) return;
            if (e.KeyCode != Keys.Enter) return;

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
                                    string allText = string.Join(" ",
                                        roomPanel.Controls.OfType<Control>()
                                                 .SelectMany(k => Flatten(k))
                                                 .Select(k => k.Text.ToLower()));
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

            IEnumerable<Control> Flatten(Control ctrl)
            {
                yield return ctrl;
                foreach (Control ch in ctrl.Controls)
                    foreach (var x in Flatten(ch)) yield return x;
            }
        }

        #endregion

        #region Sơ đồ phòng

        private (int width, int height, int leftCol) CalcTileSize()
        {
            // 3 cột trên desktop, 2 khi hẹp
            int w = flowRooms.ClientSize.Width;
            if (w <= 0) w = this.ClientSize.Width - panelLeft.Width - 40;

            int cols = w < 900 ? 2 : 3;
            int tileW = Math.Max(300, (w - 40 - (cols - 1) * 24) / cols);
            int tileH = Math.Max(100, (int)(tileW * 0.38));
            int leftCol = Math.Min(100, Math.Max(80, tileW / 3));
            return (tileW, tileH, leftCol);
        }

        private void LoadRoomTiles()
        {
            flowRooms.SuspendLayout();
            flowRooms.Controls.Clear();

            var rooms = _roomDal.GetAll();
            if (_currentFilterStatus.HasValue)
                rooms = rooms.FindAll(r => r.TrangThai == _currentFilterStatus.Value);

            var tangGroups = rooms.GroupBy(r => r.Tang).OrderBy(g => g.Key);

            foreach (var group in tangGroups)
            {
                var panelTang = BuildFloorPanel(group.Key, group.ToList());
                flowRooms.Controls.Add(panelTang);
            }

            flowRooms.ResumeLayout();

            if (_roomTimer != null)
                RoomTimer_Tick(this, EventArgs.Empty);
        }

        private Panel BuildFloorPanel(int tang, List<Room> roomsInFloor)
        {
            var (tileW, tileH, _) = CalcTileSize();

            Panel panelTang = new Panel
            {
                Width = flowRooms.ClientSize.Width - 40,
                Height = 360,
                Margin = new Padding(10, 5, 10, 10),
                BackColor = Color.White
            };

            // header
            var header = new Panel { Height = 30, Dock = DockStyle.Top, BackColor = Color.White };
            var accent = new Panel { Width = 4, Height = 20, BackColor = Color.FromArgb(63, 81, 181), Location = new Point(0, 5) };
            var lbl = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 71, 79),
                Text = $"Tầng {tang}",
                Location = new Point(10, 5)
            };
            header.Controls.Add(accent); header.Controls.Add(lbl);
            panelTang.Controls.Add(header);

            // một dòng phòng đơn
            var lblDon = new Label { AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Gray, Text = "Phòng đơn", Location = new Point(10, 35) };
            var flowDon = new FlowLayoutPanel
            {
                Location = new Point(10, 55),
                Width = panelTang.Width - 30,
                Height = tileH + 16,
                AutoScroll = false,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            // một dòng phòng đôi
            var lblDoi = new Label { AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Gray, Text = "Phòng đôi", Location = new Point(10, 55 + tileH + 50) };
            var flowDoi = new FlowLayoutPanel
            {
                Location = new Point(10, 75 + tileH + 50),
                Width = panelTang.Width - 30,
                Height = tileH + 16,
                AutoScroll = false,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            foreach (var room in roomsInFloor.OrderBy(r => r.MaPhong))
            {
                var tile = CreateRoomTile(room);
                tile.Width = tileW;
                tile.Height = tileH;
                if (room.LoaiPhongID == 1) flowDon.Controls.Add(tile);
                else flowDoi.Controls.Add(tile);
            }

            panelTang.Controls.Add(lblDon);
            panelTang.Controls.Add(flowDon);
            panelTang.Controls.Add(lblDoi);
            panelTang.Controls.Add(flowDoi);
            return panelTang;
        }

        private Color GetRoomBackColor(int st)
        {
            switch (st)
            {
                case 0: return Color.FromArgb(76, 175, 80);   // Trống
                case 1: return Color.FromArgb(33, 150, 243);  // Có khách
                case 2: return Color.FromArgb(244, 67, 54);   // Chưa dọn
                case 3: return Color.FromArgb(255, 152, 0);   // Đã đặt
                default: return Color.FromArgb(158, 158, 158);
            }
        }
        private string GetStatusIcon(int st)
        {
            switch (st)
            {
                case 0: return "✔";
                case 1: return "🛏";
                case 2: return "🧹";
                case 3: return "📅";
                default: return "";
            }
        }

        // Đọc/ghi SL=<n> trong ghi chú
        private static int? ParseSoLuong(string ghiChu)
        {
            if (string.IsNullOrWhiteSpace(ghiChu)) return null;
            var m = Regex.Match(ghiChu, @"SL=(\d+)", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            int v;
            if (int.TryParse(m.Groups[1].Value, out v)) return v;
            return null;
        }

        private static string CombineCenter(string main, string remain, string note)
        {
            // 2–3 dòng: nội dung chính + dòng phụ + note
            if (!string.IsNullOrWhiteSpace(remain))
                main = main + "\n" + remain;
            if (!string.IsNullOrWhiteSpace(note))
                main = main + "\n" + note;
            return main;
        }

        private Panel CreateRoomTile(Room room)
        {
            var (tileW, tileH, leftCol) = CalcTileSize();

            Color baseColor = GetRoomBackColor(room.TrangThai);
            Color lightColor = ControlPaint.Light(baseColor, 0.80f);
            // chữ “cùng tông nhưng đậm hơn nền”
            Color textColor = ControlPaint.Dark(baseColor, 0.20f);

            var panel = new Panel
            {
                Width = tileW,
                Height = tileH,
                Margin = new Padding(12, 8, 12, 8),
                BackColor = Color.White
            };
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 210, 210)))
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            // Cột trái
            var leftPanel = new Panel { Width = leftCol, Dock = DockStyle.Left, BackColor = baseColor, Padding = new Padding(0, 6, 0, 6) };

            var lblStd = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "STD"   // nếu muốn bỏ thì để Text = "" hoặc comment dòng này
            };

            var lblIcon = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = GetStatusIcon(room.TrangThai)
            };

            var lblCode = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = room.MaPhong
            };

            leftPanel.Controls.Add(lblCode);
            leftPanel.Controls.Add(lblIcon);
            leftPanel.Controls.Add(lblStd);

            // Cột phải
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = lightColor, Padding = new Padding(6) };

            var lblStartTime = new Label
            {
                Height = 18,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(120, 0, 0, 0),
                Name = "lblStartTime"
            };

            var lblCenter = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = textColor,
                Name = "lblCenter"
            };

            var lblElapsed = new Label
            {
                Height = 20,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.BottomCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 0, 0, 0),
                Name = "lblElapsed"
            };

            rightPanel.Controls.Add(lblCenter);
            rightPanel.Controls.Add(lblElapsed);
            rightPanel.Controls.Add(lblStartTime);

            panel.Controls.Add(rightPanel);
            panel.Controls.Add(leftPanel);

            // Tooltip ghi chú (nếu có)
            if (!string.IsNullOrWhiteSpace(room.GhiChu))
            {
                var tip = new ToolTip();
                tip.SetToolTip(panel, room.GhiChu);
            }

            // hover
            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(250, 250, 250);
            panel.MouseLeave += (s, e) => panel.BackColor = Color.White;

            // click mở chi tiết (gắn cho cả con)
            void AttachClick(Control c)
            {
                c.Cursor = Cursors.Hand;
                c.Click += (s, e) => ShowRoomDetail(room);
                foreach (Control k in c.Controls) AttachClick(k);
            }
            AttachClick(panel);

            // text ban đầu – **đã bỏ switch expression, dùng switch thường**
            string center;
            switch (room.TrangThai)
            {
                case 0:
                    center = "Trống";
                    break;
                case 2:
                    center = "Chưa dọn";
                    break;
                case 3:
                    center = "Đã có khách đặt";
                    break;
                case 1:
                    center = room.TenKhachHienThi ?? "";
                    break;
                default:
                    center = "";
                    break;
            }

            string remain = CalcRemainText(room);
            string note = (room.TrangThai == 1 && !string.IsNullOrWhiteSpace(room.GhiChu)) ? "(Có ghi chú)" : null;

            lblCenter.Text = CombineCenter(center, remain, note);

            // Lưu info cho Timer
            var info = new RoomTileInfo
            {
                Room = room,
                LblStartTime = lblStartTime,
                LblCenter = lblCenter,
                LblElapsed = lblElapsed
            };
            panel.Tag = info;

            return panel;
        }

        private string CalcRemainText(Room room)
        {
            // Chỉ tính khi có khách và có thời gian bắt đầu
            if (room.TrangThai != 1 || !room.ThoiGianBatDau.HasValue)
                return null;

            int sl;
            if (!TryParseSoLuongFromNote(room.GhiChu, out sl))
                sl = 1;

            DateTime start = room.ThoiGianBatDau.Value;
            DateTime now = DateTime.Now;

            // Thuê ĐÊM
            if (room.KieuThue == 1)
            {
                DateTime end = start.AddDays(sl);
                TimeSpan left = end - now;
                int nights = (int)Math.Ceiling(left.TotalDays);
                if (nights <= 0) return "Quá hạn";
                return "Còn " + nights + " đêm";
            }

            // Thuê NGÀY
            if (room.KieuThue == 2)
            {
                DateTime end = start.AddDays(sl);
                TimeSpan left = end - now;
                int days = (int)Math.Ceiling(left.TotalDays);
                if (days <= 0) return "Quá hạn";
                return "Còn " + days + " ngày";
            }

            // Thuê GIỜ: KHÔNG hiển thị “Còn X giờ” trên màn hình chính
            if (room.KieuThue == 3)
            {
                return null;
            }

            return null;
        }
        private bool TryParseSoLuongFromNote(string ghiChu, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(ghiChu)) return false;

            var m = System.Text.RegularExpressions.Regex.Match(
                ghiChu, @"SL=(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!m.Success) return false;

            if (!int.TryParse(m.Groups[1].Value, out value)) return false;

            return true;
        }

        #endregion

        #region Timer cập nhật thời gian

        private void SetupRoomTimer()
        {
            if (_roomTimer != null) return;
            _roomTimer = new Timer { Interval = 1000 };
            _roomTimer.Tick += RoomTimer_Tick;
            _roomTimer.Start();
        }

        private void RoomTimer_Tick(object sender, EventArgs e)
        {
            foreach (Control floor in flowRooms.Controls)
            {
                if (floor is Panel panelTang)
                {
                    foreach (Control child in panelTang.Controls.OfType<FlowLayoutPanel>())
                    {
                        foreach (Control roomPanel in child.Controls)
                        {
                            Panel p = roomPanel as Panel;
                            if (p != null)
                            {
                                RoomTileInfo info = p.Tag as RoomTileInfo;
                                if (info != null)
                                {
                                    UpdateRoomTileTime(info);
                                }
                                else
                                {
                                    Room r = p.Tag as Room;
                                    if (r != null)
                                    {
                                        var info2 = new RoomTileInfo
                                        {
                                            Room = r,
                                            LblStartTime = FindByName<Label>(p, "lblStartTime"),
                                            LblCenter = FindByName<Label>(p, "lblCenter"),
                                            LblElapsed = FindByName<Label>(p, "lblElapsed"),
                                        };
                                        p.Tag = info2;
                                        UpdateRoomTileTime(info2);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // hàm phụ trợ tìm control theo Name (C# 7.3-compatible)
            T FindByName<T>(Control root, string name) where T : Control
            {
                foreach (Control c in Flatten(root))
                {
                    T t = c as T;
                    if (t != null && t.Name == name) return t;
                }
                return null;
            }

            IEnumerable<Control> Flatten(Control c)
            {
                yield return c;
                foreach (Control k in c.Controls)
                    foreach (Control x in Flatten(k))
                        yield return x;
            }
        }

        private void UpdateRoomTileTime(RoomTileInfo info)
        {
            Room r = info.Room;
            if (r.TrangThai == 1 && r.ThoiGianBatDau.HasValue)
            {
                DateTime start = r.ThoiGianBatDau.Value;
                DateTime now = DateTime.Now;

                info.LblStartTime.Text = start.ToString("dd/MM/yyyy, HH:mm");

                // tên (đêm/ngày hiện tên; giờ không hiện tên)
                string main = (r.KieuThue == 3) ? "" : (r.TenKhachHienThi ?? "");
                string remain = CalcRemainText(r);
                string note = (!string.IsNullOrWhiteSpace(r.GhiChu)) ? "(Có ghi chú)" : null;

                if (string.IsNullOrWhiteSpace(main))
                    main = "Có khách";

                info.LblCenter.Text = CombineCenter(main, remain, note);

                TimeSpan diff = now - start;
                if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;
                info.LblElapsed.Text = string.Format("{0:00} : {1:00} : {2:00}",
                    (int)diff.TotalHours, diff.Minutes, diff.Seconds);
            }
            else
            {
                info.LblStartTime.Text = "";
                info.LblElapsed.Text = "";

                // ĐÃ BỎ switch expression, dùng switch thường
                string text;
                switch (r.TrangThai)
                {
                    case 0:
                        text = "Trống";
                        break;
                    case 2:
                        text = "Chưa dọn";
                        break;
                    case 3:
                        text = "Đã có khách đặt";
                        break;
                    default:
                        text = "";
                        break;
                }
                info.LblCenter.Text = text;
            }
        }

        #endregion

        #region Điều hướng/forms

        private void ShowRoomDetail(Room room)
        {
            // Ẩn danh sách, hiện khung detail
            flowRooms.Visible = false;
            panelFilter.Visible = false;
            panelDetailHost.Controls.Clear();

            var detail = new RoomDetailForm(room)
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };

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
                if (login.ShowDialog(this) == DialogResult.OK && login.LoggedInUser != null)
                {
                    _currentUser = login.LoggedInUser;
                    UpdateUserUI();
                    MessageBox.Show("Xin chào: " + _currentUser.Username, "Đăng nhập thành công");
                }
            }
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnFilterAll_Click(object sender, EventArgs e) { _currentFilterStatus = null; LoadRoomTiles(); }
        private void btnFilterTrong_Click(object sender, EventArgs e) { _currentFilterStatus = 0; LoadRoomTiles(); }
        private void btnFilterCoKhach_Click(object sender, EventArgs e) { _currentFilterStatus = 1; LoadRoomTiles(); }
        private void btnFilterChuaDon_Click(object sender, EventArgs e) { _currentFilterStatus = 2; LoadRoomTiles(); }
        private void btnFilterDaDat_Click(object sender, EventArgs e) { _currentFilterStatus = 3; LoadRoomTiles(); }

        #endregion
    }
}
