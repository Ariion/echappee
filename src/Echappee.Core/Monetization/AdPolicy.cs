using System;
using Echappee.Config;
using Echappee.Economy;

namespace Echappee.Monetization
{
    /// <summary>Règles de publicité. Le code d'affichage (AdMob) doit toujours passer par ces méthodes.</summary>
    public sealed class AdPolicy
    {
        readonly BalanceConfig _cfg;
        public AdPolicy(BalanceConfig cfg) { _cfg = cfg; }

        public static int DaysSinceInstall(PlayerState s, long nowUnix) =>
            s.FirstPlayUnix <= 0 ? 0 : (int)((nowUnix - s.FirstPlayUnix) / 86400);

        /// <summary>Jamais pendant une course, jamais avant le jour 3, au plus toutes les 6 minutes, jamais si "Sans pub".</summary>
        public bool CanShowInterstitial(PlayerState s, long nowUnix, bool raceInProgress, bool betweenScreens)
        {
            if (s.NoAds) return false;
            if (!betweenScreens) return false;
            if (_cfg.Ads.NeverDuringRace && raceInProgress) return false;
            if (DaysSinceInstall(s, nowUnix) < _cfg.Ads.NoAdsBeforeDay) return false;
            return nowUnix - s.LastInterstitialUnix >= (long)(_cfg.Ads.InterstitialMinGapMinutes * 60);
        }

        public void MarkInterstitialShown(PlayerState s, long nowUnix) => s.LastInterstitialUnix = nowUnix;

        /// <summary>Vidéo récompensée : toujours au choix du joueur, jamais pendant une course, gardée même avec "Sans pub".</summary>
        public bool CanOfferRewardedVideo(bool raceInProgress) => !(_cfg.Ads.NeverDuringRace && raceInProgress);
    }
}
