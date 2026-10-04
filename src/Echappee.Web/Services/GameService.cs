using Echappee.Config;
using Echappee.Economy;
using Echappee.Numbers;
using Echappee.Simulation;
using Echappee.Studio;
using Echappee.Leagues;
using Echappee.Monetization;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace Echappee.Web.Services;

public sealed class RaceSession
{
    public RaceResult Result;
    public List<Team> Field;
    public string DiscKey;
    public DisciplineConfig Disc;
    public double Elapsed;
    public double Speed = 1;
    public bool Finished;
    public int Rank;
    public double PlayerTime;
    public EconomyService.RaceReward Reward;
    public RiderCardDef CardWon;
    public bool Won => Rank == 1;
    public double LeaderTime => Result.Standings[0].FinishTime;
}

public sealed class LeagueResult { public LeagueOutcome Outcome; public int Rank; public int FromTier, ToTier; public int Watts; }

class SaveData
{
    public PlayerState State;
    public PlanDeCourse Plan;
    public JerseyDesign Jersey;
    public string TeamName;
    public string Language;
    public bool TestMode;
    public int LeagueTier; public long LeagueStart; public ulong LeagueSeed; public int[] LeaguePoints;
}

/// <summary>Colle le cœur testé (Echappee.Core) à l'interface : état, sauvegarde, boucle de jeu, course en cours.</summary>
public sealed class GameService
{
    const string SaveKey = "echappee.save.v1";
    readonly HttpClient _http;
    readonly IJSRuntime _js;

    public GameData Data;
    public Localizer Loc = new();
    public PlayerState S = new();
    public EconomyService Eco;
    public PackService Packs;
    public ShopService Shop;
    public AdPolicy Ads;
    public OfferPolicy Offers;
    public LeagueSeason League;
    public PlanDeCourse Plan = new() { Slots = 3 };
    public JerseyDesign Jersey = new();
    public string TeamName = "Cadence Mistral";
    public string Language = "fr";
    public bool TestMode = true;      // pas de vrais paiements dans cette version : les achats sont des essais
    public string Region = "";
    public bool Ready;
    public string Error;
    public Rng Rng = new((ulong)DateTime.UtcNow.Ticks);

    public RaceSession Race;
    public EconomyService.OfflineResult PendingOffline;
    public LeagueResult PendingLeague;
    public int PendingDailyWatts;
    public List<PackPull> LastPulls;
    public string LastPackKey;
    public string Toast;
    double _toastLeft, _secAcc, _saveAcc;

    public event Action Changed;
    public void Notify() => Changed?.Invoke();

    public GameService(HttpClient http, IJSRuntime js) { _http = http; _js = js; }

    public long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public string Dec => Language == "en" ? "." : ",";
    public string T(string key, params object[] a) => Loc.Get(key, a);
    public string Fmt(BigAmount v) => v.ToString(Dec);
    public bool InRace => Race != null && !Race.Finished;

    // ------------------------------------------------------------ démarrage

    public async Task InitAsync()
    {
        try
        {
            var balance = await _http.GetStringAsync("data/balance.json");
            var courses = await _http.GetStringAsync("data/courses.json");
            var riders = await _http.GetStringAsync("data/riders.json");
            Data = GameData.FromJson(balance, courses, riders);
            foreach (var l in Localizer.Languages) Loc.Load(l, await _http.GetStringAsync($"data/strings/{l}.json"));
            Eco = new EconomyService(Data.Balance);
            Packs = new PackService(Data);
            Shop = new ShopService(Data, Eco, Packs);
            Ads = new AdPolicy(Data.Balance);
            Offers = new OfferPolicy(Data.Balance);
            Region = (await _js.InvokeAsync<string>("ech.region") ?? "").ToUpperInvariant();

            var json = await _js.InvokeAsync<string>("ech.load", SaveKey);
            long now = Now;
            if (!string.IsNullOrEmpty(json) && TryLoad(json))
            {
                S.SessionCount++;
                var off = Eco.OfflineEarnings(S, now, false);
                if (off.SecondsCredited >= 60 && !off.Earned.IsZero) PendingOffline = off;
                else S.LastSeenUnix = now;
            }
            else
            {
                string lang = await _js.InvokeAsync<string>("ech.lang");
                Language = Localizer.Languages.Contains(lang) ? lang : "fr";
                Shop.NewGame(S, now, Rng);
                Plan = new PlanDeCourse { Slots = 3 };
                League = NewLeague(0, now);
            }
            Loc.Language = Language;
            Shop.EnsureSeason(S, now);
            Offers.TryStartStarter(S, now);
            PendingDailyWatts = Shop.ClaimDailyLogin(S, now);
            Ready = true;
            await SaveAsync();
        }
        catch (Exception e) { Error = e.Message; }
        Notify();
    }

