using System;
using System.Drawing;
using System.Windows.Forms;

namespace HotelManagement.Forms
{
    partial class RoomDetailForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblTitle;
        private Label lblRoomText;
        private Button btnCloseTop;

        private GroupBox grpLuuTru;
        private Label lblNhanPhong;
        private DateTimePicker dtpNhanPhong;
        private Label lblTraPhong;
        private DateTimePicker dtpTraPhong;
        private Label lblLyDo;
        private ComboBox cboLyDoLuuTru;
        private Label lblLoaiPhong;
        private ComboBox cboLoaiPhong;
        private Label lblPhong;
        private ComboBox cboPhong;
        private Label lblGiaPhong;
        private TextBox txtGiaPhong;
        private Label lblGiaPhongDonVi;

        private GroupBox grpKhach;
        private Button btnThemKhach;
        private Button btnLamMoi;
        private Button btnQuetMa;

        private Label lblHoTen;
        private TextBox txtHoTen;
        private Label lblGioiTinh;
        private ComboBox cboGioiTinh;
        private Label lblNgaySinh;
        private DateTimePicker dtpNgaySinh;

        private Label lblLoaiGiayTo;
        private ComboBox cboLoaiGiayTo;
        private Label lblSoGiayTo;
        private TextBox txtSoGiayTo;

        private Label lblQuocTich;
        private ComboBox cboQuocTich;

        private Label lblNoiCuTru;
        private RadioButton rdoThuongTru;
        private RadioButton rdoTamTru;
        private RadioButton rdoNoiKhac;

        private Label lblLoaiDiaBan;
        private RadioButton rdoDiaBanMoi;
        private RadioButton rdoDiaBanCu;

        private Label lblTinhThanh;
        private ComboBox cboTinhThanh;
        private Label lblPhuongXa;
        private ComboBox cboPhuongXa;
        private Label lblDiaChiChiTiet;
        private TextBox txtDiaChiChiTiet;

        private GroupBox grpDanhSachKhach;
        private ListBox lstKhach;

        private Button btnDong;
        private Button btnNhanPhong;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.lblTitle = new Label();
            this.lblRoomText = new Label();
            this.btnCloseTop = new Button();

            this.grpLuuTru = new GroupBox();
            this.lblNhanPhong = new Label();
            this.dtpNhanPhong = new DateTimePicker();
            this.lblTraPhong = new Label();
            this.dtpTraPhong = new DateTimePicker();
            this.lblLyDo = new Label();
            this.cboLyDoLuuTru = new ComboBox();
            this.lblLoaiPhong = new Label();
            this.cboLoaiPhong = new ComboBox();
            this.lblPhong = new Label();
            this.cboPhong = new ComboBox();
            this.lblGiaPhong = new Label();
            this.txtGiaPhong = new TextBox();
            this.lblGiaPhongDonVi = new Label();

            this.grpKhach = new GroupBox();
            this.btnThemKhach = new Button();
            this.btnLamMoi = new Button();
            this.btnQuetMa = new Button();

            this.lblHoTen = new Label();
            this.txtHoTen = new TextBox();
            this.lblGioiTinh = new Label();
            this.cboGioiTinh = new ComboBox();
            this.lblNgaySinh = new Label();
            this.dtpNgaySinh = new DateTimePicker();

            this.lblLoaiGiayTo = new Label();
            this.cboLoaiGiayTo = new ComboBox();
            this.lblSoGiayTo = new Label();
            this.txtSoGiayTo = new TextBox();

            this.lblQuocTich = new Label();
            this.cboQuocTich = new ComboBox();

            this.lblNoiCuTru = new Label();
            this.rdoThuongTru = new RadioButton();
            this.rdoTamTru = new RadioButton();
            this.rdoNoiKhac = new RadioButton();

            this.lblLoaiDiaBan = new Label();
            this.rdoDiaBanMoi = new RadioButton();
            this.rdoDiaBanCu = new RadioButton();

            this.lblTinhThanh = new Label();
            this.cboTinhThanh = new ComboBox();
            this.lblPhuongXa = new Label();
            this.cboPhuongXa = new ComboBox();
            this.lblDiaChiChiTiet = new Label();
            this.txtDiaChiChiTiet = new TextBox();

            this.grpDanhSachKhach = new GroupBox();
            this.lstKhach = new ListBox();

            this.btnDong = new Button();
            this.btnNhanPhong = new Button();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.ClientSize = new Size(1100, 680);
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.Load += new EventHandler(this.RoomDetailForm_Load);

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(33, 33, 33);
            this.lblTitle.Text = "Nhận phòng nhanh";

            this.lblRoomText.AutoSize = true;
            this.lblRoomText.Font = new Font("Segoe UI", 10F);
            this.lblRoomText.ForeColor = Color.Gray;
            this.lblRoomText.Text = "Phòng";

