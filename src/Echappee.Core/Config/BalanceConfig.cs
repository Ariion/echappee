using System.Collections.Generic;
using Newtonsoft.Json;

namespace Echappee.Config
{
    /// <summary>
    /// Toutes les valeurs d'équilibrage (prix, taux, seuils). Chargées depuis data/balance.json
    /// ou depuis Remote Config (même JSON). Aucune valeur de ce genre ne doit être en dur dans le code.
    /// </summary>
    public sealed class BalanceConfig
    {
        public RaceTuning Race = new RaceTuning();
        public Dictionary<string, DisciplineConfig> Disciplines = new Dictionary<string, DisciplineConfig>();
        public List<InfrastructureConfig> Infrastructures = new List<InfrastructureConfig>();
        public EconomyTuning Economy = new EconomyTuning();
        public Dictionary<string, PackConfig> Packs = new Dictionary<string, PackConfig>();
        public Dictionary<string, RarityConfig> Rarities = new Dictionary<string, RarityConfig>();
        public LeagueTuning Leagues = new LeagueTuning();
        public AdTuning Ads = new AdTuning();
        public OfferTuning Offers = new OfferTuning();
        public RetentionGates Gates = new RetentionGates();

        public static BalanceConfig FromJson(string json) =>
            JsonConvert.DeserializeObject<BalanceConfig>(json);
    }

    public sealed class RaceTuning
    {
        public int Teams = 12;
        public int StartersPerTeam = 5;
        public double TickSeconds = 0.1;
        public double CourseMeters = 1000;       // longueur abstraite d'une course de 60 s
        public double BaseSpeedMps = 16.7;       // vitesse moyenne de référence
        public double StatSpeedSpan = 0.18;      // écart de vitesse entre une équipe 0 et une équipe 100
        public double MinWeightFactor = 0.4;     // poids de discipline 0 => facteur 0.4 ; 3 => 1.0
        public double NoiseSpeed = 0.012;
        public double DraftRangeM = 9;
        public double DraftSpeedBonus = 0.035;
        public double DraftFatigueFactor = 0.65;
        public double FatigueBase = 0.0017;      // fatigue/s au rythme de référence
        public double FatigueRecoveryDescent = 0.004;
        public double ExhaustionStart = 0.65;
        public double ExhaustionSlowdown = 0.30;
        public double AttackChancePerSecond = 0.05;
        public double AttackDurationS = 6;
        public double AttackSpeedBonus = 0.07;
        public double AttackFatigueFactor = 2.0;
        public double SplitGapM = 14;
        public double BreakawayGapM = 30;
        public double SegmentGradientClimb = 7;
        public double SegmentGradientDescent = -6;
        public double SnapshotEverySeconds = 0.5;
        public double MaxTimeFactor = 1.6;
        public PlanTuning Plan = new PlanTuning();
    }

    public sealed class PlanTuning
    {
        public double ClimberAttackSpeed = 0.09;
        public double ClimberAttackFatigue = 2.0;
        public double StayInWheelSpeed = -0.02;
        public double StayInWheelFatigue = 0.5;
        public double SprintLaunchSpeed = 0.10;
        public double SprintLaunchFatigue = 2.5;
        public double GroupChaseSpeed = 0.05;
        public double GroupChaseFatigue = 1.3;
        public double TotalKmDisplay = 120;       // km affichés pour la règle "kilomètres restants"
    }

    public sealed class DisciplineConfig
    {
        public string Name;
        public int Sprint, Climb, Rouleur, Technique;
        public double DurationSeconds;
        public string UnlockLeague;               // null = dès le départ
        public string Circuit;                    // identifiant dans courses.json
    }

    public sealed class InfrastructureConfig
    {
        public string Id;
        public string Name;
        public double BaseCost;
        public double CostGrowth;
        public double BaseIncome;                 // Primes/s au niveau 1
        public int[] MilestoneLevels;             // chaque palier double le revenu
        public double MilestoneMultiplier = 2;
    }

    public sealed class EconomyTuning
    {
        public double OfflineCapHours = 4;
        public double OfflineVideoMultiplier = 3;
        public double BoostVideoMultiplier = 2;
        public double BoostVideoHours = 4;
        public int FreePacksPerDay = 3;
        public int VideoWattsPerDay = 20;
        public double RaceRewardSeconds = 60;     // la prime = revenu/s x ceci x facteur de place
        public double[] PlaceRewardFactor;        // index 0 = 1re place
        public int[] LeaguePointsByPlace;
        public double[] CardChanceByPlace;
        public int RiderLevelCap = 10;
        public double LevelStatBonusPct = 3;      // +% de stats par niveau
    }

    public sealed class RarityConfig
    {
        public int Order;                         // 0 Amateur .. 3 Légende
        public int DuplicateFragments;
        public int[] FragmentsToLevel;            // coût en fragments pour passer au niveau suivant
    }

    public sealed class PackConfig
    {
        public string Name;
        public int WattsCost;
        public int MultiPullCount = 10;
        public int MultiPullCost;
        public Dictionary<string, double> Odds = new Dictionary<string, double>();   // en %
        public string GuaranteeRarity;
        public int GuaranteeWithin;
    }

    public sealed class LeagueTuning
    {
        public string[] Tiers;
        public int TeamsPerLeague = 8;
        public int Promoted = 3;
        public int Relegated = 2;
        public int SeasonDays = 7;
        public double GhostPowerSpread = 0.25;
    }

    public sealed class AdTuning
    {
        public int NoAdsBeforeDay = 3;
        public double InterstitialMinGapMinutes = 6;
        public bool NeverDuringRace = true;
    }

    public sealed class OfferTuning
    {
        public int StarterAfterSession = 2;
        public double StarterDurationHours = 48;
        public int SeasonPassFromDay = 3;
        public int NoAdsAfterRacesWithoutPurchase = 3;
        public int LimitedOfferMinDays = 7;
        public int StudioAfterFirstStageWins = 1;
    }

    public sealed class RetentionGates
    {
        public double SoftLaunchD1 = 0.35, SoftLaunchD7 = 0.15;
        public double TargetD1 = 0.38, TargetD7 = 0.18, TargetD30 = 0.07;
        public double StopD7 = 0.12;
    }
}
