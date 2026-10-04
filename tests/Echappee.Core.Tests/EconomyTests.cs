using System.Collections.Generic;
using System.Linq;
using Echappee.Analytics;
using Echappee.Config;
using Echappee.Economy;
using Echappee.Leagues;
using Echappee.Monetization;
using Echappee.Numbers;
using Echappee.Simulation;
using Xunit;

public class EconomyTests
{
    static readonly GameData D = TestData.Load();
    static readonly EconomyService Eco = new EconomyService(D.Balance);
    const long Now = 1_800_000_000;

    [Fact]
    public void First_upgrade_costs_base_and_income_grows()
    {
        var s = new PlayerState { Primes = new BigAmount(15) };
        Assert.True(Eco.TryUpgrade(s, "sponsors"));
        Assert.Equal(1, s.InfraLevel("sponsors"));
        Assert.True(s.Primes.IsZero);
        Assert.Equal(1.0, Eco.IncomePerSecond(s, Now).ToDouble(), 6);
        Assert.False(Eco.TryUpgrade(s, "sponsors"));   // plus assez
    }

    [Fact]
    public void Milestone_levels_double_income()
    {
        var inf = Eco.Infra("sponsors");
        Assert.Equal(9 * inf.BaseIncome, Eco.InfraIncome(inf, 9).ToDouble(), 6);
        Assert.Equal(10 * inf.BaseIncome * 2, Eco.InfraIncome(inf, 10).ToDouble(), 6);
        Assert.Equal(25 * inf.BaseIncome * 4, Eco.InfraIncome(inf, 25).ToDouble(), 6);
    }

    [Fact]
    public void Costs_grow_geometrically_and_gain_is_positive()
    {
        var inf = Eco.Infra("bus");
        Assert.True(Eco.UpgradeCost(inf, 10) > Eco.UpgradeCost(inf, 9));
        Assert.True(Eco.UpgradeGain(inf, 9) > BigAmount.Zero);
    }

    [Fact]
    public void Max_affordable_matches_repeated_purchases()
    {
        var s = new PlayerState { Primes = new BigAmount(5000) };
        int n = Eco.MaxAffordable(s, Eco.Infra("sponsors"));
        int bought = 0;
        while (Eco.TryUpgrade(s, "sponsors")) bought++;
        Assert.Equal(n, bought);
        Assert.True(bought > 10);
    }

    [Fact]
    public void Offline_income_is_capped_at_four_hours()
    {
        var s = new PlayerState { LastSeenUnix = Now - 10 * 3600 };
        s.InfraLevels["sponsors"] = 3;                                   // 3 Primes/s
        var r = Eco.OfflineEarnings(s, Now, false);
        Assert.True(r.Capped);
        Assert.Equal(4 * 3600, r.SecondsCredited, 3);
        Assert.Equal(3.0 * 4 * 3600, r.Earned.ToDouble(), 3);
        var vid = Eco.OfflineEarnings(s, Now, true);
        Assert.Equal(r.Earned.ToDouble() * 3, vid.Earned.ToDouble(), 3);  // la vidéo multiplie, ne lève pas le plafond
    }

    [Fact]
    public void Short_absence_is_not_capped_and_claim_updates_state()
    {
        var s = new PlayerState { LastSeenUnix = Now - 600 };
        s.InfraLevels["sponsors"] = 2;
        var r = Eco.ClaimOffline(s, Now, false);
        Assert.False(r.Capped);
        Assert.Equal(1200, s.Primes.ToDouble(), 3);
        Assert.Equal(Now, s.LastSeenUnix);
    }

    [Fact]
    public void Boost_video_doubles_income_for_its_duration()
    {
        var s = new PlayerState(); s.InfraLevels["sponsors"] = 1;
        Eco.StartVideoBoost(s, Now);
        Assert.Equal(2.0, Eco.IncomePerSecond(s, Now + 100).ToDouble(), 6);
        Assert.Equal(1.0, Eco.IncomePerSecond(s, Now + 4 * 3600 + 1).ToDouble(), 6);
    }

