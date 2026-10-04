using System;
using Echappee.Config;
using Echappee.Numbers;

namespace Echappee.Economy
{
    /// <summary>Revenus, améliorations, revenu hors ligne et primes de course. Aucune valeur d'équilibrage en dur.</summary>
    public sealed class EconomyService
    {
        readonly BalanceConfig _cfg;
        public EconomyService(BalanceConfig cfg) { _cfg = cfg; }

        public InfrastructureConfig Infra(string id) =>
            _cfg.Infrastructures.Find(i => i.Id == id) ?? throw new ArgumentException("Infrastructure inconnue : " + id);

        /// <summary>Revenu par seconde d'une infrastructure à un niveau donné (paliers x2 inclus).</summary>
        public BigAmount InfraIncome(InfrastructureConfig inf, int level)
        {
            if (level <= 0) return BigAmount.Zero;
            double mult = 1;
            if (inf.MilestoneLevels != null)
                foreach (var m in inf.MilestoneLevels) if (level >= m) mult *= inf.MilestoneMultiplier;
            return new BigAmount(inf.BaseIncome * level) * mult;
        }

        public bool BoostActive(PlayerState s, long nowUnix) => s.BoostUntilUnix > nowUnix;

        public BigAmount IncomePerSecond(PlayerState s, long nowUnix)
        {
            var total = BigAmount.Zero;
            foreach (var inf in _cfg.Infrastructures) total += InfraIncome(inf, s.InfraLevel(inf.Id));
            if (BoostActive(s, nowUnix)) total = total * _cfg.Economy.BoostVideoMultiplier;
            return total;
        }

        /// <summary>Coût du prochain niveau (le niveau 1 coûte BaseCost).</summary>
        public BigAmount UpgradeCost(InfrastructureConfig inf, int currentLevel) =>
            BigAmount.Scale(inf.BaseCost, inf.CostGrowth, currentLevel);

        /// <summary>Gain de revenu/s apporté par le prochain niveau (affiché « +126 → +129 »).</summary>
        public BigAmount UpgradeGain(InfrastructureConfig inf, int currentLevel) =>
            InfraIncome(inf, currentLevel + 1) - InfraIncome(inf, currentLevel);

        public bool TryUpgrade(PlayerState s, string infraId)
        {
            var inf = Infra(infraId);
            int lvl = s.InfraLevel(infraId);
            var cost = UpgradeCost(inf, lvl);
            if (s.Primes < cost) return false;
            s.Primes = s.Primes - cost;
            s.InfraLevels[infraId] = lvl + 1;
            return true;
        }

        /// <summary>Combien d'améliorations d'affilée peut-on acheter (affichage x1 / x10 / max).</summary>
        public int MaxAffordable(PlayerState s, InfrastructureConfig inf, int cap = 1000)
        {
            int lvl = s.InfraLevel(inf.Id), n = 0;
            var budget = s.Primes;
            while (n < cap)
            {
                var c = UpgradeCost(inf, lvl + n);
                if (budget < c) break;
                budget = budget - c; n++;
            }
            return n;
        }

        public void Tick(PlayerState s, double seconds, long nowUnix) =>
            s.Primes = s.Primes + IncomePerSecond(s, nowUnix) * seconds;

        public sealed class OfflineResult
        {
            public double SecondsCredited, SecondsAway;
            public bool Capped;
            public BigAmount Earned;
        }

        /// <summary>Revenu hors ligne plafonné (4 h). Une vidéo le multiplie, sans lever le plafond.</summary>
        public OfflineResult OfflineEarnings(PlayerState s, long nowUnix, bool watchedVideo)
        {
            double away = Math.Max(0, nowUnix - s.LastSeenUnix);
            double cap = _cfg.Economy.OfflineCapHours * 3600;
            double credited = Math.Min(away, cap);
            var income = IncomePerSecond(s, nowUnix);
            var earned = income * credited * (watchedVideo ? _cfg.Economy.OfflineVideoMultiplier : 1.0);
            return new OfflineResult { SecondsAway = away, SecondsCredited = credited, Capped = away > cap, Earned = earned };
        }

        public OfflineResult ClaimOffline(PlayerState s, long nowUnix, bool watchedVideo)
        {
            var r = OfflineEarnings(s, nowUnix, watchedVideo);
            s.Primes = s.Primes + r.Earned;
            s.LastSeenUnix = nowUnix;
            return r;
        }

        public void StartVideoBoost(PlayerState s, long nowUnix) =>
            s.BoostUntilUnix = Math.Max(s.BoostUntilUnix, nowUnix) + (long)(_cfg.Economy.BoostVideoHours * 3600);

        public sealed class RaceReward
        {
            public BigAmount Primes;
            public int LeaguePoints;
            public double CardChance;
        }

        public RaceReward RewardForPlace(PlayerState s, int place, long nowUnix)
        {
            int i = Math.Max(0, Math.Min(place - 1, _cfg.Economy.PlaceRewardFactor.Length - 1));
            var income = IncomePerSecond(s, nowUnix);
            // même au tout début (revenu 0) une course doit rapporter quelque chose
            var basis = income.IsZero ? new BigAmount(1) : income;
            return new RaceReward
            {
                Primes = basis * _cfg.Economy.RaceRewardSeconds * _cfg.Economy.PlaceRewardFactor[i],
                LeaguePoints = _cfg.Economy.LeaguePointsByPlace[Math.Min(i, _cfg.Economy.LeaguePointsByPlace.Length - 1)],
                CardChance = _cfg.Economy.CardChanceByPlace[Math.Min(i, _cfg.Economy.CardChanceByPlace.Length - 1)]
            };
        }

        public static int DayIndex(long unix) => (int)(unix / 86400);

        /// <summary>Vidéos de bonus : environ 20 Watts par jour.</summary>
        public bool TryClaimVideoWatts(PlayerState s, long nowUnix, int watts)
        {
            int day = DayIndex(nowUnix);
            if (s.DayIndexOfVideoWatts != day) { s.DayIndexOfVideoWatts = day; s.VideoWattsToday = 0; }
            if (s.VideoWattsToday + watts > _cfg.Economy.VideoWattsPerDay) return false;
            s.VideoWattsToday += watts; s.Watts += watts;
            return true;
        }
    }
}
