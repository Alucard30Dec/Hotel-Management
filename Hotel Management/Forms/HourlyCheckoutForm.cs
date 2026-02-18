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
        private readonly RoomDAL _roomDal = new RoomDAL();

        private const decimal GIA_GIO_DON_DAU = 70000m;
        private const decimal GIA_GIO_DON_SAU = 20000m;
        private const decimal GIA_GIO_DOI_DAU = 120000m;
        private const decimal GIA_GIO_DOI_SAU = 30000m;
        private const decimal GIA_NUOC_NGOT = 20000m;
        private const decimal GIA_NUOC_SUOI = 10000m;

        private int _savedSoftDrinkCount;
        private int _savedWaterBottleCount;
        private decimal _savedCollectedAmount;

        private int _pendingSoftDrinkCount;
        private int _pendingWaterBottleCount;

        // ===== UI refs =====
        private Label lblTitleRoom;
        private Label lblChipRoom;
        private Label lblCheckin;

        private Label lblStartTime;
        private Label lblStayHours;

        private Label lblSavedDrink;
        private Label lblSavedWater;

        private Label lblPendingDrink;
        private Label lblPendingWater;
        private Label lblDrinkSubtotal;

        private Label lblRoomChargeValue;
        private Label lblDrinkChargeValue;
        private Label lblTotalChargeValue;
        private Label lblCollectedValue;
        private Label lblDueValue;

        private Button btnCloseTop;
        private Button btnSave;
        private Button btnPay;
        private Button btnCancel;

        private TableLayoutPanel _contentGrid;
        private TableLayoutPanel _contentLeftColumn;
        private TableLayoutPanel _contentRightColumn;
        private bool _isCompactLayout;
        private Timer _stayTimer;

        public event EventHandler BackRequested;
        public event EventHandler Saved;
        public event EventHandler PaymentCompleted;

        public HourlyCheckoutForm(Room room)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
            InitializeUi();
            Load += HourlyCheckoutForm_Load;
            Disposed += HourlyCheckoutForm_Disposed;
        }

        private void HourlyCheckoutForm_Load(object sender, EventArgs e)
        {
            _savedSoftDrinkCount = GetIntTag(_room.GhiChu, "NN", 0);
            _savedWaterBottleCount = GetIntTag(_room.GhiChu, "NS", 0);
            _savedCollectedAmount = GetDecimalTag(_room.GhiChu, "DT", 0m);

            _pendingSoftDrinkCount = 0;
            _pendingWaterBottleCount = 0;

            lblTitleRoom.Text = "Tính tiền phòng theo giờ - Phòng " + _room.MaPhong;
            lblChipRoom.Text = "  Phòng " + _room.MaPhong + "  ";
            lblCheckin.Text = "Check-in: " + (_room.ThoiGianBatDau ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm");

            RefreshTotals();
            EnsureStayTimer();
        }

        private void HourlyCheckoutForm_Disposed(object sender, EventArgs e)
        {
            if (_stayTimer == null) return;
            _stayTimer.Stop();
            _stayTimer.Dispose();
            _stayTimer = null;
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
                Height = 96,
                Padding = new Padding(12, 10, 12, 6),
                BackColor = _pageBackground,
                Margin = new Padding(0)
            };

            var title = new Label
            {
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
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
                Padding = new Padding(12, 4, 12, 10),
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
            _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63F));
            _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37F));

            _contentLeftColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 1,
                Margin = new Padding(0, 0, 10, 0)
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

            btnSave = BuildActionButton("Lưu", Color.White, _brandA, _cardBorder);
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
            card.Padding = new Padding(12, 10, 12, 10);
            card.Margin = new Padding(0);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 8
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

            body.Controls.Add(CreateCardTitle("Thông tin lưu trú", null), 0, 0);

            var stayGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(0)
            };
            stayGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            stayGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));

            var lblStayLabel = new Label
            {
                Text = "Thời gian lưu trú:",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 6)
            };
            lblStayHours = new Label
            {
                Text = "",
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 0, 0, 6)
            };
            stayGrid.Controls.Add(lblStayLabel, 0, 0);
            stayGrid.Controls.Add(lblStayHours, 1, 0);

            string roomRateText = _room.LoaiPhongID == 1
                ? "Giờ đầu 70.000đ - giờ sau 20.000đ"
                : "Giờ đầu 120.000đ - giờ sau 30.000đ";

            var lblRateLabel = new Label
            {
                Text = "Khung giá:",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = _textSecondary
            };
            var lblRateValue = new Label
            {
                Text = roomRateText,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = _textPrimary,
                TextAlign = ContentAlignment.MiddleRight
            };

            stayGrid.Controls.Add(lblRateLabel, 0, 1);
            stayGrid.Controls.Add(lblRateValue, 1, 1);

            body.Controls.Add(stayGrid, 0, 1);
            body.Controls.Add(CreateDivider(0, 8), 0, 2);

            var drinkTitle = new Label
            {
                Text = "Nước sử dụng",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 4)
            };
            body.Controls.Add(drinkTitle, 0, 3);

            var drinkRows = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0)
            };
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            drinkRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            drinkRows.Controls.Add(CreateDrinkRow("Nước ngọt", "20.000đ",
                () => _pendingSoftDrinkCount,
                v => { _pendingSoftDrinkCount = v; RefreshTotals(); },
                out lblPendingDrink), 0, 0);
            drinkRows.Controls.Add(CreateDivider(0, 4), 0, 1);

            drinkRows.Controls.Add(CreateDrinkRow("Nước suối", "10.000đ",
                () => _pendingWaterBottleCount,
                v => { _pendingWaterBottleCount = v; RefreshTotals(); },
                out lblPendingWater), 0, 2);
            drinkRows.Controls.Add(CreateDivider(0, 4), 0, 3);

            lblDrinkSubtotal = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 4, 0, 0)
            };
            drinkRows.Controls.Add(lblDrinkSubtotal, 0, 4);

            body.Controls.Add(drinkRows, 0, 4);

            body.Controls.Add(CreateDivider(0, 8), 0, 5);

            var hint = new Label
            {
                Text = "Mẹo: Bấm nút để gọi thêm nước, bấm Thanh toán để kết thúc phòng.",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 6, 0, 0)
            };
            body.Controls.Add(hint, 0, 6);

            body.Controls.Add(new Panel { Height = 2, Dock = DockStyle.Top, Margin = new Padding(0) }, 0, 7);
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
            rows.Controls.Add(CreateDrinkPriceLabel("20.000đ"), 1, 0);

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
            rows.Controls.Add(CreateDrinkPriceLabel("10.000đ"), 1, 1);

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
            lblDrinkSubtotal = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 10, 0, 0)
            };
            body.Controls.Add(lblDrinkSubtotal, 0, 2);

            // Hint
            var hint = new Label
            {
                Text = "Mẹo: Bấm +/- để gọi thêm nước, bấm Thanh toán để kết thúc phòng.",
                AutoSize = true,
                ForeColor = _textSecondary,
                Font = new Font("Segoe UI", 9.6F),
                Margin = new Padding(0, 12, 0, 0)
            };
            body.Controls.Add(hint, 0, 3);

            card.Controls.Add(body);
            return card;
        }

        private Control BuildSummaryCard()
        {
            var card = CreateCard();
            card.Padding = new Padding(12, 10, 12, 10);
            card.Margin = new Padding(0);
            card.MinimumSize = new Size(320, 0);

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

            body.Controls.Add(CreateCardTitle("Tổng kết thanh toán", null), 0, 0);

            // Due highlight
            var dueBox = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.FromArgb(230, 239, 238),
                Margin = new Padding(0, 6, 0, 8),
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
            AddSummaryRow(grid, 1, "Tiền nước:", out lblDrinkChargeValue);
            body.Controls.Add(grid, 0, 2);

            var sep = CreateDivider(0, 10);
            body.Controls.Add(sep, 0, 3);

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
            body.Controls.Add(totalRow, 0, 4);

            var spacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Margin = new Padding(0, 8, 0, 8),
                BackColor = Color.Transparent
            };
            spacer.Paint += (s, e) =>
            {
                using (var pen = new Pen(_cardBorder))
                {
                    e.Graphics.DrawLine(pen, 0, 0, spacer.Width, 0);
                }
            };
            body.Controls.Add(spacer, 0, 5);

            var actionWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(0, 0, 0, 0),
                Margin = new Padding(0)
            };

            var actionRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            btnCancel = BuildActionButton("Hủy", Color.White, _textPrimary, _cardBorder);
            btnCancel.Margin = new Padding(0, 0, 8, 0);
            btnCancel.Dock = DockStyle.Fill;
            btnCancel.Click += (s, e) => HandleCancelRequest();

            btnSave = BuildActionButton("Lưu", Color.White, _brandA, _cardBorder);
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
            body.Controls.Add(actionWrap, 0, 6);

            lblCollectedValue = new Label();

            card.Controls.Add(body);
            return card;
        }

        // =========================
        // Actions
        // =========================
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_pendingSoftDrinkCount > 0 || _pendingWaterBottleCount > 0)
            {
                _savedSoftDrinkCount += _pendingSoftDrinkCount;
                _savedWaterBottleCount += _pendingWaterBottleCount;

                _pendingSoftDrinkCount = 0;
                _pendingWaterBottleCount = 0;

                string newGhiChu = ApplyBillingTags(_room.GhiChu, _savedSoftDrinkCount, _savedWaterBottleCount, _savedCollectedAmount);
                _roomDal.UpdateTrangThaiFull(_room.PhongID, _room.TrangThai, newGhiChu, _room.ThoiGianBatDau, _room.KieuThue, _room.TenKhachHienThi);
                _room.GhiChu = newGhiChu;

                Saved?.Invoke(this, EventArgs.Empty);
                RefreshTotals();
            }

            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnPay_Click(object sender, EventArgs e)
        {
            DateTime start = _room.ThoiGianBatDau ?? DateTime.Now;
            if (start > DateTime.Now) start = DateTime.Now;

            int stayedHours = Math.Max(1, (int)Math.Ceiling((DateTime.Now - start).TotalHours));

            int finalDrink = _savedSoftDrinkCount + _pendingSoftDrinkCount;
            int finalWater = _savedWaterBottleCount + _pendingWaterBottleCount;

            decimal totalCharge = CalculateRoomCharge(stayedHours) + finalDrink * GIA_NUOC_NGOT + finalWater * GIA_NUOC_SUOI;
            decimal dueAmount = Math.Max(0m, totalCharge - _savedCollectedAmount);

            string message = "Xác nhận thanh toán phòng " + _room.MaPhong +
                             "\nTổng tiền: " + ToMoney(totalCharge) +
                             "\nCần thu: " + ToMoney(dueAmount);

            var confirm = MessageBox.Show(message, "Xác nhận thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            string paidGhiChu = ApplyBillingTags(_room.GhiChu, finalDrink, finalWater, totalCharge);
            _roomDal.UpdateTrangThaiFull(_room.PhongID, 2, paidGhiChu, null, null, null);

            _room.GhiChu = paidGhiChu;
            _room.TrangThai = 2;
            _room.ThoiGianBatDau = null;
            _room.KieuThue = null;
            _room.TenKhachHienThi = null;

            PaymentCompleted?.Invoke(this, EventArgs.Empty);
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void HandleCancelRequest()
        {
            bool hasUnsaved = _pendingSoftDrinkCount > 0 || _pendingWaterBottleCount > 0;
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
        private void RefreshTotals()
        {
            DateTime start = _room.ThoiGianBatDau ?? DateTime.Now;
            if (start > DateTime.Now) start = DateTime.Now;

            TimeSpan elapsed = DateTime.Now - start;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

            int stayedHours = Math.Max(1, (int)Math.Ceiling(elapsed.TotalHours));
            if (lblStartTime != null) lblStartTime.Text = start.ToString("dd/MM/yyyy HH:mm");
            if (lblStayHours != null) lblStayHours.Text = ToDurationText(elapsed);

            if (lblSavedDrink != null) lblSavedDrink.Text = "Đã lưu: " + _savedSoftDrinkCount;
            if (lblSavedWater != null) lblSavedWater.Text = "Đã lưu: " + _savedWaterBottleCount;

            if (lblPendingDrink != null) lblPendingDrink.Text = (_savedSoftDrinkCount + _pendingSoftDrinkCount).ToString();
            if (lblPendingWater != null) lblPendingWater.Text = (_savedWaterBottleCount + _pendingWaterBottleCount).ToString();

            decimal roomCharge = CalculateRoomCharge(stayedHours);
            decimal drinkCharge = (_savedSoftDrinkCount + _pendingSoftDrinkCount) * GIA_NUOC_NGOT
                                + (_savedWaterBottleCount + _pendingWaterBottleCount) * GIA_NUOC_SUOI;
            decimal total = roomCharge + drinkCharge;
            decimal due = Math.Max(0m, total - _savedCollectedAmount);

            if (lblRoomChargeValue != null) lblRoomChargeValue.Text = ToMoneyCompact(roomCharge);
            if (lblDrinkChargeValue != null) lblDrinkChargeValue.Text = ToMoneyCompact(drinkCharge);
            if (lblTotalChargeValue != null) lblTotalChargeValue.Text = ToMoneyCompact(total);
            if (lblCollectedValue != null) lblCollectedValue.Text = ToMoneyCompact(_savedCollectedAmount);
            if (lblDueValue != null) lblDueValue.Text = ToMoney(due);

            decimal pendingSubtotal = _pendingSoftDrinkCount * GIA_NUOC_NGOT + _pendingWaterBottleCount * GIA_NUOC_SUOI;
            if (lblDrinkSubtotal != null) lblDrinkSubtotal.Text = pendingSubtotal > 0 ? ("= " + ToMoneyCompact(pendingSubtotal)) : "= 0đ";

            if (btnPay != null) btnPay.Text = "Thanh toán " + ToMoneyCompact(due);
        }

        private decimal CalculateRoomCharge(int stayedHours)
        {
            decimal first = _room.LoaiPhongID == 1 ? GIA_GIO_DON_DAU : GIA_GIO_DOI_DAU;
            decimal next = _room.LoaiPhongID == 1 ? GIA_GIO_DON_SAU : GIA_GIO_DOI_SAU;

            if (stayedHours <= 1) return first;
            return first + (stayedHours - 1) * next;
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
                _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63F));
                _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37F));
                _contentGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                _contentLeftColumn.Margin = new Padding(0, 0, 10, 0);
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
                Margin = new Padding(0, 4, 0, 4)
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
                Margin = new Padding(0, 6, 0, 4)
            };
        }

        private Control CreateDrinkRow(string name, string price, Func<int> getValue, Action<int> setValue, out Label valueLabel)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Margin = new Padding(0, 2, 0, 2),
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
            stepper.Dock = DockStyle.Right;

            var right = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            right.Controls.Add(stepper);
            row.Controls.Add(right, 2, 0);

            return row;
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
                Height = 34,
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

        // =========================
        // Billing tags
        // =========================
        private static string ApplyBillingTags(string original, int drinkCount, int waterCount, decimal collectedAmount)
        {
            string text = original ?? string.Empty;
            text = UpsertTag(text, "NN", drinkCount.ToString());
            text = UpsertTag(text, "NS", waterCount.ToString());
            text = UpsertTag(text, "DT", ((int)Math.Round(collectedAmount, MidpointRounding.AwayFromZero)).ToString());
            return text;
        }

        private static int GetIntTag(string text, string key, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(text)) return defaultValue;
            Match m = Regex.Match(text, @"\b" + Regex.Escape(key) + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int value))
                return value;
            return defaultValue;
        }

        private static decimal GetDecimalTag(string text, string key, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(text)) return defaultValue;
            Match m = Regex.Match(text, @"\b" + Regex.Escape(key) + @"\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && decimal.TryParse(m.Groups[1].Value, out decimal value))
                return value;
            return defaultValue;
        }

        private static string UpsertTag(string input, string key, string value)
        {
            var segments = (input ?? string.Empty)
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            var updated = new List<string>();
            bool replaced = false;

            foreach (string segment in segments)
            {
                int idx = segment.IndexOf('=');
                if (idx > 0)
                {
                    string tagKey = segment.Substring(0, idx).Trim();
                    if (tagKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!replaced)
                        {
                            updated.Add(key + "=" + value);
                            replaced = true;
                        }
                        continue;
                    }
                }

                updated.Add(segment);
            }

            if (!replaced)
                updated.Add(key + "=" + value);

            return string.Join(" | ", updated);
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