    bool TryLoad(string json)
    {
        try
        {
            var d = JsonConvert.DeserializeObject<SaveData>(json);
            if (d?.State == null) return false;
            S = d.State; Plan = d.Plan ?? new PlanDeCourse { Slots = 3 }; Jersey = d.Jersey ?? new JerseyDesign();
            TeamName = d.TeamName ?? TeamName; Language = d.Language ?? "fr"; TestMode = d.TestMode;
            League = NewLeague(d.LeagueTier, d.LeagueStart > 0 ? d.LeagueStart : Now, d.LeagueSeed);
            if (d.LeaguePoints != null && d.LeaguePoints.Length == League.Entries.Count)
                for (int i = 0; i < d.LeaguePoints.Length; i++) League.Entries[i].Points = d.LeaguePoints[i];
            return true;
        }
        catch { return false; }
    }

    public async Task SaveAsync()
    {
        if (!Ready) return;
        var d = new SaveData
        {
            State = S, Plan = Plan, Jersey = Jersey, TeamName = TeamName, Language = Language, TestMode = TestMode,
            LeagueTier = League.Tier, LeagueStart = League.StartUnix, LeagueSeed = _leagueSeed,
            LeaguePoints = League.Entries.Select(e => e.Points).ToArray()
        };
        S.LastSeenUnix = Now;
        await _js.InvokeVoidAsync("ech.save", SaveKey, JsonConvert.SerializeObject(d));
    }

    public string ExportSave() => JsonConvert.SerializeObject(new { State = S, Plan, Jersey, TeamName });

    public async Task ResetAsync()
    {
        await _js.InvokeVoidAsync("ech.remove", SaveKey);
        S = new PlayerState(); Plan = new PlanDeCourse { Slots = 3 }; Jersey = new JerseyDesign(); Race = null;
        long now = Now;
        Shop.NewGame(S, now, Rng); League = NewLeague(0, now); Shop.EnsureSeason(S, now);
        PendingOffline = null; PendingLeague = null; LastPulls = null;
        await SaveAsync(); Notify();
    }

    // ------------------------------------------------------------ boucle

    /// <summary>Appelé ~10 fois par seconde par le Shell.</summary>
    public void Step(double dt)
    {
        if (!Ready) return;
        bool changed = false;
        if (_toastLeft > 0) { _toastLeft -= dt; if (_toastLeft <= 0) { Toast = null; changed = true; } }
        if (Race != null && !Race.Finished)
        {
            Race.Elapsed += dt * Race.Speed;
            if (Race.Elapsed >= Math.Min(Race.Result.Duration, Race.LeaderTime + 4)) FinishRace();
            changed = true;
        }
        _secAcc += dt;
        if (_secAcc >= 1)
        {
            int whole = (int)_secAcc; _secAcc -= whole;
            if (PendingOffline == null) Eco.Tick(S, whole, Now);
            if (League.IsOver(Now) && PendingLeague == null) SettleLeague();
            changed = true;
        }
        _saveAcc += dt;
        if (_saveAcc >= 10) { _saveAcc = 0; _ = SaveAsync(); }
        if (changed) Notify();
    }

    public void ShowToast(string text) { Toast = text; _toastLeft = 2.5; Notify(); }

    // ------------------------------------------------------------ course

    public double Income => Eco.IncomePerSecond(S, Now).ToDouble();

    public Team BuildPlayer()
    {
        var t = Packs.BuildTeam(S, "player", TeamName);
        t.Plan = Plan.Clone();
        return t;
    }

    public double BotLevel => Data.Balance.Economy.BotLevelBase + S.LeagueTier * Data.Balance.Economy.BotLevelPerTier;