            this.btnCloseTop.Text = "x";
            this.btnCloseTop.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnCloseTop.BackColor = Color.Transparent;
            this.btnCloseTop.FlatStyle = FlatStyle.Flat;
            this.btnCloseTop.FlatAppearance.BorderSize = 0;
            this.btnCloseTop.ForeColor = Color.Gray;
            this.btnCloseTop.Click += new EventHandler(this.btnDong_Click);

            this.grpLuuTru.Text = "Thông tin lưu trú";
            this.grpLuuTru.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            this.lblNhanPhong.AutoSize = true;
            this.lblNhanPhong.Font = new Font("Segoe UI", 9F);
            this.lblNhanPhong.Text = "Thời gian nhận phòng *";

            this.dtpNhanPhong.Font = new Font("Segoe UI", 9F);
            this.dtpNhanPhong.Format = DateTimePickerFormat.Custom;
            this.dtpNhanPhong.CustomFormat = "dd/MM/yyyy HH:mm";
            this.dtpNhanPhong.ShowUpDown = true;

            this.lblTraPhong.AutoSize = true;
            this.lblTraPhong.Font = new Font("Segoe UI", 9F);
            this.lblTraPhong.Text = "Thời gian trả phòng";

            this.dtpTraPhong.Font = new Font("Segoe UI", 9F);
            this.dtpTraPhong.Format = DateTimePickerFormat.Custom;
            this.dtpTraPhong.CustomFormat = "dd/MM/yyyy";
            this.dtpTraPhong.ShowCheckBox = true;
            this.dtpTraPhong.Checked = false;

            this.lblLyDo.AutoSize = true;
            this.lblLyDo.Font = new Font("Segoe UI", 9F);
            this.lblLyDo.Text = "Lý do lưu trú *";

            this.cboLyDoLuuTru.Font = new Font("Segoe UI", 9F);
            this.cboLyDoLuuTru.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblLoaiPhong.AutoSize = true;
            this.lblLoaiPhong.Font = new Font("Segoe UI", 9F);
            this.lblLoaiPhong.Text = "Loại phòng *";

            this.cboLoaiPhong.Font = new Font("Segoe UI", 9F);
            this.cboLoaiPhong.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblPhong.AutoSize = true;
            this.lblPhong.Font = new Font("Segoe UI", 9F);
            this.lblPhong.Text = "Phòng *";

            this.cboPhong.Font = new Font("Segoe UI", 9F);
            this.cboPhong.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblGiaPhong.AutoSize = true;
            this.lblGiaPhong.Font = new Font("Segoe UI", 9F);
            this.lblGiaPhong.Text = "Giá phòng:";

            this.txtGiaPhong.Font = new Font("Segoe UI", 9F);
            this.txtGiaPhong.ReadOnly = true;

            this.lblGiaPhongDonVi.AutoSize = true;
            this.lblGiaPhongDonVi.Font = new Font("Segoe UI", 9F);
            this.lblGiaPhongDonVi.ForeColor = Color.Gray;
            this.lblGiaPhongDonVi.Text = "đ";

            this.grpKhach.Text = "Thông tin khách";
            this.grpKhach.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            this.btnThemKhach.Text = "Thêm khách";
            this.btnThemKhach.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnThemKhach.BackColor = Color.FromArgb(33, 150, 243);
            this.btnThemKhach.ForeColor = Color.White;
            this.btnThemKhach.FlatStyle = FlatStyle.Flat;
            this.btnThemKhach.FlatAppearance.BorderSize = 0;
            this.btnThemKhach.Click += new EventHandler(this.btnThemKhach_Click);

            this.btnLamMoi.Text = "Làm mới";
            this.btnLamMoi.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnLamMoi.BackColor = Color.White;
            this.btnLamMoi.ForeColor = Color.FromArgb(33, 150, 243);
            this.btnLamMoi.FlatStyle = FlatStyle.Flat;
            this.btnLamMoi.FlatAppearance.BorderSize = 1;
            this.btnLamMoi.FlatAppearance.BorderColor = Color.FromArgb(33, 150, 243);
            this.btnLamMoi.Click += new EventHandler(this.btnLamMoi_Click);

            this.btnQuetMa.Text = "Quét mã (F1)";
            this.btnQuetMa.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnQuetMa.BackColor = Color.FromArgb(33, 150, 243);
            this.btnQuetMa.ForeColor = Color.White;
            this.btnQuetMa.FlatStyle = FlatStyle.Flat;
            this.btnQuetMa.FlatAppearance.BorderSize = 0;
            this.btnQuetMa.Click += new EventHandler(this.btnQuetMa_Click);

            this.lblHoTen.AutoSize = true;
            this.lblHoTen.Font = new Font("Segoe UI", 9F);
            this.lblHoTen.Text = "Họ tên *";

            this.txtHoTen.Font = new Font("Segoe UI", 9F);

