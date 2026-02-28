using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;

namespace HotelManagement.Forms
{
    public class ManagementControl : UserControl
    {
        private readonly PricingService _pricingService = PricingService.Instance;
        private readonly RoomDAL _roomDal = new RoomDAL();
        private readonly Func<string> _actorResolver;

        private readonly BindingSource _roomBinding = new BindingSource();
        private DataGridView _roomGrid;

        private NumericUpDown nudDefaultNightSingle;
        private NumericUpDown nudDefaultNightDouble;
        private NumericUpDown nudDefaultDaySingle;
        private NumericUpDown nudDefaultDayDouble;

        private NumericUpDown nudHour1Single;
        private NumericUpDown nudNextHourSingle;
        private NumericUpDown nudThresholdSingle;

        private NumericUpDown nudHour1Double;
        private NumericUpDown nudNextHourDouble;
        private NumericUpDown nudThresholdDouble;

        private NumericUpDown nudCheckoutHour;
        private NumericUpDown nudNightStartHour;
        private NumericUpDown nudGraceSingle;
        private NumericUpDown nudGraceDouble;
        private NumericUpDown nudLateFeeSingle;
        private NumericUpDown nudLateFeeDouble;

        private NumericUpDown nudDrinkSoft;
        private NumericUpDown nudDrinkWater;

        public event EventHandler RoomsChanged;

        public ManagementControl(Func<string> actorResolver)
        {
            _actorResolver = actorResolver ?? (() => AuditContext.ResolveActor(null));
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(246, 249, 253);
            InitializeUi();
            LoadPricingFromService();
            LoadRoomGrid();
        }

        public void ReloadData()
        {
            LoadPricingFromService();
            LoadRoomGrid();
        }

