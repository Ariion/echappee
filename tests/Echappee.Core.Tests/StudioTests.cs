using System.Linq;
using System.Xml.Linq;
using Echappee.Config;
using Echappee.Studio;
using Xunit;

public class StudioTests
{
    static Localizer Loc(string lang = "fr")
    {
        var l = new Localizer { Language = lang };
        foreach (var x in Localizer.Languages)
            l.Load(x, System.IO.File.ReadAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory, "data", x + ".json")));
        return l;
    }

    [Fact]
    public void Poster_is_valid_xml_for_every_pattern_and_language()
    {
        foreach (JerseyPattern p in System.Enum.GetValues(typeof(JerseyPattern)))
            foreach (var lang in Localizer.Languages)
            {
                var d = new PosterData { TeamName = "Cadence Mistral", DisciplineKey = "route", League = "Régionale", Stage = 12, TimeSeconds = 57.4, Jersey = new JerseyDesign { Pattern = p, Sponsor = "Vélo & Co" } };
                var svg = PosterRenderer.Render(d, Loc(lang));
                var doc = XDocument.Parse(svg);                      // lève une exception si le SVG est invalide
                Assert.Equal("svg", doc.Root.Name.LocalName);
            }
    }

    [Fact]
    public void Time_format_matches_the_mockup_and_depends_on_language()
    {
        Assert.Equal("00:57,4", PosterRenderer.FormatTime(57.4, "fr"));
        Assert.Equal("00:57.4", PosterRenderer.FormatTime(57.4, "en"));
        Assert.Equal("01:05,0", PosterRenderer.FormatTime(65, "fr"));
    }

    [Fact]
    public void Player_text_and_colors_cannot_inject_markup()
    {
        var d = new PosterData
        {
            TeamName = "<script>alert(1)</script>", League = "\"><img onerror=x>",
            Jersey = new JerseyDesign { Base = "red\" onload=\"x", Accent = "#12345", Sponsor = "<b>&</b>" }
        };
        var svg = PosterRenderer.Render(d, Loc());
        XDocument.Parse(svg);
        Assert.DoesNotContain("<script", svg);
        Assert.DoesNotContain("<img", svg);
        Assert.DoesNotContain("onload", svg);
        Assert.Equal("#3C5BFF", JerseyDesign.SafeColor("red", "#3C5BFF"));
        Assert.Equal("#AABBCC", JerseyDesign.SafeColor("#aabbcc", "#000000"));
    }

    [Fact]
    public void Long_names_are_truncated_and_premium_patterns_are_flagged()
    {
        var svg = PosterRenderer.Render(new PosterData { TeamName = new string('x', 80) }, Loc());
        Assert.DoesNotContain(new string('X', 30), svg);
        Assert.True(JerseyDesign.IsPremium(JerseyPattern.Checks));
        Assert.False(JerseyDesign.IsPremium(JerseyPattern.Solid));
    }
}
