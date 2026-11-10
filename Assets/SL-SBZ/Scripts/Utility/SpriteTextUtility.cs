using System;
using System.Text;
using System.Globalization;
using TMPro;

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
        public static string FormatFloatingWin(double amount, bool includeDollar = false, bool forceTwoDecimals = true)
        {
            return FormatSpriteAmount(amount, "Sweet Bonanza_night slot panel numbers", forceTwoDecimals, includeDollar);
        }

        /// <summary>
        /// Formats floating cluster win text, dynamically using targetText's assigned spriteAsset if available.
        /// </summary>
        public static string FormatFloatingWin(double amount, TMP_Text targetText, bool forceTwoDecimals = true)
        {
            return FormatWithSpriteAsset(amount, targetText, includeDollar: false, forceTwoDecimals: forceTwoDecimals);
        }

        /// <summary>
        /// Dynamically formats an amount into sprite tags using whichever TMP_SpriteAsset is assigned to targetText in the Inspector.
        /// Falls back to standard numeric string if targetText or its spriteAsset is null.
        /// </summary>
        public static string FormatWithSpriteAsset(double amount, TMP_Text targetText, bool includeDollar = false, bool forceTwoDecimals = true)
        {
            if (targetText != null && targetText.spriteAsset != null)
            {
                return FormatSpriteAmount(amount, targetText.spriteAsset.name, forceTwoDecimals, includeDollar);
            }

            if (amount < 0) amount = 0;
            string prefix = includeDollar ? "$" : "";
            string num = forceTwoDecimals
                ? amount.ToString("0.00", CultureInfo.InvariantCulture)
                : ((Math.Abs(amount - Math.Round(amount)) < 0.001)
                    ? ((long)Math.Round(amount)).ToString(CultureInfo.InvariantCulture)
                    : amount.ToString("0.00", CultureInfo.InvariantCulture));
            return prefix + num;
        }

        /// <summary>
        /// Dynamically formats an integer count into sprite tags using whichever TMP_SpriteAsset is assigned to targetText in the Inspector.
        /// Falls back to standard integer string if targetText or its spriteAsset is null.
        /// </summary>
        public static string FormatCountWithSpriteAsset(int count, TMP_Text targetText)
        {
            if (targetText != null && targetText.spriteAsset != null)
            {
                return FormatSpriteAmount(count, targetText.spriteAsset.name, forceTwoDecimals: false, includeDollar: false);
            }

            if (count < 0) count = 0;
            return count.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats an amount into sprite tags using the specified sprite asset prefix (e.g. "Sweet Bonanza_bomb numbers" or "Sweet Bonanza_night slot panel numbers").
        /// </summary>
        public static string FormatSpriteAmount(double amount, string prefix = "Sweet Bonanza_bomb numbers", bool forceTwoDecimals = true, bool includeDollar = false)
        {
            if (amount < 0) amount = 0;

            string formatted = forceTwoDecimals
                ? amount.ToString("0.00", CultureInfo.InvariantCulture)
                : ((Math.Abs(amount - Math.Round(amount)) < 0.001)
                    ? ((long)Math.Round(amount)).ToString(CultureInfo.InvariantCulture)
                    : amount.ToString("0.00", CultureInfo.InvariantCulture));

            StringBuilder sb = new StringBuilder(128);
            if (includeDollar)
            {
                sb.Append("<sprite name=\"").Append(prefix).Append("_11\">");
            }

            for (int i = 0; i < formatted.Length; i++)
            {
                char c = formatted[i];
                if (c >= '0' && c <= '9')
                {
                    sb.Append("<sprite name=\"").Append(prefix).Append('_').Append(c).Append("\">");
                }
                else if (c == '.')
                {
                    sb.Append("<sprite name=\"").Append(prefix).Append("_10\">");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Formats top tumble win text, automatically adapting to whichever sprite asset is assigned to the target text.
        /// </summary>
        public static string FormatTopTumbleWin(double amount, TMP_Text targetText = null, bool forceTwoDecimals = true)
        {
            string prefix = (targetText != null && targetText.spriteAsset != null)
                ? targetText.spriteAsset.name
                : "Sweet Bonanza_bomb numbers";
            return FormatSpriteAmount(amount, prefix, forceTwoDecimals);
        }

        /// <summary>
        /// Formats formula display: "[BaseWin] x [RunningMultiplier]" (e.g. "5.00 x 30").
        /// </summary>
        public static string FormatTopTumbleWinFormula(double baseWin, int runningMultiplier, TMP_Text targetText = null)
        {
            string winStr = FormatTopTumbleWin(baseWin, targetText, forceTwoDecimals: true);
            string prefix = (targetText != null && targetText.spriteAsset != null)
                ? targetText.spriteAsset.name
                : "Sweet Bonanza_bomb numbers";
            string multDigits = FormatSpriteAmount(runningMultiplier, prefix, forceTwoDecimals: false);
            return $"{winStr} <sprite name=\"x\"> {multDigits}";
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
            sb.Append("<sprite name=\"x\">");
            return sb.ToString();
        }
    }
}
