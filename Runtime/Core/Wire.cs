using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Nuxie.Unity.Internal
{
    internal static class Wire
    {
        internal static NuxieException Invalid(string message) => new NuxieException("invalidArgument", message);
        internal static string Text(string value, string name) => !string.IsNullOrWhiteSpace(value) ? value : throw Invalid(name + " must not be empty");
        internal static double Quantity(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 1 && value <= 9007199254740991d && Math.Truncate(value) == value ? value : throw Invalid("Quantity must be a positive safe integer");
        internal static JObject Object(string value) => JObject.Parse(value);
        internal static string Encode(JObject value) => value.ToString(Formatting.None);
        internal static string String(JObject value, string key) => value[key]?.Type == JTokenType.String ? (string)value[key] : throw Invalid("Missing string: " + key);
        internal static bool Bool(JObject value, string key) => value[key]?.Type == JTokenType.Boolean ? (bool)value[key] : throw Invalid("Missing boolean: " + key);
        internal static double Number(JObject value, string key)
        {
            var token = value[key];
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)) throw Invalid("Missing number: " + key);
            var result = (double)token;
            if (double.IsInfinity(result) || double.IsNaN(result)) throw Invalid("Nonfinite number: " + key);
            return result;
        }
        internal static double? NullableNumber(JObject value, string key) => value[key] == null || value[key].Type == JTokenType.Null ? (double?)null : Number(value, key);
        internal static JObject Properties(IReadOnlyDictionary<string, object> value)
        {
            return value == null ? new JObject() : (JObject)ToJson(value, new HashSet<object>(), 0);
        }
        private static JToken ToJson(object value, HashSet<object> parents, int depth)
        {
            if (depth > 32) throw Invalid("Properties exceed maximum nesting depth");
            if (value == null) return JValue.CreateNull();
            if (value is string || value is bool) return new JValue(value);
            if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
            {
                double n = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(n) || double.IsInfinity(n)) throw Invalid("Properties must contain finite numbers");
                return new JValue(value);
            }
            if (!parents.Add(value)) throw Invalid("Properties contain a cycle");
            try
            {
                if (value is IReadOnlyDictionary<string, object> map)
                { var result = new JObject(); foreach (var pair in map) result.Add(pair.Key, ToJson(pair.Value, parents, depth + 1)); return result; }
                if (value is IList list)
                { var result = new JArray(); foreach (var item in list) result.Add(ToJson(item, parents, depth + 1)); return result; }
                throw Invalid("Unsupported property type: " + value.GetType().Name);
            }
            finally { parents.Remove(value); }
        }
        internal static object Freeze(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token is JObject map)
            { var result = new Dictionary<string, object>(); foreach (var p in map.Properties()) result.Add(p.Name, Freeze(p.Value)); return new ReadOnlyDictionary<string, object>(result); }
            if (token is JArray array)
            { var result = new List<object>(); foreach (var item in array) result.Add(Freeze(item)); return result.AsReadOnly(); }
            return ((JValue)token).Value;
        }
        internal static IReadOnlyDictionary<string, object> Map(JToken token) => (IReadOnlyDictionary<string, object>)Freeze(token);
        internal static T EnumValue<T>(JObject value, string key) where T : struct
        { if (Enum.TryParse<T>(String(value, key), true, out var result) && Enum.IsDefined(typeof(T), result)) return result; throw Invalid("Unknown " + key); }
        internal static FeatureAccess Access(JObject v) => new FeatureAccess(Bool(v,"allowed"), Bool(v,"unlimited"), NullableNumber(v,"balance"), EnumValue<FeatureType>(v,"type"));
        internal static FeatureSnapshot Snapshot(JObject v)
        {
            var all = new Dictionary<string, FeatureAccess>();
            foreach (var p in ((JObject)v["all"]).Properties()) all.Add(p.Name, Access((JObject)p.Value));
            return new FeatureSnapshot(EnumValue<FeatureStateKind>(v,"state"), ulong.Parse(String(v,"identityGeneration"), CultureInfo.InvariantCulture), ulong.Parse(String(v,"revision"), CultureInfo.InvariantCulture), all);
        }
        internal static FeatureConsumption Receipt(JObject v) => new FeatureConsumption(String(v,"customerId"),String(v,"featureId"),String(v,"operationId"),NullableNumber(v,"occurredAtMs"),Number(v,"quantity"),Bool(v,"accepted"),String(v,"code"),NullableNumber(v,"balance"),Bool(v,"unlimited"),Bool(v,"active"),Bool(v,"idempotentReplay"));
    }
}
