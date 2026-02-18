using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using HotelManagement.Models;

namespace HotelManagement.Services
{
    public sealed class GeoDataLoader
    {
        private readonly string _jsonFilePath;
        private const string AddressFolderName = "Address";
        private const string OptimizedJsonFileName = "dvhc_optimized.json";

        public GeoDataLoader(string jsonFilePath)
        {
            if (string.IsNullOrWhiteSpace(jsonFilePath))
                throw new ArgumentException("Đường dẫn file địa bàn không hợp lệ.", nameof(jsonFilePath));

            _jsonFilePath = jsonFilePath;
        }

        public IReadOnlyList<Tinh> Load()
        {
            bool requestedOptimized = IsOptimizedJsonPath(_jsonFilePath);
            string optimizedPath = ResolveOptimizedJsonPath(_jsonFilePath);
            if (!string.IsNullOrWhiteSpace(optimizedPath))
                return LoadOptimizedJson(optimizedPath);

            if (requestedOptimized)
            {
                string resolvedExpected = ResolveLegacyJsonPath(_jsonFilePath);
                throw new FileNotFoundException(
                    "Không tìm thấy file địa bàn tối ưu (dvhc_optimized.json). Vui lòng kiểm tra lại thư mục Address.",
                    resolvedExpected);
            }

            return LoadLegacyJson();
        }

