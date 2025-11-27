using System.Windows.Forms;

namespace HotelManagement.Forms
{
    partial class InvoiceForm
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblDatPhong;
        private TextBox txtDatPhongID;
        private Button btnTinhTien;
        private Label lblTongTien;
        private TextBox txtTongTien;
        private Button btnLuuHoaDon;
        private Label lblInfo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblDatPhong = new Label();
            this.txtDatPhongID = new TextBox();
            this.btnTinhTien = new Button();
            this.lblTongTien = new Label();
            this.txtTongTien = new TextBox();
            this.btnLuuHoaDon = new Button();
            this.lblInfo = new Label();
            this.SuspendLayout();
            // 
            // lblDatPhong
            // 
            this.lblDatPhong.AutoSize = true;
            this.lblDatPhong.Location = new System.Drawing.Point(20, 20);
            this.lblDatPhong.Name = "lblDatPhong";
            this.lblDatPhong.Size = new System.Drawing.Size(104, 15);
            this.lblDatPhong.TabIndex = 0;
            this.lblDatPhong.Text = "Mã phiếu đặt phòng";
            // 
            // txtDatPhongID
            // 
            this.txtDatPhongID.Location = new System.Drawing.Point(140, 17);
            this.txtDatPhongID.Name = "txtDatPhongID";
            this.txtDatPhongID.Size = new System.Drawing.Size(120, 23);
            this.txtDatPhongID.TabIndex = 1;
            // 
            // btnTinhTien
            // 
            this.btnTinhTien.Location = new System.Drawing.Point(280, 15);
            this.btnTinhTien.Name = "btnTinhTien";
            this.btnTinhTien.Size = new System.Drawing.Size(80, 27);
            this.btnTinhTien.TabIndex = 2;
            this.btnTinhTien.Text = "Tính tiền";
            this.btnTinhTien.UseVisualStyleBackColor = true;
            this.btnTinhTien.Click += new System.EventHandler(this.btnTinhTien_Click);
            // 
            // lblTongTien
            // 
            this.lblTongTien.AutoSize = true;
            this.lblTongTien.Location = new System.Drawing.Point(20, 230);
            this.lblTongTien.Name = "lblTongTien";
            this.lblTongTien.Size = new System.Drawing.Size(59, 15);
            this.lblTongTien.TabIndex = 3;
            this.lblTongTien.Text = "Tổng tiền";
            // 
            // txtTongTien
            // 
            this.txtTongTien.Location = new System.Drawing.Point(140, 227);
            this.txtTongTien.Name = "txtTongTien";
            this.txtTongTien.Size = new System.Drawing.Size(180, 23);
            this.txtTongTien.TabIndex = 4;
            // 
            // btnLuuHoaDon
            // 
            this.btnLuuHoaDon.Location = new System.Drawing.Point(340, 225);
            this.btnLuuHoaDon.Name = "btnLuuHoaDon";
            this.btnLuuHoaDon.Size = new System.Drawing.Size(90, 27);
            this.btnLuuHoaDon.TabIndex = 5;
            this.btnLuuHoaDon.Text = "Lưu hoá đơn";
            this.btnLuuHoaDon.UseVisualStyleBackColor = true;
            this.btnLuuHoaDon.Click += new System.EventHandler(this.btnLuuHoaDon_Click);
            // 
            // lblInfo
            // 
            this.lblInfo.BorderStyle = BorderStyle.FixedSingle;
            this.lblInfo.Location = new System.Drawing.Point(20, 60);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(410, 150);
            this.lblInfo.TabIndex = 6;
            this.lblInfo.Text = "Thông tin tính tiền sẽ hiển thị ở đây...";
            // 
            // InvoiceForm
            // 
            this.ClientSize = new System.Drawing.Size(454, 271);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnLuuHoaDon);
            this.Controls.Add(this.txtTongTien);
            this.Controls.Add(this.lblTongTien);
            this.Controls.Add(this.btnTinhTien);
            this.Controls.Add(this.txtDatPhongID);
            this.Controls.Add(this.lblDatPhong);
            this.Name = "InvoiceForm";
            this.Text = "Hoá đơn";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
