using Echappee.Web.Services;
using Echappee.Simulation;
using Microsoft.AspNetCore.Components;

namespace Echappee.Web.Components;

/// <summary>Composant qui se redessine quand l'état du jeu change.</summary>
public abstract class GameComponent : ComponentBase, IDisposable
{
    [Inject] protected GameService G { get; set; }

    protected override void OnInitialized() => G.Changed += OnChanged;
    void OnChanged() => InvokeAsync(StateHasChanged);
    public void Dispose() => G.Changed -= OnChanged;

    protected string T(string key, params object[] a) => G.T(key, a);
    protected string Money(double eur) => eur.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace(".", G.Dec) + " €";

    protected string Dur(long secs)
    {
        if (secs < 0) secs = 0;
        long d = secs / 86400, h = secs % 86400 / 3600, m = secs % 3600 / 60, s = secs % 60;
        if (d > 0) return $"{d} {T("unit.d")} {h} {T("unit.h")}";
        if (h > 0) return $"{h} {T("unit.h")} {m:00} {T("unit.m")}";
        return $"{m:00}:{s:00}";
    }

    protected static string RarityClass(Rarity r) => r switch { Rarity.Amateur => "t-a", Rarity.Pro => "t-p", Rarity.Elite => "t-e", _ => "t-l" };
    protected string RarityName(Rarity r) => T("rarity." + r);
    protected static string AvatarColor(Rarity r) => r switch { Rarity.Amateur => "#C9D0E4", Rarity.Pro => "#3C5BFF", Rarity.Elite => "#FF4F8B", _ => "#FFC933" };
}
