# Performance Baseline + Optimization Execution Guide

## 1) Baseline plan (bắt buộc)

### Build/config
- Build `Release` + chạy ngoài debugger (Ctrl+F5) để tránh méo số liệu.
- DB dùng cùng dataset cho tất cả lần đo.
- Mỗi scenario đo ít nhất 5 lần, bỏ 1 lần outlier cao nhất/thấp nhất, lấy trung bình.

### Scenarios cần đo
1. `Cold start`: đóng app hoàn toàn, mở lần đầu.
2. `Warm start`: đóng/mở lại ngay sau cold start.
3. Mở màn hình chính:
- `RoomMap` (`MainForm` mặc định)
- `BookingStatistics` (`ShowBookingStatistics`)
- `RevenueReport` (`ShowRevenueReport`)
- `Management` (`ShowManagement`)
4. Thao tác nặng:
- Nhận phòng nhanh (`RoomDetailForm.btnNhanPhong_Click`)
- Lưu checkout giờ (`HourlyCheckoutForm.BtnSave_Click`)
- Thanh toán checkout giờ (`HourlyCheckoutForm.BtnPay_Click`)
- Lưu checkout đêm (`OvernightCheckoutForm.BtnSave_Click`)
- Thanh toán checkout đêm (`OvernightCheckoutForm.BtnPay_Click`)
- Tải thống kê (`RequestLoadBookingStatisticsData`)
- Tải KPI (`LoadKpiDashboardData`)
- Tải explorer (`LoadExplorerData`)
- Tải audit/alerts (`LoadAuditAndAlerts`)
- Export CSV (`ExportRevenueCsv`)

### Công cụ đo
- Visual Studio Performance Profiler:
  - `CPU Usage`
  - `.NET Object Allocation Tracking`
  - `Memory Usage`
- Windows counters (khi cần):
  - `% Processor Time` (process)
  - `Private Bytes`
  - `.NET CLR Memory` (Gen 0/1/2 collections, LOH)
- App-level PerfScope log (đã thêm sẵn):
  - log file: `bin/Release/logs/app-YYYYMMDD.log`
  - parse bằng script: `tools/perf/perf-log-report.ps1`

### Cách chạy script parse hotspot
```powershell
pwsh ./tools/perf/perf-log-report.ps1 -LogPath "./bin/Release/logs/app-20260224.log"
```
Output:
- `tools/perf/out/perfscope-raw.csv`
- `tools/perf/out/perfscope-summary.csv`
- `tools/perf/out/hotspots-top10.csv`

### Baseline table template
| Scenario | Before(ms) | CPU(%) | Alloc(MB) | Memory Peak(MB) | Notes |
|---|---:|---:|---:|---:|---|
| Cold start |  |  |  |  |  |
| Warm start |  |  |  |  |  |
| Open BookingStatistics |  |  |  |  |  |
| Open RevenueReport |  |  |  |  |  |
| Save Hourly Checkout |  |  |  |  |  |
| Pay Hourly Checkout |  |  |  |  |  |
| Save Overnight Checkout |  |  |  |  |  |
| Pay Overnight Checkout |  |  |  |  |  |
| Export CSV |  |  |  |  |  |

## 2) Performance map (path -> class -> method/event -> logic)

### Startup
- `Program.cs` -> `Program` -> `Main(...)`:
  - Bootstrap WinForms + DB initialize + schema/index ensure.
- `Program.cs` -> `Program` -> `EnsurePerformanceIndexes()`:
  - tạo index nếu chưa có.

### UI / load screen
- `Forms/MainForm.cs` -> `MainForm` -> `MainForm_Load`:
  - init UI + load RoomMap + timers.
- `Forms/MainForm.cs` -> `MainForm` -> `LoadRoomTilesAsync`:
  - lấy danh sách phòng + billing snapshot + dựng control theo tầng.
