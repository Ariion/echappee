using System.Linq;
using Echappee.Simulation;
using Xunit;

public class SimulationTests
{
    static RaceResult Race(ulong seed, double playerLevel = 50, PlanDeCourse plan = null, string disc = "route", bool frames = false)
    {
        var d = TestData.Load();
        var cfg = d.Balance;
        var dc = cfg.Disciplines[disc];
        var field = BotTeams.Field(cfg, TestData.Player(playerLevel, 7, plan), 50, 99);
        return new RaceSimulator(cfg).Run(field, d.Courses[dc.Circuit], dc, seed, frames);
    }

    [Fact]
    public void Same_seed_gives_same_race()
    {
        var a = Race(123); var b = Race(123);
        Assert.Equal(a.Fingerprint(), b.Fingerprint());
        Assert.Equal(a.Events.Count, b.Events.Count);
    }

    [Fact]
    public void Different_seeds_give_different_races()
    {
        var prints = Enumerable.Range(1, 10).Select(s => Race((ulong)s).Fingerprint()).Distinct().Count();
        Assert.True(prints >= 8);
    }

    [Fact]
    public void Everyone_gets_a_unique_rank_and_the_race_lasts_about_the_discipline_duration()
    {
        var r = Race(5);
        Assert.Equal(12, r.Standings.Count);
        Assert.Equal(Enumerable.Range(1, 12), r.Standings.Select(s => s.Rank));
        Assert.InRange(r.Standings[0].FinishTime, 52, 68);   // 60 s visées
    }

    [Theory]
    [InlineData("route", 60)] [InlineData("piste", 30)] [InlineData("bmx", 20)]
    [InlineData("vtt", 60)] [InlineData("cyclocross", 45)] [InlineData("gravel", 90)]
    public void Every_discipline_runs_to_its_duration(string disc, double secs)
    {
        var r = Race(11, disc: disc);
        Assert.InRange(r.Standings[0].FinishTime, secs * 0.85, secs * 1.15);
        Assert.All(r.Standings, s => Assert.True(s.Rank >= 1));
    }

    [Fact]
    public void Stronger_team_wins_more_often()
    {
        int strong = 0, weak = 0;
        for (ulong s = 1; s <= 60; s++)
        {
            if (Race(s, 70).PlayerRank <= 3) strong++;
            if (Race(s, 30).PlayerRank <= 3) weak++;
        }
        Assert.True(strong > weak + 20, $"fort={strong} faible={weak}");
    }

    [Fact]
    public void Race_produces_a_readable_event_feed()
    {
        var r = Race(42);
        Assert.Equal(EventKind.Start, r.Events[0].Kind);
        Assert.Contains(r.Events, e => e.Kind == EventKind.Attack);
        Assert.Contains(r.Events, e => e.Kind == EventKind.Finish);
        Assert.All(r.Events, e => Assert.False(string.IsNullOrWhiteSpace(EventText.Fr(e))));
        // les événements sont triés dans le temps
        for (int i = 1; i < r.Events.Count; i++) Assert.True(r.Events[i].Time + 0.6 >= r.Events[i - 1].Time);
    }

    [Fact]
    public void Frames_are_recorded_and_monotonic()
    {
        var r = Race(3, frames: true);
        Assert.True(r.Frames.Count > 100);
        for (int t = 1; t < r.Frames.Count; t++)
            for (int i = 0; i < 12; i++) Assert.True(r.Frames[t].Positions[i] >= r.Frames[t - 1].Positions[i]);
    }

    [Fact]
    public void Peloton_splits_and_breakaways_happen_sometimes()
    {
        int splits = 0, breaks = 0;
        for (ulong s = 1; s <= 30; s++)
        {
            var r = Race(s);
            if (r.Events.Any(e => e.Kind == EventKind.Split)) splits++;
            if (r.Events.Any(e => e.Kind == EventKind.Breakaway)) breaks++;
        }
        Assert.True(splits >= 3, "scissions: " + splits);
        Assert.True(breaks >= 1, "échappées: " + breaks);
    }

    [Fact]
    public void Plan_rule_fires_and_is_reported()
    {
        var plan = new PlanDeCourse { Slots = 3 };
        plan.Add(new PlanRule(ConditionKind.Gradient, Comparator.Greater, 6, ActionKind.ClimberAttacks));
        var r = Race(8, plan: plan);
        Assert.Contains(r.Events, e => e.Kind == EventKind.PlanRule && e.Rule == 0);
    }

    [Fact]
    public void Plan_has_limited_slots_and_ignores_inactive_rules()
    {
        var plan = new PlanDeCourse { Slots = 2 };
        Assert.True(plan.Add(new PlanRule(ConditionKind.Fatigue, Comparator.Greater, 70, ActionKind.StayInWheel, false)));
        Assert.True(plan.Add(new PlanRule(ConditionKind.KmRemaining, Comparator.Less, 2, ActionKind.SprinterLaunch)));
        Assert.False(plan.Add(new PlanRule()));
        Assert.Equal(1, plan.Evaluate(new PlanContext { FatiguePct = 90, KmRemaining = 1 }));
        Assert.Equal(-1, plan.Evaluate(new PlanContext { FatiguePct = 90, KmRemaining = 50 }));
    }

