using FuzzyMatching.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FuzzyMatching
{
    /// <summary>
    /// Method for determining the string matching by edit distance
    /// </summary>
    static class EditDistance
    {

        /// <summary>
        /// Short min function
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <param name="e3"></param>
        /// <returns></returns>
        private static int Min(int e1, int e2, int e3)
        {
            return Math.Min(Math.Min(e1, e2), e3);
        }


        /// <summary>
        /// Implementation of the hamming distance. Equal length string comparison.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int Hamming(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return Math.Max(source?.Length ?? 0, target?.Length ?? 0);
            }

            if (source.Length != target.Length)
            {
                throw new ArgumentOutOfRangeException("Strings must be equal length");
            }

            //since strings assured same length, this unsafe operation is ok

            var distance = 0;

            for (int i = 0; i < target.Length; ++i)
            {
                if (source[i] != target[i])
                {
                    distance++;
                }
            }

            return distance;
        }

        /// <summary>
        /// The measure of the similarity between two sets, here taken as the bigram sets in a string.
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int JaccardCoefficient(string source, string target)
        {
            var a = new HashSet<string>(source.ToNGrams());
            var b = new HashSet<string>(target.ToNGrams());

            var jaccardCoefficient = a.Intersect(b).Count() / (double)(a.Union(b).Count());

            //the distance 
            return (int)(jaccardCoefficient * 100);

        }

        /// <summary>
        /// RatcliffObershelp or gestalt pattern matching, Measure of similarity of character sets as the intersection of the sum of the string length
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static double RatcliffObershelp(string source, string target)
        {
            return 2 * ((double)source.Intersect(target).Count()) / (source.Length + target.Length);
        }

        /// <summary>
        /// Restricted Damerau-Levenshtein alignment
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int OptimalStringAlignment(string source, string target)
        {
            return DamerauLevenshtein(source, target, true);
        }

        /// <summary>
        /// Levenshteing edit distance, or Damerau-Levenshtein without transpositions
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int Levenshtein(string source, string target)
        {
            return DamerauLevenshtein(source, target, false);
        }

        /// <summary>
        /// The distance from one token to another, given to by how well a smaller token "fits".
        /// Essentially, the Hamming distance but padded to allow for addition/insertions at zero cost
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int PartialTokenAlignment(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return Math.Max(source?.Length ?? 0, target?.Length ?? 0);
            }

            string small = source.Length <= target.Length ? source : target;
            string big = source.Length <= target.Length ? target : source;

            int bestScore = int.MaxValue;

            for (int j = 0; j <= big.Length - small.Length; ++j)
            {
                var score = Hamming(big.Substring(j, small.Length), small);

                if (score < bestScore)
                {
                    bestScore = score;
                }
            }

            return bestScore;
        }

        /// <summary>
        /// Damerau-Levenshtein distance method, now with optional "damerau"
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int DamerauLevenshtein(string source, string target, bool damerau = false)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return Math.Max(source?.Length ?? 0, target?.Length ?? 0);
            }

            if (!damerau)
            {
                return OptimizedLevenshtein(source, target);
            }

            var sourceSize = source.Length + 1;
            var targetSize = target.Length + 1;
            var cost_matrix = new int[sourceSize, targetSize];
            for (var x = 0; x < sourceSize; x++) cost_matrix[x, 0] = x;
            for (var y = 0; y < targetSize; y++) cost_matrix[0, y] = y;

            for (int x = 1; x <= source.Length; x++)
            {
                var previousX = x - 1;
                for (int y = 1; y <= target.Length; y++)
                {
                    var previousY = y - 1;
                    var cost = source[previousX] == target[previousY] ? 0 : 1;
                    cost_matrix[x, y] = Min(
                        cost_matrix[x - 1, y] + 1, // deletion
                        cost_matrix[x, y - 1] + 1, // insertion
                        cost_matrix[x - 1, y - 1] + cost); // substitution

                    //the Damerau-Levenshtein step
                    if (damerau)
                    {
                        if (x > 1 &&
                            y > 1 &&
                            source[previousX] == target[previousY - 1] &&
                            source[previousX - 1] == target[previousY])
                        {
                            cost_matrix[x, y] = Math.Min(
                                cost_matrix[x, y],
                                cost_matrix[x - 2, y - 2] + cost); // transposition
                        }
                    }
                }
            }

            var distance = cost_matrix[sourceSize - 1, targetSize - 1];
            return distance;
        }

        /// <summary>
        /// Computes the distance between string pairs, without requiring an n*m matrix
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int OptimizedLevenshtein(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return Math.Max(source?.Length ?? 0, target?.Length ?? 0);
            }

            var current = 1;
            var previous = 0;
            var cost_matrix = new int[2, target.Length + 1];
            for (int i = 0; i <= target.Length; i++)
            {
                cost_matrix[previous, i] = i;
            }

            for (int i = 0; i < source.Length; i++)
            {
                cost_matrix[current, 0] = i + 1;

                for (int j = 1; j <= target.Length; j++)
                {
                    var cost = (target[j - 1] == source[i]) ? 0 : 1;
                    cost_matrix[current, j] = Min(cost_matrix[previous, j] + 1, cost_matrix[current, j - 1] + 1, cost_matrix[previous, j - 1] + cost);
                }

                previous = (previous + 1) % 2;
                current = (current + 1) % 2;
            }
            return cost_matrix[previous, target.Length];
        }



    }
}
