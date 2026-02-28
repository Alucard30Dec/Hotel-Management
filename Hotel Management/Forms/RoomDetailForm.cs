using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;

namespace HotelManagement.Forms
{
    public partial class RoomDetailForm : Form
    {
        private readonly Room _room;
        private readonly BookingDAL _bookingDal = new BookingDAL();
        private readonly bool _isEditMode;
        private readonly bool _deferCheckinCommit;
        private readonly bool _isHourlyMode;
        private readonly string _maXaCu;
        private readonly GeoDataLoader _geoDataLoader;

        public event EventHandler BackRequested;
        public event EventHandler Saved;

        private bool _layoutApplied = false;
        private Panel _staySectionHeader;
        private Panel _staySectionBody;
        private BookingDAL.StayInfoRecord _loadedStayInfo;
        private readonly AutoCompleteStringCollection _priceSuggestionSource = new AutoCompleteStringCollection();
        private Timer _nhanPhongTimer;
        private bool _isGiaPhongFormatting;
        private bool _isGeoBinding;
        private bool _isComboFiltering;
        private List<Tinh> _geoTinhs = new List<Tinh>();
        private Tinh _legacyTinh;
        private Huyen _legacyHuyen;
        private Xa _legacyXa;
        private Panel _diaBanOldHeaderPanel;
        private Panel _diaBanNewHeaderPanel;
        private Panel _diaBanOldTinhCellPanel;
        private Panel _diaBanOldHuyenCellPanel;
        private Panel _diaBanOldXaCellPanel;
        private Panel _diaBanNewTinhCellPanel;
        private Panel _diaBanNewXaCellPanel;
        private Panel _diaBanNewDiaChiCellPanel;
        private Label _diaBanOldHeaderLabel;
        private Label _diaBanNewHeaderLabel;
        private Label _lblTinhThanhCu;
        private Label _lblHuyenCu;
        private Label _lblPhuongXaCu;
        private ComboBox _cboTinhThanhCu;
        private ComboBox _cboHuyenCu;
        private ComboBox _cboPhuongXaCu;
        private readonly Dictionary<ComboBox, List<object>> _comboOptionCache = new Dictionary<ComboBox, List<object>>();
        private readonly Dictionary<ComboBox, object> _comboBestMatchCache = new Dictionary<ComboBox, object>();
        private readonly Dictionary<ComboBox, Timer> _comboFilterDebounceTimers = new Dictionary<ComboBox, Timer>();
        private IReadOnlyDictionary<string, GeoDataLoader.OldNewGeoMapping> _oldToNewXaMap =
            new Dictionary<string, GeoDataLoader.OldNewGeoMapping>(StringComparer.OrdinalIgnoreCase);
        private bool _isLegacyGeoBound;
        private const int SuggestMaxVisibleItems = 10;
        private const int ComboFilterDebounceMs = 120;
        private const string GuestStorageSeparator = " - ";

        // Colors
        private readonly Color clrHeaderBg = Color.FromArgb(227, 242, 253); 
        private readonly Color clrHeaderText = Color.FromArgb(25, 118, 210); 
        private readonly Color clrPrimary = Color.FromArgb(33, 150, 243);    

        public RoomDetailForm(Room room, bool isEditMode = false, string maXaCu = null, string diaBanJsonPath = @"Address\dvhc_optimized.json", bool deferCheckinCommit = false)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            _room = room;
            _isEditMode = isEditMode;
            _deferCheckinCommit = deferCheckinCommit;
            _isHourlyMode = room.KieuThue.HasValue && room.KieuThue.Value == 3;
            _maXaCu = string.IsNullOrWhiteSpace(maXaCu) ? null : maXaCu.Trim();
            var finalDiaBanPath = string.IsNullOrWhiteSpace(diaBanJsonPath) ? @"Address\dvhc_optimized.json" : diaBanJsonPath;
            _geoDataLoader = new GeoDataLoader(finalDiaBanPath);

            InitializeComponent();
            WireEvents();
            ApplyResponsiveLayout();
        }

        private void WireEvents()
        {
            btnDong.Click -= btnDong_Click;
            btnDong.Click += btnDong_Click;

            btnNhanPhong.Click -= btnNhanPhong_Click;
            btnNhanPhong.Click += btnNhanPhong_Click;

            btnLamMoi.Click -= btnLamMoi_Click;
            btnLamMoi.Click += btnLamMoi_Click;

            txtGiaPhong.KeyPress -= txtGiaPhong_KeyPress;
            txtGiaPhong.KeyPress += txtGiaPhong_KeyPress;
            txtGiaPhong.Leave -= txtGiaPhong_Leave;
            txtGiaPhong.Leave += txtGiaPhong_Leave;

            KeyPreview = true;
            KeyDown -= RoomDetailForm_KeyDown;
            KeyDown += RoomDetailForm_KeyDown;

            FormClosed -= RoomDetailForm_FormClosed;
            FormClosed += RoomDetailForm_FormClosed;

            cboTinhThanh.SelectedIndexChanged -= cboTinh_SelectedIndexChanged;
            cboTinhThanh.SelectedIndexChanged += cboTinh_SelectedIndexChanged;
            cboTinhThanh.SelectionChangeCommitted -= cboTinh_SelectionChangeCommitted;
            cboTinhThanh.SelectionChangeCommitted += cboTinh_SelectionChangeCommitted;
            cboTinhThanh.Leave -= cboTinh_Leave;
            cboTinhThanh.Leave += cboTinh_Leave;
            cboTinhThanh.KeyDown -= cboTinh_KeyDown;
            cboTinhThanh.KeyDown += cboTinh_KeyDown;
            cboTinhThanh.TextUpdate -= cboGeo_TextUpdate;
            cboTinhThanh.TextUpdate += cboGeo_TextUpdate;
            cboTinhThanh.DropDown -= cboGeo_DropDown;
            cboTinhThanh.DropDown += cboGeo_DropDown;
            cboHuyen.SelectedIndexChanged -= cboHuyen_SelectedIndexChanged;
            cboHuyen.SelectedIndexChanged += cboHuyen_SelectedIndexChanged;
            cboHuyen.SelectionChangeCommitted -= cboHuyen_SelectionChangeCommitted;
            cboHuyen.SelectionChangeCommitted += cboHuyen_SelectionChangeCommitted;
            cboHuyen.Leave -= cboHuyen_Leave;
            cboHuyen.Leave += cboHuyen_Leave;
            cboHuyen.KeyDown -= cboHuyen_KeyDown;
            cboHuyen.KeyDown += cboHuyen_KeyDown;
            cboHuyen.TextUpdate -= cboGeo_TextUpdate;
            cboHuyen.TextUpdate += cboGeo_TextUpdate;
            cboHuyen.DropDown -= cboGeo_DropDown;
            cboHuyen.DropDown += cboGeo_DropDown;
            cboPhuongXa.SelectedIndexChanged -= cboXa_SelectedIndexChanged;
            cboPhuongXa.SelectedIndexChanged += cboXa_SelectedIndexChanged;
            cboPhuongXa.Leave -= cboXa_Leave;
            cboPhuongXa.Leave += cboXa_Leave;
            cboPhuongXa.KeyDown -= cboXa_KeyDown;
            cboPhuongXa.KeyDown += cboXa_KeyDown;
            cboPhuongXa.TextUpdate -= cboGeo_TextUpdate;
            cboPhuongXa.TextUpdate += cboGeo_TextUpdate;
            cboPhuongXa.DropDown -= cboGeo_DropDown;
            cboPhuongXa.DropDown += cboGeo_DropDown;
            cboGioiTinh.Leave -= cboGioiTinh_Leave;
            cboGioiTinh.Leave += cboGioiTinh_Leave;
            cboGioiTinh.KeyDown -= cboGioiTinh_KeyDown;
            cboGioiTinh.KeyDown += cboGioiTinh_KeyDown;
            cboGioiTinh.TextUpdate -= cboGioiTinh_TextUpdate;
            cboGioiTinh.TextUpdate += cboGioiTinh_TextUpdate;
            cboGioiTinh.DropDown -= cboGioiTinh_DropDown;
            cboGioiTinh.DropDown += cboGioiTinh_DropDown;

            rdoDiaBanMoi.CheckedChanged -= DiaBanLoai_CheckedChanged;
            rdoDiaBanMoi.CheckedChanged += DiaBanLoai_CheckedChanged;
            rdoDiaBanCu.CheckedChanged -= DiaBanLoai_CheckedChanged;
            rdoDiaBanCu.CheckedChanged += DiaBanLoai_CheckedChanged;
        }

        private async void RoomDetailForm_Load(object sender, EventArgs e)
        {
            await Task.Yield();
            using (var perf = PerformanceTracker.Measure("RoomDetail.Load", new Dictionary<string, object>
            {
                ["RoomId"] = _room.PhongID
            }))
            {
                lblTitle.Text = "Nhận phòng nhanh";
                btnNhanPhong.Text = "Lưu";
                lblRoomText.Text = $"{_room.MaPhong} • {(_room.LoaiPhongID == 1 ? "Phòng Đơn" : _room.LoaiPhongID == 2 ? "Phòng Đôi" : "Phòng")}, Tầng {_room.Tang}";

                InitCombos();

                try
                {
                    BindRoomInfo();
                }
                catch (Exception ex)
                {
                    ShowFriendlyError("RoomDetail.BindRoomInfo", "Không thể tải thông tin phòng. Vui lòng thử lại.", ex, "Lỗi dữ liệu phòng");
                    return;
                }

                SetNhanPhongNow();
                StartNhanPhongClock();
                BeginInvoke(new Action(LoadStayInfoState));

                if (!string.IsNullOrWhiteSpace(_room.TenKhachHienThi) && string.IsNullOrWhiteSpace(txtHoTen.Text))
                    txtHoTen.Text = _room.TenKhachHienThi;
            }
        }

        private void ApplyResponsiveLayout()
        {
            if (_layoutApplied) return;
            _layoutApplied = true;

            this.TopLevel = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true; 
            this.BackColor = Color.White;

            this.Controls.Clear();

            // Root Table
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                BackColor = Color.White
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); 
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 

