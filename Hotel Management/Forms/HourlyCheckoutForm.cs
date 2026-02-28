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
    public class HourlyCheckoutForm : Form
    {
        // ===== Theme =====
        private readonly Color _pageBackground = Color.FromArgb(242, 245, 251);
        private readonly Color _cardBorder = Color.FromArgb(212, 221, 235);

        private readonly Color _textPrimary = Color.FromArgb(35, 45, 73);
        private readonly Color _textSecondary = Color.FromArgb(98, 111, 141);

        private readonly Color _brandA = Color.FromArgb(43, 67, 188);
        private readonly Color _brandB = Color.FromArgb(88, 116, 245);

        private readonly Color _success = Color.FromArgb(62, 159, 145);
        private readonly Color _danger = Color.FromArgb(214, 82, 82);

        // ===== Data =====
        private readonly Room _room;
        private readonly BookingDAL _bookingDal = new BookingDAL();
        private readonly PricingService _pricingService = PricingService.Instance;
        private readonly CheckoutService _checkoutService = new CheckoutService();
        private int _bookingId;
        private PricingService.PricingConfig _pricing = PricingService.GetDefaultConfig();

        private int _savedSoftDrinkCount;
        private int _savedWaterBottleCount;
        private decimal _savedDrinkCharge;
        private decimal _savedCollectedAmount;

        private int _pendingSoftDrinkCount;
        private int _pendingWaterBottleCount;

        // ===== UI refs =====
        private Label lblTitleRoom;
        private Label lblChipRoom;
        private Label lblCheckin;

        private Label lblStartTime;
        private Label lblStayHours;
        private Label lblRateFrameInline;
        private Label lblHour1Rate;
        private Label lblNextHourRate;
        private DateTimePicker dtpCheckinTime;

        private Label lblSavedDrink;
        private Label lblSavedWater;

        private Label lblPendingDrink;
        private Label lblPendingWater;
        private Label lblDrinkSubtotal;

        private Label lblRoomChargeValue;
        private Label lblDrinkChargeValue;
        private Label lblLateFeeValue;
        private Label lblSurchargeReasonValue;
        private Label lblTotalChargeValue;
        private Label lblCollectedValue;
        private Label lblDueValue;

        private Button btnCloseTop;
        private Button btnSave;
        private Button btnPay;
        private Button btnCancel;
        private Button btnCancelRoom;

        private TableLayoutPanel _contentGrid;
        private TableLayoutPanel _contentLeftColumn;
        private TableLayoutPanel _contentRightColumn;
        private bool _isCompactLayout;
        private Timer _stayTimer;
        private bool _isSyncingCheckinInput;
        private decimal _hour1Rate;
        private decimal _nextHourRate;
        private bool _hasCustomHourlyRate;
        private const decimal HourlyRateStep = 10000m;
        private const decimal MinHourlyRate = 10000m;

        public event EventHandler BackRequested;
        public event EventHandler Saved;
        public event EventHandler PaymentCompleted;
        public event EventHandler RoomCancelled;

        public HourlyCheckoutForm(Room room)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
            _pricing = _pricingService.GetCurrentPricing();
            InitializeUi();
            Load += HourlyCheckoutForm_Load;
            Disposed += HourlyCheckoutForm_Disposed;
        }

        private async void HourlyCheckoutForm_Load(object sender, EventArgs e)
        {
            bool initialized = false;
            await UiExceptionHandler.RunAsync(this, "HourlyCheckout.Load", async () =>
            {
                using (var perf = PerformanceTracker.Measure("HourlyCheckout.Load", new System.Collections.Generic.Dictionary<string, object>
                {
                    ["RoomId"] = _room.PhongID
                }))
                {
                    var loadData = await Task.Run(() => LoadHourlyCheckoutData()).ConfigureAwait(true);
                    _bookingId = loadData.BookingId;
                    ReloadPricingSettings(true);
                    _pricingService.PricingChanged += PricingService_PricingChanged;
                    ApplySavedExtras(loadData.Extras);
                    _savedCollectedAmount = loadData.PaidAmount;

                    _pendingSoftDrinkCount = 0;
                    _pendingWaterBottleCount = 0;

                    lblTitleRoom.Text = "Tính tiền phòng theo giờ - Phòng " + _room.MaPhong;
                    lblChipRoom.Text = "  Phòng " + _room.MaPhong + "  ";
                    lblCheckin.Text = "Check-in: " + (_room.ThoiGianBatDau ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss");
                    SyncCheckinInputFromRoom();

                    RefreshTotals();
                    EnsureStayTimer();
                    initialized = true;
                    perf.AddContext("BookingId", _bookingId);
                }
            }).ConfigureAwait(true);

            if (!initialized)
                BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void HourlyCheckoutForm_Disposed(object sender, EventArgs e)
        {
            _pricingService.PricingChanged -= PricingService_PricingChanged;
            if (_stayTimer == null) return;
            _stayTimer.Stop();
            _stayTimer.Dispose();
            _stayTimer = null;
        }

        private sealed class HourlyCheckoutLoadData
        {
            public int BookingId { get; set; }
            public List<BookingDAL.BookingExtraRecord> Extras { get; set; }
            public decimal PaidAmount { get; set; }
        }

        private HourlyCheckoutLoadData LoadHourlyCheckoutData()
        {
            int bookingId = _bookingDal.EnsureBookingForRoom(_room, 1);
            var extras = _bookingDal.GetBookingExtras(bookingId);
            decimal paidAmount = _bookingDal.GetPaidAmountByBooking(bookingId);
            return new HourlyCheckoutLoadData
            {
                BookingId = bookingId,
                Extras = extras,
                PaidAmount = paidAmount
            };
        }

        private void PricingService_PricingChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ReloadPricingSettings(false)));
                return;
            }

            ReloadPricingSettings(false);
        }

        private void ReloadPricingSettings(bool resetLocalHourlyRates = false)
        {
            _pricing = _pricingService.GetCurrentPricing();
            if (resetLocalHourlyRates || !_hasCustomHourlyRate || _hour1Rate <= 0m || _nextHourRate <= 0m)
            {
                ApplyDefaultHourlyRates();
            }
            RefreshTotals();
        }

        private void ApplyDefaultHourlyRates()
        {
            decimal defaultHour1 = _room.LoaiPhongID == 2 ? _pricing.HourlyDoubleHour1 : _pricing.HourlySingleHour1;
            decimal defaultNext = _room.LoaiPhongID == 2 ? _pricing.HourlyDoubleNextHour : _pricing.HourlySingleNextHour;
            _hour1Rate = NormalizeHourlyRate(defaultHour1);
            _nextHourRate = NormalizeHourlyRate(defaultNext);
            _hasCustomHourlyRate = false;
        }

        private void EnsureStayTimer()
        {
            if (_stayTimer != null) return;

            _stayTimer = new Timer { Interval = 1000 };
            _stayTimer.Tick += (s, e) =>
            {
                if (!IsDisposed && Visible)
                    RefreshTotals();
            };
            _stayTimer.Start();
        }

        // =========================
        // UI Composition
        // =========================
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
                BackColor = BackColor,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // Hero
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // Content

            var hero = BuildHero();
            var content = BuildContent();

            root.Controls.Add(hero, 0, 0);
            root.Controls.Add(content, 0, 1);

            Controls.Add(root);

            Resize += (s, e) => UpdateResponsiveLayout();
            UpdateResponsiveLayout();
        }

        private Control BuildTopBar()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(12, 10, 12, 8),
                Margin = new Padding(0)
            };

            panel.Paint += (s, e) =>
            {
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    panel.ClientRectangle, _brandA, _brandB, 0f))
                {
                    e.Graphics.FillRectangle(brush, panel.ClientRectangle);
                }
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var brand = new Label
            {
                Text = "Thanh Long Hotel",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 0)
            };

            btnCloseTop = new Button
            {
                Text = "Thoát",
                AutoSize = false,
                Width = 84,
                Height = 32,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 244, 251),
                ForeColor = Color.FromArgb(58, 66, 84),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnCloseTop.FlatAppearance.BorderSize = 1;
            btnCloseTop.FlatAppearance.BorderColor = Color.FromArgb(201, 212, 232);
            btnCloseTop.FlatAppearance.MouseOverBackColor = Color.FromArgb(246, 249, 253);
            btnCloseTop.FlatAppearance.MouseDownBackColor = Color.FromArgb(230, 236, 248);
            ApplyRoundedRegion(btnCloseTop, 6);
            btnCloseTop.Click += (s, e) => HandleCancelRequest();

            layout.Controls.Add(brand, 0, 0);
            layout.Controls.Add(btnCloseTop, 1, 0);

            panel.Controls.Add(layout);
            return panel;
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

            var title = new Label
            {
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            };
            lblTitleRoom = title;

            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            var chip = new Label
            {
                AutoSize = true,
                BackColor = Color.FromArgb(65, 161, 145),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 0, 8, 0)
            };
            chip.Paint += (s, e) =>
            {
                var rect = chip.ClientRectangle;
                rect.Inflate(-1, -1);
                using (var path = RoundedRect(rect, 14))
                using (var pen = new Pen(Color.FromArgb(55, 144, 129)))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            };
            ApplyRoundedRegion(chip, 12);
            lblChipRoom = chip;

            var checkin = new Label
            {
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 5, 0, 0)
            };
            lblCheckin = checkin;

            row.Controls.Add(chip);
            row.Controls.Add(checkin);

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
            heroLayout.Controls.Add(title, 0, 0);
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

        private Control BuildFooter()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = _pageBackground
            };

            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            btnCancel = BuildActionButton("Hủy", Color.White, _textSecondary, _cardBorder);
            btnCancel.Width = 90;
            btnCancel.Click += (s, e) => HandleCancelRequest();

            btnSave = BuildActionButton("Lưu", _brandA, Color.White, _brandA);
            btnSave.Width = 100;
            btnSave.Click += BtnSave_Click;

            btnPay = BuildActionButton("Thanh toán", _success, Color.White, _success);
            btnPay.Width = 170;
            btnPay.Click += BtnPay_Click;

            row.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 0);

            var right = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            right.Controls.Add(btnCancel);
            right.Controls.Add(btnSave);
            right.Controls.Add(btnPay);

            row.Controls.Add(right, 2, 0);

            panel.Controls.Add(row);
            return panel;
        }

        // =========================
        // Cards
        // =========================
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
                RowCount = 7
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            body.Controls.Add(CreateCardTitle("Thông tin lưu trú", null), 0, 0);

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
                Text = "Thời gian lưu trú:",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 2)
            };
            lblStayHours = new Label
            {
                Text = "",
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 0, 0, 2)
            };
            stayGrid.Controls.Add(lblStayLabel, 0, 0);
            stayGrid.Controls.Add(lblStayHours, 1, 0);

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

            var lblRateLabel = new Label
            {
                Text = "Khung giá:",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 1),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = _textSecondary
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

            stayGrid.Controls.Add(lblRateLabel, 0, 2);
            stayGrid.Controls.Add(lblRateFrameInline, 1, 2);

            var lblRateAdjustLabel = new Label
            {
                Text = "Điều chỉnh giá:",
                AutoSize = true,
                Margin = new Padding(0, 1, 0, 0),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = _textSecondary
            };
            var rateAdjustGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0)
            };
            rateAdjustGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rateAdjustGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rateAdjustGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rateAdjustGrid.Controls.Add(CreateHourlyRateEditor(
                "Giờ đầu",
                () => _hour1Rate,
                v =>
                {
                    _hour1Rate = NormalizeHourlyRate(v);
                    _hasCustomHourlyRate = true;
                    RefreshTotals();
                },
                out lblHour1Rate), 0, 0);
            rateAdjustGrid.Controls.Add(CreateHourlyRateEditor(
                "Giờ sau",
                () => _nextHourRate,
                v =>
                {
                    _nextHourRate = NormalizeHourlyRate(v);
                    _hasCustomHourlyRate = true;
                    RefreshTotals();
                },
                out lblNextHourRate), 0, 1);

            stayGrid.Controls.Add(lblRateAdjustLabel, 0, 3);
            stayGrid.Controls.Add(rateAdjustGrid, 1, 3);

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

        private Control BuildDrinkCard()
        {
            var card = CreateCard();

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 5
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            body.Controls.Add(CreateCardTitle("Nước sử dụng", "Bấm +/- để gọi thêm nước. Bấm Lưu để cập nhật."), 0, 0);

            // Rows (3 columns: name / price / stepper)
            var rows = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0, 12, 0, 0)
            };
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            // Row 0
            rows.Controls.Add(CreateDrinkNameLabel("Nước ngọt"), 0, 0);
            rows.Controls.Add(CreateDrinkPriceLabel(ToMoneyCompact(_pricing.DrinkSoftPrice)), 1, 0);

            lblSavedDrink = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.2F),
                Margin = new Padding(0, 2, 0, 0)
            };

            var stepperDrink = CreateStepper(
                getValue: () => _pendingSoftDrinkCount,
                setValue: v => { _pendingSoftDrinkCount = v; RefreshTotals(); },
                minusColor: _danger,
                plusColor: _success,
                out lblPendingDrink);

            var drinkRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            drinkRight.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            drinkRight.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            drinkRight.Controls.Add(stepperDrink, 0, 0);
            drinkRight.Controls.Add(lblSavedDrink, 0, 1);

            rows.Controls.Add(drinkRight, 2, 0);

            // Row 1
            rows.Controls.Add(CreateDrinkNameLabel("Nước suối"), 0, 1);
            rows.Controls.Add(CreateDrinkPriceLabel(ToMoneyCompact(_pricing.DrinkWaterPrice)), 1, 1);

            lblSavedWater = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.2F),
                Margin = new Padding(0, 2, 0, 0)
            };

            var stepperWater = CreateStepper(
                getValue: () => _pendingWaterBottleCount,
                setValue: v => { _pendingWaterBottleCount = v; RefreshTotals(); },
                minusColor: _danger,
                plusColor: _success,
                out lblPendingWater);

            var waterRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            waterRight.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            waterRight.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            waterRight.Controls.Add(stepperWater, 0, 0);
            waterRight.Controls.Add(lblSavedWater, 0, 1);

            rows.Controls.Add(waterRight, 2, 1);

            body.Controls.Add(rows, 0, 1);

            // Subtotal
            // Hide drink subtotal line as requested.
            lblDrinkSubtotal = null;

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
                RowCount = 10
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

            body.Controls.Add(CreateCardTitle("Tổng kết thanh toán", null), 0, 0);

            // Due highlight
            var dueBox = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.FromArgb(230, 239, 238),
                Margin = new Padding(0, 4, 0, 6),
                Padding = new Padding(10, 7, 10, 6)
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

            var dueStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            dueStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dueStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dueStack.Controls.Add(lblDueTitle, 0, 0);
            dueStack.Controls.Add(lblDueValue, 0, 1);

            dueBox.Controls.Add(dueStack);

            body.Controls.Add(dueBox, 0, 1);

            // Breakdown grid
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

            lblSurchargeReasonValue = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 8.8F),
                Margin = new Padding(0, 2, 0, 0)
            };
            body.Controls.Add(lblSurchargeReasonValue, 0, 3);

            var sep = CreateDivider(0, 8);
            body.Controls.Add(sep, 0, 4);

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
                Margin = new Padding(0, 0, 0, 0)
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
            body.Controls.Add(totalRow, 0, 5);

            var collectedRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 3, 0, 0)
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
            body.Controls.Add(collectedRow, 0, 6);

            var spacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 10,
                Margin = new Padding(0, 4, 0, 2),
                BackColor = Color.Transparent
            };
            spacer.Paint += (s, e) =>
            {
                using (var pen = new Pen(_cardBorder))
                {
                    e.Graphics.DrawLine(pen, 0, 0, spacer.Width, 0);
                }
            };
            body.Controls.Add(spacer, 0, 7);

            var actionWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(0, 0, 0, 0),
                Margin = new Padding(0)
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

            btnPay = BuildActionButton("Thanh toán", _success, Color.White, _success);
            btnPay.Dock = DockStyle.Fill;
            btnPay.Click += BtnPay_Click;

            actionRow.Controls.Add(btnCancel, 0, 0);
            actionRow.Controls.Add(btnSave, 1, 0);
            actionRow.Controls.Add(btnPay, 2, 0);
            actionWrap.Controls.Add(actionRow);
            body.Controls.Add(actionWrap, 0, 8);

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
            body.Controls.Add(cancelRoomWrap, 0, 9);

            card.Controls.Add(body);
            return card;
        }

        // =========================
        // Actions
        // =========================
        private async void BtnSave_Click(object sender, EventArgs e)
        {
            SetActionButtonsEnabled(false);
            UseWaitCursor = true;
            try
            {
                await UiExceptionHandler.RunAsync(this, "HourlyCheckout.SaveProgress", async () =>
                {
                    using (var perf = PerformanceTracker.Measure("HourlyCheckout.SaveProgress", new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["BookingId"] = _bookingId
                    }))
                    {
                        if (_bookingId <= 0)
                            throw new DomainException("Phiên đặt phòng chưa được khởi tạo. Vui lòng tải lại màn hình.");

                        int finalSoftDrinkQty = Math.Max(0, _savedSoftDrinkCount + _pendingSoftDrinkCount);
                        int finalWaterBottleQty = Math.Max(0, _savedWaterBottleCount + _pendingWaterBottleCount);

                        DateTime startTime = _room.ThoiGianBatDau ?? DateTime.Now;
                        var request = new CheckoutService.SaveHourlyRequest
                        {
                            BookingId = _bookingId,
                            RoomId = _room.PhongID,
                            StartTime = startTime,
                            GuestDisplayName = _room.TenKhachHienThi,
                            SoftDrinkQty = finalSoftDrinkQty,
                            WaterBottleQty = finalWaterBottleQty,
                            SoftDrinkUnitPrice = _pricing.DrinkSoftPrice,
                            WaterBottleUnitPrice = _pricing.DrinkWaterPrice
                        };
                        await Task.Run(() => _checkoutService.SaveHourly(request)).ConfigureAwait(true);
                        _pendingSoftDrinkCount = 0;
                        _pendingWaterBottleCount = 0;
                        LoadExtrasFromDatabase();

                        _room.TrangThai = 1;
                        _room.KieuThue = 3;
                        _room.ThoiGianBatDau = startTime;

                        Saved?.Invoke(this, EventArgs.Empty);
                        RefreshTotals();
                        BackRequested?.Invoke(this, EventArgs.Empty);
                        perf.AddContext("SoftDrinkQty", finalSoftDrinkQty);
                        perf.AddContext("WaterQty", finalWaterBottleQty);
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
                await UiExceptionHandler.RunAsync(this, "HourlyCheckout.Pay", async () =>
                {
                    using (var perf = PerformanceTracker.Measure("HourlyCheckout.Pay", new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["BookingId"] = _bookingId
                    }))
                    {
                        if (_bookingId <= 0)
                            throw new DomainException("Phiên đặt phòng chưa được khởi tạo. Vui lòng tải lại màn hình.");

                        DateTime start = _room.ThoiGianBatDau ?? DateTime.Now;
                        if (start > DateTime.Now) start = DateTime.Now;
                        DateTime now = DateTime.Now;

                        _savedCollectedAmount = await Task.Run(() => _bookingDal.GetPaidAmountByBooking(_bookingId)).ConfigureAwait(true);

                        int finalDrink = Math.Max(0, _savedSoftDrinkCount + _pendingSoftDrinkCount);
                        int finalWater = Math.Max(0, _savedWaterBottleCount + _pendingWaterBottleCount);

                        decimal roomCharge = CalculateRoomChargeWithCurrentRates(start, now);
                        decimal totalCharge = roomCharge + finalDrink * _pricing.DrinkSoftPrice + finalWater * _pricing.DrinkWaterPrice;
                        decimal dueAmount = Math.Max(0m, totalCharge - _savedCollectedAmount);

                        string message = "Xác nhận thanh toán phòng " + _room.MaPhong +
                                         "\nTổng tiền: " + ToMoney(totalCharge) +
                                         "\nCần thu: " + ToMoney(dueAmount);

                        var confirm = MessageBox.Show(message, "Xác nhận thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm != DialogResult.Yes) return;

                        var request = new CheckoutService.PayHourlyRequest
                        {
                            BookingId = _bookingId,
                            RoomId = _room.PhongID,
                            PaidAt = now,
                            SoftDrinkQty = finalDrink,
                            WaterBottleQty = finalWater,
                            SoftDrinkUnitPrice = _pricing.DrinkSoftPrice,
                            WaterBottleUnitPrice = _pricing.DrinkWaterPrice,
                            DueAmount = dueAmount
                        };
                        var result = await Task.Run(() => _checkoutService.PayHourly(request)).ConfigureAwait(true);

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
                await UiExceptionHandler.RunAsync(this, "HourlyCheckout.CancelRoom", async () =>
                {
                    using (var perf = PerformanceTracker.Measure("HourlyCheckout.CancelRoom", new Dictionary<string, object>
                    {
                        ["BookingId"] = _bookingId,
                        ["RoomId"] = _room.PhongID
                    }))
                    {
                        var request = new CheckoutService.CancelHourlyRequest
                        {
                            BookingId = _bookingId,
                            RoomId = _room.PhongID,
                            CancelledAt = DateTime.Now
                        };
                        await Task.Run(() => _checkoutService.CancelHourly(request)).ConfigureAwait(true);

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

        // =========================
        // Totals / Refresh
        // =========================
        private void LoadExtrasFromDatabase()
        {
            using (var perf = PerformanceTracker.Measure("HourlyCheckout.LoadExtras", new System.Collections.Generic.Dictionary<string, object>
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

        private void RefreshTotals()
        {
            DateTime start = _room.ThoiGianBatDau ?? DateTime.Now;
            if (start > DateTime.Now) start = DateTime.Now;
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

            TimeSpan elapsed = DateTime.Now - start;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

            if (lblStartTime != null) lblStartTime.Text = start.ToString("dd/MM/yyyy HH:mm:ss");
            if (lblStayHours != null) lblStayHours.Text = ToDurationText(elapsed);

            if (lblSavedDrink != null) lblSavedDrink.Text = "Đã lưu: " + _savedSoftDrinkCount;
            if (lblSavedWater != null) lblSavedWater.Text = "Đã lưu: " + _savedWaterBottleCount;

            int totalDrinkQty = Math.Max(0, _savedSoftDrinkCount + _pendingSoftDrinkCount);
            int totalWaterQty = Math.Max(0, _savedWaterBottleCount + _pendingWaterBottleCount);
            if (lblPendingDrink != null) lblPendingDrink.Text = totalDrinkQty.ToString();
            if (lblPendingWater != null) lblPendingWater.Text = totalWaterQty.ToString();

            decimal hour1 = NormalizeHourlyRate(_hour1Rate);
            decimal nextHour = NormalizeHourlyRate(_nextHourRate);
            if (lblRateFrameInline != null)
                lblRateFrameInline.Text = "Giờ đầu " + ToMoneyCompact(hour1) + " | Giờ sau " + ToMoneyCompact(nextHour);
            if (lblHour1Rate != null) lblHour1Rate.Text = ToMoneyCompact(hour1);
            if (lblNextHourRate != null) lblNextHourRate.Text = ToMoneyCompact(nextHour);

            decimal roomCharge = CalculateRoomChargeWithCurrentRates(start, DateTime.Now);
            decimal pendingDrinkCharge = _pendingSoftDrinkCount * _pricing.DrinkSoftPrice
                                       + _pendingWaterBottleCount * _pricing.DrinkWaterPrice;
            decimal drinkCharge = Math.Max(0m, _savedDrinkCharge + pendingDrinkCharge);
            decimal lateFeeCharge = 0m;
            decimal total = roomCharge + drinkCharge + lateFeeCharge;
            decimal due = Math.Max(0m, total - _savedCollectedAmount);

            if (lblRoomChargeValue != null) lblRoomChargeValue.Text = ToMoneyCompact(roomCharge);
            if (lblDrinkChargeValue != null) lblDrinkChargeValue.Text = ToMoneyCompact(drinkCharge);
            if (lblLateFeeValue != null) lblLateFeeValue.Text = ToMoneyCompact(lateFeeCharge);
            if (lblTotalChargeValue != null) lblTotalChargeValue.Text = ToMoneyCompact(total);
            if (lblCollectedValue != null) lblCollectedValue.Text = ToMoneyCompact(_savedCollectedAmount);
            if (lblDueValue != null) lblDueValue.Text = ToMoney(due);
            if (lblSurchargeReasonValue != null)
                lblSurchargeReasonValue.Text = BuildLateFeeReasonText(lateFeeCharge);

            if (btnPay != null) btnPay.Text = "Thanh toán " + ToMoneyCompact(due);
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

        private decimal CalculateRoomChargeWithCurrentRates(DateTime start, DateTime now)
        {
            int billableHours = _pricingService.CalculateBillableHours(start, now, _room.LoaiPhongID);
            decimal first = NormalizeHourlyRate(_hour1Rate);
            decimal next = NormalizeHourlyRate(_nextHourRate);
            if (billableHours <= 1) return first;
            return first + (billableHours - 1) * next;
        }

        private static decimal NormalizeHourlyRate(decimal value)
        {
            if (value < MinHourlyRate) return MinHourlyRate;
            return Math.Max(0m, value);
        }

        private static string BuildLateFeeReasonText(decimal lateFeeAmount)
        {
            if (lateFeeAmount <= 0m) return string.Empty;
            return "Lý do phụ thu: Trả trễ (+" + ToMoneyCompact(lateFeeAmount) + ")";
        }

        // =========================
        // Controls / Helpers
        // =========================
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

                _contentLeftColumn.Margin = new Padding(0, 0, 0, 10);
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

            var t = new Label
            {
                Text = title,
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 3)
            };

            stack.Controls.Add(t);
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                var s = new Label
                {
                    Text = subtitle,
                    AutoSize = true,
                    ForeColor = _textSecondary,
                    Font = new Font("Segoe UI", 9F),
                    Margin = new Padding(0)
                };
                stack.Controls.Add(s);
            }
            return stack;
        }

        private Label AddValueRow(TableLayoutPanel grid, int row, string label, bool emphasize, Color emphasizeColor)
        {
            if (grid.RowCount <= row) grid.RowCount = row + 1;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lbl = new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, row == 0 ? 0 : 8, 0, 0),
                Font = new Font("Segoe UI", 9.6F),
                ForeColor = _textSecondary
            };

            var value = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, row == 0 ? 0 : 8, 0, 0),
                Font = emphasize ? new Font("Segoe UI Semibold", 9.8F, FontStyle.Bold) : new Font("Segoe UI", 9.6F),
                ForeColor = emphasize ? emphasizeColor : _textPrimary
            };

            grid.Controls.Add(lbl, 0, row);
            grid.Controls.Add(value, 1, row);

            return value;
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

        private Label CreateDrinkNameLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 2)
            };
        }

        private Label CreateDrinkPriceLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI", 10.5F),
                Margin = new Padding(0, 2, 0, 2)
            };
        }

        private Control CreateDrinkRow(string name, string price, Func<int> getValue, Action<int> setValue, out Label valueLabel)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Margin = new Padding(0, 1, 0, 1),
                Padding = new Padding(0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            row.Controls.Add(CreateDrinkNameLabel(name), 0, 0);
            row.Controls.Add(CreateDrinkPriceLabel(price), 1, 0);

            var stepper = CreateStepper(
                getValue: getValue,
                setValue: setValue,
                minusColor: _danger,
                plusColor: _success,
                out valueLabel);
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
            btnMinus.Click += (s, e) =>
            {
                int v = Math.Max(0, getValue() - 1);
                setValue(v);
            };

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
                if (s is Label label)
                {
                    using (var pen = new Pen(_cardBorder))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, label.Width - 1, label.Height - 1);
                    }
                }
            };
            valueLabel.Cursor = Cursors.IBeam;
            valueLabel.DoubleClick += (s, e) =>
            {
                int current = Math.Max(0, getValue());
                int edited = ShowQuantityEditor(current);
                setValue(Math.Max(0, edited));
            };

            var btnPlus = BuildSquareButton("+", plusColor);
            btnPlus.Click += (s, e) =>
            {
                int v = Math.Min(999, getValue() + 1);
                setValue(v);
            };

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
                var btnCancel = new Button
                {
                    Text = "Hủy",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(172, 74),
                    Size = new Size(74, 28)
                };

                dialog.Controls.Add(lbl);
                dialog.Controls.Add(nud);
                dialog.Controls.Add(btnOk);
                dialog.Controls.Add(btnCancel);
                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;

                var result = dialog.ShowDialog(this);
                if (result != DialogResult.OK)
                    return initial;

                return Convert.ToInt32(nud.Value);
            }
        }

        private Control CreateHourlyRateEditor(string title, Func<decimal> getValue, Action<decimal> setValue, out Label valueLabel)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 2),
                Padding = new Padding(0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));

            var titleLabel = new Label
            {
                Text = title + ":",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Margin = new Padding(0, 8, 0, 0)
            };

            var stepper = CreateMoneyStepper(
                getValue: getValue,
                setValue: setValue,
                step: HourlyRateStep,
                minValue: MinHourlyRate,
                minusColor: _danger,
                plusColor: _success,
                out valueLabel);
            stepper.Dock = DockStyle.None;
            stepper.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            row.Controls.Add(titleLabel, 0, 0);
            row.Controls.Add(stepper, 1, 0);
            return row;
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

            decimal safeStep = step <= 0m ? HourlyRateStep : step;
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
            if (btnCloseTop != null && !btnCloseTop.IsDisposed) btnCloseTop.Enabled = enabled;
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

        private static string ToDurationText(TimeSpan elapsed)
        {
            int totalHours = (int)elapsed.TotalHours;
            return totalHours + " giờ " + elapsed.Minutes + " phút " + elapsed.Seconds + " giây";
        }
    }
}