    [Fact]
    public void Race_rewards_decrease_with_place_and_never_pay_zero()
    {
        var s = new PlayerState();
        Assert.False(Eco.RewardForPlace(s, 1, Now).Primes.IsZero);   // tout début : revenu 0
        s.InfraLevels["bus"] = 5;
        var first = Eco.RewardForPlace(s, 1, Now); var last = Eco.RewardForPlace(s, 12, Now);
        Assert.True(first.Primes > last.Primes);
        Assert.True(first.LeaguePoints > last.LeaguePoints);
    }

    [Fact]
    public void Video_watts_are_limited_per_day()
    {
        var s = new PlayerState();
        Assert.True(Eco.TryClaimVideoWatts(s, Now, 10));
        Assert.True(Eco.TryClaimVideoWatts(s, Now, 10));
        Assert.False(Eco.TryClaimVideoWatts(s, Now, 10));
        Assert.True(Eco.TryClaimVideoWatts(s, Now + 86400, 10));    // nouveau jour
        Assert.Equal(30, s.Watts);
    }

    [Fact]
    public void Player_state_roundtrips_through_json_including_big_numbers()
    {
        var s = new PlayerState { Primes = BigAmount.FromParts(4.2, 90), Watts = 77 };
        s.InfraLevels["bus"] = 12; s.Cards["r001"] = new OwnedCard { Id = "r001", Level = 3, Fragments = 40 };
        var back = PlayerState.FromJson(s.ToJson());
        Assert.Equal(s.Primes, back.Primes);
        Assert.Equal(12, back.InfraLevel("bus"));
        Assert.Equal(3, back.Cards["r001"].Level);
        Assert.Equal(1, back.Version);
    }
}

public class PackTests
{
    static readonly GameData D = TestData.Load();
    static readonly PackService Packs = new PackService(D);

    [Fact]
    public void Config_odds_sum_to_100_and_match_the_documented_values()
    {
        foreach (var k in D.Balance.Packs.Keys) Assert.Equal(100, D.Balance.Packs[k].Odds.Values.Sum(), 6);
        var pro = Packs.DisplayedOdds("pro");
        Assert.Equal(58, pro[Rarity.Amateur]); Assert.Equal(0.5, pro[Rarity.Legende]);
        Assert.Equal(135, Packs.Cost("pro", 10)); Assert.Equal(405, Packs.Cost("elite", 10));
    }

    [Fact]
    public void Not_enough_watts_opens_nothing()
    {
        var s = new PlayerState { Watts = 14 };
        Assert.Null(Packs.Open(s, "pro", 1, new Rng(1)));
        Assert.Equal(14, s.Watts);
    }

    [Fact]
    public void Observed_rates_match_configured_odds()
    {
        var rng = new Rng(2026);
        var counts = new Dictionary<Rarity, int>();
        int n = 40000;
        var s = new PlayerState { Watts = long.MaxValue / 2 };
        for (int i = 0; i < n; i++)
        {
            s.PityCounters.Clear();                     // pas de garantie : on mesure les taux bruts
            var pull = Packs.Open(s, "pro", 1, rng)[0];
            counts[pull.Rarity] = counts.GetValueOrDefault(pull.Rarity) + 1;
        }
        Assert.InRange(counts[Rarity.Amateur] / (double)n, 0.56, 0.60);
        Assert.InRange(counts[Rarity.Pro] / (double)n, 0.31, 0.35);
        Assert.InRange(counts[Rarity.Elite] / (double)n, 0.07, 0.10);
    }

    [Fact]
    public void Pro_pack_guarantees_an_elite_within_15_pulls()
    {
        for (ulong seed = 1; seed <= 300; seed++)
        {
            var rng = new Rng(seed);
            var s = new PlayerState { Watts = 100000 };
            int sinceElite = 0;
            for (int i = 0; i < 60; i++)
            {
                var p = Packs.Open(s, "pro", 1, rng)[0];
                if (p.Rarity >= Rarity.Elite) sinceElite = 0; else sinceElite++;
                Assert.True(sinceElite < 15, $"seed {seed}: {sinceElite} tirages sans Élite");
            }
        }
    }

