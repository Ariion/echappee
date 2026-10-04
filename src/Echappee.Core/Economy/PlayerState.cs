using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Echappee.Numbers;
using Echappee.Simulation;

namespace Echappee.Economy
{
    public sealed class OwnedCard
    {
        public string Id;
        public int Level = 1;
        public long Fragments;      // fragments déjà investis ou en réserve pour ce coureur
    }

    /// <summary>Tout ce qui est sauvegardé pour un joueur. Sérialisable en JSON (Newtonsoft).</summary>
    public sealed class PlayerState
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;

        [JsonConverter(typeof(BigAmountJsonConverter))]
        public BigAmount Primes = BigAmount.Zero;
        public long Watts;
        public long Fragments;                                    // réserve commune, dépensée sur n'importe quel coureur

        public Dictionary<string, int> InfraLevels = new Dictionary<string, int>();
        public Dictionary<string, OwnedCard> Cards = new Dictionary<string, OwnedCard>();
        public List<string> Starters = new List<string>();        // ids de cartes, dans l'ordre
        public Dictionary<string, int> PityCounters = new Dictionary<string, int>();   // tirages depuis le dernier rare garanti, par pack

        // temps (secondes Unix UTC)
        public long LastSeenUnix;
        public long BoostUntilUnix;
        public long FirstPlayUnix;
        public int SessionCount;
        public int DayIndexOfFreePacks = -1;
        public int FreePacksUsedToday;
        public int DayIndexOfVideoWatts = -1;
        public int VideoWattsToday;

        public int RacesPlayed;
        public int StageWins;
        public bool NoAds;
        public bool EverPurchased;

        // offres
        public long StarterOfferStartUnix;      // 0 = jamais montrée
        public bool StarterOfferBought;
        public long LastLimitedOfferUnix;
        public long LastInterstitialUnix;

        // ligue
        public int LeagueTier;

        public int InfraLevel(string id) => InfraLevels.TryGetValue(id, out var l) ? l : 0;

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.None);

        public static PlayerState FromJson(string json) =>
            string.IsNullOrEmpty(json) ? new PlayerState() : JsonConvert.DeserializeObject<PlayerState>(json);
    }
}
