using System;
using System.Text;
using System.Globalization;

namespace SweetBonanza.View
{
    public static class SpriteTextUtility
    {
        private const string DollarSprite = "<sprite=11>";
        private const string DotSprite = "<sprite=10>";

        /// <summary>
        /// Formats a currency amount into Sweet Bonanza TMP sprite tags.
        /// (e.g. 200, includeDollar=true -> "<sprite=11><sprite=2><sprite=0><sprite=0>")
        /// (e.g. 200, includeDollar=false -> "<sprite=2><sprite=0><sprite=0>")
        /// </summary>
        public static string FormatCurrency(double amount, bool includeDollar = false)
        {
            if (amount < 0) amount = 0;

            string formatted;
            if (Math.Abs(amount - Math.Round(amount)) < 0.001)
            {
                formatted = ((long)Math.Round(amount)).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                formatted = amount.ToString("0.00", CultureInfo.InvariantCulture);
            }

            StringBuilder sb = new StringBuilder(64);
            if (includeDollar)
            {
                sb.Append(DollarSprite);
            }

            for (int i = 0; i < formatted.Length; i++)
            {
                char c = formatted[i];
                if (c >= '0' && c <= '9')
                {
                    sb.Append("<sprite=").Append(c - '0').Append('>');
                }
                else if (c == '.')
                {
                    sb.Append(DotSprite);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats an integer count into Sweet Bonanza TMP sprite tags.
        /// (e.g. 10 -> "<sprite=1><sprite=0>")
        /// </summary>
        public static string FormatCount(int count)
        {
            if (count < 0) count = 0;
            string s = count.ToString(CultureInfo.InvariantCulture);

            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= '0' && c <= '9')
                {
                    sb.Append("<sprite=").Append(c - '0').Append('>');
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats floating cluster win text using "Sweet Bonanza_night slot panel numbers" sprite tags.
        /// </summary>
        public static string FormatFloatingWin(double amount, bool includeDollar = false)
        {
            if (amount < 0) amount = 0;

            string formatted = (Math.Abs(amount - Math.Round(amount)) < 0.001)
                ? ((long)Math.Round(amount)).ToString(CultureInfo.InvariantCulture)
                : amount.ToString("0.00", CultureInfo.InvariantCulture);

            StringBuilder sb = new StringBuilder(128);
            if (includeDollar)
            {
                sb.Append("<sprite name=\"Sweet Bonanza_night slot panel numbers_11\">");
            }

            for (int i = 0; i < formatted.Length; i++)
            {
                char c = formatted[i];
                if (c >= '0' && c <= '9')
                {
                    sb.Append("<sprite name=\"Sweet Bonanza_night slot panel numbers_").Append(c).Append("\">");
                }
                else if (c == '.')
                {
                    sb.Append("<sprite name=\"Sweet Bonanza_night slot panel numbers_10\">");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats bomb multiplier values into "Sweet Bonanza_bomb numbers" sprite tags.
        /// (e.g. 2 -> "<sprite name=\"Sweet Bonanza_bomb numbers_2\">")
        /// (e.g. 50 -> "<sprite name=\"Sweet Bonanza_bomb numbers_5\"><sprite name=\"Sweet Bonanza_bomb numbers_0\">")
        /// </summary>
        public static string FormatBombMultiplier(int multiplier)
        {
            if (multiplier <= 0) return string.Empty;

            string s = multiplier.ToString(CultureInfo.InvariantCulture);
            StringBuilder sb = new StringBuilder(64);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= '0' && c <= '9')
                {
                    sb.Append("<sprite name=\"Sweet Bonanza_bomb numbers_").Append(c).Append("\">");
                }
            }
            return sb.ToString();
        }
    }
}
