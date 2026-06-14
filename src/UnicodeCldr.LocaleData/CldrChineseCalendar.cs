using System;
using System.Collections.Generic;

namespace UnicodeCldr.LocaleData;

/// <summary>
/// An astronomical implementation of the Chinese lunisolar calendar, used to extend coverage
/// beyond the range of the .NET <c>ChineseLunisolarCalendar</c> (Gregorian 1901–2100). It follows
/// the algorithm of Reingold &amp; Dershowitz, <i>Calendrical Calculations</i> (the same basis ICU's
/// Chinese calendar derives from): months begin at the astronomical new moon as seen from Beijing
/// (local mean time before 1929, +08:00 afterwards), and a leap month is the first lunar month of a
/// 13-month year that contains no major solar term (zhongqi).
///
/// All computation is in double precision, which is ample at day granularity over the historical
/// range; the results agree with the .NET calendar across its full 1901–2100 span (verified by the
/// host's regression tests), so the two are interchangeable at the boundary.
///
/// A Chinese "year" here is identified by the Gregorian year in which its New Year day falls — the
/// same convention Temporal uses — and months are addressed by their 1-based ordinal (1..13), with
/// the leap month carrying the previous ordinal's number plus an "L" suffix.
/// </summary>
public static class CldrChineseCalendar
{
    private const double MeanTropicalYear = 365.242189;
    private const double MeanSynodicMonth = 29.530588861;
    private const double Winter = 270.0;

    private static readonly double J2000 = 0.5 + FixedFromGregorian(2000, 1, 1);
    private static readonly long ChineseEpoch = FixedFromGregorian(-2636, 2, 15);
    private static readonly long Rd1970 = FixedFromGregorian(1970, 1, 1);
    private static readonly double NewMoon0 = NthNewMoon(0);

    private static readonly Dictionary<int, ChineseYear> Cache = new();

