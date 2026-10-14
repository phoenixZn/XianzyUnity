using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Xease
{
    internal partial class LocalizationData : ILocalizationJsonSink
    {
        /// <summary>
        /// 切换语言并清空后，用宿主提供的来源重新填表。
        /// reload 为 false 时，语言未变或目标为 unknown 则直接返回。
        /// </summary>
        public void Load(Language language, ILocalizationJsonSource source, bool reload = false)
        {
            if (!reload && (_language == language || language == Language.unknown))
                return;

            _language = language;

            Unload();
            source?.Load(language, this);
        }

        /// <summary>
        /// 清空当前字符串表。
        /// </summary>
        public void Unload()
        {
            _dictionary.Clear();
        }

        private static JsonTextReader CreateJsonReader(TextReader reader)
        {
            return new JsonTextReader(reader)
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal,
            };
        }

        private void AddRawStringByJson(byte[] jsonBytes, string source = null)
        {
            if (jsonBytes == null || jsonBytes.Length == 0)
            {
                LogAddRawStringByJsonFailed("empty json", source);
                return;
            }

            using var stream = new MemoryStream(jsonBytes, writable: false);
            using var textReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var jsonReader = CreateJsonReader(textReader);
            AddRawStringByJson(jsonReader, source);
        }

        private void AddRawStringByJson(JsonTextReader jsonReader, string source)
        {
            try
            {
                if (!jsonReader.Read() || jsonReader.TokenType != JsonToken.StartObject)
                {
                    LogAddRawStringByJsonFailed("invalid json format", source);
                    return;
                }

                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.EndObject)
                    {
                        return;
                    }

                    if (jsonReader.TokenType != JsonToken.PropertyName)
                    {
                        LogAddRawStringByJsonFailed("invalid json format", source);
                        return;
                    }

                    var key = jsonReader.Value as string;
                    if (!jsonReader.Read())
                    {
                        LogAddRawStringByJsonFailed("invalid json format", source);
                        return;
                    }

                    if (jsonReader.TokenType != JsonToken.String && jsonReader.TokenType != JsonToken.Null)
                    {
                        LogAddRawStringByJsonFailed($"invalid value token: {jsonReader.TokenType}", source);
                        return;
                    }

                    var value = jsonReader.TokenType == JsonToken.String ? (jsonReader.Value as string) : string.Empty;
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    if (!_dictionary.TryAdd(key, value))
                    {
                        LogAddRawStringByJsonFailed($"duplicated key: {key}", source);
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                LogAddRawStringByJsonFailed($"json parse exception: {e.Message}", source);
            }
        }

        private static void LogAddRawStringByJsonFailed(string reason, string source)
        {
            G.LogError(string.IsNullOrEmpty(source)
                ? $"[Localization] AddRawStringByJson Failed: {reason}"
                : $"[Localization] AddRawStringByJson Failed: {reason}, source: {source}");
        }

        //////////////////////////////////////////////////////////////////////////
        /// ILocalizationJsonSink:

        /// <summary>
        /// 解析一份 JSON 原文写入当前字符串表。
        /// </summary>
        void ILocalizationJsonSink.AddJson(byte[] jsonBytes, string sourcePath)
        {
            AddRawStringByJson(jsonBytes, sourcePath);
        }
    }
}
