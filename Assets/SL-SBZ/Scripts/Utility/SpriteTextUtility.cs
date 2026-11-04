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
        /// (e.g. 200 -> "<sprite=11><sprite=2><sprite=0><sprite=0>")
        /// (e.g. 1.25 -> "<sprite=11><sprite=1><sprite=10><sprite=2><sprite=5>")
        /// </summary>
        public static string FormatCurrency(double amount)
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
            sb.Append(DollarSprite);

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
    }
}
