using System.Collections.Generic;
using Newtonsoft.Json;

namespace Echappee.Config
{
    /// <summary>Textes par clé. Repli : langue demandée, puis français (langue de base), puis la clé elle-même.</summary>
    public sealed class Localizer
    {
        public const string BaseLanguage = "fr";
        public static readonly string[] Languages = { "fr", "en", "es", "pt", "it", "de" };

        readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>();
        public string Language = BaseLanguage;

        public void Load(string lang, string json) =>
            _tables[lang] = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        public bool Has(string lang) => _tables.ContainsKey(lang);
        public IEnumerable<string> Keys(string lang) => _tables[lang].Keys;
        public string Raw(string lang, string key) => _tables[lang][key];

        public string Get(string key, params object[] args)
        {
            string s = null;
            if (_tables.TryGetValue(Language, out var t) && t.TryGetValue(key, out var v)) s = v;
            else if (_tables.TryGetValue(BaseLanguage, out var b) && b.TryGetValue(key, out var bv)) s = bv;
            if (s == null) return key;
            return args == null || args.Length == 0 ? s : string.Format(System.Globalization.CultureInfo.InvariantCulture, s, args);
        }
    }
}
