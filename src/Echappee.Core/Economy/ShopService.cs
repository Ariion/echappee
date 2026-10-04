using System;
using System.Collections.Generic;
using System.Linq;
using Echappee.Config;
using Echappee.Simulation;

namespace Echappee.Economy
{
    /// <summary>
    /// Effets des achats, bonus de connexion et Pass Saison. Le paiement lui-même (stores) est hors du cœur :
    /// ce service est appelé APRÈS la confirmation d'un achat, ou directement en mode test.
    /// </summary>
    public sealed class ShopService
    {
        readonly GameData _data;
        readonly BalanceConfig _cfg;
        readonly EconomyService _eco;
        readonly PackService _packs;

        public ShopService(GameData data, EconomyService eco, PackService packs)
        { _data = data; _cfg = data.Balance; _eco = eco; _packs = packs; }

        public bool BuyWattsPack(PlayerState s, string id)
        {
            var p = _cfg.Shop.WattsPacks.FirstOrDefault(x => x.Id == id);
            if (p == null) return false;
            s.Watts += p.Watts; s.EverPurchased = true;
            return true;
        }

        /// <summary>Offre de départ : 1 coureur Élite, des Watts, boost x2 pendant 24 h. Une seule fois, pendant sa fenêtre.</summary>
        public bool BuyStarter(PlayerState s, OfferPolicyLike offers, long now, Rng rng, out RiderCardDef granted)
        {
            granted = null;
            if (s.StarterOfferBought || !offers.IsStarterActive(s, now)) return false;
            s.StarterOfferBought = true; s.EverPurchased = true;
            s.Watts += _cfg.Shop.StarterWatts;
            s.BoostUntilUnix = Math.Max(s.BoostUntilUnix, now) + (long)(_cfg.Shop.StarterBoostHours * 3600);
            granted = GrantRandom(s, Rarity.Elite, rng);
            return true;
        }

        public bool BuyNoAds(PlayerState s) { s.NoAds = true; s.EverPurchased = true; return true; }

        public bool BuyStudioPattern(PlayerState s, string pattern)
        {
            if (!s.UnlockedPatterns.Contains(pattern)) s.UnlockedPatterns.Add(pattern);
            s.EverPurchased = true; return true;
        }

        public RiderCardDef GrantRandom(PlayerState s, Rarity r, Rng rng)
        {
            var pool = _data.Riders.Where(c => c.Rarity == r).ToList();
            // priorité aux cartes non possédées, sinon doublon -> fragments
            var fresh = pool.Where(c => !s.Cards.ContainsKey(c.Id)).ToList();
            var card = (fresh.Count > 0 ? fresh : pool)[rng.NextInt(fresh.Count > 0 ? fresh.Count : pool.Count)];
            if (!s.Cards.ContainsKey(card.Id)) s.Cards[card.Id] = new OwnedCard { Id = card.Id };
            else s.Cards[card.Id].Fragments += _cfg.Rarities[r.ToString()].DuplicateFragments;
            return card;
        }

        /// <summary>Premier lancement : quelques Amateurs titulaires + un Élite garanti (attachement dès les 5 premières minutes).</summary>
        public void NewGame(PlayerState s, long now, Rng rng)
        {
            s.FirstPlayUnix = now; s.LastSeenUnix = now; s.SessionCount = 1;
            for (int i = 0; i < _cfg.Economy.StartingStarters; i++) s.Starters.Add(GrantRandom(s, Rarity.Amateur, rng).Id);
            s.Starters.Add(GrantRandom(s, Rarity.Elite, rng).Id);
            s.Watts = 30;
        }

        /// <summary>Bonus de connexion quotidien. Renvoie les Watts gagnés (0 si déjà pris aujourd'hui).</summary>
        public int ClaimDailyLogin(PlayerState s, long now)
        {
            int day = EconomyService.DayIndex(now);
            if (s.LastLoginDay == day) return 0;
            s.LastLoginDay = day;
            s.Watts += _cfg.Economy.DailyLoginWatts;
            return _cfg.Economy.DailyLoginWatts;
        }

        // ---------- Pass Saison : 30 paliers sur 28 jours ----------

        public void EnsureSeason(PlayerState s, long now)
        {
            long len = (long)_cfg.Shop.SeasonPassDays * 86400;
            if (s.PassStartUnix == 0 || now >= s.PassStartUnix + len)
            {
                s.PassStartUnix = now; s.PassRaces = 0; s.PassPremium = false;
                s.PassClaimedFree.Clear(); s.PassClaimedPremium.Clear();
            }
        }

        public long PassSecondsLeft(PlayerState s, long now) =>
            Math.Max(0, s.PassStartUnix + (long)_cfg.Shop.SeasonPassDays * 86400 - now);

        public int PassTierReached(PlayerState s) =>
            Math.Min(_cfg.Shop.SeasonPassTiers, s.PassRaces / Math.Max(1, _cfg.Shop.PassRacesPerTier));

        public bool CanClaimPass(PlayerState s, int tier, bool premium)
        {
            if (tier < 1 || tier > _cfg.Shop.SeasonPassTiers || tier > PassTierReached(s)) return false;
            if (premium) return s.PassPremium && !s.PassClaimedPremium.Contains(tier);
            return !s.PassClaimedFree.Contains(tier);
        }

        public string PassRewardText(int tier, bool premium)
        {
            if (premium) return tier == _cfg.Shop.SeasonPassTiers ? "legend" : "w" + _cfg.Shop.PassPremiumWattsPerTier;
            return tier % _cfg.Shop.PassFreeWattsEvery == 0 ? "w" + _cfg.Shop.PassFreeWatts : "p" + _cfg.Shop.PassFreePrimesMinutes;
        }

        public bool ClaimPass(PlayerState s, int tier, bool premium, long now, Rng rng)
        {
            if (!CanClaimPass(s, tier, premium)) return false;
            if (premium)
            {
                s.PassClaimedPremium.Add(tier);
                if (tier == _cfg.Shop.SeasonPassTiers) GrantRandom(s, Rarity.Legende, rng);
                else s.Watts += _cfg.Shop.PassPremiumWattsPerTier;
            }
            else
            {
                s.PassClaimedFree.Add(tier);
                if (tier % _cfg.Shop.PassFreeWattsEvery == 0) s.Watts += _cfg.Shop.PassFreeWatts;
                else s.Primes = s.Primes + _eco.IncomePerSecond(s, now) * (_cfg.Shop.PassFreePrimesMinutes * 60) + new Numbers.BigAmount(10);
            }
            return true;
        }

        public bool BuyPass(PlayerState s) { s.PassPremium = true; s.EverPurchased = true; return true; }
    }

    /// <summary>Lecture seule de la politique d'offres (pour ne pas lier ShopService à une classe concrète en test).</summary>
    public interface OfferPolicyLike { bool IsStarterActive(PlayerState s, long nowUnix); }
}
