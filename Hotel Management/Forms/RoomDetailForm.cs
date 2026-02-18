using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;

namespace HotelManagement.Forms
{
    public partial class RoomDetailForm : Form
    {
        private readonly Room _room;
        private readonly RoomDAL _roomDal = new RoomDAL();
        private readonly BookingDAL _bookingDal = new BookingDAL();
        private readonly bool _isEditMode;
        private readonly string _maXaCu;
        private readonly GeoDataLoader _geoDataLoader;

        public event EventHandler BackRequested;
        public event EventHandler Saved;

        private bool _layoutApplied = false;
        private readonly AutoCompleteStringCollection _priceSuggestionSource = new AutoCompleteStringCollection();
        private Timer _nhanPhongTimer;
        private bool _isGiaPhongFormatting;
        private bool _isGeoBinding;
        private bool _isComboFiltering;
        private List<Tinh> _geoTinhs = new List<Tinh>();
        private Tinh _legacyTinh;
        private Huyen _legacyHuyen;
        private Xa _legacyXa;
        private Panel _huyenCellPanel;
        private readonly Dictionary<ComboBox, List<object>> _comboOptionCache = new Dictionary<ComboBox, List<object>>();
        private readonly Dictionary<ComboBox, object> _comboBestMatchCache = new Dictionary<ComboBox, object>();
        private const int SuggestMaxVisibleItems = 10;

        // Colors
        private readonly Color clrHeaderBg = Color.FromArgb(227, 242, 253); 
        private readonly Color clrHeaderText = Color.FromArgb(25, 118, 210); 
        private readonly Color clrPrimary = Color.FromArgb(33, 150, 243);    

        public RoomDetailForm(Room room, bool isEditMode = false, string maXaCu = null, string diaBanJsonPath = @"Address\dvhc_optimized.json")
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            _room = room;
            _isEditMode = isEditMode;
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

            btnThemKhach.Click -= btnThemKhach_Click;
            btnThemKhach.Click += btnThemKhach_Click;

            btnLamMoi.Click -= btnLamMoi_Click;
            btnLamMoi.Click += btnLamMoi_Click;

            btnQuetMa.Click -= btnQuetMa_Click;
            btnQuetMa.Click += btnQuetMa_Click;

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

        private void RoomDetailForm_Load(object sender, EventArgs e)
        {
            lblTitle.Text = "Nhận phòng nhanh";
            lblRoomText.Text = $"{_room.MaPhong} • {(_room.LoaiPhongID == 1 ? "Phòng Đơn" : _room.LoaiPhongID == 2 ? "Phòng Đôi" : "Phòng")}, Tầng {_room.Tang}";
            
            InitCombos();

            try
            {
                LoadGeoDataAndBindInitial();
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Không tìm thấy file địa bàn.\n\n" + ex.Message, "Thiếu dữ liệu địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (JsonException ex)
            {
                MessageBox.Show("Không thể đọc dữ liệu địa bàn.\n\n" + ex.Message, "Lỗi dữ liệu địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải dữ liệu địa bàn.\n\n" + ex.Message, "Lỗi địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                BindRoomInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải thông tin phòng.\n\n" + ex.Message, "Lỗi dữ liệu phòng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetNhanPhongNow();
            StartNhanPhongClock();
            LoadStateFromGhiChu(_room.GhiChu);

            if (!string.IsNullOrWhiteSpace(_room.TenKhachHienThi) && string.IsNullOrWhiteSpace(txtHoTen.Text))
                txtHoTen.Text = _room.TenKhachHienThi;
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
            var pnlLuuTruHeader = CreateSectionHeader("Thông tin lưu trú", IconHelper.CreateHomeIcon()); 
            var pnlLuuTruBody = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
            BuildLuuTruLayout(pnlLuuTruBody);

            // Section 2: Thong tin khach + danh sach khach (layout theo 2 cot)
            var pnlKhachSection = BuildKhachSectionLayout();

            innerContent.Controls.Add(pnlLuuTruHeader, 0, 0);
            innerContent.Controls.Add(pnlLuuTruBody, 0, 1);
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

            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));

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
                Text = "Thông tin khách",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = clrHeaderText,
                AutoSize = true,
                Margin = new Padding(0, 3, 0, 0)
            };
            headerTitle.Controls.Add(khachIcon);
            headerTitle.Controls.Add(khachLabel);

            StyleButton(btnThemKhach, true);
            StyleButton(btnLamMoi, false);
            StyleButton(btnQuetMa, true);
            btnThemKhach.Text = "Thêm khách";
            btnLamMoi.Text = "Làm mới";
            btnQuetMa.Text = "Quét mã (F1)";
            btnThemKhach.Width = 104;
            btnLamMoi.Width = 94;
            btnQuetMa.Width = 114;
            btnThemKhach.Height = 30;
            btnLamMoi.Height = 30;
            btnQuetMa.Height = 30;
            btnThemKhach.Dock = DockStyle.None;
            btnLamMoi.Dock = DockStyle.None;
            btnQuetMa.Dock = DockStyle.None;
            btnThemKhach.Margin = new Padding(0, 0, 8, 0);
            btnLamMoi.Margin = new Padding(0, 0, 8, 0);
            btnQuetMa.Margin = new Padding(0);

            var buttonFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            buttonFlow.Controls.Add(btnThemKhach);
            buttonFlow.Controls.Add(btnLamMoi);
            buttonFlow.Controls.Add(btnQuetMa);

            leftHeaderTable.Controls.Add(headerTitle, 0, 0);
            leftHeaderTable.Controls.Add(buttonFlow, 1, 0);
            leftHeader.Controls.Add(leftHeaderTable);

            var leftBody = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5, 5, 5, 0) };
            BuildKhachLayout(leftBody);
            left.Controls.Add(leftBody);
            left.Controls.Add(leftHeader);