    [Fact]
    public void Elite_pack_guarantees_a_legend_within_25_pulls_and_never_gives_amateurs()
    {
        for (ulong seed = 1; seed <= 200; seed++)
        {
            var rng = new Rng(seed);
            var s = new PlayerState { Watts = 1000000 };
            int since = 0;
            for (int i = 0; i < 100; i++)
            {
                var p = Packs.Open(s, "elite", 1, rng)[0];
                Assert.NotEqual(Rarity.Amateur, p.Rarity);
                if (p.Rarity == Rarity.Legende) since = 0; else since++;
                Assert.True(since < 25);
            }
        }
    }

    [Fact]
    public void Ten_pull_uses_the_discounted_price_and_duplicates_give_fragments()
    {
        var s = new PlayerState { Watts = 135 };
        var pulls = Packs.Open(s, "pro", 10, new Rng(5));
        Assert.Equal(10, pulls.Count);
        Assert.Equal(0, s.Watts);
        // forcer un doublon
        var again = new PlayerState { Watts = 100000 };
        var r = new Rng(9);
        long frags = 0;
        for (int i = 0; i < 200; i++) Packs.Open(again, "pro", 1, r);
        frags = again.Cards.Values.Sum(c => c.Fragments);
        Assert.True(frags > 0);
    }

    [Fact]
    public void Free_daily_packs_are_limited_to_three()
    {
        var s = new PlayerState(); var rng = new Rng(3);
        for (int i = 0; i < 3; i++) Assert.NotNull(Packs.OpenFreeDaily(s, 1_800_000_000, rng));
        Assert.Null(Packs.OpenFreeDaily(s, 1_800_000_000, rng));
        Assert.NotNull(Packs.OpenFreeDaily(s, 1_800_000_000 + 86400, rng));
        Assert.Equal(0, s.Watts);
    }

    [Fact]
    public void Level_up_spends_fragments_and_boosts_stats()
    {
        var s = new PlayerState();
        var def = D.Riders.First(r => r.Rarity == Rarity.Amateur);
        s.Cards[def.Id] = new OwnedCard { Id = def.Id, Fragments = 10 };
        Assert.True(Packs.TryLevelUp(s, def.Id));
        Assert.Equal(2, s.Cards[def.Id].Level);
        Assert.Equal(0, s.Cards[def.Id].Fragments);
        Assert.False(Packs.TryLevelUp(s, def.Id));
        s.Starters.Add(def.Id);
        var team = Packs.BuildTeam(s, "p", "Moi");
        Assert.True(team.Starters[0].Stats.Sprint > def.Sprint);
    }

    [Fact]
    public void Catalog_has_enough_riders_in_every_rarity()
    {
        foreach (Rarity r in System.Enum.GetValues(typeof(Rarity)))
            Assert.True(D.Riders.Count(x => x.Rarity == r) >= 4);
        Assert.Equal(D.Riders.Count, D.Riders.Select(x => x.Id).Distinct().Count());
    }
}

public class LeagueTests
{
    static readonly BalanceConfig C = TestData.Load().Balance;

    [Fact]
    public void Strong_player_wins_the_league_and_is_promoted()
    {
        var rng = new Rng(1);
        var l = new LeagueSeason(C, 1, 500, 0, 77);
        for (int i = 0; i < 40; i++) l.RecordRace(1, rng);
        Assert.Equal(1, l.PlayerRank());
        Assert.Equal(LeagueOutcome.Promoted, l.Outcome());
        Assert.Equal(2, l.NextTier());
    }

    [Fact]
    public void Weak_player_is_relegated_except_in_the_lowest_league()
    {
        var rng = new Rng(2);
        var l = new LeagueSeason(C, 2, 500, 0, 77);
        for (int i = 0; i < 40; i++) l.RecordRace(12, rng);
        Assert.Equal(LeagueOutcome.Relegated, l.Outcome());
        var low = new LeagueSeason(C, 0, 500, 0, 77);
        for (int i = 0; i < 40; i++) low.RecordRace(12, rng);
        Assert.Equal(LeagueOutcome.Stayed, low.Outcome());     // on ne descend pas sous Amateur
    }

    [Fact]
    public void Top_tier_cannot_be_promoted_and_season_lasts_seven_days()
    {
        var rng = new Rng(3);
        var l = new LeagueSeason(C, C.Leagues.Tiers.Length - 1, 500, 1000, 5);
        for (int i = 0; i < 40; i++) l.RecordRace(1, rng);
        Assert.Equal(LeagueOutcome.Stayed, l.Outcome());
        Assert.Equal(1000 + 7 * 86400, l.EndUnix);
        Assert.False(l.IsOver(1000)); Assert.True(l.IsOver(l.EndUnix));
    }

