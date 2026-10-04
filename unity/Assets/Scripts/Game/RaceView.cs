using UnityEngine;
using Echappee.Simulation;

namespace Echappee.Game
{
    /// <summary>
    /// Affichage minimal : un point coloré par équipe sur un circuit défini par des points de passage (Transform).
    /// Les écarts sont exagérés (gapScale) pour qu'un peloton serré reste lisible. NON TESTÉ dans l'éditeur Unity.
    /// </summary>
    public sealed class RaceView : MonoBehaviour
    {
        public Transform[] waypoints;            // boucle fermée, dans l'ordre
        public GameObject riderPrefab;           // petit sprite rond
        public float gapScale = 4f;
        public Color playerColor = new Color(0.12f, 0.80f, 0.56f);

        RaceResult _result;
        Transform[] _dots;
        float _t;
        float[] _cum;
        float _len;

        public void Play(RaceResult r, int teamCount)
        {
            _result = r; _t = 0;
            if (_dots != null) foreach (var d in _dots) if (d) Destroy(d.gameObject);
            _dots = new Transform[teamCount];
            for (int i = 0; i < teamCount; i++)
            {
                var go = Instantiate(riderPrefab, transform);
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr) sr.color = i == 0 ? playerColor : Color.HSVToRGB(i / (float)teamCount, 0.6f, 1f);
                _dots[i] = go.transform;
            }
            BuildPath();
        }

        void BuildPath()
        {
            _cum = new float[waypoints.Length + 1];
            for (int i = 0; i < waypoints.Length; i++)
                _cum[i + 1] = _cum[i] + Vector2.Distance(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position);
            _len = _cum[waypoints.Length];
        }

        Vector2 PointAt(float frac)
        {
            float d = Mathf.Repeat(frac, 1f) * _len;
            int i = 0;
            while (i < waypoints.Length - 1 && _cum[i + 1] < d) i++;
            float k = (d - _cum[i]) / Mathf.Max(0.0001f, _cum[i + 1] - _cum[i]);
            return Vector2.Lerp(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position, k);
        }

        void Update()
        {
            if (_result == null || _result.Frames.Count < 2) return;
            _t += Time.deltaTime;
            float snap = (float)(_result.Frames[1].Time - _result.Frames[0].Time);
            int i = Mathf.Min(_result.Frames.Count - 2, Mathf.FloorToInt(_t / snap));
            float k = Mathf.Clamp01((_t - i * snap) / snap);
            var a = _result.Frames[i].Positions; var b = _result.Frames[i + 1].Positions;
            float lead = 0;
            for (int j = 0; j < a.Length; j++) lead = Mathf.Max(lead, Mathf.Lerp(a[j], b[j], k));
            for (int j = 0; j < _dots.Length; j++)
            {
                float p = Mathf.Lerp(a[j], b[j], k);
                float vis = Mathf.Max(0, lead - (lead - p) * gapScale);
                _dots[j].position = PointAt(vis);
            }
        }
    }
}