            this.lblGioiTinh.AutoSize = true;
            this.lblGioiTinh.Font = new Font("Segoe UI", 9F);
            this.lblGioiTinh.Text = "Giới tính *";

            this.cboGioiTinh.Font = new Font("Segoe UI", 9F);
            this.cboGioiTinh.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblNgaySinh.AutoSize = true;
            this.lblNgaySinh.Font = new Font("Segoe UI", 9F);
            this.lblNgaySinh.Text = "Ngày sinh *";

            this.dtpNgaySinh.Font = new Font("Segoe UI", 9F);
            this.dtpNgaySinh.Format = DateTimePickerFormat.Custom;
            this.dtpNgaySinh.CustomFormat = "dd/MM/yyyy";

            this.lblLoaiGiayTo.AutoSize = true;
            this.lblLoaiGiayTo.Font = new Font("Segoe UI", 9F);
            this.lblLoaiGiayTo.Text = "Loại giấy tờ *";

            this.cboLoaiGiayTo.Font = new Font("Segoe UI", 9F);
            this.cboLoaiGiayTo.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblSoGiayTo.AutoSize = true;
            this.lblSoGiayTo.Font = new Font("Segoe UI", 9F);
            this.lblSoGiayTo.Text = "Số giấy tờ *";

            this.txtSoGiayTo.Font = new Font("Segoe UI", 9F);

            this.lblQuocTich.AutoSize = true;
            this.lblQuocTich.Font = new Font("Segoe UI", 9F);
            this.lblQuocTich.Text = "Quốc tịch *";

            this.cboQuocTich.Font = new Font("Segoe UI", 9F);
            this.cboQuocTich.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblNoiCuTru.AutoSize = true;
            this.lblNoiCuTru.Font = new Font("Segoe UI", 9F);
            this.lblNoiCuTru.Text = "Nơi cư trú *";

            this.rdoThuongTru.Font = new Font("Segoe UI", 9F);
            this.rdoThuongTru.Text = "Thường trú";

            this.rdoTamTru.Font = new Font("Segoe UI", 9F);
            this.rdoTamTru.Text = "Tạm trú";

            this.rdoNoiKhac.Font = new Font("Segoe UI", 9F);
            this.rdoNoiKhac.Text = "Khác";

            this.lblLoaiDiaBan.AutoSize = true;
            this.lblLoaiDiaBan.Font = new Font("Segoe UI", 9F);
            this.lblLoaiDiaBan.Text = "Loại địa bàn:";

            this.rdoDiaBanMoi.Font = new Font("Segoe UI", 9F);
            this.rdoDiaBanMoi.Text = "Địa bàn mới";

            this.rdoDiaBanCu.Font = new Font("Segoe UI", 9F);
            this.rdoDiaBanCu.Text = "Địa bàn cũ";

            this.lblTinhThanh.AutoSize = true;
            this.lblTinhThanh.Font = new Font("Segoe UI", 9F);
            this.lblTinhThanh.Text = "Tỉnh/Thành phố *";

            this.cboTinhThanh.Font = new Font("Segoe UI", 9F);
            this.cboTinhThanh.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblPhuongXa.AutoSize = true;
            this.lblPhuongXa.Font = new Font("Segoe UI", 9F);
            this.lblPhuongXa.Text = "Phường/Xã/Đặc khu *";

            this.cboPhuongXa.Font = new Font("Segoe UI", 9F);
            this.cboPhuongXa.DropDownStyle = ComboBoxStyle.DropDownList;

            this.lblDiaChiChiTiet.AutoSize = true;
            this.lblDiaChiChiTiet.Font = new Font("Segoe UI", 9F);
            this.lblDiaChiChiTiet.Text = "Địa chỉ chi tiết *";

            this.txtDiaChiChiTiet.Font = new Font("Segoe UI", 9F);

            this.grpDanhSachKhach.Text = "Danh sách khách";
            this.grpDanhSachKhach.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            this.lstKhach.Font = new Font("Segoe UI", 9F);

            this.btnDong.Text = "Đóng";
            this.btnDong.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnDong.BackColor = Color.White;
            this.btnDong.ForeColor = Color.FromArgb(33, 150, 243);
            this.btnDong.FlatStyle = FlatStyle.Flat;
            this.btnDong.FlatAppearance.BorderSize = 1;
            this.btnDong.FlatAppearance.BorderColor = Color.FromArgb(33, 150, 243);
            this.btnDong.Click += new EventHandler(this.btnDong_Click);

            this.btnNhanPhong.Text = "Nhận phòng";
            this.btnNhanPhong.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnNhanPhong.BackColor = Color.FromArgb(33, 150, 243);
            this.btnNhanPhong.ForeColor = Color.White;
            this.btnNhanPhong.FlatStyle = FlatStyle.Flat;
            this.btnNhanPhong.FlatAppearance.BorderSize = 0;
            this.btnNhanPhong.Click += new EventHandler(this.btnNhanPhong_Click);
        }
    }
}
