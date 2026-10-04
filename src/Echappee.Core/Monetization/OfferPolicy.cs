using System;
using Echappee.Config;
using Echappee.Economy;

namespace Echappee.Monetization
{
    public enum OfferKind { Starter, SeasonPass, NoAds, LimitedOffer, StudioPremium }

    /// <summary>Quand proposer chaque offre (règles du tableau Business model).</summary>
    public sealed class OfferPolicy
    {
        readonly BalanceConfig _cfg;
        public OfferPolicy(BalanceConfig cfg) { _cfg = cfg; }

        public bool IsStarterActive(PlayerState s, long nowUnix) =>
            !s.StarterOfferBought && s.StarterOfferStartUnix > 0 &&
            nowUnix < s.StarterOfferStartUnix + (long)(_cfg.Offers.StarterDurationHours * 3600);

        public long StarterSecondsLeft(PlayerState s, long nowUnix) =>
            IsStarterActive(s, nowUnix) ? s.StarterOfferStartUnix + (long)(_cfg.Offers.StarterDurationHours * 3600) - nowUnix : 0;

        /// <summary>Déclenche l'offre de départ (une seule fois) après la 2e session.</summary>
        public bool TryStartStarter(PlayerState s, long nowUnix)
        {
            if (s.StarterOfferStartUnix > 0 || s.StarterOfferBought) return false;
            if (s.SessionCount < _cfg.Offers.StarterAfterSession) return false;
            s.StarterOfferStartUnix = nowUnix;
            return true;
        }

        public bool ShouldShow(OfferKind kind, PlayerState s, long nowUnix)
        {
            int days = AdPolicy.DaysSinceInstall(s, nowUnix);
            switch (kind)
            {
                case OfferKind.Starter: return IsStarterActive(s, nowUnix);
                case OfferKind.SeasonPass: return days >= _cfg.Offers.SeasonPassFromDay;
                case OfferKind.NoAds: return !s.NoAds && !s.EverPurchased && s.RacesPlayed >= _cfg.Offers.NoAdsAfterRacesWithoutPurchase;
                case OfferKind.LimitedOffer:
                    return days >= _cfg.Offers.LimitedOfferMinDays &&
                           (s.LastLimitedOfferUnix == 0 || nowUnix - s.LastLimitedOfferUnix >= (long)_cfg.Offers.LimitedOfferMinDays * 86400);
                case OfferKind.StudioPremium: return s.StageWins >= _cfg.Offers.StudioAfterFirstStageWins;
                default: return false;
            }
        }
    }
}