    public void StartRace()
    {
        if (InRace || !Ready) return;
        var cfg = Data.Balance;
        string key = DisciplineUnlocked(S.SelectedDiscipline) ? S.SelectedDiscipline : "route";
        var disc = cfg.Disciplines[key];
        var player = BuildPlayer();
        if (player.Starters.Count == 0) { ShowToast(T("msg.noStarters")); return; }
        ulong seed = Rng.NextULong();
        var field = BotTeams.Field(cfg, player, BotLevel, seed ^ 0x5DEECE66DUL);
        var res = new RaceSimulator(cfg).Run(field, Data.Courses[disc.Circuit], disc, seed, recordFrames: true);
        Race = new RaceSession { Result = res, Field = field, DiscKey = key, Disc = disc, Rank = res.PlayerRank };
        Notify();
    }

    void FinishRace()
    {
        var r = Race;
        r.Finished = true;
        r.Rank = r.Result.PlayerRank;
        var st = r.Result.ByTeam(0);
        r.PlayerTime = double.IsInfinity(st.FinishTime) ? r.Result.Duration : st.FinishTime;
        var rew = Eco.RewardForPlace(S, r.Rank, Now);
        r.Reward = rew;
        S.Primes = S.Primes + rew.Primes;
        S.RacesPlayed++; S.PassRaces++;
        if (r.Won) S.StageWins++;
        League.RecordRace(r.Rank, Rng);
        if (Rng.Chance(rew.CardChance))
        {
            var pro = Packs.Pack("pro");
            var rarity = Packs.RollRarity(pro, 0, Rng, out _);
            r.CardWon = Shop.GrantRandom(S, rarity, Rng);
        }
        _ = SaveAsync();
    }

    public void SkipRace() { if (InRace) { Race.Elapsed = Race.Result.Duration; } }
    public void SetSpeed(double s) { if (Race != null) Race.Speed = s; Notify(); }
    public void CloseRace() { if (Race != null && Race.Finished) { Race = null; Notify(); } }

    public string PosterSvg()
    {
        if (Race == null || !Race.Finished) return "";
        var d = new PosterData
        {
            TeamName = TeamName, DisciplineKey = Race.DiscKey, League = T("tier." + League.Tier), Stage = S.StageWins,
            TimeSeconds = Race.PlayerTime, Jersey = Jersey
        };
        return PosterRenderer.Render(d, Loc);
    }

    // ------------------------------------------------------------ ligues et disciplines

    ulong _leagueSeed;

    LeagueSeason NewLeague(int tier, long start, ulong? seed = null)
    {
        _leagueSeed = seed ?? Rng.NextULong();
        double power = Math.Max(30, PlayerPower());
        return new LeagueSeason(Data.Balance, tier, power, start, _leagueSeed) { };
    }

    public double PlayerPower()
    {
        if (Packs == null) return 50;
        var t = Packs.BuildTeam(S, "p", "p");
        return t.Starters.Count == 0 ? 50 : t.Power();
    }

    public void SettleLeague()
    {
        var outcome = League.Outcome();
        var res = new LeagueResult { Outcome = outcome, Rank = League.PlayerRank(), FromTier = League.Tier, ToTier = League.NextTier() };
        if (outcome == LeagueOutcome.Promoted) { res.Watts = Data.Balance.Economy.PromotionWatts; S.Watts += res.Watts; }
        S.LeagueTier = res.ToTier;
        S.BestLeagueTier = Math.Max(S.BestLeagueTier, res.ToTier);
        PendingLeague = res;
        League = NewLeague(res.ToTier, Now);
        _ = SaveAsync();
    }

    public bool DisciplineUnlocked(string key) =>
        LeagueSeason.IsDisciplineUnlocked(Data.Balance, Data.Balance.Disciplines[key], S.BestLeagueTier);

    // ------------------------------------------------------------ actions

    public void BuyUpgrade(string id)
    {
        if (Eco.TryUpgrade(S, id)) { _ = SaveAsync(); Notify(); }
    }

    public bool CanOpenPaid() => Packs.IsPaidOpeningAllowed(Region);

    public void OpenPack(string key, int count)
    {
        if (!CanOpenPaid()) { ShowToast(T("msg.regionBlocked")); return; }
        var pulls = Packs.Open(S, key, count, Rng);
        if (pulls == null) { ShowToast(T("msg.noWatts")); return; }
        LastPulls = pulls; LastPackKey = key;
        _ = SaveAsync(); Notify();
    }