    /// <summary>The data for the Chinese year whose New Year day falls in <paramref name="gregorianYear"/>.</summary>
    public static ChineseYear GetYear(int gregorianYear)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(gregorianYear, out var cached)) return cached;
            var built = Build(gregorianYear);
            Cache[gregorianYear] = built;
            return built;
        }
    }

    /// <summary>
    /// Decompose an epoch-1970 day number into a Chinese (year, 1-based ordinal month, day). The
    /// returned year is the Gregorian year of the containing Chinese year's New Year day.
    /// </summary>
    public static (int year, int ordinalMonth, int day) FromEpochDay(long epochDay)
    {
        var rd = epochDay + Rd1970;
        var newYear = ChineseNewYearOnOrBefore(rd);
        var gregorianYear = (int)GregorianYearFromFixed(newYear);
        var info = GetYear(gregorianYear);

        var offset = epochDay - info.NewYearEpochDay;
        var ordinal = 1;
        while (offset >= info.DaysInMonth(ordinal))
        {
            offset -= info.DaysInMonth(ordinal);
            ordinal++;
        }
        return (gregorianYear, ordinal, (int)offset + 1);
    }

    private static ChineseYear Build(int gregorianYear)
    {
        var newYear = ChineseNewYearOnOrBefore(FixedFromGregorian(gregorianYear, 7, 1));
        var nextNewYear = ChineseNewYearOnOrBefore(FixedFromGregorian(gregorianYear + 1, 7, 1));
        var monthCount = (int)Math.Round((nextNewYear - newYear) / MeanSynodicMonth, MidpointRounding.ToEven);

        var moons = new long[monthCount + 1];
        moons[0] = newYear;
        for (var i = 1; i <= monthCount; i++)
            moons[i] = ChineseNewMoonOnOrAfter(moons[i - 1] + 1);

        var lengths = new int[monthCount];
        for (var i = 0; i < monthCount; i++)
            lengths[i] = (int)(moons[i + 1] - moons[i]);

        var leapOrdinal = 0;
        if (monthCount == 13)
        {
            for (var i = 0; i < monthCount; i++)
            {
                if (NoMajorSolarTerm(moons[i])) { leapOrdinal = i + 1; break; }
            }
        }

        return new ChineseYear(newYear - Rd1970, leapOrdinal, lengths);
    }

    // ── Chinese calendar primitives (Calendrical Calculations) ────────────────────

    private static long ChineseNewYearOnOrBefore(long date)
    {
        var newYear = ChineseNewYearInSui(date);
        return date >= newYear ? newYear : ChineseNewYearInSui(date - 180);
    }

    // New Year in the sui (solstice-to-solstice period) containing `date`.
    private static long ChineseNewYearInSui(long date)
    {
        var s1 = ChineseWinterSolsticeOnOrBefore(date);
        var s2 = ChineseWinterSolsticeOnOrBefore(s1 + 370);
        var nextM11 = ChineseNewMoonBefore(1 + s2);
        var m12 = ChineseNewMoonOnOrAfter(1 + s1);
        var m13 = ChineseNewMoonOnOrAfter(1 + m12);
        var leapYear = Math.Round((nextM11 - m12) / MeanSynodicMonth, MidpointRounding.ToEven) == 12;

        if (leapYear && (NoMajorSolarTerm(m12) || NoMajorSolarTerm(m13)))
            return ChineseNewMoonOnOrAfter(1 + m13);
        return m13;
    }

    private static long ChineseWinterSolsticeOnOrBefore(long date)
    {
        var approx = EstimatePriorSolarLongitude(Winter, MidnightInChina(date + 1));
        var day = (long)Math.Floor(approx) - 1;
        while (!(Winter < SolarLongitude(MidnightInChina(1 + day)))) day++;
        return day;
    }

    // True when the lunar month starting on `date` (Beijing) contains no major solar term.
    private static bool NoMajorSolarTerm(long date)
        => CurrentMajorSolarTerm(date) == CurrentMajorSolarTerm(ChineseNewMoonOnOrAfter(date + 1));

    // The index (1..12) of the last major solar term (zhongqi) at Beijing midnight of `date`.
    private static int CurrentMajorSolarTerm(long date)
    {
        var s = SolarLongitude(date - ChinaZone(date));
        return Amod(2 + FloorDiv((long)s, 30), 12);
    }

    private static long ChineseNewMoonBefore(long date)
    {
        var tee = NewMoonBefore(MidnightInChina(date));
        return (long)Math.Floor(tee + ChinaZone(tee));
    }

    private static long ChineseNewMoonOnOrAfter(long date)
    {
        var tee = NewMoonAtOrAfter(MidnightInChina(date));
        return (long)Math.Floor(tee + ChinaZone(tee));
    }

    // Universal time of Beijing clock-midnight starting `date`.
    private static double MidnightInChina(long date) => date - ChinaZone(date);

    // Beijing's offset from UT (as a fraction of a day): Beijing local mean time before 1929,
    // +08:00 from 1929 on.
    private static double ChinaZone(double tee)
    {
        var year = GregorianYearFromFixed((long)Math.Floor(tee));
        return year < 1929 ? (1397.0 / 180.0) / 24.0 : 8.0 / 24.0;
    }

    // ── New-moon search ───────────────────────────────────────────────────────────
    // The mean-month estimate of the index is corrected by stepping over the monotonic
    // NthNewMoon, avoiding the lunar-longitude series (only needed for the index, not the moment).

    private static double NewMoonBefore(double tee)
    {
        var n = (long)Math.Round((tee - NewMoon0) / MeanSynodicMonth, MidpointRounding.ToEven);
        while (NthNewMoon(n) >= tee) n--;
        while (NthNewMoon(n + 1) < tee) n++;
        return NthNewMoon(n);
    }

    private static double NewMoonAtOrAfter(double tee)
    {
        var n = (long)Math.Round((tee - NewMoon0) / MeanSynodicMonth, MidpointRounding.ToEven);
        while (NthNewMoon(n) < tee) n++;
        while (NthNewMoon(n - 1) >= tee) n--;
        return NthNewMoon(n);
    }

    // ── Astronomy (Meeus, via Calendrical Calculations) ───────────────────────────

    // Moment (UT) of the n-th new moon after the new moon of 11 January 1 CE.
    private static double NthNewMoon(long n)
    {
        const long n0 = 24724;
        var k = n - n0;
        var c = k / 1236.85;
        var approx = J2000 + Poly(c, new[]
        {
            5.09766, MeanSynodicMonth * 1236.85, 0.0001437, -0.000000150, 0.00000000073
        });
        var capE = Poly(c, new[] { 1.0, -0.002516, -0.0000074 });
        var solarAnomaly = Poly(c, new[]
        {
            2.5534, 1236.85 * 29.10535669, -0.0000014, -0.00000011
        });
        var lunarAnomaly = Poly(c, new[]
        {
            201.5643, 385.81693528 * 1236.85, 0.0107582, 0.00001238, -0.000000058
        });
        var moonArgument = Poly(c, new[]
        {
            160.7108, 390.67050284 * 1236.85, -0.0016118, -0.00000227, 0.000000011
        });
        var capOmega = Poly(c, new[]
        {
            124.7746, -1.56375588 * 1236.85, 0.0020672, 0.00000215
        });

        int[] eFactor = { 0, 1, 0, 0, 1, 1, 2, 0, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] solarCoeff = { 0, 1, 0, 0, -1, 1, 2, 0, 0, 1, 0, 1, 1, -1, 2, 0, 3, 1, 0, 1, -1, -1, 1, 0 };
        int[] lunarCoeff = { 1, 0, 2, 0, 1, 1, 0, 1, 1, 2, 3, 0, 0, 2, 1, 2, 0, 1, 2, 1, 1, 1, 3, 4 };
        int[] moonCoeff = { 0, 0, 0, 2, 0, 0, 0, -2, 2, 0, 0, 2, -2, 0, 0, -2, 0, -2, 2, 2, 2, -2, 0, 0 };
        double[] sineCoeff =
        {
            -0.40720, 0.17241, 0.01608, 0.01039, 0.00739, -0.00514, 0.00208, -0.00111,
            -0.00057, 0.00056, -0.00042, 0.00042, 0.00038, -0.00024, -0.00007, 0.00004,
            0.00004, 0.00003, 0.00003, -0.00003, 0.00003, -0.00002, -0.00002, 0.00002
        };

        var correction = -0.00017 * SinDeg(capOmega);
        for (var i = 0; i < sineCoeff.Length; i++)
        {
            correction += sineCoeff[i] * Math.Pow(capE, eFactor[i]) *
                SinDeg(solarCoeff[i] * solarAnomaly + lunarCoeff[i] * lunarAnomaly + moonCoeff[i] * moonArgument);
        }

        var extra = 0.000325 * SinDeg(Poly(c, new[] { 299.77, 132.8475848, -0.009173 }));

        double[] addConst =
        {
            251.88, 251.83, 349.42, 84.66, 141.74, 207.14, 154.84, 34.52, 207.19, 291.34,
            161.72, 239.56, 331.55
        };
        double[] addCoeff =
        {
            0.016321, 26.651886, 36.412478, 18.206239, 53.303771, 2.453732, 7.306860,
            27.261239, 0.121824, 1.844379, 24.198154, 25.513099, 3.592518
        };
        double[] addFactor =
        {
            0.000165, 0.000164, 0.000126, 0.000110, 0.000062, 0.000060, 0.000056, 0.000047,
            0.000042, 0.000040, 0.000037, 0.000035, 0.000023
        };
        var additional = 0.0;
        for (var i = 0; i < addConst.Length; i++)
            additional += addFactor[i] * SinDeg(addConst[i] + addCoeff[i] * k);

        var dynamical = approx + correction + extra + additional;
        return dynamical - EphemerisCorrection(dynamical); // universal from dynamical
    }

    // Solar longitude (degrees) at moment UT `tee`.
    private static double SolarLongitude(double tee)
    {
        var c = JulianCenturies(tee);
        double[] coefficients =
        {
            403406, 195207, 119433, 112392, 3891, 2819, 1721, 660, 350, 334, 314, 268, 242,
            234, 158, 132, 129, 114, 99, 93, 86, 78, 72, 68, 64, 46, 38, 37, 32, 29, 28, 27,
            27, 25, 24, 21, 21, 20, 18, 17, 14, 13, 13, 13, 12, 10, 10, 10, 10
        };
        double[] multipliers =
        {
            0.9287892, 35999.1376958, 35999.4089666, 35998.7287385, 71998.20261, 71998.4403,
            36000.35726, 71997.4812, 32964.4678, -19.4410, 445267.1117, 45036.8840, 3.1008,
            22518.4434, -19.9739, 65928.9345, 9038.0293, 3034.7684, 33718.148, 3034.448,
            -2280.773, 29929.992, 31556.493, 149.588, 9037.750, 107997.405, -4444.176,
            151.771, 67555.316, 31556.080, -4561.540, 107996.706, 1221.655, 62894.167,
            31437.369, 14578.298, -31931.757, 34777.243, 1221.999, 62894.511, -4442.039,
            107997.909, 119.066, 16859.071, -4.578, 26895.292, -39.127, 12297.536, 90073.778
        };
        double[] addends =
        {
            270.54861, 340.19128, 63.91854, 331.26220, 317.843, 86.631, 240.052, 310.26,
            247.23, 260.87, 297.82, 343.14, 166.79, 81.53, 3.50, 132.75, 182.95, 162.03,
            29.8, 266.4, 249.2, 157.6, 257.8, 185.1, 69.9, 8.0, 197.1, 250.4, 65.3, 162.7,
            341.5, 291.6, 98.5, 146.7, 110.0, 5.2, 342.6, 230.9, 256.1, 45.3, 242.9, 115.2,
            151.8, 285.3, 53.3, 126.6, 205.7, 85.9, 146.1
        };

        var sum = 0.0;
        for (var i = 0; i < coefficients.Length; i++)
            sum += coefficients[i] * SinDeg(addends[i] + multipliers[i] * c);

        var lam = 282.7771834 + 36000.76953744 * c + 0.000005729577951308232 * sum;
        return Mod(lam + Aberration(c) + Nutation(c), 360);
    }

    private static double Nutation(double c)
    {
        var capA = Poly(c, new[] { 124.90, -1934.134, 0.002063 });
        var capB = Poly(c, new[] { 201.11, 72001.5377, 0.00057 });
        return -0.004778 * SinDeg(capA) + -0.0003667 * SinDeg(capB);
    }

    private static double Aberration(double c)
        => 0.0000974 * CosDeg(177.63 + 35999.01848 * c) - 0.005575;

    // Approximate moment at or before `tee` when solar longitude last reached `lam`.
    private static double EstimatePriorSolarLongitude(double lam, double tee)
    {
        var rate = MeanTropicalYear / 360.0;
        var tau = tee - rate * Mod(SolarLongitude(tee) - lam, 360);
        var delta = Mod(SolarLongitude(tau) - lam + 180, 360) - 180;
        return Math.Min(tee, tau - rate * delta);
    }

    private static double JulianCenturies(double tee) => (tee + EphemerisCorrection(tee) - J2000) / 36525.0;

    // Dynamical Time minus Universal Time (in days), per Meeus.
    private static double EphemerisCorrection(double tee)
    {
        var year = (int)GregorianYearFromFixed((long)Math.Floor(tee));
        var c = (FixedFromGregorian(year, 7, 1) - FixedFromGregorian(1900, 1, 1)) / 36525.0;
        if (year is >= 1988 and <= 2019)
            return (year - 1933) / 86400.0;
        if (year is >= 1900 and <= 1987)
            return Poly(c, new[]
            {
                -0.00002, 0.000297, 0.025184, -0.181133, 0.553040, -0.861938, 0.677066, -0.212591
            });
        if (year is >= 1800 and <= 1899)
            return Poly(c, new[]
            {
                -0.000009, 0.003844, 0.083563, 0.865736, 4.867575, 15.845535, 31.332267,
                38.291999, 28.316289, 11.636204, 2.043794
            });
        if (year is >= 1700 and <= 1799)
            return Poly(year - 1700, new[] { 8.118780842, -0.005092142, 0.003336121, -0.0000266484 }) / 86400.0;
        if (year is >= 1620 and <= 1699)
            return Poly(year - 1600, new[] { 196.58333, -4.0675, 0.0219167 }) / 86400.0;
        var x = 0.5 + (FixedFromGregorian(year, 1, 1) - FixedFromGregorian(1810, 1, 1));
        return ((x * x) / 41048480.0 - 15) / 86400.0;
    }

    // ── Gregorian fixed-day conversions ───────────────────────────────────────────

    internal static long FixedFromGregorian(int year, int month, int day)
    {
        long y = year;
        var m4 = LMod(y, 4);
        var m400 = LMod(y, 400);
        var leap = m4 == 0 && m400 != 100 && m400 != 200 && m400 != 300;
        return 365 * (y - 1)
             + FloorDiv(y - 1, 4) - FloorDiv(y - 1, 100) + FloorDiv(y - 1, 400)
             + FloorDiv(367L * month - 362, 12)
             + (month <= 2 ? 0 : (leap ? -1 : -2))
             + day;
    }

    private static long GregorianYearFromFixed(long date)
    {
        var d0 = date - 1; // GREGORIAN_EPOCH = 1
        var n400 = FloorDiv(d0, 146097); var d1 = LMod(d0, 146097);
        var n100 = FloorDiv(d1, 36524); var d2 = LMod(d1, 36524);
        var n4 = FloorDiv(d2, 1461); var d3 = LMod(d2, 1461);
        var n1 = FloorDiv(d3, 365);
        var year = 400 * n400 + 100 * n100 + 4 * n4 + n1;
        return n100 == 4 || n1 == 4 ? year : year + 1;
    }

    // ── small numeric helpers ─────────────────────────────────────────────────────

    private static double Poly(double x, double[] a)
    {
        var p = a[^1];
        for (var i = a.Length - 2; i >= 0; i--) p = p * x + a[i];
        return p;
    }

    private static double Mod(double x, double m) => x - m * Math.Floor(x / m);
    private static long LMod(long x, long m) => ((x % m) + m) % m;
    private static long FloorDiv(long a, long b) => (long)Math.Floor((double)a / b);
    private static int Amod(long x, long y) => (int)(((x - 1) % y + y) % y + 1);
    private static double SinDeg(double d) => Math.Sin(d * Math.PI / 180.0);
    private static double CosDeg(double d) => Math.Cos(d * Math.PI / 180.0);
}