            var right = new Panel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(12, 0, 0, 0) };
            var rightHeader = CreateSectionHeader("Danh sách khách", IconHelper.CreateListIcon());
            rightHeader.Margin = new Padding(0);
            rightHeader.Dock = DockStyle.Top;
            var rightBody = new Panel { Dock = DockStyle.Top, Height = 420, Padding = new Padding(0, 5, 0, 0) };
            lstKhach.Dock = DockStyle.Fill;
            lstKhach.BorderStyle = BorderStyle.FixedSingle;
            lstKhach.Font = new Font("Segoe UI", 10F);
            rightBody.Controls.Add(lstKhach);
            right.Controls.Add(rightBody);
            right.Controls.Add(rightHeader);

            split.Controls.Add(left, 0, 0);
            split.Controls.Add(right, 1, 0);
            section.Controls.Add(split);
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
            AddCell(tbl, lblGiaPhong, txtGiaPhong, 2, 1, 2);
            container.Controls.Add(tbl);
        }

        private void BuildKhachLayout(Panel container)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            for(int i=0; i<3; i++) tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            AddCell(tbl, lblHoTen, txtHoTen, 0, 0);
            AddCell(tbl, lblGioiTinh, cboGioiTinh, 1, 0);
            AddCell(tbl, lblNgaySinh, dtpNgaySinh, 2, 0);

            AddCell(tbl, lblLoaiGiayTo, cboLoaiGiayTo, 0, 1);
            
            var pnlGiayTo = new Panel { Dock = DockStyle.Top, Height = 30, BorderStyle = BorderStyle.FixedSingle };
            var btnSearchGT = new Button 
            { 
                Dock = DockStyle.Right, 
                Width = 30, 
                Image = IconHelper.CreateSearchIcon(),
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.White, 
                Cursor = Cursors.Hand 
            };
            btnSearchGT.FlatAppearance.BorderSize = 0;
            btnSearchGT.Click += btnQuetMa_Click;
            txtSoGiayTo.BorderStyle = BorderStyle.None;
            txtSoGiayTo.Dock = DockStyle.Fill;
            var pnlTxtWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3,5,0,0), BackColor = Color.White };
            pnlTxtWrapper.Controls.Add(txtSoGiayTo);
            pnlGiayTo.Controls.Add(pnlTxtWrapper);
            pnlGiayTo.Controls.Add(btnSearchGT);

            var pCellGT = CreateCellPanel(lblSoGiayTo, pnlGiayTo);
            tbl.Controls.Add(pCellGT, 1, 1);

            AddCell(tbl, lblQuocTich, cboQuocTich, 2, 1);

            var pnlCuTru = CreateRadioPanel(rdoThuongTru, rdoTamTru, rdoNoiKhac);
            AddCell(tbl, lblNoiCuTru, pnlCuTru, 0, 2, 3);

            var pnlDiaBan = CreateRadioPanel(rdoDiaBanMoi, rdoDiaBanCu);
            AddCell(tbl, lblLoaiDiaBan, pnlDiaBan, 0, 3, 3);

            var diaBanHeader = CreateInlineHeader("Địa bàn mới");
            tbl.Controls.Add(diaBanHeader, 0, 4);
            tbl.SetColumnSpan(diaBanHeader, 3);

            AddCell(tbl, lblTinhThanh, cboTinhThanh, 0, 5);
            _huyenCellPanel = CreateCellPanel(lblHuyen, cboHuyen);
            tbl.Controls.Add(_huyenCellPanel, 1, 5);
            AddCell(tbl, lblPhuongXa, cboPhuongXa, 2, 5);

            AddCell(tbl, lblDiaChiChiTiet, txtDiaChiChiTiet, 0, 6, 3);

            container.Controls.Add(tbl);
        }

        private Panel CreateInlineHeader(string title)
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
            cboGioiTinh.Items.Clear();
            cboGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
            cboGioiTinh.SelectedIndex = -1;
            cboGioiTinh.Text = string.Empty;
            ConfigureTypeAheadCombo(cboGioiTinh);
            CacheComboItems(cboGioiTinh);
            cboLoaiGiayTo.Items.Clear();
            cboLoaiGiayTo.Items.AddRange(new object[] { "Thẻ CCCD", "CMND", "Hộ chiếu" });
            cboLoaiGiayTo.SelectedIndex = 0;
            cboQuocTich.Items.Clear();
            cboQuocTich.Items.AddRange(new object[] { "VNM - Việt Nam","USA - United States","KOR - Korea","JPN - Japan","CHN - China", "OTHER" });
            cboQuocTich.SelectedIndex = 0;
            InitAddressCombosUiOnly();
            _isGeoBinding = true;
            try
            {
                rdoThuongTru.Checked = true;
                rdoDiaBanMoi.Checked = true;
            }
            finally
            {
                _isGeoBinding = false;
            }
            UpdateDiaBanUiState();
            if (dtpNgaySinh.Value.Year < 1900) dtpNgaySinh.Value = new DateTime(1990, 1, 1);
            dtpNhanPhong.Enabled = false;
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

        private void LoadGeoDataAndBindInitial()
        {
            _geoTinhs = _geoDataLoader.Load().ToList();
            _legacyTinh = null;
            _legacyHuyen = null;
            _legacyXa = null;

            string legacyMaXa = _maXaCu;
            if (_isEditMode && string.IsNullOrWhiteSpace(legacyMaXa))
                legacyMaXa = GetStringTag(_room?.GhiChu, "DBMAXA", "");

            if (_isEditMode && !string.IsNullOrWhiteSpace(legacyMaXa))
            {
                TryFindXaPath(legacyMaXa, out _legacyTinh, out _legacyHuyen, out _legacyXa);
            }

            BindTinhCombo();
            if (_isEditMode && _legacyTinh != null)
            {
                SelectTinhByMa(_legacyTinh.MaTinh);
                SelectHuyenByMa(_legacyHuyen?.MaHuyen);
                SelectXaByMa(_legacyXa?.MaXa);
            }
        }

        private void ReloadGeoDataKeepingSelection()
        {
            string selectedMaTinh = GetSelectedTinh()?.MaTinh;
            string selectedMaHuyen = GetSelectedHuyen()?.MaHuyen;
            string selectedMaXa = GetSelectedXa()?.MaXa;

            LoadGeoDataAndBindInitial();

            if (!string.IsNullOrWhiteSpace(selectedMaTinh))
                SelectTinhByMa(selectedMaTinh);
            if (!string.IsNullOrWhiteSpace(selectedMaHuyen))
                SelectHuyenByMa(selectedMaHuyen);
            if (!string.IsNullOrWhiteSpace(selectedMaXa))
                SelectXaByMa(selectedMaXa);
        }

        private void cboTinh_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding || _isComboFiltering) return;
            try
            {
                if (rdoDiaBanMoi.Checked)
                    BindXaComboBySelectedTinhForNewMode(false);
                else
                    BindHuyenComboBySelectedTinh(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách địa bàn theo tỉnh.\n\n" + ex.Message, "Lỗi địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            FilterComboByContains(combo);
        }

        private void cboGeo_DropDown(object sender, EventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || _isComboFiltering) return;

            if (string.IsNullOrWhiteSpace(combo.Text))
                RestoreComboItems(combo, preserveText: true);
            else
                FilterComboByContains(combo);
        }

        private void cboHuyen_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding || _isComboFiltering) return;
            if (rdoDiaBanMoi.Checked) return;
            try
            {
                BindXaComboBySelectedHuyen(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách phường/xã.\n\n" + ex.Message, "Lỗi địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cboHuyen_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cboHuyen_SelectedIndexChanged(sender, e);
        }

        private void cboHuyen_Leave(object sender, EventArgs e)
        {
            ApplyTypedHuyenSelection();
        }

        private void cboHuyen_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            ApplyTypedHuyenSelection();
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
            lblPhuongXa.Text = rdoDiaBanCu.Checked
                ? "Xã/Phường (cũ) *"
                : "Xã/Phường/Đặc khu *";

            bool isDiaBanMoi = rdoDiaBanMoi.Checked;
            cboHuyen.Enabled = !isDiaBanMoi;
            if (_huyenCellPanel != null)
            {
                _huyenCellPanel.Visible = !isDiaBanMoi;
                _huyenCellPanel.Enabled = !isDiaBanMoi;
            }
        }

        private void DiaBanLoai_CheckedChanged(object sender, EventArgs e)
        {
            if (_isGeoBinding) return;
            UpdateDiaBanUiState();
            try
            {
                ReloadGeoDataKeepingSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể cập nhật danh sách địa bàn.\n\n" + ex.Message, "Lỗi địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindTinhCombo()
        {
            _isGeoBinding = true;
            try
            {
                string selectedMaTinh = GetSelectedTinh()?.MaTinh;
                cboTinhThanh.Items.Clear();

                var items = _geoTinhs
                    .Where(x => MatchDiaBanType(x.IsActive))
                    .OrderBy(x => x.TenTinh)
                    .ToList();
                if (rdoDiaBanCu.Checked && items.Count == 0)
                {
                    items = _geoTinhs
                        .Where(x => x != null)
                        .OrderBy(x => x.TenTinh)
                        .ToList();
                }

                if (_isEditMode && _legacyTinh != null && !_legacyTinh.IsActive && !items.Any(x => x.MaTinh == _legacyTinh.MaTinh))
                    items.Add(_legacyTinh);

                foreach (var tinh in items.OrderBy(x => x.TenTinh))
                    cboTinhThanh.Items.Add(new GeoComboItem<Tinh>(tinh, BuildDisplayName(tinh.TenTinh, tinh.IsActive)));

                if (!TrySelectComboByKey<Tinh>(cboTinhThanh, selectedMaTinh, t => t.MaTinh))
                {
                    cboTinhThanh.SelectedIndex = -1;
                    cboTinhThanh.Text = string.Empty;
                }

                CacheComboItems(cboTinhThanh);

                if (rdoDiaBanMoi.Checked)
                    BindXaComboBySelectedTinhForNewMode();
                else
                    BindHuyenComboBySelectedTinh();
            }
            finally
            {
                _isGeoBinding = false;
            }
        }

        private void BindXaComboBySelectedTinhForNewMode(bool autoSelectFirstXa = false)
        {
            _isGeoBinding = true;
            try
            {
                string selectedMaXa = GetSelectedXa()?.MaXa;
                var tinh = GetSelectedTinh();

                cboHuyen.Items.Clear();
                cboHuyen.SelectedIndex = -1;
                cboHuyen.Text = string.Empty;
                CacheComboItems(cboHuyen);

                cboPhuongXa.Items.Clear();

                if (tinh != null)
                {
                    var xas = tinh.Huyens
                        .SelectMany(h => h != null ? h.Xas : Enumerable.Empty<Xa>())
                        .Where(x => x != null && MatchDiaBanType(x.IsActive))
                        .GroupBy(x => x.MaXa ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .OrderBy(x => x.TenXa)
                        .ToList();
                    if (xas.Count == 0)
                    {
                        xas = tinh.Huyens
                            .SelectMany(h => h != null ? h.Xas : Enumerable.Empty<Xa>())
                            .Where(x => x != null)
                            .GroupBy(x => x.MaXa ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.First())
                            .OrderBy(x => x.TenXa)
                            .ToList();
                    }

                    if (_isEditMode && _legacyXa != null && !_legacyXa.IsActive &&
                        string.Equals(_legacyTinh?.MaTinh, tinh.MaTinh, StringComparison.OrdinalIgnoreCase) &&
                        !xas.Any(x => string.Equals(x.MaXa, _legacyXa.MaXa, StringComparison.OrdinalIgnoreCase)))
                    {
                        xas.Add(_legacyXa);
                    }

                    foreach (var xa in xas.OrderBy(x => x.TenXa))
                        cboPhuongXa.Items.Add(new GeoComboItem<Xa>(xa, BuildDisplayName(xa.TenXa, xa.IsActive)));
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
                _isGeoBinding = false;
            }
        }

        private void BindHuyenComboBySelectedTinh(bool autoSelectFirstHuyen = false)
        {
            _isGeoBinding = true;
            try
            {
                string selectedMaHuyen = GetSelectedHuyen()?.MaHuyen;
                var tinh = GetSelectedTinh();

                cboHuyen.Items.Clear();

                if (tinh != null)
                {
                    var huyens = tinh.Huyens
                        .Where(x => x != null && MatchDiaBanType(x.IsActive))
                        .OrderBy(x => x.TenHuyen)
                        .ToList();
                    if (rdoDiaBanCu.Checked && huyens.Count == 0)
                    {
                        huyens = tinh.Huyens
                            .Where(x => x != null)
                            .OrderBy(x => x.TenHuyen)
                            .ToList();
                    }

                    if (_isEditMode && _legacyHuyen != null && !_legacyHuyen.IsActive &&
                        string.Equals(_legacyTinh?.MaTinh, tinh.MaTinh, StringComparison.OrdinalIgnoreCase) &&
                        !huyens.Any(x => x.MaHuyen == _legacyHuyen.MaHuyen))
                    {
                        huyens.Add(_legacyHuyen);
                    }

                    foreach (var huyen in huyens.OrderBy(x => x.TenHuyen))
                        cboHuyen.Items.Add(new GeoComboItem<Huyen>(huyen, BuildDisplayName(huyen.TenHuyen, huyen.IsActive)));
                }

                if (!TrySelectComboByKey<Huyen>(cboHuyen, selectedMaHuyen, x => x.MaHuyen))
                {
                    cboHuyen.SelectedIndex = autoSelectFirstHuyen && cboHuyen.Items.Count > 0 ? 0 : -1;
                    if (cboHuyen.SelectedIndex < 0)
                        cboHuyen.Text = string.Empty;
                }

                CacheComboItems(cboHuyen);

                BindXaComboBySelectedHuyen(autoSelectFirstHuyen);
            }
            finally
            {
                _isGeoBinding = false;
            }
        }

        private void BindXaComboBySelectedHuyen(bool autoSelectFirstXa = false)
        {
            _isGeoBinding = true;
            try
            {
                string selectedMaXa = GetSelectedXa()?.MaXa;
                var huyen = GetSelectedHuyen();

                cboPhuongXa.Items.Clear();

                if (huyen != null)
                {
                    var xas = huyen.Xas
                        .Where(x => x != null && MatchDiaBanType(x.IsActive))
                        .OrderBy(x => x.TenXa)
                        .ToList();
                    if (xas.Count == 0)
                    {
                        xas = huyen.Xas
                            .Where(x => x != null)
                            .OrderBy(x => x.TenXa)
                            .ToList();
                    }

                    if (_isEditMode && _legacyXa != null && !_legacyXa.IsActive &&
                        string.Equals(_legacyHuyen?.MaHuyen, huyen.MaHuyen, StringComparison.OrdinalIgnoreCase) &&
                        !xas.Any(x => x.MaXa == _legacyXa.MaXa))
                    {
                        xas.Add(_legacyXa);
                    }

                    foreach (var xa in xas.OrderBy(x => x.TenXa))
                        cboPhuongXa.Items.Add(new GeoComboItem<Xa>(xa, BuildDisplayName(xa.TenXa, xa.IsActive)));
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
                _isGeoBinding = false;
            }
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
            if (string.IsNullOrWhiteSpace(maHuyen)) return;
            TrySelectComboByKey<Huyen>(cboHuyen, maHuyen, x => x.MaHuyen);
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

        private bool MatchDiaBanType(bool isActive)
        {
            return rdoDiaBanCu.Checked ? !isActive : isActive;
        }

        private void ApplyTypedTinhSelection()
        {
            TrySelectComboByTextContains(cboTinhThanh, cboTinhThanh.Text);
        }

        private void ApplyTypedHuyenSelection()
        {
            if (rdoDiaBanMoi.Checked) return;
            TrySelectComboByTextContains(cboHuyen, cboHuyen.Text);
        }

        private void ApplyTypedXaSelection()
        {
            TrySelectComboByTextContains(cboPhuongXa, cboPhuongXa.Text);
        }

        private void ApplyTypedGeoSelections()
        {
            string tinhText = cboTinhThanh.Text;
            string huyenText = cboHuyen.Text;
            string xaText = cboPhuongXa.Text;

            TrySelectComboByTextContains(cboTinhThanh, tinhText);
            if (!rdoDiaBanMoi.Checked)
                TrySelectComboByTextContains(cboHuyen, huyenText);
            TrySelectComboByTextContains(cboPhuongXa, xaText);
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
            var item = cboHuyen.SelectedItem as GeoComboItem<Huyen>;
            return item?.Value;
        }

        private Xa GetSelectedXa()
        {
            var item = cboPhuongXa.SelectedItem as GeoComboItem<Xa>;
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
            decimal gia = _bookingDal.GetDonGiaNgayByPhong(_room.PhongID);
            if (gia <= 0) gia = (_room.LoaiPhongID == 1) ? 250000m : 350000m;
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

        private void RoomDetailForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopNhanPhongClock();
        }

        private void btnDong_Click(object sender, EventArgs e) { BackRequested?.Invoke(this, EventArgs.Empty); }

        private void RoomDetailForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.F1) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            btnQuetMa_Click(sender, EventArgs.Empty);
        }

        private void btnQuetMa_Click(object sender, EventArgs e)
        {
            using (var scanForm = new FrmCccdScan())
            {
                if (scanForm.ShowDialog(this) != DialogResult.OK || scanForm.ResultInfo == null)
                    return;

                ApplyCccdInfoToCheckinForm(scanForm.ResultInfo);
            }

            txtHoTen.Focus();
            txtHoTen.SelectAll();
            MessageBox.Show("Đã áp dụng dữ liệu CCCD lên phiếu nhận phòng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void btnNhanPhong_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            string tenChinh = GetPrimaryGuestName();
            var checkinNow = DateTime.Now;
            dtpNhanPhong.Value = checkinNow;
            _room.TrangThai = 1; 
            _room.KieuThue = (_room.KieuThue.HasValue && _room.KieuThue.Value > 0) ? _room.KieuThue : 1;
            _room.ThoiGianBatDau = checkinNow;
            _room.TenKhachHienThi = tenChinh;
            string ghiChu = BuildGhiChuForSave();
            _roomDal.UpdateTrangThaiFull(_room.PhongID, _room.TrangThai, ghiChu, _room.ThoiGianBatDau, _room.KieuThue, _room.TenKhachHienThi);
            _room.GhiChu = ghiChu;
            Saved?.Invoke(this, EventArgs.Empty);
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateForm()
        {
            TrySelectComboByTextContains(cboGioiTinh, cboGioiTinh.Text);
            ApplyTypedGeoSelections();

            if (cboLyDoLuuTru.SelectedIndex <= 0) { MessageBox.Show("Vui lòng chọn Lý do lưu trú.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); cboLyDoLuuTru.Focus(); return false; }
            if (string.IsNullOrWhiteSpace(GetPrimaryGuestName())) { MessageBox.Show("Vui lòng nhập Họ tên.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtHoTen.Focus(); return false; }

            if (GetSelectedTinh() == null)
            {
                MessageBox.Show("Vui lòng chọn Tỉnh/Thành phố.", "Thiếu thông tin địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboTinhThanh.Focus();
                return false;
            }
            if (rdoDiaBanCu.Checked && GetSelectedHuyen() == null)
            {
                MessageBox.Show("Vui lòng chọn Quận/Huyện.", "Thiếu thông tin địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboHuyen.Focus();
                return false;
            }
            if (GetSelectedXa() == null)
            {
                MessageBox.Show(rdoDiaBanMoi.Checked
                        ? "Vui lòng chọn Xã/Phường/Đặc khu."
                        : "Vui lòng chọn Xã/Phường.",
                    "Thiếu thông tin địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboPhuongXa.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtDiaChiChiTiet.Text))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ chi tiết.", "Thiếu thông tin địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDiaChiChiTiet.Focus();
                return false;
            }
            return true;
        }

        private string GetPrimaryGuestName()
        {
            if (lstKhach.Items.Count > 0)
            {
                string item = lstKhach.Items[0]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(item))
                {
                    int idx = item.IndexOf(" - ", StringComparison.Ordinal);
                    return idx > 0 ? item.Substring(0, idx).Trim() : item.Trim();
                }
            }
            return (txtHoTen.Text ?? "").Trim();
        }

        private void btnThemKhach_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHoTen.Text)) return;
            string display = $"{txtHoTen.Text.Trim()} - {txtSoGiayTo.Text.Trim()}";
            lstKhach.Items.Add(display);
            txtHoTen.SelectAll();
            txtHoTen.Focus();
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            txtHoTen.Text = "";
            cboGioiTinh.SelectedIndex = -1;
            cboGioiTinh.Text = string.Empty;
            dtpNgaySinh.Value = new DateTime(1990, 1, 1);
            txtSoGiayTo.Text = "";
            try
            {
                LoadGeoDataAndBindInitial();
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Không tìm thấy file địa bàn.\n\n" + ex.Message, "Thiếu dữ liệu địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show("Không thể đọc dữ liệu địa bàn.\n\n" + ex.Message, "Lỗi dữ liệu địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải lại dữ liệu địa bàn.\n\n" + ex.Message, "Lỗi địa bàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            cboTinhThanh.SelectedIndex = -1;
            cboTinhThanh.Text = string.Empty;
            cboHuyen.SelectedIndex = -1;
            cboHuyen.Text = string.Empty;
            cboPhuongXa.SelectedIndex = -1;
            cboPhuongXa.Text = string.Empty;
            txtDiaChiChiTiet.Text = "";
        }

        private string BuildGhiChuForSave()
        {
            var tags = new List<string>();
            tags.Add("LYDO=" + SafeTagValue(cboLyDoLuuTru.SelectedItem?.ToString()));
            tags.Add("GT=" + SafeTagValue(cboGioiTinh.SelectedItem?.ToString() ?? cboGioiTinh.Text));
            tags.Add("NS=" + dtpNgaySinh.Value.ToString("yyyyMMdd"));
            tags.Add("LGT=" + SafeTagValue(cboLoaiGiayTo.SelectedItem?.ToString()));
            tags.Add("SGT=" + SafeTagValue(txtSoGiayTo.Text.Trim()));
            tags.Add("QT=" + SafeTagValue(cboQuocTich.SelectedItem?.ToString()));
            tags.Add("NOICUTRU=" + SafeTagValue(GetNoiCuTruText()));
            tags.Add("LOAIDB=" + SafeTagValue(rdoDiaBanCu.Checked ? "Địa bàn cũ" : "Địa bàn mới"));
            var selectedTinh = GetSelectedTinh();
            var selectedHuyen = GetSelectedHuyen();
            var selectedXa = GetSelectedXa();
            tags.Add("DBMATINH=" + SafeTagValue(selectedTinh?.MaTinh));
            tags.Add("DBMAHUYEN=" + SafeTagValue(selectedHuyen?.MaHuyen));
            tags.Add("DBMAXA=" + SafeTagValue(selectedXa?.MaXa));
            tags.Add("DBTINH=" + SafeTagValue(selectedTinh?.TenTinh));
            tags.Add("DBHUYEN=" + SafeTagValue(selectedHuyen?.TenHuyen));
            tags.Add("DBXA=" + SafeTagValue(selectedXa?.TenXa));
            tags.Add("DBDCCT=" + SafeTagValue(txtDiaChiChiTiet.Text.Trim()));
            tags.Add("GIA=" + ParseMoneyToDecimal(txtGiaPhong.Text).ToString("0"));
            if (dtpTraPhong.Checked) tags.Add("TRAP=" + dtpTraPhong.Value.ToString("yyyyMMdd"));
            if (lstKhach.Items.Count > 0)
            {
                var guests = new List<string>();
                foreach (var it in lstKhach.Items) {
                    if (it == null) continue;
                    string s = it.ToString().Trim();
                    if (s.Length > 0) guests.Add(SafeTagValue(s));
                }
                if (guests.Count > 0) tags.Add("DSK=" + string.Join(";", guests));
            }
            return string.Join(" | ", tags);
        }

        private string GetNoiCuTruText()
        {
            if (rdoThuongTru.Checked) return "Thường trú";
            if (rdoTamTru.Checked) return "Tạm trú";
            return "Khác";
        }

        private static string SafeTagValue(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "";
            s = s.Replace("|", "/");
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private static string GetStringTag(string text, string key, string defaultVal)
        {
            if (string.IsNullOrEmpty(text)) return defaultVal;
            Match m = Regex.Match(text, @"\b" + Regex.Escape(key) + @"\s*=\s*([^|]+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.Trim();
            return defaultVal;
        }

        private void LoadStateFromGhiChu(string ghiChu)
        {
            if (string.IsNullOrWhiteSpace(ghiChu)) return;
            string lyDo = GetStringTag(ghiChu, "LYDO", "");
            if (string.Equals(lyDo, "Khác", StringComparison.OrdinalIgnoreCase)) lyDo = "Mục đích khác";
            SelectComboByText(cboLyDoLuuTru, lyDo);
            SelectComboByText(cboGioiTinh, GetStringTag(ghiChu, "GT", ""));
            string ns = GetStringTag(ghiChu, "NS", "");
            if (ns.Length == 8 && DateTime.TryParseExact(ns, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob)) dtpNgaySinh.Value = dob;
            txtSoGiayTo.Text = GetStringTag(ghiChu, "SGT", "");

            string noiCuTru = GetStringTag(ghiChu, "NOICUTRU", "");
            if (string.Equals(noiCuTru, "Thường trú", StringComparison.OrdinalIgnoreCase)) rdoThuongTru.Checked = true;
            else if (string.Equals(noiCuTru, "Tạm trú", StringComparison.OrdinalIgnoreCase)) rdoTamTru.Checked = true;
            else if (!string.IsNullOrWhiteSpace(noiCuTru)) rdoNoiKhac.Checked = true;

            string loaiDiaBan = GetStringTag(ghiChu, "LOAIDB", "");
            if (string.Equals(loaiDiaBan, "Địa bàn cũ", StringComparison.OrdinalIgnoreCase)) rdoDiaBanCu.Checked = true;
            else if (string.Equals(loaiDiaBan, "Địa bàn mới", StringComparison.OrdinalIgnoreCase)) rdoDiaBanMoi.Checked = true;

            string maTinh = GetStringTag(ghiChu, "DBMATINH", "");
            string maHuyen = GetStringTag(ghiChu, "DBMAHUYEN", "");
            string maXa = GetStringTag(ghiChu, "DBMAXA", "");
            string tenTinh = GetStringTag(ghiChu, "DBTINH", "");
            string tenHuyen = GetStringTag(ghiChu, "DBHUYEN", "");
            string tenXa = GetStringTag(ghiChu, "DBXA", "");
            if (string.IsNullOrWhiteSpace(tenXa))
                tenXa = GetStringTag(ghiChu, "DBPX", "");

            if (!string.IsNullOrWhiteSpace(maTinh)) SelectTinhByMa(maTinh);
            else if (!string.IsNullOrWhiteSpace(tenTinh)) SelectTinhByNameContains(tenTinh);

            if (!string.IsNullOrWhiteSpace(maHuyen)) SelectHuyenByMa(maHuyen);
            else if (!string.IsNullOrWhiteSpace(tenHuyen)) SelectHuyenByNameContains(tenHuyen);

            if (!string.IsNullOrWhiteSpace(maXa)) SelectXaByMa(maXa);
            else if (!string.IsNullOrWhiteSpace(tenXa)) SelectXaByNameContains(tenXa);

            string diaChiChiTiet = GetStringTag(ghiChu, "DBDCCT", "");
            if (!string.IsNullOrWhiteSpace(diaChiChiTiet))
                txtDiaChiChiTiet.Text = diaChiChiTiet;

            string gia = GetStringTag(ghiChu, "GIA", "");
            if (!string.IsNullOrWhiteSpace(gia))
            {
                var giaDecimal = ParseMoneyToDecimal(gia);
                txtGiaPhong.Text = giaDecimal.ToString("N0");
            }
            string dsk = GetStringTag(ghiChu, "DSK", "");
            if (!string.IsNullOrWhiteSpace(dsk)) {
                var parts = dsk.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts) lstKhach.Items.Add(p.Trim());
            }
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

            if (rdoDiaBanMoi.Checked && GetSelectedTinh() != null && GetSelectedXa() == null)
            {
                for (int i = 0; i < cboPhuongXa.Items.Count; i++)
                {
                    var xItem = cboPhuongXa.Items[i] as GeoComboItem<Xa>;
                    if (xItem == null || xItem.Value == null) continue;
                    if (!raw.Contains(NormalizeForCompare(xItem.Value.TenXa))) continue;
                    cboPhuongXa.SelectedIndex = i;
                    break;
                }
                return;
            }

            if (GetSelectedTinh() != null && GetSelectedHuyen() == null)
            {
                for (int i = 0; i < cboHuyen.Items.Count; i++)
                {
                    var hItem = cboHuyen.Items[i] as GeoComboItem<Huyen>;
                    if (hItem == null || hItem.Value == null) continue;
                    if (!raw.Contains(NormalizeForCompare(hItem.Value.TenHuyen))) continue;
                    cboHuyen.SelectedIndex = i;
                    break;
                }
            }

            if (GetSelectedHuyen() != null && GetSelectedXa() == null)
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
