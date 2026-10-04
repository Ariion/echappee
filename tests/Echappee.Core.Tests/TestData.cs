using System;
using System.IO;
using Echappee.Config;
using Echappee.Simulation;

public static class TestData
{
    static GameData _d;
    public static GameData Load()
    {
        if (_d != null) return _d;
        string dir = Path.Combine(AppContext.BaseDirectory, "data");
        _d = GameData.FromJson(File.ReadAllText(Path.Combine(dir, "balance.json")),
            File.ReadAllText(Path.Combine(dir, "courses.json")), File.ReadAllText(Path.Combine(dir, "riders.json")));
        return _d;
    }

    public static Team Player(double level, ulong seed = 7, PlanDeCourse plan = null)
    {
        var t = BotTeams.Create("player", "Mon équipe", level, new Rng(seed), Load().Balance.Race.StartersPerTeam);
        t.IsPlayer = true; t.Plan = plan;
        return t;
    }
}
