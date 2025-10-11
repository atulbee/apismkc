using System;
using System.Text;

namespace SmkcApi.Utils
{
    public static class LegacyIndicConverter
    {
        // .NET code page for ISCII Devanagari is 57002
        private static readonly Encoding IsciiDevanagari = Encoding.GetEncoding(57002);

        // Latin-1 (28591) gives a 1:1 char->byte mapping, preserving 0x00-0xFF values.
        private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);
        private static readonly Encoding Win1252 = Encoding.GetEncoding(1252);

        /// <summary>
        /// Try to recover Unicode Devanagari (Marathi) from legacy ISM/ISCII mojibake like "¯ÖÏê´Ö».
        /// Strategy: take the mojibake string's bytes (Latin-1 or 1252), then decode as ISCII Devanagari.
        /// Picks the version that yields the most Devanagari codepoints.
        /// </summary>
        public static string ToUnicodeMarathi(string mojibake)
        {
            if (string.IsNullOrEmpty(mojibake)) return mojibake;

            // Attempt 1: Latin-1 bytes -> ISCII Devanagari
            var b1 = Latin1.GetBytes(mojibake);
            var u1 = IsciiDevanagari.GetString(b1);

            // Attempt 2: Windows-1252 bytes -> ISCII Devanagari
            var b2 = Win1252.GetBytes(mojibake);
            var u2 = IsciiDevanagari.GetString(b2);

            // Heuristic: choose the result with more Devanagari code points (U+0900–U+097F)
            int score1 = CountDevanagari(u1);
            int score2 = CountDevanagari(u2);

            return score2 > score1 ? u2 : u1;
        }

        private static int CountDevanagari(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int cnt = 0;
            foreach (var ch in s)
            {
                if (ch >= '\u0900' && ch <= '\u097F') cnt++;
            }
            return cnt;
        }
    }
}
