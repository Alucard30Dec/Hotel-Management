using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Forms
{
    public partial class RoomDetailForm : Form
    {
        private readonly Room _room;
        private readonly RoomDAL _roomDal = new RoomDAL();
        private readonly BookingDAL _bookingDal = new BookingDAL();

        public event EventHandler BackRequested;
        public event EventHandler Saved;

        private bool _layoutApplied = false;

        public RoomDetailForm(Room room)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            _room = room;

            InitializeComponent();
            ApplyResponsiveLayout();
        }

        private void RoomDetailForm_Load(object sender, EventArgs e)
        {
            lblTitle.Text = "Nhận phòng nhanh";
            lblRoomText.Text = $"{_room.MaPhong} • {( _room.LoaiPhongID == 1 ? "Phòng Đơn" : _room.LoaiPhongID == 2 ? "Phòng Đôi" : "Phòng")}, Tầng {_room.Tang}";

            InitCombos();
            BindRoomInfo();

            dtpNhanPhong.Value = _room.ThoiGianBatDau ?? DateTime.Now;

            LoadStateFromGhiChu(_room.GhiChu);

            if (!string.IsNullOrWhiteSpace(_room.TenKhachHienThi) && string.IsNullOrWhiteSpace(txtHoTen.Text))
                txtHoTen.Text = _room.TenKhachHienThi;
        }

        private void ApplyResponsiveLayout()
        {
            if (_layoutApplied) return;
            _layoutApplied = true;

            SuspendLayout();
            AutoScroll = false;
            BackColor = Color.White;

            // ===== ROOT =====
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12),
                BackColor = Color.White
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // ===== HEADER =====
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                BackColor = Color.White
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var headerText = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0),
                BackColor = Color.White
            };

            lblTitle.AutoSize = true;
            lblRoomText.AutoSize = true;

            headerText.Controls.Add(lblTitle);
            headerText.Controls.Add(lblRoomText);

            btnCloseTop.Text = "x";
            btnCloseTop.AutoSize = true;
            btnCloseTop.FlatStyle = FlatStyle.Flat;
            btnCloseTop.FlatAppearance.BorderSize = 0;
            btnCloseTop.ForeColor = Color.Gray;
            btnCloseTop.BackColor = Color.Transparent;
            btnCloseTop.Margin = new Padding(8, 0, 0, 0);
            btnCloseTop.Click -= btnDong_Click;
            btnCloseTop.Click += new EventHandler(this.btnDong_Click);

            header.Controls.Add(headerText, 0, 0);
            header.Controls.Add(btnCloseTop, 1, 0);

            // ===== CONTENT (SCROLL) =====
            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Margin = new Padding(0, 8, 0, 0)
            };

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                AutoSize = true,
                BackColor = Color.White
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Group: Lưu trú
            grpLuuTru.Dock = DockStyle.Top;
            grpLuuTru.AutoSize = true;
            grpLuuTru.Padding = new Padding(10);
            grpLuuTru.Margin = new Padding(0, 0, 0, 10);

            BuildLuuTruLayout();

            // Group: Khách
            grpKhach.Dock = DockStyle.Top;
            grpKhach.AutoSize = true;
            grpKhach.Padding = new Padding(10);
            grpKhach.Margin = new Padding(0);

            BuildKhachLayout();

            content.Controls.Add(grpLuuTru, 0, 0);
            content.Controls.Add(grpKhach, 0, 1);
            contentHost.Controls.Add(content);

            // ===== FOOTER =====
            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0),
                BackColor = Color.White
            };

            btnNhanPhong.AutoSize = true;
            btnDong.AutoSize = true;
            footer.Controls.Add(btnNhanPhong);
            footer.Controls.Add(btnDong);

            // ===== ASSEMBLE =====
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(contentHost, 0, 1);
            root.Controls.Add(footer, 0, 2);

            Controls.Clear();
            Controls.Add(root);
            AcceptButton = btnNhanPhong;

            ResumeLayout(true);
        }

        private void BuildLuuTruLayout()
        {
            NormalizeInput(dtpNhanPhong);
            NormalizeInput(dtpTraPhong);
            NormalizeInput(cboLyDoLuuTru);
            NormalizeInput(cboLoaiPhong);
            NormalizeInput(cboPhong);
            NormalizeInput(txtGiaPhong);

            dtpNhanPhong.ShowUpDown = true;

            txtGiaPhong.ReadOnly = true;
            txtGiaPhong.TextAlign = HorizontalAlignment.Right;

            lblGiaPhongDonVi.AutoSize = true;
            lblGiaPhongDonVi.ForeColor = Color.Gray;
            lblGiaPhongDonVi.Text = "đ";

            var tbl = NewFormTable();

            AddRow(tbl, lblNhanPhong, dtpNhanPhong);
            AddRow(tbl, lblTraPhong, dtpTraPhong);
            AddRow(tbl, lblLyDo, cboLyDoLuuTru);
            AddRow(tbl, lblLoaiPhong, cboLoaiPhong);
            AddRow(tbl, lblPhong, cboPhong);

            var pricePanel = new Panel { Dock = DockStyle.Top, Height = 28, Margin = new Padding(0, 2, 0, 8) };
            txtGiaPhong.Dock = DockStyle.Fill;
            lblGiaPhongDonVi.Dock = DockStyle.Right;
            lblGiaPhongDonVi.Padding = new Padding(6, 5, 0, 0);
            pricePanel.Controls.Add(txtGiaPhong);
            pricePanel.Controls.Add(lblGiaPhongDonVi);

            AddRow(tbl, lblGiaPhong, pricePanel);

            grpLuuTru.Controls.Clear();
            grpLuuTru.Controls.Add(tbl);
        }

        private void BuildKhachLayout()
        {
            NormalizeInput(txtHoTen);
            NormalizeInput(cboGioiTinh);
            NormalizeInput(dtpNgaySinh);
            NormalizeInput(cboLoaiGiayTo);
            NormalizeInput(txtSoGiayTo);
            NormalizeInput(cboQuocTich);
            NormalizeInput(cboTinhThanh);
            NormalizeInput(cboPhuongXa);
            NormalizeInput(txtDiaChiChiTiet);

            // Buttons bar
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.Transparent
            };
            btnThemKhach.AutoSize = true;
            btnLamMoi.AutoSize = true;
            btnQuetMa.AutoSize = true;
            bar.Controls.Add(btnQuetMa);
            bar.Controls.Add(btnLamMoi);
            bar.Controls.Add(btnThemKhach);

            // Fields table (1 cột label + 1 cột input)
            var fields = NewFormTable();

            AddRow(fields, lblHoTen, txtHoTen);
            AddRow(fields, lblGioiTinh, cboGioiTinh);
            AddRow(fields, lblNgaySinh, dtpNgaySinh);
            AddRow(fields, lblLoaiGiayTo, cboLoaiGiayTo);
            AddRow(fields, lblSoGiayTo, txtSoGiayTo);
            AddRow(fields, lblQuocTich, cboQuocTich);

            var pnlNoiCuTru = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 2, 0, 8)
            };
            rdoThuongTru.AutoSize = true;
            rdoTamTru.AutoSize = true;
            rdoNoiKhac.AutoSize = true;
            pnlNoiCuTru.Controls.Add(rdoThuongTru);
            pnlNoiCuTru.Controls.Add(rdoTamTru);
            pnlNoiCuTru.Controls.Add(rdoNoiKhac);
            AddRow(fields, lblNoiCuTru, pnlNoiCuTru);

            var pnlDiaBan = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 2, 0, 8)
            };
            rdoDiaBanMoi.AutoSize = true;
            rdoDiaBanCu.AutoSize = true;
            pnlDiaBan.Controls.Add(rdoDiaBanMoi);
            pnlDiaBan.Controls.Add(rdoDiaBanCu);
            AddRow(fields, lblLoaiDiaBan, pnlDiaBan);

            AddRow(fields, lblTinhThanh, cboTinhThanh);
            AddRow(fields, lblPhuongXa, cboPhuongXa);
            AddRow(fields, lblDiaChiChiTiet, txtDiaChiChiTiet);

            grpDanhSachKhach.Text = "Danh sách khách";
            grpDanhSachKhach.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpDanhSachKhach.Dock = DockStyle.Top;
            grpDanhSachKhach.Height = 170;
            grpDanhSachKhach.Padding = new Padding(8);
            lstKhach.Dock = DockStyle.Fill;

            grpDanhSachKhach.Controls.Clear();
            grpDanhSachKhach.Controls.Add(lstKhach);

            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true };
            wrapper.Controls.Add(grpDanhSachKhach);
            wrapper.Controls.Add(fields);
            wrapper.Controls.Add(bar);

            grpKhach.Controls.Clear();
            grpKhach.Controls.Add(wrapper);
        }

        private static TableLayoutPanel NewFormTable()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            return tbl;
        }

        private static void AddRow(TableLayoutPanel tbl, Control label, Control input)
        {
            if (tbl == null || label == null || input == null) return;

            int row = tbl.RowCount;
            tbl.RowCount += 1;
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            label.AutoSize = true;
            label.Margin = new Padding(0, 6, 10, 0);
            label.Anchor = AnchorStyles.Left | AnchorStyles.Top;

            input.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            tbl.Controls.Add(label, 0, row);
            tbl.Controls.Add(input, 1, row);
        }

        private static void NormalizeInput(Control c)
        {
            if (c == null) return;

            c.Margin = new Padding(0, 2, 0, 8);
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            if (c is TextBox tb)
            {
                tb.BorderStyle = BorderStyle.FixedSingle;
                tb.Height = 28;
            }
            else if (c is ComboBox cb)
            {
                cb.IntegralHeight = false;
                cb.DropDownHeight = 200;
                cb.Height = 28;
            }
            else if (c is DateTimePicker dp)
            {
                dp.Height = 28;
            }
        }

        private void InitCombos()
        {
            cboLyDoLuuTru.Items.Clear();
            cboLyDoLuuTru.Items.AddRange(new object[] { "-- Chọn --", "Du lịch", "Công tác", "Thăm thân", "Khác" });
            cboLyDoLuuTru.SelectedIndex = 0;

            cboGioiTinh.Items.Clear();
            cboGioiTinh.Items.AddRange(new object[] { "-- Chọn --", "Nam", "Nữ", "Khác" });
            cboGioiTinh.SelectedIndex = 0;

            cboLoaiGiayTo.Items.Clear();
            cboLoaiGiayTo.Items.AddRange(new object[] { "Thẻ CCCD", "CMND", "Hộ chiếu" });
            cboLoaiGiayTo.SelectedIndex = 0;

            cboQuocTich.Items.Clear();
            cboQuocTich.Items.AddRange(new object[]
            {
                "VNM - Việt Nam","USA - United States","KOR - Korea","JPN - Japan","CHN - China",
                "FRA - France","DEU - Germany","GBR - United Kingdom","AUS - Australia","CAN - Canada","OTHER"
            });
            cboQuocTich.SelectedIndex = 0;

            cboTinhThanh.Items.Clear();
            cboTinhThanh.Items.AddRange(new object[]
            {
                "-- Chọn Tỉnh/Thành --","Hà Nội","TP. Hồ Chí Minh","Đà Nẵng","Hải Phòng","Cần Thơ","Khác"
            });
            cboTinhThanh.SelectedIndex = 0;

            cboPhuongXa.Items.Clear();
            cboPhuongXa.Items.AddRange(new object[]
            {
                "-- Chọn Phường/Xã --","Phường 1","Phường 2","Phường 3","Xã 1","Xã 2","Khác"
            });
            cboPhuongXa.SelectedIndex = 0;

            rdoThuongTru.Checked = true;
            rdoDiaBanMoi.Checked = true;

            if (dtpNgaySinh.Value.Year < 1900) dtpNgaySinh.Value = new DateTime(1990, 1, 1);
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

            try
            {
                decimal gia = _bookingDal.GetDonGiaNgayByPhong(_room.PhongID);
                if (gia <= 0)
                    gia = (_room.LoaiPhongID == 1) ? 250000m : 350000m;

                txtGiaPhong.Text = gia.ToString("N0");
            }
            catch
            {
                txtGiaPhong.Text = "0";
            }
        }

        private void btnDong_Click(object sender, EventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnNhanPhong_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            string tenChinh = GetPrimaryGuestName();

            _room.TrangThai = 1;
            _room.KieuThue = (_room.KieuThue.HasValue && _room.KieuThue.Value > 0) ? _room.KieuThue : 1;
            _room.ThoiGianBatDau = dtpNhanPhong.Value;
            _room.TenKhachHienThi = tenChinh;

            string ghiChu = BuildGhiChuForSave();

            _roomDal.UpdateTrangThaiFull(
                _room.PhongID,
                _room.TrangThai,
                ghiChu,
                _room.ThoiGianBatDau,
                _room.KieuThue,
                _room.TenKhachHienThi
            );

            _room.GhiChu = ghiChu;

            Saved?.Invoke(this, EventArgs.Empty);
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateForm()
        {
            if (cboLyDoLuuTru.SelectedIndex <= 0)
            {
                MessageBox.Show("Vui lòng chọn Lý do lưu trú.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboLyDoLuuTru.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(GetPrimaryGuestName()))
            {
                MessageBox.Show("Vui lòng nhập Họ tên.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHoTen.Focus();
                return false;
            }

            if (cboGioiTinh.SelectedIndex <= 0)
            {
                MessageBox.Show("Vui lòng chọn Giới tính.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboGioiTinh.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSoGiayTo.Text))
            {
                MessageBox.Show("Vui lòng nhập Số giấy tờ.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSoGiayTo.Focus();
                return false;
            }

            if (cboTinhThanh.SelectedIndex <= 0 || cboPhuongXa.SelectedIndex <= 0 || string.IsNullOrWhiteSpace(txtDiaChiChiTiet.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ địa chỉ (Tỉnh/Thành, Phường/Xã, Địa chỉ chi tiết).", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (string.IsNullOrWhiteSpace(txtHoTen.Text))
            {
                MessageBox.Show("Vui lòng nhập Họ tên.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHoTen.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSoGiayTo.Text))
            {
                MessageBox.Show("Vui lòng nhập Số giấy tờ.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSoGiayTo.Focus();
                return;
            }

            string display = $"{txtHoTen.Text.Trim()} - {txtSoGiayTo.Text.Trim()}";
            lstKhach.Items.Add(display);
            txtHoTen.SelectAll();
            txtHoTen.Focus();
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            txtHoTen.Text = "";
            cboGioiTinh.SelectedIndex = 0;
            dtpNgaySinh.Value = new DateTime(1990, 1, 1);
            cboLoaiGiayTo.SelectedIndex = 0;
            txtSoGiayTo.Text = "";
            cboQuocTich.SelectedIndex = 0;

            rdoThuongTru.Checked = true;
            rdoDiaBanMoi.Checked = true;

            cboTinhThanh.SelectedIndex = 0;
            cboPhuongXa.SelectedIndex = 0;
            txtDiaChiChiTiet.Text = "";

            lstKhach.Items.Clear();
        }

        private void btnQuetMa_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Chức năng quét mã sẽ được bổ sung sau.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string BuildGhiChuForSave()
        {
            var tags = new List<string>();

            tags.Add("LYDO=" + SafeTagValue(cboLyDoLuuTru.SelectedItem?.ToString()));
            tags.Add("GT=" + SafeTagValue(cboGioiTinh.SelectedItem?.ToString()));
            tags.Add("NS=" + dtpNgaySinh.Value.ToString("yyyyMMdd"));
            tags.Add("LGT=" + SafeTagValue(cboLoaiGiayTo.SelectedItem?.ToString()));
            tags.Add("SGT=" + SafeTagValue(txtSoGiayTo.Text.Trim()));
            tags.Add("QT=" + SafeTagValue(cboQuocTich.SelectedItem?.ToString()));
            tags.Add("NCT=" + SafeTagValue(GetNoiCuTru()));
            tags.Add("LDB=" + SafeTagValue(GetLoaiDiaBan()));
            tags.Add("TINH=" + SafeTagValue(cboTinhThanh.SelectedItem?.ToString()));
            tags.Add("PX=" + SafeTagValue(cboPhuongXa.SelectedItem?.ToString()));
            tags.Add("DC=" + SafeTagValue(txtDiaChiChiTiet.Text.Trim()));

            if (dtpTraPhong.Checked)
                tags.Add("TRAP=" + dtpTraPhong.Value.ToString("yyyyMMdd"));

            if (lstKhach.Items.Count > 0)
            {
                var guests = new List<string>();
                foreach (var it in lstKhach.Items)
                {
                    if (it == null) continue;
                    string s = it.ToString().Trim();
                    if (s.Length > 0) guests.Add(SafeTagValue(s));
                }
                if (guests.Count > 0)
                    tags.Add("DSK=" + string.Join(";", guests));
            }

            return string.Join(" | ", tags);
        }

        private string GetNoiCuTru()
        {
            if (rdoTamTru.Checked) return "Tạm trú";
            if (rdoNoiKhac.Checked) return "Khác";
            return "Thường trú";
        }

        private string GetLoaiDiaBan()
        {
            return rdoDiaBanCu.Checked ? "Địa bàn cũ" : "Địa bàn mới";
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

            string lydo = GetStringTag(ghiChu, "LYDO", "");
            SelectComboByText(cboLyDoLuuTru, lydo);

            string gt = GetStringTag(ghiChu, "GT", "");
            SelectComboByText(cboGioiTinh, gt);

            string ns = GetStringTag(ghiChu, "NS", "");
            if (ns.Length == 8 && DateTime.TryParseExact(ns, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                dtpNgaySinh.Value = dob;

            string lgt = GetStringTag(ghiChu, "LGT", "");
            SelectComboByText(cboLoaiGiayTo, lgt);

            txtSoGiayTo.Text = GetStringTag(ghiChu, "SGT", "");

            string qt = GetStringTag(ghiChu, "QT", "");
            SelectComboByText(cboQuocTich, qt);

            string nct = GetStringTag(ghiChu, "NCT", "");
            string nctLower = (nct ?? "").ToLowerInvariant();
            if (nctLower.Contains("tạm") || nctLower.Contains("táº¡m") || nctLower.Contains("tam"))
                rdoTamTru.Checked = true;
            else if (nctLower.Contains("khác") || nctLower.Contains("khÃ¡c") || nctLower.Contains("khac"))
                rdoNoiKhac.Checked = true;
            else
                rdoThuongTru.Checked = true;

            string ldb = GetStringTag(ghiChu, "LDB", "");
            string ldbLower = (ldb ?? "").ToLowerInvariant();
            if (ldbLower.Contains("cũ") || ldbLower.Contains("cÅ©") || ldbLower.Contains("cu"))
                rdoDiaBanCu.Checked = true;
            else
                rdoDiaBanMoi.Checked = true;

            string tinh = GetStringTag(ghiChu, "TINH", "");
            SelectComboByText(cboTinhThanh, tinh);

            string px = GetStringTag(ghiChu, "PX", "");
            SelectComboByText(cboPhuongXa, px);

            txtDiaChiChiTiet.Text = GetStringTag(ghiChu, "DC", "");

            string trap = GetStringTag(ghiChu, "TRAP", "");
            if (trap.Length == 8 && DateTime.TryParseExact(trap, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var outDate))
            {
                dtpTraPhong.Value = outDate;
                dtpTraPhong.Checked = true;
            }

            string dsk = GetStringTag(ghiChu, "DSK", "");
            if (!string.IsNullOrWhiteSpace(dsk))
            {
                var parts = dsk.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                    lstKhach.Items.Add(p.Trim());
            }
        }

        private static void SelectComboByText(ComboBox cbo, string text)
        {
            if (cbo == null || cbo.Items.Count == 0) return;
            if (string.IsNullOrWhiteSpace(text)) return;

            for (int i = 0; i < cbo.Items.Count; i++)
            {
                string it = cbo.Items[i]?.ToString() ?? "";

                if (string.Equals(it, text, StringComparison.OrdinalIgnoreCase))
                {
                    cbo.SelectedIndex = i;
                    return;
                }

                if (it.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                {
                    cbo.SelectedIndex = i;
                    return;
                }
            }
        }
    }
}
