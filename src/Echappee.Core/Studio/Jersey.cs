using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Echappee.Studio
{
    public enum JerseyPattern { Solid, Bands, Chevrons, Stripes, Checks, Diagonal }

    /// <summary>Maillot de l'équipe. Purement cosmétique : n'a aucun effet sur la puissance.</summary>
    public sealed class JerseyDesign
    {
        public JerseyPattern Pattern = JerseyPattern.Chevrons;
        public string Base = "#3C5BFF";
        public string Accent = "#FFC933";
        public string Band = "#141A33";
        public string Sponsor = "";

        /// <summary>Motifs réservés aux achats cosmétiques (Maillot Studio premium).</summary>
        public static bool IsPremium(JerseyPattern p) => p == JerseyPattern.Checks || p == JerseyPattern.Diagonal;

        static readonly Regex Hex = new Regex("^#[0-9A-Fa-f]{6}$");

        /// <summary>N'accepte que #RRGGBB : une couleur saisie ne peut jamais injecter autre chose dans le SVG.</summary>
        public static string SafeColor(string c, string fallback) => c != null && Hex.IsMatch(c) ? c.ToUpperInvariant() : fallback;

        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    default: if (!char.IsControl(ch)) sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>SVG du maillot seul (viewBox 0 0 100 100), réutilisable sur les cartes et sur l'affiche.</summary>
        public string ToSvgGroup(string idPrefix = "j")
        {
            string b = SafeColor(Base, "#3C5BFF"), a = SafeColor(Accent, "#FFC933"), k = SafeColor(Band, "#141A33");
            const string shape = "M30 8 L8 20 L16 40 L26 36 L26 90 L74 90 L74 36 L84 40 L92 20 L70 8 Q50 24 30 8 Z";
            var sb = new StringBuilder();
            sb.Append("<defs><clipPath id=\"" + idPrefix + "c\"><path d=\"" + shape + "\"/></clipPath></defs>");
            sb.Append("<g clip-path=\"url(#" + idPrefix + "c)\"><rect x=\"0\" y=\"0\" width=\"100\" height=\"100\" fill=\"" + b + "\"/>");
            switch (Pattern)
            {
                case JerseyPattern.Bands:
                    sb.Append("<rect x=\"0\" y=\"46\" width=\"100\" height=\"9\" fill=\"" + a + "\"/><rect x=\"0\" y=\"58\" width=\"100\" height=\"5\" fill=\"" + k + "\"/>"); break;
                case JerseyPattern.Chevrons:
                    for (int i = 0; i < 3; i++)
                        sb.Append("<path d=\"M26 " + (44 + i * 11) + " L50 " + (58 + i * 11) + " L74 " + (44 + i * 11) + " L74 " + (51 + i * 11) + " L50 " + (65 + i * 11) + " L26 " + (51 + i * 11) + " Z\" fill=\"" + (i == 1 ? k : a) + "\"/>");
                    break;
                case JerseyPattern.Stripes:
                    for (int i = 0; i < 5; i++) sb.Append("<rect x=\"" + (22 + i * 12) + "\" y=\"0\" width=\"6\" height=\"100\" fill=\"" + a + "\"/>"); break;
                case JerseyPattern.Checks:
                    for (int x = 0; x < 6; x++) for (int y = 0; y < 8; y++) if ((x + y) % 2 == 0)
                        sb.Append("<rect x=\"" + (26 + x * 8) + "\" y=\"" + (36 + y * 8) + "\" width=\"8\" height=\"8\" fill=\"" + a + "\"/>");
                    break;
                case JerseyPattern.Diagonal:
                    for (int i = 0; i < 4; i++)
                        sb.Append("<path d=\"M" + (10 + i * 22) + " 100 L" + (34 + i * 22) + " 0 L" + (44 + i * 22) + " 0 L" + (20 + i * 22) + " 100 Z\" fill=\"" + a + "\"/>");
                    break;
            }
            sb.Append("</g><path d=\"" + shape + "\" fill=\"none\" stroke=\"#141A33\" stroke-width=\"2\" stroke-linejoin=\"round\"/>");
            if (!string.IsNullOrEmpty(Sponsor))
                sb.Append("<text x=\"50\" y=\"30\" text-anchor=\"middle\" font-family=\"Figtree,sans-serif\" font-weight=\"800\" font-size=\"7\" fill=\"#FFFFFF\" stroke=\"#141A33\" stroke-width=\"0.4\">" + Escape(Sponsor.Length > 14 ? Sponsor.Substring(0, 14) : Sponsor) + "</text>");
            return sb.ToString();
        }
    }
}
