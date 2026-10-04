using System;
using System.Globalization;

namespace Echappee.Numbers
{
    /// <summary>
    /// Grand nombre positif pour les revenus (K, M, B...). Valeur = Mantissa x 10^Exponent,
    /// avec Mantissa dans [1, 10[ (ou 0). Ne descend jamais sous zéro.
    /// </summary>
    public readonly struct BigAmount : IComparable<BigAmount>, IEquatable<BigAmount>
    {
        static readonly string[] Suffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
        };

        public readonly double Mantissa;
        public readonly int Exponent;

        public static readonly BigAmount Zero = new BigAmount(0, 0, true);
        public static readonly BigAmount One = new BigAmount(1);

        BigAmount(double m, int e, bool _) { Mantissa = m; Exponent = e; }

        public BigAmount(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                Mantissa = 0; Exponent = 0;
                return;
            }
            int e = (int)Math.Floor(Math.Log10(value));
            double m = value / Math.Pow(10, e);
            Normalize(ref m, ref e);
            Mantissa = m; Exponent = e;
        }

        public static BigAmount FromParts(double mantissa, int exponent)
        {
            if (mantissa <= 0 || double.IsNaN(mantissa) || double.IsInfinity(mantissa)) return Zero;
            double m = mantissa; int e = exponent;
            while (m >= 10) { m /= 10; e++; }
            while (m < 1) { m *= 10; e--; }
            return new BigAmount(m, e, true);
        }

        static void Normalize(ref double m, ref int e)
        {
            while (m >= 10) { m /= 10; e++; }
            while (m < 1) { m *= 10; e--; }
        }

        public bool IsZero => Mantissa <= 0;

        /// <summary>Base^Exposant sans passer par un double (coûts d'amélioration).</summary>
        public static BigAmount Scale(double baseValue, double growth, int level)
        {
            if (baseValue <= 0) return Zero;
            if (level <= 0) return new BigAmount(baseValue);
            double log = Math.Log10(baseValue) + level * Math.Log10(growth);
            int e = (int)Math.Floor(log);
            return FromParts(Math.Pow(10, log - e), e);
        }

        public double ToDouble() => IsZero ? 0 : Mantissa * Math.Pow(10, Exponent);

        public static BigAmount operator +(BigAmount a, BigAmount b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            if (a.Exponent < b.Exponent) { var t = a; a = b; b = t; }
            int d = a.Exponent - b.Exponent;
            if (d > 15) return a;
            return FromParts(a.Mantissa + b.Mantissa / Math.Pow(10, d), a.Exponent);
        }

        /// <summary>Soustraction bornée à zéro.</summary>
        public static BigAmount operator -(BigAmount a, BigAmount b)
        {
            if (b.IsZero) return a;
            if (a <= b) return Zero;
            int d = a.Exponent - b.Exponent;
            if (d > 15) return a;
            return FromParts(a.Mantissa - b.Mantissa / Math.Pow(10, d), a.Exponent);
        }

        public static BigAmount operator *(BigAmount a, BigAmount b)
        {
            if (a.IsZero || b.IsZero) return Zero;
            return FromParts(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);
        }

        public static BigAmount operator *(BigAmount a, double k)
        {
            if (a.IsZero || k <= 0) return Zero;
            return FromParts(a.Mantissa * k, a.Exponent);
        }

        public static BigAmount operator *(double k, BigAmount a) => a * k;

        public static BigAmount operator /(BigAmount a, double k)
        {
            if (a.IsZero || k <= 0) return Zero;
            return FromParts(a.Mantissa / k, a.Exponent);
        }

        public int CompareTo(BigAmount o)
        {
            if (IsZero && o.IsZero) return 0;
            if (IsZero) return -1;
            if (o.IsZero) return 1;
            if (Exponent != o.Exponent) return Exponent.CompareTo(o.Exponent);
            return Mantissa.CompareTo(o.Mantissa);
        }

        public bool Equals(BigAmount o) => CompareTo(o) == 0;
        public override bool Equals(object obj) => obj is BigAmount b && Equals(b);
        public override int GetHashCode() => IsZero ? 0 : unchecked(Mantissa.GetHashCode() * 397 ^ Exponent);
        public static bool operator ==(BigAmount a, BigAmount b) => a.CompareTo(b) == 0;
        public static bool operator !=(BigAmount a, BigAmount b) => a.CompareTo(b) != 0;
        public static bool operator <(BigAmount a, BigAmount b) => a.CompareTo(b) < 0;
        public static bool operator >(BigAmount a, BigAmount b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigAmount a, BigAmount b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigAmount a, BigAmount b) => a.CompareTo(b) >= 0;
        public static implicit operator BigAmount(double v) => new BigAmount(v);

        public static BigAmount Max(BigAmount a, BigAmount b) => a >= b ? a : b;
        public static BigAmount Min(BigAmount a, BigAmount b) => a <= b ? a : b;

        /// <summary>Affichage compact : 999, 1,26K, 22,5K, 3,40M... puis aa, ab... puis notation scientifique.</summary>
        public string ToString(CultureInfo culture)
        {
            if (IsZero) return "0";
            if (Exponent < 3)
            {
                double v = ToDouble();
                return v >= 100 ? Math.Floor(v).ToString("0", culture) : Trim(v, 1, culture);
            }
            int group = Exponent / 3;
            double scaled = Mantissa * Math.Pow(10, Exponent % 3);
            string suffix = group < Suffixes.Length ? Suffixes[group] : LetterSuffix(group - Suffixes.Length);
            if (suffix == null) return Mantissa.ToString("0.00", culture) + "e" + Exponent;
            int decimals = scaled >= 100 ? 0 : scaled >= 10 ? 1 : 2;
            return Trim(scaled, decimals, culture) + suffix;
        }

        public override string ToString() => ToString(CultureInfo.GetCultureInfo("fr-FR"));

        static string Trim(double v, int decimals, CultureInfo c) =>
            (Math.Floor(v * Math.Pow(10, decimals)) / Math.Pow(10, decimals)).ToString("0." + new string('#', decimals), c);

        // aa, ab, ac... az, ba... jusqu'à zz (676 suffixes)
        static string LetterSuffix(int i)
        {
            if (i < 0 || i >= 26 * 26) return null;
            return new string(new[] { (char)('a' + i / 26), (char)('a' + i % 26) });
        }

        /// <summary>Format sûr pour la sauvegarde : "mantisse|exposant" en culture invariante.</summary>
        public string ToSaveString() =>
            IsZero ? "0" : Mantissa.ToString("R", CultureInfo.InvariantCulture) + "|" + Exponent.ToString(CultureInfo.InvariantCulture);

        public static BigAmount FromSaveString(string s)
        {
            if (string.IsNullOrEmpty(s) || s == "0") return Zero;
            var p = s.Split('|');
            if (p.Length != 2) throw new FormatException("BigAmount invalide : " + s);
            return FromParts(double.Parse(p[0], CultureInfo.InvariantCulture), int.Parse(p[1], CultureInfo.InvariantCulture));
        }
    }
}
