namespace Xease
{
    public static partial class LnString
    {
        public static string Format<T>(string format, T obj)
        {
            return string.Format(format, obj);
        }

        public static string Format<T1, T2>(string format, T1 obj1, T2 obj2)
        {
            return string.Format(format, obj1, obj2);
        }

        public static string Format<T1, T2, T3>(string format, T1 obj1, T2 obj2, T3 obj3)
        {
            return string.Format(format, obj1, obj2, obj3);
        }

        public static string Format<T1, T2, T3, T4>(string format, T1 obj1, T2 obj2, T3 obj3, T4 obj4)
        {
            return string.Format(format, obj1, obj2, obj3, obj4);
        }

        public static string Format<T1, T2, T3, T4, T5>(string format, T1 obj1, T2 obj2, T3 obj3, T4 obj4, T5 obj5)
        {
            return string.Format(format, obj1, obj2, obj3, obj4, obj5);
        }

        public static string Format<T1, T2, T3, T4, T5, T6>(string format, T1 obj1, T2 obj2, T3 obj3, T4 obj4, T5 obj5, T6 obj6)
        {
            return string.Format(format, obj1, obj2, obj3, obj4, obj5, obj6);
        }

        public static string Format<T1, T2, T3, T4, T5, T6, T7>(string format, T1 obj1, T2 obj2, T3 obj3, T4 obj4, T5 obj5, T6 obj6, T7 obj7)
        {
            return string.Format(format, obj1, obj2, obj3, obj4, obj5, obj6, obj7);
        }

        public static string Format<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 obj1, T2 obj2, T3 obj3, T4 obj4, T5 obj5, T6 obj6, T7 obj7, T8 obj8)
        {
            return string.Format(format, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8);
        }

        public static string Format(string format, params object[] param)
        {
            return string.Format(format, param);
        }
    }
}
