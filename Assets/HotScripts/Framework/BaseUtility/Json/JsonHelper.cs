using System.IO;
using TinyJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Framework
{
    public static class JsonHelper
    {
        // 轻量的Json解析使用TinyJson，性能更好
        public static string ToJson(object obj)
        {
            return obj.ToJson();
        }

        // 轻量的Json解析使用TinyJson，性能更好
        public static T FromJson<T>(string json)
        {
            return json.FromJson<T>();
        }

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new ()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };

        // 复杂的结构使用Newtonsoft.Json，功能更强大
        public static string ToJsonNewtonsoft(object obj)
        {
            return JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
        }
        
        public static string ToJsonNewtonsoft(object obj, JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }
        
        // 复杂的结构使用Newtonsoft.Json，功能更强大
        public static T FromJsonNewtonsoft<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }
        
        public static string PrettyPrintFromJsonNewtonsoft(string json)
        {
            var obj = JsonConvert.DeserializeObject(json, _jsonSerializerSettings);
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            jsonWriter.Formatting = Formatting.Indented;
            jsonWriter.IndentChar = ' ';
            jsonWriter.Indentation = 4;
            var serializer = JsonSerializer.CreateDefault(_jsonSerializerSettings);
            serializer.Serialize(jsonWriter, obj);
            return stringWriter.ToString();
        }
        
        public static JObject ToJObject(string json)
        {
            return JObject.Parse(json);
        }
        
        public static T GetProperty<T>(this JObject jObject, string propertyPath)
        {
            if (jObject == null)
                return default(T);

            JToken token = jObject.SelectToken(propertyPath);
            return token != null ? token.Value<T>() : default(T);
        }
        
        public static object GetProperty(this JObject jObject, string propertyPath)
        {
            return GetProperty<object>(jObject, propertyPath);
        }
        
        public static T GetProperty<T>(this JObject jObject, string propertyPath, T defaultValue)
        {
            if (jObject == null)
                return defaultValue;

            JToken token = jObject.SelectToken(propertyPath);
            return token != null ? token.Value<T>() : defaultValue;
        }
        
        public static bool HasProperty(this JObject jObject, string propertyPath)
        {
            if (jObject == null)
                return false;

            JToken token = jObject.SelectToken(propertyPath);
            return token != null;
        }

        public static T GetArrayElement<T>(JArray jArray, int index)
        {
            if (jArray == null || index < 0 || index >= jArray.Count)
                return default(T);

            return jArray[index].Value<T>();
        }

        public static T GetArrayElement<T>(JArray jArray, int index, T defaultValue)
        {
            if (jArray == null || index < 0 || index >= jArray.Count)
                return defaultValue;

            return jArray[index].Value<T>();
        }
    }
}