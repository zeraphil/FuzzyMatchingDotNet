using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FuzzyMatching.Extensions
{
    public static class StringExtensions
    {
        private static readonly char[] delimeters = new char[] { ' ', '-' }; // TODO: Are there other delimiters we should use? Japanese has something like a bullet

        /// <summary>
        /// Simple tokenizer method for splitting strings
        /// </summary>
        /// <remarks>
        /// Note: This does not support CJK tokenization
        /// </remarks>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string[] Tokenize(this string str)
        {
            return str.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Remove punctuation etc
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Clean(this string str)
        {
            return Regex.Replace(str, @"[\s\p{P}]+", " ").Trim();
        }

        /// <summary>
        /// Method to break down strings into their constituted nGrams. Ngrams are symmetric (only one size)
        /// </summary>
        /// <param name="str"></param>
        /// <param name="nGramSize"></param>
        /// <returns></returns>
        public static IEnumerable<string> ToNGrams(this string str, int nGramSize = 2)
        {
            if (nGramSize >= str.Length)
            {
                return new string[] { str };
            }

            var target = str.Clean();

            var nGrams = new List<string>();
            int offset = 0;
            while (nGramSize + offset <= str.Length)
            {
                nGrams.Add(str.Substring(offset, nGramSize).Trim());
                offset++;
            }

            return nGrams;
        }

        public static IEnumerable<string> ToTokenNGrams(this string str, int nGramSize = 2)
        {
            var punctFreeInput = str.Clean();
            string[] tokens = punctFreeInput.Tokenize();
            if (nGramSize >= tokens.Length)
            {
                return new string[] { punctFreeInput };
            }

            var nGrams = new List<string>();
            for (int tokenIndex = 0; tokenIndex + nGramSize <= tokens.Length; tokenIndex++)
            {
                nGrams.Add(string.Join(" ", tokens.Skip(tokenIndex).Take(nGramSize)));
            }
            return nGrams;
        }

        public static IEnumerable<string> ShingleFilter(this string str, int minShingleSize = 1, int maxShingleSize = 0)
        {
            var punctFreeInput = str.Trim().Clean();
            string[] tokens = punctFreeInput.Tokenize();

            // Max shingle size can be function of number of tokens
            // 0 = all tokens
            // -1 = all tokens - 1
            // etc.
            if (maxShingleSize <= 0)
            {
                maxShingleSize = tokens.Length + maxShingleSize;
            }

            // Boundary check the arguments...
            if (minShingleSize <= 0)
            {
                minShingleSize = 1; // TODO: Or should this be error?
            }
            if (maxShingleSize < minShingleSize)
            {
                maxShingleSize = minShingleSize; // TODO: Or should this be error?
            }

            var shingles = new List<string>();
            for (int n = minShingleSize; n <= maxShingleSize; n++)
            {
                shingles.AddRange(punctFreeInput.ToTokenNGrams(n));
            }
            return shingles;
        }

        /// <summary>
        /// Method to turn char list into UTF32 codepoints
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static int[] ToCodePoints(this string str)
        {
            if (str == null)
                return null;

            var codePoints = new List<int>(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                codePoints.Add(char.ConvertToUtf32(str, i));
                if (char.IsHighSurrogate(str[i]))
                    i += 1;
            }

            return codePoints.ToArray();
        }

        /// <summary>
        /// Return true if the any of the characters used in the text belong to Chinese/Japanese/Korean Unicode blocks
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsCJK(this string text)
        {
            var codepoints = text?.ToCodePoints();

            if (codepoints == null)
            {
                return false;
            }

            //Refer to https://en.wikipedia.org/wiki/CJK_Unified_Ideographs#Other_CJK_Ideographs_in_Unicode,_not_Unified

            bool inRange = codepoints.Any(c =>
            (c >= 0x2E80 && c <= 0xFAFF) || //CJK Radical Supplements to CJK Compatibility Ideographs
            (c >= 0xFE30 && c <= 0xFE4F) || //CJK Compatibility Forms
            (c >= 0xFF00 && c <= 0xFFEF) || //Japanese halfwidth forms
            (c >= 0x20000 && c <= 0x2FA1F)); //CJKExtensions and supplements

            return inRange;
        }

    }
}
