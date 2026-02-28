using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;

namespace HotelManagement.Forms
{
    public partial class MainForm : Form
    {
        private User _currentUser;
        private readonly RoomDAL _roomDal = new RoomDAL();
        private readonly BookingDAL _bookingDal = new BookingDAL();
        private readonly InvoiceDAL _invoiceDal = new InvoiceDAL();
        private readonly StatisticsDAL _statisticsDal = new StatisticsDAL();
        private readonly ToolTip _roomToolTip = new ToolTip();
        private readonly PricingService _pricingService = PricingService.Instance;

        private int? _currentFilterStatus = null;
        
        private readonly Color _placeholderColor = Color.Gray;
        private readonly Color _normalColor = Color.Black;
        private const string _placeholderText = "Nhập từ khóa tìm kiếm";
        
        private Timer _roomTimer;
        private ToolStripDropDown _emptyRoomDropDown;
        private Timer _realtimeWatcherTimer;
        private Timer _resizeDebounceTimer;
        private Timer _explorerDetailDebounceTimer;
        private bool _isRealtimeRefreshing;
        private string _lastRoomStateFingerprint = string.Empty;
        private string _lastBookingStatsFingerprint = string.Empty;
        private bool _isRoomMapCheckRunning;
        private bool _roomTilesInitialized;
        private bool _isRoomTilesLoading;
        private bool _roomTilesReloadPending;
        private bool _pendingRoomTilesForceBillingRefresh;
        private bool _isBookingStatsLoading;
        private bool _isBillingSnapshotRefreshRunning;
        private bool _isRevenueReportLoading;
        private bool _isReportSeedLoading;
        private bool _isRevenueCsvExporting;
        private readonly List<RoomTileInfo> _roomTileInfos = new List<RoomTileInfo>();
        private readonly Dictionary<string, Font> _roomTileFontCache = new Dictionary<string, Font>(StringComparer.Ordinal);
        private Dictionary<int, BookingDAL.ActiveRoomBillingSnapshot> _roomBillingSnapshots = new Dictionary<int, BookingDAL.ActiveRoomBillingSnapshot>();
        private DateTime _lastBillingSnapshotRefreshUtc = DateTime.MinValue;
        private DateTime _lastBillingSnapshotErrorToastUtc = DateTime.MinValue;
        private DateTime _lastRoomTilesErrorToastUtc = DateTime.MinValue;
        private DateTime _lastExplorerDetailErrorToastUtc = DateTime.MinValue;
        private DateTime _lastExplorerLoadedUtc = DateTime.MinValue;
        private DateTime _lastAuditLoadedUtc = DateTime.MinValue;
        private DateTime _lastManagementReloadUtc = DateTime.MinValue;
        private DateTime _lastRoomMapFingerprintCheckUtc = DateTime.MinValue;
        private DateTime _lastBookingStatsFingerprintCheckUtc = DateTime.MinValue;
        private int _lastRoomTilePerRow;
        private int _lastRoomTileLayoutWidth;
        private Control _bookingStatisticsView;
        private Control _revenueReportView;
        private Control _managementView;
        private string _lastExplorerLoadSignature = string.Empty;
        private string _lastAuditLoadSignature = string.Empty;
        private Task _geoDataWarmupTask;
        private bool _roomDetailWarmupStarted;
        private readonly Dictionary<string, Form> _transientDetailFormCache = new Dictionary<string, Form>(StringComparer.Ordinal);
        private readonly LinkedList<string> _transientDetailCacheOrder = new LinkedList<string>();
        private const int TRANSIENT_DETAIL_CACHE_LIMIT = 8;
        private const int VIEW_RELOAD_COOLDOWN_SECONDS = 12;
        private const int ROOMMAP_RESIZE_DEBOUNCE_MS = 180;
        private const int EXPLORER_DETAIL_DEBOUNCE_MS = 220;
        private const int ROOMMAP_FINGERPRINT_MIN_INTERVAL_MS = 6000;
        private const int BOOKING_STATS_FINGERPRINT_MIN_INTERVAL_MS = 8000;
        private const int BILLING_SNAPSHOT_REFRESH_SECONDS = 5;

        private enum ActiveViewMode
        {
            RoomMap,
            BookingStatistics,
            RevenueReport,
            Management,
            RoomDetail,
            HourlyCheckout,
            OvernightCheckout
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
        private Label _kpiTotalBookingsValue;
        private Label _kpiTotalRevenueValue;
        private Label _kpiHourlyBookingsValue;
        private Label _kpiOvernightBookingsValue;
        private Label _kpiExtrasRevenueValue;
        private Label _kpiCancelCountValue;
        private DataGridView _statsDailyGrid;
        private DataGridView _statsRoomGrid;
        private DataGridView _kpiRevenueTrendGrid;
        private DataGridView _kpiChannelGrid;
        private DataGridView _kpiRoomTypeGrid;
        private DataGridView _kpiCheckInHourGrid;
        private DataGridView _kpiCheckOutHourGrid;
        private Button _statsByDayButton;
        private Button _statsByMonthButton;
        private Button _statsByQuarterButton;
        private Button _statsByYearButton;
        private ComboBox _statsBookingTypeCombo;
        private BookingStatsGroupMode _statsGroupMode = BookingStatsGroupMode.Day;
        private string _selectedStatsPeriodKey = string.Empty;
        private List<BookingDAL.BookingDetailStats> _statsCurrentBookings = new List<BookingDAL.BookingDetailStats>();
        private TabControl _statsTabControl;

        private TextBox _explorerKeywordTextBox;
        private ComboBox _explorerStatusCombo;
        private ComboBox _explorerBookingTypeCombo;
        private ComboBox _explorerPaymentCombo;
        private ComboBox _explorerRoomTypeCombo;
        private ComboBox _explorerChannelCombo;
        private ComboBox _explorerSortCombo;
        private ComboBox _explorerPageSizeCombo;
        private Label _explorerPageLabel;
        private Label _explorerFilterSnapshotLabel;
        private DataGridView _explorerGrid;
        private DataGridView _explorerStayGrid;
        private DataGridView _explorerExtrasGrid;
        private DataGridView _explorerTimelineGrid;
        private TabControl _explorerDetailTabControl;
        private TabPage _explorerStayTab;
        private TabPage _explorerExtrasTab;
        private int _explorerCurrentPage = 1;
        private int _explorerTotalPages = 1;
        private string _explorerSavedFilterSnapshot = string.Empty;

        private ComboBox _auditEntityCombo;
        private TextBox _auditActorTextBox;
        private TextBox _auditKeywordTextBox;
        private DataGridView _auditGrid;
        private DataGridView _alertGrid;
        private Label _auditPageLabel;
        private int _auditCurrentPage = 1;
        private int _auditTotalPages = 1;
        private int _kpiLoadVersion;
        private int _explorerLoadVersion;
        private int _explorerDetailLoadVersion;
        private int _auditLoadVersion;

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
        
        private const int ROOM_TILE_GAP_X = 12;
        private const int ROOM_TILE_GAP_Y = 12;
        private const int ROOM_TILE_MIN_WIDTH = 190;
        private const int ROOM_TILE_MAX_WIDTH = 420;
        private const int ROOM_TILE_MIN_HEIGHT = 94;

        private class RoomTileInfo
        {
            public Room Room { get; set; }
            public Label LblStartTime { get; set; } 
            public Label LblCenter { get; set; }    
            public Label LblElapsed { get; set; }
            public Label LblIcon { get; set; }
            public Panel LeftPanel { get; set; }
            public Panel RightPanel { get; set; }
            public BookingDAL.ActiveRoomBillingSnapshot BillingSnapshot { get; set; }
            public int LastStatus { get; set; }
        }

        public MainForm()
        {
            InitializeComponent();
            _currentUser = new User { Username = "Khách", Role = "Letan" };
            AuditContext.CurrentActor = BuildAuditActor(_currentUser);
            btnThongKe.Click += btnThongKe_Click;
            btnQuanLy.Click += btnQuanLy_Click;

            _roomToolTip.AutoPopDelay = 8000;
            _roomToolTip.InitialDelay = 250;
            _roomToolTip.ReshowDelay = 120;
            _roomToolTip.ShowAlways = true;
            InitializeDebounceTimers();
        }

        public MainForm(User user) : this()
        {
            if (user != null) _currentUser = user;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            using (PerformanceTracker.Measure("MainForm.Load"))
            {
                InitializePerformanceSettings();
                UpdateUserUI();
                InitSearchPlaceholder();
                LoadRoomTiles();
                SetupRoomTimer();
                SetupRealtimeWatcher();
                BeginInvoke(new Action(StartGeoDataWarmup));
                BeginInvoke(new Action(StartRoomDetailWarmup));
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (_activeView == ActiveViewMode.RoomMap && flowRooms.Visible)
            {
                if (_resizeDebounceTimer == null)
                {
                    HandleRoomMapResizeDebounced();
                    return;
                }

                _resizeDebounceTimer.Stop();
                _resizeDebounceTimer.Start();
            }
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

        private void InitializeDebounceTimers()
        {
            if (_resizeDebounceTimer == null)
            {
                _resizeDebounceTimer = new Timer { Interval = ROOMMAP_RESIZE_DEBOUNCE_MS };
                _resizeDebounceTimer.Tick += (s, e) =>
                {
                    _resizeDebounceTimer.Stop();
                    HandleRoomMapResizeDebounced();
                };
            }

            if (_explorerDetailDebounceTimer == null)
            {
                _explorerDetailDebounceTimer = new Timer { Interval = EXPLORER_DETAIL_DEBOUNCE_MS };
                _explorerDetailDebounceTimer.Tick += (s, e) =>
                {
                    _explorerDetailDebounceTimer.Stop();
                    LoadExplorerDetailForSelection();
                };
            }
        }

        private void HandleRoomMapResizeDebounced()
        {
            if (IsDisposed || _activeView != ActiveViewMode.RoomMap || !flowRooms.Visible)
                return;

            int layoutWidth = Math.Max(320, flowRooms.ClientSize.Width - 20);
            int perRow = ResolveTilePerRow(layoutWidth);
            bool layoutBucketChanged = perRow != _lastRoomTilePerRow;
            bool significantWidthShift = Math.Abs(layoutWidth - _lastRoomTileLayoutWidth) >= 120;

            if (!_roomTilesInitialized || layoutBucketChanged || significantWidthShift)
            {
                RequestLoadRoomTiles(forceBillingRefresh: false);
                return;
            }

            ResizeFloorPanelsInPlace();
        }

        private void ResizeFloorPanelsInPlace()
        {
            int panelWidth = Math.Max(300, flowRooms.ClientSize.Width - 20);
            foreach (Panel floor in flowRooms.Controls.OfType<Panel>())
            {
                floor.Width = panelWidth;
                foreach (FlowLayoutPanel rowFlow in floor.Controls.OfType<FlowLayoutPanel>())
                    rowFlow.Width = floor.Width - 20;
            }
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
            btnQuanLy.Enabled = true;
            _roomToolTip.SetToolTip(btnQuanLy, "Mở màn quản lí giá/phụ thu và danh sách phòng.");
            AuditContext.CurrentActor = BuildAuditActor(_currentUser);
        }

        private static string BuildAuditActor(User user)
        {
            if (user == null) return "guest";
            string name = string.IsNullOrWhiteSpace(user.Username) ? "guest" : user.Username.Trim();
            string role = string.IsNullOrWhiteSpace(user.Role) ? "unknown" : user.Role.Trim();
            return name + ":" + role;
        }

        private async void StartGeoDataWarmup()
        {
            if (_geoDataWarmupTask != null) return;
            await Task.Delay(3000);
            if (IsDisposed) return;

            _geoDataWarmupTask = Task.Run(() =>
            {
                try
                {
                    var loader = new GeoDataLoader(@"Address\dvhc_optimized.json");
                    loader.Load();
                    loader.LoadOldToNewCommuneMap();
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("Geo data warmup failed.", new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message
                    });
                }
            });
        }

        private async void StartRoomDetailWarmup()
        {
            if (_roomDetailWarmupStarted) return;
            _roomDetailWarmupStarted = true;

            await Task.Delay(6000);
            if (IsDisposed) return;

            try
            {
                var warmupRoom = new Room
                {
                    PhongID = -1,
                    MaPhong = "WARMUP",
                    LoaiPhongID = 1,
                    Tang = 0,
                    TrangThai = 0
                };

                using (var warmupForm = new RoomDetailForm(warmupRoom, false, null, @"Address\dvhc_optimized.json"))
                {
                    warmupForm.CreateControl();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Room detail warmup failed.", new Dictionary<string, object>
                {
                    ["Error"] = ex.Message
                });
            }
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
            RequestLoadRoomTiles(forceBillingRefresh: true);
        }

        private void RequestLoadRoomTiles(bool forceBillingRefresh)
        {
            if (IsDisposed) return;

            if (_isRoomTilesLoading)
            {
                _roomTilesReloadPending = true;
                _pendingRoomTilesForceBillingRefresh = _pendingRoomTilesForceBillingRefresh || forceBillingRefresh;
                return;
            }

            _ = LoadRoomTilesAsync(forceBillingRefresh);
        }

        private async Task LoadRoomTilesAsync(bool forceBillingRefresh)
        {
            if (IsDisposed) return;
            _isRoomTilesLoading = true;

            try
            {
                using (var perf = PerformanceTracker.Measure("MainForm.LoadRoomTiles"))
                {
                    CloseEmptyRoomActionPopup();
                    Point scrollPos = flowRooms.AutoScrollPosition;

                    bool shouldRefreshBilling = forceBillingRefresh
                        || _roomBillingSnapshots == null
                        || _roomBillingSnapshots.Count == 0
                        || (DateTime.UtcNow - _lastBillingSnapshotRefreshUtc).TotalSeconds >= BILLING_SNAPSHOT_REFRESH_SECONDS;

                    Task<List<Room>> roomTask = Task.Run(() => _roomDal.GetAll());
                    Task<string> roomFingerprintTask = Task.Run(() => _roomDal.GetRoomStateFingerprint());
                    Task<BookingDAL.BookingSummaryStats> roomMapSummaryTask =
                        Task.Run(() => _bookingDal.GetRoomMapDailySummary(DateTime.Today));
                    Task<Dictionary<int, BookingDAL.ActiveRoomBillingSnapshot>> billingTask = shouldRefreshBilling
                        ? Task.Run(() => _bookingDal.GetActiveRoomBillingSnapshotsByRoom())
                        : Task.FromResult(_roomBillingSnapshots ?? new Dictionary<int, BookingDAL.ActiveRoomBillingSnapshot>());

                    await Task.WhenAll(roomTask, roomFingerprintTask, billingTask, roomMapSummaryTask);
                    if (IsDisposed) return;

                    var allRooms = roomTask.Result ?? new List<Room>();
                    _lastRoomStateFingerprint = roomFingerprintTask.Result ?? string.Empty;

                    if (shouldRefreshBilling)
                    {
                        _roomBillingSnapshots = billingTask.Result ?? new Dictionary<int, BookingDAL.ActiveRoomBillingSnapshot>();
                        _lastBillingSnapshotRefreshUtc = DateTime.UtcNow;
                    }

                    var rooms = allRooms;
                    if (_currentFilterStatus.HasValue)
                        rooms = rooms.FindAll(r => r.TrangThai == _currentFilterStatus.Value);
                    var tangGroups = rooms.GroupBy(r => r.Tang).OrderBy(g => g.Key);
                    UpdateRoomMapFilterSummary(roomMapSummaryTask.Result);

                    flowRooms.SuspendLayout();
                    try
                    {
                        _roomTileInfos.Clear();
                        flowRooms.Controls.Clear();
                        foreach (var group in tangGroups)
                        {
                            var panelTang = BuildFloorPanel(group.Key, group.ToList());
                            flowRooms.Controls.Add(panelTang);
                        }
                    }
                    finally
                    {
                        flowRooms.ResumeLayout();
                    }

                    flowRooms.AutoScrollPosition = new Point(Math.Abs(scrollPos.X), Math.Abs(scrollPos.Y));
                    _roomTilesInitialized = true;
                    _lastRoomTileLayoutWidth = Math.Max(320, flowRooms.ClientSize.Width - 20);
                    _lastRoomTilePerRow = ResolveTilePerRow(_lastRoomTileLayoutWidth);

                    perf.AddContext("RoomCount", allRooms.Count);
                    perf.AddContext("FloorCount", tangGroups.Count());

                    if (_roomTimer != null)
                        RoomTimer_Tick(this, EventArgs.Empty);

                    if (!_isRealtimeRefreshing)
                        await CheckRoomMapChangesAsync();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "MainForm.LoadRoomTiles failed.");
                if ((DateTime.UtcNow - _lastRoomTilesErrorToastUtc).TotalSeconds >= 10)
                {
                    _lastRoomTilesErrorToastUtc = DateTime.UtcNow;
                    ShowToast("Không thể tải sơ đồ phòng. Vui lòng thử lại.", true);
                }
            }
            finally
            {
                _isRoomTilesLoading = false;
                if (_roomTilesReloadPending)
                {
                    bool pendingForce = _pendingRoomTilesForceBillingRefresh;
                    _roomTilesReloadPending = false;
                    _pendingRoomTilesForceBillingRefresh = false;
                    RequestLoadRoomTiles(pendingForce);
                }
            }
        }

        private Panel BuildFloorPanel(int tang, List<Room> roomsInFloor)
        {
            int panelWidth = Math.Max(300, flowRooms.ClientSize.Width - 20);
            var donRooms = roomsInFloor
                .Where(r => r.LoaiPhongID == 1)
                .OrderBy(r => r.MaPhong)
                .ToList();
            var doiRooms = roomsInFloor
                .Where(r => r.LoaiPhongID != 1)
                .OrderBy(r => r.MaPhong)
                .ToList();

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
                Text = tang == 0 ? "Tầng trệt" : $"Tầng {tang}",
                Location = new Point(10, 5)
            };
            header.Controls.Add(accent);
            header.Controls.Add(lbl);
            panelTang.Controls.Add(header);

            int sectionTop = 35;
            if (donRooms.Count > 0)
            {
                var lblDon = new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.Gray,
                    Text = "Phòng đơn",
                    Location = new Point(10, sectionTop)
                };

                var flowDon = new FlowLayoutPanel
                {
                    Name = "flowDon",
                    Location = new Point(10, sectionTop + 20),
                    Width = panelTang.Width - 20,
                    Height = CalculateTileLayout(panelTang.Width - 20, donRooms.Count).flowHeight,
                    AutoScroll = false,
                    WrapContents = true,
                    FlowDirection = FlowDirection.LeftToRight
                };
                var donLayout = CalculateTileLayout(flowDon.Width, donRooms.Count);

                foreach (var room in donRooms)
                {
                    _roomBillingSnapshots.TryGetValue(room.PhongID, out var billing);
                    var tile = CreateRoomTile(room, donLayout.tileWidth, donLayout.tileHeight, donLayout.leftCol, billing);
                    flowDon.Controls.Add(tile);
                }

                panelTang.Controls.Add(lblDon);
                panelTang.Controls.Add(flowDon);
                sectionTop = flowDon.Bottom + 30;
            }

