using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;

namespace HotelManagement.Forms
{
    public class OvernightCheckoutForm : Form
    {
        private readonly Color _pageBackground = Color.FromArgb(242, 245, 251);
        private readonly Color _cardBorder = Color.FromArgb(212, 221, 235);
        private readonly Color _textPrimary = Color.FromArgb(35, 45, 73);
        private readonly Color _textSecondary = Color.FromArgb(98, 111, 141);
        private readonly Color _success = Color.FromArgb(62, 159, 145);
        private readonly Color _danger = Color.FromArgb(214, 82, 82);
        private readonly Color _brandA = Color.FromArgb(43, 67, 188);

        private readonly Room _room;
        private readonly BookingDAL _bookingDal = new BookingDAL();
        private readonly PricingService _pricingService = PricingService.Instance;
        private readonly CheckoutService _checkoutService = new CheckoutService();
        private int _bookingId;
        private PricingService.PricingConfig _pricing = PricingService.GetDefaultConfig();

        private int _nightCount = 1;
        private int _savedSoftDrinkCount;
        private int _savedWaterBottleCount;
        private decimal _savedDrinkCharge;
        private decimal _savedCollectedAmount;
        private int _pendingSoftDrinkCount;
        private int _pendingWaterBottleCount;
        private decimal _nightlyRate;
        private decimal _dailyRate;

        private Label lblTitleRoom;
        private Label lblChipRoom;
        private Label lblCheckin;
        private DateTimePicker dtpCheckinTime;
        private Label lblNightCount;
        private Label lblRateFrameInline;
        private Label lblNightlyRate;
        private Label lblDailyRate;
        private Label lblPendingDrink;
        private Label lblPendingWater;
        private Label lblDrinkSubtotal;
        private Label lblRoomChargeValue;
        private Label lblRoomChargeDetail;
        private Label lblDrinkChargeValue;
        private Label lblLateFeeValue;
        private Label lblSurchargeReasonValue;
        private Label lblTotalChargeValue;
        private Label lblCollectedValue;
        private Label lblDueValue;
        private TextBox txtGuestName;
        private Button btnCancel;
        private Button btnSave;
        private Button btnPay;
        private Button btnCancelRoom;
        private Timer _refreshTimer;
        private TableLayoutPanel _contentGrid;
        private TableLayoutPanel _contentLeftColumn;
        private TableLayoutPanel _contentRightColumn;
        private bool _isCompactLayout;
        private bool _isSyncingCheckinInput;
        private const decimal NightlyRateStep = 10000m;
        private const decimal MinNightlyRate = 10000m;
        private const decimal DailyRateStep = 10000m;
        private const decimal MinDailyRate = 10000m;

        public event EventHandler BackRequested;
        public event EventHandler Saved;
        public event EventHandler PaymentCompleted;
        public event EventHandler RoomCancelled;

        public OvernightCheckoutForm(Room room)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
            _pricing = _pricingService.GetCurrentPricing();
            InitializeUi();
            Load += OvernightCheckoutForm_Load;
            Disposed += OvernightCheckoutForm_Disposed;
        }

        private async void OvernightCheckoutForm_Load(object sender, EventArgs e)
        {
            bool initialized = false;
            await UiExceptionHandler.RunAsync(this, "OvernightCheckout.Load", async () =>
            {
                using (var perf = PerformanceTracker.Measure("OvernightCheckout.Load", new Dictionary<string, object>
                {
                    ["RoomId"] = _room.PhongID
                }))
                {
                    var loadData = await Task.Run(() => LoadOvernightCheckoutData()).ConfigureAwait(true);
                    _bookingId = loadData.BookingId;
                    ReloadPricingSettings();
                    _pricingService.PricingChanged += PricingService_PricingChanged;
                    var stayInfo = loadData.StayInfo;
                    _nightCount = stayInfo == null ? 1 : Math.Max(1, stayInfo.SoDemLuuTru);
                    _savedCollectedAmount = loadData.PaidAmount;
                    _pendingSoftDrinkCount = 0;
                    _pendingWaterBottleCount = 0;
                    ApplySavedExtras(loadData.Extras);
                    _nightlyRate = stayInfo == null ? 0m : stayInfo.GiaPhong;
                    if (_nightlyRate <= 0m)
                        _nightlyRate = loadData.DefaultNightRate > 0m ? loadData.DefaultNightRate : GetDefaultNightRateByRoomType();
                    if (_dailyRate <= 0m)
                        _dailyRate = GetDefaultDailyRateByRoomType();

                    lblTitleRoom.Text = "Quản lý phòng qua đêm - Phòng " + _room.MaPhong;
                    lblChipRoom.Text = "  Phòng " + _room.MaPhong + "  ";
                    lblCheckin.Text = "Check-in: " + (_room.ThoiGianBatDau ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss");
                    SyncCheckinInputFromRoom();
                    if (txtGuestName != null)
                        txtGuestName.Text = ResolveGuestNameForForm(stayInfo);

                    RefreshTotals();
                    EnsureRefreshTimer();
                    initialized = true;
                    perf.AddContext("BookingId", _bookingId);
                }
            }).ConfigureAwait(true);

            if (!initialized)
                BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OvernightCheckoutForm_Disposed(object sender, EventArgs e)
        {
            _pricingService.PricingChanged -= PricingService_PricingChanged;
            if (_refreshTimer == null) return;
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _refreshTimer = null;
        }

        private sealed class OvernightCheckoutLoadData
        {
            public int BookingId { get; set; }
            public BookingDAL.StayInfoRecord StayInfo { get; set; }
            public List<BookingDAL.BookingExtraRecord> Extras { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal DefaultNightRate { get; set; }
        }

        private OvernightCheckoutLoadData LoadOvernightCheckoutData()
        {
            int bookingId = _bookingDal.EnsureBookingForRoom(_room, 2);
            var stayInfo = _bookingDal.GetStayInfoByBooking(bookingId);
            var extras = _bookingDal.GetBookingExtras(bookingId);
            decimal paidAmount = _bookingDal.GetPaidAmountByBooking(bookingId);
            decimal defaultNightRate = _bookingDal.GetDonGiaNgayByPhong(_room.PhongID);
            return new OvernightCheckoutLoadData
            {
                BookingId = bookingId,
                StayInfo = stayInfo,
                Extras = extras,
                PaidAmount = paidAmount,
                DefaultNightRate = defaultNightRate
            };
        }

        private void PricingService_PricingChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ReloadPricingSettings));
                return;
            }