/// <summary>One Chinese calendar year's layout: where it starts and its month structure.</summary>
public sealed class ChineseYear
{
    private readonly int[] _monthLengths;

    internal ChineseYear(long newYearEpochDay, int leapMonthOrdinal, int[] monthLengths)
    {
        NewYearEpochDay = newYearEpochDay;
        LeapMonthOrdinal = leapMonthOrdinal;
        _monthLengths = monthLengths;
    }

    /// <summary>Epoch-1970 day number of this year's first month, day 1.</summary>
    public long NewYearEpochDay { get; }

    /// <summary>The 1-based ordinal of the leap month, or 0 if the year has none.</summary>
    public int LeapMonthOrdinal { get; }

    /// <summary>The number of months (12 or 13) in the year.</summary>
    public int MonthCount => _monthLengths.Length;

    /// <summary>The length (29 or 30 days) of the given 1-based ordinal month.</summary>
    public int DaysInMonth(int ordinalMonth) => _monthLengths[ordinalMonth - 1];

    /// <summary>Total number of days in the year.</summary>
    public int DaysInYear
    {
        get
        {
            var total = 0;
            foreach (var len in _monthLengths) total += len;
            return total;
        }
    }

    /// <summary>Epoch-1970 day number of day 1 of the given 1-based ordinal month.</summary>
    public long FirstEpochDayOfMonth(int ordinalMonth)
    {
        var day = NewYearEpochDay;
        for (var k = 1; k < ordinalMonth; k++) day += _monthLengths[k - 1];
        return day;
    }
}