    [Fact]
    public void Simulation_core_has_no_unity_or_system_random_dependency()
    {
        var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../src/Echappee.Core"));
        foreach (var f in System.IO.Directory.GetFiles(root, "*.cs", System.IO.SearchOption.AllDirectories).Where(x => !x.Contains("/obj/") && !x.Contains("/bin/")))
        {
            var text = System.IO.File.ReadAllText(f);
            Assert.DoesNotContain("UnityEngine", text);
            Assert.DoesNotContain("new System.Random", text);
            Assert.DoesNotContain("UnityEngine.Random", text);
        }
    }
}

public class PlanBalanceTests
{
    static PlanReport Report(PlanDeCourse plan)
    {
        var d = TestData.Load();
        var disc = d.Balance.Disciplines["route"];
        return PlanAnalyzer.Compare(d.Balance, d.Courses["route"], disc, TestData.Player(58), new PlanDeCourse { Slots = 5 }, plan, 50, 1000, 120);
    }

    [Fact]
    public void A_sensible_plan_beats_no_plan()
    {
        var smart = new PlanDeCourse { Slots = 5 };
        smart.Add(new PlanRule(ConditionKind.KmRemaining, Comparator.Less, 12, ActionKind.SprinterLaunch));
        smart.Add(new PlanRule(ConditionKind.Fatigue, Comparator.Greater, 65, ActionKind.StayInWheel));
        smart.Add(new PlanRule(ConditionKind.Gradient, Comparator.Greater, 6, ActionKind.ClimberAttacks));
        var r = Report(smart);
        Assert.True(r.AvgRankAfter < r.AvgRankBefore, $"{r.AvgRankBefore} -> {r.AvgRankAfter}");
        Assert.True(r.WinRateAfter >= r.WinRateBefore);
    }

    [Fact]
    public void Always_staying_in_the_wheel_is_a_bad_plan()
    {
        var wheel = new PlanDeCourse { Slots = 5 };
        wheel.Add(new PlanRule(ConditionKind.Fatigue, Comparator.Greater, 0, ActionKind.StayInWheel));
        var r = Report(wheel);
        Assert.True(r.AvgRankAfter > r.AvgRankBefore + 1);
    }

    [Fact]
    public void Analyzer_is_deterministic()
    {
        var plan = new PlanDeCourse { Slots = 3 };
        plan.Add(new PlanRule(ConditionKind.Gradient, Comparator.Greater, 6, ActionKind.ClimberAttacks));
        Assert.Equal(Report(plan).AvgRankAfter, Report(plan).AvgRankAfter);
    }
}

public class LocalizationTests
{
    static Echappee.Config.Localizer Load()
    {
        var loc = new Echappee.Config.Localizer();
        foreach (var l in Echappee.Config.Localizer.Languages)
            loc.Load(l, System.IO.File.ReadAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory, "data", l + ".json")));
        return loc;
    }

    [Fact]
    public void Every_language_has_every_key_with_the_same_placeholders()
    {
        var loc = Load();
        var fr = loc.Keys("fr").ToList();
        var ph = new System.Text.RegularExpressions.Regex(@"\{\d\}");
        foreach (var l in Echappee.Config.Localizer.Languages)
        {
            Assert.Equal(fr.OrderBy(x => x), loc.Keys(l).OrderBy(x => x));
            foreach (var k in fr)
            {
                Assert.False(string.IsNullOrWhiteSpace(loc.Raw(l, k)), l + ":" + k);
                Assert.Equal(ph.Matches(loc.Raw("fr", k)).Select(m => m.Value).OrderBy(x => x),
                             ph.Matches(loc.Raw(l, k)).Select(m => m.Value).OrderBy(x => x));
            }
        }
    }

    [Fact]
    public void French_table_reproduces_the_built_in_event_text_and_other_languages_differ()
    {
        var loc = Load();
        var d = TestData.Load();
        var plan = new PlanDeCourse { Slots = 3 };
        plan.Add(new PlanRule(ConditionKind.Gradient, Comparator.Greater, 6, ActionKind.ClimberAttacks));
        var field = BotTeams.Field(d.Balance, TestData.Player(55, 7, plan), 50, 99);
        var r = new RaceSimulator(d.Balance).Run(field, d.Courses["route"], d.Balance.Disciplines["route"], 8);
        Assert.Contains(r.Events, e => e.Kind == EventKind.PlanRule);
        foreach (var e in r.Events)
        {
            loc.Language = "fr";
            Assert.Equal(EventText.Fr(e), EventText.Format(e, loc));
            loc.Language = "en";
            if (e.Kind == EventKind.Attack || e.Kind == EventKind.Start) Assert.NotEqual(EventText.Fr(e), EventText.Format(e, loc));
        }
    }

    [Fact]
    public void Unknown_language_falls_back_to_french_then_to_the_key()
    {
        var loc = Load();
        loc.Language = "ja";
        Assert.Equal("Course", loc.Get("ui.race"));
        Assert.Equal("clé.inconnue", loc.Get("clé.inconnue"));
    }
}