- `Forms/MainForm.cs` -> `MainForm` -> `RequestLoadBookingStatisticsData`:
  - fingerprint + truy vấn thống kê + bind grid.
- `Forms/MainForm.cs` -> `MainForm` -> `LoadKpiDashboardData`:
  - truy vấn KPI dashboard + bind nhiều grid.
- `Forms/MainForm.cs` -> `MainForm` -> `LoadExplorerData`:
  - truy vấn explorer có phân trang + map DTO/grid.
- `Forms/MainForm.cs` -> `MainForm` -> `LoadExplorerDetailForSelection`:
  - truy vấn detail booking theo row chọn.
- `Forms/MainForm.cs` -> `MainForm` -> `LoadAuditAndAlerts`:
  - truy vấn audit log + alerts.
- `Forms/MainForm.cs` -> `MainForm` -> `LoadRevenueReportData`:
  - truy vấn report + build dữ liệu grid.
- `Forms/MainForm.cs` -> `MainForm` -> `ExportRevenueCsv`:
  - truy vấn report + ghi CSV.

### Business heavy actions
- `Forms/RoomDetailForm.cs` -> `RoomDetailForm` -> `btnNhanPhong_Click`:
  - lưu checkin atomic + stay info.
- `Forms/HourlyCheckoutForm.cs` -> `HourlyCheckoutForm` -> `BtnSave_Click` / `BtnPay_Click`:
  - lưu phát sinh + tính/thu tiền + cập nhật trạng thái phòng.
- `Forms/OvernightCheckoutForm.cs` -> `OvernightCheckoutForm` -> `BtnSave_Click` / `BtnPay_Click`:
  - lưu qua đêm + tính/thu tiền + cập nhật trạng thái.

### Data / DB hotspots
- `Data/RoomDAL.cs` -> `RoomDAL` -> `GetRoomStateFingerprint()`:
  - aggregate fingerprint 1 query (COUNT/SUM/MAX/CRC32).
- `Data/BookingDAL.cs` -> `BookingDAL` -> `GetBookingStatisticsFingerprint(...)`:
  - aggregate fingerprint cho booking/invoice/extras.
- `Data/BookingDAL.cs` -> `BookingDAL` -> `GetBookingStatistics(...)`:
  - truy vấn thống kê + join aggregates.

## 3) Hotspot analysis (từ trace)

Sau khi chạy profiler + script `hotspots-top10.csv`, điền bảng:

| Rank | Path | Class | Method | Why slow | Fix đề xuất | Impact ước lượng | Risk |
|---:|---|---|---|---|---|---|---|
| 1 |  |  |  |  |  |  |  |
| 2 |  |  |  |  |  |  |  |
| 3 |  |  |  |  |  |  |  |
| 4 |  |  |  |  |  |  |  |
| 5 |  |  |  |  |  |  |  |
| 6 |  |  |  |  |  |  |  |
| 7 |  |  |  |  |  |  |  |
| 8 |  |  |  |  |  |  |  |
| 9 |  |  |  |  |  |  |  |
| 10 |  |  |  |  |  |  |  |

## 4) Changesets (impact cao -> thấp)

### CS-01: Instrumentation & baseline visibility
- Mục tiêu: đo được startup/screen/action nặng theo `PerfScope`.
- Files:
  - `Services/PerformanceTracker.cs`
  - `Program.cs`
  - `Forms/MainForm.cs`
  - `Forms/RoomDetailForm.cs`
  - `Forms/HourlyCheckoutForm.cs`
  - `Forms/OvernightCheckoutForm.cs`
- Rủi ro: overhead logging nhẹ.
- Rollback: remove `PerformanceTracker.Measure(...)` callsites.
- Đo lại:
  - Verify có đủ `PerfScope` lines cho startup + action nặng.

### CS-02: Fix realtime reload loop + giảm block UI RoomMap
- Mục tiêu: giảm reload lặp, giảm freeze khi map phòng realtime.
- Files:
  - `Forms/MainForm.cs` (`LoadRoomTilesAsync`, `CheckRoomMapChangesAsync`)
  - `Data/RoomDAL.cs` (`GetRoomStateFingerprint`)