            // 1. Header
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.White
            };
            var headerTable = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 1 };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var pnlTitle = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(33, 33, 33);
            lblTitle.AutoSize = true;

            lblRoomText.Font = new Font("Segoe UI", 10F);
            lblRoomText.ForeColor = Color.Gray;
            lblRoomText.AutoSize = true;

            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(lblRoomText);

            btnCloseTop.Text = "✕"; 
            btnCloseTop.Font = new Font("Arial", 14F, FontStyle.Regular);
            btnCloseTop.FlatStyle = FlatStyle.Flat;
            btnCloseTop.FlatAppearance.BorderSize = 0;
            btnCloseTop.ForeColor = Color.Gray;
            btnCloseTop.Size = new Size(40, 40);
            btnCloseTop.Cursor = Cursors.Hand;
            btnCloseTop.Click -= btnDong_Click;
            btnCloseTop.Click += btnDong_Click;

            headerTable.Controls.Add(pnlTitle, 0, 0);
            headerTable.Controls.Add(btnCloseTop, 1, 0);
            header.Controls.Add(headerTable);

            // 2. Content
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            var innerContent = new TableLayoutPanel 
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Padding = new Padding(0,0,15,0)
            };
            innerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Section 1: Luu Tru (Using IconHelper)
            _staySectionHeader = CreateSectionHeader("Thông tin lưu trú", IconHelper.CreateHomeIcon());
            _staySectionBody = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
            BuildLuuTruLayout(_staySectionBody);
            _staySectionHeader.Visible = !_isHourlyMode;
            _staySectionBody.Visible = !_isHourlyMode;

            // Section 2: Thong tin khach + danh sach khach (layout theo 2 cot)
            var pnlKhachSection = BuildKhachSectionLayout();

            innerContent.Controls.Add(_staySectionHeader, 0, 0);
            innerContent.Controls.Add(_staySectionBody, 0, 1);
            innerContent.Controls.Add(pnlKhachSection, 0, 2);
            contentPanel.Controls.Add(innerContent);

            // 3. Footer
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 60,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.WhiteSmoke
            };
            StyleButton(btnNhanPhong, true);
            btnNhanPhong.Width = 120;
            btnNhanPhong.Dock = DockStyle.Right;
            
            StyleButton(btnDong, false);
            btnDong.Width = 100;
            btnDong.Dock = DockStyle.Right;
            var footerSpacer = new Panel { Dock = DockStyle.Right, Width = 10 };

            footer.Controls.Add(btnNhanPhong);
            footer.Controls.Add(footerSpacer);
            footer.Controls.Add(btnDong);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(contentPanel, 0, 1);
            root.Controls.Add(footer, 0, 2);
            this.Controls.Add(root);
        }

        private Panel CreateSectionHeader(string title, Image icon)
        {
            var p = new Panel
            {
                Height = 36,
                BackColor = clrHeaderBg,
                Margin = new Padding(0, 10, 0, 0)
            };
            if (icon != null)
            {
                var pic = new PictureBox
                {
                    Image = icon,
                    SizeMode = PictureBoxSizeMode.AutoSize,
                    Location = new Point(10, 6),
                    BackColor = Color.Transparent
                };
                p.Controls.Add(pic);
            }
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = clrHeaderText,
                AutoSize = true,
                Location = new Point(40, 8) 
            };
            p.Controls.Add(lbl);
            return p;
        }

        private Panel BuildKhachSectionLayout()
        {
            var section = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };

            var left = new Panel { Dock = DockStyle.Fill, AutoSize = true };
            var leftHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = clrHeaderBg,
                Padding = new Padding(8, 6, 8, 6)
            };

            var leftHeaderTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            leftHeaderTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            leftHeaderTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var headerTitle = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            var khachIcon = new PictureBox
            {
                Image = IconHelper.CreateUserIcon(20),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Size = new Size(24, 24),
                Margin = new Padding(0, 0, 6, 0)
            };
            var khachLabel = new Label
            {
                Text = "Tên khách hàng",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = clrHeaderText,
                AutoSize = true,
                Margin = new Padding(0, 3, 0, 0)
            };
            headerTitle.Controls.Add(khachIcon);
            headerTitle.Controls.Add(khachLabel);

            StyleButton(btnLamMoi, false);
            btnLamMoi.Text = "Làm mới";
            btnLamMoi.Width = 94;
            btnLamMoi.Height = 30;
            btnLamMoi.Dock = DockStyle.None;
            btnLamMoi.Margin = new Padding(0, 0, 8, 0);

            var buttonFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            buttonFlow.Controls.Add(btnLamMoi);

            leftHeaderTable.Controls.Add(headerTitle, 0, 0);
            leftHeaderTable.Controls.Add(buttonFlow, 1, 0);
            leftHeader.Controls.Add(leftHeaderTable);

            var leftBody = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5, 5, 5, 0) };
            BuildKhachLayout(leftBody);
            left.Controls.Add(leftBody);
            left.Controls.Add(leftHeader);

            section.Controls.Add(left);
            return section;
        }

        private void BuildLuuTruLayout(Panel container)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            for(int i=0; i<4; i++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            AddCell(tbl, lblNhanPhong, dtpNhanPhong, 0, 0);
            AddCell(tbl, lblTraPhong, dtpTraPhong, 1, 0);
            AddCell(tbl, lblLyDo, cboLyDoLuuTru, 2, 0, 2);

            AddCell(tbl, lblLoaiPhong, cboLoaiPhong, 0, 1);
            AddCell(tbl, lblPhong, cboPhong, 1, 1);
            container.Controls.Add(tbl);
        }

        private void BuildKhachLayout(Panel container)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            AddCell(tbl, lblHoTen, txtHoTen, 0, 0);

            container.Controls.Add(tbl);
        }

        private void EnsureDiaBanCuControls()
        {
            if (_cboTinhThanhCu != null) return;

            _lblTinhThanhCu = new Label { Text = "Tỉnh/Thành phố Cũ *" };
            _lblHuyenCu = new Label { Text = "Quận/Huyện Cũ *" };
            _lblPhuongXaCu = new Label { Text = "Phường/Xã Cũ *" };

            _cboTinhThanhCu = new ComboBox();
            _cboHuyenCu = new ComboBox();
            _cboPhuongXaCu = new ComboBox();

            ConfigureTypeAheadCombo(_cboTinhThanhCu);
            ConfigureTypeAheadCombo(_cboHuyenCu);
            ConfigureTypeAheadCombo(_cboPhuongXaCu);

            _cboTinhThanhCu.SelectedIndexChanged += cboTinhCu_SelectedIndexChanged;
            _cboTinhThanhCu.SelectionChangeCommitted += cboTinhCu_SelectionChangeCommitted;
            _cboTinhThanhCu.Leave += cboTinhCu_Leave;
            _cboTinhThanhCu.KeyDown += cboTinhCu_KeyDown;
            _cboTinhThanhCu.TextUpdate += cboGeo_TextUpdate;
            _cboTinhThanhCu.DropDown += cboGeo_DropDown;

            _cboHuyenCu.SelectedIndexChanged += cboHuyenCu_SelectedIndexChanged;
            _cboHuyenCu.SelectionChangeCommitted += cboHuyenCu_SelectionChangeCommitted;
            _cboHuyenCu.Leave += cboHuyenCu_Leave;
            _cboHuyenCu.KeyDown += cboHuyenCu_KeyDown;
            _cboHuyenCu.TextUpdate += cboGeo_TextUpdate;
            _cboHuyenCu.DropDown += cboGeo_DropDown;

            _cboPhuongXaCu.Leave += cboPhuongXaCu_Leave;
            _cboPhuongXaCu.KeyDown += cboPhuongXaCu_KeyDown;
            _cboPhuongXaCu.SelectedIndexChanged += cboPhuongXaCu_SelectedIndexChanged;
            _cboPhuongXaCu.SelectionChangeCommitted += cboPhuongXaCu_SelectionChangeCommitted;
            _cboPhuongXaCu.TextUpdate += cboGeo_TextUpdate;
            _cboPhuongXaCu.DropDown += cboGeo_DropDown;
        }

        private Panel CreateInlineHeader(string title, out Label headerLabel)
        {
            var p = new Panel
            {
                Height = 26,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(239, 246, 255),
                Margin = new Padding(5, 2, 5, 8)
            };
            var lbl = new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = clrHeaderText,
                Location = new Point(10, 5)
            };
            p.Controls.Add(lbl);
            headerLabel = lbl;
            return p;
        }

        private Panel CreateCellPanel(Control label, Control input)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(5, 5, 5, 10), AutoSize = true };
            label.Dock = DockStyle.Top;
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9F);
            label.ForeColor = Color.Black;
            label.Padding = new Padding(0, 0, 0, 4);
            if (label.Text.Contains("*")) label.ForeColor = Color.Black; 

            input.Dock = DockStyle.Top;
            if (!(input is Panel) && !(input is FlowLayoutPanel)) StyleControl(input);
            p.Controls.Add(input);
            p.Controls.Add(label);
            return p;
        }

        private void AddCell(TableLayoutPanel tbl, Control lbl, Control ctrl, int col, int row, int colSpan = 1)
        {
            var p = CreateCellPanel(lbl, ctrl);
            tbl.Controls.Add(p, col, row);
            if (colSpan > 1) tbl.SetColumnSpan(p, colSpan);
        }

        private Panel CreateRadioPanel(params RadioButton[] radios)
        {
            var p = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 5, 0, 0)
            };
            foreach (var r in radios)
            {
                r.AutoSize = true;
                r.Margin = new Padding(0, 0, 20, 0);
                r.Font = new Font("Segoe UI", 9.5f);
                p.Controls.Add(r);
            }
            return p;
        }

        private void StyleControl(Control c)
        {
            c.Font = new Font("Segoe UI", 10F);
            if (c is TextBox txt) 
            { 
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Height = 30;
                if (txt.Multiline) txt.Height = 60;
            }
            else if (c is ComboBox cb) { cb.FlatStyle = FlatStyle.System; cb.Height = 30; }
            else if (c is DateTimePicker dt) { dt.Height = 30; }
        }

        private void StyleButton(Button b, bool primary)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = clrPrimary;
            b.BackColor = primary ? clrPrimary : Color.White;
            b.ForeColor = primary ? Color.White : clrPrimary;
            b.Cursor = Cursors.Hand;
            b.Height = 36;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        }

        private void InitCombos()
        {
            cboLyDoLuuTru.Items.Clear();
            cboLyDoLuuTru.Items.AddRange(new object[] { "-- Chọn --", "Du lịch", "Công tác", "Thăm thân", "Mục đích khác" });
            int defaultLyDoIndex = cboLyDoLuuTru.FindStringExact("Mục đích khác");
            cboLyDoLuuTru.SelectedIndex = defaultLyDoIndex >= 0 ? defaultLyDoIndex : 0;
            dtpNhanPhong.Enabled = false;
            SetTextBoxCueBanner(txtHoTen, "Nhập tên khách");
        }

        private void InitAddressCombosUiOnly()
        {
            ConfigureTypeAheadCombo(cboTinhThanh);
            ConfigureTypeAheadCombo(cboHuyen);
            ConfigureTypeAheadCombo(cboPhuongXa);
        }

        private static void ConfigureTypeAheadCombo(ComboBox comboBox)
        {
            if (comboBox == null) return;
            comboBox.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox.AutoCompleteMode = AutoCompleteMode.None;
            comboBox.AutoCompleteSource = AutoCompleteSource.None;
            comboBox.IntegralHeight = false;
            comboBox.MaxDropDownItems = SuggestMaxVisibleItems;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private const int CB_SETCUEBANNER = 0x1703;
        private const int EM_SETCUEBANNER = 0x1501;

        private static void SetComboCueBanner(ComboBox comboBox, string text)
        {
            if (comboBox == null || !comboBox.IsHandleCreated) return;
            SendMessage(comboBox.Handle, CB_SETCUEBANNER, IntPtr.Zero, text ?? string.Empty);
        }

        private static void SetTextBoxCueBanner(TextBox textBox, string text)
        {
            if (textBox == null || !textBox.IsHandleCreated) return;
            SendMessage(textBox.Handle, EM_SETCUEBANNER, new IntPtr(1), text ?? string.Empty);
        }

        private void CacheComboItems(ComboBox comboBox)
        {
            if (comboBox == null) return;
            var dedup = new List<object>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in comboBox.Items.Cast<object>())
            {
                string text = (item?.ToString() ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;

                string key = NormalizeForCompare(text);
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!seen.Add(key)) continue;

                dedup.Add(item);
            }

            _comboOptionCache[comboBox] = dedup;
            _comboBestMatchCache[comboBox] = null;
            AdjustComboDropDownHeight(comboBox, dedup.Count);
        }

        private static void AdjustComboDropDownHeight(ComboBox comboBox, int itemCount)
        {
            if (comboBox == null) return;

            int visibleItems = Math.Max(1, Math.Min(SuggestMaxVisibleItems, itemCount <= 0 ? 1 : itemCount));
            int itemHeight = Math.Max(16, comboBox.ItemHeight);
            comboBox.DropDownHeight = visibleItems * itemHeight + 8;
        }

        private void RestoreComboItems(ComboBox comboBox, bool preserveText)
        {
            if (comboBox == null) return;
            if (!_comboOptionCache.TryGetValue(comboBox, out var allItems))
                return;

            var text = comboBox.Text;
            _isComboFiltering = true;
            try
            {
                comboBox.BeginUpdate();
                comboBox.Items.Clear();
                foreach (var item in allItems)
                    comboBox.Items.Add(item);
                comboBox.EndUpdate();
                AdjustComboDropDownHeight(comboBox, allItems.Count);

                comboBox.SelectedIndex = -1;
                if (preserveText)
                {
                    comboBox.Text = text ?? string.Empty;
                    comboBox.SelectionStart = comboBox.Text.Length;
                    comboBox.SelectionLength = 0;
                }
            }
            finally
            {
                _comboBestMatchCache[comboBox] = null;
                _isComboFiltering = false;
            }
        }

        private void FilterComboByContains(ComboBox comboBox)
        {
            if (comboBox == null) return;
            if (!_comboOptionCache.TryGetValue(comboBox, out var allItems))
                return;

            string raw = comboBox.Text ?? string.Empty;
            string normalizedInput = NormalizeForCompare(raw);

            List<object> filtered;
            object bestItem = null;
            if (string.IsNullOrWhiteSpace(normalizedInput))
            {
                filtered = allItems.ToList();
            }
            else
            {
                var ranked = allItems
                    .Select(item =>
                    {
                        string itemText = item?.ToString() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(itemText))
                            return new { Item = item, Rank = int.MaxValue, Text = string.Empty };

                        int rank = ComputeSuggestionRank(raw, normalizedInput, itemText);
                        return new { Item = item, Rank = rank, Text = itemText };
                    })
                    .Where(x => x.Rank < int.MaxValue)
                    .OrderBy(x => x.Rank)
                    .ThenBy(x => x.Text, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                bestItem = ranked.FirstOrDefault()?.Item;
                filtered = ranked.Select(x => x.Item).ToList();
            }

            _isComboFiltering = true;
            try
            {
                comboBox.BeginUpdate();
                comboBox.Items.Clear();
                foreach (var item in filtered)
                    comboBox.Items.Add(item);
                comboBox.EndUpdate();
                AdjustComboDropDownHeight(comboBox, filtered.Count);

                comboBox.SelectedIndex = -1;
                comboBox.Text = raw;
                comboBox.SelectionStart = comboBox.Text.Length;
                comboBox.SelectionLength = 0;

                if (comboBox.Focused && filtered.Count > 0 && !string.IsNullOrWhiteSpace(raw))
                {
                    comboBox.DroppedDown = true;
                    Cursor.Current = Cursors.Default;
                }
                else if (filtered.Count == 0 && comboBox.DroppedDown)
                {
                    comboBox.DroppedDown = false;
                }
            }
            finally
            {
                _comboBestMatchCache[comboBox] = bestItem;
                _isComboFiltering = false;
            }
        }

        private static int ComputeSuggestionRank(string rawInput, string normalizedInput, string itemText)
        {
            string item = (itemText ?? string.Empty).Trim();
            if (item.Length == 0) return int.MaxValue;

            string raw = (rawInput ?? string.Empty).Trim();
            if (raw.Length == 0) return 0;

            string itemNormalized = NormalizeForCompare(item);
            if (itemNormalized.Length == 0) return int.MaxValue;

            if (item.Equals(raw, StringComparison.CurrentCultureIgnoreCase))
                return 0;
            if (item.StartsWith(raw, StringComparison.CurrentCultureIgnoreCase))
                return 100 + Math.Abs(item.Length - raw.Length);
            if (item.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(w => w.StartsWith(raw, StringComparison.CurrentCultureIgnoreCase)))
                return 200 + Math.Abs(item.Length - raw.Length);
            if (item.IndexOf(raw, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return 300 + Math.Abs(item.Length - raw.Length);

            if (itemNormalized.Equals(normalizedInput, StringComparison.Ordinal))
                return 400;
            if (itemNormalized.StartsWith(normalizedInput, StringComparison.Ordinal))
                return 500 + Math.Abs(itemNormalized.Length - normalizedInput.Length);
            if (itemNormalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(w => w.StartsWith(normalizedInput, StringComparison.Ordinal)))
                return 600 + Math.Abs(itemNormalized.Length - normalizedInput.Length);
            if (itemNormalized.Contains(normalizedInput))
                return 700 + Math.Abs(itemNormalized.Length - normalizedInput.Length);

            return int.MaxValue;
        }

        private async Task LoadGeoDataAndBindInitialAsync()
        {
            var geoTinhs = await Task.Run(() => _geoDataLoader.Load().ToList()).ConfigureAwait(true);
            var oldToNewMap = await Task.Run(() => _geoDataLoader.LoadOldToNewCommuneMap()).ConfigureAwait(true);
            ApplyGeoDataAndBindInitial(geoTinhs, oldToNewMap);
        }

        private void LoadGeoDataAndBindInitial()
        {
            var geoTinhs = _geoDataLoader.Load().ToList();
            var oldToNewMap = _geoDataLoader.LoadOldToNewCommuneMap();
            ApplyGeoDataAndBindInitial(geoTinhs, oldToNewMap);
        }

        private void ApplyGeoDataAndBindInitial(
            List<Tinh> geoTinhs,
            IReadOnlyDictionary<string, GeoDataLoader.OldNewGeoMapping> oldToNewMap)
        {
            _geoTinhs = geoTinhs ?? new List<Tinh>();
            _oldToNewXaMap = oldToNewMap
                ?? new Dictionary<string, GeoDataLoader.OldNewGeoMapping>(StringComparer.OrdinalIgnoreCase);
            _legacyTinh = null;
            _legacyHuyen = null;
            _legacyXa = null;
            _isLegacyGeoBound = false;

            string legacyMaXa = _maXaCu;

            if (_isEditMode && !string.IsNullOrWhiteSpace(legacyMaXa))
                TryFindXaPath(legacyMaXa, out _legacyTinh, out _legacyHuyen, out _legacyXa);

            BindAllGeoCombos();
        }

        private void ReloadGeoDataKeepingSelection()
        {
            string selectedMaTinh = GetSelectedTinh()?.MaTinh;
            string selectedMaXa = GetSelectedXa()?.MaXa;
            string selectedMaTinhCu = GetSelectedTinhCu()?.MaTinh;
            string selectedMaHuyenCu = GetSelectedHuyenCu()?.MaHuyen;
            string selectedMaXaCu = GetSelectedXaCu()?.MaXa;

            LoadGeoDataAndBindInitial();

            if (!string.IsNullOrWhiteSpace(selectedMaTinh))
                SelectTinhByMa(selectedMaTinh);
            if (!string.IsNullOrWhiteSpace(selectedMaXa))
                SelectXaByMa(selectedMaXa);
            bool hasLegacySelection =
                !string.IsNullOrWhiteSpace(selectedMaTinhCu) ||
                !string.IsNullOrWhiteSpace(selectedMaHuyenCu) ||
                !string.IsNullOrWhiteSpace(selectedMaXaCu);
            if (hasLegacySelection)
            {
                EnsureLegacyGeoBound();
                if (!string.IsNullOrWhiteSpace(selectedMaTinhCu))
                    SelectTinhCuByMa(selectedMaTinhCu);
                if (!string.IsNullOrWhiteSpace(selectedMaHuyenCu))
                    SelectHuyenCuByMa(selectedMaHuyenCu);
                if (!string.IsNullOrWhiteSpace(selectedMaXaCu))
                    SelectXaCuByMa(selectedMaXaCu);
            }
        }

        private void cboTinh_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding || _isComboFiltering) return;
            try
            {
                BindXaComboBySelectedTinhForNewMode(false);
            }
            catch (Exception ex)
            {
                ShowFriendlyError("RoomDetail.BindXaByTinh", "Không thể tải danh sách địa bàn theo tỉnh. Vui lòng thử lại.", ex, "Lỗi địa bàn");
            }
        }

        private void cboTinh_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cboTinh_SelectedIndexChanged(sender, e);
        }

        private void cboTinh_Leave(object sender, EventArgs e)
        {
            ApplyTypedTinhSelection();
        }

        private void cboTinh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            ApplyTypedTinhSelection();
        }

        private void cboGeo_TextUpdate(object sender, EventArgs e)
        {
            if (_isGeoBinding || _isComboFiltering) return;
            var combo = sender as ComboBox;
            if (combo == null) return;
            DebounceFilterCombo(combo);
        }

        private void cboGeo_DropDown(object sender, EventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || _isComboFiltering) return;
            if (_comboFilterDebounceTimers.TryGetValue(combo, out var debounceTimer))
                debounceTimer.Stop();

            if (string.IsNullOrWhiteSpace(combo.Text))
                RestoreComboItems(combo, preserveText: true);
            else
                FilterComboByContains(combo);
        }

        private void DebounceFilterCombo(ComboBox combo)
        {
            if (combo == null) return;
            if (!_comboFilterDebounceTimers.TryGetValue(combo, out var timer))
            {
                timer = new Timer { Interval = ComboFilterDebounceMs };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    if (!IsDisposed && !Disposing)
                        FilterComboByContains(combo);
                };
                _comboFilterDebounceTimers[combo] = timer;
            }

            timer.Stop();
            timer.Start();
        }

        private void cboHuyen_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Không dùng trong bố cục hiện tại.
        }

        private void cboHuyen_SelectionChangeCommitted(object sender, EventArgs e)
        {
            // Không dùng trong bố cục hiện tại.
        }

        private void cboHuyen_Leave(object sender, EventArgs e)
        {
            // Không dùng trong bố cục hiện tại.
        }

        private void cboHuyen_KeyDown(object sender, KeyEventArgs e)
        {
            // Không dùng trong bố cục hiện tại.
        }

        private void cboXa_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding || _isComboFiltering) return;
        }

        private void cboXa_Leave(object sender, EventArgs e)
        {
            ApplyTypedXaSelection();
        }

        private void cboXa_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            ApplyTypedXaSelection();
        }

        private void cboGioiTinh_Leave(object sender, EventArgs e)
        {
            TrySelectComboByTextContains(cboGioiTinh, cboGioiTinh.Text);
        }

        private void cboGioiTinh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            TrySelectComboByTextContains(cboGioiTinh, cboGioiTinh.Text);
        }

        private void cboGioiTinh_TextUpdate(object sender, EventArgs e)
        {
            if (_isComboFiltering) return;
            FilterComboByContains(cboGioiTinh);
        }

        private void cboGioiTinh_DropDown(object sender, EventArgs e)
        {
            if (_isComboFiltering) return;
            if (string.IsNullOrWhiteSpace(cboGioiTinh.Text))
                RestoreComboItems(cboGioiTinh, preserveText: true);
            else
                FilterComboByContains(cboGioiTinh);
        }

        private void UpdateDiaBanUiState()
        {
            bool isDiaBanMoi = rdoDiaBanMoi.Checked;

            if (_diaBanOldHeaderPanel != null) _diaBanOldHeaderPanel.Visible = !isDiaBanMoi;
            if (_diaBanOldTinhCellPanel != null) _diaBanOldTinhCellPanel.Visible = !isDiaBanMoi;
            if (_diaBanOldHuyenCellPanel != null) _diaBanOldHuyenCellPanel.Visible = !isDiaBanMoi;
            if (_diaBanOldXaCellPanel != null) _diaBanOldXaCellPanel.Visible = !isDiaBanMoi;

            if (_diaBanNewHeaderPanel != null) _diaBanNewHeaderPanel.Visible = true;
            if (_diaBanNewTinhCellPanel != null) _diaBanNewTinhCellPanel.Visible = true;
            if (_diaBanNewXaCellPanel != null) _diaBanNewXaCellPanel.Visible = true;
            if (_diaBanNewDiaChiCellPanel != null) _diaBanNewDiaChiCellPanel.Visible = true;

            if (_diaBanNewHeaderLabel != null) _diaBanNewHeaderLabel.Text = "Địa bàn mới";

            SetComboCueBanner(_cboTinhThanhCu, "Chọn Tỉnh/Thành phố Cũ");
            SetComboCueBanner(_cboHuyenCu, "Chọn Quận/Huyện Cũ");
            SetComboCueBanner(_cboPhuongXaCu, "Chọn Phường/Xã Cũ");
            SetComboCueBanner(cboTinhThanh, "Chọn Tỉnh/Thành");
            SetComboCueBanner(cboPhuongXa, "Chọn Phường/Xã/Đặc khu");
            SetTextBoxCueBanner(txtDiaChiChiTiet, "Số nhà, ngách, hẻm, đường...");
        }

        private void DiaBanLoai_CheckedChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding) return;
            UpdateDiaBanUiState();
            try
            {
                if (rdoDiaBanCu.Checked)
                    EnsureLegacyGeoBound();
            }
            catch (Exception ex)
            {
                ShowFriendlyError("RoomDetail.BindAllGeoCombos", "Không thể cập nhật danh sách địa bàn. Vui lòng thử lại.", ex, "Lỗi địa bàn");
            }
        }

        private void BindAllGeoCombos()
        {
            BindTinhCombo();
            if (rdoDiaBanCu.Checked || _isEditMode)
                EnsureLegacyGeoBound();
        }

        private void EnsureLegacyGeoBound()
        {
            if (_isLegacyGeoBound) return;
            BindTinhCuCombo();
            _isLegacyGeoBound = true;
        }

        private void BindTinhCombo()
        {
            _isGeoBinding = true;
            cboTinhThanh.BeginUpdate();
            try
            {
                string selectedMaTinh = GetSelectedTinh()?.MaTinh;
                cboTinhThanh.Items.Clear();

                var items = _geoTinhs
                    .Where(x => x != null && x.IsActive)
                    .OrderBy(x => x.TenTinh)
                    .ToList();

                foreach (var tinh in items)
                    cboTinhThanh.Items.Add(new GeoComboItem<Tinh>(tinh, BuildDisplayName(tinh.TenTinh, true)));

                if (!TrySelectComboByKey<Tinh>(cboTinhThanh, selectedMaTinh, t => t.MaTinh))
                {
                    cboTinhThanh.SelectedIndex = -1;
                    cboTinhThanh.Text = string.Empty;
                }

                CacheComboItems(cboTinhThanh);
                BindXaComboBySelectedTinhForNewMode();
            }
            finally
            {
                cboTinhThanh.EndUpdate();
                _isGeoBinding = false;
            }
        }

        private void BindTinhCuCombo()
        {
            EnsureDiaBanCuControls();
            _isGeoBinding = true;
            _cboTinhThanhCu.BeginUpdate();
            try
            {
                string selectedMaTinh = GetSelectedTinhCu()?.MaTinh;
                _cboTinhThanhCu.Items.Clear();

                var items = _geoTinhs
                    .Where(x => x != null && !x.IsActive)
                    .OrderBy(x => x.TenTinh)
                    .ToList();

                if (_isEditMode && _legacyTinh != null && !_legacyTinh.IsActive &&
                    !items.Any(x => string.Equals(x.MaTinh, _legacyTinh.MaTinh, StringComparison.OrdinalIgnoreCase)))
                {
                    items.Add(_legacyTinh);
                }

                foreach (var tinh in items.OrderBy(x => x.TenTinh))
                    _cboTinhThanhCu.Items.Add(new GeoComboItem<Tinh>(tinh, BuildDisplayName(tinh.TenTinh, false)));

                if (!TrySelectComboByKey<Tinh>(_cboTinhThanhCu, selectedMaTinh, t => t.MaTinh))
                {
                    _cboTinhThanhCu.SelectedIndex = -1;
                    _cboTinhThanhCu.Text = string.Empty;
                }

                CacheComboItems(_cboTinhThanhCu);
                BindHuyenCuComboBySelectedTinh();
            }
            finally
            {
                _cboTinhThanhCu.EndUpdate();
                _isGeoBinding = false;
            }
        }

        private void BindHuyenCuComboBySelectedTinh(bool autoSelectFirst = false)
        {
            EnsureDiaBanCuControls();
            _isGeoBinding = true;
            _cboHuyenCu.BeginUpdate();
            try
            {
                string selectedMaHuyen = GetSelectedHuyenCu()?.MaHuyen;
                var tinh = GetSelectedTinhCu();
                _cboHuyenCu.Items.Clear();

                if (tinh != null)
                {
                    var huyens = (tinh.Huyens ?? new List<Huyen>())
                        .Where(x => x != null && !x.IsActive)
                        .OrderBy(x => x.TenHuyen)
                        .ToList();

                    if (_isEditMode && _legacyHuyen != null && !_legacyHuyen.IsActive &&
                        string.Equals(_legacyTinh?.MaTinh, tinh.MaTinh, StringComparison.OrdinalIgnoreCase) &&
                        !huyens.Any(x => string.Equals(x.MaHuyen, _legacyHuyen.MaHuyen, StringComparison.OrdinalIgnoreCase)))
                    {
                        huyens.Add(_legacyHuyen);
                    }

                    foreach (var huyen in huyens.OrderBy(x => x.TenHuyen))
                        _cboHuyenCu.Items.Add(new GeoComboItem<Huyen>(huyen, BuildDisplayName(huyen.TenHuyen, false)));
                }

                if (!TrySelectComboByKey<Huyen>(_cboHuyenCu, selectedMaHuyen, x => x.MaHuyen))
                {
                    _cboHuyenCu.SelectedIndex = autoSelectFirst && _cboHuyenCu.Items.Count > 0 ? 0 : -1;
                    if (_cboHuyenCu.SelectedIndex < 0)
                        _cboHuyenCu.Text = string.Empty;
                }

                CacheComboItems(_cboHuyenCu);
                BindXaCuComboBySelectedHuyen(autoSelectFirst);
            }
            finally
            {
                _cboHuyenCu.EndUpdate();
                _isGeoBinding = false;
            }
        }

        private void BindXaCuComboBySelectedHuyen(bool autoSelectFirst = false)
        {
            EnsureDiaBanCuControls();
            _isGeoBinding = true;
            _cboPhuongXaCu.BeginUpdate();
            try
            {
                string selectedMaXa = GetSelectedXaCu()?.MaXa;
                var huyen = GetSelectedHuyenCu();
                _cboPhuongXaCu.Items.Clear();

                if (huyen != null)
                {
                    var xas = (huyen.Xas ?? new List<Xa>())
                        .Where(x => x != null && !x.IsActive)
                        .OrderBy(x => x.TenXa)
                        .ToList();

                    if (_isEditMode && _legacyXa != null && !_legacyXa.IsActive &&
                        string.Equals(_legacyHuyen?.MaHuyen, huyen.MaHuyen, StringComparison.OrdinalIgnoreCase) &&
                        !xas.Any(x => string.Equals(x.MaXa, _legacyXa.MaXa, StringComparison.OrdinalIgnoreCase)))
                    {
                        xas.Add(_legacyXa);
                    }

                    foreach (var xa in xas.OrderBy(x => x.TenXa))
                        _cboPhuongXaCu.Items.Add(new GeoComboItem<Xa>(xa, BuildDisplayName(xa.TenXa, false)));
                }

                if (!TrySelectComboByKey<Xa>(_cboPhuongXaCu, selectedMaXa, x => x.MaXa))
                {
                    _cboPhuongXaCu.SelectedIndex = autoSelectFirst && _cboPhuongXaCu.Items.Count > 0 ? 0 : -1;
                    if (_cboPhuongXaCu.SelectedIndex < 0)
                        _cboPhuongXaCu.Text = string.Empty;
                }

                CacheComboItems(_cboPhuongXaCu);
            }
            finally
            {
                _cboPhuongXaCu.EndUpdate();
                _isGeoBinding = false;
            }
        }

        private void BindXaComboBySelectedTinhForNewMode(bool autoSelectFirstXa = false)
        {
            _isGeoBinding = true;
            cboPhuongXa.BeginUpdate();
            try
            {
                string selectedMaXa = GetSelectedXa()?.MaXa;
                var tinh = GetSelectedTinh();
                cboPhuongXa.Items.Clear();

                if (tinh != null)
                {
                    var xas = (tinh.Huyens ?? new List<Huyen>())
                        .Where(h => h != null)
                        .SelectMany(h => h.Xas ?? new List<Xa>())
                        .Where(x => x != null && x.IsActive)
                        .GroupBy(x => x.MaXa ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .OrderBy(x => x.TenXa)
                        .ToList();

                    foreach (var xa in xas)
                        cboPhuongXa.Items.Add(new GeoComboItem<Xa>(xa, BuildDisplayName(xa.TenXa, true)));
                }

                if (!TrySelectComboByKey<Xa>(cboPhuongXa, selectedMaXa, x => x.MaXa))
                {
                    cboPhuongXa.SelectedIndex = autoSelectFirstXa && cboPhuongXa.Items.Count > 0 ? 0 : -1;
                    if (cboPhuongXa.SelectedIndex < 0)
                        cboPhuongXa.Text = string.Empty;
                }

                CacheComboItems(cboPhuongXa);
            }
            finally
            {
                cboPhuongXa.EndUpdate();
                _isGeoBinding = false;
            }
        }

        private void BindHuyenComboBySelectedTinh(bool autoSelectFirstHuyen = false)
        {
            // Không sử dụng trong bố cục hiện tại (địa bàn mới không có cấp quận/huyện).
        }

        private void BindXaComboBySelectedHuyen(bool autoSelectFirstXa = false)
        {
            // Không sử dụng trong bố cục hiện tại.
        }

        private bool TryFindXaPath(string maXa, out Tinh tinh, out Huyen huyen, out Xa xa)
        {
            tinh = null;
            huyen = null;
            xa = null;

            foreach (var t in _geoTinhs)
            {
                foreach (var h in t.Huyens)
                {
                    var foundXa = h.Xas.FirstOrDefault(x => string.Equals(x.MaXa, maXa, StringComparison.OrdinalIgnoreCase));
                    if (foundXa == null) continue;

                    tinh = t;
                    huyen = h;
                    xa = foundXa;
                    return true;
                }
            }

            return false;
        }

        private void SelectTinhByMa(string maTinh)
        {
            if (string.IsNullOrWhiteSpace(maTinh)) return;
            TrySelectComboByKey<Tinh>(cboTinhThanh, maTinh, x => x.MaTinh);
        }

        private void SelectHuyenByMa(string maHuyen)
        {
            // Không còn sử dụng trong bố cục hiện tại.
        }

        private void SelectXaByMa(string maXa)
        {
            if (string.IsNullOrWhiteSpace(maXa)) return;
            TrySelectComboByKey<Xa>(cboPhuongXa, maXa, x => x.MaXa);
        }

        private static bool TrySelectComboByKey<T>(ComboBox comboBox, string key, Func<T, string> keySelector) where T : class
        {
            if (comboBox == null || comboBox.Items.Count == 0 || string.IsNullOrWhiteSpace(key) || keySelector == null)
                return false;

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                var item = comboBox.Items[i] as GeoComboItem<T>;
                if (item == null || item.Value == null) continue;

                string itemKey = keySelector(item.Value);
                if (!string.Equals(itemKey, key, StringComparison.OrdinalIgnoreCase)) continue;
                comboBox.SelectedIndex = i;
                return true;
            }

            return false;
        }

        private static string BuildDisplayName(string name, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            return name;
        }

        private void ApplyTypedTinhSelection()
        {
            TrySelectComboByTextContains(cboTinhThanh, cboTinhThanh.Text);
        }

        private void ApplyTypedHuyenSelection()
        {
            // Không còn sử dụng trong bố cục hiện tại.
        }

        private void ApplyTypedXaSelection()
        {
            TrySelectComboByTextContains(cboPhuongXa, cboPhuongXa.Text);
        }

        private void ApplyTypedGeoSelections()
        {
            string tinhText = cboTinhThanh.Text;
            string xaText = cboPhuongXa.Text;
            string tinhCuText = _cboTinhThanhCu?.Text ?? string.Empty;
            string huyenCuText = _cboHuyenCu?.Text ?? string.Empty;
            string xaCuText = _cboPhuongXaCu?.Text ?? string.Empty;

            TrySelectComboByTextContains(cboTinhThanh, tinhText);
            TrySelectComboByTextContains(cboPhuongXa, xaText);
            if (!rdoDiaBanMoi.Checked)
            {
                TrySelectComboByTextContains(_cboTinhThanhCu, tinhCuText);
                TrySelectComboByTextContains(_cboHuyenCu, huyenCuText);
                TrySelectComboByTextContains(_cboPhuongXaCu, xaCuText);
            }
        }

        private bool TrySelectComboByTextContains(ComboBox comboBox, string input)
        {
            if (comboBox == null || string.IsNullOrWhiteSpace(input))
                return false;

            if (!_comboOptionCache.TryGetValue(comboBox, out var sourceItems))
                sourceItems = comboBox.Items.Cast<object>().ToList();

            object matchedItem = null;
            if (_comboBestMatchCache.TryGetValue(comboBox, out var cached) && cached != null)
            {
                matchedItem = cached;
            }

            if (matchedItem == null)
            {
                string raw = (input ?? string.Empty).Trim();
                string normalizedInput = NormalizeForCompare(raw);
                if (string.IsNullOrWhiteSpace(normalizedInput))
                    return false;

                matchedItem = sourceItems
                    .Select(item =>
                    {
                        string itemText = (item?.ToString() ?? string.Empty).Trim();
                        int rank = ComputeSuggestionRank(raw, normalizedInput, itemText);
                        return new { Item = item, Rank = rank, Text = itemText };
                    })
                    .Where(x => x.Rank < int.MaxValue)
                    .OrderBy(x => x.Rank)
                    .ThenBy(x => x.Text, StringComparer.CurrentCultureIgnoreCase)
                    .Select(x => x.Item)
                    .FirstOrDefault();
            }

            if (matchedItem == null) return false;

            RestoreComboItems(comboBox, preserveText: false);
            int indexInCurrent = comboBox.Items.IndexOf(matchedItem);
            if (indexInCurrent < 0) return false;
            comboBox.SelectedIndex = indexInCurrent;
            return true;
        }

        private Tinh GetSelectedTinh()
        {
            var item = cboTinhThanh.SelectedItem as GeoComboItem<Tinh>;
            return item?.Value;
        }

        private Huyen GetSelectedHuyen()
        {
            return null;
        }

        private Xa GetSelectedXa()
        {
            var item = cboPhuongXa.SelectedItem as GeoComboItem<Xa>;
            return item?.Value;
        }

        private Tinh GetSelectedTinhCu()
        {
            var item = _cboTinhThanhCu?.SelectedItem as GeoComboItem<Tinh>;
            return item?.Value;
        }

        private Huyen GetSelectedHuyenCu()
        {
            var item = _cboHuyenCu?.SelectedItem as GeoComboItem<Huyen>;
            return item?.Value;
        }

        private Xa GetSelectedXaCu()
        {
            var item = _cboPhuongXaCu?.SelectedItem as GeoComboItem<Xa>;
            return item?.Value;
        }

        private sealed class GeoComboItem<T> where T : class
        {
            public GeoComboItem(T value, string text)
            {
                Value = value;
                Text = text ?? string.Empty;
            }

            public T Value { get; private set; }
            public string Text { get; private set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private sealed class GuestListEntry
        {
            public string HoTen { get; set; }
            public string SoGiayTo { get; set; }
            public string GioiTinh { get; set; }
            public DateTime? NgaySinh { get; set; }
            public string LoaiGiayTo { get; set; }
            public string QuocTich { get; set; }
            public string NoiCuTru { get; set; }
            public bool? LaDiaBanCu { get; set; }
            public string MaTinhMoi { get; set; }
            public string MaXaMoi { get; set; }
            public string MaTinhCu { get; set; }
            public string MaHuyenCu { get; set; }
            public string MaXaCu { get; set; }
            public string DiaChiChiTiet { get; set; }

            public override string ToString()
            {
                string name = (HoTen ?? string.Empty).Trim();
                string paper = (SoGiayTo ?? string.Empty).Trim();
                if (name.Length == 0) return string.Empty;
                if (paper.Length == 0) return name;
                return name + GuestStorageSeparator + paper;
            }
        }

        private void BindRoomInfo()
        {
            cboLoaiPhong.Items.Clear();
            cboLoaiPhong.Items.Add(_room.LoaiPhongID == 1 ? "Phòng Đơn" : _room.LoaiPhongID == 2 ? "Phòng Đôi" : "Phòng");
            cboLoaiPhong.SelectedIndex = 0;
            cboLoaiPhong.Enabled = false;
            cboPhong.Items.Clear();
            cboPhong.Items.Add("Phòng " + _room.MaPhong);
            cboPhong.SelectedIndex = 0;
            cboPhong.Enabled = false;
            decimal gia = _loadedStayInfo != null ? _loadedStayInfo.GiaPhong : 0m;
            bool shouldQueryRoomRate =
                _deferCheckinCommit ||
                (_room != null
                    && _room.TrangThai == 1
                    && _room.KieuThue.HasValue
                    && _room.KieuThue.Value == 1);

            if (gia <= 0 && shouldQueryRoomRate)
            {
                try
                {
                    gia = _bookingDal.GetDonGiaNgayByPhong(_room.PhongID);
                }
                catch
                {
                    gia = 0m;
                }
            }
            if (gia <= 0) gia = PricingService.Instance.GetDefaultNightlyRate(_room.LoaiPhongID);
            txtGiaPhong.Text = gia.ToString("N0");
            ConfigureGiaPhongSuggestions(gia);
        }

        private void SetNhanPhongNow()
        {
            dtpNhanPhong.Value = DateTime.Now;
        }

        private void StartNhanPhongClock()
        {
            if (_nhanPhongTimer == null)
            {
                _nhanPhongTimer = new Timer { Interval = 1000 };
                _nhanPhongTimer.Tick += NhanPhongTimer_Tick;
            }
            dtpNhanPhong.Enabled = false;
            _nhanPhongTimer.Start();
        }

        private void NhanPhongTimer_Tick(object sender, EventArgs e)
        {
            SetNhanPhongNow();
        }

        private void StopNhanPhongClock()
        {
            if (_nhanPhongTimer == null) return;
            _nhanPhongTimer.Stop();
            _nhanPhongTimer.Tick -= NhanPhongTimer_Tick;
            _nhanPhongTimer.Dispose();
            _nhanPhongTimer = null;
        }

        private void ConfigureGiaPhongSuggestions(decimal basePrice)
        {
            var presets = new[] { 100000m, 150000m, 200000m, 250000m, 300000m, 350000m, 400000m, 500000m, 700000m, 1000000m, basePrice };
            _priceSuggestionSource.Clear();
            foreach (var value in presets.Distinct().OrderBy(v => v))
            {
                if (value <= 0) continue;
                _priceSuggestionSource.Add(value.ToString("N0"));
            }

            txtGiaPhong.ReadOnly = false;
            txtGiaPhong.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtGiaPhong.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtGiaPhong.AutoCompleteCustomSource = _priceSuggestionSource;
            txtGiaPhong.TextAlign = HorizontalAlignment.Right;
        }

        private void txtGiaPhong_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;
            e.Handled = true;
        }

        private void txtGiaPhong_Leave(object sender, EventArgs e)
        {
            FormatGiaPhongText();
        }

        private void FormatGiaPhongText()
        {
            if (_isGiaPhongFormatting) return;
            _isGiaPhongFormatting = true;
            try
            {
                var number = ParseMoneyToDecimal(txtGiaPhong.Text);
                txtGiaPhong.Text = number.ToString("N0");
            }
            finally
            {
                _isGiaPhongFormatting = false;
            }
        }

        private static decimal ParseMoneyToDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var digitsOnly = Regex.Replace(text, @"[^\d]", "");
            if (string.IsNullOrWhiteSpace(digitsOnly)) return 0;
            if (decimal.TryParse(digitsOnly, out var value)) return value;
            return 0;
        }

        private void ShowFriendlyError(string source, string message, Exception ex, string title = "Lỗi")
        {
            AppLogger.Error(ex, "Room detail operation failed.", new Dictionary<string, object>
            {
                ["Source"] = source,
                ["RoomId"] = _room == null ? 0 : _room.PhongID
            });
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void RoomDetailForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopNhanPhongClock();
            foreach (var timer in _comboFilterDebounceTimers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            _comboFilterDebounceTimers.Clear();
        }

        private void btnDong_Click(object sender, EventArgs e) { BackRequested?.Invoke(this, EventArgs.Empty); }

        private void RoomDetailForm_KeyDown(object sender, KeyEventArgs e)
        {
            // F1 scan flow was removed in simplified guest-name-only check-in.
        }

        private void btnQuetMa_Click(object sender, EventArgs e)
        {
            // Removed: no customer-detail scan input in guest-name-only flow.
        }

        private void ApplyCccdInfoToCheckinForm(CccdInfo info)
        {
            if (info == null) return;

            SelectComboByText(cboLoaiGiayTo, "Thẻ CCCD");
            SelectComboByText(cboQuocTich, string.IsNullOrWhiteSpace(info.Nationality) ? "VNM - Việt Nam" : info.Nationality);

            if (!string.IsNullOrWhiteSpace(info.DocumentNumber))
                txtSoGiayTo.Text = info.DocumentNumber;

            if (!string.IsNullOrWhiteSpace(info.FullName))
                txtHoTen.Text = info.FullName;

            if (info.DateOfBirth.HasValue)
                dtpNgaySinh.Value = info.DateOfBirth.Value;

            if (!string.IsNullOrWhiteSpace(info.Gender))
                SelectComboByText(cboGioiTinh, info.Gender);

            rdoThuongTru.Checked = true;
            rdoDiaBanMoi.Checked = true;

            if (!string.IsNullOrWhiteSpace(info.Province))
            {
                SelectTinhByNameContains(info.Province);
            }
            if (!string.IsNullOrWhiteSpace(info.Ward))
            {
                if (!SelectXaByNameContains(info.Ward) && !string.IsNullOrWhiteSpace(info.AddressRaw))
                    SelectGeoByAddressRaw(info.AddressRaw);
            }
            else if (!string.IsNullOrWhiteSpace(info.AddressRaw))
                SelectGeoByAddressRaw(info.AddressRaw);

            if (!string.IsNullOrWhiteSpace(info.AddressDetail))
                txtDiaChiChiTiet.Text = info.AddressDetail;
            else if (!string.IsNullOrWhiteSpace(info.AddressRaw))
                txtDiaChiChiTiet.Text = info.AddressRaw;
            else
                txtDiaChiChiTiet.Text = string.Empty;
        }

        private async void btnNhanPhong_Click(object sender, EventArgs e)
        {
            if (btnNhanPhong != null) btnNhanPhong.Enabled = false;
            UseWaitCursor = true;
            try
            {
                await UiExceptionHandler.RunAsync(this, "RoomDetail.SaveCheckin", async () =>
                {
                    using (var perf = PerformanceTracker.Measure("RoomDetail.SaveCheckin", new Dictionary<string, object>
                    {
                        ["RoomId"] = _room.PhongID
                    }))
                    {
                        if (!ValidateForm()) return;

                        string tenChinh = GetPrimaryGuestName();
                        int bookingType = _isHourlyMode ? (int)BookingType.Hourly : (int)BookingType.Overnight;
                        int? existingBookingId = await Task.Run(() =>
                            _bookingDal.GetCurrentBookingByRoom(_room.PhongID)?.DatPhongID).ConfigureAwait(true);
                        var checkinNow = _deferCheckinCommit
                            ? (_room.ThoiGianBatDau ?? DateTime.Now)
                            : DateTime.Now;
                        dtpNhanPhong.Value = checkinNow;
                        int rentalType = (_room.KieuThue.HasValue && _room.KieuThue.Value > 0)
                            ? _room.KieuThue.Value
                            : (_isHourlyMode ? (int)RentalType.Hourly : (int)RentalType.Overnight);
                        BookingDAL.StayInfoRecord stayInfo = null;

                        if (!_isHourlyMode)
                        {
                            stayInfo = BuildStayInfoForSave(existingBookingId.GetValueOrDefault());
                        }

                        var request = new BookingDAL.SaveCheckinRequest
                        {
                            RoomId = _room.PhongID,
                            BookingType = bookingType,
                            CheckinAt = checkinNow,
                            GuestDisplayName = tenChinh,
                            CommitRoomState = !_deferCheckinCommit,
                            RentalType = rentalType,
                            StayInfo = stayInfo
                        };
                        int bookingId = await Task.Run(() => _bookingDal.SaveCheckinAtomic(request)).ConfigureAwait(true);

                        _room.TrangThai = (int)RoomStatus.CoKhach;
                        _room.KieuThue = rentalType;
                        _room.ThoiGianBatDau = checkinNow;
                        _room.TenKhachHienThi = tenChinh;

                        if (stayInfo != null)
                        {
                            stayInfo.DatPhongID = bookingId;
                            _loadedStayInfo = stayInfo;
                        }

                        Saved?.Invoke(this, EventArgs.Empty);
                        BackRequested?.Invoke(this, EventArgs.Empty);
                        perf.AddContext("BookingId", bookingId);
                        perf.AddContext("BookingType", bookingType);
                    }
                }).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
                if (btnNhanPhong != null) btnNhanPhong.Enabled = true;
            }
        }

        private bool ValidateForm()
        {
            if (!_isHourlyMode)
                return true;

            if (string.IsNullOrWhiteSpace(GetPrimaryGuestName()))
            {
                MessageBox.Show("Vui lòng nhập Họ tên.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHoTen.Focus();
                return false;
            }

            return true;
        }

        private string GetPrimaryGuestName()
        {
            return (txtHoTen.Text ?? "").Trim();
        }

        private void btnThemKhach_Click(object sender, EventArgs e)
        {
            txtHoTen.SelectAll();
            txtHoTen.Focus();
        }

        private void lstKhach_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Removed guest list UI.
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            txtHoTen.Clear();
            txtHoTen.Focus();
        }

        private BookingDAL.StayInfoRecord BuildStayInfoForSave(int bookingId)
        {
            var existing = ResolveExistingStayInfoForSave(bookingId);
            decimal giaPhong = ParseMoneyToDecimal(txtGiaPhong.Text);
            if (giaPhong <= 0m && existing != null && existing.GiaPhong > 0m)
                giaPhong = existing.GiaPhong;
            if (giaPhong <= 0m)
            {
                try
                {
                    giaPhong = _bookingDal.GetDonGiaNgayByPhong(_room.PhongID);
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("Cannot resolve nightly rate when saving stay info.", new Dictionary<string, object>
                    {
                        ["RoomId"] = _room.PhongID,
                        ["Error"] = ex.Message
                    });
                }
            }
            if (giaPhong <= 0m)
                giaPhong = PricingService.Instance.GetDefaultNightlyRate(_room.LoaiPhongID);

            int soDemLuuTru = existing != null && existing.SoDemLuuTru > 0
                ? existing.SoDemLuuTru
                : 1;

            return new BookingDAL.StayInfoRecord
            {
                DatPhongID = bookingId,
                LyDoLuuTru = cboLyDoLuuTru.SelectedItem == null ? null : cboLyDoLuuTru.SelectedItem.ToString(),
                GioiTinh = null,
                NgaySinh = null,
                LoaiGiayTo = null,
                SoGiayTo = null,
                QuocTich = null,
                NoiCuTru = null,
                LaDiaBanCu = false,
                MaTinhMoi = null,
                MaXaMoi = null,
                MaTinhCu = null,
                MaHuyenCu = null,
                MaXaCu = null,
                DiaChiChiTiet = null,
                GiaPhong = giaPhong,
                SoDemLuuTru = soDemLuuTru,
                GuestListJson = BuildGuestListForSave(existing == null ? null : existing.GuestListJson)
            };
        }

        private BookingDAL.StayInfoRecord ResolveExistingStayInfoForSave(int bookingId)
        {
            if (_loadedStayInfo != null)
                return _loadedStayInfo;

            if (bookingId <= 0) return null;

            try
            {
                return _bookingDal.GetStayInfoByBooking(bookingId);
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Cannot load existing stay info before save.", new Dictionary<string, object>
                {
                    ["BookingId"] = bookingId,
                    ["Error"] = ex.Message
                });
                return null;
            }
        }

        private string BuildGuestListForSave(string existingGuestListJson)
        {
            string primaryName = (txtHoTen.Text ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(primaryName))
                return primaryName;

            return string.IsNullOrWhiteSpace(existingGuestListJson)
                ? string.Empty
                : existingGuestListJson.Trim();
        }

        private string SerializeGuestList()
        {
            var items = new List<string>();
            foreach (var it in lstKhach.Items)
            {
                if (it == null) continue;
                string value = it.ToString();
                if (string.IsNullOrWhiteSpace(value)) continue;
                items.Add(value.Replace("\r", " ").Replace("\n", " ").Trim());
            }
            return string.Join("\n", items);
        }

        private void LoadStayInfoState()
        {
            if (IsDisposed) return;
            if (_isHourlyMode) return;
            if (_room == null || _room.TrangThai != 1) return;
            if (!_deferCheckinCommit && (!_room.KieuThue.HasValue || _room.KieuThue.Value != 1)) return;

            var booking = _bookingDal.GetCurrentBookingByRoom(_room.PhongID);
            if (booking == null || booking.BookingType != 2) return;

            _loadedStayInfo = _bookingDal.GetStayInfoByBooking(booking.DatPhongID);
            if (_loadedStayInfo == null) return;

            SelectComboByText(cboLyDoLuuTru, _loadedStayInfo.LyDoLuuTru);
            if (_loadedStayInfo.GiaPhong > 0m) txtGiaPhong.Text = _loadedStayInfo.GiaPhong.ToString("N0");

            string savedGuestName = ExtractPrimaryGuestName(_loadedStayInfo.GuestListJson);
            if (!string.IsNullOrWhiteSpace(savedGuestName))
                txtHoTen.Text = savedGuestName;
        }

        private static string ExtractPrimaryGuestName(string guestListJson)
        {
            if (string.IsNullOrWhiteSpace(guestListJson))
                return string.Empty;

            string firstLine = guestListJson
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => (x ?? string.Empty).Trim())
                .FirstOrDefault(x => x.Length > 0);

            if (string.IsNullOrWhiteSpace(firstLine))
                return string.Empty;

            int idx = firstLine.IndexOf(GuestStorageSeparator, StringComparison.Ordinal);
            return idx > 0 ? firstLine.Substring(0, idx).Trim() : firstLine;
        }

        private string GetNoiCuTruText()
        {
            if (rdoThuongTru.Checked) return "Thường trú";
            if (rdoTamTru.Checked) return "Tạm trú";
            return "Khác";
        }
        private GuestListEntry BuildGuestEntryFromCurrentForm()
        {
            return new GuestListEntry
            {
                HoTen = (txtHoTen.Text ?? string.Empty).Trim(),
                SoGiayTo = (txtSoGiayTo.Text ?? string.Empty).Trim(),
                GioiTinh = (cboGioiTinh.SelectedItem?.ToString() ?? cboGioiTinh.Text ?? string.Empty).Trim(),
                NgaySinh = dtpNgaySinh.Value,
                LoaiGiayTo = (cboLoaiGiayTo.SelectedItem?.ToString() ?? string.Empty).Trim(),
                QuocTich = (cboQuocTich.SelectedItem?.ToString() ?? string.Empty).Trim(),
                NoiCuTru = GetNoiCuTruText(),
                LaDiaBanCu = rdoDiaBanCu.Checked,
                MaTinhMoi = GetSelectedTinh()?.MaTinh,
                MaXaMoi = GetSelectedXa()?.MaXa,
                MaTinhCu = GetSelectedTinhCu()?.MaTinh,
                MaHuyenCu = GetSelectedHuyenCu()?.MaHuyen,
                MaXaCu = GetSelectedXaCu()?.MaXa,
                DiaChiChiTiet = (txtDiaChiChiTiet.Text ?? string.Empty).Trim()
            };
        }

        private static bool TryGetGuestEntry(object item, out GuestListEntry entry)
        {
            entry = null;
            if (item == null) return false;

            if (item is GuestListEntry ge)
            {
                entry = ge;
                return true;
            }

            entry = ParseGuestEntryFromLegacyText(item.ToString());
            return entry != null;
        }

        private void ApplyGuestEntryToForm(GuestListEntry entry)
        {
            if (entry == null) return;

            if (!string.IsNullOrWhiteSpace(entry.HoTen))
                txtHoTen.Text = entry.HoTen;
            if (!string.IsNullOrWhiteSpace(entry.SoGiayTo))
                txtSoGiayTo.Text = entry.SoGiayTo;
            if (!string.IsNullOrWhiteSpace(entry.GioiTinh))
                SelectComboByText(cboGioiTinh, entry.GioiTinh);
            if (entry.NgaySinh.HasValue)
                dtpNgaySinh.Value = entry.NgaySinh.Value;
            if (!string.IsNullOrWhiteSpace(entry.LoaiGiayTo))
                SelectComboByText(cboLoaiGiayTo, entry.LoaiGiayTo);
            if (!string.IsNullOrWhiteSpace(entry.QuocTich))
                SelectComboByText(cboQuocTich, entry.QuocTich);
            if (!string.IsNullOrWhiteSpace(entry.DiaChiChiTiet))
                txtDiaChiChiTiet.Text = entry.DiaChiChiTiet;

            if (string.Equals(entry.NoiCuTru, "Thường trú", StringComparison.OrdinalIgnoreCase))
                rdoThuongTru.Checked = true;
            else if (string.Equals(entry.NoiCuTru, "Tạm trú", StringComparison.OrdinalIgnoreCase))
                rdoTamTru.Checked = true;
            else if (!string.IsNullOrWhiteSpace(entry.NoiCuTru))
                rdoNoiKhac.Checked = true;

            if (entry.LaDiaBanCu.HasValue)
                rdoDiaBanCu.Checked = entry.LaDiaBanCu.Value;

            if (!string.IsNullOrWhiteSpace(entry.MaTinhMoi))
                SelectTinhByMa(entry.MaTinhMoi);
            if (!string.IsNullOrWhiteSpace(entry.MaXaMoi))
                SelectXaByMa(entry.MaXaMoi);

            bool hasLegacyGeo =
                !string.IsNullOrWhiteSpace(entry.MaTinhCu) ||
                !string.IsNullOrWhiteSpace(entry.MaHuyenCu) ||
                !string.IsNullOrWhiteSpace(entry.MaXaCu);
            if (hasLegacyGeo)
                EnsureLegacyGeoBound();

            if (!string.IsNullOrWhiteSpace(entry.MaTinhCu))
                SelectTinhCuByMa(entry.MaTinhCu);
            if (!string.IsNullOrWhiteSpace(entry.MaHuyenCu))
                SelectHuyenCuByMa(entry.MaHuyenCu);
            if (!string.IsNullOrWhiteSpace(entry.MaXaCu))
                SelectXaCuByMa(entry.MaXaCu);
        }

        private static GuestListEntry ParseGuestEntryFromLegacyText(string text)
        {
            string raw = (text ?? string.Empty).Trim();
            if (raw.Length == 0) return null;

            int idx = raw.IndexOf(GuestStorageSeparator, StringComparison.Ordinal);
            if (idx <= 0)
            {
                return new GuestListEntry
                {
                    HoTen = raw,
                    SoGiayTo = string.Empty
                };
            }

            return new GuestListEntry
            {
                HoTen = raw.Substring(0, idx).Trim(),
                SoGiayTo = raw.Substring(idx + GuestStorageSeparator.Length).Trim()
            };
        }

        private bool SelectTinhByNameContains(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string key = NormalizeForCompare(input);
            for (int i = 0; i < cboTinhThanh.Items.Count; i++)
            {
                var item = cboTinhThanh.Items[i] as GeoComboItem<Tinh>;
                if (item == null || item.Value == null) continue;
                if (!NormalizeForCompare(item.Value.TenTinh).Contains(key) && !key.Contains(NormalizeForCompare(item.Value.TenTinh))) continue;
                cboTinhThanh.SelectedIndex = i;
                return true;
            }
            return false;
        }

        private bool SelectHuyenByNameContains(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string key = NormalizeForCompare(input);
            for (int i = 0; i < cboHuyen.Items.Count; i++)
            {
                var item = cboHuyen.Items[i] as GeoComboItem<Huyen>;
                if (item == null || item.Value == null) continue;
                if (!NormalizeForCompare(item.Value.TenHuyen).Contains(key) && !key.Contains(NormalizeForCompare(item.Value.TenHuyen))) continue;
                cboHuyen.SelectedIndex = i;
                return true;
            }
            return false;
        }

        private bool SelectTinhCuByNameContains(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            EnsureLegacyGeoBound();
            if (_cboTinhThanhCu == null) return false;
            string key = NormalizeForCompare(input);
            for (int i = 0; i < _cboTinhThanhCu.Items.Count; i++)
            {
                var item = _cboTinhThanhCu.Items[i] as GeoComboItem<Tinh>;
                if (item == null || item.Value == null) continue;
                if (!NormalizeForCompare(item.Value.TenTinh).Contains(key) && !key.Contains(NormalizeForCompare(item.Value.TenTinh))) continue;
                _cboTinhThanhCu.SelectedIndex = i;
                return true;
            }
            return false;
        }

        private bool SelectHuyenCuByNameContains(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            EnsureLegacyGeoBound();
            if (_cboHuyenCu == null) return false;
            string key = NormalizeForCompare(input);
            for (int i = 0; i < _cboHuyenCu.Items.Count; i++)
            {
                var item = _cboHuyenCu.Items[i] as GeoComboItem<Huyen>;
                if (item == null || item.Value == null) continue;
                if (!NormalizeForCompare(item.Value.TenHuyen).Contains(key) && !key.Contains(NormalizeForCompare(item.Value.TenHuyen))) continue;
                _cboHuyenCu.SelectedIndex = i;
                return true;
            }
            return false;
        }

        private bool SelectXaByNameContains(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string key = NormalizeForCompare(input);
            for (int i = 0; i < cboPhuongXa.Items.Count; i++)
            {
                var item = cboPhuongXa.Items[i] as GeoComboItem<Xa>;
                if (item == null || item.Value == null) continue;
                if (!NormalizeForCompare(item.Value.TenXa).Contains(key) && !key.Contains(NormalizeForCompare(item.Value.TenXa))) continue;
                cboPhuongXa.SelectedIndex = i;
                return true;
            }
            return false;
        }

        private bool SelectXaCuByNameContains(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            EnsureLegacyGeoBound();
            if (_cboPhuongXaCu == null) return false;
            string key = NormalizeForCompare(input);
            for (int i = 0; i < _cboPhuongXaCu.Items.Count; i++)
            {
                var item = _cboPhuongXaCu.Items[i] as GeoComboItem<Xa>;
                if (item == null || item.Value == null) continue;
                if (!NormalizeForCompare(item.Value.TenXa).Contains(key) && !key.Contains(NormalizeForCompare(item.Value.TenXa))) continue;
                _cboPhuongXaCu.SelectedIndex = i;
                return true;
            }
            return false;
        }

        private void SelectGeoByAddressRaw(string addressRaw)
        {
            if (string.IsNullOrWhiteSpace(addressRaw)) return;

            var raw = NormalizeForCompare(addressRaw);

            if (GetSelectedTinh() == null)
            {
                for (int i = 0; i < cboTinhThanh.Items.Count; i++)
                {
                    var tItem = cboTinhThanh.Items[i] as GeoComboItem<Tinh>;
                    if (tItem == null || tItem.Value == null) continue;
                    if (!raw.Contains(NormalizeForCompare(tItem.Value.TenTinh))) continue;
                    cboTinhThanh.SelectedIndex = i;
                    break;
                }
            }

            if (GetSelectedTinh() != null && GetSelectedXa() == null)
            {
                for (int i = 0; i < cboPhuongXa.Items.Count; i++)
                {
                    var xItem = cboPhuongXa.Items[i] as GeoComboItem<Xa>;
                    if (xItem == null || xItem.Value == null) continue;
                    if (!raw.Contains(NormalizeForCompare(xItem.Value.TenXa))) continue;
                    cboPhuongXa.SelectedIndex = i;
                    break;
                }
            }
        }

        private void cboTinhCu_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding || _isComboFiltering) return;
            try
            {
                BindHuyenCuComboBySelectedTinh(false);
                SyncNewFromOldTinh();
            }
            catch (Exception ex)
            {
                ShowFriendlyError("RoomDetail.BindHuyenCu", "Không thể tải quận/huyện cũ. Vui lòng thử lại.", ex, "Lỗi địa bàn");
            }
        }

        private void cboTinhCu_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cboTinhCu_SelectedIndexChanged(sender, e);
        }

        private void cboTinhCu_Leave(object sender, EventArgs e)
        {
            TrySelectComboByTextContains(_cboTinhThanhCu, _cboTinhThanhCu.Text);
        }

        private void cboTinhCu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            TrySelectComboByTextContains(_cboTinhThanhCu, _cboTinhThanhCu.Text);
        }

        private void cboHuyenCu_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding || _isComboFiltering) return;
            try
            {
                BindXaCuComboBySelectedHuyen(false);
                SyncNewFromOldHuyen();
            }
            catch (Exception ex)
            {
                ShowFriendlyError("RoomDetail.BindXaCu", "Không thể tải phường/xã cũ. Vui lòng thử lại.", ex, "Lỗi địa bàn");
            }
        }

        private void cboHuyenCu_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cboHuyenCu_SelectedIndexChanged(sender, e);
        }

        private void cboHuyenCu_Leave(object sender, EventArgs e)
        {
            TrySelectComboByTextContains(_cboHuyenCu, _cboHuyenCu.Text);
        }

        private void cboHuyenCu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            TrySelectComboByTextContains(_cboHuyenCu, _cboHuyenCu.Text);
        }

        private void cboPhuongXaCu_Leave(object sender, EventArgs e)
        {
            TrySelectComboByTextContains(_cboPhuongXaCu, _cboPhuongXaCu.Text);
        }

        private void cboPhuongXaCu_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding || _isComboFiltering) return;
            SyncNewFromOldXa();
        }

        private void cboPhuongXaCu_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cboPhuongXaCu_SelectedIndexChanged(sender, e);
        }

        private void cboPhuongXaCu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            TrySelectComboByTextContains(_cboPhuongXaCu, _cboPhuongXaCu.Text);
        }

        private void SyncNewFromOldTinh()
        {
            var oldTinh = GetSelectedTinhCu();
            if (oldTinh == null) return;

            string mappedProvince = ResolveNewProvinceFromOldTinh(oldTinh.MaTinh);
            if (string.IsNullOrWhiteSpace(mappedProvince))
                mappedProvince = oldTinh.MaTinh;

            if (!string.IsNullOrWhiteSpace(mappedProvince))
                SelectTinhByMa(mappedProvince);

            ClearNewXaSelection();
        }

        private void SyncNewFromOldHuyen()
        {
            var oldTinh = GetSelectedTinhCu();
            var oldHuyen = GetSelectedHuyenCu();
            if (oldTinh == null || oldHuyen == null) return;

            string mappedProvince;
            string mappedXa;
            if (TryResolveUniqueNewXaByOldDistrict(oldTinh.MaTinh, oldHuyen.MaHuyen, out mappedProvince, out mappedXa))
            {
                SelectMappedNewLocation(mappedProvince, mappedXa);
                return;
            }

            mappedProvince = ResolveNewProvinceFromOldTinh(oldTinh.MaTinh);
            if (string.IsNullOrWhiteSpace(mappedProvince))
                mappedProvince = oldTinh.MaTinh;

            if (!string.IsNullOrWhiteSpace(mappedProvince))
                SelectTinhByMa(mappedProvince);
            ClearNewXaSelection();
        }

        private void SyncNewFromOldXa()
        {
            var oldXa = GetSelectedXaCu();
            var oldTinh = GetSelectedTinhCu();
            if (oldXa == null)
            {
                SyncNewFromOldHuyen();
                return;
            }

            if (TryResolveNewByOldXa(oldXa.MaXa, out var mappedProvince, out var mappedXa))
            {
                SelectMappedNewLocation(mappedProvince, mappedXa);
                return;
            }

            if (oldTinh != null)
            {
                SelectTinhByMa(oldTinh.MaTinh);
                if (!TrySelectComboByTextContains(cboPhuongXa, oldXa.TenXa))
                    ClearNewXaSelection();
            }
        }

        private bool TryResolveNewByOldXa(string oldMaXa, out string newProvinceCode, out string newCommuneCode)
        {
            newProvinceCode = null;
            newCommuneCode = null;
            if (string.IsNullOrWhiteSpace(oldMaXa) || _oldToNewXaMap == null || _oldToNewXaMap.Count == 0)
                return false;

            if (!_oldToNewXaMap.TryGetValue(oldMaXa.Trim(), out var mapped) || mapped == null)
                return false;

            newProvinceCode = mapped.ProvinceCodeNew;
            newCommuneCode = mapped.CommuneCodeNew;
            return !string.IsNullOrWhiteSpace(newCommuneCode);
        }

        private string ResolveNewProvinceFromOldTinh(string oldProvinceCode)
        {
            if (string.IsNullOrWhiteSpace(oldProvinceCode) || _oldToNewXaMap == null || _oldToNewXaMap.Count == 0)
                return string.Empty;

            string oldCode = oldProvinceCode.Trim();
            var most = _oldToNewXaMap.Values
                .Where(x => x != null
                    && string.Equals(x.ProvinceCodeOld, oldCode, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(x.ProvinceCodeNew))
                .GroupBy(x => x.ProvinceCodeNew, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            return most ?? string.Empty;
        }

        private bool TryResolveUniqueNewXaByOldDistrict(string oldProvinceCode, string oldDistrictCode, out string newProvinceCode, out string newCommuneCode)
        {
            newProvinceCode = null;
            newCommuneCode = null;
            if (string.IsNullOrWhiteSpace(oldProvinceCode) || string.IsNullOrWhiteSpace(oldDistrictCode) || _oldToNewXaMap == null || _oldToNewXaMap.Count == 0)
                return false;

            var candidates = _oldToNewXaMap.Values
                .Where(x => x != null
                    && string.Equals(x.ProvinceCodeOld, oldProvinceCode.Trim(), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.DistrictCodeOld, oldDistrictCode.Trim(), StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(x.ProvinceCodeNew)
                    && !string.IsNullOrWhiteSpace(x.CommuneCodeNew))
                .GroupBy(x => (x.ProvinceCodeNew ?? string.Empty).Trim() + "|" + (x.CommuneCodeNew ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            if (candidates.Count != 1)
                return false;

            var parts = candidates[0].Key.Split('|');
            if (parts.Length != 2)
                return false;

            newProvinceCode = parts[0];
            newCommuneCode = parts[1];
            return !string.IsNullOrWhiteSpace(newCommuneCode);
        }

        private void SelectMappedNewLocation(string newProvinceCode, string newCommuneCode)
        {
            bool oldBinding = _isGeoBinding;
            _isGeoBinding = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(newProvinceCode))
                    SelectTinhByMa(newProvinceCode);

                BindXaComboBySelectedTinhForNewMode(false);
                if (!string.IsNullOrWhiteSpace(newCommuneCode))
                {
                    if (!TrySelectComboByKey<Xa>(cboPhuongXa, newCommuneCode, x => x.MaXa))
                        TrySelectComboByTextContains(cboPhuongXa, GetNewNameByCode(newCommuneCode));
                }
            }
            finally
            {
                _isGeoBinding = oldBinding;
            }
        }

        private string GetNewNameByCode(string newCommuneCode)
        {
            if (string.IsNullOrWhiteSpace(newCommuneCode) || _oldToNewXaMap == null || _oldToNewXaMap.Count == 0)
                return string.Empty;

            return _oldToNewXaMap.Values
                .Where(x => x != null && string.Equals(x.CommuneCodeNew, newCommuneCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(x => x.NameViNew)
                .FirstOrDefault() ?? string.Empty;
        }

        private void ClearNewXaSelection()
        {
            if (cboPhuongXa == null) return;
            cboPhuongXa.SelectedIndex = -1;
            cboPhuongXa.Text = string.Empty;
        }

        private void SelectTinhCuByMa(string maTinh)
        {
            if (string.IsNullOrWhiteSpace(maTinh)) return;
            EnsureLegacyGeoBound();
            if (_cboTinhThanhCu == null) return;
            TrySelectComboByKey<Tinh>(_cboTinhThanhCu, maTinh, x => x.MaTinh);
        }

        private void SelectHuyenCuByMa(string maHuyen)
        {
            if (string.IsNullOrWhiteSpace(maHuyen)) return;
            EnsureLegacyGeoBound();
            if (_cboHuyenCu == null) return;
            TrySelectComboByKey<Huyen>(_cboHuyenCu, maHuyen, x => x.MaHuyen);
        }

        private void SelectXaCuByMa(string maXa)
        {
            if (string.IsNullOrWhiteSpace(maXa)) return;
            EnsureLegacyGeoBound();
            if (_cboPhuongXaCu == null) return;
            TrySelectComboByKey<Xa>(_cboPhuongXaCu, maXa, x => x.MaXa);
        }

        private static string NormalizeForCompare(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            for (int i = 0; i < normalized.Length; i++)
            {
                var ch = normalized[i];
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var result = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            result = result.Replace('đ', 'd');
            result = Regex.Replace(result, @"\s+", " ");
            return result.Trim();
        }

        private static void SelectComboByText(ComboBox cbo, string text)
        {
            if (cbo == null || cbo.Items.Count == 0 || string.IsNullOrWhiteSpace(text)) return;
            for (int i = 0; i < cbo.Items.Count; i++) {
                string it = cbo.Items[i]?.ToString() ?? "";
                if (string.Equals(it, text, StringComparison.OrdinalIgnoreCase)) { cbo.SelectedIndex = i; return; }
            }
            cbo.Text = text.Trim();
        }
    }

    // IconHelper class definition
    public static class IconHelper
    {
        private static readonly Color PrimaryColor = Color.FromArgb(25, 118, 210);

        public static Bitmap CreateHomeIcon(int size = 24)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(PrimaryColor))
                {
                    Point[] roof = { new Point(size / 2, 2), new Point(2, size / 2 + 2), new Point(size - 2, size / 2 + 2) };
                    g.FillPolygon(brush, roof);
                    g.FillRectangle(brush, 5, size / 2 + 2, size - 10, size / 2 - 2);
                }
            }
            return bmp;
        }

        public static Bitmap CreateUserIcon(int size = 24)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(PrimaryColor))
                {
                    int headSize = size / 2 - 2;
                    g.FillEllipse(brush, (size - headSize) / 2, 2, headSize, headSize);
                    int bodyH = size / 2;
                    g.FillPie(brush, 2, size - bodyH - 2, size - 4, bodyH * 2, 180, 180);
                }
            }
            return bmp;
        }

        public static Bitmap CreateListIcon(int size = 24)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.None;
                using (var brush = new SolidBrush(PrimaryColor))
                {
                    int h = 4; int gap = 3; int y = 4;
                    g.FillRectangle(brush, 4, y, size - 8, h);
                    g.FillRectangle(brush, 4, y + h + gap, size - 8, h);
                    g.FillRectangle(brush, 4, y + (h + gap) * 2, size - 12, h);
                }
            }
            return bmp;
        }

        public static Bitmap CreateSearchIcon(int size = 16)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.Gray, 2))
                {
                    g.DrawEllipse(pen, 2, 2, 8, 8);
                    g.DrawLine(pen, 9, 9, 13, 13);
                }
            }
            return bmp;
        }
    }
}
