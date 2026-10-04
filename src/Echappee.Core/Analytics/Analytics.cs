using System.Collections.Generic;
using Echappee.Config;

namespace Echappee.Analytics
{
    /// <summary>Noms d'événements (Firebase Analytics, gratuit). Un seul endroit pour ne pas se tromper de nom.</summary>
    public static class EventNames
    {
        public const string SessionStart = "session_start_ec";
        public const string RaceComplete = "race_complete";
        public const string UpgradeBought = "upgrade_bought";
        public const string PackOpened = "pack_opened";
        public const string RewardedVideoWatched = "rewarded_video_watched";
        public const string InterstitialShown = "interstitial_shown";
        public const string IapPurchase = "iap_purchase";
        public const string LeaguePromoted = "league_promoted";
        public const string PlanSaved = "plan_saved";
        public const string TutorialStep = "tutorial_step";
        public const string PosterShared = "poster_shared";
    }

    public interface IAnalytics
    {
        void Log(string name, IDictionary<string, object> parameters = null);
    }

    public sealed class NullAnalytics : IAnalytics
    {
        public void Log(string name, IDictionary<string, object> parameters = null) { }
    }

    /// <summary>Utile en test : garde les événements en mémoire.</summary>
    public sealed class MemoryAnalytics : IAnalytics
    {
        public readonly List<KeyValuePair<string, IDictionary<string, object>>> Events = new List<KeyValuePair<string, IDictionary<string, object>>>();
        public void Log(string name, IDictionary<string, object> parameters = null) =>
            Events.Add(new KeyValuePair<string, IDictionary<string, object>>(name, parameters));
    }

    public enum RetentionVerdict { Stop, Fix, SoftLaunchOk, OnTarget }

    /// <summary>Décision chiffrée de soft launch : une seule définition des seuils (data/balance.json, section Gates).</summary>
    public static class RetentionGate
    {
        public static RetentionVerdict Evaluate(RetentionGates g, double d1, double d7, double d30)
        {
            if (d7 < g.StopD7) return RetentionVerdict.Stop;
            if (d1 < g.SoftLaunchD1 || d7 < g.SoftLaunchD7) return RetentionVerdict.Fix;
            if (d1 >= g.TargetD1 && d7 >= g.TargetD7 && d30 >= g.TargetD30) return RetentionVerdict.OnTarget;
            return RetentionVerdict.SoftLaunchOk;
        }
    }
}
