using Echappee.Config;

namespace Echappee.Simulation
{
    public struct PlanReport
    {
        public int Races;
        public double WinRateBefore, WinRateAfter;      // 1re place
        public double PodiumBefore, PodiumAfter;        // 3 premières
        public double AvgRankBefore, AvgRankAfter;
    }

    /// <summary>"Simulation sur 100 courses" de l'écran Plan de course : taux de victoire avant / après.</summary>
    public static class PlanAnalyzer
    {
        public static PlanReport Compare(BalanceConfig cfg, Course course, DisciplineConfig disc, Team player,
            PlanDeCourse before, PlanDeCourse after, double botLevel, ulong seed, int races = 100)
        {
            var sim = new RaceSimulator(cfg);
            var rep = new PlanReport { Races = races };
            for (int i = 0; i < races; i++)
            {
                ulong s = seed + (ulong)i * 7919UL;
                // mêmes adversaires et même graine des deux côtés : seule la stratégie change
                Tally(sim, cfg, course, disc, player, before, botLevel, s, ref rep.WinRateBefore, ref rep.PodiumBefore, ref rep.AvgRankBefore);
                Tally(sim, cfg, course, disc, player, after, botLevel, s, ref rep.WinRateAfter, ref rep.PodiumAfter, ref rep.AvgRankAfter);
            }
            rep.WinRateBefore /= races; rep.WinRateAfter /= races;
            rep.PodiumBefore /= races; rep.PodiumAfter /= races;
            rep.AvgRankBefore /= races; rep.AvgRankAfter /= races;
            return rep;
        }

        static void Tally(RaceSimulator sim, BalanceConfig cfg, Course course, DisciplineConfig disc, Team player,
            PlanDeCourse plan, double botLevel, ulong seed, ref double win, ref double podium, ref double avg)
        {
            var p = new Team { Id = player.Id, Name = player.Name, IsPlayer = true, Starters = player.Starters, Plan = plan };
            var field = BotTeams.Field(cfg, p, botLevel, seed ^ 0xABCDEF);
            var r = sim.Run(field, course, disc, seed);
            if (r.PlayerRank == 1) win++;
            if (r.PlayerRank <= 3) podium++;
            avg += r.PlayerRank;
        }
    }
}
