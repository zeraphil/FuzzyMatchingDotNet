using FuzzyMatching.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FuzzyMatching
{
    public static class FuzzyCompare
    {
        /// <summary>
        /// Based on the properties of the string, selects the best combination of ratio operations
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static FuzzyResult SmartRatio(string source, string target, bool preprocess = true)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return FuzzyResult.NoResult();
            }

            //get some information about the strings: # tokens and their lengths
            if (preprocess)
            {
                source = source.Clean();
                target = target.Clean();
            }

            var sourceTokens = source.ToLower().Tokenize();
            var targetTokens = target.ToLower().Tokenize();
            var sourceLength = source.Length;
            var targetLength = target.Length;
            var avgSourceTokenLength = sourceTokens.Average(x => x.Length);
            var avgTargetTokenLength = targetTokens.Average(x => x.Length);

            var lengthRatio = (double)Math.Max(sourceLength, targetLength) / Math.Min(sourceLength, targetLength);
            var avgTokenRatio = (double)Math.Max(sourceTokens.Count(), targetTokens.Count()) / Math.Min(sourceTokens.Count(), targetTokens.Count());

            try
            {
                if (sourceTokens == targetTokens)
                {
                    if (source.Length == target.Length)
                    {
                        return HammingRatio(source, target);
                    }
                    else if (lengthRatio < 1.5)
                    {
                        return LevenshteinRatio(source, target);
                    }
                    else
                    {
                        return PartialHamming(source, target);
                    }
                }
                else
                {
                    if (lengthRatio < 1.5)
                    {
                        return TokenSortRatio(source, target);
                    }
                    else
                    {
                        return PartialTokenSortRatio(source, target);
                    }
                }
            }
            catch (Exception)
            {
                return FuzzyResult.NoResult();
            }
        }

        /// <summary>
        /// Public API for comparing strings with a specific method
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static FuzzyResult Ratio(string source, string target, RatioMethod method = RatioMethod.Levenshtein, bool preprocess = true)
        {
            if (preprocess)
            {
                source = source.Clean();
                target = target.Clean();

                if (source.IsCJK() || target.IsCJK())
                {
                    source = HanDecomposition.Decompose(source);
                    target = HanDecomposition.Decompose(target);
                }

            }

            return method switch
            {
                RatioMethod.Hamming => HammingRatio(source, target),
                RatioMethod.Partial => PartialHamming(source, target),
                RatioMethod.Jaccard => JaccardRatio(source, target),
                RatioMethod.Levenshtein => LevenshteinRatio(source, target),
                RatioMethod.OSA => OSARatio(source, target),
                RatioMethod.PartialTokenSet => PartialTokenSetRatio(source, target),
                RatioMethod.PartialTokenSort => PartialTokenSortRatio(source, target),
                RatioMethod.TokenSet => TokenSetRatio(source, target),
                RatioMethod.TokenSort => TokenSetRatio(source, target),
                _ => LevenshteinRatio(source, target),
            };
        }

        /// <summary>
        /// Simple Hamming matching strategy
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult HammingRatio(string source, string target)
        {
            var distance = EditDistance.Hamming(source, target);

            return new FuzzyResult
            {
                DistanceFunction = nameof(EditDistance.Hamming),
                Metric = distance,
                Start = 0,
                End = target.Length,
                Ratio = Scoring.SimpleScore(source, target, distance),
                Source = source,
                Target = target
            };
        }

        /// <summary>
        /// Simple Hamming matching strategy
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult JaccardRatio(string source, string target)
        {
            var ratio = EditDistance.JaccardCoefficient(source, target);

            return new FuzzyResult
            {
                DistanceFunction = nameof(EditDistance.JaccardCoefficient),
                Source = source,
                Target = target,
                Metric = 1 - ratio,
                Start = 0,
                End = target.Length,
                Ratio = ratio //Jacard ratio is the coefficient... lossy operation
            };
        }

        /// <summary>
        /// Simple Levenshteing matching strategy
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult LevenshteinRatio(string source, string target)
        {
            var distance = EditDistance.Levenshtein(source, target);

            return new FuzzyResult
            {
                DistanceFunction = nameof(EditDistance.Levenshtein),
                Source = source,
                Target = target,
                Metric = distance,
                Start = 0,
                End = target.Length,
                Ratio = Scoring.SimpleScore(source, target, distance)
            };
        }


        /// <summary>
        /// Damerau-Levenshtein, or OptimalStringAlignment, matching strategy
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult OSARatio(string source, string target)
        {
            var distance = EditDistance.OptimalStringAlignment(source, target);

            return new FuzzyResult
            {
                DistanceFunction = nameof(EditDistance.OptimalStringAlignment),
                Source = source,
                Target = target,
                Metric = distance,
                Start = 0,
                End = target.Length,
                Ratio = Scoring.SimpleScore(source, target, distance)
            };
        }

        /// <summary>
        /// Sliding partial strategy, naive implementation of Karp–Rabin algorithm, without the hashing
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult PartialHamming(string source, string target)
        {

            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                var wrong = Math.Max(source?.Length ?? 0, target?.Length ?? 0);
                return FuzzyResult.NoResult();
            }

            string small = source.Length <= target.Length ? source : target;
            string big = source.Length <= target.Length ? target : source;

            var window = small.Length;
            int offset = 0;

            int lowestDistance = int.MaxValue;
            int startIndex = int.MinValue;
            while (window + offset <= big.Length)
            {
                var hamming = EditDistance.Hamming(small, big.Substring(offset, window));
                if (hamming < lowestDistance)
                {
                    lowestDistance = hamming;
                    startIndex = offset;
                }

                offset++;
            }

            var start = source == small ? startIndex : 0;
            var end = source == small ? startIndex + small.Length : target.Length;

            return new FuzzyResult
            {
                DistanceFunction = nameof(EditDistance.Hamming),
                Metric = lowestDistance,
                Source = source,
                Target = target,
                Start = start,
                End = end,
                Ratio = Scoring.SlidingScore(small, big, lowestDistance)
            };
        }

        /// <summary>
        /// Token sort tokenizes the text, sorts the tokens, joins them, and finds the ratio using the Levenshtein distance metric
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult TokenSortRatio(string source, string target)
        {
            var sourceTokens = source.ToLower().Tokenize().OrderBy(x => x);
            var targetTokens = target.ToLower().Tokenize().OrderBy(x => x);
            return LevenshteinRatio(string.Join(" ", sourceTokens), string.Join(" ", targetTokens));
        }

        /// <summary>
        /// Token set tokenizes the text, creates a hash set of the tokens to remove redundant words, joins them and finds the ratio using the Levenshtein distance metric
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult TokenSetRatio(string source, string target)
        {
            var sourceTokens = new HashSet<string>(source.ToLower().Tokenize());
            var targetTokens = new HashSet<string>(target.ToLower().Tokenize());
            return LevenshteinRatio(string.Join(" ", sourceTokens), string.Join(" ", targetTokens));
        }

        /// <summary>
        /// Token sort tokenizes the text, sorts the tokens, joins them, and finds the ratio using the sliding Hamming distance metric
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult PartialTokenSortRatio(string source, string target)
        {
            var sourceTokens = source.ToLower().Tokenize().OrderBy(x => x);
            var targetTokens = target.ToLower().Tokenize().OrderBy(x => x);
            return PartialHamming(string.Join(" ", sourceTokens), string.Join(" ", targetTokens));
        }

        /// <summary>
        /// Token set tokenizes the text, creates a hash set of the tokens to remove redundant words, joins them and finds the ratio using the sliding Hamming distance metric
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult PartialTokenSetRatio(string source, string target)
        {
            var sourceTokens = new HashSet<string>(source.ToLower().Tokenize());
            var targetTokens = new HashSet<string>(target.ToLower().Tokenize());
            return PartialHamming(string.Join(" ", sourceTokens), string.Join(" ", targetTokens));
        }

    }
}
