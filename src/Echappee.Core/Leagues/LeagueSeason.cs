using System;
using System.Collections.Generic;
using System.Linq;
using Echappee.Config;
using Echappee.Simulation;

namespace Echappee.Leagues
{
    public enum LeagueOutcome { Promoted, Stayed, Relegated }

    public sealed class LeagueEntry
    {
        public string Id, Name;
        public double Power;
        public int Points;
        public bool IsPlayer;
    }

    /// <summary>
    /// Ligue asynchrone : le joueur + des équipes simulées. Pas de serveur temps réel.
    /// Les "fantômes" de vrais joueurs pourront remplacer des équipes simulées plus tard (même structure).
    /// </summary>
    public sealed class LeagueSeason
    {
        readonly BalanceConfig _cfg;
        public int Tier;
        public long StartUnix;
        public List<LeagueEntry> Entries = new List<LeagueEntry>();

        public LeagueSeason(BalanceConfig cfg, int tier, double playerPower, long startUnix, ulong seed)
        {
            _cfg = cfg; Tier = tier; StartUnix = startUnix;
            var rng = new Rng(seed);
            Entries.Add(new LeagueEntry { Id = "player", Name = "Toi", Power = playerPower, IsPlayer = true });
            int ghosts = cfg.Leagues.TeamsPerLeague - 1;
            for (int i = 0; i < ghosts; i++)
            {
                // puissances étalées autour de celle du joueur : de -spread à +spread
                double f = 1 + cfg.Leagues.GhostPowerSpread * (2.0 * i / Math.Max(1, ghosts - 1) - 1) + rng.Gauss() * 0.03;
                Entries.Add(new LeagueEntry { Id = "ghost" + i, Name = BotTeams.NameAt(i + tier * 3), Power = Math.Round(playerPower * f) });
            }
        }

        public long EndUnix => StartUnix + (long)_cfg.Leagues.SeasonDays * 86400;
        public string TierName => _cfg.Leagues.Tiers[Tier];
        public bool IsOver(long nowUnix) => nowUnix >= EndUnix;

        /// <summary>Le joueur a fini 'playerPlace' dans une course : lui et les fantômes marquent des points.</summary>
        public void RecordRace(int playerPlace, Rng rng)
        {
            var table = _cfg.Economy.LeaguePointsByPlace;
            int Pts(int place) => table[Math.Max(0, Math.Min(place - 1, table.Length - 1))];
            Entries.First(e => e.IsPlayer).Points += Pts(playerPlace);

            var ghosts = Entries.Where(e => !e.IsPlayer)
                .Select(e => new { e, score = e.Power * (1 + rng.Gauss() * 0.15) })
                .OrderByDescending(x => x.score).ToList();
            for (int k = 0; k < ghosts.Count; k++)
            {
                int ghostRank = k + 1;
                int place = ghostRank + (playerPlace <= ghostRank ? 1 : 0);   // le joueur occupe une place du classement
                ghosts[k].e.Points += Pts(place);
            }
        }

        public List<LeagueEntry> Standings() =>
            Entries.OrderByDescending(e => e.Points).ThenByDescending(e => e.Power).ToList();

        public int PlayerRank() => Standings().FindIndex(e => e.IsPlayer) + 1;

        public LeagueOutcome Outcome()
        {
            int rank = PlayerRank(), n = Entries.Count;
            if (rank <= _cfg.Leagues.Promoted && Tier < _cfg.Leagues.Tiers.Length - 1) return LeagueOutcome.Promoted;
            if (rank > n - _cfg.Leagues.Relegated && Tier > 0) return LeagueOutcome.Relegated;
            return LeagueOutcome.Stayed;
        }

        public int NextTier()
        {
            switch (Outcome())
            {
                case LeagueOutcome.Promoted: return Tier + 1;
                case LeagueOutcome.Relegated: return Tier - 1;
                default: return Tier;
            }
        }

        /// <summary>Zone de l'équipe au classement (promotion / maintien / relégation) pour colorer l'écran Ligues.</summary>
        public LeagueOutcome ZoneOfRank(int rank)
        {
            if (rank <= _cfg.Leagues.Promoted) return LeagueOutcome.Promoted;
            if (rank > Entries.Count - _cfg.Leagues.Relegated) return LeagueOutcome.Relegated;
            return LeagueOutcome.Stayed;
        }

        /// <summary>Une discipline est débloquée quand la ligue atteinte (ou dépassée) est celle demandée.</summary>
        public static bool IsDisciplineUnlocked(BalanceConfig cfg, DisciplineConfig d, int bestTier)
        {
            if (string.IsNullOrEmpty(d.UnlockLeague)) return true;
            int need = Array.IndexOf(cfg.Leagues.Tiers, d.UnlockLeague);
            return need >= 0 && bestTier >= need;
        }
    }
}