        private List<Tinh> LoadOptimizedJson(string fullPath)
        {
            try
            {
                using (var stream = File.OpenRead(fullPath))
                {
                    var serializer = new DataContractJsonSerializer(
                        typeof(DvhcOptimizedRoot),
                        new DataContractJsonSerializerSettings
                        {
                            UseSimpleDictionaryFormat = true
                        });
                    var root = serializer.ReadObject(stream) as DvhcOptimizedRoot;
                    if (root == null || root.Old == null || root.New == null)
                        throw new JsonException("File dvhc_optimized.json thiếu dữ liệu bắt buộc (old/new).");

                    var data = BuildGeoFromOptimized(root);
                    NormalizeCollections(data);
                    return data;
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (JsonException)
            {
                throw;
            }
            catch (SerializationException ex)
            {
                throw new JsonException("File dvhc_optimized.json không đúng định dạng mong đợi.", ex);
            }
        }

        private List<Tinh> LoadLegacyJson()
        {
            string fullPath = ResolveLegacyJsonPath(_jsonFilePath);

            try
            {
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException("Không tìm thấy file địa bàn JSON.", fullPath);

                using (var stream = File.OpenRead(fullPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<Tinh>));
                    var data = serializer.ReadObject(stream) as List<Tinh>;
                    if (data == null)
                        throw new JsonException("Nội dung file địa bàn rỗng hoặc sai cấu trúc.");

                    NormalizeCollections(data);
                    return data;
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (JsonException)
            {
                throw;
            }
            catch (SerializationException ex)
            {
                throw new JsonException("File địa bàn JSON không đúng định dạng mong đợi.", ex);
            }
        }

        private static List<Tinh> BuildGeoFromOptimized(DvhcOptimizedRoot root)
        {
            int provincePad = root.CodePadding?.Province > 0 ? root.CodePadding.Province : 2;
            int districtPad = root.CodePadding?.District > 0 ? root.CodePadding.District : 3;
            int communePad = root.CodePadding?.Commune > 0 ? root.CodePadding.Commune : 5;

            var result = new List<Tinh>();

            var oldProvinceNameMap = (root.Old.Provinces ?? new List<ProvinceDto>())
                .GroupBy(p => NormalizeCode(p.ProvinceCode, provincePad), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => FirstNonEmpty(g.Select(x => x.ProvinceName)),
                    StringComparer.OrdinalIgnoreCase);

            var newProvinceNameMap = (root.New.Provinces ?? new List<ProvinceDto>())
                .GroupBy(p => NormalizeCode(p.ProvinceCode, provincePad), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => FirstNonEmpty(g.Select(x => x.ProvinceName)),
                    StringComparer.OrdinalIgnoreCase);

            var oldProvinceMap = new Dictionary<string, Tinh>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in oldProvinceNameMap)
            {
                var tinh = new Tinh
                {
                    MaTinh = kv.Key,
                    TenTinh = kv.Value,
                    IsActive = false,
                    GhiChu = "",
                    Huyens = new List<Huyen>()
                };
                oldProvinceMap[kv.Key] = tinh;
                result.Add(tinh);
            }

            var oldDistrictMap = new Dictionary<string, Huyen>(StringComparer.OrdinalIgnoreCase);
            if (root.Old.DistrictsByProvince != null)
            {
                foreach (var entry in root.Old.DistrictsByProvince)
                {
                    string provinceCode = NormalizeCode(entry.Key, provincePad);
                    if (string.IsNullOrWhiteSpace(provinceCode))
                        continue;

                    var province = GetOrCreateOldProvince(oldProvinceMap, result, provinceCode, oldProvinceNameMap);

                    foreach (var district in entry.Value ?? Enumerable.Empty<DistrictDto>())
                    {
                        string districtCode = NormalizeCode(district.DistrictCode, districtPad);
                        string districtName = district.DistrictName?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(districtCode) || string.IsNullOrWhiteSpace(districtName))
                            continue;

                        var existing = province.Huyens.FirstOrDefault(h => string.Equals(h.MaHuyen, districtCode, StringComparison.OrdinalIgnoreCase));
                        if (existing == null)
                        {
                            existing = new Huyen
                            {
                                MaHuyen = districtCode,
                                TenHuyen = districtName,
                                IsActive = false,
                                GhiChu = "",
                                Xas = new List<Xa>()
                            };
                            province.Huyens.Add(existing);
                        }

                        oldDistrictMap[provinceCode + districtCode] = existing;
                    }
                }
            }

            if (root.Old.CommunesByDistrict != null)
            {
                foreach (var entry in root.Old.CommunesByDistrict)
                {
                    var key = NormalizeDigitsOnly(entry.Key);
                    if (string.IsNullOrWhiteSpace(key) || key.Length < provincePad + districtPad)
                        continue;

                    string provinceCode = key.Substring(0, provincePad);
                    string districtCode = key.Substring(provincePad, districtPad);

                    var province = GetOrCreateOldProvince(oldProvinceMap, result, provinceCode, oldProvinceNameMap);
                    var district = GetOrCreateOldDistrict(oldDistrictMap, province, provinceCode, districtCode);

                    foreach (var commune in entry.Value ?? Enumerable.Empty<CommuneDto>())
                    {
                        string communeCode = NormalizeCode(commune.CommuneCode, communePad);
                        string communeName = commune.CommuneName?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(communeCode) || string.IsNullOrWhiteSpace(communeName))
                            continue;

                        if (district.Xas.Any(x => string.Equals(x.MaXa, communeCode, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        district.Xas.Add(new Xa
                        {
                            MaXa = communeCode,
                            TenXa = communeName,
                            IsActive = false,
                            GhiChu = BuildNote(commune.Note, commune.LegalDoc)
                        });
                    }
                }
            }

            var newProvinceMap = new Dictionary<string, Tinh>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in newProvinceNameMap)
            {
                var tinh = new Tinh
                {
                    MaTinh = kv.Key,
                    TenTinh = kv.Value,
                    IsActive = true,
                    GhiChu = "",
                    Huyens = new List<Huyen>()
                };
                newProvinceMap[kv.Key] = tinh;
                result.Add(tinh);
            }

            if (root.New.CommunesByProvince != null)
            {
                foreach (var entry in root.New.CommunesByProvince)
                {
                    string provinceCode = NormalizeCode(entry.Key, provincePad);
                    if (string.IsNullOrWhiteSpace(provinceCode))
                        continue;

                    var province = GetOrCreateNewProvince(newProvinceMap, result, provinceCode, newProvinceNameMap);
                    var district = GetOrCreateSyntheticDistrictForNewProvince(province, provinceCode, districtPad);

                    foreach (var commune in entry.Value ?? Enumerable.Empty<CommuneDto>())
                    {
                        string communeCode = NormalizeCode(commune.CommuneCode, communePad);
                        string communeName = commune.CommuneName?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(communeCode) || string.IsNullOrWhiteSpace(communeName))
                            continue;

                        var existing = district.Xas.FirstOrDefault(x => string.Equals(x.MaXa, communeCode, StringComparison.OrdinalIgnoreCase));
                        if (existing == null)
                        {
                            district.Xas.Add(new Xa
                            {
                                MaXa = communeCode,
                                TenXa = communeName,
                                IsActive = true,
                                GhiChu = BuildNote(commune.Note, commune.LegalDoc)
                            });
                        }
                        else
                        {
                            existing.TenXa = string.IsNullOrWhiteSpace(existing.TenXa) ? communeName : existing.TenXa;
                            existing.GhiChu = string.IsNullOrWhiteSpace(existing.GhiChu)
                                ? BuildNote(commune.Note, commune.LegalDoc)
                                : existing.GhiChu;
                            existing.IsActive = true;
                        }
                    }
                }
            }

            // Bổ sung ghi chú map lịch sử sát nhập nếu file có block map_old_new_heuristic.
            foreach (var mapItem in root.MapOldNewHeuristic ?? Enumerable.Empty<OldNewMapDto>())
            {
                string provinceOld = NormalizeCode(mapItem.ProvinceCodeOld, provincePad);
                string districtOld = NormalizeCode(mapItem.DistrictCodeOld, districtPad);
                string communeOld = NormalizeCode(mapItem.CommuneCodeOld, communePad);
                if (string.IsNullOrWhiteSpace(provinceOld) || string.IsNullOrWhiteSpace(districtOld) || string.IsNullOrWhiteSpace(communeOld))
                    continue;

                string key = provinceOld + districtOld;
                if (!oldDistrictMap.TryGetValue(key, out var oldDistrict))
                    continue;

                var oldXa = oldDistrict.Xas.FirstOrDefault(x => string.Equals(x.MaXa, communeOld, StringComparison.OrdinalIgnoreCase));
                if (oldXa == null)
                    continue;

                if (string.IsNullOrWhiteSpace(oldXa.GhiChu))
                    oldXa.GhiChu = BuildNote(mapItem.SourceNote, mapItem.LegalDoc);
            }

            SortGeo(result);
            return result;
        }

        private static Tinh GetOrCreateOldProvince(
            IDictionary<string, Tinh> oldProvinceMap,
            IList<Tinh> allTinhs,
            string provinceCode,
            IDictionary<string, string> provinceNameMap)
        {
            if (oldProvinceMap.TryGetValue(provinceCode, out var existing))
                return existing;

            string name = provinceNameMap.ContainsKey(provinceCode)
                ? provinceNameMap[provinceCode]
                : ("Tỉnh/TP " + provinceCode);

            var created = new Tinh
            {
                MaTinh = provinceCode,
                TenTinh = name,
                IsActive = false,
                GhiChu = "",
                Huyens = new List<Huyen>()
            };

            oldProvinceMap[provinceCode] = created;
            allTinhs.Add(created);
            return created;
        }

        private static Huyen GetOrCreateOldDistrict(
            IDictionary<string, Huyen> oldDistrictMap,
            Tinh province,
            string provinceCode,
            string districtCode)
        {
            string key = provinceCode + districtCode;
            if (oldDistrictMap.TryGetValue(key, out var existing))
                return existing;

            var created = new Huyen
            {
                MaHuyen = districtCode,
                TenHuyen = "Đơn vị cũ " + districtCode,
                IsActive = false,
                GhiChu = "",
                Xas = new List<Xa>()
            };
            province.Huyens.Add(created);
            oldDistrictMap[key] = created;
            return created;
        }

        private static Tinh GetOrCreateNewProvince(
            IDictionary<string, Tinh> newProvinceMap,
            IList<Tinh> allTinhs,
            string provinceCode,
            IDictionary<string, string> provinceNameMap)
        {
            if (newProvinceMap.TryGetValue(provinceCode, out var existing))
                return existing;

            string name = provinceNameMap.ContainsKey(provinceCode)
                ? provinceNameMap[provinceCode]
                : ("Tỉnh/TP " + provinceCode);

            var created = new Tinh
            {
                MaTinh = provinceCode,
                TenTinh = name,
                IsActive = true,
                GhiChu = "",
                Huyens = new List<Huyen>()
            };

            newProvinceMap[provinceCode] = created;
            allTinhs.Add(created);
            return created;
        }

        private static Huyen GetOrCreateSyntheticDistrictForNewProvince(Tinh province, string provinceCode, int districtPad)
        {
            string syntheticCode = ("9" + provinceCode).PadLeft(districtPad, '9');
            var district = province.Huyens.FirstOrDefault(h => string.Equals(h.MaHuyen, syntheticCode, StringComparison.OrdinalIgnoreCase));
            if (district != null)
                return district;

            district = new Huyen
            {
                MaHuyen = syntheticCode,
                TenHuyen = "Địa bàn theo tỉnh",
                IsActive = true,
                GhiChu = "Dữ liệu địa bàn mới theo mô hình bỏ cấp quận/huyện.",
                Xas = new List<Xa>()
            };
            province.Huyens.Add(district);
            return district;
        }

        private static void SortGeo(IEnumerable<Tinh> tinhs)
        {
            foreach (var tinh in tinhs)
            {
                if (tinh?.Huyens == null) continue;

                tinh.Huyens = tinh.Huyens
                    .Where(h => h != null)
                    .OrderBy(h => h.TenHuyen)
                    .ToList();

                foreach (var huyen in tinh.Huyens)
                {
                    huyen.Xas = (huyen.Xas ?? new List<Xa>())
                        .Where(x => x != null)
                        .OrderBy(x => x.TenXa)
                        .ToList();
                }
            }
        }

        private static string BuildNote(string note, string legalDoc)
        {
            note = (note ?? string.Empty).Trim();
            legalDoc = (legalDoc ?? string.Empty).Trim();
            if (note.Length == 0) return legalDoc;
            if (legalDoc.Length == 0) return note;
            return note + " | " + legalDoc;
        }

        private static string FirstNonEmpty(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private static string NormalizeDigitsOnly(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var chars = raw.Where(char.IsDigit).ToArray();
            return new string(chars);
        }

        private static string NormalizeCode(string raw, int padLength)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string value = raw.Trim();
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                value = Convert.ToInt64(Math.Truncate(number)).ToString(CultureInfo.InvariantCulture);

            value = NormalizeDigitsOnly(value);
            if (padLength > 0 && value.Length > 0 && value.Length < padLength)
                value = value.PadLeft(padLength, '0');

            return value;
        }

        private static string ResolveLegacyJsonPath(string jsonFilePath)
        {
            if (Path.IsPathRooted(jsonFilePath))
                return jsonFilePath;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Environment.CurrentDirectory;
            string projectDir = baseDir;
            var parent = Directory.GetParent(baseDir);
            if (parent != null && parent.Parent != null)
                projectDir = parent.Parent.FullName;

            var candidates = new List<string>
            {
                Path.Combine(currentDir, jsonFilePath),
                Path.Combine(baseDir, jsonFilePath),
                Path.Combine(projectDir, jsonFilePath)
            };

            string latestExisting = null;
            DateTime latestWriteTime = DateTime.MinValue;
            foreach (var path in candidates)
            {
                if (!File.Exists(path)) continue;
                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime <= latestWriteTime) continue;
                latestWriteTime = writeTime;
                latestExisting = path;
            }

            if (!string.IsNullOrWhiteSpace(latestExisting))
                return latestExisting;

            return Path.Combine(baseDir, jsonFilePath);
        }

