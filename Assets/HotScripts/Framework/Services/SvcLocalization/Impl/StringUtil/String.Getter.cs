namespace Xease
{
    public static partial class LnString
    {
        public static string GetString(string key)
            => G.Localization.GetString(key);

        public static string GetString<T>(string key, T param)
            => G.Localization.GetString(key, param);

        public static string GetString<T1, T2>(string key, T1 param1, T2 param2)
            => G.Localization.GetString(key, param1, param2);

        public static string GetString<T1, T2, T3>(string key, T1 param1, T2 param2, T3 param3)
            => G.Localization.GetString(key, param1, param2, param3);

        public static string GetString<T1, T2, T3, T4>(string key, T1 param1, T2 param2, T3 param3, T4 param4)
            => G.Localization.GetString(key, param1, param2, param3, param4);

        public static string GetString<T1, T2, T3, T4, T5>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
            => G.Localization.GetString(key, param1, param2, param3, param4, param5);

        public static string GetString<T1, T2, T3, T4, T5, T6>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
            => G.Localization.GetString(key, param1, param2, param3, param4, param5, param6);

        public static string GetString<T1, T2, T3, T4, T5, T6, T7>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5,
            T6 param6, T7 param7)
            => G.Localization.GetString(key, param1, param2, param3, param4, param5, param6, param7);

        public static string GetString<T1, T2, T3, T4, T5, T6, T7, T8>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5,
            T6 param6, T7 param7, T8 param8)
            => G.Localization.GetString(key, param1, param2, param3, param4, param5, param6, param7, param8);

        public static string GetString(string key, params object[] param)
            => G.Localization.GetString(key, param);
    }
}
