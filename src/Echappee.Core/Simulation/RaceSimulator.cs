using System;
using System.Collections.Generic;
using Echappee.Config;

namespace Echappee.Simulation
{
    /// <summary>
    /// Simulation de course déterministe, sans dépendance Unity : une graine donne toujours la même course.
    /// Une "équipe" est une unité du peloton ; chaque segment désigne son meilleur coureur pour nommer les événements.
    /// </summary>
    public sealed class RaceSimulator
    {
        readonly BalanceConfig _cfg;
        readonly RaceTuning _t;

        public RaceSimulator(BalanceConfig cfg) { _cfg = cfg; _t = cfg.Race; }

        static readonly SegmentType[] SegTypes = (SegmentType[])Enum.GetValues(typeof(SegmentType));

        static double TypeSpeedFactor(SegmentType s)
        {
            switch (s)
            {
                case SegmentType.Climb: return 0.80;
                case SegmentType.Descent: return 1.25;
                case SegmentType.Technical: return 0.90;
                case SegmentType.Sprint: return 1.10;
                default: return 1.0;
            }
        }

        static double AttackAppeal(SegmentType s)
        {
            switch (s)
            {
                case SegmentType.Climb: return 2.0;
                case SegmentType.Sprint: return 3.0;
                case SegmentType.Technical: return 1.0;
                case SegmentType.Descent: return 0.7;
                default: return 0.5;
            }
        }

        // Combinaison de stats utilisée par chaque type de segment.
        static void Profile(SegmentType s, out StatKind a, out double wa, out StatKind b, out double wb)
        {
            b = StatKind.Rouleur; wb = 0;
            switch (s)
            {
                case SegmentType.Climb: a = StatKind.Climb; wa = 1; break;
                case SegmentType.Descent: a = StatKind.Technique; wa = 0.7; b = StatKind.Rouleur; wb = 0.3; break;
                case SegmentType.Technical: a = StatKind.Technique; wa = 1; break;
                case SegmentType.Sprint: a = StatKind.Sprint; wa = 1; break;
                default: a = StatKind.Rouleur; wa = 1; break;
            }
        }

        double Factor(DisciplineConfig d, StatKind k)
        {
            int w;
            switch (k)
            {
                case StatKind.Sprint: w = d.Sprint; break;
                case StatKind.Climb: w = d.Climb; break;
                case StatKind.Rouleur: w = d.Rouleur; break;
                default: w = d.Technique; break;
            }
            return _t.MinWeightFactor + (1 - _t.MinWeightFactor) * w / 3.0;
        }

        double RiderScore(Rider r, SegmentType s, DisciplineConfig d)
        {
            Profile(s, out var a, out var wa, out var b, out var wb);
            return wa * Factor(d, a) * r.Stats.Get(a) + wb * Factor(d, b) * r.Stats.Get(b);
        }

        sealed class TeamState
        {
            public Team Team;
            public double Pos, Fatigue, AttackUntil = -1, Noise, Speed, FinishTime = double.PositiveInfinity;
            public bool Finished, ExhaustedFlagged;
            public double[] Strength = new double[SegTypes.Length];
            public string[] BestName = new string[SegTypes.Length];
            public double BestClimb, BestSprint;
            public int LastRule = -1;
            public double Form;
        }

        TeamState Prepare(Team team, DisciplineConfig d)
        {
            var st = new TeamState { Team = team };
            for (int si = 0; si < SegTypes.Length; si++)
            {
                double best = 0, sum = 0; string bestName = team.Name;
                foreach (var r in team.Starters)
                {
                    double sc = RiderScore(r, SegTypes[si], d);
                    sum += sc;
                    if (sc > best) { best = sc; bestName = r.Name; }
                }
                double mean = sum / Math.Max(1, _t.StartersPerTeam);
                st.Strength[si] = 0.6 * best + 0.4 * mean;
                st.BestName[si] = bestName;
            }
            foreach (var r in team.Starters)
            {
                st.BestClimb = Math.Max(st.BestClimb, r.Stats.Climb);
                st.BestSprint = Math.Max(st.BestSprint, r.Stats.Sprint);
            }
            return st;
        }

