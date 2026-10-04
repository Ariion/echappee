using System;
using System.Collections.Generic;
using System.Linq;
using Echappee.Config;
using Echappee.Simulation;

namespace Echappee.Economy
{
    public sealed class PackPull
    {
        public RiderCardDef Card;
        public Rarity Rarity;
        public bool IsNew;
        public bool ForcedByGuarantee;
        public long FragmentsGained;
    }

    /// <summary>Ouverture de packs : taux issus de la config, garantie (pitié) par pack, doublons en fragments.</summary>
    public sealed class PackService
    {
        readonly GameData _data;
        readonly BalanceConfig _cfg;

        public PackService(GameData data) { _data = data; _cfg = data.Balance; }

        static Rarity ParseRarity(string s) => (Rarity)Enum.Parse(typeof(Rarity), s);

        public PackConfig Pack(string key) =>
            _cfg.Packs.TryGetValue(key, out var p) ? p : throw new ArgumentException("Pack inconnu : " + key);

        /// <summary>Taux à afficher dans le jeu (obligation Apple / Google), en %.</summary>
        public IReadOnlyDictionary<Rarity, double> DisplayedOdds(string key) =>
            Pack(key).Odds.ToDictionary(kv => ParseRarity(kv.Key), kv => kv.Value);

        public int Cost(string key, int count)
        {
            var p = Pack(key);
            if (count == 1) return p.WattsCost;
            if (count == p.MultiPullCount) return p.MultiPullCost;
            return p.WattsCost * count;
        }

        /// <summary>Tire une rareté. 'pity' = nombre de tirages sans rareté garantie depuis la dernière.</summary>
        public Rarity RollRarity(PackConfig p, int pity, Rng rng, out bool forced)
        {
            forced = false;
            var guarantee = ParseRarity(p.GuaranteeRarity);
            bool mustHit = p.GuaranteeWithin > 0 && pity + 1 >= p.GuaranteeWithin;
            double total = 0;
            foreach (var kv in p.Odds)
                if (!mustHit || ParseRarity(kv.Key) >= guarantee) total += kv.Value;
            double roll = rng.NextDouble() * total, acc = 0;
            Rarity chosen = guarantee;
            foreach (var kv in p.Odds.OrderBy(kv => (int)ParseRarity(kv.Key)))
            {
                var r = ParseRarity(kv.Key);
                if (mustHit && r < guarantee) continue;
                acc += kv.Value;
                if (roll < acc && kv.Value > 0) { chosen = r; break; }
            }
            if (mustHit && chosen >= guarantee) forced = true;
            return chosen;
        }

        /// <summary>Ouvre 'count' packs en débitant les Watts (ou gratuitement si 'free'). Renvoie null si pas assez de Watts.</summary>
        public List<PackPull> Open(PlayerState s, string key, int count, Rng rng, bool free = false)
        {
            var p = Pack(key);
            int cost = free ? 0 : Cost(key, count);
            if (s.Watts < cost) return null;
            s.Watts -= cost;
            var guarantee = ParseRarity(p.GuaranteeRarity);
            var result = new List<PackPull>();
            for (int n = 0; n < count; n++)
            {
                s.PityCounters.TryGetValue(key, out int pity);
                var rarity = RollRarity(p, pity, rng, out bool forced);
                s.PityCounters[key] = rarity >= guarantee ? 0 : pity + 1;

                var pool = _data.Riders.Where(c => c.Rarity == rarity).ToList();
                var card = pool[rng.NextInt(pool.Count)];
                var pull = new PackPull { Card = card, Rarity = rarity, ForcedByGuarantee = forced && rarity == guarantee };
                if (!s.Cards.ContainsKey(card.Id))
                {
                    s.Cards[card.Id] = new OwnedCard { Id = card.Id };
                    pull.IsNew = true;
                }
                else
                {
                    long frag = _cfg.Rarities[rarity.ToString()].DuplicateFragments;
                    s.Cards[card.Id].Fragments += frag;
                    pull.FragmentsGained = frag;
                }
                result.Add(pull);
            }
            return result;
        }

        /// <summary>3 packs gratuits par jour (Pack Pro).</summary>
        public List<PackPull> OpenFreeDaily(PlayerState s, long nowUnix, Rng rng)
        {
            int day = EconomyService.DayIndex(nowUnix);
            if (s.DayIndexOfFreePacks != day) { s.DayIndexOfFreePacks = day; s.FreePacksUsedToday = 0; }
            if (s.FreePacksUsedToday >= _cfg.Economy.FreePacksPerDay) return null;
            s.FreePacksUsedToday++;
            return Open(s, "pro", 1, rng, free: true);
        }

        /// <summary>Coût en fragments pour passer au niveau suivant, ou -1 si niveau maximal.</summary>
        public long LevelUpCost(OwnedCard c)
        {
            var def = _data.Riders.First(r => r.Id == c.Id);
            if (c.Level >= _cfg.Economy.RiderLevelCap) return -1;
            return _cfg.Rarities[def.Rarity.ToString()].FragmentsToLevel[c.Level - 1];
        }

        public bool TryLevelUp(PlayerState s, string cardId)
        {
            if (!s.Cards.TryGetValue(cardId, out var c)) return false;
            long cost = LevelUpCost(c);
            if (cost < 0 || c.Fragments < cost) return false;
            c.Fragments -= cost;
            c.Level++;
            return true;
        }

        /// <summary>Convertit les cartes titulaires en coureurs de simulation.</summary>
        public Team BuildTeam(PlayerState s, string teamId, string teamName)
        {
            var team = new Team { Id = teamId, Name = teamName, IsPlayer = true };
            foreach (var id in s.Starters.Take(_cfg.Race.StartersPerTeam))
            {
                if (!s.Cards.TryGetValue(id, out var owned)) continue;
                var def = _data.Riders.First(r => r.Id == id);
                team.Starters.Add(def.ToRider(owned.Level, _cfg.Economy.LevelStatBonusPct));
            }
            return team;
        }
    }
}
