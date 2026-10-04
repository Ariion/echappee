using System;
using Newtonsoft.Json;

namespace Echappee.Numbers
{
    public sealed class BigAmountJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(BigAmount) || t == typeof(BigAmount?);

        public override void WriteJson(JsonWriter w, object v, JsonSerializer s) =>
            w.WriteValue(((BigAmount)v).ToSaveString());

        public override object ReadJson(JsonReader r, Type t, object existing, JsonSerializer s) =>
            r.Value == null ? BigAmount.Zero : BigAmount.FromSaveString(r.Value.ToString());
    }
}
