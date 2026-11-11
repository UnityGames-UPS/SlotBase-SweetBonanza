using System;
using System.Text;
using System.Globalization;
using TMPro;

namespace SweetBonanza.View
{
    /// <summary>
    /// Formats numbers and currencies into TextMeshPro sprite tags.
    /// Sweet Bonanza TMP Sprite Assets map indices 0..9 to digits 0..9, index 10 to '.', and index 11 to '$'.
    /// </summary>
    internal static class SpriteTextUtility
    {
        private const string DollarSprite = "<sprite=11>";
        private const string DotSprite = "<sprite=10>";

        /// <summary>
        /// Formats an amount into TMP sprite index tags (<sprite=0>..<sprite=11>).
        /// If targetText has no spriteAsset assigned, falls back to plain numeric string.
        /// </summary>
        internal static string FormatWithSpriteAsset(double amount, TMP_Text targetText, bool includeDollar = false, bool forceTwoDecimals = true)
        {
            if (targetText != null && targetText.spriteAsset == null)
            {
                if (amount < 0) amount = 0;
                string prefix = includeDollar ? "$" : "";
                string num = forceTwoDecimals
                    ? amount.ToString("0.00", CultureInfo.InvariantCulture)
                    : ((Math.Abs(amount - Math.Round(amount)) < 0.001)
                        ? ((long)Math.Round(amount)).ToString(CultureInfo.InvariantCulture)
                        : amount.ToString("0.00", CultureInfo.InvariantCulture));
                return prefix + num;
            }

            return FormatSpriteAmount(amount, null, forceTwoDecimals, includeDollar);
        }

        /// <summary>
        /// Formats an integer count into TMP sprite index tags (<sprite=0>..<sprite=9>).
        /// If targetText has no spriteAsset assigned, falls back to plain integer string.
        /// </summary>
        internal static string FormatCountWithSpriteAsset(int count, TMP_Text targetText)
        {
            if (targetText != null && targetText.spriteAsset == null)
            {
                if (count < 0) count = 0;
                return count.ToString(CultureInfo.InvariantCulture);
            }

            return FormatCount(count);
        }

        /// <summary>
        /// Formats a currency amount into Sweet Bonanza TMP sprite tags.
        /// (e.g. 200, includeDollar=true -> "<sprite=11><sprite=2><sprite=0><sprite=0>")
        /// </summary>
        internal static string FormatCurrency(double amount, bool includeDollar = false)
        {
            return FormatSpriteAmount(amount, null, forceTwoDecimals: false, includeDollar: includeDollar);
        }

        /// <summary>
        /// Formats an integer count into Sweet Bonanza TMP sprite tags.
        /// (e.g. 10 -> "<sprite=1><sprite=0>")
        /// </summary>
        internal static string FormatCount(int count)
        {
            if (count < 0) count = 0;
            string s = count.ToString(CultureInfo.InvariantCulture);

            StringBuilder sb = new StringBuilder(s.Length * 10);
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
        /// Formats an amount into sprite index tags.
        /// Prefix parameter is preserved for backwards compatibility with existing call sites.
        /// </summary>
        internal static string FormatSpriteAmount(double amount, string prefix = null, bool forceTwoDecimals = true, bool includeDollar = false)
        {
            if (amount < 0) amount = 0;

            string formatted = forceTwoDecimals
                ? amount.ToString("0.00", CultureInfo.InvariantCulture)
                : ((Math.Abs(amount - Math.Round(amount)) < 0.001)
                    ? ((long)Math.Round(amount)).ToString(CultureInfo.InvariantCulture)
                    : amount.ToString("0.00", CultureInfo.InvariantCulture));

            StringBuilder sb = new StringBuilder(formatted.Length * 10 + 12);
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
        /// Formats floating cluster win text.
        /// </summary>
        internal static string FormatFloatingWin(double amount, bool includeDollar = false, bool forceTwoDecimals = true)
        {
            return FormatSpriteAmount(amount, null, forceTwoDecimals, includeDollar);
        }

        /// <summary>
        /// Formats floating cluster win text, dynamically checking targetText.
        /// </summary>
        internal static string FormatFloatingWin(double amount, TMP_Text targetText, bool forceTwoDecimals = true)
        {
            return FormatWithSpriteAsset(amount, targetText, includeDollar: false, forceTwoDecimals: forceTwoDecimals);
        }

        /// <summary>
        /// Formats top tumble win text.
        /// </summary>
        internal static string FormatTopTumbleWin(double amount, TMP_Text targetText = null, bool forceTwoDecimals = true)
        {
            return FormatWithSpriteAsset(amount, targetText, includeDollar: false, forceTwoDecimals: forceTwoDecimals);
        }

        /// <summary>
        /// Formats formula display: "[BaseWin] x [RunningMultiplier]" (e.g. "5.00 x 30").
        /// </summary>
        internal static string FormatTopTumbleWinFormula(double baseWin, int runningMultiplier, TMP_Text targetText = null)
        {
            string winStr = FormatTopTumbleWin(baseWin, targetText, forceTwoDecimals: true);
            string multDigits = FormatCountWithSpriteAsset(runningMultiplier, targetText);
            return $"{winStr} <sprite name=\"x\"> {multDigits}";
        }

        /// <summary>
        /// Formats bomb multiplier values into sprite tags with an "x" suffix.
        /// (e.g. 2 -> "<sprite=2><sprite name=\"x\">")
        /// (e.g. 50 -> "<sprite=5><sprite=0><sprite name=\"x\">")
        /// </summary>
        internal static string FormatBombMultiplier(int multiplier)
        {
            if (multiplier <= 0) return string.Empty;

            string s = multiplier.ToString(CultureInfo.InvariantCulture);
            StringBuilder sb = new StringBuilder(s.Length * 10 + 16);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= '0' && c <= '9')
                {
                    sb.Append("<sprite=").Append(c - '0').Append('>');
                }
            }
            sb.Append("<sprite name=\"x\">");
            return sb.ToString();
        }
    }
}
