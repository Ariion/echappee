using System.Collections.Generic;

namespace Echappee.Simulation
{
    public enum ConditionKind { Gradient, Fatigue, KmRemaining, GapToBreakaway }
    public enum Comparator { Greater, Less }
    public enum ActionKind { ClimberAttacks, StayInWheel, SprinterLaunch, GroupChase }

    /// <summary>Une règle SI / ALORS du Plan de course.</summary>
    public sealed class PlanRule
    {
        public ConditionKind Condition;
        public Comparator Compare;
        public double Threshold;           // % pour pente et fatigue, km, secondes
        public ActionKind Action;
        public bool Active = true;

        public PlanRule() { }
        public PlanRule(ConditionKind c, Comparator cmp, double threshold, ActionKind a, bool active = true)
        { Condition = c; Compare = cmp; Threshold = threshold; Action = a; Active = active; }

        public bool Matches(PlanContext ctx)
        {
            double v;
            switch (Condition)
            {
                case ConditionKind.Gradient: v = ctx.GradientPct; break;
                case ConditionKind.Fatigue: v = ctx.FatiguePct; break;
                case ConditionKind.KmRemaining: v = ctx.KmRemaining; break;
                default: v = ctx.GapToBreakawayS; break;
            }
            return Compare == Comparator.Greater ? v > Threshold : v < Threshold;
        }
    }

    public struct PlanContext
    {
        public double GradientPct;
        public double FatiguePct;
        public double KmRemaining;
        public double GapToBreakawayS;   // 0 s'il n'y a pas d'échappée devant
    }

    /// <summary>Règles lues de haut en bas : la première règle active qui correspond s'applique.</summary>
    public sealed class PlanDeCourse
    {
        public int Slots = 3;
        public List<PlanRule> Rules = new List<PlanRule>();

        public bool Add(PlanRule r)
        {
            if (Rules.Count >= Slots) return false;
            Rules.Add(r);
            return true;
        }

        /// <summary>Renvoie l'index de la règle appliquée, ou -1.</summary>
        public int Evaluate(PlanContext ctx)
        {
            for (int i = 0; i < Rules.Count; i++)
                if (Rules[i].Active && Rules[i].Matches(ctx)) return i;
            return -1;
        }

        public PlanDeCourse Clone()
        {
            var p = new PlanDeCourse { Slots = Slots };
            foreach (var r in Rules) p.Rules.Add(new PlanRule(r.Condition, r.Compare, r.Threshold, r.Action, r.Active));
            return p;
        }
    }
}
