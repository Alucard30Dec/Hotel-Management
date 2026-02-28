using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MySql.Data.MySqlClient;
using HotelManagement.Data;

namespace HotelManagement.Services
{
    public sealed class PricingService
    {
        public sealed class OvernightChargeBreakdown
        {
            public decimal RoomBaseAmount { get; set; }
            public decimal NightAmount { get; set; }
            public decimal DayAmount { get; set; }
            public decimal NightUnitPrice { get; set; }
            public decimal DayUnitPrice { get; set; }
            public int NightUnits { get; set; }
            public int DayUnits { get; set; }
            public decimal LateFeeAmount { get; set; }
            public decimal TotalAmount { get; set; }
            public bool FirstSegmentIsNight { get; set; }
        }

        public sealed class PricingConfig
        {
            public decimal DefaultNightlySingle { get; set; }
            public decimal DefaultNightlyDouble { get; set; }
            public decimal DefaultDailySingle { get; set; }
            public decimal DefaultDailyDouble { get; set; }

            public decimal HourlySingleHour1 { get; set; }
            public decimal HourlySingleNextHour { get; set; }
            public int HourlySingleThresholdMinutes { get; set; }

            public decimal HourlyDoubleHour1 { get; set; }
            public decimal HourlyDoubleNextHour { get; set; }
            public int HourlyDoubleThresholdMinutes { get; set; }

            public int OvernightCheckoutHour { get; set; }
            public int OvernightNightStartHour { get; set; }
            public int OvernightSingleGraceHours { get; set; }
            public int OvernightDoubleGraceHours { get; set; }
            public decimal OvernightSingleLateFee { get; set; }
            public decimal OvernightDoubleLateFee { get; set; }

            public decimal DrinkSoftPrice { get; set; }
            public decimal DrinkWaterPrice { get; set; }

            public PricingConfig Clone()
            {
                return (PricingConfig)MemberwiseClone();
            }
        }

        public const string KeyDefaultNightlySingle = "Default.Single.NightlyRate";
        public const string KeyDefaultNightlyDouble = "Default.Double.NightlyRate";
        public const string KeyDefaultDailySingle = "Default.Single.DailyRate";
        public const string KeyDefaultDailyDouble = "Default.Double.DailyRate";

        public const string KeyHourlySingleHour1 = "Hourly.Single.Hour1";
        public const string KeyHourlySingleNextHour = "Hourly.Single.NextHour";
        public const string KeyHourlySingleThresholdMinutes = "Hourly.Single.ThresholdMinutes";

        public const string KeyHourlyDoubleHour1 = "Hourly.Double.Hour1";
        public const string KeyHourlyDoubleNextHour = "Hourly.Double.NextHour";
        public const string KeyHourlyDoubleThresholdMinutes = "Hourly.Double.ThresholdMinutes";

        public const string KeyOvernightCheckoutHour = "Overnight.CheckoutHour";
        public const string KeyOvernightNightStartHour = "Overnight.NightStartHour";
        public const string KeyOvernightSingleGraceHours = "Overnight.Single.GraceHours";
        public const string KeyOvernightDoubleGraceHours = "Overnight.Double.GraceHours";
        public const string KeyOvernightSingleLateFee = "Overnight.Single.LateFee";
        public const string KeyOvernightDoubleLateFee = "Overnight.Double.LateFee";

        public const string KeyDrinkSoftPrice = "Drink.Soft.UnitPrice";
        public const string KeyDrinkWaterPrice = "Drink.Water.UnitPrice";

        private static readonly Lazy<PricingService> _lazy =
            new Lazy<PricingService>(() => new PricingService());

        private readonly object _sync = new object();
        private readonly SettingsDAL _settingsDal = new SettingsDAL();

        private PricingConfig _cached = GetDefaultConfig();
        private bool _loaded;

        public static PricingService Instance => _lazy.Value;

        public event EventHandler PricingChanged;

        private PricingService()
        {
        }

        public static PricingConfig GetDefaultConfig()
        {
            return new PricingConfig
            {
                DefaultNightlySingle = 200000m,
                DefaultNightlyDouble = 300000m,
                DefaultDailySingle = 250000m,
                DefaultDailyDouble = 350000m,

                HourlySingleHour1 = 60000m,
                HourlySingleNextHour = 20000m,
                HourlySingleThresholdMinutes = 1,

                HourlyDoubleHour1 = 60000m,
                HourlyDoubleNextHour = 20000m,
                HourlyDoubleThresholdMinutes = 1,

                OvernightCheckoutHour = 12,
                OvernightNightStartHour = 20,
                OvernightSingleGraceHours = 0,
                OvernightDoubleGraceHours = 0,
                OvernightSingleLateFee = 20000m,
                OvernightDoubleLateFee = 20000m,

                DrinkSoftPrice = 20000m,
                DrinkWaterPrice = 10000m
            };
        }

