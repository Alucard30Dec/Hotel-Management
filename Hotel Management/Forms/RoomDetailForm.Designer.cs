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
        private Label lblSoDienThoai;
        private TextBox txtSoDienThoai;
        
        private Label lblQuocTich;
        private ComboBox cboQuocTich;
        private Label lblGhiChuKhach;
        private TextBox txtGhiChuKhach;

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
        private Label lblNgheNghiep;
        private ComboBox cboNgheNghiep;
        private Label lblNoiLamViec;
        private TextBox txtNoiLamViec;
        
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
            
            this.lblNhanPhong = new Label { Text = "Thời gian nhận phòng *" };
            this.dtpNhanPhong = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
            this.lblTraPhong = new Label { Text = "Thời gian trả phòng" };
            this.dtpTraPhong = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
            this.lblLyDo = new Label { Text = "Lý do lưu trú *" };
            this.cboLyDoLuuTru = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            
            this.lblLoaiPhong = new Label { Text = "Loại phòng *" };
            this.cboLoaiPhong = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.lblPhong = new Label { Text = "Phòng *" };
            this.cboPhong = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.lblGiaPhong = new Label { Text = "Giá phòng" };
            this.txtGiaPhong = new TextBox();

            this.btnThemKhach = new Button { Text = "Thêm khách" };
            this.btnLamMoi = new Button { Text = "Làm mới" };
            this.btnQuetMa = new Button { Text = "Quét CCCD (F1)" };
            
            this.lblHoTen = new Label { Text = "Họ tên *" };
            this.txtHoTen = new TextBox();
            this.lblGioiTinh = new Label { Text = "Giới tính *" };
            this.cboGioiTinh = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.lblNgaySinh = new Label { Text = "Ngày sinh *" };
            this.dtpNgaySinh = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            
            this.lblLoaiGiayTo = new Label { Text = "Loại giấy tờ *" };
            this.cboLoaiGiayTo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.lblSoGiayTo = new Label { Text = "Số giấy tờ *" };
            this.txtSoGiayTo = new TextBox();
            this.lblSoDienThoai = new Label { Text = "Số điện thoại" };
            this.txtSoDienThoai = new TextBox();
            
            this.lblQuocTich = new Label { Text = "Quốc tịch *" };
            this.cboQuocTich = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.lblGhiChuKhach = new Label { Text = "Ghi chú" };
            this.txtGhiChuKhach = new TextBox();

            this.lblNoiCuTru = new Label { Text = "Nơi cư trú *" };
            this.rdoThuongTru = new RadioButton { Text = "Thường trú" };
            this.rdoTamTru = new RadioButton { Text = "Tạm trú" };
            this.rdoNoiKhac = new RadioButton { Text = "Khác" };
            
            this.lblLoaiDiaBan = new Label { Text = "Loại địa bàn:" };
            this.rdoDiaBanMoi = new RadioButton { Text = "Địa bàn mới" };
            this.rdoDiaBanCu = new RadioButton { Text = "Địa bàn cũ" };
            
            this.lblTinhThanh = new Label { Text = "Tỉnh/ Thành phố *" };
            this.cboTinhThanh = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.lblPhuongXa = new Label { Text = "Phường/ Xã/ Đặc khu *" };
            this.cboPhuongXa = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.lblDiaChiChiTiet = new Label { Text = "Địa chỉ chi tiết *" };
            this.txtDiaChiChiTiet = new TextBox();
            this.lblNgheNghiep = new Label { Text = "Nghề nghiệp" };
            this.cboNgheNghiep = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            this.lblNoiLamViec = new Label { Text = "Nơi làm việc" };
            this.txtNoiLamViec = new TextBox();

            this.lstKhach = new ListBox();
            this.btnDong = new Button { Text = "Đóng" };
            this.btnNhanPhong = new Button { Text = "Nhận phòng" };

            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1100, 700);
            this.Load += new EventHandler(this.RoomDetailForm_Load);
        }
    }
}
