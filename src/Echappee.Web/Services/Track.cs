using System.Globalization;
using System.Text;

namespace Echappee.Web.Services;

/// <summary>Circuit affiché (même tracé que le dossier de concept), échantillonné pour placer les équipes sans JavaScript.</summary>
public static class Track
{
    // M40 110 puis 10 courbes de Bézier cubiques fermant la boucle
    static readonly double[][] Curves =
    {
        new double[]{40,110, 20,110, 15,80, 30,65},
        new double[]{30,65, 45,50, 70,60, 85,45},
        new double[]{85,45, 100,30, 125,20, 150,28},
        new double[]{150,28, 180,38, 195,65, 175,80},
        new double[]{175,80, 160,91, 140,80, 125,90},
        new double[]{125,90, 110,100, 125,125, 100,130},
        new double[]{100,130, 75,135, 60,112, 40,110},
    };

    public const string PathD = "M40 110 C20 110 15 80 30 65 C45 50 70 60 85 45 C100 30 125 20 150 28 C180 38 195 65 175 80 C160 91 140 80 125 90 C110 100 125 125 100 130 C75 135 60 112 40 110 Z";

    static readonly List<(double x, double y, double d)> Pts = Build();
    public static readonly double Length = Pts[^1].d;

    static List<(double, double, double)> Build()
    {
        var l = new List<(double, double, double)>();
        double px = 0, py = 0, acc = 0; bool first = true;
        foreach (var c in Curves)
            for (int i = 0; i <= 40; i++)
            {
                if (i == 0 && !first) continue;
                double t = i / 40.0, u = 1 - t;
                double x = u*u*u*c[0] + 3*u*u*t*c[2] + 3*u*t*t*c[4] + t*t*t*c[6];
                double y = u*u*u*c[1] + 3*u*u*t*c[3] + 3*u*t*t*c[5] + t*t*t*c[7];
                if (!first) acc += Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
                l.Add((x, y, acc)); px = x; py = y; first = false;
            }
        return l;
    }

    public static (double x, double y) At(double frac, double lane = 0)
    {
        frac -= Math.Floor(frac);
        double d = frac * Length;
        int lo = 0, hi = Pts.Count - 1;
        while (hi - lo > 1) { int m = (lo + hi) / 2; if (Pts[m].d <= d) lo = m; else hi = m; }
        var a = Pts[lo]; var b = Pts[hi];
        double k = b.d - a.d < 1e-9 ? 0 : (d - a.d) / (b.d - a.d);
        double x = a.x + (b.x - a.x) * k, y = a.y + (b.y - a.y) * k;
        double dx = b.x - a.x, dy = b.y - a.y, n = Math.Sqrt(dx * dx + dy * dy);
        if (n < 1e-9 || lane == 0) return (x, y);
        return (x - dy / n * lane, y + dx / n * lane);
    }

    /// <summary>Points d'une portion du circuit (pour colorer les segments).</summary>
    public static string Polyline(double fromFrac, double toFrac)
    {
        var sb = new StringBuilder();
        int steps = Math.Max(2, (int)((toFrac - fromFrac) * 120));
        for (int i = 0; i <= steps; i++)
        {
            var (x, y) = At(fromFrac + (toFrac - fromFrac) * i / steps);
            sb.Append(x.ToString("0.#", CultureInfo.InvariantCulture)).Append(',').Append(y.ToString("0.#", CultureInfo.InvariantCulture)).Append(' ');
        }
        return sb.ToString();
    }
}