        public PricingConfig GetCurrentPricing()
        {
            EnsureLoaded();
            lock (_sync)
            {
                return _cached.Clone();
            }
        }

        public void Reload()
        {
            var loaded = LoadFromDb();
            lock (_sync)
            {
                _cached = Normalize(loaded);
                _loaded = true;
            }

            RaisePricingChanged();
        }

        public void SavePricing(PricingConfig input, string actor)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var normalized = Normalize(input);
            var nowUtc = DateTime.UtcNow;
            string safeActor = AuditContext.ResolveActor(actor);
            var pairs = ToKeyValues(normalized);

            foreach (var pair in pairs)
                _settingsDal.Upsert(pair.Key, pair.Value, safeActor, nowUtc);

            SyncLegacyRoomTypePricing(normalized);

            lock (_sync)
            {
                _cached = normalized;
                _loaded = true;
            }

            RaisePricingChanged();
        }

        public void RestoreDefaults(string actor)
        {
            SavePricing(GetDefaultConfig(), actor);
        }

        public decimal GetDefaultNightlyRate(int roomTypeId)
        {
            var cfg = GetCurrentPricing();
            return roomTypeId == 2 ? cfg.DefaultNightlyDouble : cfg.DefaultNightlySingle;
        }

        public decimal GetDefaultDailyRate(int roomTypeId)
        {
            var cfg = GetCurrentPricing();
            return roomTypeId == 2 ? cfg.DefaultDailyDouble : cfg.DefaultDailySingle;
        }

        public decimal GetDrinkPriceSoft()
        {
            return GetCurrentPricing().DrinkSoftPrice;
        }

        public decimal GetDrinkPriceWater()
        {
            return GetCurrentPricing().DrinkWaterPrice;
        }

        public int CalculateBillableHours(DateTime start, DateTime now, int roomTypeId)
        {
            if (start > now) start = now;

            double totalMinutes = (now - start).TotalMinutes;
            if (totalMinutes <= 60d) return 1;

            var cfg = GetCurrentPricing();
            int threshold = roomTypeId == 2 ? cfg.HourlyDoubleThresholdMinutes : cfg.HourlySingleThresholdMinutes;
            threshold = Clamp(threshold, 0, 59);

            double minutesAfterFirst = totalMinutes - 60d;
            int fullHours = (int)Math.Floor(minutesAfterFirst / 60d);
            double remainder = minutesAfterFirst - fullHours * 60d;

            int extra;
            if (remainder <= 0.0001d)
                extra = 0;
            else if (threshold <= 0)
                extra = 1;
            else
                extra = remainder >= threshold ? 1 : 0;

            return Math.Max(1, 1 + fullHours + extra);
        }

        public decimal CalculateHourlyCharge(DateTime start, DateTime now, int roomTypeId)
        {
            int billableHours = CalculateBillableHours(start, now, roomTypeId);
            var cfg = GetCurrentPricing();

            decimal first = roomTypeId == 2 ? cfg.HourlyDoubleHour1 : cfg.HourlySingleHour1;
            decimal next = roomTypeId == 2 ? cfg.HourlyDoubleNextHour : cfg.HourlySingleNextHour;
            first = Math.Max(0m, first);
            next = Math.Max(0m, next);

            if (billableHours <= 1) return first;
            return first + (billableHours - 1) * next;
        }

        public DateTime CalculateOvernightCheckoutDeadline(DateTime checkIn, int nights, int roomTypeId)
        {
            int safeNights = Math.Max(1, nights);
            var cfg = GetCurrentPricing();
            int graceHours = roomTypeId == 2 ? cfg.OvernightDoubleGraceHours : cfg.OvernightSingleGraceHours;

            DateTime firstCheckout = checkIn.Date.AddHours(Clamp(cfg.OvernightCheckoutHour, 0, 23));
            if (checkIn >= firstCheckout)
                firstCheckout = firstCheckout.AddDays(1);

            DateTime checkout = firstCheckout.AddDays(Math.Max(0, safeNights - 1));
            return checkout.AddHours(Math.Max(0, graceHours));
        }