            ReloadPricingSettings();
        }

        private void ReloadPricingSettings()
        {
            _pricing = _pricingService.GetCurrentPricing();
            if (_nightlyRate <= 0m)
                _nightlyRate = GetDefaultNightRateByRoomType();
            if (_dailyRate <= 0m)
                _dailyRate = GetDefaultDailyRateByRoomType();
            RefreshTotals();
        }

        private void EnsureRefreshTimer()
        {
            if (_refreshTimer != null) return;
            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += (s, e) =>
            {
                if (!IsDisposed && Visible)
                    RefreshTotals();
            };
            _refreshTimer.Start();
        }

        private void InitializeUi()
        {
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9F);
            BackColor = _pageBackground;
            Dock = DockStyle.Fill;
            Margin = new Padding(0);
            Padding = new Padding(0);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = _pageBackground,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var content = BuildContent();
            root.Controls.Add(BuildHero(), 0, 0);
            root.Controls.Add(content, 0, 1);

            Controls.Add(root);
            Resize += (s, e) => UpdateResponsiveLayout();
            UpdateResponsiveLayout();
        }

        private Control BuildHero()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 86,
                Padding = new Padding(10, 8, 10, 4),
                BackColor = _pageBackground,
                Margin = new Padding(0)
            };

            lblTitleRoom = new Label
            {
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            };

            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            lblChipRoom = new Label
            {
                AutoSize = true,
                BackColor = Color.FromArgb(65, 161, 145),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 0, 8, 0)
            };
            lblChipRoom.Paint += (s, e) =>
            {
                var rect = lblChipRoom.ClientRectangle;
                rect.Inflate(-1, -1);
                using (var path = RoundedRect(rect, 14))
                using (var pen = new Pen(Color.FromArgb(55, 144, 129)))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            };
            ApplyRoundedRegion(lblChipRoom, 12);

            lblCheckin = new Label
            {
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 5, 0, 0)
            };

            row.Controls.Add(lblChipRoom);
            row.Controls.Add(lblCheckin);

            var heroLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heroLayout.Controls.Add(lblTitleRoom, 0, 0);
            heroLayout.Controls.Add(row, 0, 1);

            panel.Controls.Add(heroLayout);
            return panel;
        }

        private Control BuildContent()
        {
            var wrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 2, 10, 8),
                AutoScroll = true,
                BackColor = _pageBackground,
                Margin = new Padding(0)
            };

            _contentGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
            _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));

            _contentLeftColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 1,
                Margin = new Padding(0, 0, 8, 0)
            };
            _contentLeftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _contentRightColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 1,
                Margin = new Padding(0)
            };
            _contentRightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _contentLeftColumn.Controls.Add(BuildStayCard(), 0, 0);
            _contentRightColumn.Controls.Add(BuildSummaryCard(), 0, 0);

            _contentGrid.Controls.Add(_contentLeftColumn, 0, 0);
            _contentGrid.Controls.Add(_contentRightColumn, 1, 0);

            wrapper.Controls.Add(_contentGrid);
            return wrapper;
        }

        private Control BuildStayCard()
        {
            var card = CreateCard();
            card.Padding = new Padding(10, 8, 10, 8);
            card.Margin = new Padding(0);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 6
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            body.Controls.Add(CreateCardTitle("Thông tin lưu trú qua đêm", null), 0, 0);

            var stayGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 2, 0, 0),
                Padding = new Padding(0)
            };
            stayGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43F));
            stayGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57F));

            var lblStayLabel = new Label
            {
                Text = "Số đêm ở:",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 6, 0, 2)
            };
            var stepNight = CreateStepper(
                getValue: () => _nightCount,
                setValue: v =>
                {
                    _nightCount = Math.Max(1, v);
                    RefreshTotals();
                },
                minusColor: _danger,
                plusColor: _success,
                out lblNightCount);
            stepNight.Dock = DockStyle.None;
            stepNight.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            stayGrid.Controls.Add(lblStayLabel, 0, 0);
            stayGrid.Controls.Add(stepNight, 1, 0);

            var lblCheckinLabel = new Label
            {
                Text = "Giờ vào:",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 2)
            };
            dtpCheckinTime = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm:ss",
                ShowUpDown = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Font = new Font("Segoe UI", 9.5F)
            };
            dtpCheckinTime.ValueChanged += (s, e) => HandleCheckinTimeChanged();

            var checkinHost = CreateInputFrame(dtpCheckinTime, 34);
            checkinHost.Margin = new Padding(0, 0, 0, 2);
            stayGrid.Controls.Add(lblCheckinLabel, 0, 1);
            stayGrid.Controls.Add(checkinHost, 1, 1);

            var lblStartLabel = new Label
            {
                Text = "Khung giá:",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 1)
            };
            lblRateFrameInline = new Label
            {
                Text = string.Empty,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 1),
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                AutoEllipsis = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            stayGrid.Controls.Add(lblStartLabel, 0, 2);
            stayGrid.Controls.Add(lblRateFrameInline, 1, 2);

            var lblNightlyRateLabel = new Label
            {
                Text = "Giá đêm áp dụng:",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 2)
            };
            var stepNightlyRate = CreateMoneyStepper(
                getValue: () => _nightlyRate,
                setValue: v =>
                {
                    _nightlyRate = Math.Max(MinNightlyRate, v);
                    RefreshTotals();
                },
                step: NightlyRateStep,
                minValue: MinNightlyRate,
                minusColor: _danger,
                plusColor: _success,
                out lblNightlyRate);
            stepNightlyRate.Dock = DockStyle.None;
            stepNightlyRate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            stayGrid.Controls.Add(lblNightlyRateLabel, 0, 3);
            stayGrid.Controls.Add(stepNightlyRate, 1, 3);

            var lblDailyRateLabel = new Label
            {
                Text = "Giá ngày áp dụng:",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 2)
            };
            var stepDailyRate = CreateMoneyStepper(
                getValue: () => _dailyRate,
                setValue: v =>
                {
                    _dailyRate = Math.Max(MinDailyRate, v);
                    RefreshTotals();
                },
                step: DailyRateStep,
                minValue: MinDailyRate,
                minusColor: _danger,
                plusColor: _success,
                out lblDailyRate);
            stepDailyRate.Dock = DockStyle.None;
            stepDailyRate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            stayGrid.Controls.Add(lblDailyRateLabel, 0, 4);
            stayGrid.Controls.Add(stepDailyRate, 1, 4);

            var lblGuestLabel = new Label
            {
                Text = "Tên khách (không bắt buộc):",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 2)
            };
            txtGuestName = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 2),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5F)
            };
            stayGrid.Controls.Add(lblGuestLabel, 0, 5);
            stayGrid.Controls.Add(txtGuestName, 1, 5);

            body.Controls.Add(stayGrid, 0, 1);
            body.Controls.Add(CreateDivider(0, 4), 0, 2);

            var drinkTitle = new Label
            {
                Text = "Nước sử dụng",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 1)
            };
            body.Controls.Add(drinkTitle, 0, 3);

            var drinkHint = new Label
            {
                Text = "Double-click vào ô số để nhập số lượng bất kỳ",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 8.8F),
                Margin = new Padding(0, 0, 0, 2)
            };
            body.Controls.Add(drinkHint, 0, 4);

            var drinkRows = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0)
            };
            drinkRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            drinkRows.Controls.Add(CreateDrinkRow("Nước ngọt", ToMoneyCompact(_pricing.DrinkSoftPrice),
                () => Math.Max(0, _savedSoftDrinkCount + _pendingSoftDrinkCount),
                v =>
                {
                    int target = Math.Max(0, v);
                    _pendingSoftDrinkCount = target - _savedSoftDrinkCount;
                    RefreshTotals();
                },
                out lblPendingDrink), 0, 0);
            drinkRows.Controls.Add(CreateDivider(0, 2), 0, 1);
            drinkRows.Controls.Add(CreateDrinkRow("Nước suối", ToMoneyCompact(_pricing.DrinkWaterPrice),
                () => Math.Max(0, _savedWaterBottleCount + _pendingWaterBottleCount),
                v =>
                {
                    int target = Math.Max(0, v);
                    _pendingWaterBottleCount = target - _savedWaterBottleCount;
                    RefreshTotals();
                },
                out lblPendingWater), 0, 2);
            drinkRows.Controls.Add(CreateDivider(0, 2), 0, 3);

            // Hide drink subtotal line as requested.
            lblDrinkSubtotal = null;
            body.Controls.Add(drinkRows, 0, 5);

            card.Controls.Add(body);
            return card;
        }

        private Control BuildSummaryCard()
        {
            var card = CreateCard();
            card.Padding = new Padding(10, 8, 10, 8);
            card.Margin = new Padding(0);
            card.MinimumSize = new Size(320, 0);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 11
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            body.Controls.Add(CreateCardTitle("Tổng kết thanh toán", null), 0, 0);

            var dueBox = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(230, 239, 238),
                Margin = new Padding(0, 4, 0, 6),
                Padding = new Padding(10, 8, 10, 6)
            };
            dueBox.Paint += (s, e) =>
            {
                var rect = dueBox.ClientRectangle;
                rect.Inflate(-1, -1);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = RoundedRect(rect, 12))
                using (var pen = new Pen(Color.FromArgb(189, 218, 214)))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            ApplyRoundedRegion(dueBox, 12);
            var lblDueTitle = new Label
            {
                Text = "CẦN THANH TOÁN",
                AutoSize = true,
                ForeColor = Color.FromArgb(85, 133, 131),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 2)
            };
            lblDueValue = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.FromArgb(194, 68, 68),
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                Margin = new Padding(0)
            };
            var dueStack = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            dueStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dueStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dueStack.Controls.Add(lblDueTitle, 0, 0);
            dueStack.Controls.Add(lblDueValue, 0, 1);
            dueBox.Controls.Add(dueStack);
            body.Controls.Add(dueBox, 0, 1);

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 0)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            AddSummaryRow(grid, 0, "Tiền phòng:", out lblRoomChargeValue);
            AddSummaryRow(grid, 1, "Nước:", out lblDrinkChargeValue);
            AddSummaryRow(grid, 2, "Phụ thu:", out lblLateFeeValue);
            body.Controls.Add(grid, 0, 2);

            lblRoomChargeDetail = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 8.8F),
                Margin = new Padding(0, 2, 0, 0)
            };
            body.Controls.Add(lblRoomChargeDetail, 0, 3);

            lblSurchargeReasonValue = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 8.8F),
                Margin = new Padding(0, 2, 0, 0)
            };
            body.Controls.Add(lblSurchargeReasonValue, 0, 4);

            body.Controls.Add(CreateDivider(4, 4), 0, 5);

            var totalRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 0)
            };
            totalRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            totalRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            var totalLabel = new Label
            {
                Text = "Tổng cộng:",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Margin = new Padding(0)
            };
            lblTotalChargeValue = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            totalRow.Controls.Add(totalLabel, 0, 0);
            totalRow.Controls.Add(lblTotalChargeValue, 1, 0);
            body.Controls.Add(totalRow, 0, 6);

            var collectedRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 4, 0, 0)
            };
            collectedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            collectedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            var collectedLabel = new Label
            {
                Text = "Đã thu:",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0)
            };
            lblCollectedValue = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            collectedRow.Controls.Add(collectedLabel, 0, 0);
            collectedRow.Controls.Add(lblCollectedValue, 1, 0);
            body.Controls.Add(collectedRow, 0, 7);

            var suggestionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 0),
                Padding = new Padding(0)
            };
            var btnSuggestRoomOnly = BuildActionButton("Thu tiền phòng", Color.White, _brandA, _cardBorder);
            btnSuggestRoomOnly.AutoSize = true;
            btnSuggestRoomOnly.Height = 30;
            btnSuggestRoomOnly.Padding = new Padding(8, 0, 8, 0);
            btnSuggestRoomOnly.Margin = new Padding(0, 0, 8, 0);
            btnSuggestRoomOnly.Click += (s, e) => ApplyCollectedSuggestion(includeDrinks: false);

            var btnSuggestFull = BuildActionButton("Thu đầy đủ", _success, Color.White, _success);
            btnSuggestFull.AutoSize = true;
            btnSuggestFull.Height = 30;
            btnSuggestFull.Padding = new Padding(10, 0, 10, 0);
            btnSuggestFull.Margin = new Padding(0);
            btnSuggestFull.Click += (s, e) => ApplyCollectedSuggestion(includeDrinks: true);

            suggestionRow.Controls.Add(btnSuggestRoomOnly);
            suggestionRow.Controls.Add(btnSuggestFull);
            body.Controls.Add(suggestionRow, 0, 8);

            var actionWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(0)
            };

            var actionRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            btnCancel = BuildActionButton("Hủy", Color.White, _textPrimary, _cardBorder);
            btnCancel.Margin = new Padding(0, 0, 8, 0);
            btnCancel.Dock = DockStyle.Fill;
            btnCancel.Click += (s, e) => HandleCancelRequest();

            btnSave = BuildActionButton("Lưu", _brandA, Color.White, _brandA);
            btnSave.Margin = new Padding(0, 0, 8, 0);
            btnSave.Dock = DockStyle.Fill;
            btnSave.Click += BtnSave_Click;

            btnPay = BuildActionButton("Trả phòng", _success, Color.White, _success);
            btnPay.Dock = DockStyle.Fill;
            btnPay.Click += BtnPay_Click;

            actionRow.Controls.Add(btnCancel, 0, 0);
            actionRow.Controls.Add(btnSave, 1, 0);
            actionRow.Controls.Add(btnPay, 2, 0);
            actionWrap.Controls.Add(actionRow);
            body.Controls.Add(actionWrap, 0, 9);

            var cancelRoomWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(0),
                Margin = new Padding(0, 6, 0, 0)
            };
            btnCancelRoom = BuildActionButton("Hủy phòng về trống", Color.White, _danger, Color.FromArgb(230, 179, 179));
            btnCancelRoom.Dock = DockStyle.Fill;
            btnCancelRoom.Click += BtnCancelRoom_Click;
            cancelRoomWrap.Controls.Add(btnCancelRoom);
            body.Controls.Add(cancelRoomWrap, 0, 10);

            card.Controls.Add(body);
            return card;
        }

        private decimal GetDefaultNightRateByRoomType()
        {
            try
            {
                decimal fromDb = _bookingDal.GetDonGiaNgayByPhong(_room.PhongID);
                if (fromDb > 0m) return fromDb;
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Cannot read default nightly rate from DB.", new Dictionary<string, object>
                {
                    ["RoomId"] = _room == null ? 0 : _room.PhongID,
                    ["Error"] = ex.Message
                });
            }

            return _pricingService.GetDefaultNightlyRate(_room.LoaiPhongID);
        }

        private decimal GetDefaultDailyRateByRoomType()
        {
            return _pricingService.GetDefaultDailyRate(_room.LoaiPhongID);
        }

        private void ApplyCollectedSuggestion(bool includeDrinks)
        {
            DateTime now = DateTime.Now;
            DateTime start = _room.ThoiGianBatDau ?? now;
            if (start > now) start = now;
            int roomType = _room.LoaiPhongID == 2 ? 2 : 1;
            var breakdown = _pricingService.CalculateOvernightChargeBreakdown(start, Math.Max(1, _nightCount), roomType, _nightlyRate, now, _dailyRate);
            decimal roomCharge = breakdown.RoomBaseAmount;
            decimal drinks = _savedDrinkCharge
                           + _pendingSoftDrinkCount * _pricing.DrinkSoftPrice
                           + _pendingWaterBottleCount * _pricing.DrinkWaterPrice;
            _savedCollectedAmount = includeDrinks
                ? (roomCharge + drinks + breakdown.LateFeeAmount)
                : roomCharge;
            RefreshTotals();
        }

        private void LoadExtrasFromDatabase()
        {
            using (var perf = PerformanceTracker.Measure("OvernightCheckout.LoadExtras", new Dictionary<string, object>
            {
                ["BookingId"] = _bookingId
            }))
            {
                var extras = _bookingDal.GetBookingExtras(_bookingId);
                ApplySavedExtras(extras);

                perf.AddContext("SoftDrinkQty", _savedSoftDrinkCount);
                perf.AddContext("WaterQty", _savedWaterBottleCount);
            }
        }

        private void ApplySavedExtras(IEnumerable<BookingDAL.BookingExtraRecord> extras)
        {
            _savedSoftDrinkCount = 0;
            _savedWaterBottleCount = 0;
            _savedDrinkCharge = 0m;
            if (extras == null) return;

            foreach (var line in extras)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.ItemCode)) continue;
                string code = line.ItemCode.Trim().ToUpperInvariant();
                if (code == "NN") _savedSoftDrinkCount = Math.Max(0, line.Qty);
                else if (code == "NS") _savedWaterBottleCount = Math.Max(0, line.Qty);
                _savedDrinkCharge += Math.Max(0m, line.Amount);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            SetActionButtonsEnabled(false);
            UseWaitCursor = true;
            try
            {
                await UiExceptionHandler.RunAsync(this, "OvernightCheckout.SaveProgress", async () =>
                {
                    using (var perf = PerformanceTracker.Measure("OvernightCheckout.SaveProgress", new Dictionary<string, object>
                    {
                        ["BookingId"] = _bookingId
                    }))
                    {
                        if (_bookingId <= 0)
                            throw new DomainException("Phiên đặt phòng chưa được khởi tạo. Vui lòng tải lại màn hình.");
                        string guestName = GetGuestNameInput();
                        _room.TenKhachHienThi = guestName;

                        int finalDrink = Math.Max(0, _savedSoftDrinkCount + _pendingSoftDrinkCount);
                        int finalWater = Math.Max(0, _savedWaterBottleCount + _pendingWaterBottleCount);

                        DateTime startTime = _room.ThoiGianBatDau ?? DateTime.Now;
                        var request = new CheckoutService.SaveOvernightRequest
                        {
                            BookingId = _bookingId,
                            RoomId = _room.PhongID,
                            StartTime = startTime,
                            GuestDisplayName = guestName,
                            NightCount = Math.Max(1, _nightCount),
                            NightlyRate = _nightlyRate,
                            SoftDrinkQty = finalDrink,
                            WaterBottleQty = finalWater,
                            SoftDrinkUnitPrice = _pricing.DrinkSoftPrice,
                            WaterBottleUnitPrice = _pricing.DrinkWaterPrice,
                            TargetCollectedAmount = _savedCollectedAmount
                        };
                        var result = await Task.Run(() => _checkoutService.SaveOvernight(request)).ConfigureAwait(true);

                        _pendingSoftDrinkCount = 0;
                        _pendingWaterBottleCount = 0;
                        _savedCollectedAmount = result.PaidAmountAfterOperation;
                        LoadExtrasFromDatabase();

                        _room.TrangThai = 1;
                        _room.KieuThue = 1;
                        _room.ThoiGianBatDau = startTime;

                        Saved?.Invoke(this, EventArgs.Empty);
                        RefreshTotals();
                        perf.AddContext("NightCount", _nightCount);
                        perf.AddContext("SoftDrinkQty", _savedSoftDrinkCount);
                        perf.AddContext("WaterQty", _savedWaterBottleCount);
                    }
                }).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
                SetActionButtonsEnabled(true);
            }
        }

        private async void BtnPay_Click(object sender, EventArgs e)
        {
            SetActionButtonsEnabled(false);
            UseWaitCursor = true;
            try
            {
                await UiExceptionHandler.RunAsync(this, "OvernightCheckout.Pay", async () =>
                {
                    using (var perf = PerformanceTracker.Measure("OvernightCheckout.Pay", new Dictionary<string, object>
                    {
                        ["BookingId"] = _bookingId
                    }))
                    {
                        if (_bookingId <= 0)
                            throw new DomainException("Phiên đặt phòng chưa được khởi tạo. Vui lòng tải lại màn hình.");
                        string guestName = GetGuestNameInput();
                        _room.TenKhachHienThi = guestName;

                        int finalDrink = Math.Max(0, _savedSoftDrinkCount + _pendingSoftDrinkCount);
                        int finalWater = Math.Max(0, _savedWaterBottleCount + _pendingWaterBottleCount);
                        DateTime startTime = _room.ThoiGianBatDau ?? DateTime.Now;

                        decimal totalCharge = CalculateOvernightCharge(_nightCount)
                                            + finalDrink * _pricing.DrinkSoftPrice
                                            + finalWater * _pricing.DrinkWaterPrice;
                        decimal dueAmount = Math.Max(0m, totalCharge - _savedCollectedAmount);

                        string message = "Xác nhận trả phòng " + _room.MaPhong +
                                         "\nTổng tiền: " + ToMoney(totalCharge) +
                                         "\nCần thu: " + ToMoney(dueAmount);

                        var confirm = MessageBox.Show(message, "Xác nhận trả phòng", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm != DialogResult.Yes) return;

                        var request = new CheckoutService.PayOvernightRequest
                        {
                            BookingId = _bookingId,
                            RoomId = _room.PhongID,
                            PaidAt = DateTime.Now,
                            StartTime = startTime,
                            NightCount = Math.Max(1, _nightCount),
                            NightlyRate = _nightlyRate,
                            SoftDrinkQty = finalDrink,
                            WaterBottleQty = finalWater,
                            SoftDrinkUnitPrice = _pricing.DrinkSoftPrice,
                            WaterBottleUnitPrice = _pricing.DrinkWaterPrice,
                            TargetCollectedAmount = _savedCollectedAmount,
                            GuestDisplayName = guestName,
                            TotalCharge = totalCharge
                        };
                        var result = await Task.Run(() => _checkoutService.PayOvernight(request)).ConfigureAwait(true);

                        _savedSoftDrinkCount = finalDrink;
                        _savedWaterBottleCount = finalWater;
                        _pendingSoftDrinkCount = 0;
                        _pendingWaterBottleCount = 0;
                        _savedCollectedAmount = result.PaidAmountAfterOperation;

                        _room.TrangThai = 2;
                        _room.ThoiGianBatDau = null;
                        _room.KieuThue = null;
                        _room.TenKhachHienThi = null;

                        PaymentCompleted?.Invoke(this, EventArgs.Empty);
                        BackRequested?.Invoke(this, EventArgs.Empty);
                        perf.AddContext("NightCount", _nightCount);
                        perf.AddContext("TotalCharge", totalCharge);
                        perf.AddContext("DueAmount", dueAmount);
                    }
                }).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
                SetActionButtonsEnabled(true);
            }
        }

        private async void BtnCancelRoom_Click(object sender, EventArgs e)
        {
            if (_bookingId <= 0)
            {
                MessageBox.Show("Không xác định được booking hiện tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var confirm = MessageBox.Show(
                "Xác nhận hủy phòng " + _room.MaPhong + " và đưa về trạng thái trống?",
                "Hủy phòng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
                return;

            SetActionButtonsEnabled(false);
            UseWaitCursor = true;
            try
            {
                await UiExceptionHandler.RunAsync(this, "OvernightCheckout.CancelRoom", async () =>
                {
                    using (var perf = PerformanceTracker.Measure("OvernightCheckout.CancelRoom", new Dictionary<string, object>
                    {
                        ["BookingId"] = _bookingId,
                        ["RoomId"] = _room.PhongID
                    }))
                    {
                        var request = new CheckoutService.CancelOvernightRequest
                        {
                            BookingId = _bookingId,
                            RoomId = _room.PhongID,
                            CancelledAt = DateTime.Now
                        };
                        await Task.Run(() => _checkoutService.CancelOvernight(request)).ConfigureAwait(true);

                        _savedSoftDrinkCount = 0;
                        _savedWaterBottleCount = 0;
                        _pendingSoftDrinkCount = 0;
                        _pendingWaterBottleCount = 0;
                        _savedDrinkCharge = 0m;

                        _room.TrangThai = (int)RoomStatus.Trong;
                        _room.KieuThue = null;
                        _room.ThoiGianBatDau = null;
                        _room.TenKhachHienThi = null;

                        RoomCancelled?.Invoke(this, EventArgs.Empty);
                        BackRequested?.Invoke(this, EventArgs.Empty);
                        perf.AddContext("Action", "CancelledToEmpty");
                    }
                }).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
                SetActionButtonsEnabled(true);
            }
        }

        private void HandleCancelRequest()
        {
            bool hasUnsaved = _pendingSoftDrinkCount != 0 || _pendingWaterBottleCount != 0;
            if (!hasUnsaved)
            {
                BackRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            var confirm = MessageBox.Show(
                "Bạn đang có nước chưa lưu. Xác nhận hủy?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
                BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SyncCheckinInputFromRoom()
        {
            if (dtpCheckinTime == null) return;

            DateTime start = _room.ThoiGianBatDau ?? DateTime.Now;
            DateTime now = DateTime.Now;
            if (start > now) start = now;

            _isSyncingCheckinInput = true;
            try
            {
                dtpCheckinTime.Value = start;
            }
            finally
            {
                _isSyncingCheckinInput = false;
            }

            UpdateCheckinLabel(start);
        }

        private void HandleCheckinTimeChanged()
        {
            if (_isSyncingCheckinInput || dtpCheckinTime == null) return;

            DateTime selected = dtpCheckinTime.Value;
            DateTime now = DateTime.Now;
            if (selected > now)
            {
                selected = now;
                _isSyncingCheckinInput = true;
                try
                {
                    dtpCheckinTime.Value = selected;
                }
                finally
                {
                    _isSyncingCheckinInput = false;
                }
            }

            _room.ThoiGianBatDau = selected;
            UpdateCheckinLabel(selected);
            RefreshTotals();
        }

        private void UpdateCheckinLabel(DateTime startTime)
        {
            if (lblCheckin != null)
                lblCheckin.Text = "Check-in: " + startTime.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void RefreshTotals()
        {
            DateTime now = DateTime.Now;
            DateTime start = _room.ThoiGianBatDau ?? now;
            if (start > now) start = now;
            UpdateCheckinLabel(start);

            if (dtpCheckinTime != null && !_isSyncingCheckinInput && !dtpCheckinTime.Focused)
            {
                _isSyncingCheckinInput = true;
                try
                {
                    if (dtpCheckinTime.Value != start)
                        dtpCheckinTime.Value = start;
                }
                finally
                {
                    _isSyncingCheckinInput = false;
                }
            }

            if (lblNightCount != null) lblNightCount.Text = _nightCount.ToString();
            if (lblPendingDrink != null) lblPendingDrink.Text = (_savedSoftDrinkCount + _pendingSoftDrinkCount).ToString();
            if (lblPendingWater != null) lblPendingWater.Text = (_savedWaterBottleCount + _pendingWaterBottleCount).ToString();

            int roomType = _room.LoaiPhongID == 2 ? 2 : 1;
            var breakdown = _pricingService.CalculateOvernightChargeBreakdown(start, Math.Max(1, _nightCount), roomType, _nightlyRate, now, _dailyRate);
            decimal roomCharge = breakdown.RoomBaseAmount;
            decimal pendingDrinkCharge = _pendingSoftDrinkCount * _pricing.DrinkSoftPrice
                                       + _pendingWaterBottleCount * _pricing.DrinkWaterPrice;
            decimal drinkCharge = _savedDrinkCharge + pendingDrinkCharge;
            decimal lateFeeCharge = breakdown.LateFeeAmount;
            decimal total = roomCharge + drinkCharge + lateFeeCharge;
            decimal due = Math.Max(0m, total - _savedCollectedAmount);
            bool isDouble = _room.LoaiPhongID == 2;
            decimal nightRate = _nightlyRate > 0m
                ? _nightlyRate
                : (isDouble ? _pricing.DefaultNightlyDouble : _pricing.DefaultNightlySingle);
            decimal dayRate = _dailyRate > 0m
                ? _dailyRate
                : (isDouble ? _pricing.DefaultDailyDouble : _pricing.DefaultDailySingle);
            if (lblRateFrameInline != null)
            {
                string firstType = breakdown.FirstSegmentIsNight ? "Phòng đêm" : "Phòng ngày";
                lblRateFrameInline.Text = firstType + " | Đêm " + ToMoneyCompact(nightRate) + " | Ngày " + ToMoneyCompact(dayRate);
            }
            if (lblNightlyRate != null)
                lblNightlyRate.Text = ToMoneyCompact(Math.Max(MinNightlyRate, _nightlyRate));
            if (lblDailyRate != null)
                lblDailyRate.Text = ToMoneyCompact(Math.Max(MinDailyRate, _dailyRate));
            if (lblRoomChargeDetail != null)
                lblRoomChargeDetail.Text = "Tiền phòng gồm: Đêm " + ToMoneyCompact(breakdown.NightAmount) + " | Ngày " + ToMoneyCompact(breakdown.DayAmount);

            if (lblRoomChargeValue != null) lblRoomChargeValue.Text = ToMoneyCompact(roomCharge);
            if (lblDrinkChargeValue != null) lblDrinkChargeValue.Text = ToMoneyCompact(drinkCharge);
            if (lblLateFeeValue != null) lblLateFeeValue.Text = ToMoneyCompact(lateFeeCharge);
            if (lblTotalChargeValue != null) lblTotalChargeValue.Text = ToMoneyCompact(total);
            if (lblCollectedValue != null) lblCollectedValue.Text = ToMoneyCompact(_savedCollectedAmount);
            if (lblDueValue != null) lblDueValue.Text = ToMoney(due);
            if (lblSurchargeReasonValue != null)
                lblSurchargeReasonValue.Text = BuildLateFeeReasonText(breakdown.LateFeeAmount);

            if (btnPay != null) btnPay.Text = "Trả phòng";
        }

        private void UpdateResponsiveLayout()
        {
            if (_contentGrid == null) return;

            bool compact = Width < 980;
            if (compact == _isCompactLayout) return;
            _isCompactLayout = compact;

            _contentGrid.Controls.Clear();
            _contentGrid.RowStyles.Clear();
            _contentGrid.ColumnStyles.Clear();

            if (compact)
            {
                _contentGrid.ColumnCount = 1;
                _contentGrid.RowCount = 2;
                _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                _contentGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _contentGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                _contentLeftColumn.Margin = new Padding(0, 0, 0, 8);
                _contentGrid.Controls.Add(_contentLeftColumn, 0, 0);
                _contentGrid.Controls.Add(_contentRightColumn, 0, 1);
            }
            else
            {
                _contentGrid.ColumnCount = 2;
                _contentGrid.RowCount = 1;
                _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
                _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
                _contentGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                _contentLeftColumn.Margin = new Padding(0, 0, 8, 0);
                _contentGrid.Controls.Add(_contentLeftColumn, 0, 0);
                _contentGrid.Controls.Add(_contentRightColumn, 1, 0);
            }
        }

        private decimal CalculateOvernightCharge(int nights)
        {
            if (nights <= 0) return 0m;
            DateTime start = _room.ThoiGianBatDau ?? DateTime.Now;
            int roomType = _room.LoaiPhongID == 2 ? 2 : 1;
            return _pricingService.CalculateOvernightCharge(start, nights, roomType, _nightlyRate, DateTime.Now, _dailyRate);
        }

        private string BuildLateFeeReasonText(decimal lateFeeAmount)
        {
            if (lateFeeAmount <= 0m) return string.Empty;
            return "Lý do phụ thu: Trả trễ sau " + _pricing.OvernightCheckoutHour + "h (+" + ToMoneyCompact(lateFeeAmount) + ")";
        }

        private string ResolveGuestNameForForm(BookingDAL.StayInfoRecord stayInfo)
        {
            string name = (_room.TenKhachHienThi ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(name)) return name;

            string stayGuest = ExtractPrimaryGuestName(stayInfo == null ? null : stayInfo.GuestListJson);
            if (!string.IsNullOrWhiteSpace(stayGuest)) return stayGuest;
            return string.Empty;
        }

        private static string ExtractPrimaryGuestName(string guestListRaw)
        {
            if (string.IsNullOrWhiteSpace(guestListRaw)) return string.Empty;

            string[] lines = guestListRaw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return string.Empty;

            string first = (lines[0] ?? string.Empty).Trim();
            if (first.Length == 0) return string.Empty;

            int sepIndex = first.IndexOf(" - ", StringComparison.Ordinal);
            return sepIndex > 0 ? first.Substring(0, sepIndex).Trim() : first;
        }

        private string GetGuestNameInput()
        {
            if (txtGuestName == null) return string.Empty;
            return (txtGuestName.Text ?? string.Empty).Trim();
        }

        private Panel CreateCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 0, 8)
            };
            card.Paint += (s, e) =>
            {
                var rect = card.ClientRectangle;
                rect.Inflate(-1, -1);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = RoundedRect(rect, 8))
                using (var pen = new Pen(_cardBorder))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            ApplyRoundedRegion(card, 8);
            return card;
        }

        private Control CreateCardTitle(string title, string subtitle)
        {
            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            stack.Controls.Add(new Label
            {
                Text = title,
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 3)
            });
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                stack.Controls.Add(new Label
                {
                    Text = subtitle,
                    AutoSize = true,
                    ForeColor = _textSecondary,
                    Font = new Font("Segoe UI", 9F),
                    Margin = new Padding(0)
                });
            }
            return stack;
        }

        private void AddSummaryRow(TableLayoutPanel grid, int row, string label, out Label valueLabel)
        {
            if (grid.RowCount <= row) grid.RowCount = row + 1;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, row == 0 ? 0 : 4, 0, 0)
            };
            valueLabel = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, row == 0 ? 0 : 4, 0, 0)
            };
            grid.Controls.Add(lbl, 0, row);
            grid.Controls.Add(valueLabel, 1, row);
        }

        private Control CreateDrinkRow(string name, string price, Func<int> getValue, Action<int> setValue, out Label valueLabel)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Margin = new Padding(0, 1, 0, 1)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            row.Controls.Add(new Label
            {
                Text = name,
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 2)
            }, 0, 0);
            row.Controls.Add(new Label
            {
                Text = price,
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI", 10.5F),
                Margin = new Padding(0, 2, 0, 2)
            }, 1, 0);

            var stepper = CreateStepper(getValue, setValue, _danger, _success, out valueLabel);
            stepper.Dock = DockStyle.None;
            stepper.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            row.Controls.Add(stepper, 2, 0);
            return row;
        }

        private Panel CreateInputFrame(Control innerControl, int height)
        {
            var frame = new Panel
            {
                Dock = DockStyle.Top,
                Height = Math.Max(30, height),
                BackColor = Color.FromArgb(248, 250, 254),
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(0)
            };

            if (innerControl != null)
            {
                innerControl.Dock = DockStyle.Fill;
                frame.Controls.Add(innerControl);
            }

            frame.Paint += (s, e) =>
            {
                var rect = frame.ClientRectangle;
                rect.Inflate(-1, -1);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = RoundedRect(rect, 6))
                using (var pen = new Pen(_cardBorder))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            ApplyRoundedRegion(frame, 6);
            return frame;
        }

        private Panel CreateDivider(int marginTop, int marginBottom)
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = _cardBorder,
                Margin = new Padding(0, marginTop, 0, marginBottom)
            };
        }

        private Control CreateStepper(Func<int> getValue, Action<int> setValue, Color minusColor, Color plusColor, out Label valueLabel)
        {
            var host = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Dock = DockStyle.Right
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var btnMinus = BuildSquareButton("−", minusColor);
            btnMinus.Click += (s, e) => setValue(Math.Max(0, getValue() - 1));

            valueLabel = new Label
            {
                Text = getValue().ToString(),
                AutoSize = false,
                Width = 64,
                Height = 34,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(250, 251, 253),
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Margin = new Padding(0)
            };
            valueLabel.Paint += (s, e) =>
            {
                if (!(s is Label label)) return;
                using (var pen = new Pen(_cardBorder))
                    e.Graphics.DrawRectangle(pen, 0, 0, label.Width - 1, label.Height - 1);
            };
            valueLabel.Cursor = Cursors.IBeam;
            valueLabel.DoubleClick += (s, e) =>
            {
                int current = Math.Max(0, getValue());
                int edited = ShowQuantityEditor(current);
                setValue(Math.Max(0, edited));
            };

            var btnPlus = BuildSquareButton("+", plusColor);
            btnPlus.Click += (s, e) => setValue(Math.Min(999, getValue() + 1));

            host.Controls.Add(btnMinus, 0, 0);
            host.Controls.Add(valueLabel, 1, 0);
            host.Controls.Add(btnPlus, 2, 0);
            return host;
        }

        private int ShowQuantityEditor(int currentValue)
        {
            int initial = Math.Max(0, currentValue);
            using (var dialog = new Form())
            {
                dialog.Text = "Nhập số lượng";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new Size(260, 112);

                var lbl = new Label
                {
                    Text = "Số lượng:",
                    AutoSize = true,
                    Location = new Point(14, 16),
                    Font = new Font("Segoe UI", 9.5F)
                };

                var nud = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 99999,
                    Value = Math.Min(99999, initial),
                    ThousandsSeparator = true,
                    Location = new Point(14, 38),
                    Size = new Size(232, 25),
                    Font = new Font("Segoe UI", 9.5F)
                };

                var btnOk = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(90, 74),
                    Size = new Size(74, 28)
                };
                var btnCancelLocal = new Button
                {
                    Text = "Hủy",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(172, 74),
                    Size = new Size(74, 28)
                };

                dialog.Controls.Add(lbl);
                dialog.Controls.Add(nud);
                dialog.Controls.Add(btnOk);
                dialog.Controls.Add(btnCancelLocal);
                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancelLocal;

                var result = dialog.ShowDialog(this);
                if (result != DialogResult.OK)
                    return initial;

                return Convert.ToInt32(nud.Value);
            }
        }

        private Control CreateMoneyStepper(
            Func<decimal> getValue,
            Action<decimal> setValue,
            decimal step,
            decimal minValue,
            Color minusColor,
            Color plusColor,
            out Label valueLabel)
        {
            var host = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Dock = DockStyle.Right
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            decimal safeStep = step <= 0m ? 10000m : step;
            decimal safeMin = minValue < 0m ? 0m : minValue;

            var btnMinus = BuildSquareButton("−", minusColor);
            btnMinus.Click += (s, e) =>
            {
                decimal next = getValue() - safeStep;
                if (next < safeMin) next = safeMin;
                setValue(next);
            };

            valueLabel = new Label
            {
                Text = ToMoneyCompact(Math.Max(safeMin, getValue())),
                AutoSize = false,
                Width = 104,
                Height = 34,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(250, 251, 253),
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 9.8F, FontStyle.Bold),
                Margin = new Padding(0)
            };
            valueLabel.Paint += (s, e) =>
            {
                if (!(s is Label label)) return;
                using (var pen = new Pen(_cardBorder))
                    e.Graphics.DrawRectangle(pen, 0, 0, label.Width - 1, label.Height - 1);
            };

            var btnPlus = BuildSquareButton("+", plusColor);
            btnPlus.Click += (s, e) => setValue(Math.Max(safeMin, getValue() + safeStep));

            host.Controls.Add(btnMinus, 0, 0);
            host.Controls.Add(valueLabel, 1, 0);
            host.Controls.Add(btnPlus, 2, 0);
            return host;
        }

        private Button BuildSquareButton(string text, Color back)
        {
            var btn = new Button
            {
                Text = text,
                Width = 36,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = ControlPaint.Dark(back);
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(back);
            ApplyRoundedRegion(btn, 5);
            return btn;
        }

        private Button BuildActionButton(string text, Color back, Color fore, Color border)
        {
            var btn = new Button
            {
                Text = text,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0),
                Padding = new Padding(6, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = back == Color.White ? 1 : 0;
            btn.FlatAppearance.BorderColor = border;
            btn.FlatAppearance.MouseOverBackColor = back == Color.White ? Color.FromArgb(243, 246, 252) : ControlPaint.Light(back);
            btn.FlatAppearance.MouseDownBackColor = back == Color.White ? Color.FromArgb(236, 241, 250) : ControlPaint.Dark(back);
            ApplyRoundedRegion(btn, 5);
            return btn;
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            if (btnCancel != null && !btnCancel.IsDisposed) btnCancel.Enabled = enabled;
            if (btnSave != null && !btnSave.IsDisposed) btnSave.Enabled = enabled;
            if (btnPay != null && !btnPay.IsDisposed) btnPay.Enabled = enabled;
            if (btnCancelRoom != null && !btnCancelRoom.IsDisposed) btnCancelRoom.Enabled = enabled;
        }

        private void ApplyRoundedRegion(Control control, int radius)
        {
            Action updateRegion = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                var rect = new Rectangle(0, 0, control.Width, control.Height);
                using (var path = RoundedRect(rect, radius))
                {
                    var old = control.Region;
                    control.Region = new Region(path);
                    if (old != null) old.Dispose();
                }
            };
            control.SizeChanged += (s, e) => updateRegion();
            updateRegion();
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int r = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
            int d = r * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static string ToMoney(decimal amount)
        {
            return amount.ToString("N0") + " đ";
        }

        private static string ToMoneyCompact(decimal amount)
        {
            return amount.ToString("N0") + "đ";
        }
    }
}
