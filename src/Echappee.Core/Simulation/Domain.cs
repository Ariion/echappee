using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Echappee.Simulation
{
    public enum StatKind { Sprint, Climb, Rouleur, Technique }

    public enum Rarity { Amateur = 0, Pro = 1, Elite = 2, Legende = 3 }

    public enum SegmentType { Flat, Climb, Descent, Technical, Sprint }

    public struct RiderStats
    {
        public double Sprint, Climb, Rouleur, Technique;

        public RiderStats(double sprint, double climb, double rouleur, double technique)
        { Sprint = sprint; Climb = climb; Rouleur = rouleur; Technique = technique; }

        public double Get(StatKind k)
        {
            switch (k)
            {
                case StatKind.Sprint: return Sprint;
                case StatKind.Climb: return Climb;
                case StatKind.Rouleur: return Rouleur;
                default: return Technique;
            }
        }

        public RiderStats Scaled(double factor) =>
            new RiderStats(Sprint * factor, Climb * factor, Rouleur * factor, Technique * factor);
    }

    /// <summary>Un coureur aligné au départ (carte déjà convertie en stats effectives).</summary>
    public sealed class Rider
    {
        public string Id;
        public string Name;
        public RiderStats Stats;
        public Rarity Rarity;

        public Rider() { }
        public Rider(string id, string name, RiderStats stats, Rarity rarity = Rarity.Amateur)
        { Id = id; Name = name; Stats = stats; Rarity = rarity; }
    }

    public sealed class Team
    {
        public string Id;
        public string Name;
        public bool IsPlayer;
        public List<Rider> Starters = new List<Rider>();
        public PlanDeCourse Plan;                 // seulement pour l'équipe du joueur

        /// <summary>Puissance affichée (somme des stats des titulaires, comme l'écran Équipe).</summary>
        public int Power()
        {
            double s = 0;
            foreach (var r in Starters) s += r.Stats.Sprint + r.Stats.Climb + r.Stats.Rouleur;
            return (int)Math.Round(s / 3.0);
        }
    }

    public sealed class Segment
    {
        public SegmentType Type;
        [JsonProperty("lengthPct")] public double LengthPct;   // part de la course
        public double Gradient;                                 // pente en %
    }

    public sealed class Course
    {
        public string Id;
        public double Meters;
        public List<Segment> Segments = new List<Segment>();

        public double[] SegmentStarts()
        {
            var a = new double[Segments.Count + 1];
            double acc = 0;
            for (int i = 0; i < Segments.Count; i++) { a[i] = acc; acc += Segments[i].LengthPct / 100.0 * Meters; }
            a[Segments.Count] = acc;
            return a;
        }

        public int SegmentIndexAt(double[] starts, double pos)
        {
            for (int i = Segments.Count - 1; i >= 0; i--) if (pos >= starts[i]) return i;
            return 0;
        }

        public void Validate()
        {
            double sum = 0;
            foreach (var s in Segments) sum += s.LengthPct;
            if (Math.Abs(sum - 100) > 0.01) throw new InvalidOperationException("Circuit " + Id + " : les segments font " + sum + " % au lieu de 100 %");
        }
    }

    public enum EventKind
    {
        Start, Attack, PlanRule, Split, Breakaway, BreakawayCaught, Exhausted, SprintFinish, Finish, LeadChange
    }

    public sealed class RaceEvent
    {
        public double Time;
        public EventKind Kind;
        public int Team = -1;
        public string Rider;
        public SegmentType Segment;
        public int Rule = -1;
        public int Value;
        public ActionKind Action;     // pour PlanRule

        public override string ToString() => Time.ToString("0.0") + "s " + EventText.Fr(this);
    }

    public sealed class Standing
    {
        public int TeamIndex;
        public string TeamId;
        public int Rank;
        public double FinishTime;     // double.PositiveInfinity si pas arrivé
        public double Distance;
        public double Fatigue;        // 0..1 à l'arrivée
    }

    public sealed class Snapshot
    {
        public double Time;
        public float[] Positions;
    }

    public sealed class RaceResult
    {
        public ulong Seed;
        public double Duration;
        public List<Standing> Standings = new List<Standing>();   // trié par rang
        public List<RaceEvent> Events = new List<RaceEvent>();
        public List<Snapshot> Frames = new List<Snapshot>();
        public int PlayerRank = -1;

        public Standing ByTeam(int index)
        {
            foreach (var s in Standings) if (s.TeamIndex == index) return s;
            return null;
        }

        /// <summary>Empreinte stable pour les tests de déterminisme.</summary>
        public string Fingerprint()
        {
            unchecked
            {
                long h = 1469598103934665603L;
                foreach (var s in Standings)
                {
                    h = (h ^ s.TeamIndex) * 1099511628211L;
                    h = (h ^ (long)Math.Round(s.FinishTime * 1000)) * 1099511628211L;
                }
                foreach (var e in Events)
                {
                    h = (h ^ (int)e.Kind) * 1099511628211L;
                    h = (h ^ (long)Math.Round(e.Time * 1000)) * 1099511628211L;
                    h = (h ^ e.Team) * 1099511628211L;
                }
                return h.ToString("x");
            }
        }
    }

    /// <summary>Textes du fil d'événements. Le français est la langue de base ; les autres langues viendront d'un fichier de traduction.</summary>
    public static class EventText
    {
        public static string Fr(RaceEvent e)
        {
            string who = e.Rider ?? "?";
            switch (e.Kind)
            {
                case EventKind.Start: return "Départ ! Le peloton s'élance.";
                case EventKind.Attack: return "Attaque de " + who + " " + Where(e.Segment);
                case EventKind.PlanRule: return "Règle " + (e.Rule + 1) + " déclenchée : " + who;
                case EventKind.Split: return "Le peloton se scinde en deux groupes";
                case EventKind.Breakaway: return "Échappée de " + who + " : " + e.Value + " m d'avance";
                case EventKind.BreakawayCaught: return "Échappée reprise";
                case EventKind.Exhausted: return who + " est à bout de forces";
                case EventKind.LeadChange: return who + " passe en tête";
                case EventKind.SprintFinish: return "Sprint final lancé !";
                case EventKind.Finish: return "Victoire de l'équipe " + who + " !";
                default: return string.Empty;
            }
        }

        /// <summary>Même fil d'événements, dans la langue du Localizer. Les clés viennent de data/strings/*.json.</summary>
        public static string Format(RaceEvent e, Echappee.Config.Localizer loc)
        {
            string who = e.Rider ?? "?";
            switch (e.Kind)
            {
                case EventKind.Start: return loc.Get("ev.start");
                case EventKind.Attack: return loc.Get("ev.attack", who, loc.Get(WhereKey(e.Segment)));
                case EventKind.PlanRule: return loc.Get("ev.planRule", e.Rule + 1, loc.Get(ActionKey(e.Action)));
                case EventKind.Split: return loc.Get("ev.split");
                case EventKind.Breakaway: return loc.Get("ev.breakaway", who, e.Value);
                case EventKind.BreakawayCaught: return loc.Get("ev.caught");
                case EventKind.Exhausted: return loc.Get("ev.exhausted", who);
                case EventKind.LeadChange: return loc.Get("ev.leadChange", who);
                case EventKind.SprintFinish: return loc.Get("ev.sprint");
                case EventKind.Finish: return loc.Get("ev.finish", who);
                default: return string.Empty;
            }
        }

        public static string WhereKey(SegmentType t)
        {
            switch (t)
            {
                case SegmentType.Climb: return "where.climb";
                case SegmentType.Descent: return "where.descent";
                case SegmentType.Technical: return "where.technical";
                case SegmentType.Sprint: return "where.sprint";
                default: return "where.flat";
            }
        }

        public static string ActionKey(ActionKind a)
        {
            switch (a)
            {
                case ActionKind.ClimberAttacks: return "act.climber";
                case ActionKind.StayInWheel: return "act.wheel";
                case ActionKind.SprinterLaunch: return "act.sprinter";
                default: return "act.chase";
            }
        }

        static string Where(SegmentType t)
        {
            switch (t)
            {
                case SegmentType.Climb: return "dans la côte";
                case SegmentType.Descent: return "dans la descente";
                case SegmentType.Technical: return "dans le passage technique";
                case SegmentType.Sprint: return "dans la ligne droite";
                default: return "sur le plat";
            }
        }
    }
}