        public decimal GetOvernightLateFee(int roomTypeId)
        {
            var cfg = GetCurrentPricing();
            return roomTypeId == 2 ? cfg.OvernightDoubleLateFee : cfg.OvernightSingleLateFee;
        }

        public bool IsOvernightNightWindow(DateTime at)
        {
            var cfg = GetCurrentPricing();
            return IsOvernightNightWindow(at.TimeOfDay, cfg.OvernightNightStartHour, cfg.OvernightCheckoutHour);
        }

        public OvernightChargeBreakdown CalculateOvernightChargeBreakdown(
            DateTime checkIn,
            int nights,
            int roomTypeId,
            decimal nightlyRate,
            DateTime now,
            decimal dailyRate = 0m)
        {
            int safeNights = Math.Max(1, nights);
            var cfg = GetCurrentPricing();
            decimal safeNightlyRate = Math.Max(0m, nightlyRate);
            if (safeNightlyRate <= 0m)
                safeNightlyRate = roomTypeId == 2 ? cfg.DefaultNightlyDouble : cfg.DefaultNightlySingle;

            decimal defaultDailyRate = roomTypeId == 2 ? cfg.DefaultDailyDouble : cfg.DefaultDailySingle;
            decimal safeDailyRate = Math.Max(0m, dailyRate);
            if (safeDailyRate <= 0m)
                safeDailyRate = Math.Max(0m, defaultDailyRate);

            bool firstSegmentIsNight = IsOvernightNightWindow(checkIn.TimeOfDay, cfg.OvernightNightStartHour, cfg.OvernightCheckoutHour);

            int nightUnits;
            int dayUnits;
            if (firstSegmentIsNight)
            {
                nightUnits = 1;
                dayUnits = Math.Max(0, safeNights - 1);
            }
            else
            {
                nightUnits = 0;
                dayUnits = safeNights;
            }
            decimal nightAmount = nightUnits * safeNightlyRate;
            decimal dayAmount = dayUnits * safeDailyRate;
            decimal roomBase = nightAmount + dayAmount;

            DateTime deadline = CalculateOvernightCheckoutDeadline(checkIn, safeNights, roomTypeId);
            decimal lateFee = now > deadline ? Math.Max(0m, GetOvernightLateFee(roomTypeId)) : 0m;

            return new OvernightChargeBreakdown
            {
                RoomBaseAmount = roomBase,
                NightAmount = nightAmount,
                DayAmount = dayAmount,
                NightUnitPrice = safeNightlyRate,
                DayUnitPrice = safeDailyRate,
                NightUnits = nightUnits,
                DayUnits = dayUnits,
                LateFeeAmount = lateFee,
                TotalAmount = roomBase + lateFee,
                FirstSegmentIsNight = firstSegmentIsNight
            };
        }

        public decimal CalculateOvernightCharge(DateTime checkIn, int nights, int roomTypeId, decimal nightlyRate, DateTime now, decimal dailyRate = 0m)
        {
            return CalculateOvernightChargeBreakdown(checkIn, nights, roomTypeId, nightlyRate, now, dailyRate).TotalAmount;
        }

        private void EnsureLoaded()
        {
            lock (_sync)
            {
                if (_loaded) return;
            }

            var loaded = LoadFromDb();
            lock (_sync)
            {
                if (_loaded) return;
                _cached = Normalize(loaded);
                _loaded = true;
            }
        }