            if (doiRooms.Count > 0)
            {
                var lblDoi = new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.Gray,
                    Text = "Phòng đôi",
                    Location = new Point(10, sectionTop)
                };

                var flowDoi = new FlowLayoutPanel
                {
                    Name = "flowDoi",
                    Location = new Point(10, sectionTop + 20),
                    Width = panelTang.Width - 20,
                    Height = CalculateTileLayout(panelTang.Width - 20, doiRooms.Count).flowHeight,
                    AutoScroll = false,
                    WrapContents = true,
                    FlowDirection = FlowDirection.LeftToRight
                };
                var doiLayout = CalculateTileLayout(flowDoi.Width, doiRooms.Count);

                foreach (var room in doiRooms)
                {
                    _roomBillingSnapshots.TryGetValue(room.PhongID, out var billing);
                    var tile = CreateRoomTile(room, doiLayout.tileWidth, doiLayout.tileHeight, doiLayout.leftCol, billing);
                    flowDoi.Controls.Add(tile);
                }

                panelTang.Controls.Add(lblDoi);
                panelTang.Controls.Add(flowDoi);
                sectionTop = flowDoi.Bottom + 20;
            }

            panelTang.Height = Math.Max(header.Bottom + 16, sectionTop);
            return panelTang;
        }

        private static (int tileWidth, int tileHeight, int leftCol, int flowHeight) CalculateTileLayout(int flowWidth, int itemCount)
        {
            int safeWidth = Math.Max(320, flowWidth);
            int perRow = ResolveTilePerRow(safeWidth);

            int calculatedWidth = (safeWidth - (perRow - 1) * ROOM_TILE_GAP_X) / perRow;
            int tileWidth = Math.Max(ROOM_TILE_MIN_WIDTH, Math.Min(ROOM_TILE_MAX_WIDTH, calculatedWidth));
            int tileHeight = Math.Max(ROOM_TILE_MIN_HEIGHT, (int)(tileWidth * 0.36));
            int leftCol = Math.Min(100, Math.Max(70, (int)(tileWidth * 0.34)));
            int rowCount = Math.Max(1, (int)Math.Ceiling(Math.Max(1, itemCount) / (double)perRow));
            int flowHeight = rowCount * (tileHeight + ROOM_TILE_GAP_Y);

            return (tileWidth, tileHeight, leftCol, flowHeight);
        }

