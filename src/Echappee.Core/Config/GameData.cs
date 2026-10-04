using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Echappee.Simulation;

namespace Echappee.Config
{
    public sealed class RiderCardDef
    {
        public string Id;
        public string Name;
        public Rarity Rarity;
        public double Sprint, Climb, Rouleur, Technique;

        public Rider ToRider(int level, double levelBonusPct)
        {
            double k = 1 + (level - 1) * levelBonusPct / 100.0;
            return new Rider(Id, Name, new RiderStats(Sprint, Climb, Rouleur, Technique).Scaled(k), Rarity);
        }
    }

    /// <summary>Tout le contenu chargé depuis les JSON (disque en test/outil, Remote Config ou Resources dans Unity).</summary>
    public sealed class GameData
    {
        public BalanceConfig Balance;
        public Dictionary<string, Course> Courses;
        public List<RiderCardDef> Riders;

        public static GameData FromJson(string balanceJson, string coursesJson, string ridersJson)
        {
            var d = new GameData
            {
                Balance = BalanceConfig.FromJson(balanceJson),
                Courses = JsonConvert.DeserializeObject<Dictionary<string, Course>>(coursesJson),
                Riders = JsonConvert.DeserializeObject<List<RiderCardDef>>(ridersJson)
            };
            foreach (var c in d.Courses.Values) c.Validate();
            d.Validate();
            return d;
        }

        public void Validate()
        {
            foreach (var kv in Balance.Packs)
            {
                double sum = 0;
                foreach (var o in kv.Value.Odds) sum += o.Value;
                if (Math.Abs(sum - 100) > 0.001) throw new InvalidOperationException("Pack " + kv.Key + " : taux = " + sum + " % au lieu de 100 %");
            }
            foreach (var kv in Balance.Disciplines)
                if (!Courses.ContainsKey(kv.Value.Circuit)) throw new InvalidOperationException("Circuit manquant : " + kv.Value.Circuit);
            foreach (var r in Balance.Rarities.Values)
                if (r.FragmentsToLevel.Length < Balance.Economy.RiderLevelCap - 1) throw new InvalidOperationException("FragmentsToLevel trop court");
        }
    }
}
