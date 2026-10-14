using System;
using System.Text;

namespace Xease
{
    public static partial class LnString
    {
        private const string StrFormatNumberKilo = "number_kilo";
        private const string StrFormatNumberMega = "number_mega";

        private static readonly StringBuilder _numFormat = new();

        /// <summary>
        /// 获取数字的三位分节法表示
        /// </summary>
        /// <param name="l">需要表示的数字</param>
        /// <returns>按","分隔的字符串</returns>
        public static string DigitalSegmentation_3(this long l) => DigitalSegmentation3((ulong)l);
        
        /// <summary>
        /// 获取数字的三位分节法表示
        /// </summary>
        /// <param name="i">需要表示的数字</param>
        /// <returns>按","分隔的字符串</returns>
        public static string DigitalSegmentation_3(this int i) => DigitalSegmentation3((ulong)i);
        
        /// <summary>
        /// 获取数字的三位分节法表示
        /// </summary>
        /// <param name="ui">需要表示的数字</param>
        /// <returns>按","分隔的字符串</returns>
        public static string DigitalSegmentation_3(this uint ui) => DigitalSegmentation3((ulong)ui);

        /// <summary>
        /// 获取数字的三位分节法表示
        /// </summary>
        /// <param name="ul">需要表示的数字</param>
        /// <returns>按","分隔的字符串</returns>
        public static string DigitalSegmentation3(this ulong ul) 
        {
            if (ul < 999) return ul.ToString();

            ushort tail = (ushort)(ul % 1000);
            
            ul /= 1000;
            _numFormat.Clear();
            
            int idx = 0;
            while (0 != ul)
            {
                if (0 == idx % 3)
                    _numFormat.Append(",");
                
                _numFormat.Append(ul % 10);
                ul /= 10;
                idx++;
            }
            
            char[] charArray = _numFormat.ToString().ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray) + tail;
        }
        
        /// <summary>
        /// 获取数字缩写，支持千级、百万级显示，低于1000则全部显示
        /// 小数部分超出保留长度的将会直接舍弃
        /// </summary>
        /// <param name="number"></param>
        /// <param name="decimalLen">小数部分保留长度</param>
        /// <returns>缩写后的字符串</returns>
        public static string Abbreviation(this int number, int decimalLen = 1)
        {
            return Inner_FormatNumberAbbreviation(number.ToString(), decimalLen);
        }

        /// <summary>
        /// 获取数字缩写，支持千级、百万级显示，低于1000则全部显示
        /// 小数部分超出保留长度的将会直接舍弃
        /// </summary>
        /// <param name="number"></param>
        /// <param name="decimalLen">小数部分保留长度</param>
        /// <returns>缩写后的字符串</returns>
        public static string Abbreviation(this uint number, int decimalLen = 1)
        {
            return Inner_FormatNumberAbbreviation(number.ToString(), decimalLen);
        }

        /// <summary>
        /// 获取数字缩写，支持千级、百万级显示，低于1000则全部显示
        /// 小数部分超出保留长度的将会直接舍弃
        /// </summary>
        /// <param name="number"></param>
        /// <param name="decimalLen">小数部分保留长度</param>
        /// <returns>缩写后的字符串</returns>
        public static string Abbreviation(this long number, int decimalLen = 1)
        {
            return Inner_FormatNumberAbbreviation(number.ToString(), decimalLen);
        }

        /// <summary>
        /// 获取数字缩写，支持千级、百万级显示，低于1000则全部显示
        /// 小数部分超出保留长度的将会直接舍弃
        /// </summary>
        /// <param name="number"></param>
        /// <param name="decimalLen">小数部分保留长度</param>
        /// <returns>缩写后的字符串</returns>
        public static string Abbreviation(this ulong number, int decimalLen = 1)
        {
            return Inner_FormatNumberAbbreviation(number.ToString(), decimalLen);
        }

        /// <summary>
        /// 获取数字缩写，支持千级、百万级显示，低于1000则全部显示
        /// 小数部分超出保留长度的将会直接舍弃
        /// </summary>
        /// <param name="number">数字字符串（仅包含数字）</param>
        /// <param name="decimalLen">小数部分保留长度</param>
        /// <returns>缩写后的字符串</returns>
        private static string Inner_FormatNumberAbbreviation(string number, int decimalLen = 1)
        {
            var len = number.Length;
            var integerLen = 0; // 整数部分的长度
            var formatKey = string.Empty; // 使用的字符串键值
            if (len >= 7)
            {
                // 百万级
                integerLen = len - 6;
                formatKey = StrFormatNumberMega;
            }
            else if (len >= 4)
            {
                // 千级
                integerLen = len - 3;
                formatKey = StrFormatNumberKilo;
            }

            // 不需要缩写
            if (integerLen <= 0)
                return number;
            
            var decimalIndex = integerLen; // 小数数字开始位置的索引
            var builder = GetStringBuilder(number);
            builder.Length = integerLen + decimalLen;
            builder.Insert(decimalIndex, '.');
            return GetString(formatKey, builder.ToString());
        }
    }
}