        /// <summary>Longueur du circuit ajustée pour qu'une équipe moyenne boucle la course en DurationSeconds.</summary>
        public double FitCourseMeters(Course c, DisciplineConfig d)
        {
            double inv = 0;
            foreach (var s in c.Segments) inv += s.LengthPct / 100.0 / TypeSpeedFactor(s.Type);
            return d.DurationSeconds * _t.BaseSpeedMps / inv;
        }

        public RaceResult Run(IList<Team> teams, Course course, DisciplineConfig discipline, ulong seed, bool recordFrames = false)
        {
            course.Validate();
            var rng = new Rng(seed);
            double meters = FitCourseMeters(course, discipline);
            double[] starts = ScaledStarts(course, meters);
            int n = teams.Count;
            var ts = new TeamState[n];
            for (int i = 0; i < n; i++) { ts[i] = Prepare(teams[i], discipline); ts[i].Form = _t.FormSpread * rng.Gauss(); }

            var res = new RaceResult { Seed = seed };
            double dt = _t.TickSeconds, t = 0, maxT = discipline.DurationSeconds * _t.MaxTimeFactor;
            double nextSnap = 0, nextNoise = 0;
            int leader = -1, groups = 1, finishedCount = 0;
            double lastSplitT = -99, lastLeadT = -99;
            bool inBreak = false, sprintAnnounced = false, firstFinish = false;
            var order = new int[n];
            var rank = new int[n];
            res.Events.Add(new RaceEvent { Time = 0, Kind = EventKind.Start });

            while (finishedCount < n && t < maxT)
            {
                // ordre actuel (tête -> queue)
                for (int i = 0; i < n; i++) order[i] = i;
                Array.Sort(order, (a, b) => ts[b].Pos.CompareTo(ts[a].Pos));
                for (int k = 0; k < n; k++) rank[order[k]] = k;

                if (t >= nextNoise)
                {
                    for (int i = 0; i < n; i++) ts[i].Noise = _t.NoiseSpeed * rng.Gauss();
                    nextNoise = t + 2;
                }

                // groupes, échappée
                int g = 1, groupAtBreak = 1;
                for (int k = 1; k < n; k++)
                {
                    if (ts[order[k - 1]].Pos - ts[order[k]].Pos > _t.SplitGapM) g++;
                    if (g == 1) groupAtBreak = k + 1;
                }
                bool breakNow = n > 3 && g > 1 && groupAtBreak <= 3 &&
                    ts[order[groupAtBreak - 1]].Pos - ts[order[groupAtBreak]].Pos >= _t.BreakawayGapM;
                double leaderPos = ts[order[0]].Pos;

                if (g > 1 && groups == 1 && t - lastSplitT > 8)
                {
                    res.Events.Add(new RaceEvent { Time = t, Kind = EventKind.Split });
                    lastSplitT = t;
                }
                groups = g;
                if (breakNow && !inBreak)
                {
                    int segI = course.SegmentIndexAt(starts, leaderPos);
                    res.Events.Add(new RaceEvent
                    {
                        Time = t, Kind = EventKind.Breakaway, Team = order[0],
                        Rider = ts[order[0]].BestName[(int)course.Segments[segI].Type],
                        Value = (int)Math.Round(leaderPos - ts[order[groupAtBreak]].Pos)
                    });
                }
                else if (!breakNow && inBreak)
                    res.Events.Add(new RaceEvent { Time = t, Kind = EventKind.BreakawayCaught });
                inBreak = breakNow;

                if (order[0] != leader && t - lastLeadT > 3 && ts[order[0]].Pos > 0)
                {
                    if (leader != -1)
                        res.Events.Add(new RaceEvent { Time = t, Kind = EventKind.LeadChange, Team = order[0], Rider = ts[order[0]].Team.Name });
                    leader = order[0]; lastLeadT = t;
                }

                for (int i = 0; i < n; i++)
                {
                    var s = ts[i];
                    if (s.Finished) continue;
                    int segIdx = course.SegmentIndexAt(starts, s.Pos);
                    var seg = course.Segments[segIdx];
                    int si = (int)seg.Type;

                    if (seg.Type == SegmentType.Sprint && !sprintAnnounced && s.Pos >= starts[segIdx]) { sprintAnnounced = true; res.Events.Add(new RaceEvent { Time = t, Kind = EventKind.SprintFinish }); }

                    // base
                    double speed = _t.BaseSpeedMps * TypeSpeedFactor(seg.Type) *
                        (1 + _t.StatSpeedSpan * (s.Strength[si] - 50) / 100.0 + s.Noise + s.Form);
                    double fat = _t.FatigueBase * (seg.Type == SegmentType.Climb ? 1.3 : 1.0);
                    if (seg.Type == SegmentType.Descent) fat = -_t.FatigueRecoveryDescent;
                    double speedBonus = 0, fatMul = 1;

                    // aspiration
                    bool drafted = false;
                    int r = rank[i];
                    if (r > 0 && ts[order[r - 1]].Pos - s.Pos <= _t.DraftRangeM) drafted = true;
                    if (drafted) { speedBonus += _t.DraftSpeedBonus; fatMul *= _t.DraftFatigueFactor; }

                    // plan de course du joueur
                    int ruleApplied = -1;
                    var plan = s.Team.Plan;
                    if (s.Team.IsPlayer && plan != null)
                    {
                        double remainFrac = Math.Max(0, 1 - s.Pos / meters);
                        double gapS = 0;
                        if (breakNow && r >= groupAtBreak) gapS = (leaderPos - s.Pos) / Math.Max(1, s.Speed > 0 ? s.Speed : _t.BaseSpeedMps);
                        ruleApplied = plan.Evaluate(new PlanContext
                        {
                            GradientPct = seg.Gradient,
                            FatiguePct = s.Fatigue * 100,
                            KmRemaining = remainFrac * _t.Plan.TotalKmDisplay,
                            GapToBreakawayS = gapS
                        });
                        if (ruleApplied >= 0)
                        {
                            ApplyAction(plan.Rules[ruleApplied].Action, s, breakNow && r >= groupAtBreak, ref speedBonus, ref fatMul);
                            if (ruleApplied != s.LastRule)
                                res.Events.Add(new RaceEvent { Time = t, Kind = EventKind.PlanRule, Team = i, Rule = ruleApplied, Action = plan.Rules[ruleApplied].Action, Rider = ActionText(plan.Rules[ruleApplied].Action), Segment = seg.Type });
                        }
                        s.LastRule = ruleApplied;
                    }

                    // attaques (IA, et joueur quand aucune règle ne s'applique)
                    if (ruleApplied < 0 && (!s.Team.IsPlayer || _t.PlayerAutoAttack) && t >= s.AttackUntil && s.Fatigue < 0.5 && s.Pos > 0.03 * meters)
                    {
                        double p = _t.AttackChancePerSecond * dt * AttackAppeal(seg.Type) * (s.Strength[si] / 50.0);
                        if (rng.Chance(p))
                        {
                            s.AttackUntil = t + _t.AttackDurationS;
                            res.Events.Add(new RaceEvent { Time = t, Kind = EventKind.Attack, Team = i, Rider = s.BestName[si], Segment = seg.Type });
                        }
                    }
                    if (t < s.AttackUntil && ruleApplied < 0) { speedBonus += _t.AttackSpeedBonus; fatMul *= _t.AttackFatigueFactor; }

                    // fatigue
                    double rouleurRel = 1.2 - 0.4 * s.Strength[(int)SegmentType.Flat] / 100.0;
                    double rate = fat >= 0 ? fat * fatMul * rouleurRel : fat;
                    s.Fatigue = Math.Max(0, Math.Min(1, s.Fatigue + rate * dt));
                    if (s.Fatigue > 0.85 && !s.ExhaustedFlagged)
                    {
                        s.ExhaustedFlagged = true;
                        res.Events.Add(new RaceEvent { Time = t, Kind = EventKind.Exhausted, Team = i, Rider = s.BestName[si] });
                    }
                    double tired = s.Fatigue > _t.ExhaustionStart ? (s.Fatigue - _t.ExhaustionStart) / (1 - _t.ExhaustionStart) : 0;

                    speed *= (1 + speedBonus) * (1 - _t.ExhaustionSlowdown * tired);
                    s.Speed = speed;
                    s.Pos += speed * dt;

                    if (s.Pos >= meters)
                    {
                        s.Finished = true;
                        s.FinishTime = t + dt - (s.Pos - meters) / speed;
                        finishedCount++;
                        if (!firstFinish)
                        {
                            firstFinish = true;
                            res.Events.Add(new RaceEvent { Time = s.FinishTime, Kind = EventKind.Finish, Team = i, Rider = s.Team.Name });
                        }
                    }
                }

                if (recordFrames && t >= nextSnap)
                {
                    var f = new float[n];
                    for (int i = 0; i < n; i++) f[i] = (float)(Math.Min(ts[i].Pos, meters) / meters);
                    res.Frames.Add(new Snapshot { Time = t, Positions = f });
                    nextSnap = t + _t.SnapshotEverySeconds;
                }
                t += dt;
            }

            res.Duration = t;
            var idx = new List<int>();
            for (int i = 0; i < n; i++) idx.Add(i);
            idx.Sort((a, b) =>
            {
                bool fa = ts[a].Finished, fb = ts[b].Finished;
                if (fa && fb) return ts[a].FinishTime.CompareTo(ts[b].FinishTime);
                if (fa) return -1;
                if (fb) return 1;
                return ts[b].Pos.CompareTo(ts[a].Pos);
            });
            for (int k = 0; k < idx.Count; k++)
            {
                int i = idx[k];
                res.Standings.Add(new Standing { TeamIndex = i, TeamId = ts[i].Team.Id, Rank = k + 1, FinishTime = ts[i].FinishTime, Distance = ts[i].Pos, Fatigue = ts[i].Fatigue });
                if (ts[i].Team.IsPlayer) res.PlayerRank = k + 1;
            }
            return res;
        }

