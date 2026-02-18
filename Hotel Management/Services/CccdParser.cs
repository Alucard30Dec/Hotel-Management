using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HotelManagement.Services
{
    public sealed class CccdParser
    {
        public CccdInfo Parse(string rawOcrText, string rawQrText = null)
        {
            var normalizedText = NormalizeText(rawOcrText);
            var lines = normalizedText
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            var info = new CccdInfo
            {
                RawQr = rawQrText,
                RawOcrText = normalizedText,
                DocumentNumber = ExtractDocumentNumber(rawQrText, normalizedText),
                FullName = ExtractFullName(lines),
                Gender = ExtractGender(lines),
                DateOfBirth = ExtractDateOfBirth(lines),
                Nationality = ExtractNationality(lines),
                AddressRaw = ExtractAddressRaw(lines)
            };

            SplitAddress(info);

            if (string.IsNullOrWhiteSpace(info.Nationality))
                info.Nationality = "VNM - Việt Nam";

            return info;
        }

        public static IReadOnlyList<string> GetSampleOcrTexts()
        {
            return new[]
            {
                "CĂN CƯỚC CÔNG DÂN\nSố: 079203001234\nHọ và tên: NGUYỄN VĂN A\nGiới tính: Nam\nNgày sinh: 01/02/1990\nQuốc tịch: Việt Nam\nNơi thường trú: 123 Đường Lê Lợi, Phường Bến Thành, Quận 1, TP Hồ Chí Minh",
                "CAN CUOC CONG DAN\nSo 012345678901\nHo va ten TRAN THI B\nGioi tinh Nu\nSinh ngay 3/9/1995\nNoi thuong tru 45 Nguyen Trai, Phuong 2, Quan 5, Thanh pho Ho Chi Minh",
                "THE CCCD\nID NO: 001099887766\nFULL NAME: LE VAN C\nSEX: MALE\nDOB: 12041988\nADDRESS: THON 5, XA HOA BINH, HUYEN X, TINH Y",
                "Số CCCD 079200112233\nHọ tên PHAM D THU\nNgày sinh 19900131\nGiới tính Nam\nĐịa chỉ: Tổ 7, Phường Minh Khai, Quận Bắc Từ Liêm, Hà Nội",
                "CCCD\nHo va ten: DO THI E\nGioi tinh: Nu\nNgay sinh: 15-08-1992\nNoi thuong tru: 9 Tran Phu, P.5, TP Da Nang"
            };
        }

        public IReadOnlyList<CccdInfo> RunSampleParses()
        {
            var list = new List<CccdInfo>();
            var samples = GetSampleOcrTexts();
            for (int i = 0; i < samples.Count; i++)
            {
                list.Add(Parse(samples[i], null));
            }

            return list;
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            normalized = Regex.Replace(normalized, "[ \t]+", " ");
            normalized = Regex.Replace(normalized, "\n{2,}", "\n");
            return normalized.Trim();
        }

        private static string ExtractDocumentNumber(string rawQrText, string normalizedOcrText)
        {
            var fromQr = ExtractDigits(rawQrText, 12);
            if (!string.IsNullOrWhiteSpace(fromQr)) return fromQr;

            var fromKeyword = Regex.Match(normalizedOcrText ?? string.Empty,
                @"(?im)(so|số|cccd|cmnd|id\s*no)\s*[:\-]?\s*(\d{9,12})");
            if (fromKeyword.Success)
                return fromKeyword.Groups[2].Value;

            return ExtractDigits(normalizedOcrText, 12) ?? ExtractDigits(normalizedOcrText, 9);
        }

        private static string ExtractDigits(string text, int length)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Regex.Match(text, @"\b\d{" + length + @"}\b");
            if (m.Success) return m.Value;

            var digits = Regex.Replace(text, @"\D", "");
            if (digits.Length == length) return digits;
            return null;
        }

        private static string ExtractFullName(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return null;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var m = Regex.Match(line,
                    @"(?i)(ho\s*va\s*ten|họ\s*và\s*tên|full\s*name|name)\s*[:\-]?\s*(.*)$");
                if (m.Success)
                {
                    var value = CleanName(m.Groups[2].Value);
                    if (!string.IsNullOrWhiteSpace(value)) return value;

                    if (i + 1 < lines.Count)
                    {
                        value = CleanName(lines[i + 1]);
                        if (!string.IsNullOrWhiteSpace(value)) return value;
                    }
                }
            }

            foreach (var line in lines)
            {
                if (line.Length < 5) continue;
                if (Regex.IsMatch(line, @"\d")) continue;
                if (IsLikelyFieldName(line)) continue;

                var upperRatio = CountUpperLetters(line) / (double)Math.Max(1, CountLetters(line));
                if (upperRatio >= 0.6)
                    return CleanName(line);
            }

            return null;
        }

        private static string ExtractGender(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return null;

            foreach (var line in lines)
            {
                var normalized = NormalizeComparable(line);
                if (!normalized.Contains("gioi tinh") && !normalized.Contains("sex"))
                {
                    if (normalized.Contains(" nu") || normalized.EndsWith("nu")) return "Nữ";
                    if (normalized.Contains(" nam") || normalized.EndsWith("nam")) return "Nam";
                    if (normalized.Contains(" female")) return "Nữ";
                    if (normalized.Contains(" male")) return "Nam";
                    continue;
                }

                if (normalized.Contains("nu") || normalized.Contains("female")) return "Nữ";
                if (normalized.Contains("nam") || normalized.Contains("male")) return "Nam";
            }

            return null;
        }

        private static DateTime? ExtractDateOfBirth(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return null;

            string[] patterns = { "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "ddMMyyyy", "yyyyMMdd" };
            var candidates = new List<string>();

            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"(?i)(ngay\s*sinh|date\s*of\s*birth|dob|sinh\s*ngay)"))
                    candidates.Add(line);
            }

            if (candidates.Count == 0)
                candidates.AddRange(lines);

            foreach (var text in candidates)
            {
                var matches = Regex.Matches(text, @"\b\d{1,2}[/-]\d{1,2}[/-]\d{4}\b|\b\d{8}\b");
                foreach (Match m in matches)
                {
                    var token = m.Value;
                    for (int i = 0; i < patterns.Length; i++)
                    {
                        if (DateTime.TryParseExact(token, patterns[i], CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var date))
                        {
                            if (date.Year >= 1900 && date <= DateTime.Today)
                                return date;
                        }
                    }
                }
            }

            return null;
        }

        private static string ExtractNationality(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return "VNM - Việt Nam";

            foreach (var line in lines)
            {
                var normalized = NormalizeComparable(line);
                if (!normalized.Contains("quoc tich") && !normalized.Contains("nationality"))
                {
                    if (normalized.Contains("viet nam")) return "VNM - Việt Nam";
                    continue;
                }

                if (normalized.Contains("viet nam")) return "VNM - Việt Nam";
            }

            return "VNM - Việt Nam";
        }

        private static string ExtractAddressRaw(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return null;

            int idx = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var normalized = NormalizeComparable(lines[i]);
                if (normalized.Contains("noi thuong tru") ||
                    normalized.Contains("thuong tru") ||
                    normalized.Contains("dia chi") ||
                    normalized.Contains("address"))
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
            {
                var probable = lines.FirstOrDefault(l => l.Contains(",") && !IsLikelyFieldName(l));
                return probable;
            }

            var first = Regex.Replace(lines[idx],
                @"(?i)(noi\s*thuong\s*tru|thường\s*trú|thuong\s*tru|dia\s*chi|địa\s*chỉ|address)\s*[:\-]?",
                string.Empty).Trim();

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(first)) parts.Add(first);

            for (int j = idx + 1; j < lines.Count; j++)
            {
                if (IsLikelyFieldName(lines[j])) break;
                parts.Add(lines[j]);
            }

            return string.Join(" ", parts).Trim();
        }

        private static void SplitAddress(CccdInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.AddressRaw)) return;

            var raw = Regex.Replace(info.AddressRaw, @"\s+", " ").Trim();
            var segments = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (segments.Count >= 2)
            {
                info.Province = segments[segments.Count - 1];
                info.Ward = segments[segments.Count - 2];
                if (segments.Count > 2)
                    info.AddressDetail = string.Join(", ", segments.Take(segments.Count - 2));
                else
                    info.AddressDetail = segments[0];
            }
            else
            {
                info.AddressDetail = raw;
            }
        }

        private static bool IsLikelyFieldName(string line)
        {
            var normalized = NormalizeComparable(line);
            return normalized.Contains("ho va ten") || normalized.Contains("full name") ||
                   normalized.Contains("gioi tinh") || normalized.Contains("sex") ||
                   normalized.Contains("ngay sinh") || normalized.Contains("dob") ||
                   normalized.Contains("quoc tich") || normalized.Contains("nationality") ||
                   normalized.Contains("co gia tri") || normalized.Contains("ngay cap") ||
                   normalized.Contains("id no") || normalized.Contains("so") ||
                   normalized.Contains("can cuoc cong dan");
        }

        private static int CountUpperLetters(string text)
        {
            return text.Count(c => char.IsLetter(c) && char.IsUpper(c));
        }

        private static int CountLetters(string text)
        {
            return text.Count(char.IsLetter);
        }

        private static string CleanName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var cleaned = Regex.Replace(value.Trim(), @"\s+", " ");
            cleaned = Regex.Replace(cleaned, @"[^\p{L}\s]", "");
            cleaned = cleaned.Trim();
            return cleaned.Length == 0 ? null : cleaned;
        }

        private static string NormalizeComparable(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var s = text.ToLowerInvariant();
            s = s.Replace('đ', 'd');
            s = s.Normalize(NormalizationForm.FormD);
            var chars = new List<char>(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                    chars.Add(ch);
            }

            return new string(chars.ToArray());
        }
    }
}