- Sửa chính:
  - đồng bộ cùng định dạng fingerprint (DB fingerprint dùng cho cả load + watcher).
  - load room tiles chạy nền + coalescing reload pending.
- Rủi ro: fingerprint false-positive/false-negative nếu schema đổi.
- Rollback: dùng lại fingerprint cũ + sync load cũ.
- Đo lại:
  - Scenario `Open RoomMap`, `Realtime refresh idle 60s`.
  - Expect: số lần `LoadRoomTiles` giảm, CPU idle giảm.

### CS-03: Non-blocking report/export
- Mục tiêu: không block UI khi load report + export.
- Files:
  - `Forms/MainForm.cs` (`LoadRevenueReportData`, `SeedReportSampleData`, `ExportRevenueCsv`)
- Sửa chính:
  - chuyển DB/report/export sang background.
  - export CSV theo stream (không build full string trong RAM).
  - thêm guard chống re-entry (`_isRevenueReportLoading`, `_isRevenueCsvExporting`).
- Rủi ro: stale UI nếu user đổi filter liên tục.
- Rollback: quay về sync methods cũ.
- Đo lại:
  - `LoadRevenueReportData`, `ExportRevenueCsv` (dataset lớn).
  - Expect: UI responsive, alloc peak giảm rõ ở export.

### CS-04: Remove silent catches + explicit error handling
- Mục tiêu: không nuốt lỗi trong background/async.
- Files:
  - `Forms/MainForm.cs` (`LoadExplorerDetailForSelection`)
- Sửa chính:
  - thay `catch {}` bằng log + toast throttle + clear state an toàn.
- Rủi ro: toast hiển thị nhiều khi DB lỗi liên tục (đã throttle).
- Rollback: bỏ toast/log.
- Đo lại:
  - Trigger lỗi DB giả lập.
  - Expect: có log + thông báo UI, app không crash.

### CS-05: Reduce duplicated data loads
- Mục tiêu: giảm truy vấn KPI dư.
- Files:
  - `Forms/MainForm.cs` (event handlers filter/refresh thống kê)
- Sửa chính:
  - bỏ `LoadKpiDashboardData()` call trùng vì đã gọi trong `BindBookingStatisticsData()`.
- Rủi ro: KPI update chậm nếu không qua bind (không xảy ra trong flow hiện tại).
- Rollback: thêm lại call.
- Đo lại:
  - `Refresh BookingStatistics`.
  - Expect: số call KPI giảm, CPU/DB load giảm.

## 5) Before/After table (sau từng CS)

| Scenario | Before(ms) | After CS-02(ms) | After CS-03(ms) | After CS-05(ms) | CPU delta | Alloc delta | Memory peak delta | Notes |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| Cold start |  |  |  |  |  |  |  |  |
| Warm start |  |  |  |  |  |  |  |  |
| Open RoomMap |  |  |  |  |  |  |  |  |
| Open BookingStatistics |  |  |  |  |  |  |  |  |
| Load KPI Dashboard |  |  |  |  |  |  |  |  |
| Load Explorer page |  |  |  |  |  |  |  |  |
| Load Audit + Alerts |  |  |  |  |  |  |  |  |
| Load RevenueReport |  |  |  |  |  |  |  |  |
| Export CSV |  |  |  |  |  |  |  |  |
| Pay Overnight |  |  |  |  |  |  |  |  |

## Acceptance criteria đề xuất
- Cold start giảm >= 30% so baseline.
- `Open BookingStatistics` < 700ms median.
- `LoadRevenueReport` không làm UI đứng > 100ms.
- `Export CSV` với 10k invoices không OOM, UI vẫn thao tác được.
- Không còn `catch {}` trống trong luồng async/background đã tối ưu.