        private string ResolveOptimizedJsonPath(string preferredPath)
        {
            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(preferredPath))
            {
                if (IsOptimizedJsonPath(preferredPath))
                {
                    string resolvedPreferred = ResolveLegacyJsonPath(preferredPath);
                    candidates.Add(resolvedPreferred);
                }
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Environment.CurrentDirectory;
            var roots = new List<string>
            {
                currentDir,
                baseDir,
                Directory.GetParent(baseDir)?.FullName ?? baseDir,
                Directory.GetParent(Directory.GetParent(baseDir)?.FullName ?? baseDir)?.FullName ?? baseDir
            };

            foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                    continue;

                string directAddress = Path.Combine(root, AddressFolderName);
                if (Directory.Exists(directAddress))
                {
                    string directFile = Path.Combine(directAddress, OptimizedJsonFileName);
                    candidates.Add(directFile);
                }

                foreach (var dir in Directory.GetDirectories(root))
                {
                    string name = Path.GetFileName(dir) ?? string.Empty;
                    if (!name.Equals("address", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string candidate = Path.Combine(dir, OptimizedJsonFileName);
                    candidates.Add(candidate);
                }
            }

            return ResolveLatestExistingPath(candidates);
        }

        private static void NormalizeCollections(List<Tinh> tinhs)
        {
            if (tinhs == null) return;

            for (int i = 0; i < tinhs.Count; i++)
            {
                var tinh = tinhs[i];
                if (tinh == null) continue;
                if (tinh.Huyens == null)
                    tinh.Huyens = new List<Huyen>();

                for (int j = 0; j < tinh.Huyens.Count; j++)
                {
                    var huyen = tinh.Huyens[j];
                    if (huyen == null) continue;
                    if (huyen.Xas == null)
                        huyen.Xas = new List<Xa>();
                }
            }
        }

        private static bool IsOptimizedJsonPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string fileName = Path.GetFileName(path);
            return fileName.Equals(OptimizedJsonFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveLatestExistingPath(IEnumerable<string> candidates)
        {
            string latestExisting = null;
            DateTime latestWriteTime = DateTime.MinValue;

            foreach (var candidate in candidates ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
                    continue;

                DateTime writeTime = File.GetLastWriteTimeUtc(candidate);
                if (writeTime <= latestWriteTime)
                    continue;

                latestWriteTime = writeTime;
                latestExisting = candidate;
            }

            return latestExisting;
        }

        [DataContract]
        private sealed class DvhcOptimizedRoot
        {
            [DataMember(Name = "code_padding")]
            public CodePaddingDto CodePadding { get; set; }

            [DataMember(Name = "old")]
            public OldGeoDto Old { get; set; }

            [DataMember(Name = "new")]
            public NewGeoDto New { get; set; }

            [DataMember(Name = "map_old_new_heuristic")]
            public List<OldNewMapDto> MapOldNewHeuristic { get; set; }
        }

        [DataContract]
        private sealed class CodePaddingDto
        {
            [DataMember(Name = "province")]
            public int Province { get; set; }

            [DataMember(Name = "district")]
            public int District { get; set; }

            [DataMember(Name = "commune")]
            public int Commune { get; set; }
        }

        [DataContract]
        private sealed class OldGeoDto
        {
            [DataMember(Name = "provinces")]
            public List<ProvinceDto> Provinces { get; set; }

            [DataMember(Name = "districts_by_province")]
            public Dictionary<string, List<DistrictDto>> DistrictsByProvince { get; set; }

            [DataMember(Name = "communes_by_district")]
            public Dictionary<string, List<CommuneDto>> CommunesByDistrict { get; set; }
        }

        [DataContract]
        private sealed class NewGeoDto
        {
            [DataMember(Name = "provinces")]
            public List<ProvinceDto> Provinces { get; set; }

            [DataMember(Name = "communes_by_province")]
            public Dictionary<string, List<CommuneDto>> CommunesByProvince { get; set; }
        }

        [DataContract]
        private sealed class ProvinceDto
        {
            [DataMember(Name = "prov_code")]
            public string ProvinceCode { get; set; }

            [DataMember(Name = "prov_name")]
            public string ProvinceName { get; set; }
        }

        [DataContract]
        private sealed class DistrictDto
        {
            [DataMember(Name = "dist_code")]
            public string DistrictCode { get; set; }

            [DataMember(Name = "dist_name")]
            public string DistrictName { get; set; }
        }

        [DataContract]
        private sealed class CommuneDto
        {
            [DataMember(Name = "comm_code")]
            public string CommuneCode { get; set; }

            [DataMember(Name = "comm_name")]
            public string CommuneName { get; set; }

            [DataMember(Name = "cap_name")]
            public string CapName { get; set; }

            [DataMember(Name = "name_en")]
            public string NameEn { get; set; }

            [DataMember(Name = "note")]
            public string Note { get; set; }

            [DataMember(Name = "legal_doc")]
            public string LegalDoc { get; set; }
        }

        [DataContract]
        private sealed class OldNewMapDto
        {
            [DataMember(Name = "province_code_old")]
            public string ProvinceCodeOld { get; set; }

            [DataMember(Name = "district_code_old")]
            public string DistrictCodeOld { get; set; }

            [DataMember(Name = "commune_code_old")]
            public string CommuneCodeOld { get; set; }

            [DataMember(Name = "source_note")]
            public string SourceNote { get; set; }

            [DataMember(Name = "legal_doc")]
            public string LegalDoc { get; set; }
        }
    }
}