    [Fact]
    public void Standings_have_eight_teams_with_promotion_and_relegation_zones()
    {
        var l = new LeagueSeason(C, 1, 500, 0, 9);
        Assert.Equal(8, l.Standings().Count);
        Assert.Equal(LeagueOutcome.Promoted, l.ZoneOfRank(3));
        Assert.Equal(LeagueOutcome.Stayed, l.ZoneOfRank(4));
        Assert.Equal(LeagueOutcome.Relegated, l.ZoneOfRank(7));
    }

    [Fact]
    public void Disciplines_unlock_with_league_tier()
    {
        var d = C.Disciplines;
        Assert.True(LeagueSeason.IsDisciplineUnlocked(C, d["route"], 0));
        Assert.False(LeagueSeason.IsDisciplineUnlocked(C, d["bmx"], 0));
        Assert.True(LeagueSeason.IsDisciplineUnlocked(C, d["bmx"], 1));
        Assert.False(LeagueSeason.IsDisciplineUnlocked(C, d["gravel"], 3));
        Assert.True(LeagueSeason.IsDisciplineUnlocked(C, d["gravel"], 4));
    }
}

public class MonetizationTests
{
    static readonly BalanceConfig C = TestData.Load().Balance;
    const long T0 = 1_800_000_000;

    [Fact]
    public void Interstitials_follow_every_rule()
    {
        var ads = new AdPolicy(C);
        var s = new PlayerState { FirstPlayUnix = T0 };
        long day2 = T0 + 2 * 86400, day3 = T0 + 3 * 86400;
        Assert.False(ads.CanShowInterstitial(s, day2, false, true));      // pas avant le jour 3
        Assert.True(ads.CanShowInterstitial(s, day3, false, true));
        Assert.False(ads.CanShowInterstitial(s, day3, true, true));       // jamais en course
        Assert.False(ads.CanShowInterstitial(s, day3, false, false));     // seulement entre deux écrans
        ads.MarkInterstitialShown(s, day3);
        Assert.False(ads.CanShowInterstitial(s, day3 + 5 * 60, false, true));   // 6 minutes minimum
        Assert.True(ads.CanShowInterstitial(s, day3 + 6 * 60, false, true));
        s.NoAds = true;
        Assert.False(ads.CanShowInterstitial(s, day3 + 3600, false, true));
        Assert.True(ads.CanOfferRewardedVideo(false));                    // les vidéos de bonus restent
        Assert.False(ads.CanOfferRewardedVideo(true));
    }

    [Fact]
    public void Starter_offer_starts_after_second_session_once_and_lasts_48h()
    {
        var o = new OfferPolicy(C);
        var s = new PlayerState { SessionCount = 1 };
        Assert.False(o.TryStartStarter(s, T0));
        s.SessionCount = 2;
        Assert.True(o.TryStartStarter(s, T0));
        Assert.False(o.TryStartStarter(s, T0 + 10));                      // une seule fois
        Assert.True(o.IsStarterActive(s, T0 + 47 * 3600));
        Assert.False(o.IsStarterActive(s, T0 + 49 * 3600));
        Assert.False(o.TryStartStarter(s, T0 + 100 * 3600));              // jamais redonnée
    }

    [Fact]
    public void Season_pass_noads_limited_and_studio_timing()
    {
        var o = new OfferPolicy(C);
        var s = new PlayerState { FirstPlayUnix = T0 };
        Assert.False(o.ShouldShow(OfferKind.SeasonPass, s, T0 + 2 * 86400));
        Assert.True(o.ShouldShow(OfferKind.SeasonPass, s, T0 + 3 * 86400));
        s.RacesPlayed = 2; Assert.False(o.ShouldShow(OfferKind.NoAds, s, T0));
        s.RacesPlayed = 3; Assert.True(o.ShouldShow(OfferKind.NoAds, s, T0));
        s.EverPurchased = true; Assert.False(o.ShouldShow(OfferKind.NoAds, s, T0));
        Assert.False(o.ShouldShow(OfferKind.StudioPremium, s, T0));
        s.StageWins = 1; Assert.True(o.ShouldShow(OfferKind.StudioPremium, s, T0));
        Assert.False(o.ShouldShow(OfferKind.LimitedOffer, s, T0 + 6 * 86400));
        Assert.True(o.ShouldShow(OfferKind.LimitedOffer, s, T0 + 7 * 86400));
        s.LastLimitedOfferUnix = T0 + 7 * 86400;
        Assert.False(o.ShouldShow(OfferKind.LimitedOffer, s, T0 + 10 * 86400));   // au plus une par semaine
        Assert.True(o.ShouldShow(OfferKind.LimitedOffer, s, T0 + 14 * 86400));
    }