        private static int ResolveTilePerRow(int safeWidth)
        {
            if (safeWidth < 700) return 1;
            if (safeWidth < 1080) return 2;
            if (safeWidth < 1520) return 3;
            return 4;
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float ClampFloat(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private Color GetRoomBackColor(int st)
        {
            switch (st)
            {
                case 0: return Color.FromArgb(76, 175, 80);
                case 1: return Color.FromArgb(33, 150, 243);
                case 2: return Color.FromArgb(244, 67, 54);
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
                default: return "";
            }
        }

        private static string CombineCenter(string main, string remain)
        {
            string primary = (main ?? string.Empty).Trim();
            string secondary = (remain ?? string.Empty).Trim();

            if (primary.Length == 0) return secondary;
            if (secondary.Length == 0) return primary;
            return primary + "\n" + secondary;
        }

        private Panel CreateRoomTile(Room room, int tileW, int tileH, int leftCol, BookingDAL.ActiveRoomBillingSnapshot billingSnapshot)
        {
            Color baseColor = GetRoomBackColor(room.TrangThai);
            Color lightColor = ControlPaint.Light(baseColor, 0.80f);
            Color textColor = ControlPaint.Dark(baseColor, 0.20f);
            int leftPaddingY = ClampInt((int)Math.Round(tileH * 0.05), 4, 8);
            int stdHeight = ClampInt((int)Math.Round(tileH * 0.16), 16, 22);
            int iconHeight = ClampInt((int)Math.Round(tileH * 0.20), 18, 26);
            int startHeight = ClampInt((int)Math.Round(tileH * 0.16), 16, 22);
            int elapsedHeight = ClampInt((int)Math.Round(tileH * 0.18), 16, 24);
            int rightPadding = ClampInt((int)Math.Round(tileW * 0.02), 5, 9);
            float stdFontSize = ClampFloat(tileW * 0.032f, 7f, 8.5f);
            float iconFontSize = ClampFloat(tileW * 0.045f, 10f, 12f);
            float codeFontSize = ClampFloat(tileW * 0.055f, 13.5f, 18f);
            float startFontSize = ClampFloat(tileW * 0.03f, 7.5f, 9f);
            float centerFontSize = ClampFloat(tileW * 0.04f, 11f, 13.5f);
            float elapsedFontSize = ClampFloat(tileW * 0.033f, 8.5f, 10f);

            var panel = new Panel
            {
                Width = tileW,
                Height = tileH,
                Margin = new Padding(0, 0, ROOM_TILE_GAP_X, ROOM_TILE_GAP_Y),
                BackColor = Color.White
            };
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 210, 210)))
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var leftPanel = new Panel { Width = leftCol, Dock = DockStyle.Left, BackColor = baseColor, Padding = new Padding(0, leftPaddingY, 0, leftPaddingY) };
            var lblStd = new Label
            {
                Dock = DockStyle.Top,
                Height = stdHeight,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = GetRoomTileFont("Segoe UI", stdFontSize, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "STD"
            };
            var lblIcon = new Label
            {
                Dock = DockStyle.Bottom,
                Height = iconHeight,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = GetRoomTileFont("Segoe UI", iconFontSize, FontStyle.Bold),
                ForeColor = Color.White,
                Text = GetStatusIcon(room.TrangThai)
            };
            var lblCode = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = GetRoomTileFont("Segoe UI Semibold", codeFontSize, FontStyle.Bold),
                ForeColor = Color.White,
                Text = room.MaPhong
            };
            leftPanel.Controls.Add(lblCode);
            leftPanel.Controls.Add(lblIcon);
            leftPanel.Controls.Add(lblStd);

            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = lightColor, Padding = new Padding(rightPadding) };
            var lblStartTime = new Label
            {
                Height = startHeight,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = GetRoomTileFont("Segoe UI", startFontSize, FontStyle.Regular),
                ForeColor = Color.FromArgb(120, 0, 0, 0),
                Name = "lblStartTime"
            };
            var lblCenter = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Font = GetRoomTileFont("Segoe UI", centerFontSize, FontStyle.Bold),
                ForeColor = textColor,
                Name = "lblCenter"
            };
            var lblElapsed = new Label
            {
                Height = elapsedHeight,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.BottomCenter,
                Font = GetRoomTileFont("Segoe UI", elapsedFontSize, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 0, 0, 0),
                Name = "lblElapsed"
            };
            rightPanel.Controls.Add(lblCenter);
            rightPanel.Controls.Add(lblElapsed);
            rightPanel.Controls.Add(lblStartTime);

            panel.Controls.Add(rightPanel);
            panel.Controls.Add(leftPanel);

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
            var info = new RoomTileInfo
            {
                Room = room,
                LblStartTime = lblStartTime,
                LblCenter = lblCenter,
                LblElapsed = lblElapsed,
                LblIcon = lblIcon,
                LeftPanel = leftPanel,
                RightPanel = rightPanel,
                BillingSnapshot = billingSnapshot,
                LastStatus = room.TrangThai
            };
            panel.Tag = info;
            _roomTileInfos.Add(info);
            UpdateRoomTileTime(info);

            return panel;
        }

        private Font GetRoomTileFont(string family, float size, FontStyle style)
        {
            string key = family + "|" + Math.Round(size, 2) + "|" + (int)style;
            if (_roomTileFontCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var created = new Font(family, size, style);
            _roomTileFontCache[key] = created;
            return created;
        }

        private void SetRoomFromDirtyToEmpty(Room room)
        {
            if (room == null) return;

            UiExceptionHandler.Run(this, "MainForm.SetRoomFromDirtyToEmpty", () =>
            {
                bool cleared = _roomDal.TrySetDirtyToEmpty(room.PhongID);
                if (!cleared)
                {
                    LoadRoomTiles();
                    ShowToast("Không thể dọn phòng vì trạng thái đã thay đổi. Vui lòng tải lại.", true);
                    return;
                }

                room.TrangThai = (int)RoomStatus.Trong;
                room.ThoiGianBatDau = null;
                room.KieuThue = null;
                room.TenKhachHienThi = null;
                LoadRoomTiles();
            });
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
            _realtimeWatcherTimer = new Timer { Interval = 2500 };
            _realtimeWatcherTimer.Tick += RealtimeWatcherTimer_Tick;
            _realtimeWatcherTimer.Start();
        }

        private async void RealtimeWatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_isRealtimeRefreshing || IsDisposed || !Visible) return;
            if (_activeView != ActiveViewMode.RoomMap && _activeView != ActiveViewMode.BookingStatistics) return;

            DateTime nowUtc = DateTime.UtcNow;
            if (_activeView == ActiveViewMode.RoomMap)
            {
                if ((nowUtc - _lastRoomMapFingerprintCheckUtc).TotalMilliseconds < ROOMMAP_FINGERPRINT_MIN_INTERVAL_MS)
                    return;
            }
            else if ((nowUtc - _lastBookingStatsFingerprintCheckUtc).TotalMilliseconds < BOOKING_STATS_FINGERPRINT_MIN_INTERVAL_MS)
            {
                return;
            }

            _isRealtimeRefreshing = true;
            try
            {
                if (_activeView == ActiveViewMode.RoomMap)
                {
                    _lastRoomMapFingerprintCheckUtc = nowUtc;
                    await CheckRoomMapChangesAsync();
                }
                else if (_activeView == ActiveViewMode.BookingStatistics)
                {
                    _lastBookingStatsFingerprintCheckUtc = nowUtc;
                    await CheckBookingStatisticsChangesAsync();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Realtime watcher tick failed.", new Dictionary<string, object>
                {
                    ["Error"] = ex.Message
                });
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
                using (PerformanceTracker.Measure("MainForm.CheckRoomMapChanges"))
                {
                    string latestFingerprint;
                    try
                    {
                        latestFingerprint = await Task.Run(() => _roomDal.GetRoomStateFingerprint());
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Warn("Cannot read room-map realtime fingerprint.", new Dictionary<string, object>
                        {
                            ["Error"] = ex.Message
                        });
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
                        RequestLoadRoomTiles(forceBillingRefresh: false);
                }
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
            int? bookingType = GetSelectedStatsBookingType();
            using (PerformanceTracker.Measure("MainForm.CheckBookingStatisticsChanges"))
            {
                string latestFingerprint;
                try
                {
                    latestFingerprint = await Task.Run(() => _bookingDal.GetBookingStatisticsFingerprint(fromDate, toDate, bookingType));
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("Cannot read booking-statistics realtime fingerprint.", new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message
                    });
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
                    RequestLoadBookingStatisticsData(force: false, knownFingerprint: latestFingerprint);
            }
        }

        private void RoomTimer_Tick(object sender, EventArgs e)
        {
            if (_activeView != ActiveViewMode.RoomMap || !flowRooms.Visible) return;
            TriggerBillingSnapshotRefreshIfDue();

            for (int i = 0; i < _roomTileInfos.Count; i++)
            {
                var info = _roomTileInfos[i];
                if (info == null || info.Room == null) continue;
                UpdateRoomTileTime(info);
            }
        }

        private void UpdateRoomTileTime(RoomTileInfo info)
        {
            Room r = info.Room;
            if (r == null) return;

            if (_roomBillingSnapshots.TryGetValue(r.PhongID, out var loaded))
                info.BillingSnapshot = loaded;
            else
                info.BillingSnapshot = null;

            ApplyRoomTileVisualState(info, r.TrangThai);

            if (r.TrangThai == 1)
            {
                DateTime now = DateTime.Now;
                DateTime? resolvedStart = ResolveTileStartTime(r, info.BillingSnapshot, now);
                DateTime start = resolvedStart ?? now;

                info.LblStartTime.Text = resolvedStart.HasValue
                    ? start.ToString("dd/MM/yyyy, HH:mm")
                    : string.Empty;

                decimal due = CalculateDueAmount(
                    r,
                    start,
                    now,
                    info.BillingSnapshot,
                    out decimal roomCharge,
                    out decimal waterCharge,
                    out decimal lateFeeCharge,
                    out decimal totalCharge,
                    out decimal paidAmount,
                    out string lateFeeReason);

                bool isOvernight = IsOvernightBooking(r, info.BillingSnapshot);
                if (isOvernight)
                {
                    string periodLabel = ResolveOvernightRoomPhaseLabel(start, info.BillingSnapshot);
                    info.LblCenter.Text = due <= 0m
                        ? BuildOvernightPaidCenterText(r, periodLabel)
                        : periodLabel + "\nCần thu: " + ToMoneyCompact(due);

                    int bookedNights = Math.Max(1, info.BillingSnapshot?.SoDemLuuTru ?? 1);
                    info.LblElapsed.Text = "Còn " + bookedNights + " đêm";
                }
                else
                {
                    string mainText = BuildTileMainText(r);
                    info.LblCenter.Text = due <= 0m
                        ? CombineCenter(mainText, "Đã thanh toán")
                        : CombineCenter(mainText, "Cần thu: " + ToMoneyCompact(due));

                    TimeSpan diff = now - start;
                    if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;
                    info.LblElapsed.Text = string.Format("{0:00} : {1:00} : {2:00}",
                        (int)diff.TotalHours, diff.Minutes, diff.Seconds);
                }

                ApplyRoomTileMoneyTooltip(info, roomCharge, waterCharge, lateFeeCharge, totalCharge, paidAmount, due, lateFeeReason);
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
                    default: text = ""; break;
                }
                info.LblCenter.Text = text;
                ApplyRoomTileMoneyTooltip(info, 0m, 0m, 0m, 0m, 0m, 0m, clear: true);
            }
        }

        private void ApplyRoomTileVisualState(RoomTileInfo info, int status)
        {
            if (info == null) return;
            if (info.LastStatus == status && info.LeftPanel != null && info.RightPanel != null)
                return;

            Color baseColor = GetRoomBackColor(status);
            Color lightColor = ControlPaint.Light(baseColor, 0.80f);
            Color textColor = ControlPaint.Dark(baseColor, 0.20f);

            if (info.LeftPanel != null) info.LeftPanel.BackColor = baseColor;
            if (info.RightPanel != null) info.RightPanel.BackColor = lightColor;
            if (info.LblCenter != null) info.LblCenter.ForeColor = textColor;
            if (info.LblIcon != null) info.LblIcon.Text = GetStatusIcon(status);
            info.LastStatus = status;
        }

        private string BuildTileMainText(Room room)
        {
            if (room == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(room.TenKhachHienThi))
                return room.TenKhachHienThi.Trim();
            if (room.KieuThue == 1) return "Phòng đêm";
            if (room.KieuThue == 3) return "Phòng giờ";
            return "Có khách";
        }

        private decimal CalculateDueAmount(
            Room room,
            DateTime start,
            DateTime now,
            BookingDAL.ActiveRoomBillingSnapshot billing,
            out decimal roomCharge,
            out decimal waterCharge,
            out decimal lateFeeCharge,
            out decimal totalCharge,
            out decimal paidAmount,
            out string lateFeeReason)
        {
            roomCharge = 0m;
            waterCharge = 0m;
            lateFeeCharge = 0m;
            totalCharge = 0m;
            paidAmount = 0m;
            lateFeeReason = string.Empty;

            bool isOvernight = IsOvernightBooking(room, billing);
            bool isHourly = IsHourlyBooking(room, billing);

            if (isOvernight)
            {
                var breakdown = CalculateOvernightRoomChargeBreakdown(room, start, now, billing);
                roomCharge = breakdown.RoomBaseAmount;
                lateFeeCharge = breakdown.LateFeeAmount;
            }
            else if (isHourly)
            {
                roomCharge = CalculateHourlyRoomCharge(room, start, now);
            }

            waterCharge = Math.Max(0m, billing?.ExtrasAmount ?? 0m);
            paidAmount = Math.Max(0m, billing?.PaidAmount ?? 0m);
            totalCharge = roomCharge + waterCharge + lateFeeCharge;
            lateFeeReason = BuildLateFeeReasonText(lateFeeCharge, isOvernight);
            return Math.Max(0m, totalCharge - paidAmount);
        }

        private static bool IsOvernightBooking(Room room, BookingDAL.ActiveRoomBillingSnapshot billing)
        {
            if (billing != null)
            {
                if (billing.BookingType == (int)BookingType.Overnight) return true;
                if (billing.BookingType == (int)BookingType.Hourly) return false;
            }

            return room != null && room.KieuThue == (int)RentalType.Overnight;
        }

        private static bool IsHourlyBooking(Room room, BookingDAL.ActiveRoomBillingSnapshot billing)
        {
            if (billing != null)
            {
                if (billing.BookingType == (int)BookingType.Hourly) return true;
                if (billing.BookingType == (int)BookingType.Overnight) return false;
            }

            return room != null && room.KieuThue == (int)RentalType.Hourly;
        }

        private static DateTime? ResolveTileStartTime(Room room, BookingDAL.ActiveRoomBillingSnapshot billing, DateTime now)
        {
            DateTime? start = null;

            if (billing != null && billing.NgayDen > DateTime.MinValue)
                start = billing.NgayDen;
            else if (room != null && room.ThoiGianBatDau.HasValue)
                start = room.ThoiGianBatDau.Value;

            if (!start.HasValue)
                return null;

            if (start.Value > now)
                return now;

            return start.Value;
        }

        private decimal CalculateHourlyRoomCharge(Room room, DateTime start, DateTime now)
        {
            int roomType = room?.LoaiPhongID == 2 ? 2 : 1;
            return _pricingService.CalculateHourlyCharge(start, now, roomType);
        }

        private PricingService.OvernightChargeBreakdown CalculateOvernightRoomChargeBreakdown(
            Room room,
            DateTime start,
            DateTime now,
            BookingDAL.ActiveRoomBillingSnapshot billing)
        {
            int nights = Math.Max(1, billing?.SoDemLuuTru ?? 1);
            decimal nightlyRate = billing != null && billing.NightlyRate > 0m
                ? billing.NightlyRate
                : GetFallbackNightlyRateByRoomType(room);

            int roomType = room?.LoaiPhongID == 2 ? 2 : 1;
            return _pricingService.CalculateOvernightChargeBreakdown(start, nights, roomType, nightlyRate, now);
        }

        private void ApplyRoomTileMoneyTooltip(
            RoomTileInfo info,
            decimal roomCharge,
            decimal waterCharge,
            decimal lateFeeCharge,
            decimal totalCharge,
            decimal paidAmount,
            decimal dueAmount,
            string lateFeeReason = null,
            bool clear = false)
        {
            if (info == null) return;

            if (clear)
            {
                _roomToolTip.SetToolTip(info.LeftPanel, string.Empty);
                _roomToolTip.SetToolTip(info.RightPanel, string.Empty);
                _roomToolTip.SetToolTip(info.LblCenter, string.Empty);
                _roomToolTip.SetToolTip(info.LblElapsed, string.Empty);
                return;
            }

            string tip = "Tiền phòng: " + ToMoneyCompact(roomCharge)
                + "\nNước: " + ToMoneyCompact(waterCharge)
                + "\nPhụ thu: " + ToMoneyCompact(lateFeeCharge)
                + "\nTổng tiền: " + ToMoneyCompact(totalCharge)
                + "\nĐã thu: " + ToMoneyCompact(paidAmount)
                + "\nCần thu: " + ToMoneyCompact(dueAmount);
            if (!string.IsNullOrWhiteSpace(lateFeeReason))
                tip += "\nLý do phụ thu: " + lateFeeReason;

            _roomToolTip.SetToolTip(info.LeftPanel, tip);
            _roomToolTip.SetToolTip(info.RightPanel, tip);
            _roomToolTip.SetToolTip(info.LblCenter, tip);
            _roomToolTip.SetToolTip(info.LblElapsed, tip);
        }

        private string BuildLateFeeReasonText(decimal lateFee, bool overnight)
        {
            if (!overnight || lateFee <= 0m) return string.Empty;
            return "Trả trễ sau " + _pricingService.GetCurrentPricing().OvernightCheckoutHour + "h (+"
                + ToMoneyCompact(lateFee) + ")";
        }

        private string ResolveOvernightRoomPhaseLabel(DateTime start, BookingDAL.ActiveRoomBillingSnapshot billing)
        {
            int nights = Math.Max(1, billing?.SoDemLuuTru ?? 1);
            if (nights >= 2) return "Phòng ngày";

            return _pricingService.IsOvernightNightWindow(start) ? "Phòng đêm" : "Phòng ngày";
        }

        private static string BuildOvernightPaidCenterText(Room room, string periodLabel)
        {
            string guestName = NormalizeGuestNameForTile(room);
            if (string.IsNullOrWhiteSpace(guestName))
                return periodLabel + "\nĐã thanh toán";

            return periodLabel + "\nĐã thanh toán\n" + guestName;
        }

        private static string NormalizeGuestNameForTile(Room room)
        {
            string raw = room == null ? string.Empty : (room.TenKhachHienThi ?? string.Empty).Trim();
            if (raw.Length == 0) return string.Empty;

            const int maxLength = 24;
            if (raw.Length <= maxLength) return raw;
            return raw.Substring(0, maxLength - 3).TrimEnd() + "...";
        }

        private decimal GetFallbackNightlyRateByRoomType(Room room)
        {
            int roomType = room?.LoaiPhongID == 2 ? 2 : 1;
            return _pricingService.GetDefaultNightlyRate(roomType);
        }

        private static string ToMoneyCompact(decimal amount)
        {
            decimal safe = Math.Max(0m, amount);
            return safe.ToString("N0") + "đ";
        }

        private void UpdateRoomMapFilterSummary(BookingDAL.BookingSummaryStats summary)
        {
            if (lblFilterHourlyCount == null || lblFilterOvernightCount == null || lblFilterTodayIncome == null)
                return;

            int hourlyCount = summary == null ? 0 : Math.Max(0, summary.HourlyGuests);
            int overnightCount = summary == null ? 0 : Math.Max(0, summary.OvernightGuests);
            decimal todayRevenue = summary == null ? 0m : Math.Max(0m, summary.TotalRevenue);

            lblFilterHourlyCount.Text = "Lượt giờ: " + hourlyCount.ToString("N0");
            lblFilterOvernightCount.Text = "Lượt ngày/đêm: " + overnightCount.ToString("N0");
            lblFilterTodayIncome.Text = "Thu hôm nay: " + todayRevenue.ToString("N0") + "đ";
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

            if (room.TrangThai == 1 && room.KieuThue.HasValue && room.KieuThue.Value == 1)
            {
                ShowOvernightCheckout(room);
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
                "Mở quản lý qua đêm",
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

            UiExceptionHandler.Run(this, "MainForm.HandleEmptyRoomAction", () =>
            {
                if (action == EmptyRoomAction.ByHour)
                {
                    DateTime startTime = DateTime.Now;
                    bool started = _roomDal.TryStartOccupancyFromEmpty(room.PhongID, (int)RentalType.Hourly, startTime, null);
                    if (!started)
                    {
                        LoadRoomTiles();
                        ShowToast("Phòng vừa được nhận bởi người dùng khác. Vui lòng tải lại.", true);
                        return;
                    }

                    room.TrangThai = (int)RoomStatus.CoKhach;
                    room.KieuThue = (int)RentalType.Hourly;
                    room.ThoiGianBatDau = startTime;
                    room.TenKhachHienThi = null;

                    try
                    {
                        _bookingDal.EnsureBookingForRoom(room, 1);
                    }
                    catch
                    {
                        TryRollbackFailedFastCheckin(room.PhongID);
                        throw;
                    }
                    LoadRoomTiles();
                    return;
                }

                DateTime overnightStart = DateTime.Now;
                bool overnightStarted = _roomDal.TryStartOccupancyFromEmpty(room.PhongID, (int)RentalType.Overnight, overnightStart, null);
                if (!overnightStarted)
                {
                    LoadRoomTiles();
                    ShowToast("Phòng không còn trống. Vui lòng tải lại.", true);
                    return;
                }

                room.TrangThai = (int)RoomStatus.CoKhach;
                room.KieuThue = (int)RentalType.Overnight;
                room.ThoiGianBatDau = overnightStart;
                room.TenKhachHienThi = null;

                try
                {
                    _bookingDal.EnsureBookingForRoom(room, 2);
                }
                catch
                {
                    TryRollbackFailedFastCheckin(room.PhongID);
                    throw;
                }
                ShowOvernightCheckout(room);
            });
        }

        private void TryRollbackFailedFastCheckin(int phongId)
        {
            try
            {
                _roomDal.TryRollbackStartedOccupancyWithoutBooking(phongId);
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Rollback fast check-in state failed.", new Dictionary<string, object>
                {
                    ["RoomId"] = phongId,
                    ["Error"] = ex.Message
                });
            }
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

        private void ShowToast(string message, bool isError = false)
        {
            ToastNotifier.Show(this, message, isError);
        }

        private void ShowFriendlyError(string source, string message, Exception ex, string title = "Lỗi")
        {
            AppLogger.Error(ex, "Main form operation failed.", new Dictionary<string, object>
            {
                ["Source"] = source
            });
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void CloseToast()
        {
            ToastNotifier.Close(this);
        }

        private void PrepareDetailHostForFullView()
        {
            flowRooms.Visible = false;
            panelFilter.Visible = false;

            panelDetailHost.Dock = DockStyle.Fill;
            panelDetailHost.Visible = true;
            panelDetailHost.BringToFront();
        }

        private bool IsPersistentDetailView(Control control)
        {
            if (control == null) return false;
            return ReferenceEquals(control, _bookingStatisticsView)
                || ReferenceEquals(control, _managementView)
                || ReferenceEquals(control, _revenueReportView);
        }

        private bool IsTransientCachedView(Control control)
        {
            if (control == null) return false;
            foreach (var cached in _transientDetailFormCache.Values)
            {
                if (ReferenceEquals(cached, control))
                    return true;
            }

            return false;
        }

        private void DetachDetailHostControls(bool disposeDetachedTransientControls)
        {
            if (panelDetailHost.Controls.Count == 0) return;

            var controls = panelDetailHost.Controls.Cast<Control>().ToList();
            panelDetailHost.Controls.Clear();

            if (!disposeDetachedTransientControls) return;

            foreach (var control in controls)
            {
                if (control == null || control.IsDisposed) continue;
                if (IsPersistentDetailView(control)) continue;
                if (IsTransientCachedView(control)) continue;

                control.Dispose();
            }
        }

        private void ShowDetailView(Control control, bool disposeDetachedTransientControls = true)
        {
            if (control == null || control.IsDisposed) return;

            panelDetailHost.SuspendLayout();
            try
            {
                PrepareDetailHostForFullView();
                DetachDetailHostControls(disposeDetachedTransientControls);

                if (!panelDetailHost.Controls.Contains(control))
                    panelDetailHost.Controls.Add(control);

                control.Visible = true;
                control.BringToFront();
            }
            finally
            {
                panelDetailHost.ResumeLayout(true);
            }
        }

        private static string BuildTransientDetailCacheKey(string viewType, Room room)
        {
            if (room == null) return viewType + ":0";
            return viewType + ":" + room.PhongID;
        }

        private void TouchTransientDetailCacheKey(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey)) return;
            var existing = _transientDetailCacheOrder.Find(cacheKey);
            if (existing != null)
                _transientDetailCacheOrder.Remove(existing);
            _transientDetailCacheOrder.AddLast(cacheKey);
        }

        private void RemoveTransientDetailCacheKey(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey)) return;
            var existing = _transientDetailCacheOrder.Find(cacheKey);
            if (existing != null)
                _transientDetailCacheOrder.Remove(existing);
        }

        private void TrimTransientDetailCache()
        {
            while (_transientDetailFormCache.Count > TRANSIENT_DETAIL_CACHE_LIMIT)
            {
                if (_transientDetailCacheOrder.Count == 0) break;

                string key = _transientDetailCacheOrder.First.Value;
                _transientDetailCacheOrder.RemoveFirst();
                if (!_transientDetailFormCache.TryGetValue(key, out var form))
                    continue;

                _transientDetailFormCache.Remove(key);
                if (form == null || form.IsDisposed) continue;
                form.Dispose();
            }
        }

        private T GetOrCreateTransientDetailForm<T>(string cacheKey, Func<T> factory) where T : Form
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentException("Cache key is required.", nameof(cacheKey));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (_transientDetailFormCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached != null && !cached.IsDisposed && cached is T typed)
                {
                    TouchTransientDetailCacheKey(cacheKey);
                    return typed;
                }

                if (cached != null && !cached.IsDisposed)
                    cached.Dispose();

                _transientDetailFormCache.Remove(cacheKey);
                RemoveTransientDetailCacheKey(cacheKey);
            }

            var created = factory();
            if (created == null)
                throw new InvalidOperationException("Factory must return a valid form instance.");

            created.TopLevel = false;
            created.FormBorderStyle = FormBorderStyle.None;
            created.Dock = DockStyle.Fill;
            created.Disposed += (s, e) =>
            {
                _transientDetailFormCache.Remove(cacheKey);
                RemoveTransientDetailCacheKey(cacheKey);
            };
            _transientDetailFormCache[cacheKey] = created;
            TouchTransientDetailCacheKey(cacheKey);
            TrimTransientDetailCache();
            return created;
        }

        private void DisposeTransientDetailFormCache()
        {
            if (_transientDetailFormCache.Count == 0) return;

            var forms = _transientDetailFormCache.Values
                .Where(form => form != null)
                .Distinct()
                .ToList();
            _transientDetailFormCache.Clear();

            foreach (var form in forms)
            {
                if (form.IsDisposed) continue;
                form.Dispose();
            }

            _transientDetailCacheOrder.Clear();
        }

        private void ShowHourlyCheckout(Room room)
        {
            if (room == null) return;
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.HourlyCheckout;

            var checkout = CreateTransientDetailForm(() =>
            {
                var view = new HourlyCheckoutForm(room);
                view.BackRequested += (s, e) =>
                {
                    ShowRoomMap();
                };
                view.Saved += (s, e) =>
                {
                    LoadRoomTiles();
                    ShowToast("Đã lưu đặt phòng theo giờ.");
                };
                view.PaymentCompleted += (s, e) =>
                {
                    LoadRoomTiles();
                    ShowToast("Đã thanh toán thành công.");
                };
                return view;
            });

            ShowDetailView(checkout);
            checkout.Show();
        }

        private void ShowOvernightCheckout(Room room)
        {
            if (room == null) return;
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.OvernightCheckout;

            var checkout = CreateTransientDetailForm(() =>
            {
                var view = new OvernightCheckoutForm(room);
                view.BackRequested += (s, e) =>
                {
                    ShowRoomMap();
                };
                view.Saved += (s, e) =>
                {
                    LoadRoomTiles();
                    ShowRoomMap();
                    ShowToast("Đã lưu đặt phòng qua đêm.");
                };
                view.PaymentCompleted += (s, e) =>
                {
                    LoadRoomTiles();
                    ShowToast("Đã trả phòng thành công.");
                };
                return view;
            });

            ShowDetailView(checkout);
            checkout.Show();
        }

        private static T CreateTransientDetailForm<T>(Func<T> factory) where T : Form
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var created = factory();
            if (created == null)
                throw new InvalidOperationException("Factory must return a valid form instance.");

            created.TopLevel = false;
            created.FormBorderStyle = FormBorderStyle.None;
            created.Dock = DockStyle.Fill;
            return created;
        }

        private void ShowRoomDetail(Room room)
        {
            if (room == null) return;
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.RoomDetail;

            string cacheKey = BuildTransientDetailCacheKey("detail", room);
            var detail = GetOrCreateTransientDetailForm(cacheKey, () =>
            {
                var view = new RoomDetailForm(room, false, null, @"Address\dvhc_optimized.json");
                view.BackRequested += (s, e) =>
                {
                    ShowRoomMap();
                };
                view.Saved += (s, e) =>
                {
                    LoadRoomTiles();
                    ShowToast("Đã lưu đặt phòng.");
                };
                return view;
            });

            ShowDetailView(detail);
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
            ShowRoomMap();
        }

        private void btnQuanLy_Click(object sender, EventArgs e)
        {
            ShowManagement();
        }

        private void btnAdmin_Click(object sender, EventArgs e)
        {
            using (var login = new LoginForm())
            {
                if (login.ShowDialog(this) == DialogResult.OK && login.LoggedInUser != null)
                {
                    _currentUser = login.LoggedInUser;
                    UpdateUserUI();
                    ShowToast("Đăng nhập thành công: " + _currentUser.Username);
                }
            }
        }

        private void TriggerBillingSnapshotRefreshIfDue()
        {
            if (_isBillingSnapshotRefreshRunning) return;
            if ((DateTime.UtcNow - _lastBillingSnapshotRefreshUtc).TotalSeconds < BILLING_SNAPSHOT_REFRESH_SECONDS) return;
            _ = RefreshRoomBillingSnapshotsAsync();
        }

        private async Task RefreshRoomBillingSnapshotsAsync()
        {
            if (_isBillingSnapshotRefreshRunning) return;
            _isBillingSnapshotRefreshRunning = true;

            try
            {
                using (PerformanceTracker.Measure("MainForm.RefreshRoomBillingSnapshots"))
                {
                    var snapshotsTask = Task.Run(() => _bookingDal.GetActiveRoomBillingSnapshotsByRoom());
                    var summaryTask = Task.Run(() => _bookingDal.GetRoomMapDailySummary(DateTime.Today));
                    await Task.WhenAll(snapshotsTask, summaryTask);
                    if (IsDisposed) return;
                    _roomBillingSnapshots = snapshotsTask.Result ?? new Dictionary<int, BookingDAL.ActiveRoomBillingSnapshot>();
                    UpdateRoomMapFilterSummary(summaryTask.Result);
                    _lastBillingSnapshotRefreshUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Cannot refresh active room billing snapshots.", new Dictionary<string, object>
                {
                    ["Error"] = ex.Message
                });

                if ((DateTime.UtcNow - _lastBillingSnapshotErrorToastUtc).TotalSeconds >= 20)
                {
                    _lastBillingSnapshotErrorToastUtc = DateTime.UtcNow;
                    ShowToast("Không thể cập nhật công nợ realtime. Dữ liệu có thể chậm vài giây.", true);
                }
            }
            finally
            {
                _isBillingSnapshotRefreshRunning = false;
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

        private void ShowBookingStatistics()
        {
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.BookingStatistics;
            DisposeTransientDetailFormCache();

            bool createdNow = _bookingStatisticsView == null;
            if (createdNow)
            {
                _bookingStatisticsView = BuildBookingStatisticsView();
                ApplyDefaultBookingStatsRange();
                _lastBookingStatsFingerprint = string.Empty;
            }

            ShowDetailView(_bookingStatisticsView, disposeDetachedTransientControls: true);

            RequestLoadBookingStatisticsData(force: createdNow);
            if (createdNow)
            {
                _explorerCurrentPage = 1;
                _auditCurrentPage = 1;
            }
            LoadExplorerData(force: createdNow);
            LoadAuditAndAlerts(force: createdNow);
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
            // Báo cáo đã bị loại bỏ khỏi ứng dụng.
            ShowRoomMap();
        }

        private void ShowManagement()
        {
            CloseEmptyRoomActionPopup();
            _activeView = ActiveViewMode.Management;
            DisposeTransientDetailFormCache();

            if (_managementView == null)
            {
                var view = new ManagementControl(() => BuildAuditActor(_currentUser));
                view.RoomsChanged += (s, e) => LoadRoomTiles();
                _managementView = view;
                _lastManagementReloadUtc = DateTime.UtcNow;
            }
            else if (_managementView is ManagementControl mgmtView)
            {
                if ((DateTime.UtcNow - _lastManagementReloadUtc).TotalSeconds >= VIEW_RELOAD_COOLDOWN_SECONDS)
                {
                    mgmtView.ReloadData();
                    _lastManagementReloadUtc = DateTime.UtcNow;
                }
            }

            ShowDetailView(_managementView, disposeDetachedTransientControls: true);
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
                RowCount = 5,
                BackColor = Color.FromArgb(246, 249, 253),
                Padding = new Padding(16, 14, 16, 14)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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

            filterLayout.Controls.Add(new Label
            {
                Text = "Loại đặt",
                AutoSize = true,
                Margin = new Padding(0, 9, 8, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 92, 125)
            });
            _statsBookingTypeCombo = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 14, 0)
            };
            _statsBookingTypeCombo.Items.AddRange(new object[] { "Tất cả", "Phòng giờ", "Phòng đêm" });
            _statsBookingTypeCombo.SelectedIndex = 0;
            _statsBookingTypeCombo.SelectedIndexChanged += (s, e) =>
            {
                RequestLoadBookingStatisticsData(force: true);
            };
            filterLayout.Controls.Add(_statsBookingTypeCombo);

            var btnRefresh = new Button
            {
                Text = "Tải dữ liệu",
                Width = 108,
                Height = 31,
                Margin = new Padding(0, 5, 12, 0),
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(33, 106, 186)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) =>
            {
                RequestLoadBookingStatisticsData(force: true);
                _explorerCurrentPage = 1;
                LoadExplorerData(force: true);
                _auditCurrentPage = 1;
                LoadAuditAndAlerts(force: true);
            };
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

            var kpiGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 88,
                ColumnCount = 6,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };
            for (int i = 0; i < 6; i++) kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));
            kpiGrid.Controls.Add(CreateStatCard("Tổng lượt đặt", out _kpiTotalBookingsValue, Color.FromArgb(33, 106, 186)), 0, 0);
            kpiGrid.Controls.Add(CreateStatCard("Tổng doanh thu", out _kpiTotalRevenueValue, Color.FromArgb(31, 71, 136)), 1, 0);
            kpiGrid.Controls.Add(CreateStatCard("Lượt PHÒNG GIỜ", out _kpiHourlyBookingsValue, Color.FromArgb(188, 79, 94)), 2, 0);
            kpiGrid.Controls.Add(CreateStatCard("Lượt PHÒNG ĐÊM", out _kpiOvernightBookingsValue, Color.FromArgb(71, 120, 66)), 3, 0);
            kpiGrid.Controls.Add(CreateStatCard("Doanh thu phát sinh", out _kpiExtrasRevenueValue, Color.FromArgb(236, 137, 41)), 4, 0);
            kpiGrid.Controls.Add(CreateStatCard("Hủy / No-show", out _kpiCancelCountValue, Color.FromArgb(214, 94, 83)), 5, 0);

            _statsTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            var tabDashboard = new TabPage("Dashboard KPI") { BackColor = Color.FromArgb(246, 249, 253) };
            var tabExplorer = new TabPage("Data Explorer") { BackColor = Color.FromArgb(246, 249, 253) };
            var tabAudit = new TabPage("Audit & Cảnh báo") { BackColor = Color.FromArgb(246, 249, 253) };

            var dashboardRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            dashboardRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));
            dashboardRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));

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
            _statsRoomGrid.CellClick += StatsRoomGrid_CellClick;

            var dailyCard = CreateGridCard("Danh sách thống kê", _statsDailyGrid);
            var roomCard = CreateGridCard("Chi tiết theo phòng", _statsRoomGrid);
            roomCard.Margin = new Padding(8, 0, 0, 0);

            contentGrid.Controls.Add(dailyCard, 0, 0);
            contentGrid.Controls.Add(roomCard, 1, 0);

            var kpiDetailGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            kpiDetailGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
            kpiDetailGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));

            _kpiRevenueTrendGrid = CreateStatsGrid();
            var trendCard = CreateGridCard("Xu hướng doanh thu theo ngày", _kpiRevenueTrendGrid);

            var rightKpiGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(8, 0, 0, 0),
                BackColor = Color.Transparent
            };
            rightKpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            rightKpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            rightKpiGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            rightKpiGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _kpiChannelGrid = CreateStatsGrid();
            _kpiRoomTypeGrid = CreateStatsGrid();
            _kpiCheckInHourGrid = CreateStatsGrid();
            _kpiCheckOutHourGrid = CreateStatsGrid();

            rightKpiGrid.Controls.Add(CreateGridCard("Top nguồn đặt phòng", _kpiChannelGrid), 0, 0);
            rightKpiGrid.Controls.Add(CreateGridCard("Top loại phòng", _kpiRoomTypeGrid), 1, 0);
            rightKpiGrid.Controls.Add(CreateGridCard("Khung giờ check-in", _kpiCheckInHourGrid), 0, 1);
            rightKpiGrid.Controls.Add(CreateGridCard("Khung giờ check-out", _kpiCheckOutHourGrid), 1, 1);

            kpiDetailGrid.Controls.Add(trendCard, 0, 0);
            kpiDetailGrid.Controls.Add(rightKpiGrid, 1, 0);

            dashboardRoot.Controls.Add(contentGrid, 0, 0);
            dashboardRoot.Controls.Add(kpiDetailGrid, 0, 1);
            tabDashboard.Controls.Add(dashboardRoot);

            tabExplorer.Controls.Add(BuildExplorerTab());
            tabAudit.Controls.Add(BuildAuditTab());

            _statsTabControl.TabPages.Add(tabDashboard);
            _statsTabControl.TabPages.Add(tabExplorer);
            _statsTabControl.TabPages.Add(tabAudit);
            _statsTabControl.SelectedIndexChanged += (s, e) =>
            {
                if (_statsTabControl.SelectedIndex == 1)
                {
                    if (_explorerGrid != null && _explorerGrid.Rows.Count == 0)
                        LoadExplorerData();
                }
                else if (_statsTabControl.SelectedIndex == 2)
                {
                    if (_auditGrid != null && _auditGrid.Rows.Count == 0)
                        LoadAuditAndAlerts();
                }
            };

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(filterCard, 0, 1);
            root.Controls.Add(summaryGrid, 0, 2);
            root.Controls.Add(kpiGrid, 0, 3);
            root.Controls.Add(_statsTabControl, 0, 4);

            SetBookingStatsGroupMode(BookingStatsGroupMode.Day, rebuild: false);
            return root;
        }

        private Control BuildExplorerTab()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0, 8, 0, 0),
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var filterCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 8),
                Margin = new Padding(0, 0, 0, 8)
            };
            filterCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 230, 242)))
                    e.Graphics.DrawRectangle(pen, 0, 0, filterCard.Width - 1, filterCard.Height - 1);
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };
            flow.Controls.Add(new Label
            {
                Text = "Từ khóa",
                AutoSize = true,
                Margin = new Padding(0, 9, 6, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });
            _explorerKeywordTextBox = new TextBox
            {
                Width = 220,
                Margin = new Padding(0, 6, 10, 0)
            };
            flow.Controls.Add(_explorerKeywordTextBox);

            flow.Controls.Add(new Label { Text = "Trạng thái", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _explorerStatusCombo = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 6, 10, 0) };
            _explorerStatusCombo.Items.AddRange(new object[] { "Tất cả", "Đặt trước", "Đang ở", "Đã trả", "Đã hủy", "No-show" });
            _explorerStatusCombo.SelectedIndex = 0;
            flow.Controls.Add(_explorerStatusCombo);

            flow.Controls.Add(new Label { Text = "Loại đặt", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _explorerBookingTypeCombo = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 6, 10, 0) };
            _explorerBookingTypeCombo.Items.AddRange(new object[] { "Tất cả", "Phòng giờ", "Phòng đêm" });
            _explorerBookingTypeCombo.SelectedIndex = 0;
            flow.Controls.Add(_explorerBookingTypeCombo);

            flow.Controls.Add(new Label { Text = "Thanh toán", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _explorerPaymentCombo = new ComboBox { Width = 146, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 6, 10, 0) };
            _explorerPaymentCombo.Items.AddRange(new object[] { "Tất cả", "Đã thanh toán đủ", "Chưa/thiếu thanh toán" });
            _explorerPaymentCombo.SelectedIndex = 0;
            flow.Controls.Add(_explorerPaymentCombo);

            flow.Controls.Add(new Label { Text = "Loại phòng", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _explorerRoomTypeCombo = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 6, 10, 0) };
            _explorerRoomTypeCombo.Items.AddRange(new object[] { "Tất cả", "Phòng đơn", "Phòng đôi" });
            _explorerRoomTypeCombo.SelectedIndex = 0;
            flow.Controls.Add(_explorerRoomTypeCombo);

            flow.Controls.Add(new Label { Text = "Kênh đặt", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _explorerChannelCombo = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 6, 10, 0) };
            _explorerChannelCombo.Items.AddRange(new object[] { "Tất cả", "TrucTiep" });
            _explorerChannelCombo.SelectedIndex = 0;
            flow.Controls.Add(_explorerChannelCombo);

            flow.Controls.Add(new Label { Text = "Sắp xếp", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _explorerSortCombo = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 6, 10, 0) };
            _explorerSortCombo.Items.AddRange(new object[] { "Check-in mới nhất", "Check-in cũ nhất", "Doanh thu giảm dần", "Doanh thu tăng dần", "Cập nhật gần nhất", "Mã phòng A-Z" });
            _explorerSortCombo.SelectedIndex = 0;
            flow.Controls.Add(_explorerSortCombo);

            var btnApply = new Button
            {
                Text = "Áp dụng",
                Width = 90,
                Height = 30,
                Margin = new Padding(0, 5, 8, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(33, 106, 186)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) =>
            {
                _explorerCurrentPage = 1;
                LoadExplorerData(force: true);
            };
            flow.Controls.Add(btnApply);

            var btnReset = new Button
            {
                Text = "Đặt lại",
                Width = 90,
                Height = 30,
                Margin = new Padding(0, 5, 8, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(49, 73, 112),
                BackColor = Color.White
            };
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(198, 214, 238);
            btnReset.Click += (s, e) => ResetExplorerFilters();
            flow.Controls.Add(btnReset);

            var btnSaveFilter = new Button
            {
                Text = "Lưu bộ lọc",
                Width = 96,
                Height = 30,
                Margin = new Padding(0, 5, 8, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(49, 73, 112),
                BackColor = Color.White
            };
            btnSaveFilter.FlatAppearance.BorderColor = Color.FromArgb(198, 214, 238);
            btnSaveFilter.Click += (s, e) => SaveExplorerFilterSnapshot();
            flow.Controls.Add(btnSaveFilter);

            _explorerFilterSnapshotLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(2, 10, 0, 0),
                ForeColor = Color.FromArgb(103, 114, 132),
                Font = new Font("Segoe UI", 8.8F),
                Text = string.Empty
            };
            flow.Controls.Add(_explorerFilterSnapshotLabel);
            filterCard.Controls.Add(flow);

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));

            _explorerGrid = CreateStatsGrid();
            _explorerStayGrid = CreateStatsGrid();
            _explorerExtrasGrid = CreateStatsGrid();
            _explorerTimelineGrid = CreateStatsGrid();
            _explorerGrid.SelectionChanged += ExplorerGrid_SelectionChanged;
            _explorerGrid.CellClick += ExplorerGrid_CellClick;

            var masterCard = CreateGridCard("Dữ liệu tổng hợp", _explorerGrid);
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(8, 0, 0, 0)
            };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 46f));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 54f));

            _explorerDetailTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            _explorerStayTab = new TabPage("Lưu trú");
            _explorerExtrasTab = new TabPage("Phát sinh");
            _explorerStayGrid.Dock = DockStyle.Fill;
            _explorerExtrasGrid.Dock = DockStyle.Fill;
            _explorerStayTab.Controls.Add(_explorerStayGrid);
            _explorerExtrasTab.Controls.Add(_explorerExtrasGrid);
            _explorerDetailTabControl.TabPages.Add(_explorerStayTab);
            _explorerDetailTabControl.TabPages.Add(_explorerExtrasTab);
            right.Controls.Add(CreateGridCard("Chi tiết booking", _explorerDetailTabControl), 0, 0);
            right.Controls.Add(CreateGridCard("Timeline thay đổi", _explorerTimelineGrid), 0, 1);

            content.Controls.Add(masterCard, 0, 0);
            content.Controls.Add(right, 1, 0);

            var pager = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 8, 0, 0)
            };
            pager.Controls.Add(new Label
            {
                Text = "Số dòng/trang",
                AutoSize = true,
                Margin = new Padding(0, 9, 6, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });
            _explorerPageSizeCombo = new ComboBox
            {
                Width = 70,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 10, 0)
            };
            _explorerPageSizeCombo.Items.AddRange(new object[] { "20", "50", "100" });
            _explorerPageSizeCombo.SelectedIndex = 0;
            _explorerPageSizeCombo.SelectedIndexChanged += (s, e) =>
            {
                _explorerCurrentPage = 1;
                LoadExplorerData();
            };
            pager.Controls.Add(_explorerPageSizeCombo);

            var btnPrev = new Button
            {
                Text = "Trang trước",
                Width = 95,
                Height = 28,
                Margin = new Padding(0, 5, 6, 0),
                FlatStyle = FlatStyle.Flat
            };
            btnPrev.Click += (s, e) =>
            {
                if (_explorerCurrentPage <= 1) return;
                _explorerCurrentPage--;
                LoadExplorerData();
            };
            pager.Controls.Add(btnPrev);

            var btnNext = new Button
            {
                Text = "Trang sau",
                Width = 85,
                Height = 28,
                Margin = new Padding(0, 5, 8, 0),
                FlatStyle = FlatStyle.Flat
            };
            btnNext.Click += (s, e) =>
            {
                if (_explorerCurrentPage >= _explorerTotalPages) return;
                _explorerCurrentPage++;
                LoadExplorerData();
            };
            pager.Controls.Add(btnNext);

            _explorerPageLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 9, 0, 0),
                ForeColor = Color.FromArgb(103, 114, 132),
                Font = new Font("Segoe UI", 9F),
                Text = "Trang 1/1"
            };
            pager.Controls.Add(_explorerPageLabel);

            root.Controls.Add(filterCard, 0, 0);
            root.Controls.Add(content, 0, 1);
            root.Controls.Add(pager, 0, 2);
            return root;
        }

        private Control BuildAuditTab()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0, 8, 0, 0),
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var filterCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 8),
                Margin = new Padding(0, 0, 0, 8)
            };
            filterCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 230, 242)))
                    e.Graphics.DrawRectangle(pen, 0, 0, filterCard.Width - 1, filterCard.Height - 1);
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            flow.Controls.Add(new Label { Text = "Entity", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _auditEntityCombo = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 10, 0) };
            _auditEntityCombo.Items.AddRange(new object[] { "ALL", "DATPHONG", "HOADON", "PHONG", "KHACHHANG" });
            _auditEntityCombo.SelectedIndex = 0;
            flow.Controls.Add(_auditEntityCombo);

            flow.Controls.Add(new Label { Text = "Actor", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _auditActorTextBox = new TextBox { Width = 160, Margin = new Padding(0, 6, 10, 0) };
            flow.Controls.Add(_auditActorTextBox);

            flow.Controls.Add(new Label { Text = "Từ khóa", AutoSize = true, Margin = new Padding(0, 9, 6, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            _auditKeywordTextBox = new TextBox { Width = 240, Margin = new Padding(0, 6, 10, 0) };
            flow.Controls.Add(_auditKeywordTextBox);

            var btnLoad = new Button
            {
                Text = "Tải audit",
                Width = 90,
                Height = 30,
                Margin = new Padding(0, 5, 8, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(33, 106, 186)
            };
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Click += (s, e) =>
            {
                _auditCurrentPage = 1;
                LoadAuditAndAlerts(force: true);
            };
            flow.Controls.Add(btnLoad);

            var btnReloadAlerts = new Button
            {
                Text = "Tải cảnh báo",
                Width = 96,
                Height = 30,
                Margin = new Padding(0, 5, 0, 0),
                FlatStyle = FlatStyle.Flat
            };
            btnReloadAlerts.Click += (s, e) => LoadAuditAndAlerts(force: true);
            flow.Controls.Add(btnReloadAlerts);

            filterCard.Controls.Add(flow);

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));

            _auditGrid = CreateStatsGrid();
            _alertGrid = CreateStatsGrid();

            content.Controls.Add(CreateGridCard("Audit log", _auditGrid), 0, 0);
            var alertCard = CreateGridCard("Cảnh báo bất thường dữ liệu", _alertGrid);
            alertCard.Margin = new Padding(8, 0, 0, 0);
            content.Controls.Add(alertCard, 1, 0);

            var pager = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 8, 0, 0)
            };
            var btnPrev = new Button
            {
                Text = "Trang trước",
                Width = 95,
                Height = 28,
                Margin = new Padding(0, 5, 6, 0),
                FlatStyle = FlatStyle.Flat
            };
            btnPrev.Click += (s, e) =>
            {
                if (_auditCurrentPage <= 1) return;
                _auditCurrentPage--;
                LoadAuditAndAlerts();
            };
            pager.Controls.Add(btnPrev);

            var btnNext = new Button
            {
                Text = "Trang sau",
                Width = 85,
                Height = 28,
                Margin = new Padding(0, 5, 8, 0),
                FlatStyle = FlatStyle.Flat
            };
            btnNext.Click += (s, e) =>
            {
                if (_auditCurrentPage >= _auditTotalPages) return;
                _auditCurrentPage++;
                LoadAuditAndAlerts();
            };
            pager.Controls.Add(btnNext);

            _auditPageLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 9, 0, 0),
                ForeColor = Color.FromArgb(103, 114, 132),
                Font = new Font("Segoe UI", 9F),
                Text = "Trang 1/1"
            };
            pager.Controls.Add(_auditPageLabel);

            root.Controls.Add(filterCard, 0, 0);
            root.Controls.Add(content, 0, 1);
            root.Controls.Add(pager, 0, 2);
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

        private Panel CreateGridCard(string title, Control content)
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

            content.Dock = DockStyle.Fill;
            content.Margin = new Padding(0, 6, 0, 0);

            card.Controls.Add(content);
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(228, 236, 247),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
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
                },
                RowTemplate = { Height = 26 }
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
            public int DatPhongID { get; set; }
            public int BookingType { get; set; }
            public string LoaiDat { get; set; }
            public string SoPhong { get; set; }
            public string ThoiGianNhan { get; set; }
            public string ThoiGianTra { get; set; }
            public string TongGioPhut { get; set; }
            public int NuocSuoi { get; set; }
            public int NuocNgot { get; set; }
            public string TongTien { get; set; }
            public string XemNguoiO { get; set; }
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

        private sealed class KpiRevenueTrendViewRow
        {
            public string Ngay { get; set; }
            public string DoanhThu { get; set; }
        }

        private sealed class KpiDistributionViewRow
        {
            public string Nhom { get; set; }
            public int SoLuong { get; set; }
            public string DoanhThu { get; set; }
        }

        private sealed class KpiHourViewRow
        {
            public string Gio { get; set; }
            public int Luot { get; set; }
        }

        private sealed class ExplorerGridViewRow
        {
            public int DatPhongID { get; set; }
            public string MaPhong { get; set; }
            public string LoaiPhong { get; set; }
            public string LoaiDat { get; set; }
            public string KhachHang { get; set; }
            public string CCCD { get; set; }
            public string CheckIn { get; set; }
            public string CheckOutDuKien { get; set; }
            public string CheckOutThucTe { get; set; }
            public string TrangThai { get; set; }
            public string KenhDat { get; set; }
            public int SoHoaDon { get; set; }
            public string TongHoaDon { get; set; }
            public string PhatSinh { get; set; }
            public string ThanhToan { get; set; }
            public string CreatedBy { get; set; }
            public string UpdatedBy { get; set; }
        }

        private sealed class ExplorerStayViewRow
        {
            public string Truong { get; set; }
            public string GiaTri { get; set; }
        }

        private sealed class ExplorerExtraViewRow
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public string UnitPrice { get; set; }
            public string Amount { get; set; }
            public string Note { get; set; }
        }

        private sealed class ExplorerTimelineViewRow
        {
            public string ThoiGianUtc { get; set; }
            public string Entity { get; set; }
            public string HanhDong { get; set; }
            public string NguoiThucHien { get; set; }
            public string TruocKhiSua { get; set; }
            public string SauKhiSua { get; set; }
            public string Nguon { get; set; }
        }

        private sealed class AuditGridViewRow
        {
            public int AuditLogID { get; set; }
            public string ThoiGianUtc { get; set; }
            public string Entity { get; set; }
            public string EntityId { get; set; }
            public string HanhDong { get; set; }
            public string NguoiThucHien { get; set; }
            public string Nguon { get; set; }
            public string TruocKhiSua { get; set; }
            public string SauKhiSua { get; set; }
        }

        private sealed class AlertGridViewRow
        {
            public string MucDo { get; set; }
            public string MaCanhBao { get; set; }
            public string NoiDung { get; set; }
            public string ThamChieu { get; set; }
            public string ThoiGian { get; set; }
        }

        private void LoadBookingStatisticsData()
        {
            RequestLoadBookingStatisticsData(force: true);
        }

        private int? GetSelectedStatsBookingType()
        {
            if (_statsBookingTypeCombo == null) return null;
            if (_statsBookingTypeCombo.SelectedIndex == 1) return 1;
            if (_statsBookingTypeCombo.SelectedIndex == 2) return 2;
            return null;
        }

        private async void RequestLoadBookingStatisticsData(bool force, string knownFingerprint = null)
        {
            if (_isBookingStatsLoading) return;
            if (_statsFromPicker == null || _statsToPicker == null) return;

            DateTime fromDate = _statsFromPicker.Value.Date;
            DateTime toDate = _statsToPicker.Value.Date;
            int? bookingType = GetSelectedStatsBookingType();

            if (fromDate > toDate)
            {
                MessageBox.Show("Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.", "Khoảng thời gian không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _statsFromPicker.Focus();
                return;
            }

            _isBookingStatsLoading = true;
            try
            {
                using (var perf = PerformanceTracker.Measure("MainForm.LoadBookingStats", new Dictionary<string, object>
                {
                    ["FromDate"] = fromDate.ToString("yyyy-MM-dd"),
                    ["ToDate"] = toDate.ToString("yyyy-MM-dd"),
                    ["BookingType"] = bookingType.HasValue ? bookingType.Value : -1,
                    ["ForceReload"] = force
                }))
                {
                    string latestFingerprint = knownFingerprint;
                    if (string.IsNullOrWhiteSpace(latestFingerprint))
                        latestFingerprint = await Task.Run(() => _bookingDal.GetBookingStatisticsFingerprint(fromDate, toDate, bookingType));
                    if (!force &&
                        !string.IsNullOrEmpty(_lastBookingStatsFingerprint) &&
                        string.Equals(latestFingerprint, _lastBookingStatsFingerprint, StringComparison.Ordinal))
                    {
                        perf.AddContext("SkippedByFingerprint", true);
                        return;
                    }

                    var data = await Task.Run(() => _bookingDal.GetBookingStatistics(fromDate, toDate, bookingType));
                    if (_statsFromPicker == null || _statsToPicker == null) return;
                    if (_statsFromPicker.Value.Date != fromDate || _statsToPicker.Value.Date != toDate) return;
                    if (GetSelectedStatsBookingType() != bookingType) return;

                    BindBookingStatisticsData(data, fromDate, toDate);
                    _lastBookingStatsFingerprint = latestFingerprint;
                    perf.AddContext("BookingRows", data?.Bookings?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.LoadBookingStats", "Không thể tải dữ liệu thống kê. Vui lòng thử lại.", ex, "Lỗi thống kê");
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
            LoadKpiDashboardData();
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
                    DatPhongID = x.DatPhongID,
                    BookingType = x.BookingType,
                    LoaiDat = x.IsHourly ? "Phòng giờ" : "Phòng đêm",
                    SoPhong = x.MaPhong,
                    ThoiGianNhan = x.CheckInTime.ToString("dd/MM/yyyy HH:mm"),
                    ThoiGianTra = x.CheckOutTime.HasValue ? x.CheckOutTime.Value.ToString("dd/MM/yyyy HH:mm") : string.Empty,
                    TongGioPhut = FormatDurationShort(x.TotalDuration),
                    NuocSuoi = x.WaterBottleCount,
                    NuocNgot = x.SoftDrinkCount,
                    TongTien = x.TotalAmount.ToString("N0") + " đ",
                    XemNguoiO = x.BookingType == 2 ? "Xem chi tiết" : string.Empty
                })
                .ToList();

            _statsRoomGrid.DataSource = details;
            ApplyBookingDetailGridHeaders();
        }

        private async void StatsRoomGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _statsRoomGrid == null) return;
            var column = _statsRoomGrid.Columns[e.ColumnIndex];
            if (column == null || !string.Equals(column.Name, "XemNguoiO", StringComparison.Ordinal)) return;

            string action = Convert.ToString(_statsRoomGrid.Rows[e.RowIndex].Cells["XemNguoiO"]?.Value);
            if (string.IsNullOrWhiteSpace(action)) return;

            int bookingType;
            if (!int.TryParse(Convert.ToString(_statsRoomGrid.Rows[e.RowIndex].Cells["BookingType"]?.Value), out bookingType))
                bookingType = 0;
            if (bookingType != 2)
            {
                MessageBox.Show("Thông tin người ở chỉ áp dụng cho phòng đêm.", "Không khả dụng", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int bookingId;
            if (!int.TryParse(Convert.ToString(_statsRoomGrid.Rows[e.RowIndex].Cells["DatPhongID"]?.Value), out bookingId) || bookingId <= 0)
            {
                MessageBox.Show("Không xác định được mã đặt phòng để mở chi tiết.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await ShowOvernightGuestDetailAsync(bookingId);
        }

        private async Task ShowOvernightGuestDetailAsync(int bookingId)
        {
            StatisticsDAL.ExplorerDocumentData doc;
            try
            {
                UseWaitCursor = true;
                using (var perf = PerformanceTracker.Measure("MainForm.ShowOvernightGuestDetail", new Dictionary<string, object>
                {
                    ["BookingId"] = bookingId
                }))
                {
                    doc = await Task.Run(() => _statisticsDal.GetBookingDocumentData(bookingId, 100));
                    perf.AddContext("StayInfoRows", doc?.StayInfo?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.LoadExplorerDetail", "Không thể tải thông tin người ở. Vui lòng thử lại.", ex, "Lỗi dữ liệu");
                return;
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (doc == null || doc.Booking == null)
            {
                MessageBox.Show("Không tìm thấy dữ liệu cho mã đặt phòng #" + bookingId + ".", "Không có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string content = BuildOvernightGuestDetailText(doc);
            ShowTextDetailDialog("Thông tin người ở - Phòng " + (string.IsNullOrWhiteSpace(doc.Booking.MaPhong) ? "#" + doc.Booking.PhongID : doc.Booking.MaPhong), content);
        }

        private static string BuildOvernightGuestDetailText(StatisticsDAL.ExplorerDocumentData doc)
        {
            var booking = doc?.Booking;
            var stayInfo = doc?.StayInfo ?? new List<StatisticsDAL.ExplorerStayLine>();
            var sb = new StringBuilder(2048);

            if (booking != null)
            {
                sb.AppendLine("Mã đặt phòng: " + booking.DatPhongID);
                sb.AppendLine("Mã phòng: " + (string.IsNullOrWhiteSpace(booking.MaPhong) ? "#" + booking.PhongID : booking.MaPhong));
                sb.AppendLine("Loại đặt: " + booking.BookingTypeText);
                sb.AppendLine("Trạng thái: " + booking.TrangThaiText);
                sb.AppendLine("Check-in: " + booking.NgayDen.ToString("dd/MM/yyyy HH:mm"));
                sb.AppendLine("Check-out dự kiến: " + booking.NgayDiDuKien.ToString("dd/MM/yyyy HH:mm"));
                sb.AppendLine("Check-out thực tế: " + (booking.NgayDiThucTe.HasValue ? booking.NgayDiThucTe.Value.ToString("dd/MM/yyyy HH:mm") : "-"));
                sb.AppendLine("Khách chính: " + (string.IsNullOrWhiteSpace(booking.KhachHang) ? "(Không rõ)" : booking.KhachHang));
                sb.AppendLine("CCCD/CMND: " + (string.IsNullOrWhiteSpace(booking.CCCD) ? "-" : booking.CCCD));
                sb.AppendLine("Điện thoại: " + (string.IsNullOrWhiteSpace(booking.DienThoai) ? "-" : booking.DienThoai));
                sb.AppendLine();
            }

            sb.AppendLine("Thông tin lưu trú:");
            if (stayInfo.Count == 0)
            {
                sb.AppendLine("- Chưa có dữ liệu STAY_INFO cho booking này.");
            }
            else
            {
                foreach (var line in stayInfo)
                {
                    if (line == null) continue;
                    string field = string.IsNullOrWhiteSpace(line.Field) ? "Trường" : line.Field.Trim();
                    string value = string.IsNullOrWhiteSpace(line.Value) ? "-" : line.Value.Trim();
                    sb.AppendLine("- " + field + ": " + value);
                }
            }

            return sb.ToString().Trim();
        }

        private void ShowTextDetailDialog(string title, string content)
        {
            using (var dialog = new Form())
            {
                dialog.Text = title;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Size = new Size(780, 620);
                dialog.MinimumSize = new Size(620, 460);
                dialog.BackColor = Color.White;

                var root = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    Padding = new Padding(12)
                };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                var txtContent = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    WordWrap = true,
                    Font = new Font("Segoe UI", 9.5F),
                    Text = content ?? string.Empty
                };

                var btnClose = new Button
                {
                    Text = "Đóng",
                    DialogResult = DialogResult.OK,
                    AutoSize = true,
                    Anchor = AnchorStyles.Right,
                    Margin = new Padding(0, 10, 0, 0),
                    Padding = new Padding(14, 5, 14, 5),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };

                root.Controls.Add(txtContent, 0, 0);
                root.Controls.Add(btnClose, 0, 1);

                dialog.Controls.Add(root);
                dialog.AcceptButton = btnClose;
                dialog.CancelButton = btnClose;
                dialog.ShowDialog(this);
            }
        }

        private async void LoadKpiDashboardData()
        {
            if (_statsFromPicker == null || _statsToPicker == null) return;

            DateTime from = _statsFromPicker.Value.Date;
            DateTime to = _statsToPicker.Value.Date;
            int? bookingType = GetSelectedStatsBookingType();
            int loadVersion = ++_kpiLoadVersion;

            StatisticsDAL.KpiDashboardData data;
            try
            {
                using (var perf = PerformanceTracker.Measure("MainForm.LoadKpiDashboard", new Dictionary<string, object>
                {
                    ["FromDate"] = from.ToString("yyyy-MM-dd"),
                    ["ToDate"] = to.ToString("yyyy-MM-dd"),
                    ["BookingType"] = bookingType.HasValue ? bookingType.Value : -1
                }))
                {
                    data = await Task.Run(() => _statisticsDal.GetKpiDashboard(from, to, bookingType));
                    perf.AddContext("RevenueTrendRows", data?.RevenueByDay?.Count ?? 0);
                    perf.AddContext("TopChannelRows", data?.TopChannels?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.LoadKpiDashboard", "Không thể tải KPI dashboard. Vui lòng thử lại.", ex, "Lỗi KPI");
                return;
            }

            if (loadVersion != _kpiLoadVersion) return;
            if (_statsFromPicker == null || _statsToPicker == null) return;
            if (_statsFromPicker.Value.Date != from || _statsToPicker.Value.Date != to) return;
            if (GetSelectedStatsBookingType() != bookingType) return;

            if (_kpiTotalBookingsValue != null) _kpiTotalBookingsValue.Text = data.TotalBookings.ToString("N0");
            if (_kpiTotalRevenueValue != null) _kpiTotalRevenueValue.Text = data.TotalRevenue.ToString("N0") + " đ";
            if (_kpiHourlyBookingsValue != null) _kpiHourlyBookingsValue.Text = data.HourlyBookings.ToString("N0");
            if (_kpiOvernightBookingsValue != null) _kpiOvernightBookingsValue.Text = data.OvernightBookings.ToString("N0");
            if (_kpiExtrasRevenueValue != null) _kpiExtrasRevenueValue.Text = data.ExtrasRevenue.ToString("N0") + " đ";
            if (_kpiCancelCountValue != null)
            {
                _kpiCancelCountValue.Text = data.CancelCount.ToString("N0");
                _kpiCancelCountValue.Parent?.Invalidate();
                _roomToolTip.SetToolTip(_kpiCancelCountValue, "Hủy: " + data.CancelCount.ToString("N0") + " | No-show: " + data.NoShowCount.ToString("N0"));
            }

            if (_kpiRevenueTrendGrid != null)
            {
                _kpiRevenueTrendGrid.DataSource = (data.RevenueByDay ?? new List<StatisticsDAL.RevenuePoint>())
                    .Select(x => new KpiRevenueTrendViewRow
                    {
                        Ngay = x.Date.ToString("dd/MM/yyyy"),
                        DoanhThu = x.Revenue.ToString("N0") + " đ"
                    })
                    .ToList();
                if (_kpiRevenueTrendGrid.Columns["Ngay"] != null) _kpiRevenueTrendGrid.Columns["Ngay"].HeaderText = "Ngày";
                if (_kpiRevenueTrendGrid.Columns["DoanhThu"] != null)
                {
                    _kpiRevenueTrendGrid.Columns["DoanhThu"].HeaderText = "Doanh thu";
                    _kpiRevenueTrendGrid.Columns["DoanhThu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            if (_kpiChannelGrid != null)
            {
                _kpiChannelGrid.DataSource = (data.TopChannels ?? new List<StatisticsDAL.DistributionPoint>())
                    .Select(x => new KpiDistributionViewRow
                    {
                        Nhom = x.Name,
                        SoLuong = x.Count,
                        DoanhThu = x.Revenue.ToString("N0") + " đ"
                    })
                    .ToList();
                ApplyKpiDistributionGridHeaders(_kpiChannelGrid, "Kênh");
            }

            if (_kpiRoomTypeGrid != null)
            {
                _kpiRoomTypeGrid.DataSource = (data.TopRoomTypes ?? new List<StatisticsDAL.DistributionPoint>())
                    .Select(x => new KpiDistributionViewRow
                    {
                        Nhom = x.Name,
                        SoLuong = x.Count,
                        DoanhThu = x.Revenue.ToString("N0") + " đ"
                    })
                    .ToList();
                ApplyKpiDistributionGridHeaders(_kpiRoomTypeGrid, "Loại phòng");
            }

            if (_kpiCheckInHourGrid != null)
            {
                _kpiCheckInHourGrid.DataSource = (data.CheckInByHour ?? new List<StatisticsDAL.HourDistributionPoint>())
                    .Select(x => new KpiHourViewRow
                    {
                        Gio = x.Hour.ToString("00") + ":00",
                        Luot = x.Count
                    })
                    .ToList();
                ApplyKpiHourGridHeaders(_kpiCheckInHourGrid);
            }

            if (_kpiCheckOutHourGrid != null)
            {
                _kpiCheckOutHourGrid.DataSource = (data.CheckOutByHour ?? new List<StatisticsDAL.HourDistributionPoint>())
                    .Select(x => new KpiHourViewRow
                    {
                        Gio = x.Hour.ToString("00") + ":00",
                        Luot = x.Count
                    })
                    .ToList();
                ApplyKpiHourGridHeaders(_kpiCheckOutHourGrid);
            }
        }

        private void ApplyKpiDistributionGridHeaders(DataGridView grid, string groupHeader)
        {
            if (grid == null || grid.Columns.Count == 0) return;
            if (grid.Columns["Nhom"] != null) grid.Columns["Nhom"].HeaderText = groupHeader;
            if (grid.Columns["SoLuong"] != null) grid.Columns["SoLuong"].HeaderText = "Số lượng";
            if (grid.Columns["DoanhThu"] != null)
            {
                grid.Columns["DoanhThu"].HeaderText = "Doanh thu";
                grid.Columns["DoanhThu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void ApplyKpiHourGridHeaders(DataGridView grid)
        {
            if (grid == null || grid.Columns.Count == 0) return;
            if (grid.Columns["Gio"] != null) grid.Columns["Gio"].HeaderText = "Giờ";
            if (grid.Columns["Luot"] != null) grid.Columns["Luot"].HeaderText = "Lượt";
        }

        private void ResetExplorerFilters()
        {
            if (_explorerKeywordTextBox != null) _explorerKeywordTextBox.Text = string.Empty;
            if (_explorerStatusCombo != null) _explorerStatusCombo.SelectedIndex = 0;
            if (_explorerBookingTypeCombo != null) _explorerBookingTypeCombo.SelectedIndex = 0;
            if (_explorerPaymentCombo != null) _explorerPaymentCombo.SelectedIndex = 0;
            if (_explorerRoomTypeCombo != null) _explorerRoomTypeCombo.SelectedIndex = 0;
            if (_explorerChannelCombo != null) _explorerChannelCombo.SelectedIndex = 0;
            if (_explorerSortCombo != null) _explorerSortCombo.SelectedIndex = 0;
            if (_explorerPageSizeCombo != null) _explorerPageSizeCombo.SelectedIndex = 0;
            _explorerCurrentPage = 1;
            _explorerSavedFilterSnapshot = string.Empty;
            if (_explorerFilterSnapshotLabel != null) _explorerFilterSnapshotLabel.Text = string.Empty;
            LoadExplorerData();
        }

        private void SaveExplorerFilterSnapshot()
        {
            var builder = new StringBuilder();
            builder.Append("Từ khóa=").Append(_explorerKeywordTextBox?.Text?.Trim() ?? string.Empty).Append("; ");
            builder.Append("Trạng thái=").Append(_explorerStatusCombo?.SelectedItem?.ToString() ?? "Tất cả").Append("; ");
            builder.Append("Loại đặt=").Append(_explorerBookingTypeCombo?.SelectedItem?.ToString() ?? "Tất cả").Append("; ");
            builder.Append("Thanh toán=").Append(_explorerPaymentCombo?.SelectedItem?.ToString() ?? "Tất cả").Append("; ");
            builder.Append("Loại phòng=").Append(_explorerRoomTypeCombo?.SelectedItem?.ToString() ?? "Tất cả").Append("; ");
            builder.Append("Kênh=").Append(_explorerChannelCombo?.SelectedItem?.ToString() ?? "Tất cả").Append("; ");
            builder.Append("Sắp xếp=").Append(_explorerSortCombo?.SelectedItem?.ToString() ?? "Check-in mới nhất");
            _explorerSavedFilterSnapshot = builder.ToString();
            if (_explorerFilterSnapshotLabel != null)
                _explorerFilterSnapshotLabel.Text = "Bộ lọc đã lưu: " + _explorerSavedFilterSnapshot;
        }

        private static string BuildExplorerLoadSignature(
            DateTime fromDate,
            DateTime toDate,
            int currentPage,
            int pageSize,
            string keyword,
            int? status,
            int? bookingType,
            bool? isPaid,
            int? roomType,
            string channel,
            string sortBy)
        {
            return string.Join("|", new[]
            {
                fromDate.ToString("yyyyMMdd"),
                toDate.ToString("yyyyMMdd"),
                currentPage.ToString(),
                pageSize.ToString(),
                (keyword ?? string.Empty).Trim(),
                status.HasValue ? status.Value.ToString() : "-",
                bookingType.HasValue ? bookingType.Value.ToString() : "-",
                isPaid.HasValue ? (isPaid.Value ? "1" : "0") : "-",
                roomType.HasValue ? roomType.Value.ToString() : "-",
                (channel ?? string.Empty).Trim(),
                sortBy ?? string.Empty
            });
        }

        private static string BuildAuditLoadSignature(
            DateTime fromDate,
            DateTime toDate,
            int currentPage,
            string entity,
            string actor,
            string keyword)
        {
            return string.Join("|", new[]
            {
                fromDate.ToString("yyyyMMdd"),
                toDate.ToString("yyyyMMdd"),
                currentPage.ToString(),
                (entity ?? string.Empty).Trim(),
                (actor ?? string.Empty).Trim(),
                (keyword ?? string.Empty).Trim()
            });
        }

        private static bool IsRecentLoad(DateTime loadedUtc)
        {
            if (loadedUtc == DateTime.MinValue) return false;
            return (DateTime.UtcNow - loadedUtc).TotalSeconds < VIEW_RELOAD_COOLDOWN_SECONDS;
        }

        private async void LoadExplorerData(bool force = false)
        {
            if (_statsFromPicker == null || _statsToPicker == null || _explorerGrid == null) return;
            if (_statsFromPicker.Value.Date > _statsToPicker.Value.Date) return;

            int pageSize = 20;
            int.TryParse(Convert.ToString(_explorerPageSizeCombo?.SelectedItem), out pageSize);
            if (pageSize <= 0) pageSize = 20;

            int? status = null;
            if (_explorerStatusCombo != null && _explorerStatusCombo.SelectedIndex > 0)
                status = _explorerStatusCombo.SelectedIndex - 1;

            int? bookingType = null;
            if (_explorerBookingTypeCombo != null)
            {
                if (_explorerBookingTypeCombo.SelectedIndex == 1) bookingType = 1;
                else if (_explorerBookingTypeCombo.SelectedIndex == 2) bookingType = 2;
            }

            bool? isPaid = null;
            if (_explorerPaymentCombo != null)
            {
                if (_explorerPaymentCombo.SelectedIndex == 1) isPaid = true;
                else if (_explorerPaymentCombo.SelectedIndex == 2) isPaid = false;
            }

            int? roomType = null;
            if (_explorerRoomTypeCombo != null)
            {
                if (_explorerRoomTypeCombo.SelectedIndex == 1) roomType = 1;
                else if (_explorerRoomTypeCombo.SelectedIndex == 2) roomType = 2;
            }

            string channel = null;
            if (_explorerChannelCombo != null && _explorerChannelCombo.SelectedIndex > 0)
                channel = Convert.ToString(_explorerChannelCombo.SelectedItem);

            string sortBy = "checkin_desc";
            if (_explorerSortCombo != null)
            {
                if (_explorerSortCombo.SelectedIndex == 1) sortBy = "checkin_asc";
                else if (_explorerSortCombo.SelectedIndex == 2) sortBy = "revenue_desc";
                else if (_explorerSortCombo.SelectedIndex == 3) sortBy = "revenue_asc";
                else if (_explorerSortCombo.SelectedIndex == 4) sortBy = "updated_desc";
                else if (_explorerSortCombo.SelectedIndex == 5) sortBy = "room_asc";
            }

            DateTime fromDate = _statsFromPicker.Value.Date;
            DateTime toDate = _statsToPicker.Value.Date;
            int currentPage = _explorerCurrentPage;
            string keyword = _explorerKeywordTextBox?.Text ?? string.Empty;
            string signature = BuildExplorerLoadSignature(
                fromDate, toDate, currentPage, pageSize, keyword, status, bookingType, isPaid, roomType, channel, sortBy);
            if (!force
                && string.Equals(signature, _lastExplorerLoadSignature, StringComparison.Ordinal)
                && IsRecentLoad(_lastExplorerLoadedUtc))
            {
                return;
            }

            int loadVersion = ++_explorerLoadVersion;

            StatisticsDAL.ExplorerResult data;
            try
            {
                using (var perf = PerformanceTracker.Measure("MainForm.LoadExplorerData", new Dictionary<string, object>
                {
                    ["FromDate"] = fromDate.ToString("yyyy-MM-dd"),
                    ["ToDate"] = toDate.ToString("yyyy-MM-dd"),
                    ["Page"] = currentPage,
                    ["PageSize"] = pageSize,
                    ["SortBy"] = sortBy
                }))
                {
                    data = await Task.Run(() => _statisticsDal.GetExplorerRows(new StatisticsDAL.ExplorerQuery
                    {
                        FromDate = fromDate,
                        ToDate = toDate,
                        Keyword = keyword,
                        BookingStatus = status,
                        BookingType = bookingType,
                        IsFullyPaid = isPaid,
                        RoomTypeId = roomType,
                        Channel = channel,
                        SortBy = sortBy,
                        Page = currentPage,
                        PageSize = pageSize
                    }));
                    perf.AddContext("DbRows", data?.Rows?.Count ?? 0);
                    perf.AddContext("TotalRows", data?.TotalCount ?? 0);
                }
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.LoadDataExplorer", "Không thể tải Data Explorer. Vui lòng thử lại.", ex, "Lỗi Data Explorer");
                return;
            }

            if (loadVersion != _explorerLoadVersion) return;
            if (_statsFromPicker == null || _statsToPicker == null) return;
            if (_statsFromPicker.Value.Date != fromDate || _statsToPicker.Value.Date != toDate) return;

            int total = data?.TotalCount ?? 0;
            _explorerTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            if (_explorerCurrentPage > _explorerTotalPages)
            {
                _explorerCurrentPage = _explorerTotalPages;
                LoadExplorerData();
                return;
            }

            var rows = (data?.Rows ?? new List<StatisticsDAL.ExplorerRow>())
                .Select(x => new ExplorerGridViewRow
                {
                    DatPhongID = x.DatPhongID,
                    MaPhong = x.MaPhong,
                    LoaiPhong = x.LoaiPhong,
                    LoaiDat = x.BookingTypeText,
                    KhachHang = string.IsNullOrWhiteSpace(x.KhachHang) ? "(Không rõ)" : x.KhachHang,
                    CCCD = x.CCCD,
                    CheckIn = x.NgayDen.ToString("dd/MM/yyyy HH:mm"),
                    CheckOutDuKien = x.NgayDiDuKien.ToString("dd/MM/yyyy HH:mm"),
                    CheckOutThucTe = x.NgayDiThucTe.HasValue ? x.NgayDiThucTe.Value.ToString("dd/MM/yyyy HH:mm") : string.Empty,
                    TrangThai = x.TrangThaiText,
                    KenhDat = string.IsNullOrWhiteSpace(x.KenhDat) ? "TrucTiep" : x.KenhDat,
                    SoHoaDon = x.SoHoaDon,
                    TongHoaDon = x.TongHoaDon.ToString("N0") + " đ",
                    PhatSinh = x.ExtrasRevenue.ToString("N0") + " đ",
                    ThanhToan = x.SoHoaDon <= 0 ? "Chưa có hóa đơn" : (x.DaThanhToanDayDu ? "Đủ" : "Thiếu"),
                    CreatedBy = string.IsNullOrWhiteSpace(x.CreatedBy) ? "-" : x.CreatedBy,
                    UpdatedBy = string.IsNullOrWhiteSpace(x.UpdatedBy) ? "-" : x.UpdatedBy
                })
                .ToList();
            _explorerGrid.DataSource = rows;
            ApplyExplorerGridHeaders();
            if (_explorerPageLabel != null)
                _explorerPageLabel.Text = "Trang " + _explorerCurrentPage + "/" + _explorerTotalPages + " - " + total + " bản ghi";

            _lastExplorerLoadSignature = signature;
            _lastExplorerLoadedUtc = DateTime.UtcNow;

            RequestLoadExplorerDetailForSelection(immediate: true);
        }

        private void ExplorerGrid_SelectionChanged(object sender, EventArgs e)
        {
            RequestLoadExplorerDetailForSelection();
        }

        private void ExplorerGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            RequestLoadExplorerDetailForSelection();
        }

        private void RequestLoadExplorerDetailForSelection(bool immediate = false)
        {
            if (_explorerDetailDebounceTimer == null || immediate)
            {
                if (_explorerDetailDebounceTimer != null)
                    _explorerDetailDebounceTimer.Stop();
                LoadExplorerDetailForSelection();
                return;
            }

            _explorerDetailDebounceTimer.Stop();
            _explorerDetailDebounceTimer.Start();
        }

        private async void LoadExplorerDetailForSelection()
        {
            if (_explorerGrid == null || _explorerStayGrid == null || _explorerExtrasGrid == null || _explorerTimelineGrid == null) return;
            if (_explorerGrid.CurrentRow == null)
            {
                ClearExplorerDetailDataSource();
                return;
            }

            int bookingId;
            if (!int.TryParse(Convert.ToString(_explorerGrid.CurrentRow.Cells["DatPhongID"]?.Value), out bookingId))
            {
                ClearExplorerDetailDataSource();
                return;
            }

            int loadVersion = ++_explorerDetailLoadVersion;
            StatisticsDAL.ExplorerDocumentData doc;
            try
            {
                using (var perf = PerformanceTracker.Measure("MainForm.LoadExplorerDetail",
                    new Dictionary<string, object>
                    {
                        ["BookingId"] = bookingId
                    }))
                {
                    doc = await Task.Run(() => _statisticsDal.GetBookingDocumentData(bookingId, 250));
                    perf.AddContext("StayInfoRows", doc?.StayInfo?.Count ?? 0);
                    perf.AddContext("ExtrasRows", doc?.Extras?.Count ?? 0);
                    perf.AddContext("TimelineRows", doc?.Timeline?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Cannot load explorer detail for current selection.", new Dictionary<string, object>
                {
                    ["BookingId"] = bookingId,
                    ["Error"] = ex.Message
                });
                if ((DateTime.UtcNow - _lastExplorerDetailErrorToastUtc).TotalSeconds >= 10)
                {
                    _lastExplorerDetailErrorToastUtc = DateTime.UtcNow;
                    ShowToast("Không thể tải chi tiết booking. Vui lòng thử lại.", true);
                }
                ClearExplorerDetailDataSource();
                return;
            }

            if (loadVersion != _explorerDetailLoadVersion) return;

            bool overnight = doc != null && doc.Booking != null && doc.Booking.BookingType == 2;
            if (_explorerDetailTabControl != null && _explorerStayTab != null)
            {
                bool hasStayTab = _explorerDetailTabControl.TabPages.Contains(_explorerStayTab);
                if (overnight && !hasStayTab)
                    _explorerDetailTabControl.TabPages.Insert(0, _explorerStayTab);
                else if (!overnight && hasStayTab)
                    _explorerDetailTabControl.TabPages.Remove(_explorerStayTab);
            }

            _explorerStayGrid.DataSource = BuildExplorerStayHorizontalTable(doc?.StayInfo);
            ApplyExplorerStayGridHeaders();

            _explorerExtrasGrid.DataSource = (doc?.Extras ?? new List<StatisticsDAL.ExplorerExtraLine>())
                .Select(x => new ExplorerExtraViewRow
                {
                    ItemCode = x.ItemCode,
                    ItemName = x.ItemName,
                    Qty = x.Qty,
                    UnitPrice = x.UnitPrice.ToString("N0") + " đ",
                    Amount = x.Amount.ToString("N0") + " đ",
                    Note = x.Note
                })
                .ToList();
            ApplyExplorerExtrasGridHeaders();

            _explorerTimelineGrid.DataSource = (doc?.Timeline ?? new List<AuditLogDAL.AuditLogEntry>())
                .Select(x => new ExplorerTimelineViewRow
                {
                    ThoiGianUtc = x.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                    Entity = x.EntityName,
                    HanhDong = x.ActionType,
                    NguoiThucHien = string.IsNullOrWhiteSpace(x.Actor) ? "-" : x.Actor,
                    TruocKhiSua = TruncateText(x.BeforeData, 120),
                    SauKhiSua = TruncateText(x.AfterData, 120),
                    Nguon = string.IsNullOrWhiteSpace(x.Source) ? "-" : x.Source
                })
                .ToList();
            ApplyExplorerTimelineGridHeaders();
        }

        private void ClearExplorerDetailDataSource()
        {
            if (_explorerStayGrid != null) _explorerStayGrid.DataSource = new System.Data.DataTable();
            if (_explorerExtrasGrid != null) _explorerExtrasGrid.DataSource = new List<ExplorerExtraViewRow>();
            if (_explorerTimelineGrid != null) _explorerTimelineGrid.DataSource = new List<ExplorerTimelineViewRow>();
        }

        private async void LoadAuditAndAlerts(bool force = false)
        {
            if (_statsFromPicker == null || _statsToPicker == null || _auditGrid == null || _alertGrid == null) return;
            if (_statsFromPicker.Value.Date > _statsToPicker.Value.Date) return;

            var auditDal = new AuditLogDAL();
            DateTime fromDate = _statsFromPicker.Value.Date;
            DateTime toDate = _statsToPicker.Value.Date;
            int currentPage = _auditCurrentPage;
            string entity = _auditEntityCombo == null ? null : Convert.ToString(_auditEntityCombo.SelectedItem);
            string actor = _auditActorTextBox == null ? null : _auditActorTextBox.Text;
            string keyword = _auditKeywordTextBox == null ? null : _auditKeywordTextBox.Text;
            string signature = BuildAuditLoadSignature(fromDate, toDate, currentPage, entity, actor, keyword);
            if (!force
                && string.Equals(signature, _lastAuditLoadSignature, StringComparison.Ordinal)
                && IsRecentLoad(_lastAuditLoadedUtc))
            {
                return;
            }

            int loadVersion = ++_auditLoadVersion;

            AuditLogDAL.AuditLogPage page;
            try
            {
                using (var perf = PerformanceTracker.Measure("MainForm.LoadAuditLog", new Dictionary<string, object>
                {
                    ["FromDate"] = fromDate.ToString("yyyy-MM-dd"),
                    ["ToDate"] = toDate.ToString("yyyy-MM-dd"),
                    ["Page"] = currentPage
                }))
                {
                    page = await Task.Run(() => auditDal.GetAuditLogs(
                        fromDate,
                        toDate,
                        entity,
                        actor,
                        keyword,
                        currentPage,
                        50));
                    perf.AddContext("PageRows", page?.Items?.Count ?? 0);
                    perf.AddContext("TotalRows", page?.TotalCount ?? 0);
                }
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.LoadAuditLog", "Không thể tải audit log. Vui lòng thử lại.", ex, "Lỗi audit");
                return;
            }

            if (loadVersion != _auditLoadVersion) return;
            if (_statsFromPicker == null || _statsToPicker == null) return;
            if (_statsFromPicker.Value.Date != fromDate || _statsToPicker.Value.Date != toDate) return;

            int total = page?.TotalCount ?? 0;
            _auditTotalPages = Math.Max(1, (int)Math.Ceiling(total / 50d));
            if (_auditCurrentPage > _auditTotalPages)
            {
                _auditCurrentPage = _auditTotalPages;
                LoadAuditAndAlerts();
                return;
            }

            _auditGrid.DataSource = (page?.Items ?? new List<AuditLogDAL.AuditLogEntry>())
                .Select(x => new AuditGridViewRow
                {
                    AuditLogID = x.AuditLogID,
                    ThoiGianUtc = x.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                    Entity = x.EntityName,
                    EntityId = x.EntityId.HasValue ? x.EntityId.Value.ToString() : string.Empty,
                    HanhDong = x.ActionType,
                    NguoiThucHien = string.IsNullOrWhiteSpace(x.Actor) ? "-" : x.Actor,
                    Nguon = string.IsNullOrWhiteSpace(x.Source) ? "-" : x.Source,
                    TruocKhiSua = TruncateText(x.BeforeData, 80),
                    SauKhiSua = TruncateText(x.AfterData, 80)
                })
                .ToList();
            ApplyAuditGridHeaders();
            if (_auditPageLabel != null)
                _auditPageLabel.Text = "Trang " + _auditCurrentPage + "/" + _auditTotalPages + " - " + total + " bản ghi";

            List<StatisticsDAL.DataQualityAlert> alerts;
            try
            {
                using (var perf = PerformanceTracker.Measure("MainForm.LoadDataAlerts", new Dictionary<string, object>
                {
                    ["FromDate"] = fromDate.ToString("yyyy-MM-dd"),
                    ["ToDate"] = toDate.ToString("yyyy-MM-dd")
                }))
                {
                    alerts = await Task.Run(() => _statisticsDal.GetDataQualityAlerts(fromDate, toDate, 300));
                    perf.AddContext("AlertRows", alerts?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.LoadDataAlerts", "Không thể tải cảnh báo dữ liệu. Vui lòng thử lại.", ex, "Lỗi cảnh báo");
                return;
            }

            if (loadVersion != _auditLoadVersion) return;

            _alertGrid.DataSource = (alerts ?? new List<StatisticsDAL.DataQualityAlert>())
                .Select(x => new AlertGridViewRow
                {
                    MucDo = x.Severity,
                    MaCanhBao = x.Code,
                    NoiDung = x.Message,
                    ThamChieu = x.Reference,
                    ThoiGian = x.EventTime.HasValue ? x.EventTime.Value.ToString("dd/MM/yyyy HH:mm") : string.Empty
                })
                .ToList();
            ApplyAlertGridHeaders();

            _lastAuditLoadSignature = signature;
            _lastAuditLoadedUtc = DateTime.UtcNow;
        }

        private void ApplyExplorerGridHeaders()
        {
            if (_explorerGrid == null || _explorerGrid.Columns.Count == 0) return;
            if (_explorerGrid.Columns["DatPhongID"] != null) _explorerGrid.Columns["DatPhongID"].HeaderText = "Mã đặt";
            if (_explorerGrid.Columns["MaPhong"] != null) _explorerGrid.Columns["MaPhong"].HeaderText = "Phòng";
            if (_explorerGrid.Columns["LoaiPhong"] != null) _explorerGrid.Columns["LoaiPhong"].HeaderText = "Loại phòng";
            if (_explorerGrid.Columns["LoaiDat"] != null) _explorerGrid.Columns["LoaiDat"].HeaderText = "Loại đặt";
            if (_explorerGrid.Columns["KhachHang"] != null) _explorerGrid.Columns["KhachHang"].HeaderText = "Khách hàng";
            if (_explorerGrid.Columns["CCCD"] != null) _explorerGrid.Columns["CCCD"].HeaderText = "CCCD";
            if (_explorerGrid.Columns["CheckIn"] != null) _explorerGrid.Columns["CheckIn"].HeaderText = "Check-in";
            if (_explorerGrid.Columns["CheckOutDuKien"] != null) _explorerGrid.Columns["CheckOutDuKien"].HeaderText = "Check-out dự kiến";
            if (_explorerGrid.Columns["CheckOutThucTe"] != null) _explorerGrid.Columns["CheckOutThucTe"].HeaderText = "Check-out thực tế";
            if (_explorerGrid.Columns["TrangThai"] != null) _explorerGrid.Columns["TrangThai"].HeaderText = "Trạng thái";
            if (_explorerGrid.Columns["KenhDat"] != null) _explorerGrid.Columns["KenhDat"].HeaderText = "Kênh đặt";
            if (_explorerGrid.Columns["SoHoaDon"] != null) _explorerGrid.Columns["SoHoaDon"].HeaderText = "Số hóa đơn";
            if (_explorerGrid.Columns["TongHoaDon"] != null)
            {
                _explorerGrid.Columns["TongHoaDon"].HeaderText = "Tổng hóa đơn";
                _explorerGrid.Columns["TongHoaDon"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_explorerGrid.Columns["PhatSinh"] != null)
            {
                _explorerGrid.Columns["PhatSinh"].HeaderText = "Phát sinh";
                _explorerGrid.Columns["PhatSinh"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_explorerGrid.Columns["ThanhToan"] != null) _explorerGrid.Columns["ThanhToan"].HeaderText = "Thanh toán";
            if (_explorerGrid.Columns["CreatedBy"] != null) _explorerGrid.Columns["CreatedBy"].HeaderText = "Tạo bởi";
            if (_explorerGrid.Columns["UpdatedBy"] != null) _explorerGrid.Columns["UpdatedBy"].HeaderText = "Cập nhật bởi";
        }

        private void ApplyExplorerStayGridHeaders()
        {
            if (_explorerStayGrid == null || _explorerStayGrid.Columns.Count == 0) return;
            _explorerStayGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _explorerStayGrid.RowHeadersVisible = false;

            foreach (DataGridViewColumn column in _explorerStayGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                var headerFont = _explorerStayGrid.ColumnHeadersDefaultCellStyle.Font ?? _explorerStayGrid.Font;
                int measured = TextRenderer.MeasureText(column.HeaderText ?? string.Empty, headerFont).Width + 28;
                column.Width = Math.Max(120, Math.Min(280, measured));
            }
        }

        private static System.Data.DataTable BuildExplorerStayHorizontalTable(List<StatisticsDAL.ExplorerStayLine> stayLines)
        {
            var table = new System.Data.DataTable();
            var normalized = (stayLines ?? new List<StatisticsDAL.ExplorerStayLine>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Field))
                .ToList();
            if (normalized.Count == 0) return table;

            foreach (var line in normalized)
            {
                string columnName = GetUniqueStayColumnName(table.Columns, line.Field.Trim());
                table.Columns.Add(columnName, typeof(string));
            }

            var row = table.NewRow();
            for (int i = 0; i < normalized.Count; i++)
            {
                row[i] = string.IsNullOrWhiteSpace(normalized[i].Value) ? "-" : normalized[i].Value.Trim();
            }
            table.Rows.Add(row);
            return table;
        }

        private static string GetUniqueStayColumnName(System.Data.DataColumnCollection columns, string desiredName)
        {
            string baseName = string.IsNullOrWhiteSpace(desiredName) ? "Thong tin" : desiredName;
            string current = baseName;
            int suffix = 2;
            while (columns.Contains(current))
            {
                current = baseName + " (" + suffix + ")";
                suffix++;
            }
            return current;
        }

        private void ApplyExplorerExtrasGridHeaders()
        {
            if (_explorerExtrasGrid == null || _explorerExtrasGrid.Columns.Count == 0) return;
            if (_explorerExtrasGrid.Columns["ItemCode"] != null) _explorerExtrasGrid.Columns["ItemCode"].HeaderText = "Mã";
            if (_explorerExtrasGrid.Columns["ItemName"] != null) _explorerExtrasGrid.Columns["ItemName"].HeaderText = "Tên phát sinh";
            if (_explorerExtrasGrid.Columns["Qty"] != null) _explorerExtrasGrid.Columns["Qty"].HeaderText = "SL";
            if (_explorerExtrasGrid.Columns["UnitPrice"] != null)
            {
                _explorerExtrasGrid.Columns["UnitPrice"].HeaderText = "Đơn giá";
                _explorerExtrasGrid.Columns["UnitPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_explorerExtrasGrid.Columns["Amount"] != null)
            {
                _explorerExtrasGrid.Columns["Amount"].HeaderText = "Thành tiền";
                _explorerExtrasGrid.Columns["Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_explorerExtrasGrid.Columns["Note"] != null) _explorerExtrasGrid.Columns["Note"].HeaderText = "Ghi chú";
        }

        private void ApplyExplorerTimelineGridHeaders()
        {
            if (_explorerTimelineGrid == null || _explorerTimelineGrid.Columns.Count == 0) return;
            if (_explorerTimelineGrid.Columns["ThoiGianUtc"] != null) _explorerTimelineGrid.Columns["ThoiGianUtc"].HeaderText = "Thời gian UTC";
            if (_explorerTimelineGrid.Columns["Entity"] != null) _explorerTimelineGrid.Columns["Entity"].HeaderText = "Entity";
            if (_explorerTimelineGrid.Columns["HanhDong"] != null) _explorerTimelineGrid.Columns["HanhDong"].HeaderText = "Hành động";
            if (_explorerTimelineGrid.Columns["NguoiThucHien"] != null) _explorerTimelineGrid.Columns["NguoiThucHien"].HeaderText = "Người thực hiện";
            if (_explorerTimelineGrid.Columns["TruocKhiSua"] != null) _explorerTimelineGrid.Columns["TruocKhiSua"].HeaderText = "Trước khi sửa";
            if (_explorerTimelineGrid.Columns["SauKhiSua"] != null) _explorerTimelineGrid.Columns["SauKhiSua"].HeaderText = "Sau khi sửa";
            if (_explorerTimelineGrid.Columns["Nguon"] != null) _explorerTimelineGrid.Columns["Nguon"].HeaderText = "Nguồn";
        }

        private void ApplyAuditGridHeaders()
        {
            if (_auditGrid == null || _auditGrid.Columns.Count == 0) return;
            if (_auditGrid.Columns["AuditLogID"] != null) _auditGrid.Columns["AuditLogID"].HeaderText = "Mã log";
            if (_auditGrid.Columns["ThoiGianUtc"] != null) _auditGrid.Columns["ThoiGianUtc"].HeaderText = "Thời gian UTC";
            if (_auditGrid.Columns["Entity"] != null) _auditGrid.Columns["Entity"].HeaderText = "Entity";
            if (_auditGrid.Columns["EntityId"] != null) _auditGrid.Columns["EntityId"].HeaderText = "EntityID";
            if (_auditGrid.Columns["HanhDong"] != null) _auditGrid.Columns["HanhDong"].HeaderText = "Hành động";
            if (_auditGrid.Columns["NguoiThucHien"] != null) _auditGrid.Columns["NguoiThucHien"].HeaderText = "Người thực hiện";
            if (_auditGrid.Columns["Nguon"] != null) _auditGrid.Columns["Nguon"].HeaderText = "Nguồn";
            if (_auditGrid.Columns["TruocKhiSua"] != null) _auditGrid.Columns["TruocKhiSua"].HeaderText = "Trước khi sửa";
            if (_auditGrid.Columns["SauKhiSua"] != null) _auditGrid.Columns["SauKhiSua"].HeaderText = "Sau khi sửa";
        }

        private void ApplyAlertGridHeaders()
        {
            if (_alertGrid == null || _alertGrid.Columns.Count == 0) return;
            if (_alertGrid.Columns["MucDo"] != null) _alertGrid.Columns["MucDo"].HeaderText = "Mức độ";
            if (_alertGrid.Columns["MaCanhBao"] != null) _alertGrid.Columns["MaCanhBao"].HeaderText = "Mã";
            if (_alertGrid.Columns["NoiDung"] != null) _alertGrid.Columns["NoiDung"].HeaderText = "Nội dung";
            if (_alertGrid.Columns["ThamChieu"] != null) _alertGrid.Columns["ThamChieu"].HeaderText = "Tham chiếu";
            if (_alertGrid.Columns["ThoiGian"] != null) _alertGrid.Columns["ThoiGian"].HeaderText = "Thời gian";
        }

        private static string TruncateText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (value.Length <= maxLength) return value;
            return value.Substring(0, maxLength) + "...";
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
            if (_statsRoomGrid.Columns["DatPhongID"] != null) _statsRoomGrid.Columns["DatPhongID"].Visible = false;
            if (_statsRoomGrid.Columns["BookingType"] != null) _statsRoomGrid.Columns["BookingType"].Visible = false;
            if (_statsRoomGrid.Columns["LoaiDat"] != null) _statsRoomGrid.Columns["LoaiDat"].HeaderText = "Loại đặt";
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
            if (_statsRoomGrid.Columns["XemNguoiO"] != null)
            {
                _statsRoomGrid.Columns["XemNguoiO"].HeaderText = "Người ở";
                _statsRoomGrid.Columns["XemNguoiO"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _statsRoomGrid.Columns["XemNguoiO"].DefaultCellStyle.ForeColor = Color.FromArgb(31, 71, 136);
            }
        }

        private sealed class RevenueReportViewData
        {
            public string TotalInvoices { get; set; }
            public string PaidInvoices { get; set; }
            public string UnpaidInvoices { get; set; }
            public string TotalRevenue { get; set; }
            public string UnpaidRevenue { get; set; }
            public string RangeText { get; set; }
            public List<RevenueDailyViewRow> DailyRows { get; set; }
            public List<RevenueRoomViewRow> RoomRows { get; set; }
            public List<RevenueInvoiceViewRow> InvoiceRows { get; set; }
        }

        private async void LoadRevenueReportData()
        {
            if (_reportFromPicker == null || _reportToPicker == null) return;
            if (_isRevenueReportLoading) return;

            DateTime fromDate = _reportFromPicker.Value.Date;
            DateTime toDate = _reportToPicker.Value.Date;
            if (fromDate > toDate)
            {
                MessageBox.Show("Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.", "Khoảng thời gian không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _reportFromPicker.Focus();
                return;
            }

            _isRevenueReportLoading = true;
            try
            {
                using (var perf = PerformanceTracker.Measure("MainForm.LoadRevenueReport", new Dictionary<string, object>
                {
                    ["FromDate"] = fromDate.ToString("yyyy-MM-dd"),
                    ["ToDate"] = toDate.ToString("yyyy-MM-dd")
                }))
                {
                    InvoiceDAL.RevenueReportData data = await Task.Run(() => _invoiceDal.GetRevenueReport(fromDate, toDate));
                    RevenueReportViewData viewData = await Task.Run(() => BuildRevenueReportViewData(data, fromDate, toDate));

                    if (IsDisposed || _reportFromPicker == null || _reportToPicker == null) return;
                    if (_reportFromPicker.Value.Date != fromDate || _reportToPicker.Value.Date != toDate) return;

                    ApplyRevenueReportViewData(viewData);
                    perf.AddContext("DailyRows", viewData?.DailyRows?.Count ?? 0);
                    perf.AddContext("RoomRows", viewData?.RoomRows?.Count ?? 0);
                    perf.AddContext("InvoiceRows", viewData?.InvoiceRows?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.LoadRevenueReport", "Không thể tải dữ liệu báo cáo. Vui lòng thử lại.", ex, "Lỗi báo cáo");
            }
            finally
            {
                _isRevenueReportLoading = false;
            }
        }

        private static RevenueReportViewData BuildRevenueReportViewData(InvoiceDAL.RevenueReportData data, DateTime fromDate, DateTime toDate)
        {
            var summary = data?.Summary ?? new InvoiceDAL.RevenueSummaryStats();
            return new RevenueReportViewData
            {
                TotalInvoices = summary.TotalInvoices.ToString("N0"),
                PaidInvoices = summary.PaidInvoices.ToString("N0"),
                UnpaidInvoices = summary.UnpaidInvoices.ToString("N0"),
                TotalRevenue = summary.TotalRevenue.ToString("N0") + " đ",
                UnpaidRevenue = summary.UnpaidRevenue.ToString("N0") + " đ",
                RangeText = "Khoảng lọc: " + fromDate.ToString("dd/MM/yyyy") + " - " + toDate.ToString("dd/MM/yyyy"),
                DailyRows = (data?.Daily ?? new List<InvoiceDAL.RevenueDailyStats>())
                    .Select(x => new RevenueDailyViewRow
                    {
                        Ngay = x.Date.ToString("dd/MM/yyyy"),
                        SoHoaDon = x.InvoiceCount,
                        TongDoanhThu = x.TotalRevenue.ToString("N0") + " đ",
                        DaThu = x.PaidRevenue.ToString("N0") + " đ",
                        ChuaThu = x.UnpaidRevenue.ToString("N0") + " đ"
                    })
                    .ToList(),
                RoomRows = (data?.ByRoom ?? new List<InvoiceDAL.RevenueRoomStats>())
                    .Select(x => new RevenueRoomViewRow
                    {
                        MaPhong = x.MaPhong,
                        SoHoaDon = x.InvoiceCount,
                        TongDoanhThu = x.TotalRevenue.ToString("N0") + " đ",
                        DaThu = x.PaidRevenue.ToString("N0") + " đ"
                    })
                    .ToList(),
                InvoiceRows = (data?.Invoices ?? new List<InvoiceDAL.RevenueInvoiceStats>())
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
                    .ToList()
            };
        }

        private void ApplyRevenueReportViewData(RevenueReportViewData viewData)
        {
            if (viewData == null) viewData = new RevenueReportViewData();

            if (_reportTotalInvoicesValue != null) _reportTotalInvoicesValue.Text = viewData.TotalInvoices ?? "0";
            if (_reportPaidInvoicesValue != null) _reportPaidInvoicesValue.Text = viewData.PaidInvoices ?? "0";
            if (_reportUnpaidInvoicesValue != null) _reportUnpaidInvoicesValue.Text = viewData.UnpaidInvoices ?? "0";
            if (_reportTotalRevenueValue != null) _reportTotalRevenueValue.Text = viewData.TotalRevenue ?? "0 đ";
            if (_reportUnpaidRevenueValue != null) _reportUnpaidRevenueValue.Text = viewData.UnpaidRevenue ?? "0 đ";
            if (_reportRangeLabel != null) _reportRangeLabel.Text = viewData.RangeText ?? string.Empty;

            if (_reportDailyGrid != null)
            {
                _reportDailyGrid.DataSource = viewData.DailyRows ?? new List<RevenueDailyViewRow>();
                ApplyRevenueDailyGridHeaders();
            }

            if (_reportRoomGrid != null)
            {
                _reportRoomGrid.DataSource = viewData.RoomRows ?? new List<RevenueRoomViewRow>();
                ApplyRevenueRoomGridHeaders();
            }

            if (_reportInvoiceGrid != null)
            {
                _reportInvoiceGrid.DataSource = viewData.InvoiceRows ?? new List<RevenueInvoiceViewRow>();
                ApplyRevenueInvoiceGridHeaders();
            }
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

        private async void SeedReportSampleData()
        {
            if (_isReportSeedLoading) return;
            _isReportSeedLoading = true;
            try
            {
                InvoiceDAL.SampleSeedResult result;
                using (var perf = PerformanceTracker.Measure("MainForm.SeedSampleData"))
                {
                    result = await Task.Run(() => _invoiceDal.SeedSampleReportData());
                    perf.AddContext("AlreadySeeded", result?.AlreadySeeded ?? false);
                    perf.AddContext("AddedBookings", result?.AddedBookings ?? 0);
                    perf.AddContext("AddedInvoices", result?.AddedInvoices ?? 0);
                }

                if (result.AlreadySeeded)
                {
                    ShowToast("Dữ liệu mẫu đã tồn tại trước đó.");
                }
                else
                {
                    ShowToast(
                        "Đã tạo dữ liệu mẫu: Khách mẫu " + result.AddedCustomers +
                        ", Đặt phòng " + result.AddedBookings +
                        ", Hóa đơn " + result.AddedInvoices + ".");
                }

                LoadRevenueReportData();
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.SeedSampleData", "Không thể tạo dữ liệu mẫu. Vui lòng thử lại.", ex, "Lỗi seed dữ liệu");
            }
            finally
            {
                _isReportSeedLoading = false;
            }
        }

        private async void ExportRevenueCsv()
        {
            if (_reportFromPicker == null || _reportToPicker == null) return;
            if (_isRevenueCsvExporting) return;

            DateTime fromDate = _reportFromPicker.Value.Date;
            DateTime toDate = _reportToPicker.Value.Date;
            if (fromDate > toDate)
            {
                MessageBox.Show("Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.", "Khoảng thời gian không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _reportFromPicker.Focus();
                return;
            }

            string targetPath;
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV (*.csv)|*.csv";
                dialog.FileName = "bao-cao-doanh-thu-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                targetPath = dialog.FileName;
            }

            _isRevenueCsvExporting = true;
            UseWaitCursor = true;
            try
            {
                int invoiceCount;
                using (var perf = PerformanceTracker.Measure("MainForm.ExportRevenueCsv", new Dictionary<string, object>
                {
                    ["FromDate"] = fromDate.ToString("yyyy-MM-dd"),
                    ["ToDate"] = toDate.ToString("yyyy-MM-dd")
                }))
                {
                    invoiceCount = await Task.Run(() => ExportRevenueCsvToFile(targetPath, fromDate, toDate));
                    perf.AddContext("InvoiceRows", invoiceCount);
                }

                if (invoiceCount <= 0)
                {
                    MessageBox.Show("Không có dữ liệu hóa đơn trong khoảng lọc để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowToast("Đã xuất báo cáo CSV thành công (" + invoiceCount.ToString("N0") + " hóa đơn).");
            }
            catch (Exception ex)
            {
                ShowFriendlyError("MainForm.ExportCsvWriteFile", "Ghi file CSV thất bại. Vui lòng thử lại.", ex, "Lỗi xuất CSV");
            }
            finally
            {
                UseWaitCursor = false;
                _isRevenueCsvExporting = false;
            }
        }

        private int ExportRevenueCsvToFile(string filePath, DateTime fromDate, DateTime toDate)
        {
            InvoiceDAL.RevenueReportData data = _invoiceDal.GetRevenueReport(fromDate, toDate);
            var invoices = data?.Invoices ?? new List<InvoiceDAL.RevenueInvoiceStats>();
            if (invoices.Count == 0) return 0;

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
            {
                writer.WriteLine("HoaDonID,DatPhongID,NgayLap,MaPhong,KhachHang,TongTien,DaThanhToan");
                foreach (var row in invoices)
                {
                    writer.Write(EscapeCsv(row.HoaDonID.ToString()));
                    writer.Write(",");
                    writer.Write(EscapeCsv(row.DatPhongID.ToString()));
                    writer.Write(",");
                    writer.Write(EscapeCsv(row.NgayLap.ToString("yyyy-MM-dd HH:mm:ss")));
                    writer.Write(",");
                    writer.Write(EscapeCsv(row.MaPhong));
                    writer.Write(",");
                    writer.Write(EscapeCsv(row.KhachHang));
                    writer.Write(",");
                    writer.Write(EscapeCsv(row.TongTien.ToString("0.##")));
                    writer.Write(",");
                    writer.Write(EscapeCsv(row.DaThanhToan ? "1" : "0"));
                    writer.WriteLine();
                }
            }

            return invoices.Count;
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
            DetachDetailHostControls(disposeDetachedTransientControls: false);
            panelDetailHost.Visible = false;
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

            if (_resizeDebounceTimer != null)
            {
                _resizeDebounceTimer.Stop();
                _resizeDebounceTimer.Dispose();
                _resizeDebounceTimer = null;
            }

            if (_explorerDetailDebounceTimer != null)
            {
                _explorerDetailDebounceTimer.Stop();
                _explorerDetailDebounceTimer.Dispose();
                _explorerDetailDebounceTimer = null;
            }

            DisposeTransientDetailFormCache();
            DetachDetailHostControls(disposeDetachedTransientControls: true);

            if (_bookingStatisticsView != null)
            {
                _bookingStatisticsView.Dispose();
                _bookingStatisticsView = null;
            }

            if (_managementView != null)
            {
                _managementView.Dispose();
                _managementView = null;
            }

            if (_revenueReportView != null)
            {
                _revenueReportView.Dispose();
                _revenueReportView = null;
            }

            CloseToast();
            foreach (var font in _roomTileFontCache.Values)
            {
                font?.Dispose();
            }
            _roomTileFontCache.Clear();
            _roomTileInfos.Clear();
            _roomToolTip.Dispose();

            base.OnFormClosed(e);
        }

        #endregion
    }
}
