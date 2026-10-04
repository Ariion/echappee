using System.Collections.Generic;
using Echappee.Config;

namespace Echappee.Simulation
{
    /// <summary>Équipes simulées (adversaires, fantômes de ligue).</summary>
    public static class BotTeams
    {
        static readonly string[] Names =
        {
            "Nova Club", "Vent Debout", "Cadence 90", "Pignon Fixe", "Sprint Nord", "Col Raiders",
            "Dérailleurs FC", "Les Rouleurs", "Mistral Racing", "Brume Team", "Chaîne Libre", "Maillon Fort",
            "Peloton Sud", "Roue Libre", "Tour de Bras", "Garde-Boue"
        };

        public static string NameAt(int i) => Names[i % Names.Length];

        /// <summary>Équipe dont chaque titulaire a des stats autour de 'level' (0-100), avec un écart de spécialité.</summary>
        public static Team Create(string id, string name, double level, Rng rng, int starters)
        {
            var t = new Team { Id = id, Name = name };
            for (int i = 0; i < starters; i++)
            {
                double j() => level + rng.Gauss() * 6;
                var stats = new RiderStats(j(), j(), j(), j());
                int spec = rng.NextInt(4);
                if (spec == 0) stats.Sprint += 10; else if (spec == 1) stats.Climb += 10; else if (spec == 2) stats.Rouleur += 10; else stats.Technique += 10;
                t.Starters.Add(new Rider(id + "_" + i, Initial(rng) + ". " + Surname(rng), Clamp(stats)));
            }
            return t;
        }

        static readonly string[] Sur = { "Brandão", "Varenne", "Moreau", "Ricci", "Haas", "Costa", "Navarro", "Keller", "Dubois", "Santos", "Novak", "Fabre" };
        static string Surname(Rng r) => Sur[r.NextInt(Sur.Length)];
        static string Initial(Rng r) => ((char)('A' + r.NextInt(26))).ToString();
        static RiderStats Clamp(RiderStats s) => new RiderStats(C(s.Sprint), C(s.Climb), C(s.Rouleur), C(s.Technique));
        static double C(double v) => v < 5 ? 5 : v > 100 ? 100 : v;

        /// <summary>Peloton de départ : le joueur + (Teams-1) équipes simulées de niveau proche.</summary>
        public static List<Team> Field(BalanceConfig cfg, Team player, double botLevel, ulong seed)
        {
            var rng = new Rng(seed);
            var list = new List<Team> { player };
            for (int i = 0; i < cfg.Race.Teams - 1; i++)
            {
                double lvl = botLevel + (i - (cfg.Race.Teams - 2) / 2.0) * 1.5;
                list.Add(Create("bot" + i, NameAt(i), lvl, rng, cfg.Race.StartersPerTeam));
            }
            return list;
        }
    }
}
