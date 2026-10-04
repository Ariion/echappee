using System.Globalization;
using System.Text;
using Echappee.Config;

namespace Echappee.Studio
{
    public sealed class PosterData
    {
        public string TeamName = "";
        public string DisciplineKey = "route";     // clé de disc.* dans les textes
        public string League = "";
        public int Stage = 1;
        public double TimeSeconds;
        public JerseyDesign Jersey = new JerseyDesign();
    }

    /// <summary>Affiche de victoire : modèle SVG 4:5, style affiche de course vintage, avec variables. Partageable (convertie en image par le client).</summary>
    public static class PosterRenderer
    {
        public static string FormatTime(double seconds, string lang)
        {
            if (seconds < 0) seconds = 0;
            int tenths = (int)System.Math.Round(seconds * 10);
            int m = tenths / 600, s = (tenths / 10) % 60, t = tenths % 10;
            string sep = lang == "en" ? "." : ",";
            return m.ToString("00", CultureInfo.InvariantCulture) + ":" + s.ToString("00", CultureInfo.InvariantCulture) + sep + t;
        }

        public static string Render(PosterData d, Localizer loc)
        {
            string lang = loc.Language;
            string team = JerseyDesign.Escape(Trim(d.TeamName, 22).ToUpperInvariant());
            string disc = JerseyDesign.Escape(loc.Get("disc." + d.DisciplineKey).ToUpperInvariant());
            string win = JerseyDesign.Escape(loc.Get("ui.victory").ToUpperInvariant());
            string league = JerseyDesign.Escape(Trim(d.League, 22).ToUpperInvariant());
            var sb = new StringBuilder();
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 400 500\" width=\"400\" height=\"500\" role=\"img\">");
            sb.Append("<rect width=\"400\" height=\"500\" fill=\"#FFC933\" stroke=\"#141A33\" stroke-width=\"6\"/>");
            // rayons
            for (int i = 0; i < 20; i += 2)
            {
                double a1 = i * 18 * System.Math.PI / 180, a2 = (i + 1) * 18 * System.Math.PI / 180;
                sb.Append("<path d=\"M200 370 L" + P(200 + 600 * System.Math.Cos(a1)) + " " + P(370 + 600 * System.Math.Sin(a1)) + " L" + P(200 + 600 * System.Math.Cos(a2)) + " " + P(370 + 600 * System.Math.Sin(a2)) + " Z\" fill=\"#FFD966\"/>");
            }
            sb.Append("<rect x=\"3\" y=\"3\" width=\"394\" height=\"30\" fill=\"#141A33\"/>");
            sb.Append("<text x=\"14\" y=\"23\" font-family=\"JetBrains Mono,monospace\" font-size=\"12\" fill=\"#FFC933\" letter-spacing=\"2\">" + win + " · " + disc + "</text>");
            sb.Append("<text x=\"386\" y=\"23\" text-anchor=\"end\" font-family=\"JetBrains Mono,monospace\" font-size=\"12\" fill=\"#FFC933\" letter-spacing=\"2\">" + league + "</text>");
            sb.Append("<text x=\"200\" y=\"110\" text-anchor=\"middle\" font-family=\"Big Shoulders Display,Impact,sans-serif\" font-weight=\"900\" font-size=\"" + Fit(win, 372, 0.55, 84) + "\"" + Len(win, 372, 0.55, 84) + " fill=\"#141A33\">" + win + "</text>");
            sb.Append("<text x=\"200\" y=\"148\" text-anchor=\"middle\" font-family=\"Big Shoulders Display,Impact,sans-serif\" font-weight=\"800\" font-size=\"30\" fill=\"#FF4F8B\" letter-spacing=\"8\">#" + d.Stage + "</text>");
            sb.Append("<g transform=\"translate(105 170) scale(1.9)\">" + d.Jersey.ToSvgGroup("p") + "</g>");
            sb.Append("<rect x=\"3\" y=\"440\" width=\"394\" height=\"57\" fill=\"#FF4F8B\" stroke=\"#141A33\" stroke-width=\"3\"/>");
            sb.Append("<text x=\"16\" y=\"477\" font-family=\"Big Shoulders Display,Impact,sans-serif\" font-weight=\"900\" font-size=\"" + Fit(team, 230, 0.6, 30) + "\"" + Len(team, 230, 0.6, 30) + " fill=\"#FFFFFF\">" + team + "</text>");
            sb.Append("<text x=\"384\" y=\"477\" text-anchor=\"end\" font-family=\"JetBrains Mono,monospace\" font-weight=\"800\" font-size=\"24\" fill=\"#FFFFFF\">" + FormatTime(d.TimeSeconds, lang) + "</text>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>Taille de police pour que le texte tienne dans 'width', quelle que soit la langue ou la police de repli.</summary>
        static string Fit(string text, double width, double charWidthEm, double max)
        {
            int n = System.Math.Max(1, text.Length);
            double size = System.Math.Min(max, width / (charWidthEm * n));
            return System.Math.Max(10, size).ToString("0.#", CultureInfo.InvariantCulture);
        }

        /// <summary>textLength garantit que le texte tient dans la largeur même si la police de repli est plus large.</summary>
        static string Len(string text, double width, double charWidthEm, double max)
        {
            int n = System.Math.Max(1, text.Length);
            double size = double.Parse(Fit(text, width, charWidthEm, max), CultureInfo.InvariantCulture);
            double w = System.Math.Min(width, size * charWidthEm * n);
            return " textLength=\"" + w.ToString("0.#", CultureInfo.InvariantCulture) + "\" lengthAdjust=\"spacingAndGlyphs\"";
        }

        static string P(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
        static string Trim(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length > n ? s.Substring(0, n) : s);
    }
}
