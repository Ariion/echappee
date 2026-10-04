using System;
using System.IO;
using System.Linq;
using Echappee.Config;
using Echappee.Simulation;

// Outil de développement : regarder une course et mesurer l'équilibrage sans Unity.
//   dotnet run --project tools/Echappee.Cli -- race [seed] [discipline] [niveau]
//   dotnet run --project tools/Echappee.Cli -- stats [discipline] [niveau]
//   dotnet run --project tools/Echappee.Cli -- plan [discipline] [niveau]
var dir = Path.Combine(AppContext.BaseDirectory, "data");
var data = GameData.FromJson(File.ReadAllText(Path.Combine(dir, "balance.json")),
    File.ReadAllText(Path.Combine(dir, "courses.json")), File.ReadAllText(Path.Combine(dir, "riders.json")));
var cfg = data.Balance;
string cmd = args.Length > 0 ? args[0] : "race";

string Arg(int i, string def) => args.Length > i ? args[i] : def;

switch (cmd)
{
    case "race":
    {
        ulong seed = ulong.Parse(Arg(1, "1"));
        var disc = cfg.Disciplines[Arg(2, "route")];
        double lvl = double.Parse(Arg(3, "50"));
        var p = BotTeams.Create("player", "Cadence Mistral", lvl, new Rng(7), cfg.Race.StartersPerTeam); p.IsPlayer = true;
        var plan = new PlanDeCourse { Slots = 5 };
        plan.Add(new PlanRule(ConditionKind.Gradient, Comparator.Greater, 6, ActionKind.ClimberAttacks));
        plan.Add(new PlanRule(ConditionKind.KmRemaining, Comparator.Less, 20, ActionKind.SprinterLaunch));
        p.Plan = plan;
        var field = BotTeams.Field(cfg, p, 50, 99);
        var r = new RaceSimulator(cfg).Run(field, data.Courses[disc.Circuit], disc, seed);
        foreach (var e in r.Events) Console.WriteLine(e);
        Console.WriteLine();
        foreach (var s in r.Standings)
            Console.WriteLine($"{s.Rank,2}. {field[s.TeamIndex].Name,-16} {s.FinishTime,6:0.0}s");
        Console.WriteLine("empreinte: " + r.Fingerprint());
        break;
    }
    case "stats":
    {
        var disc = cfg.Disciplines[Arg(1, "route")];
        double lvl = double.Parse(Arg(2, "50"));
        var sim = new RaceSimulator(cfg);
        double fat = 0, pfat = 0; int n = 300, splits = 0, brk = 0, atk = 0, exh = 0; double t1 = 0, spread = 0; var wins = new int[12];
        for (int i = 1; i <= n; i++)
        {
            var p = BotTeams.Create("player", "Joueur", lvl, new Rng(7), cfg.Race.StartersPerTeam); p.IsPlayer = true;
            var r = sim.Run(BotTeams.Field(cfg, p, 50, 99), data.Courses[disc.Circuit], disc, (ulong)i);
            wins[r.Standings[0].TeamIndex]++; fat += r.Standings.Average(x => x.Fatigue); pfat += r.Standings.First(x => x.TeamIndex == 0).Fatigue;
            if (r.Events.Any(e => e.Kind == EventKind.Split)) splits++;
            if (r.Events.Any(e => e.Kind == EventKind.Breakaway)) brk++;
            atk += r.Events.Count(e => e.Kind == EventKind.Attack);
            exh += r.Events.Count(e => e.Kind == EventKind.Exhausted);
            t1 += r.Standings[0].FinishTime; spread += r.Standings[^1].FinishTime - r.Standings[0].FinishTime;
        }
        Console.WriteLine($"{n} courses, niveau joueur {lvl} vs 50");
        Console.WriteLine($"durée moyenne du vainqueur {t1 / n:0.0}s, écart 1er-dernier {spread / n:0.0}s");
        Console.WriteLine($"courses avec scission {100.0 * splits / n:0}%, avec échappée {100.0 * brk / n:0}%");
        Console.WriteLine($"attaques/course {(double)atk / n:0.0}, épuisements/course {(double)exh / n:0.0}");
        Console.WriteLine($"fatigue moyenne à l'arrivée {fat / n:P0}, joueur {pfat / n:P0}");
        Console.WriteLine("victoires par équipe (0 = joueur): " + string.Join(" ", wins));
        break;
    }
    case "plan":
    {
        var disc = cfg.Disciplines[Arg(1, "route")];
        double lvl = double.Parse(Arg(2, "50"));
        var p = BotTeams.Create("player", "Joueur", lvl, new Rng(7), cfg.Race.StartersPerTeam); p.IsPlayer = true;
        var none = new PlanDeCourse { Slots = 5 };
        var naive = new PlanDeCourse { Slots = 5 };
        naive.Add(new PlanRule(ConditionKind.Gradient, Comparator.Greater, 6, ActionKind.ClimberAttacks));
        naive.Add(new PlanRule(ConditionKind.KmRemaining, Comparator.Less, 20, ActionKind.SprinterLaunch));
        var smart = new PlanDeCourse { Slots = 5 };
        smart.Add(new PlanRule(ConditionKind.KmRemaining, Comparator.Less, 12, ActionKind.SprinterLaunch));
        smart.Add(new PlanRule(ConditionKind.Fatigue, Comparator.Greater, 65, ActionKind.StayInWheel));
        smart.Add(new PlanRule(ConditionKind.Gradient, Comparator.Greater, 6, ActionKind.ClimberAttacks));
        var wheel = new PlanDeCourse { Slots = 5 };
        wheel.Add(new PlanRule(ConditionKind.Fatigue, Comparator.Greater, 0, ActionKind.StayInWheel));
        foreach (var (name, plan) in new[] { ("naïf", naive), ("malin", smart), ("toujours dans la roue", wheel) })
        {
            var rep = PlanAnalyzer.Compare(cfg, data.Courses[disc.Circuit], disc, p, none, plan, 50, 1000, 200);
            Console.WriteLine($"{name,-22} victoires {rep.WinRateBefore:P0} -> {rep.WinRateAfter:P0}   podiums {rep.PodiumBefore:P0} -> {rep.PodiumAfter:P0}   rang moyen {rep.AvgRankBefore:0.0} -> {rep.AvgRankAfter:0.0}");
        }
        break;
    }
    default:
        Console.WriteLine("Commandes : race | stats | plan");
        break;
}
