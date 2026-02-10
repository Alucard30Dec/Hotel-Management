@echo off
setlocal EnableExtensions

:: Hiển thị tiếng Việt ổn hơn trong console (không ảnh hưởng encoding file output)
chcp 65001 >nul

:: ================= CẤU HÌNH ĐƯỜNG DẪN =================
set "SOURCE_DIR=E:\Project\Hotel-Management\Hotel Management"
set "DEST_DIR=E:\Project\Hotel-Management"
set "OUTPUT_FILE=%DEST_DIR%\ALL_FILES_TEXT.txt"

echo Dang khoi tao...
echo Source: %SOURCE_DIR%
echo Output: %OUTPUT_FILE%

:: Kiểm tra SOURCE có tồn tại không
if not exist "%SOURCE_DIR%" (
  echo [ERROR] Khong tim thay thu muc SOURCE_DIR: %SOURCE_DIR%
  pause
  exit /b 1
)

:: Tạo thư mục đích nếu chưa có
if not exist "%DEST_DIR%" mkdir "%DEST_DIR%"

:: Xóa file cũ nếu đã tồn tại
if exist "%OUTPUT_FILE%" del "%OUTPUT_FILE%"

echo.
echo Dang quet va xuat code... Vui long doi...

:: Dùng PowerShell để:
:: - Lấy file theo extension (ổn định hơn -Include)
:: - Loại bỏ bin/obj/.vs/.git/packages...
:: - Ghi UTF-8 để không lỗi dấu tiếng Việt
PowerShell -NoProfile -ExecutionPolicy Bypass -Command ^
  "& { " ^
  "  $source = $env:SOURCE_DIR; " ^
  "  $outFile = $env:OUTPUT_FILE; " ^
  "  $includeExt = @('.cs','.config','.xml','.resx','.csproj','.sln','.sql','.json','.js','.css','.md','.txt'); " ^
  "  $excludeRegex = '\\(bin|obj|\.vs|\.git|packages|TestResults|\.idea|node_modules)\\'; " ^
  "  $files = Get-ChildItem -LiteralPath $source -Recurse -File -Force -ErrorAction SilentlyContinue | " ^
  "    Where-Object { ($includeExt -contains $_.Extension.ToLowerInvariant()) -and ($_.FullName -notmatch $excludeRegex) } | " ^
  "    Sort-Object FullName; " ^
  "  Set-Content -LiteralPath $outFile -Value '' -Encoding UTF8; " ^
  "  foreach ($f in $files) { " ^
  "    $rel = $f.FullName.Substring($source.Length).TrimStart('\','/'); " ^
  "    Add-Content -LiteralPath $outFile -Encoding UTF8 -Value ('=' * 96); " ^
  "    Add-Content -LiteralPath $outFile -Encoding UTF8 -Value ('FILE PATH: ' + $rel); " ^
  "    Add-Content -LiteralPath $outFile -Encoding UTF8 -Value ('=' * 96); " ^
  "    try { $c = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction Stop } " ^
  "    catch { $c = '[ERROR READ FILE] ' + $f.FullName + ' :: ' + $_.Exception.Message } " ^
  "    Add-Content -LiteralPath $outFile -Encoding UTF8 -Value $c; " ^
  "    Add-Content -LiteralPath $outFile -Encoding UTF8 -Value ''; " ^
  "    Write-Host ('[EXPORTED] ' + $rel) " ^
  "  } " ^
  "  Write-Host ('DONE. Total files: ' + $files.Count) " ^
  "}"

if errorlevel 1 (
  echo.
  echo [ERROR] PowerShell export that bai. Hay copy dong lenh PowerShell ra chay thu de xem loi chi tiet.
  pause
  exit /b 1
)

echo.
echo ======================================================
echo HOAN THANH! File code da duoc luu tai:
echo %OUTPUT_FILE%
echo ======================================================
pause