    public void OpenFreePack()
    {
        var pulls = Packs.OpenFreeDaily(S, Now, Rng);
        if (pulls == null) { ShowToast(T("msg.noFree")); return; }
        LastPulls = pulls; LastPackKey = "pro";
        _ = SaveAsync(); Notify();
    }

    public int FreePacksLeft
    {
        get
        {
            int day = EconomyService.DayIndex(Now);
            int used = S.DayIndexOfFreePacks == day ? S.FreePacksUsedToday : 0;
            return Data.Balance.Economy.FreePacksPerDay - used;
        }
    }

    public int VideoWattsLeft
    {
        get
        {
            int day = EconomyService.DayIndex(Now);
            int used = S.DayIndexOfVideoWatts == day ? S.VideoWattsToday : 0;
            return Data.Balance.Economy.VideoWattsPerDay - used;
        }
    }

    public void LevelUp(string id) { if (Packs.TryLevelUp(S, id)) { _ = SaveAsync(); Notify(); } }

    public void ToStarters(string id)
    {
        if (S.Starters.Contains(id) || !S.Cards.ContainsKey(id)) return;
        int cap = Data.Balance.Race.StartersPerTeam;
        if (S.Starters.Count >= cap)
        {
            var weakest = S.Starters.OrderBy(Score).First();
            S.Starters.Remove(weakest);
        }
        S.Starters.Add(id); _ = SaveAsync(); Notify();
    }

    public void ToBench(string id)
    {
        if (S.Starters.Count <= 1) return;
        S.Starters.Remove(id); _ = SaveAsync(); Notify();
    }

    public double Score(string id)
    {
        var def = Data.Riders.First(r => r.Id == id);
        var o = S.Cards[id];
        var r = def.ToRider(o.Level, Data.Balance.Economy.LevelStatBonusPct);
        return r.Stats.Sprint + r.Stats.Climb + r.Stats.Rouleur + r.Stats.Technique;
    }

    public RiderCardDef Def(string id) => Data.Riders.First(r => r.Id == id);

    public void SelectDiscipline(string key) { if (DisciplineUnlocked(key)) { S.SelectedDiscipline = key; _ = SaveAsync(); Notify(); } }

    public void ClaimOffline(bool video)
    {
        if (PendingOffline == null) return;
        var r = Eco.ClaimOffline(S, Now, video);
        PendingOffline = null; _ = SaveAsync();
        ShowToast("+" + Fmt(r.Earned));
    }

    public void AckDaily() { PendingDailyWatts = 0; Notify(); }
    public void AckLeague() { PendingLeague = null; Notify(); }
    public void CloseCards() { LastPulls = null; Notify(); }

    // Vidéos récompensées : SIMULÉES (aucun réseau publicitaire dans cette version).
    public Action PendingVideoReward;
    public double VideoCountdown;
    public void WatchVideo(Action reward)
    {
        if (!Ads.CanOfferRewardedVideo(InRace)) { ShowToast(T("msg.noAdsInRace")); return; }
        PendingVideoReward = reward; VideoCountdown = 5; Notify();
    }
    public void TickVideo(double dt)
    {
        if (PendingVideoReward == null) return;
        VideoCountdown -= dt;
        if (VideoCountdown <= 0) { var r = PendingVideoReward; PendingVideoReward = null; r(); _ = SaveAsync(); }
        Notify();
    }
    public void CancelVideo() { PendingVideoReward = null; Notify(); }

    public void VideoBoost() => WatchVideo(() => { Eco.StartVideoBoost(S, Now); ShowToast(T("msg.boost")); });
    public void VideoWatts() => WatchVideo(() =>
    {
        if (Eco.TryClaimVideoWatts(S, Now, 10)) ShowToast("+10 W"); else ShowToast(T("msg.dailyLimit"));
    });

    public async Task Share()
    {
        var svg = PosterSvg();
        if (svg.Length == 0) return;
        await _js.InvokeAsync<string>("ech.sharePoster", svg, T("msg.shareText", TeamName));
    }

    public async Task Download(string name, string text) => await _js.InvokeVoidAsync("ech.download", name, text);
}
