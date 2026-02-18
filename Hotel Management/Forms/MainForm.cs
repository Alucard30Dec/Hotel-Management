using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class MainForm : Form
    {
        private User _currentUser;
        private readonly RoomDAL _roomDal = new RoomDAL();
        private readonly BookingDAL _bookingDal = new BookingDAL();
        private readonly InvoiceDAL _invoiceDal = new InvoiceDAL();
        private readonly ToolTip _roomToolTip = new ToolTip();

        private int? _currentFilterStatus = null;
        
        private readonly Color _placeholderColor = Color.Gray;
        private readonly Color _normalColor = Color.Black;
        private const string _placeholderText = "Nhập từ khóa tìm kiếm";
        
        private Timer _roomTimer;
        private ToolStripDropDown _emptyRoomDropDown;
        private Timer _realtimeWatcherTimer;
        private bool _isRealtimeRefreshing;
        private string _lastRoomStateFingerprint = string.Empty;
        private string _lastBookingStatsFingerprint = string.Empty;
        private bool _isRoomMapCheckRunning;
        private bool _roomTilesInitialized;
        private bool _isBookingStatsLoading;
        private Control _bookingStatisticsView;
        private Control _revenueReportView;

        private enum ActiveViewMode
        {
            RoomMap,
            BookingStatistics,
            RevenueReport,
            RoomDetail,
            HourlyCheckout
        }

        private enum BookingStatsGroupMode
        {
            Day,
            Month,
            Quarter,
            Year
        }
        private ActiveViewMode _activeView = ActiveViewMode.RoomMap;

        private DateTimePicker _statsFromPicker;
        private DateTimePicker _statsToPicker;
        private Label _statsRangeLabel;
        private Label _statsHourlyGuestsValue;
        private Label _statsOvernightGuestsValue;
        private Label _statsStayingValue;
        private Label _statsCompletedValue;
        private Label _statsRevenueValue;
        private DataGridView _statsDailyGrid;
        private DataGridView _statsRoomGrid;
        private Button _statsByDayButton;
        private Button _statsByMonthButton;
        private Button _statsByQuarterButton;
        private Button _statsByYearButton;
        private BookingStatsGroupMode _statsGroupMode = BookingStatsGroupMode.Day;
        private string _selectedStatsPeriodKey = string.Empty;
        private List<BookingDAL.BookingDetailStats> _statsCurrentBookings = new List<BookingDAL.BookingDetailStats>();

        private DateTimePicker _reportFromPicker;
        private DateTimePicker _reportToPicker;
        private Label _reportRangeLabel;
        private Label _reportTotalInvoicesValue;
        private Label _reportPaidInvoicesValue;
        private Label _reportUnpaidInvoicesValue;
        private Label _reportTotalRevenueValue;
        private Label _reportUnpaidRevenueValue;
        private DataGridView _reportDailyGrid;
        private DataGridView _reportRoomGrid;
        private DataGridView _reportInvoiceGrid;
        
        // ====== HẰNG SỐ GIÁ ======
        private const decimal GIA_DEM_DON_SAU_18 = 200000m;
        private const decimal GIA_DEM_DON_TRUOC_18 = 250000m;
        private const decimal GIA_DEM_DON_NHIEU = 250000m;

        private const decimal GIA_DEM_DOI_SAU_18 = 300000m;
        private const decimal GIA_DEM_DOI_TRUOC_18 = 350000m;
        private const decimal GIA_DEM_DOI_NHIEU = 350000m;

        private const decimal GIA_GIO_DON_DAU = 70000m;
        private const decimal GIA_GIO_DON_SAU = 20000m;

        private const decimal GIA_GIO_DOI_DAU = 120000m;
        private const decimal GIA_GIO_DOI_SAU = 30000m;

        private const decimal GIA_NUOC_NGOT = 20000m;
        private const decimal GIA_NUOC_SUOI = 10000m;

        private const decimal PHU_THU_TRA_TRE = 40000m;

        private class RoomTileInfo
        {
            public Room Room { get; set; }
            public Label LblStartTime { get; set; } 
            public Label LblCenter { get; set; }    
            public Label LblElapsed { get; set; }   
        }

        public MainForm()
        {
            InitializeComponent();
            _currentUser = new User { Username = "Khách", Role = "Letan" };
            btnThongKe.Click += btnThongKe_Click;
            btnReports.Click += btnReports_Click;

            _roomToolTip.AutoPopDelay = 8000;
            _roomToolTip.InitialDelay = 250;
            _roomToolTip.ReshowDelay = 120;
            _roomToolTip.ShowAlways = true;
        }

        public MainForm(User user) : this()
        {
            if (user != null) _currentUser = user;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializePerformanceSettings();
            UpdateUserUI();
            InitSearchPlaceholder();
            LoadRoomTiles();
            SetupRoomTimer();
            SetupRealtimeWatcher();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (_activeView == ActiveViewMode.RoomMap && flowRooms.Visible)
                LoadRoomTiles();
        }

        private void flowRooms_ControlAdded(object sender, ControlEventArgs e)
        {
            if (e.Control is Panel panelTang)
            {
                int panelWidth = Math.Max(300, flowRooms.ClientSize.Width - 20);
                panelTang.Width = panelWidth;
            }
        }

        private void InitializePerformanceSettings()
        {
            SetDoubleBuffered(flowRooms);
            SetDoubleBuffered(panelDetailHost);
        }

        private static void SetDoubleBuffered(Control control)
        {
            if (control == null) return;
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
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
            CloseEmptyRoomActionPopup();
            Point scrollPos = flowRooms.AutoScrollPosition;
            flowRooms.SuspendLayout();
            flowRooms.Controls.Clear();

            var allRooms = _roomDal.GetAll();
            _lastRoomStateFingerprint = BuildRoomStateFingerprint(allRooms);

            var rooms = allRooms;
            if (_currentFilterStatus.HasValue)
                rooms = rooms.FindAll(r => r.TrangThai == _currentFilterStatus.Value);
            var tangGroups = rooms.GroupBy(r => r.Tang).OrderBy(g => g.Key);

            foreach (var group in tangGroups)
            {
                var panelTang = BuildFloorPanel(group.Key, group.ToList());
                flowRooms.Controls.Add(panelTang);
            }

            flowRooms.ResumeLayout();
            flowRooms.AutoScrollPosition = new Point(Math.Abs(scrollPos.X), Math.Abs(scrollPos.Y));
            _roomTilesInitialized = true;

            if (_roomTimer != null)
                RoomTimer_Tick(this, EventArgs.Empty);

            if (!_isRealtimeRefreshing)
                _ = CheckRoomMapChangesAsync();
        }

        private static string BuildRoomStateFingerprint(IEnumerable<Room> rooms)
        {
            if (rooms == null) return string.Empty;

            var sb = new StringBuilder(1024);
            foreach (var room in rooms.OrderBy(x => x.PhongID))
            {
                sb.Append(room.PhongID).Append('|')
                  .Append(room.TrangThai).Append('|')
                  .Append(room.ThoiGianBatDau?.ToString("yyyyMMddHHmmss") ?? "").Append('|')
                  .Append(room.KieuThue?.ToString() ?? "").Append('|')
                  .Append(room.TenKhachHienThi ?? "").Append('|')
                  .Append(room.GhiChu ?? "").Append('\n');
            }

            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                return Convert.ToBase64String(sha.ComputeHash(bytes));
            }
        }

        private Panel BuildFloorPanel(int tang, List<Room> roomsInFloor)
        {
            var (tileW, tileH, _) = CalcTileSize();
            int panelWidth = Math.Max(300, flowRooms.ClientSize.Width - 20);
            Panel panelTang = new Panel
            {
                Width = panelWidth,
                Margin = new Padding(10, 5, 10, 10),
                BackColor = Color.White
            };
            var header = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = Color.White
            };
            var accent = new Panel
            {
                Width = 4,
                Height = 20,
                BackColor = Color.FromArgb(63, 81, 181),
                Location = new Point(0, 5)
            };
            var lbl = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 71, 79),
                Text = $"Tầng {tang}",
                Location = new Point(10, 5)
            };
            header.Controls.Add(accent);
            header.Controls.Add(lbl);
            panelTang.Controls.Add(header);

            var lblDon = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Gray,
                Text = "Phòng đơn",
                Location = new Point(10, 35)
            };
            var flowDon = new FlowLayoutPanel
            {
                Name = "flowDon",
                Location = new Point(10, 55),
                Width = panelTang.Width - 20,
                Height = tileH + 16,
                AutoScroll = false,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            var lblDoi = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Gray,
                Text = "Phòng đôi",
                Location = new Point(10, 55 + tileH + 50)
            };
            var flowDoi = new FlowLayoutPanel
            {
                Name = "flowDoi",
                Location = new Point(10, 75 + tileH + 50),
                Width = panelTang.Width - 20,
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

                if (room.LoaiPhongID == 1)
                    flowDon.Controls.Add(tile);
                else
                    flowDoi.Controls.Add(tile);
            }

            panelTang.Controls.Add(lblDon);
            panelTang.Controls.Add(flowDon);
            panelTang.Controls.Add(lblDoi);
            panelTang.Controls.Add(flowDoi);
            panelTang.Height = flowDoi.Bottom + 20;
            return panelTang;
        }

        private Color GetRoomBackColor(int st)
        {
            switch (st)
            {
                case 0: return Color.FromArgb(76, 175, 80);
                case 1: return Color.FromArgb(33, 150, 243);
                case 2: return Color.FromArgb(244, 67, 54);
                case 3: return Color.FromArgb(255, 152, 0);
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

        private static int GetIntTag(string text, string key, int defaultVal = 0)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            var m = Regex.Match(text, @"\b" + key + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int v))
                return v;
            return defaultVal;
        }
        private static decimal GetDecimalTag(string text, string key, decimal defaultVal = 0m)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            var m = Regex.Match(text, @"\b" + key + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && decimal.TryParse(m.Groups[1].Value, out decimal v))
                return v;
            return defaultVal;
        }
        private static string RemoveSystemTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string pattern = @"(\s*\|\s*)?(SL|NN|NS|DT)\s*=\s*[^|]*";
            string result = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase).Trim();
            if (result.EndsWith("|")) result = result.TrimEnd('|').Trim();
            return result;
        }

        private static string CombineCenter(string main, string remain, string note)
        {
            if (!string.IsNullOrWhiteSpace(remain))
                main = main + "\n" + remain;
            if (!string.IsNullOrWhiteSpace(note))
                main = main + "\n" + note;
            return main;
        }

        private decimal TinhTienDemForRoom(Room room, DateTime start, int soDem)
        {
            if (soDem <= 0) return 0m;
            bool laPhongDon = room.LoaiPhongID == 1;
            decimal tong = 0m;
            if (soDem == 1)
            {
                bool sau18h = start.TimeOfDay >= new TimeSpan(18, 0, 0);
                if (laPhongDon)
                    tong = sau18h ? GIA_DEM_DON_SAU_18 : GIA_DEM_DON_TRUOC_18;
                else
                    tong = sau18h ? GIA_DEM_DOI_SAU_18 : GIA_DEM_DOI_TRUOC_18;
            }
            else
            {
                decimal giaMoiDem = laPhongDon ? GIA_DEM_DON_NHIEU : GIA_DEM_DOI_NHIEU;
                tong = giaMoiDem * soDem;
            }

            DateTime now = DateTime.Now;
            DateTime ngayTraChuan = start.Date.AddDays(soDem).AddHours(12);
            if (now > ngayTraChuan)
                tong += PHU_THU_TRA_TRE;
            return tong;
        }

        private string CalcExtraText(Room room)
        {
            if (room.TrangThai != 1 || !room.ThoiGianBatDau.HasValue || !room.KieuThue.HasValue)
                return null;
            int kieu = room.KieuThue.Value;
            DateTime start = room.ThoiGianBatDau.Value;

            // ĐÊM
            if (kieu == 1)
            {
                int soDem = GetIntTag(room.GhiChu, "SL", 1);
                if (soDem <= 0) soDem = 1;

                decimal tienPhong = TinhTienDemForRoom(room, start, soDem);

                int nn = GetIntTag(room.GhiChu, "NN", 0);
                int ns = GetIntTag(room.GhiChu, "NS", 0);
                decimal tienNuoc = nn * GIA_NUOC_NGOT + ns * GIA_NUOC_SUOI;
                decimal daThu = GetDecimalTag(room.GhiChu, "DT", 0m);
                decimal conLai = Math.Max(0, tienPhong + tienNuoc - daThu);

                DateTime end = start.Date.AddDays(soDem);
                TimeSpan left = end - DateTime.Now;
                int nightsLeft = (int)Math.Ceiling(left.TotalDays);
                string line1 = nightsLeft <= 0 ? "Quá hạn" : "Còn " + nightsLeft + " đêm";
                string line2 = "Chưa thu: " + conLai.ToString("N0") + " đ";

                return line1 + "\n" + line2;
            }

            // GIỜ
            if (kieu == 3)
            {
                DateTime now = DateTime.Now;
                if (now < start) now = start;

                TimeSpan diff = now - start;
                int soGio = Math.Max(1, (int)Math.Ceiling(diff.TotalHours));
                bool laPhongDon = room.LoaiPhongID == 1;
                decimal giaGioDau = laPhongDon ? GIA_GIO_DON_DAU : GIA_GIO_DOI_DAU;
                decimal giaGioSau = laPhongDon ? GIA_GIO_DON_SAU : GIA_GIO_DOI_SAU;

                decimal tienPhong = (soGio <= 1)
                    ? giaGioDau
                    : giaGioDau + (soGio - 1) * giaGioSau;
                int nn = GetIntTag(room.GhiChu, "NN", 0);
                int ns = GetIntTag(room.GhiChu, "NS", 0);
                decimal tienNuoc = nn * GIA_NUOC_NGOT + ns * GIA_NUOC_SUOI;

                decimal daThu = GetDecimalTag(room.GhiChu, "DT", 0m);
                decimal conLai = Math.Max(0, tienPhong + tienNuoc - daThu);

                return "Phải thu: " + conLai.ToString("N0") + " đ";
            }

            return null;
        }

        private Panel CreateRoomTile(Room room)
        {
            var (tileW, tileH, leftCol) = CalcTileSize();
            Color baseColor = GetRoomBackColor(room.TrangThai);
            Color lightColor = ControlPaint.Light(baseColor, 0.80f);
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

            var leftPanel = new Panel { Width = leftCol, Dock = DockStyle.Left, BackColor = baseColor, Padding = new Padding(0, 6, 0, 6) };
            var lblStd = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "STD"
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

            if (!string.IsNullOrWhiteSpace(room.GhiChu))
            {
                _roomToolTip.SetToolTip(panel, RemoveSystemTags(room.GhiChu));
            }

            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(250, 250, 250);
            panel.MouseLeave += (s, e) => panel.BackColor = Color.White;

            void AttachClick(Control c)
            {
                c.Cursor = Cursors.Hand;
                if (room.TrangThai == 2) 
                {
                    c.Click += (s, e) => { };
                    c.DoubleClick += (s, e) => SetRoomFromDirtyToEmpty(room);
                }
                else
                {
                    c.Click += (s, e) => HandleRoomClick(room);
                }

                foreach (Control k in c.Controls) AttachClick(k);
            }
            AttachClick(panel);
            string center;
            switch (room.TrangThai)
            {
                case 0: center = "Trống"; break;
                case 2: center = "Chưa dọn"; break;
                case 3: center = "Đã có khách đặt"; break;
                case 1: center = room.TenKhachHienThi ?? ""; break;
                default: center = ""; break;
            }

            string extra = CalcExtraText(room);
            string note = (room.TrangThai == 1 && !string.IsNullOrWhiteSpace(room.GhiChu) &&
                           !string.IsNullOrWhiteSpace(RemoveSystemTags(room.GhiChu)))
                          ? "(Có ghi chú)" : null;
            lblCenter.Text = CombineCenter(center, extra, note);

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

        private void SetRoomFromDirtyToEmpty(Room room)
        {
            room.TrangThai = 0;
            room.ThoiGianBatDau = null;
            room.KieuThue = null;
            room.TenKhachHienThi = null;

            string ghiChu = RemoveSystemTags(room.GhiChu);
            _roomDal.UpdateTrangThaiFull(room.PhongID, room.TrangThai, ghiChu, null, null, null);
            room.GhiChu = ghiChu;

            LoadRoomTiles();
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

        private void SetupRealtimeWatcher()
        {
            if (_realtimeWatcherTimer != null) return;
            _realtimeWatcherTimer = new Timer { Interval = 700 };
            _realtimeWatcherTimer.Tick += RealtimeWatcherTimer_Tick;
            _realtimeWatcherTimer.Start();
        }

        private async void RealtimeWatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_isRealtimeRefreshing || IsDisposed || !Visible) return;
            if (_activeView != ActiveViewMode.RoomMap && _activeView != ActiveViewMode.BookingStatistics) return;

            _isRealtimeRefreshing = true;
            try
            {
                if (_activeView == ActiveViewMode.RoomMap)
                    await CheckRoomMapChangesAsync();
                else if (_activeView == ActiveViewMode.BookingStatistics)
                    await CheckBookingStatisticsChangesAsync();
            }
            catch
            {
                // Swallow to keep watcher alive.
            }
            finally
            {
                _isRealtimeRefreshing = false;
            }
        }

        private async Task CheckRoomMapChangesAsync()
        {
            if (_isRoomMapCheckRunning) return;
            _isRoomMapCheckRunning = true;
            try
            {
                string latestFingerprint;
                try
                {
                    latestFingerprint = await Task.Run(() => _roomDal.GetRoomStateFingerprint());
                }
                catch
                {
                    return;
                }

                if (string.IsNullOrEmpty(_lastRoomStateFingerprint))
                {
                    _lastRoomStateFingerprint = latestFingerprint;
                    return;
                }

                if (string.Equals(latestFingerprint, _lastRoomStateFingerprint, StringComparison.Ordinal))
                    return;

                _lastRoomStateFingerprint = latestFingerprint;

                if (_activeView == ActiveViewMode.RoomMap && flowRooms.Visible)
                    LoadRoomTiles();
            }
            finally
            {
                _isRoomMapCheckRunning = false;
            }
        }

        private async Task CheckBookingStatisticsChangesAsync()
        {
            if (_statsFromPicker == null || _statsToPicker == null) return;

            DateTime fromDate = _statsFromPicker.Value.Date;
            DateTime toDate = _statsToPicker.Value.Date;
            string latestFingerprint;
            try
            {
                latestFingerprint = await Task.Run(() => _bookingDal.GetBookingStatisticsFingerprint(fromDate, toDate));
            }
            catch
            {
                return;
            }

            if (string.IsNullOrEmpty(_lastBookingStatsFingerprint))
            {
                _lastBookingStatsFingerprint = latestFingerprint;
                return;
            }

            if (string.Equals(latestFingerprint, _lastBookingStatsFingerprint, StringComparison.Ordinal))
                return;

            _lastBookingStatsFingerprint = latestFingerprint;

            if (_activeView == ActiveViewMode.BookingStatistics && panelDetailHost.Visible)
                RequestLoadBookingStatisticsData(force: false);
        }

        private void RoomTimer_Tick(object sender, EventArgs e)
        {
            if (_activeView != ActiveViewMode.RoomMap || !flowRooms.Visible) return;

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

                string main = (r.KieuThue == 3) ? "" : (r.TenKhachHienThi ?? "");
                string extra = CalcExtraText(r);
                string note = (!string.IsNullOrWhiteSpace(r.GhiChu) &&
                               !string.IsNullOrWhiteSpace(RemoveSystemTags(r.GhiChu)))
                              ? "(Có ghi chú)" : null;
                if (string.IsNullOrWhiteSpace(main))
                    main = "Có khách";
                info.LblCenter.Text = CombineCenter(main, extra, note);

                TimeSpan diff = now - start;
                if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;
                info.LblElapsed.Text = string.Format("{0:00} : {1:00} : {2:00}",
                    (int)diff.TotalHours, diff.Minutes, diff.Seconds);
            }
            else
            {
                info.LblStartTime.Text = "";
                info.LblElapsed.Text = "";

                string text;
                switch (r.TrangThai)
                {
                    case 0: text = "Trống"; break;
                    case 2: text = "Chưa dọn"; break;
                    case 3: text = "Đã có khách đặt"; break;
                    default: text = ""; break;
                }
                info.LblCenter.Text = text;
            }
        }

        #endregion

        #region Điều hướng/forms

        private enum EmptyRoomAction
        {
            ByHour = 3,
            Overnight = 1
        }

        private void HandleRoomClick(Room room)
        {
            if (room == null) return;

            if (room.TrangThai == 0)
            {
                ShowEmptyRoomActionPopup(room, Cursor.Position);
                return;
            }

            if (room.TrangThai == 1 && room.KieuThue.HasValue && room.KieuThue.Value == 3)
            {
                ShowHourlyCheckout(room);
                return;
            }

            ShowRoomDetail(room);
        }

        private void ShowEmptyRoomActionPopup(Room room, Point clickScreenPoint)
        {
            CloseEmptyRoomActionPopup();

            var popupContent = BuildEmptyRoomPopupContent(room);
            var popupSize = popupContent.Size;
            var host = new ToolStripControlHost(popupContent)
            {
                AutoSize = false,
                Size = popupSize,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            var dropDown = new ToolStripDropDown
            {
                AutoClose = true,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                DropShadowEnabled = true,
                BackColor = Color.White
            };
            dropDown.Items.Add(host);
            dropDown.Closed += (s, e) =>
            {
                if (ReferenceEquals(_emptyRoomDropDown, dropDown))
                    _emptyRoomDropDown = null;
            };

            _emptyRoomDropDown = dropDown;
            Point popupScreenPoint = ClampPopupPoint(new Point(clickScreenPoint.X + 8, clickScreenPoint.Y + 8), popupSize);
            dropDown.Show(this, this.PointToClient(popupScreenPoint));
        }

        private Panel BuildEmptyRoomPopupContent(Room room)
        {
            var container = new Panel
            {
                Size = new Size(340, 182),
                BackColor = Color.White
            };

            container.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 230, 240)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, container.Width - 1, container.Height - 1);
                }
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.FromArgb(238, 246, 255)
            };

            var lblTitle = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(12, 8, 12, 0),
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 71, 136),
                Text = "Phòng " + room.MaPhong + " đang trống"
            };

            var lblHint = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 0, 12, 8),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                Text = "Chọn hình thức đặt phòng phù hợp"
            };

            header.Controls.Add(lblHint);
            header.Controls.Add(lblTitle);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12, 14, 12, 10)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var btnByHour = CreatePopupActionButton(
                "Đặt theo giờ",
                "Nhận phòng nhanh",
                Color.FromArgb(33, 150, 243),
                Color.FromArgb(235, 247, 255));
            btnByHour.Click += (s, e) =>
            {
                CloseEmptyRoomActionPopup();
                HandleEmptyRoomAction(room, EmptyRoomAction.ByHour);
            };

            var btnOvernight = CreatePopupActionButton(
                "Qua đêm",
                "Mở chi tiết nhận phòng",
                Color.FromArgb(46, 125, 50),
                Color.FromArgb(236, 248, 237));
            btnOvernight.Click += (s, e) =>
            {
                CloseEmptyRoomActionPopup();
                HandleEmptyRoomAction(room, EmptyRoomAction.Overnight);
            };

            body.Controls.Add(btnByHour, 0, 0);
            body.Controls.Add(btnOvernight, 1, 0);

            var footer = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = 22,
                Padding = new Padding(12, 0, 12, 6),
                Font = new Font("Segoe UI", 8.2F),
                ForeColor = Color.FromArgb(130, 130, 130),
                Text = "Click ra ngoài để đóng"
            };

            container.Controls.Add(body);
            container.Controls.Add(footer);
            container.Controls.Add(header);
            return container;
        }

        private Button CreatePopupActionButton(string title, string subtitle, Color borderColor, Color hoverColor)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 6, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 9.8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Text = title + Environment.NewLine + subtitle
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = borderColor;
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = hoverColor;

            return button;
        }

        private void HandleEmptyRoomAction(Room room, EmptyRoomAction action)
        {
            if (room == null) return;

            room.TrangThai = 1;
            room.KieuThue = (int)action;
            room.ThoiGianBatDau = DateTime.Now;
            room.TenKhachHienThi = null;

            if (action == EmptyRoomAction.ByHour)
            {
                _roomDal.UpdateTrangThaiFull(
                    room.PhongID,
                    room.TrangThai,
                    room.GhiChu,
                    room.ThoiGianBatDau,
                    room.KieuThue,
                    room.TenKhachHienThi);

                LoadRoomTiles();
                return;
            }

            ShowRoomDetail(room);
        }

        private Point ClampPopupPoint(Point desiredScreenPoint, Size popupSize)
        {
            Rectangle wa = Screen.FromPoint(desiredScreenPoint).WorkingArea;
            int x = Math.Max(wa.Left + 4, Math.Min(desiredScreenPoint.X, wa.Right - popupSize.Width - 4));
            int y = Math.Max(wa.Top + 4, Math.Min(desiredScreenPoint.Y, wa.Bottom - popupSize.Height - 4));
            return new Point(x, y);
        }

        private void CloseEmptyRoomActionPopup()
        {
            if (_emptyRoomDropDown == null) return;
            if (_emptyRoomDropDown.IsDisposed)
            {
                _emptyRoomDropDown = null;
                return;
            }

            _emptyRoomDropDown.Close();
            _emptyRoomDropDown = null;
        }

        private void ShowHourlyCheckout(Room room)
        {
            if (room == null) return;
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.HourlyCheckout;

            flowRooms.Visible = false;
            panelFilter.Visible = false;

            panelDetailHost.Dock = DockStyle.Fill;
            panelDetailHost.Controls.Clear();

            var checkout = new HourlyCheckoutForm(room)
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };

            checkout.BackRequested += (s, e) =>
            {
                ShowRoomMap();
            };

            checkout.Saved += (s, e) =>
            {
                var updated = _roomDal.GetById(room.PhongID);
                if (updated != null)
                    room = updated;
                LoadRoomTiles();
            };

            checkout.PaymentCompleted += (s, e) =>
            {
                LoadRoomTiles();
            };

            panelDetailHost.Visible = true;
            panelDetailHost.Controls.Add(checkout);
            panelDetailHost.BringToFront();
            checkout.Show();
        }

        private void ShowRoomDetail(Room room)
        {
            if (room == null) return;
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.RoomDetail;

            // Ẩn các panel danh sách phòng
            flowRooms.Visible = false;
            panelFilter.Visible = false;
            
            // --- CẬP NHẬT QUAN TRỌNG: Fill toàn bộ form ---
            panelDetailHost.Dock = DockStyle.Fill;
            panelDetailHost.Controls.Clear();

            var detail = new RoomDetailForm(room, false, null, @"Address\dvhc_optimized.json")
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };
            detail.BackRequested += (s, e) =>
            {
                ShowRoomMap();
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
            CloseEmptyRoomActionPopup();
            ShowRoomMap();
        }
        private void btnThongKe_Click(object sender, EventArgs e)
        {
            ShowBookingStatistics();
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            ShowRevenueReport();
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

        private void ShowBookingStatistics()
        {
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.BookingStatistics;
            flowRooms.Visible = false;
            panelFilter.Visible = false;

            panelDetailHost.Dock = DockStyle.Fill;
            panelDetailHost.Controls.Clear();

            bool createdNow = _bookingStatisticsView == null;
            if (createdNow)
            {
                _bookingStatisticsView = BuildBookingStatisticsView();
                ApplyDefaultBookingStatsRange();
                _lastBookingStatsFingerprint = string.Empty;
            }

            panelDetailHost.Controls.Add(_bookingStatisticsView);
            panelDetailHost.Visible = true;
            panelDetailHost.BringToFront();

            RequestLoadBookingStatisticsData(force: createdNow);
        }

        private void ApplyDefaultBookingStatsRange()
        {
            if (_statsFromPicker == null || _statsToPicker == null) return;

            try
            {
                var range = _bookingDal.GetBookingDateRange();
                if (!range.HasData)
                {
                    _statsFromPicker.Value = DateTime.Today;
                    _statsToPicker.Value = DateTime.Today;
                    return;
                }

                _statsFromPicker.Value = range.MinDate;
                _statsToPicker.Value = range.MaxDate < range.MinDate ? range.MinDate : range.MaxDate;
            }
            catch
            {
                _statsFromPicker.Value = DateTime.Today;
                _statsToPicker.Value = DateTime.Today;
            }
        }

        private void ShowRevenueReport()
        {
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.RevenueReport;
            flowRooms.Visible = false;
            panelFilter.Visible = false;

            panelDetailHost.Dock = DockStyle.Fill;
            panelDetailHost.Controls.Clear();

            if (_revenueReportView == null)
                _revenueReportView = BuildRevenueReportView();

            panelDetailHost.Controls.Add(_revenueReportView);
            panelDetailHost.Visible = true;
            panelDetailHost.BringToFront();

            LoadRevenueReportData();
        }

        private Control BuildRevenueReportView()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.FromArgb(246, 249, 253),
                Padding = new Padding(16, 14, 16, 14)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };
            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var title = new Label
            {
                Text = "Báo cáo doanh thu",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 53, 89)
            };

            var actionFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            var btnSeed = new Button
            {
                Text = "Tạo dữ liệu mẫu",
                Width = 128,
                Height = 34,
                Margin = new Padding(0, 4, 8, 4),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(23, 118, 85),
                BackColor = Color.White
            };
            btnSeed.FlatAppearance.BorderSize = 1;
            btnSeed.FlatAppearance.BorderColor = Color.FromArgb(175, 222, 205);
            btnSeed.Click += (s, e) => SeedReportSampleData();

            var btnExport = new Button
            {
                Text = "Xuất CSV",
                Width = 95,
                Height = 34,
                Margin = new Padding(0, 4, 8, 4),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(31, 71, 136),
                BackColor = Color.White
            };
            btnExport.FlatAppearance.BorderSize = 1;
            btnExport.FlatAppearance.BorderColor = Color.FromArgb(178, 197, 224);
            btnExport.Click += (s, e) => ExportRevenueCsv();

            var btnBack = new Button
            {
                Text = "Quay lại sơ đồ",
                Width = 130,
                Height = 34,
                Margin = new Padding(0, 4, 0, 4),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(31, 71, 136),
                BackColor = Color.White
            };
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(178, 197, 224);
            btnBack.Click += (s, e) => ShowRoomMap();

            actionFlow.Controls.Add(btnSeed);
            actionFlow.Controls.Add(btnExport);
            actionFlow.Controls.Add(btnBack);

            headerTable.Controls.Add(title, 0, 0);
            headerTable.Controls.Add(actionFlow, 1, 0);
            header.Controls.Add(headerTable);

            var filterCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 10, 0, 10)
            };
            filterCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(218, 228, 242)))
                    e.Graphics.DrawRectangle(pen, 0, 0, filterCard.Width - 1, filterCard.Height - 1);
            };

            var filterLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };
            filterLayout.Controls.Add(new Label
            {
                Text = "Từ ngày",
                AutoSize = true,
                Margin = new Padding(0, 9, 8, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 92, 125)
            });

            _reportFromPicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Today.AddDays(-30),
                Width = 120,
                Margin = new Padding(0, 5, 14, 0)
            };
            _reportToPicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Today,
                Width = 120,
                Margin = new Padding(0, 5, 14, 0)
            };

            filterLayout.Controls.Add(_reportFromPicker);
            filterLayout.Controls.Add(new Label
            {
                Text = "Đến ngày",
                AutoSize = true,
                Margin = new Padding(0, 9, 8, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 92, 125)
            });
            filterLayout.Controls.Add(_reportToPicker);

            var btnRefresh = new Button
            {
                Text = "Cập nhật",
                Width = 100,
                Height = 31,
                Margin = new Padding(0, 5, 12, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(33, 106, 186)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadRevenueReportData();
            filterLayout.Controls.Add(btnRefresh);

            _reportRangeLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 9, 0, 0),
                Font = new Font("Segoe UI", 9.1F),
                ForeColor = Color.FromArgb(103, 114, 132),
                Text = string.Empty
            };
            filterLayout.Controls.Add(_reportRangeLabel);
            filterCard.Controls.Add(filterLayout);

            var summaryGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 88,
                ColumnCount = 5,
                Margin = new Padding(0, 0, 0, 10)
            };
            for (int i = 0; i < 5; i++) summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            summaryGrid.Controls.Add(CreateStatCard("Số hóa đơn", out _reportTotalInvoicesValue, Color.FromArgb(31, 71, 136)), 0, 0);
            summaryGrid.Controls.Add(CreateStatCard("Đã thanh toán", out _reportPaidInvoicesValue, Color.FromArgb(43, 145, 114)), 1, 0);
            summaryGrid.Controls.Add(CreateStatCard("Chưa thanh toán", out _reportUnpaidInvoicesValue, Color.FromArgb(214, 94, 83)), 2, 0);
            summaryGrid.Controls.Add(CreateStatCard("Tổng doanh thu", out _reportTotalRevenueValue, Color.FromArgb(42, 118, 207)), 3, 0);
            summaryGrid.Controls.Add(CreateStatCard("Còn phải thu", out _reportUnpaidRevenueValue, Color.FromArgb(188, 79, 94)), 4, 0);

            var topContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            topContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));
            topContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));

            _reportDailyGrid = CreateStatsGrid();
            _reportRoomGrid = CreateStatsGrid();
            var dailyCard = CreateGridCard("Doanh thu theo ngày", _reportDailyGrid);
            var roomCard = CreateGridCard("Doanh thu theo phòng", _reportRoomGrid);
            roomCard.Margin = new Padding(8, 0, 0, 0);
            topContent.Controls.Add(dailyCard, 0, 0);
            topContent.Controls.Add(roomCard, 1, 0);

            _reportInvoiceGrid = CreateStatsGrid();
            var invoiceCard = CreateGridCard("Chi tiết hóa đơn", _reportInvoiceGrid);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(filterCard, 0, 1);
            root.Controls.Add(summaryGrid, 0, 2);
            root.Controls.Add(topContent, 0, 3);
            root.Controls.Add(invoiceCard, 0, 4);

            return root;
        }

        private Control BuildBookingStatisticsView()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.FromArgb(246, 249, 253),
                Padding = new Padding(16, 14, 16, 14)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };
            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var title = new Label
            {
                Text = "Thống kê đặt phòng",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 53, 89)
            };

            var btnBack = new Button
            {
                Text = "Quay lại sơ đồ",
                Width = 130,
                Height = 34,
                Margin = new Padding(0, 4, 0, 4),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(31, 71, 136),
                BackColor = Color.White
            };
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(178, 197, 224);
            btnBack.Click += (s, e) => ShowRoomMap();

            headerTable.Controls.Add(title, 0, 0);
            headerTable.Controls.Add(btnBack, 1, 0);
            header.Controls.Add(headerTable);

            var filterCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 10, 0, 10)
            };
            filterCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(218, 228, 242)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, filterCard.Width - 1, filterCard.Height - 1);
                }
            };

            var filterLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };
            filterLayout.Controls.Add(new Label
            {
                Text = "Từ ngày",
                AutoSize = true,
                Margin = new Padding(0, 9, 8, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 92, 125)
            });

            _statsFromPicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Today.AddDays(-6),
                Width = 120,
                Margin = new Padding(0, 5, 14, 0)
            };
            _statsToPicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Today,
                Width = 120,
                Margin = new Padding(0, 5, 14, 0)
            };

            filterLayout.Controls.Add(_statsFromPicker);
            filterLayout.Controls.Add(new Label
            {
                Text = "Đến ngày",
                AutoSize = true,
                Margin = new Padding(0, 9, 8, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 92, 125)
            });
            filterLayout.Controls.Add(_statsToPicker);

            var btnRefresh = new Button
            {
                Text = "Cập nhật",
                Width = 100,
                Height = 31,
                Margin = new Padding(0, 5, 12, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(33, 106, 186)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RequestLoadBookingStatisticsData(force: true);
            filterLayout.Controls.Add(btnRefresh);

            filterLayout.Controls.Add(new Label
            {
                Text = "Xem theo",
                AutoSize = true,
                Margin = new Padding(0, 9, 8, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 92, 125)
            });

            _statsByDayButton = CreateStatsGroupButton("Ngày");
            _statsByMonthButton = CreateStatsGroupButton("Tháng");
            _statsByQuarterButton = CreateStatsGroupButton("Quý");
            _statsByYearButton = CreateStatsGroupButton("Năm");
            _statsByDayButton.Click += (s, e) => SetBookingStatsGroupMode(BookingStatsGroupMode.Day);
            _statsByMonthButton.Click += (s, e) => SetBookingStatsGroupMode(BookingStatsGroupMode.Month);
            _statsByQuarterButton.Click += (s, e) => SetBookingStatsGroupMode(BookingStatsGroupMode.Quarter);
            _statsByYearButton.Click += (s, e) => SetBookingStatsGroupMode(BookingStatsGroupMode.Year);

            filterLayout.Controls.Add(_statsByDayButton);
            filterLayout.Controls.Add(_statsByMonthButton);
            filterLayout.Controls.Add(_statsByQuarterButton);
            filterLayout.Controls.Add(_statsByYearButton);

            _statsRangeLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 9, 0, 0),
                Font = new Font("Segoe UI", 9.1F),
                ForeColor = Color.FromArgb(103, 114, 132),
                Text = string.Empty
            };
            filterLayout.Controls.Add(_statsRangeLabel);
            filterCard.Controls.Add(filterLayout);

            var summaryGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 88,
                ColumnCount = 5,
                Margin = new Padding(0, 0, 0, 10)
            };
            for (int i = 0; i < 5; i++) summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            summaryGrid.Controls.Add(CreateStatCard("Khách giờ", out _statsHourlyGuestsValue, Color.FromArgb(31, 71, 136)), 0, 0);
            summaryGrid.Controls.Add(CreateStatCard("Khách đêm", out _statsOvernightGuestsValue, Color.FromArgb(236, 137, 41)), 1, 0);
            summaryGrid.Controls.Add(CreateStatCard("Đang ở", out _statsStayingValue, Color.FromArgb(42, 118, 207)), 2, 0);
            summaryGrid.Controls.Add(CreateStatCard("Đã trả", out _statsCompletedValue, Color.FromArgb(43, 145, 114)), 3, 0);
            summaryGrid.Controls.Add(CreateStatCard("Tổng doanh thu", out _statsRevenueValue, Color.FromArgb(188, 79, 94)), 4, 0);

            var contentGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));

            _statsDailyGrid = CreateStatsGrid();
            _statsRoomGrid = CreateStatsGrid();
            _statsDailyGrid.SelectionChanged += StatsDailyGrid_SelectionChanged;
            _statsDailyGrid.CellClick += StatsDailyGrid_CellClick;

            var dailyCard = CreateGridCard("Danh sách thống kê", _statsDailyGrid);
            var roomCard = CreateGridCard("Chi tiết theo phòng", _statsRoomGrid);
            roomCard.Margin = new Padding(8, 0, 0, 0);

            contentGrid.Controls.Add(dailyCard, 0, 0);
            contentGrid.Controls.Add(roomCard, 1, 0);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(filterCard, 0, 1);
            root.Controls.Add(summaryGrid, 0, 2);
            root.Controls.Add(contentGrid, 0, 3);

            SetBookingStatsGroupMode(BookingStatsGroupMode.Day, rebuild: false);
            return root;
        }

        private Panel CreateStatCard(string title, out Label valueLabel, Color accent)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(10, 8, 10, 8)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 230, 242)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(106, 120, 145)
            };

            valueLabel = new Label
            {
                Text = "0",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = accent
            };

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblTitle);
            return card;
        }

        private Button CreateStatsGroupButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = 66,
                Height = 31,
                Margin = new Padding(0, 5, 6, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(64, 82, 118)
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(198, 214, 238);
            return button;
        }

        private void SetBookingStatsGroupMode(BookingStatsGroupMode mode, bool rebuild = true)
        {
            _statsGroupMode = mode;
            UpdateBookingStatsGroupButtons();
            if (rebuild) RebuildBookingStatsPeriodRows();
        }

        private void UpdateBookingStatsGroupButtons()
        {
            var allButtons = new[] { _statsByDayButton, _statsByMonthButton, _statsByQuarterButton, _statsByYearButton };
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                btn.BackColor = Color.White;
                btn.ForeColor = Color.FromArgb(64, 82, 118);
                btn.FlatAppearance.BorderColor = Color.FromArgb(198, 214, 238);
            }

            Button activeButton = null;
            if (_statsGroupMode == BookingStatsGroupMode.Day) activeButton = _statsByDayButton;
            else if (_statsGroupMode == BookingStatsGroupMode.Month) activeButton = _statsByMonthButton;
            else if (_statsGroupMode == BookingStatsGroupMode.Quarter) activeButton = _statsByQuarterButton;
            else if (_statsGroupMode == BookingStatsGroupMode.Year) activeButton = _statsByYearButton;

            if (activeButton == null) return;
            activeButton.BackColor = Color.FromArgb(33, 106, 186);
            activeButton.ForeColor = Color.White;
            activeButton.FlatAppearance.BorderColor = Color.FromArgb(33, 106, 186);
        }

        private Panel CreateGridCard(string title, DataGridView grid)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 230, 242)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lbl = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 63, 103)
            };

            grid.Dock = DockStyle.Fill;
            grid.Margin = new Padding(0, 6, 0, 0);

            card.Controls.Add(grid);
            card.Controls.Add(lbl);
            return card;
        }

        private DataGridView CreateStatsGrid()
        {
            return new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(228, 236, 247),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(241, 246, 255),
                    ForeColor = Color.FromArgb(49, 73, 112),
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                EnableHeadersVisualStyles = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(59, 74, 99),
                    SelectionBackColor = Color.FromArgb(223, 236, 255),
                    SelectionForeColor = Color.FromArgb(20, 40, 70)
                }
            };
        }

        private sealed class BookingPeriodViewRow
        {
            public string KyKey { get; set; }
            public DateTime KyBatDau { get; set; }
            public string KyThongKe { get; set; }
            public int KhachGio { get; set; }
            public int KhachDem { get; set; }
            public int DangO { get; set; }
            public int DaTra { get; set; }
            public string TongDoanhThu { get; set; }
        }

        private sealed class BookingDetailViewRow
        {
            public string SoPhong { get; set; }
            public string ThoiGianNhan { get; set; }
            public string ThoiGianTra { get; set; }
            public string TongGioPhut { get; set; }
            public int NuocSuoi { get; set; }
            public int NuocNgot { get; set; }
            public string TongTien { get; set; }
        }

        private sealed class RevenueDailyViewRow
        {
            public string Ngay { get; set; }
            public int SoHoaDon { get; set; }
            public string TongDoanhThu { get; set; }
            public string DaThu { get; set; }
            public string ChuaThu { get; set; }
        }

        private sealed class RevenueRoomViewRow
        {
            public string MaPhong { get; set; }
            public int SoHoaDon { get; set; }
            public string TongDoanhThu { get; set; }
            public string DaThu { get; set; }
        }

        private sealed class RevenueInvoiceViewRow
        {
            public int HoaDonID { get; set; }
            public int DatPhongID { get; set; }
            public string NgayLap { get; set; }
            public string MaPhong { get; set; }
            public string KhachHang { get; set; }
            public string TongTien { get; set; }
            public string TrangThai { get; set; }
        }

        private void LoadBookingStatisticsData()
        {
            RequestLoadBookingStatisticsData(force: true);
        }

        private async void RequestLoadBookingStatisticsData(bool force)
        {
            if (_isBookingStatsLoading) return;
            if (_statsFromPicker == null || _statsToPicker == null) return;

            DateTime fromDate = _statsFromPicker.Value.Date;
            DateTime toDate = _statsToPicker.Value.Date;

            if (fromDate > toDate)
            {
                MessageBox.Show("Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.", "Khoảng thời gian không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _statsFromPicker.Focus();
                return;
            }

            _isBookingStatsLoading = true;
            try
            {
                string latestFingerprint = await Task.Run(() => _bookingDal.GetBookingStatisticsFingerprint(fromDate, toDate));
                if (!force &&
                    !string.IsNullOrEmpty(_lastBookingStatsFingerprint) &&
                    string.Equals(latestFingerprint, _lastBookingStatsFingerprint, StringComparison.Ordinal))
                {
                    return;
                }

                var data = await Task.Run(() => _bookingDal.GetBookingStatistics(fromDate, toDate));
                if (_statsFromPicker == null || _statsToPicker == null) return;
                if (_statsFromPicker.Value.Date != fromDate || _statsToPicker.Value.Date != toDate) return;

                BindBookingStatisticsData(data, fromDate, toDate);
                _lastBookingStatsFingerprint = latestFingerprint;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải dữ liệu thống kê.\n\nChi tiết: " + ex.Message, "Lỗi thống kê", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBookingStatsLoading = false;
            }
        }

        private void BindBookingStatisticsData(BookingDAL.BookingStatisticsData data, DateTime fromDate, DateTime toDate)
        {
            var summary = data?.Summary ?? new BookingDAL.BookingSummaryStats();

            _statsHourlyGuestsValue.Text = summary.HourlyGuests.ToString("N0");
            _statsOvernightGuestsValue.Text = summary.OvernightGuests.ToString("N0");
            _statsStayingValue.Text = summary.StayingBookings.ToString("N0");
            _statsCompletedValue.Text = summary.CompletedBookings.ToString("N0");
            _statsRevenueValue.Text = summary.TotalRevenue.ToString("N0") + " đ";
            _statsRangeLabel.Text = "Khoảng lọc: " + fromDate.ToString("dd/MM/yyyy") + " - " + toDate.ToString("dd/MM/yyyy");

            _statsCurrentBookings = (data?.Bookings ?? new List<BookingDAL.BookingDetailStats>())
                .OrderByDescending(x => x.CheckInTime)
                .ThenByDescending(x => x.DatPhongID)
                .ToList();

            RebuildBookingStatsPeriodRows();
        }

        private void RebuildBookingStatsPeriodRows()
        {
            if (_statsDailyGrid == null) return;
            if (_statsFromPicker == null || _statsToPicker == null) return;

            DateTime fromDate = _statsFromPicker.Value.Date;
            DateTime toDate = _statsToPicker.Value.Date;
            if (fromDate > toDate) return;

            string preferredKey = _selectedStatsPeriodKey;
            var periodRows = BuildBookingPeriodRows(_statsCurrentBookings, fromDate, toDate, _statsGroupMode);
            _statsDailyGrid.DataSource = periodRows;
            ApplyBookingPeriodGridHeaders();

            if (periodRows.Count == 0)
            {
                _selectedStatsPeriodKey = string.Empty;
                _statsRoomGrid.DataSource = new List<BookingDetailViewRow>();
                ApplyBookingDetailGridHeaders();
                return;
            }

            bool hasPreferred = !string.IsNullOrWhiteSpace(preferredKey) && periodRows.Any(x => x.KyKey == preferredKey);
            string keyToSelect = hasPreferred ? preferredKey : periodRows[0].KyKey;

            int rowIndex = -1;
            for (int i = 0; i < _statsDailyGrid.Rows.Count; i++)
            {
                string key = Convert.ToString(_statsDailyGrid.Rows[i].Cells["KyKey"]?.Value);
                if (!string.Equals(key, keyToSelect, StringComparison.Ordinal)) continue;
                rowIndex = i;
                break;
            }

            if (rowIndex >= 0 && _statsDailyGrid.Rows[rowIndex].Cells.Count > 0)
            {
                _statsDailyGrid.ClearSelection();
                _statsDailyGrid.Rows[rowIndex].Selected = true;
                _statsDailyGrid.CurrentCell = _statsDailyGrid.Rows[rowIndex].Cells["KyThongKe"];
            }

            BindBookingDetailsBySelectedPeriod();
        }

        private List<BookingPeriodViewRow> BuildBookingPeriodRows(List<BookingDAL.BookingDetailStats> bookings, DateTime fromDate, DateTime toDate, BookingStatsGroupMode mode)
        {
            var rows = new List<BookingPeriodViewRow>();
            var safeBookings = bookings ?? new List<BookingDAL.BookingDetailStats>();

            var grouped = safeBookings
                .GroupBy(x => GetBookingPeriodKey(x.CheckInTime, mode))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        DateTime periodStart = GetBookingPeriodStart(g.Min(x => x.CheckInTime), mode);
                        return new BookingPeriodViewRow
                        {
                            KyKey = g.Key,
                            KyBatDau = periodStart,
                            KyThongKe = FormatBookingPeriodLabel(periodStart, mode),
                            KhachGio = g.Count(x => x.IsHourly),
                            KhachDem = g.Count(x => !x.IsHourly),
                            DangO = g.Count(x => x.TrangThai == 1),
                            DaTra = g.Count(x => x.TrangThai == 2),
                            TongDoanhThu = g.Sum(x => x.TotalAmount).ToString("N0") + " đ"
                        };
                    });

            foreach (DateTime periodStart in EnumerateBookingPeriodStarts(fromDate, toDate, mode))
            {
                string key = GetBookingPeriodKey(periodStart, mode);
                BookingPeriodViewRow row;
                if (!grouped.TryGetValue(key, out row))
                {
                    row = new BookingPeriodViewRow
                    {
                        KyKey = key,
                        KyBatDau = periodStart,
                        KyThongKe = FormatBookingPeriodLabel(periodStart, mode),
                        KhachGio = 0,
                        KhachDem = 0,
                        DangO = 0,
                        DaTra = 0,
                        TongDoanhThu = "0 đ"
                    };
                }

                rows.Add(row);
            }

            return rows
                .OrderByDescending(x => x.KyBatDau)
                .ToList();
        }

        private IEnumerable<DateTime> EnumerateBookingPeriodStarts(DateTime fromDate, DateTime toDate, BookingStatsGroupMode mode)
        {
            if (fromDate > toDate) yield break;

            if (mode == BookingStatsGroupMode.Day)
            {
                for (DateTime date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                    yield return date;
                yield break;
            }

            if (mode == BookingStatsGroupMode.Month)
            {
                DateTime start = new DateTime(fromDate.Year, fromDate.Month, 1);
                DateTime end = new DateTime(toDate.Year, toDate.Month, 1);
                for (DateTime date = start; date <= end; date = date.AddMonths(1))
                    yield return date;
                yield break;
            }

            if (mode == BookingStatsGroupMode.Quarter)
            {
                DateTime start = GetQuarterStart(fromDate);
                DateTime end = GetQuarterStart(toDate);
                for (DateTime date = start; date <= end; date = date.AddMonths(3))
                    yield return date;
                yield break;
            }

            DateTime yearStart = new DateTime(fromDate.Year, 1, 1);
            DateTime yearEnd = new DateTime(toDate.Year, 1, 1);
            for (DateTime date = yearStart; date <= yearEnd; date = date.AddYears(1))
                yield return date;
        }

        private static DateTime GetQuarterStart(DateTime date)
        {
            int quarterStartMonth = ((date.Month - 1) / 3) * 3 + 1;
            return new DateTime(date.Year, quarterStartMonth, 1);
        }

        private static DateTime GetBookingPeriodStart(DateTime date, BookingStatsGroupMode mode)
        {
            if (mode == BookingStatsGroupMode.Day) return date.Date;
            if (mode == BookingStatsGroupMode.Month) return new DateTime(date.Year, date.Month, 1);
            if (mode == BookingStatsGroupMode.Quarter) return GetQuarterStart(date);
            return new DateTime(date.Year, 1, 1);
        }

        private static string GetBookingPeriodKey(DateTime date, BookingStatsGroupMode mode)
        {
            if (mode == BookingStatsGroupMode.Day) return date.ToString("yyyyMMdd");
            if (mode == BookingStatsGroupMode.Month) return date.ToString("yyyyMM");
            if (mode == BookingStatsGroupMode.Quarter) return date.Year + "Q" + (((date.Month - 1) / 3) + 1);
            return date.Year.ToString();
        }

        private static string FormatBookingPeriodLabel(DateTime periodStart, BookingStatsGroupMode mode)
        {
            if (mode == BookingStatsGroupMode.Day) return periodStart.ToString("dd/MM/yyyy");
            if (mode == BookingStatsGroupMode.Month) return periodStart.ToString("MM/yyyy");
            if (mode == BookingStatsGroupMode.Quarter) return "Q" + (((periodStart.Month - 1) / 3) + 1) + "/" + periodStart.Year;
            return periodStart.Year.ToString();
        }

        private void StatsDailyGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            BindBookingDetailsBySelectedPeriod();
        }

        private void StatsDailyGrid_SelectionChanged(object sender, EventArgs e)
        {
            BindBookingDetailsBySelectedPeriod();
        }

        private void BindBookingDetailsBySelectedPeriod()
        {
            if (_statsDailyGrid == null || _statsDailyGrid.CurrentRow == null)
            {
                _selectedStatsPeriodKey = string.Empty;
                _statsRoomGrid.DataSource = new List<BookingDetailViewRow>();
                ApplyBookingDetailGridHeaders();
                return;
            }

            string key = Convert.ToString(_statsDailyGrid.CurrentRow.Cells["KyKey"]?.Value);
            if (string.IsNullOrWhiteSpace(key))
            {
                _selectedStatsPeriodKey = string.Empty;
                _statsRoomGrid.DataSource = new List<BookingDetailViewRow>();
                ApplyBookingDetailGridHeaders();
                return;
            }

            _selectedStatsPeriodKey = key;
            var details = (_statsCurrentBookings ?? new List<BookingDAL.BookingDetailStats>())
                .Where(x => string.Equals(GetBookingPeriodKey(x.CheckInTime, _statsGroupMode), key, StringComparison.Ordinal))
                .OrderByDescending(x => x.CheckInTime)
                .ThenByDescending(x => x.DatPhongID)
                .Select(x => new BookingDetailViewRow
                {
                    SoPhong = x.MaPhong,
                    ThoiGianNhan = x.CheckInTime.ToString("dd/MM/yyyy HH:mm"),
                    ThoiGianTra = x.CheckOutTime.HasValue ? x.CheckOutTime.Value.ToString("dd/MM/yyyy HH:mm") : string.Empty,
                    TongGioPhut = FormatDurationShort(x.TotalDuration),
                    NuocSuoi = x.WaterBottleCount,
                    NuocNgot = x.SoftDrinkCount,
                    TongTien = x.TotalAmount.ToString("N0") + " đ"
                })
                .ToList();

            _statsRoomGrid.DataSource = details;
            ApplyBookingDetailGridHeaders();
        }

        private static string FormatDurationShort(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;
            int totalMinutes = (int)Math.Floor(duration.TotalMinutes);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return hours + "h " + minutes.ToString("00") + "m";
        }

        private void ApplyBookingPeriodGridHeaders()
        {
            if (_statsDailyGrid == null || _statsDailyGrid.Columns.Count == 0) return;

            if (_statsDailyGrid.Columns["KyKey"] != null) _statsDailyGrid.Columns["KyKey"].Visible = false;
            if (_statsDailyGrid.Columns["KyBatDau"] != null) _statsDailyGrid.Columns["KyBatDau"].Visible = false;
            if (_statsDailyGrid.Columns["KyThongKe"] != null) _statsDailyGrid.Columns["KyThongKe"].HeaderText = "Kỳ thống kê";
            if (_statsDailyGrid.Columns["KhachGio"] != null) _statsDailyGrid.Columns["KhachGio"].HeaderText = "Khách giờ";
            if (_statsDailyGrid.Columns["KhachDem"] != null) _statsDailyGrid.Columns["KhachDem"].HeaderText = "Khách đêm";
            if (_statsDailyGrid.Columns["DangO"] != null) _statsDailyGrid.Columns["DangO"].HeaderText = "Đang ở";
            if (_statsDailyGrid.Columns["DaTra"] != null) _statsDailyGrid.Columns["DaTra"].HeaderText = "Đã trả";
            if (_statsDailyGrid.Columns["TongDoanhThu"] != null)
            {
                _statsDailyGrid.Columns["TongDoanhThu"].HeaderText = "Tổng doanh thu";
                _statsDailyGrid.Columns["TongDoanhThu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void ApplyBookingDetailGridHeaders()
        {
            if (_statsRoomGrid == null || _statsRoomGrid.Columns.Count == 0) return;
            if (_statsRoomGrid.Columns["SoPhong"] != null) _statsRoomGrid.Columns["SoPhong"].HeaderText = "Số phòng";
            if (_statsRoomGrid.Columns["ThoiGianNhan"] != null) _statsRoomGrid.Columns["ThoiGianNhan"].HeaderText = "Thời gian nhận";
            if (_statsRoomGrid.Columns["ThoiGianTra"] != null) _statsRoomGrid.Columns["ThoiGianTra"].HeaderText = "Thời gian trả";
            if (_statsRoomGrid.Columns["TongGioPhut"] != null) _statsRoomGrid.Columns["TongGioPhut"].HeaderText = "Tổng giờ phút";
            if (_statsRoomGrid.Columns["NuocSuoi"] != null) _statsRoomGrid.Columns["NuocSuoi"].HeaderText = "Nước suối";
            if (_statsRoomGrid.Columns["NuocNgot"] != null) _statsRoomGrid.Columns["NuocNgot"].HeaderText = "Nước ngọt";
            if (_statsRoomGrid.Columns["TongTien"] != null)
            {
                _statsRoomGrid.Columns["TongTien"].HeaderText = "Tổng tiền";
                _statsRoomGrid.Columns["TongTien"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void LoadRevenueReportData()
        {
            if (_reportFromPicker == null || _reportToPicker == null) return;

            if (_reportFromPicker.Value.Date > _reportToPicker.Value.Date)
            {
                MessageBox.Show("Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.", "Khoảng thời gian không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _reportFromPicker.Focus();
                return;
            }

            InvoiceDAL.RevenueReportData data;
            try
            {
                data = _invoiceDal.GetRevenueReport(_reportFromPicker.Value.Date, _reportToPicker.Value.Date);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải dữ liệu báo cáo.\n\nChi tiết: " + ex.Message, "Lỗi báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var summary = data?.Summary ?? new InvoiceDAL.RevenueSummaryStats();
            _reportTotalInvoicesValue.Text = summary.TotalInvoices.ToString("N0");
            _reportPaidInvoicesValue.Text = summary.PaidInvoices.ToString("N0");
            _reportUnpaidInvoicesValue.Text = summary.UnpaidInvoices.ToString("N0");
            _reportTotalRevenueValue.Text = summary.TotalRevenue.ToString("N0") + " đ";
            _reportUnpaidRevenueValue.Text = summary.UnpaidRevenue.ToString("N0") + " đ";
            _reportRangeLabel.Text = "Khoảng lọc: " + _reportFromPicker.Value.ToString("dd/MM/yyyy") + " - " + _reportToPicker.Value.ToString("dd/MM/yyyy");

            var dailyRows = (data?.Daily ?? new List<InvoiceDAL.RevenueDailyStats>())
                .Select(x => new RevenueDailyViewRow
                {
                    Ngay = x.Date.ToString("dd/MM/yyyy"),
                    SoHoaDon = x.InvoiceCount,
                    TongDoanhThu = x.TotalRevenue.ToString("N0") + " đ",
                    DaThu = x.PaidRevenue.ToString("N0") + " đ",
                    ChuaThu = x.UnpaidRevenue.ToString("N0") + " đ"
                })
                .ToList();
            _reportDailyGrid.DataSource = dailyRows;
            ApplyRevenueDailyGridHeaders();

            var roomRows = (data?.ByRoom ?? new List<InvoiceDAL.RevenueRoomStats>())
                .Select(x => new RevenueRoomViewRow
                {
                    MaPhong = x.MaPhong,
                    SoHoaDon = x.InvoiceCount,
                    TongDoanhThu = x.TotalRevenue.ToString("N0") + " đ",
                    DaThu = x.PaidRevenue.ToString("N0") + " đ"
                })
                .ToList();
            _reportRoomGrid.DataSource = roomRows;
            ApplyRevenueRoomGridHeaders();

            var invoiceRows = (data?.Invoices ?? new List<InvoiceDAL.RevenueInvoiceStats>())
                .Select(x => new RevenueInvoiceViewRow
                {
                    HoaDonID = x.HoaDonID,
                    DatPhongID = x.DatPhongID,
                    NgayLap = x.NgayLap.ToString("dd/MM/yyyy HH:mm"),
                    MaPhong = x.MaPhong,
                    KhachHang = x.KhachHang,
                    TongTien = x.TongTien.ToString("N0") + " đ",
                    TrangThai = x.DaThanhToan ? "Đã thanh toán" : "Chưa thanh toán"
                })
                .ToList();
            _reportInvoiceGrid.DataSource = invoiceRows;
            ApplyRevenueInvoiceGridHeaders();
        }

        private void ApplyRevenueDailyGridHeaders()
        {
            if (_reportDailyGrid == null || _reportDailyGrid.Columns.Count == 0) return;
            if (_reportDailyGrid.Columns["Ngay"] != null) _reportDailyGrid.Columns["Ngay"].HeaderText = "Ngày";
            if (_reportDailyGrid.Columns["SoHoaDon"] != null) _reportDailyGrid.Columns["SoHoaDon"].HeaderText = "Số hóa đơn";
            if (_reportDailyGrid.Columns["TongDoanhThu"] != null)
            {
                _reportDailyGrid.Columns["TongDoanhThu"].HeaderText = "Tổng doanh thu";
                _reportDailyGrid.Columns["TongDoanhThu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_reportDailyGrid.Columns["DaThu"] != null)
            {
                _reportDailyGrid.Columns["DaThu"].HeaderText = "Đã thu";
                _reportDailyGrid.Columns["DaThu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_reportDailyGrid.Columns["ChuaThu"] != null)
            {
                _reportDailyGrid.Columns["ChuaThu"].HeaderText = "Chưa thu";
                _reportDailyGrid.Columns["ChuaThu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void ApplyRevenueRoomGridHeaders()
        {
            if (_reportRoomGrid == null || _reportRoomGrid.Columns.Count == 0) return;
            if (_reportRoomGrid.Columns["MaPhong"] != null) _reportRoomGrid.Columns["MaPhong"].HeaderText = "Mã phòng";
            if (_reportRoomGrid.Columns["SoHoaDon"] != null) _reportRoomGrid.Columns["SoHoaDon"].HeaderText = "Số hóa đơn";
            if (_reportRoomGrid.Columns["TongDoanhThu"] != null)
            {
                _reportRoomGrid.Columns["TongDoanhThu"].HeaderText = "Tổng doanh thu";
                _reportRoomGrid.Columns["TongDoanhThu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_reportRoomGrid.Columns["DaThu"] != null)
            {
                _reportRoomGrid.Columns["DaThu"].HeaderText = "Đã thu";
                _reportRoomGrid.Columns["DaThu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void ApplyRevenueInvoiceGridHeaders()
        {
            if (_reportInvoiceGrid == null || _reportInvoiceGrid.Columns.Count == 0) return;
            if (_reportInvoiceGrid.Columns["HoaDonID"] != null) _reportInvoiceGrid.Columns["HoaDonID"].HeaderText = "Mã HĐ";
            if (_reportInvoiceGrid.Columns["DatPhongID"] != null) _reportInvoiceGrid.Columns["DatPhongID"].HeaderText = "Mã đặt";
            if (_reportInvoiceGrid.Columns["NgayLap"] != null) _reportInvoiceGrid.Columns["NgayLap"].HeaderText = "Ngày lập";
            if (_reportInvoiceGrid.Columns["MaPhong"] != null) _reportInvoiceGrid.Columns["MaPhong"].HeaderText = "Phòng";
            if (_reportInvoiceGrid.Columns["KhachHang"] != null) _reportInvoiceGrid.Columns["KhachHang"].HeaderText = "Khách hàng";
            if (_reportInvoiceGrid.Columns["TongTien"] != null)
            {
                _reportInvoiceGrid.Columns["TongTien"].HeaderText = "Tổng tiền";
                _reportInvoiceGrid.Columns["TongTien"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_reportInvoiceGrid.Columns["TrangThai"] != null) _reportInvoiceGrid.Columns["TrangThai"].HeaderText = "Trạng thái";
        }

        private void SeedReportSampleData()
        {
            InvoiceDAL.SampleSeedResult result;
            try
            {
                result = _invoiceDal.SeedSampleReportData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tạo dữ liệu mẫu.\n\nChi tiết: " + ex.Message, "Lỗi seed dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (result.AlreadySeeded)
            {
                MessageBox.Show("Dữ liệu mẫu đã tồn tại trước đó.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "Đã tạo dữ liệu mẫu thành công.\n" +
                    "- Khách hàng: " + result.AddedCustomers + "\n" +
                    "- Đặt phòng: " + result.AddedBookings + "\n" +
                    "- Hóa đơn: " + result.AddedInvoices,
                    "Seed dữ liệu mẫu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            LoadRevenueReportData();
        }

        private void ExportRevenueCsv()
        {
            if (_reportFromPicker == null || _reportToPicker == null) return;

            InvoiceDAL.RevenueReportData data;
            try
            {
                data = _invoiceDal.GetRevenueReport(_reportFromPicker.Value.Date, _reportToPicker.Value.Date);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lấy dữ liệu để xuất CSV.\n\nChi tiết: " + ex.Message, "Lỗi xuất CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var invoices = data?.Invoices ?? new List<InvoiceDAL.RevenueInvoiceStats>();
            if (invoices.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu hóa đơn trong khoảng lọc để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV (*.csv)|*.csv";
                dialog.FileName = "bao-cao-doanh-thu-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("HoaDonID,DatPhongID,NgayLap,MaPhong,KhachHang,TongTien,DaThanhToan");

                    foreach (var row in invoices)
                    {
                        sb.Append(EscapeCsv(row.HoaDonID.ToString())).Append(",");
                        sb.Append(EscapeCsv(row.DatPhongID.ToString())).Append(",");
                        sb.Append(EscapeCsv(row.NgayLap.ToString("yyyy-MM-dd HH:mm:ss"))).Append(",");
                        sb.Append(EscapeCsv(row.MaPhong)).Append(",");
                        sb.Append(EscapeCsv(row.KhachHang)).Append(",");
                        sb.Append(EscapeCsv(row.TongTien.ToString("0.##"))).Append(",");
                        sb.Append(EscapeCsv(row.DaThanhToan ? "1" : "0"));
                        sb.AppendLine();
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
                    MessageBox.Show("Đã xuất báo cáo: " + dialog.FileName, "Xuất CSV thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ghi file CSV thất bại.\n\nChi tiết: " + ex.Message, "Lỗi xuất CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value == null) return "";
            bool needQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            if (!needQuote) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void ShowRoomMap()
        {
            _activeView = ActiveViewMode.RoomMap;
            panelDetailHost.Visible = false;
            panelDetailHost.Controls.Clear();
            panelDetailHost.Dock = DockStyle.Right;
            panelDetailHost.Width = 420;

            panelFilter.Visible = true;
            flowRooms.Visible = true;
            if (!_roomTilesInitialized)
            {
                LoadRoomTiles();
                return;
            }

            if (_roomTimer != null)
                RoomTimer_Tick(this, EventArgs.Empty);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_roomTimer != null)
            {
                _roomTimer.Stop();
                _roomTimer.Dispose();
                _roomTimer = null;
            }

            if (_realtimeWatcherTimer != null)
            {
                _realtimeWatcherTimer.Stop();
                _realtimeWatcherTimer.Dispose();
                _realtimeWatcherTimer = null;
            }

            _roomToolTip.Dispose();

            base.OnFormClosed(e);
        }

        #endregion
    }
}