    [Fact]
    public void Retention_gate_uses_single_set_of_thresholds()
    {
        var g = C.Gates;
        Assert.Equal(RetentionVerdict.Stop, RetentionGate.Evaluate(g, 0.40, 0.11, 0.05));
        Assert.Equal(RetentionVerdict.Fix, RetentionGate.Evaluate(g, 0.34, 0.16, 0.05));
        Assert.Equal(RetentionVerdict.Fix, RetentionGate.Evaluate(g, 0.40, 0.14, 0.05));
        Assert.Equal(RetentionVerdict.SoftLaunchOk, RetentionGate.Evaluate(g, 0.36, 0.16, 0.05));
        Assert.Equal(RetentionVerdict.OnTarget, RetentionGate.Evaluate(g, 0.38, 0.18, 0.07));
    }

    [Fact]
    public void No_price_or_rate_literals_live_in_core_source()
    {
        var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../src/Echappee.Core"));
        var src = string.Join("\n", System.IO.Directory.GetFiles(root, "*.cs", System.IO.SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("BalanceConfig.cs")).Select(System.IO.File.ReadAllText));
        foreach (var banned in new[] { "2.99", "4.99", "0.99", "99.99", "58.0", "8.5", "135", "405" })
            Assert.DoesNotContain(banned, src);
    }
}

public class PacingTests
{
    // Joueur glouton : meilleure amélioration (coût / gain), une course classée 6e toutes les 90 s.
    static double[] FirstPurchaseMinutes(double hours)
    {
        var cfg = TestData.Load().Balance;
        var eco = new EconomyService(cfg);
        var s = new PlayerState();
        const long now = 1_800_000_000;
        var first = cfg.Infrastructures.Select(_ => double.NaN).ToArray();
        for (int t = 0; t < hours * 3600; t++)
        {
            eco.Tick(s, 1, now);
            if (t % 90 == 0) s.Primes = s.Primes + eco.RewardForPlace(s, 6, now).Primes;
            while (true)
            {
                var best = cfg.Infrastructures
                    .OrderBy(i => eco.UpgradeCost(i, s.InfraLevel(i.Id)).ToDouble() / System.Math.Max(1e-9, eco.UpgradeGain(i, s.InfraLevel(i.Id)).ToDouble()))
                    .First();
                if (!eco.TryUpgrade(s, best.Id)) break;
                int idx = cfg.Infrastructures.IndexOf(best);
                if (double.IsNaN(first[idx])) first[idx] = t / 60.0;
            }
        }
        return first;
    }

    [Fact]
    public void Infrastructures_unlock_progressively_not_all_at_once()
    {
        var f = FirstPurchaseMinutes(8);
        Assert.InRange(f[1], 0.5, 10);          // 2e : quelques minutes
        Assert.InRange(f[2], 2, 30);
        Assert.InRange(f[3], 8, 90);
        Assert.InRange(f[4], 30, 360);          // 5e : au bout d'une à quelques heures, jamais en 5 minutes
        for (int i = 1; i < f.Length; i++) Assert.True(f[i] > f[i - 1]);
    }
}

public class ComplianceTests
{
    [Fact]
    public void Paid_packs_can_be_blocked_per_country()
    {
        var packs = new PackService(TestData.Load());
        Assert.False(packs.IsPaidOpeningAllowed("BE"));
        Assert.False(packs.IsPaidOpeningAllowed("be"));
        Assert.True(packs.IsPaidOpeningAllowed("FR"));
        Assert.True(packs.IsPaidOpeningAllowed(null));
    }
}