        void ApplyAction(ActionKind a, TeamState s, bool inChaseSituation, ref double speedBonus, ref double fatMul)
        {
            var p = _t.Plan;
            switch (a)
            {
                case ActionKind.ClimberAttacks:
                    speedBonus += p.ClimberAttackSpeed * (0.5 + s.BestClimb / 100.0); fatMul *= p.ClimberAttackFatigue; break;
                case ActionKind.SprinterLaunch:
                    speedBonus += p.SprintLaunchSpeed * (0.5 + s.BestSprint / 100.0); fatMul *= p.SprintLaunchFatigue; break;
                case ActionKind.StayInWheel:
                    speedBonus += p.StayInWheelSpeed; fatMul *= p.StayInWheelFatigue; break;
                case ActionKind.GroupChase:
                    if (inChaseSituation) { speedBonus += p.GroupChaseSpeed; fatMul *= p.GroupChaseFatigue; }
                    break;
            }
        }

        public static string ActionText(ActionKind a)
        {
            switch (a)
            {
                case ActionKind.ClimberAttacks: return "ton grimpeur attaque";
                case ActionKind.StayInWheel: return "ton équipe reste dans la roue";
                case ActionKind.SprinterLaunch: return "sprinteur lancé";
                default: return "chasse groupée";
            }
        }

        double[] ScaledStarts(Course c, double meters)
        {
            var a = new double[c.Segments.Count + 1];
            double acc = 0;
            for (int i = 0; i < c.Segments.Count; i++) { a[i] = acc; acc += c.Segments[i].LengthPct / 100.0 * meters; }
            a[c.Segments.Count] = acc;
            return a;
        }
    }
}