        private void InitializeUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(14, 12, 14, 12),
                BackColor = BackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            root.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Text = "Quản lí",
                Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 53, 89),
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 0);

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f)
            };

            var tabPricing = new TabPage("Giá & phụ thu")
            {
                BackColor = Color.White
            };
            tabPricing.Controls.Add(BuildPricingPanel());

            var tabRooms = new TabPage("Danh sách phòng")
            {
                BackColor = Color.White
            };
            tabRooms.Controls.Add(BuildRoomPanel());

            tabs.TabPages.Add(tabPricing);
            tabs.TabPages.Add(tabRooms);

            root.Controls.Add(tabs, 0, 1);
            Controls.Add(root);
        }

        private Control BuildPricingPanel()
        {
            var wrapper = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(12)
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 1
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

            int row = 0;

            AddSectionTitle(grid, row++, "Giá phòng mặc định");
            AddMoneyRow(grid, row++, "Phòng đêm (đơn)", out nudDefaultNightSingle, "Phòng đêm (đôi)", out nudDefaultNightDouble);
            AddMoneyRow(grid, row++, "Phòng ngày (đơn)", out nudDefaultDaySingle, "Phòng ngày (đôi)", out nudDefaultDayDouble);

            AddSectionTitle(grid, row++, "Tính theo giờ");
            AddMoneyRow(grid, row++, "Giờ đầu (đơn)", out nudHour1Single, "Giờ đầu (đôi)", out nudHour1Double);
            AddMoneyRow(grid, row++, "Từ giờ thứ 2 (đơn)", out nudNextHourSingle, "Từ giờ thứ 2 (đôi)", out nudNextHourDouble);
            AddNumberRow(grid, row++, "Ngưỡng phút làm tròn (đơn)", 0, 59, out nudThresholdSingle, "Ngưỡng phút làm tròn (đôi)", 0, 59, out nudThresholdDouble);

            AddSectionTitle(grid, row++, "Qua đêm");
            AddNumberRow(grid, row++, "Bắt đầu tính phòng đêm từ", 0, 23, out nudNightStartHour, "Giờ checkout chuẩn", 0, 23, out nudCheckoutHour);
            AddNumberRow(grid, row++, "Ân hạn (giờ) phòng đơn", 0, 48, out nudGraceSingle, "Ân hạn (giờ) phòng đôi", 0, 48, out nudGraceDouble);
            AddMoneyRow(grid, row++, "Phụ thu trả trễ (đơn)", out nudLateFeeSingle, "Phụ thu trả trễ (đôi)", out nudLateFeeDouble);

            AddSectionTitle(grid, row++, "Đồ uống");
            AddMoneyRow(grid, row++, "Nước ngọt", out nudDrinkSoft, "Nước suối", out nudDrinkWater);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0)
            };

            var btnSave = new Button
            {
                Text = "Lưu",
                Width = 110,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(43, 67, 188),
                ForeColor = Color.White
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSavePricing_Click;

            var btnDefault = new Button
            {
                Text = "Khôi phục mặc định",
                Width = 170,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(55, 71, 79)
            };
            btnDefault.FlatAppearance.BorderColor = Color.FromArgb(198, 210, 230);
            btnDefault.FlatAppearance.BorderSize = 1;
            btnDefault.Click += BtnRestoreDefaults_Click;

            var btnReload = new Button
            {
                Text = "Reload",
                Width = 110,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(55, 71, 79)
            };
            btnReload.FlatAppearance.BorderColor = Color.FromArgb(198, 210, 230);
            btnReload.FlatAppearance.BorderSize = 1;
            btnReload.Click += (s, e) => LoadPricingFromService();

            actions.Controls.Add(btnSave);
            actions.Controls.Add(btnDefault);
            actions.Controls.Add(btnReload);

            wrapper.Controls.Add(actions);
            wrapper.Controls.Add(grid);
            return wrapper;
        }

        private Control BuildRoomPanel()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };

            var btnAdd = BuildSecondaryButton("Thêm");
            btnAdd.Click += BtnAddRoom_Click;

            var btnEdit = BuildSecondaryButton("Sửa");
            btnEdit.Click += BtnEditRoom_Click;

            var btnDelete = BuildSecondaryButton("Xóa");
            btnDelete.Click += BtnDeleteRoom_Click;

            var btnRefresh = BuildSecondaryButton("Làm mới");
            btnRefresh.Click += (s, e) => LoadRoomGrid();

            actions.Controls.Add(btnAdd);
            actions.Controls.Add(btnEdit);
            actions.Controls.Add(btnDelete);
            actions.Controls.Add(btnRefresh);

            _roomGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false
            };

            _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PhongID",
                HeaderText = "ID",
                Width = 60
            });
            _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "MaPhong",
                HeaderText = "Mã phòng",
                Width = 120
            });
            _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LoaiPhongID",
                HeaderText = "Loại",
                Width = 90
            });
            _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Tang",
                HeaderText = "Tầng",
                Width = 80
            });
            _roomGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TrangThai",
                HeaderText = "Trạng thái",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            _roomGrid.DataSource = _roomBinding;
            _roomGrid.DoubleClick += BtnEditRoom_Click;

            root.Controls.Add(actions, 0, 0);
            root.Controls.Add(_roomGrid, 0, 1);

            return root;
        }

        private void LoadPricingFromService()
        {
            try
            {
                var cfg = _pricingService.GetCurrentPricing();
                nudDefaultNightSingle.Value = ToNumeric(cfg.DefaultNightlySingle, nudDefaultNightSingle.Maximum);
                nudDefaultNightDouble.Value = ToNumeric(cfg.DefaultNightlyDouble, nudDefaultNightDouble.Maximum);
                nudDefaultDaySingle.Value = ToNumeric(cfg.DefaultDailySingle, nudDefaultDaySingle.Maximum);
                nudDefaultDayDouble.Value = ToNumeric(cfg.DefaultDailyDouble, nudDefaultDayDouble.Maximum);

                nudHour1Single.Value = ToNumeric(cfg.HourlySingleHour1, nudHour1Single.Maximum);
                nudNextHourSingle.Value = ToNumeric(cfg.HourlySingleNextHour, nudNextHourSingle.Maximum);
                nudThresholdSingle.Value = ClampDecimal(cfg.HourlySingleThresholdMinutes, nudThresholdSingle.Minimum, nudThresholdSingle.Maximum);

                nudHour1Double.Value = ToNumeric(cfg.HourlyDoubleHour1, nudHour1Double.Maximum);
                nudNextHourDouble.Value = ToNumeric(cfg.HourlyDoubleNextHour, nudNextHourDouble.Maximum);
                nudThresholdDouble.Value = ClampDecimal(cfg.HourlyDoubleThresholdMinutes, nudThresholdDouble.Minimum, nudThresholdDouble.Maximum);

                nudNightStartHour.Value = ClampDecimal(cfg.OvernightNightStartHour, nudNightStartHour.Minimum, nudNightStartHour.Maximum);
                nudCheckoutHour.Value = ClampDecimal(cfg.OvernightCheckoutHour, nudCheckoutHour.Minimum, nudCheckoutHour.Maximum);
                nudGraceSingle.Value = ClampDecimal(cfg.OvernightSingleGraceHours, nudGraceSingle.Minimum, nudGraceSingle.Maximum);
                nudGraceDouble.Value = ClampDecimal(cfg.OvernightDoubleGraceHours, nudGraceDouble.Minimum, nudGraceDouble.Maximum);

                nudLateFeeSingle.Value = ToNumeric(cfg.OvernightSingleLateFee, nudLateFeeSingle.Maximum);
                nudLateFeeDouble.Value = ToNumeric(cfg.OvernightDoubleLateFee, nudLateFeeDouble.Maximum);

                nudDrinkSoft.Value = ToNumeric(cfg.DrinkSoftPrice, nudDrinkSoft.Maximum);
                nudDrinkWater.Value = ToNumeric(cfg.DrinkWaterPrice, nudDrinkWater.Maximum);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải cấu hình giá từ DB.\n\nChi tiết: " + ex.Message,
                    "Lỗi tải cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadRoomGrid()
        {
            try
            {
                var rooms = _roomDal.GetAll()
                    .OrderBy(r => r.Tang)
                    .ThenBy(r => r.MaPhong)
                    .Select(r => new RoomGridRow
                    {
                        PhongID = r.PhongID,
                        MaPhong = r.MaPhong,
                        LoaiPhongID = r.LoaiPhongID,
                        Tang = r.Tang,
                        TrangThai = MapRoomStatus(r.TrangThai),
                        RawRoom = r
                    })
                    .ToList();

                _roomBinding.DataSource = rooms;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách phòng.\n\nChi tiết: " + ex.Message,
                    "Lỗi tải phòng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSavePricing_Click(object sender, EventArgs e)
        {
            try
            {
                var cfg = new PricingService.PricingConfig
                {
                    DefaultNightlySingle = nudDefaultNightSingle.Value,
                    DefaultNightlyDouble = nudDefaultNightDouble.Value,
                    DefaultDailySingle = nudDefaultDaySingle.Value,
                    DefaultDailyDouble = nudDefaultDayDouble.Value,

                    HourlySingleHour1 = nudHour1Single.Value,
                    HourlySingleNextHour = nudNextHourSingle.Value,
                    HourlySingleThresholdMinutes = (int)nudThresholdSingle.Value,

                    HourlyDoubleHour1 = nudHour1Double.Value,
                    HourlyDoubleNextHour = nudNextHourDouble.Value,
                    HourlyDoubleThresholdMinutes = (int)nudThresholdDouble.Value,

                    OvernightNightStartHour = (int)nudNightStartHour.Value,
                    OvernightCheckoutHour = (int)nudCheckoutHour.Value,
                    OvernightSingleGraceHours = (int)nudGraceSingle.Value,
                    OvernightDoubleGraceHours = (int)nudGraceDouble.Value,
                    OvernightSingleLateFee = nudLateFeeSingle.Value,
                    OvernightDoubleLateFee = nudLateFeeDouble.Value,

                    DrinkSoftPrice = nudDrinkSoft.Value,
                    DrinkWaterPrice = nudDrinkWater.Value
                };

                _pricingService.SavePricing(cfg, _actorResolver());
                ToastNotifier.Show(this, "Đã lưu cấu hình giá/phụ thu.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lưu cấu hình thất bại.\n\nChi tiết: " + ex.Message,
                    "Lỗi lưu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestoreDefaults_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show("Khôi phục toàn bộ cấu hình về mặc định?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                _pricingService.RestoreDefaults(_actorResolver());
                LoadPricingFromService();
                ToastNotifier.Show(this, "Đã khôi phục cấu hình mặc định.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Khôi phục mặc định thất bại.\n\nChi tiết: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddRoom_Click(object sender, EventArgs e)
        {
            using (var dlg = new RoomEditorDialog(null))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var room = dlg.BuildRoom();
                if (!ValidateRoomInput(room, null)) return;

                try
                {
                    _roomDal.CreateRoom(room);
                    LoadRoomGrid();
                    RoomsChanged?.Invoke(this, EventArgs.Empty);
                    ToastNotifier.Show(this, "Đã thêm phòng mới.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Thêm phòng thất bại.\n\nChi tiết: " + ex.Message,
                        "Lỗi thêm phòng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnEditRoom_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedRoomRow();
            if (selected == null)
            {
                MessageBox.Show("Hãy chọn một phòng để sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new RoomEditorDialog(selected.RawRoom))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var room = dlg.BuildRoom();
                room.PhongID = selected.RawRoom.PhongID;

                if (!ValidateRoomInput(room, room.PhongID)) return;

                try
                {
                    _roomDal.UpdateRoom(room);
                    LoadRoomGrid();
                    RoomsChanged?.Invoke(this, EventArgs.Empty);
                    ToastNotifier.Show(this, "Đã cập nhật phòng.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cập nhật phòng thất bại.\n\nChi tiết: " + ex.Message,
                        "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDeleteRoom_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedRoomRow();
            if (selected == null)
            {
                MessageBox.Show("Hãy chọn một phòng để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show("Xóa mềm phòng " + selected.MaPhong + "?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                _roomDal.SoftDeleteRoom(selected.PhongID);
                LoadRoomGrid();
                RoomsChanged?.Invoke(this, EventArgs.Empty);
                ToastNotifier.Show(this, "Đã xóa mềm phòng.");
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xóa phòng thất bại.\n\nChi tiết: " + ex.Message,
                    "Lỗi xóa phòng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateRoomInput(Room room, int? excludeRoomId)
        {
            if (room == null)
            {
                MessageBox.Show("Dữ liệu phòng không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            room.MaPhong = (room.MaPhong ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(room.MaPhong))
            {
                MessageBox.Show("Mã phòng không được để trống.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (room.LoaiPhongID != 1 && room.LoaiPhongID != 2)
            {
                MessageBox.Show("Loại phòng chỉ nhận 1 (đơn) hoặc 2 (đôi).", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (room.Tang < 0)
            {
                MessageBox.Show("Tầng phải >= 0.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_roomDal.ExistsRoomCode(room.MaPhong, excludeRoomId))
            {
                MessageBox.Show("Mã phòng đã tồn tại.", "Lỗi trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private RoomGridRow GetSelectedRoomRow()
        {
            if (_roomGrid == null || _roomGrid.CurrentRow == null) return null;
            return _roomGrid.CurrentRow.DataBoundItem as RoomGridRow;
        }

        private static string MapRoomStatus(int status)
        {
            if (status == 0) return "0 - Trống";
            if (status == 1) return "1 - Có khách";
            if (status == 2) return "2 - Chưa dọn";
            return status + " - Khác";
        }

        private static Button BuildSecondaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Width = 110,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(55, 71, 79),
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(198, 210, 230);
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private void AddSectionTitle(TableLayoutPanel grid, int rowIndex, string title)
        {
            grid.RowCount = Math.Max(grid.RowCount, rowIndex + 1);
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(43, 67, 118),
                Margin = new Padding(0, rowIndex == 0 ? 0 : 10, 0, 6)
            };

            grid.Controls.Add(label, 0, rowIndex);
            grid.SetColumnSpan(label, 4);
        }

        private void AddMoneyRow(
            TableLayoutPanel grid,
            int rowIndex,
            string leftLabel,
            out NumericUpDown leftInput,
            string rightLabel,
            out NumericUpDown rightInput)
        {
            leftInput = BuildMoneyNumeric();
            rightInput = BuildMoneyNumeric();
            AddGenericRow(grid, rowIndex, leftLabel, leftInput, rightLabel, rightInput);
        }

        private void AddNumberRow(
            TableLayoutPanel grid,
            int rowIndex,
            string leftLabel,
            decimal leftMin,
            decimal leftMax,
            out NumericUpDown leftInput,
            string rightLabel,
            decimal rightMin,
            decimal rightMax,
            out NumericUpDown rightInput)
        {
            leftInput = BuildNumberNumeric(leftMin, leftMax);
            rightInput = BuildNumberNumeric(rightMin, rightMax);
            AddGenericRow(grid, rowIndex, leftLabel, leftInput, rightLabel, rightInput);
        }

        private void AddGenericRow(TableLayoutPanel grid, int rowIndex, string leftLabel, Control leftInput, string rightLabel, Control rightInput)
        {
            grid.RowCount = Math.Max(grid.RowCount, rowIndex + 1);
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            grid.Controls.Add(BuildFieldLabel(leftLabel), 0, rowIndex);
            grid.Controls.Add(leftInput, 1, rowIndex);

            var rightLabelControl = BuildFieldLabel(rightLabel);
            rightLabelControl.Visible = !string.IsNullOrWhiteSpace(rightLabel);
            rightInput.Visible = !string.IsNullOrWhiteSpace(rightLabel);

            grid.Controls.Add(rightLabelControl, 2, rowIndex);
            grid.Controls.Add(rightInput, 3, rowIndex);
        }

        private static Label BuildFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(73, 82, 97),
                Margin = new Padding(0, 6, 8, 6)
            };
        }

        private static NumericUpDown BuildMoneyNumeric()
        {
            return new NumericUpDown
            {
                DecimalPlaces = 0,
                ThousandsSeparator = true,
                Minimum = 0,
                Maximum = 1000000000,
                Increment = 1000,
                Width = 140,
                Margin = new Padding(0, 3, 0, 3),
                TextAlign = HorizontalAlignment.Right
            };
        }

        private static NumericUpDown BuildNumberNumeric(decimal min, decimal max)
        {
            return new NumericUpDown
            {
                DecimalPlaces = 0,
                ThousandsSeparator = false,
                Minimum = min,
                Maximum = max <= min ? min + 1 : max,
                Increment = 1,
                Width = 140,
                Margin = new Padding(0, 3, 0, 3),
                TextAlign = HorizontalAlignment.Right
            };
        }

        private static decimal ToNumeric(decimal value, decimal max)
        {
            if (value < 0m) value = 0m;
            return value > max ? max : value;
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static string ToMoneyCompact(decimal amount)
        {
            decimal safe = amount < 0m ? 0m : amount;
            return safe.ToString("N0") + "đ";
        }

        private sealed class RoomGridRow
        {
            public int PhongID { get; set; }
            public string MaPhong { get; set; }
            public int LoaiPhongID { get; set; }
            public int Tang { get; set; }
            public string TrangThai { get; set; }
            public Room RawRoom { get; set; }
        }

        private sealed class RoomEditorDialog : Form
        {
            private readonly Room _source;
            private readonly TextBox _txtMaPhong;
            private readonly NumericUpDown _nudLoaiPhong;
            private readonly NumericUpDown _nudTang;
            private readonly NumericUpDown _nudTrangThai;

            public RoomEditorDialog(Room source)
            {
                _source = source;
                Text = source == null ? "Thêm phòng" : "Sửa phòng";
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                Width = 420;
                Height = 290;
                Font = new Font("Segoe UI", 9f);

                var grid = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 5,
                    Padding = new Padding(12)
                };
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));

                _txtMaPhong = new TextBox { Dock = DockStyle.Fill };
                _nudLoaiPhong = new NumericUpDown { Minimum = 1, Maximum = 2, Value = 1, Dock = DockStyle.Left, Width = 120 };
                _nudTang = new NumericUpDown { Minimum = 0, Maximum = 200, Value = 0, Dock = DockStyle.Left, Width = 120 };
                _nudTrangThai = new NumericUpDown { Minimum = 0, Maximum = 2, Value = 0, Dock = DockStyle.Left, Width = 120 };

                AddRow(grid, 0, "Mã phòng", _txtMaPhong);
                AddRow(grid, 1, "Loại phòng (1/2)", _nudLoaiPhong);
                AddRow(grid, 2, "Tầng", _nudTang);
                AddRow(grid, 3, "Trạng thái", _nudTrangThai);

                var actions = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false
                };

                var btnOk = new Button
                {
                    Text = "OK",
                    Width = 90,
                    Height = 30,
                    DialogResult = DialogResult.OK
                };
                btnOk.Click += BtnOk_Click;

                var btnCancel = new Button
                {
                    Text = "Hủy",
                    Width = 90,
                    Height = 30,
                    DialogResult = DialogResult.Cancel
                };

                actions.Controls.Add(btnOk);
                actions.Controls.Add(btnCancel);
                AddRow(grid, 4, string.Empty, actions);

                Controls.Add(grid);

                if (_source != null)
                {
                    _txtMaPhong.Text = _source.MaPhong;
                    _nudLoaiPhong.Value = _source.LoaiPhongID == 2 ? 2 : 1;
                    _nudTang.Value = _source.Tang < 0 ? 0 : _source.Tang;
                    _nudTrangThai.Value = _source.TrangThai < 0 ? 0 : (_source.TrangThai > 2 ? 2 : _source.TrangThai);
                }
            }

            public Room BuildRoom()
            {
                return new Room
                {
                    MaPhong = (_txtMaPhong.Text ?? string.Empty).Trim(),
                    LoaiPhongID = (int)_nudLoaiPhong.Value,
                    Tang = (int)_nudTang.Value,
                    TrangThai = (int)_nudTrangThai.Value
                };
            }

            private void BtnOk_Click(object sender, EventArgs e)
            {
                string code = (_txtMaPhong.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(code))
                {
                    MessageBox.Show("Mã phòng không được để trống.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (_nudLoaiPhong.Value != 1 && _nudLoaiPhong.Value != 2)
                {
                    MessageBox.Show("Loại phòng chỉ nhận 1 hoặc 2.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            }

            private static void AddRow(TableLayoutPanel grid, int row, string label, Control valueControl)
            {
                while (grid.RowStyles.Count <= row)
                    grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                if (!string.IsNullOrEmpty(label))
                {
                    var lbl = new Label
                    {
                        Text = label,
                        AutoSize = true,
                        Margin = new Padding(0, 7, 8, 4)
                    };
                    grid.Controls.Add(lbl, 0, row);
                }
                else
                {
                    grid.Controls.Add(new Label { AutoSize = true }, 0, row);
                }

                valueControl.Margin = new Padding(0, 3, 0, 4);
                grid.Controls.Add(valueControl, 1, row);
            }
        }
    }
}