        private PricingConfig LoadFromDb()
        {
            var defaults = GetDefaultConfig();

            try
            {
                var rows = _settingsDal.GetAll();
                var map = rows
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Key))
                    .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Last().Value, StringComparer.OrdinalIgnoreCase);

                decimal nightlySingle = ReadDecimal(map, KeyDefaultNightlySingle, defaults.DefaultNightlySingle);
                decimal nightlyDouble = ReadDecimal(map, KeyDefaultNightlyDouble, defaults.DefaultNightlyDouble);

                return new PricingConfig
                {
                    DefaultNightlySingle = nightlySingle,
                    DefaultNightlyDouble = nightlyDouble,
                    DefaultDailySingle = ReadDecimal(map, KeyDefaultDailySingle, nightlySingle),
                    DefaultDailyDouble = ReadDecimal(map, KeyDefaultDailyDouble, nightlyDouble),

                    HourlySingleHour1 = ReadDecimal(map, KeyHourlySingleHour1, defaults.HourlySingleHour1),
                    HourlySingleNextHour = ReadDecimal(map, KeyHourlySingleNextHour, defaults.HourlySingleNextHour),
                    HourlySingleThresholdMinutes = ReadInt(map, KeyHourlySingleThresholdMinutes, defaults.HourlySingleThresholdMinutes),

                    HourlyDoubleHour1 = ReadDecimal(map, KeyHourlyDoubleHour1, defaults.HourlyDoubleHour1),
                    HourlyDoubleNextHour = ReadDecimal(map, KeyHourlyDoubleNextHour, defaults.HourlyDoubleNextHour),
                    HourlyDoubleThresholdMinutes = ReadInt(map, KeyHourlyDoubleThresholdMinutes, defaults.HourlyDoubleThresholdMinutes),

                    OvernightCheckoutHour = ReadInt(map, KeyOvernightCheckoutHour, defaults.OvernightCheckoutHour),
                    OvernightNightStartHour = ReadInt(map, KeyOvernightNightStartHour, defaults.OvernightNightStartHour),
                    OvernightSingleGraceHours = ReadInt(map, KeyOvernightSingleGraceHours, defaults.OvernightSingleGraceHours),
                    OvernightDoubleGraceHours = ReadInt(map, KeyOvernightDoubleGraceHours, defaults.OvernightDoubleGraceHours),
                    OvernightSingleLateFee = ReadDecimal(map, KeyOvernightSingleLateFee, defaults.OvernightSingleLateFee),
                    OvernightDoubleLateFee = ReadDecimal(map, KeyOvernightDoubleLateFee, defaults.OvernightDoubleLateFee),

                    DrinkSoftPrice = ReadDecimal(map, KeyDrinkSoftPrice, defaults.DrinkSoftPrice),
                    DrinkWaterPrice = ReadDecimal(map, KeyDrinkWaterPrice, defaults.DrinkWaterPrice)
                };
            }
            catch
            {
                return defaults;
            }
        }

        private static PricingConfig Normalize(PricingConfig input)
        {
            var cfg = input?.Clone() ?? GetDefaultConfig();

            cfg.DefaultNightlySingle = Math.Max(0m, cfg.DefaultNightlySingle);
            cfg.DefaultNightlyDouble = Math.Max(0m, cfg.DefaultNightlyDouble);
            cfg.DefaultDailySingle = Math.Max(0m, cfg.DefaultDailySingle);
            cfg.DefaultDailyDouble = Math.Max(0m, cfg.DefaultDailyDouble);

            cfg.HourlySingleHour1 = Math.Max(0m, cfg.HourlySingleHour1);
            cfg.HourlySingleNextHour = Math.Max(0m, cfg.HourlySingleNextHour);
            cfg.HourlySingleThresholdMinutes = Clamp(cfg.HourlySingleThresholdMinutes, 0, 59);

            cfg.HourlyDoubleHour1 = Math.Max(0m, cfg.HourlyDoubleHour1);
            cfg.HourlyDoubleNextHour = Math.Max(0m, cfg.HourlyDoubleNextHour);
            cfg.HourlyDoubleThresholdMinutes = Clamp(cfg.HourlyDoubleThresholdMinutes, 0, 59);

            cfg.OvernightCheckoutHour = Clamp(cfg.OvernightCheckoutHour, 0, 23);
            cfg.OvernightNightStartHour = Clamp(cfg.OvernightNightStartHour, 0, 23);
            cfg.OvernightSingleGraceHours = Math.Max(0, cfg.OvernightSingleGraceHours);
            cfg.OvernightDoubleGraceHours = Math.Max(0, cfg.OvernightDoubleGraceHours);
            cfg.OvernightSingleLateFee = Math.Max(0m, cfg.OvernightSingleLateFee);
            cfg.OvernightDoubleLateFee = Math.Max(0m, cfg.OvernightDoubleLateFee);

            cfg.DrinkSoftPrice = Math.Max(0m, cfg.DrinkSoftPrice);
            cfg.DrinkWaterPrice = Math.Max(0m, cfg.DrinkWaterPrice);

            return cfg;
        }

        private static Dictionary<string, string> ToKeyValues(PricingConfig cfg)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KeyDefaultNightlySingle] = cfg.DefaultNightlySingle.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyDefaultNightlyDouble] = cfg.DefaultNightlyDouble.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyDefaultDailySingle] = cfg.DefaultDailySingle.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyDefaultDailyDouble] = cfg.DefaultDailyDouble.ToString("0.##", CultureInfo.InvariantCulture),

                [KeyHourlySingleHour1] = cfg.HourlySingleHour1.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyHourlySingleNextHour] = cfg.HourlySingleNextHour.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyHourlySingleThresholdMinutes] = cfg.HourlySingleThresholdMinutes.ToString(CultureInfo.InvariantCulture),

                [KeyHourlyDoubleHour1] = cfg.HourlyDoubleHour1.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyHourlyDoubleNextHour] = cfg.HourlyDoubleNextHour.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyHourlyDoubleThresholdMinutes] = cfg.HourlyDoubleThresholdMinutes.ToString(CultureInfo.InvariantCulture),

                [KeyOvernightCheckoutHour] = cfg.OvernightCheckoutHour.ToString(CultureInfo.InvariantCulture),
                [KeyOvernightNightStartHour] = cfg.OvernightNightStartHour.ToString(CultureInfo.InvariantCulture),
                [KeyOvernightSingleGraceHours] = cfg.OvernightSingleGraceHours.ToString(CultureInfo.InvariantCulture),
                [KeyOvernightDoubleGraceHours] = cfg.OvernightDoubleGraceHours.ToString(CultureInfo.InvariantCulture),
                [KeyOvernightSingleLateFee] = cfg.OvernightSingleLateFee.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyOvernightDoubleLateFee] = cfg.OvernightDoubleLateFee.ToString("0.##", CultureInfo.InvariantCulture),

                [KeyDrinkSoftPrice] = cfg.DrinkSoftPrice.ToString("0.##", CultureInfo.InvariantCulture),
                [KeyDrinkWaterPrice] = cfg.DrinkWaterPrice.ToString("0.##", CultureInfo.InvariantCulture)
            };
        }

        private static decimal ReadDecimal(IReadOnlyDictionary<string, string> map, string key, decimal fallback)
        {
            if (map == null || string.IsNullOrWhiteSpace(key)) return fallback;
            if (!map.TryGetValue(key, out string raw)) return fallback;
            if (string.IsNullOrWhiteSpace(raw)) return fallback;

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return value;
            return fallback;
        }

        private static int ReadInt(IReadOnlyDictionary<string, string> map, string key, int fallback)
        {
            if (map == null || string.IsNullOrWhiteSpace(key)) return fallback;
            if (!map.TryGetValue(key, out string raw)) return fallback;
            if (string.IsNullOrWhiteSpace(raw)) return fallback;

            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return value;
            return fallback;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static bool IsOvernightNightWindow(TimeSpan point, int nightStartHour, int checkoutHour)
        {
            TimeSpan nightStart = TimeSpan.FromHours(Clamp(nightStartHour, 0, 23));
            TimeSpan checkout = TimeSpan.FromHours(Clamp(checkoutHour, 0, 23));

            if (nightStart == checkout)
                return true;

            if (nightStart < checkout)
                return point >= nightStart && point < checkout;

            return point >= nightStart || point < checkout;
        }

        private static void SyncLegacyRoomTypePricing(PricingConfig cfg)
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    const string sql = @"INSERT INTO LOAIPHONG (LoaiPhongID, TenLoaiPhong, DonGiaNgay)
                                         VALUES (@LoaiPhongID, @TenLoaiPhong, @DonGiaNgay)
                                         ON DUPLICATE KEY UPDATE
                                            TenLoaiPhong = VALUES(TenLoaiPhong),
                                            DonGiaNgay = VALUES(DonGiaNgay)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@LoaiPhongID", MySqlDbType.Int32);
                        cmd.Parameters.Add("@TenLoaiPhong", MySqlDbType.VarChar);
                        cmd.Parameters.Add("@DonGiaNgay", MySqlDbType.Decimal);

                        cmd.Parameters["@LoaiPhongID"].Value = 1;
                        cmd.Parameters["@TenLoaiPhong"].Value = "Phòng đơn";
                        cmd.Parameters["@DonGiaNgay"].Value = cfg.DefaultNightlySingle;
                        cmd.ExecuteNonQuery();

                        cmd.Parameters["@LoaiPhongID"].Value = 2;
                        cmd.Parameters["@TenLoaiPhong"].Value = "Phòng đôi";
                        cmd.Parameters["@DonGiaNgay"].Value = cfg.DefaultNightlyDouble;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number != 1146 && ex.Number != 1054)
                    throw;
            }
        }

        private void RaisePricingChanged()
        {
            PricingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
