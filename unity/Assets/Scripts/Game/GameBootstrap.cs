using UnityEngine;
using Echappee.Config;
using Echappee.Simulation;

namespace Echappee.Game
{
    /// <summary>
    /// Scène minimale de l'étape 2 : lance une course simulée et la fait jouer par RaceView.
    /// Dépose ce script sur un GameObject vide, assigne 'view', puis Play.
    /// NON TESTÉ dans l'éditeur Unity (écrit sans Unity) : à vérifier à la première ouverture.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public RaceView view;
        public string discipline = "route";
        public int playerLevel = 55;
        public ulong seed = 1;

        GameData _data;

        void Start()
        {
            _data = GameData.FromJson(
                Resources.Load<TextAsset>("Data/balance").text,
                Resources.Load<TextAsset>("Data/courses").text,
                Resources.Load<TextAsset>("Data/riders").text);
            PlayRace();
        }

        [ContextMenu("Rejouer")]
        public void PlayRace()
        {
            var cfg = _data.Balance;
            var disc = cfg.Disciplines[discipline];
            var me = BotTeams.Create("player", "Mon équipe", playerLevel, new Rng(7), cfg.Race.StartersPerTeam);
            me.IsPlayer = true;
            var field = BotTeams.Field(cfg, me, 50, 99);
            var result = new RaceSimulator(cfg).Run(field, _data.Courses[disc.Circuit], disc, seed++, recordFrames: true);
            view.Play(result, field.Count);
        }
    }
}
