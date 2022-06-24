using FuzzyMatching.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FuzzyMatching
{
    public static class FuzzySearch
    {
        private delegate int DistanceFunction(string a, string b);
        private delegate double ScoringFunction(string a, string b, int distance);

        /// <summary>
        /// Nested object to hold the return values of the TokenDistance function
        /// </summary>
        public struct TokenPair
        {
            public string Source { get; set; }
            public string Target { get; set; }
            public int Distance { get; set; }
        }

        /// <summary>
        /// Use a collection of heuristics to find the best matching algorithm for the string search task.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static FuzzyResult SmartSearch(string source, string target, bool preprocess = true)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return FuzzyResult.NoResult();
            }
            else if (source.Length > target.Length)
            {
                return FuzzyResult.NoResult("Source Larger than Target");
            }

            bool isCJK = source.IsCJK() || target.IsCJK();

            if (preprocess)
            {
                source = source.Clean();
                target = target.Clean();
            }
            try
            {
                //バッテリー設定
                //only partial search makes sense here
                if (isCJK)
                {
                    return PartialCJKSearch(source, target);
                }

                //if the source string is tokenizable, we want to search those tokens out of order as well
                //but do not tokenize nor use any token ratio on CJK text
                if ( source.Tokenize().Length > 1)
                {
                    return PartialTokenSetSearch(source, target);
                }
                else
                {
                    if (source.Length < 32)
                    {
                        return Bitap(source, target);
                    }
                    else
                    {
                        //handle bigger source patterns, but slower
                        return PartialSearch(source, target);
                    }
                }
            }
            catch (Exception)
            {
                return FuzzyResult.NoResult();
            }
        }

        /// <summary>
        /// Public API for doing string search with a specific method. CJK not compatible with bitap
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="preprocess"></param>
        /// <returns></returns>
        public static FuzzyResult Search(string source, string target, SearchMethod method = SearchMethod.Bitap, bool preprocess = true)
        {

            if (preprocess)
            {
                source = source.Clean();
                target = target.Clean();
            }

            try
            {
                return method switch
                {
                    SearchMethod.Bitap => Bitap(source, target),
                    SearchMethod.SlidingSearch => SlidingTokenSearch(source, target),
                    SearchMethod.TokenSearch => TokenSearch(source, target),
                    SearchMethod.PartialSearch => PartialSearch(source, target),
                    SearchMethod.PartialCJKSearch => PartialCJKSearch(source, target),
                    _ => Bitap(source, target),
                };
            }
            catch (Exception)
            {
                return FuzzyResult.NoResult();
            }
        }

        /// <summary>
        /// Fuzzy-find substring source in string target. Source should be smaller than target
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult SlidingSearch(string source, string target, bool useTokens = true, bool preprocess = true)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return FuzzyResult.NoResult();
            }
            else if (source.Length > target.Length)
            {
                return FuzzyResult.NoResult();
            }

            if (preprocess)
            {
                source = source.Clean();
                target = target.Clean();
            }

            if (useTokens)
            {
                return SlidingTokenSearch(source, target);
            }
            else
            {
                //here we're just sliding the string through the target
                if (source.Length < 32 && (!source.IsCJK() || !target.IsCJK())) 
                {
                    return Bitap(source, target);
                }
                else
                {
                    //handle bigger source patterns, but slower
                    return PartialSearch(source, target);
                }
            }
        }

        /// <summary>
        /// Fuzzy-find substring source in string target. Source should be smaller than target
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult TokenSearch(string source, string target, bool useSet = true, bool preprocess = true)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return FuzzyResult.NoResult();
            }
            else if (source.Length > target.Length)
            {
                return FuzzyResult.NoResult();
            }

            if (preprocess)
            {
                source = source.Clean();
                target = target.Clean();
            }


            if (useSet)
            {
                return PartialTokenSetSearch(source, target);
            }
            else
            {
                return PartialTokenSearch(source, target);
            }
        }

        /// <summary>
        /// Token search tokenizes the text, sorts the tokens, and finds the best matching token pairs using the Levenshtein distance metric
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult PartialTokenSearch(string source, string target)
        {
            var sourceTokens = source.ToLower().Tokenize();
            var targetTokens = target.ToLower().Tokenize();

            DistanceFunction function = EditDistance.Levenshtein;

            return EvaluateTokenPairs(source, target, MeasureTokenDistance(sourceTokens, targetTokens, function), Scoring.SimpleScore, function.Method?.Name ?? nameof(PartialTokenSearch));
        }

        /// <summary>
        /// Partial token search tokenizes the text, creates a hash set of the tokens to remove redundant words, and finds the best matching token pairs using the Levenshtein distance metric
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult PartialTokenSetSearch(string source, string target)
        {
            var sourceTokens = new HashSet<string>(source.ToLower().Tokenize());
            var targetTokens = new HashSet<string>(target.ToLower().Tokenize());

            DistanceFunction function = EditDistance.Levenshtein;

            return EvaluateTokenPairs(source, target, MeasureTokenDistance(sourceTokens, targetTokens, function), Scoring.SimpleScore, function.Method?.Name ?? nameof(PartialTokenSetSearch));
        }



        /// <summary>
        /// Token search tokenizes the text, sorts the tokens, and finds the best matching token pairs using the Hamming distance metric
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult SlidingTokenSearch(string source, string target)
        {
            var sourceTokens = source.ToLower().Tokenize();
            var targetTokens = target.ToLower().Tokenize();

            DistanceFunction function = EditDistance.PartialTokenAlignment;

            return EvaluateTokenPairs(source, target, MeasureTokenDistance(sourceTokens, targetTokens, function), Scoring.SlidingScore, function.Method?.Name ?? nameof(SlidingTokenSearch));
        }


        /// <summary>
        /// Given a list of token pairs and their distances, find the indices corresponding to the target string matches, and their score
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="bestPairs"></param>
        /// <param name="scorer"></param>
        /// <returns></returns>
        private static FuzzyResult EvaluateTokenPairs(string source, string target, IEnumerable<TokenPair> bestPairs, ScoringFunction scorer, string distanceFunction)
        {
            //Work back through the tokens to find the indices of our target
            int startIdx = int.MaxValue;
            int endIdx = int.MinValue;
            int distance = 0;

            foreach (var p in bestPairs)
            {
                var low = target.IndexOf(p.Target);
                var hi = target.LastIndexOf(p.Target) + (p.Target.Length - 1);

                if (low < startIdx)
                {
                    startIdx = low;
                }

                if (hi > endIdx)
                {
                    endIdx = hi;
                }

                distance += p.Distance;
            }

            var sourceTokensUsed = string.Join(" ", bestPairs.Select(x => x.Source));
            var targetTokensUsed = string.Join(" ", bestPairs.Select(x => x.Target));

            return new FuzzyResult
            {
                DistanceFunction = distanceFunction,
                Start = startIdx,
                End = endIdx,
                Source = source,
                Target = target,
                Metric = distance,
                Ratio = scorer(sourceTokensUsed, targetTokensUsed, distance)
            };
        }
        /// <summary>
        /// Using a distance function, finds the best matches between source and target tokens and returns a list of them.
        /// </summary>
        /// <param name="sourceTokens"></param>
        /// <param name="targetTokens"></param>
        /// <param name="distanceFunction"></param>
        /// <returns></returns>
        private static IEnumerable<TokenPair> MeasureTokenDistance(IEnumerable<string> sourceTokens, IEnumerable<string> targetTokens, DistanceFunction distanceFunction)
        {
            var pairs = new List<TokenPair>();

            //get all distance / token pair values first
            foreach (var s in sourceTokens)
            {
                foreach (var t in targetTokens)
                {
                    var distance = distanceFunction(s, t);
                    pairs.Add(new TokenPair
                    {
                        Source = s,
                        Target = t,
                        Distance = distance
                    });
                }
            }

            //sort them descending by distance
            var sorted = pairs.OrderBy(x => x.Distance);
            var bestPairs = new List<TokenPair>();

            //take the lowest distance unique/distinct pairs (no repetition)
            foreach (var s in sorted)
            {
                if (!bestPairs.Any(x => x.Source == s.Source) && !bestPairs.Any(x => x.Target == s.Target))
                {
                    bestPairs.Add(s);
                }
            }

            return bestPairs;
        }

        /// <summary>
        /// Same as doing Partial ratio, but search nomenclature
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult PartialSearch(string source, string target)
        {
            return FuzzyCompare.Ratio(source, target, RatioMethod.Partial, false);
        }

        /// <summary>
        /// Search that accounts for the index changes in decomposed strings
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult PartialCJKSearch(string source, string target)
        {
            var decompSourceBlocks = HanDecomposition.DecomposeToBlocks(source);
            var decompTargetBlocks = HanDecomposition.DecomposeToBlocks(target);
            var decompSourceMaxSize = decompSourceBlocks.Max(x => x.Length);
            var decompTargetMaxSize = decompTargetBlocks.Max(x => x.Length);
            var maxSize = Math.Max(decompSourceMaxSize, decompTargetMaxSize);

            var decompSource = string.Join("",decompSourceBlocks.Select(x=>x.PadRight(maxSize, '#')));
            var decompTarget = string.Join("", decompTargetBlocks.Select(x => x.PadRight(maxSize, '#')));

            //do not preprocess
            var result = PartialSearch(decompSource, decompTarget);

            //normalize the start and end.
            var start = result.Start / (double)maxSize;
            var end = result.End / (double)maxSize;
            //what we find in the search, now we can restructure the result
            return new FuzzyResult
            {
                Source = source,
                Target = target,
                Start = (int)Math.Floor(start),
                End = (int)Math.Ceiling(end),
                DistanceFunction = result.DistanceFunction,
                Metric = result.Metric,
                Ratio = result.Ratio
            };

        }

        /// <summary>
        /// Modified Bitap (Shift-Or) to do fast fuzzy substring search, pseudo hamming, without having to specify max errors (tracks the best)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static FuzzyResult Bitap(string source, string target)
        {
            int index = -1;
            int sourceLength = source.Length;
            int maxDistance = source.Length; //furthest possible :a completely different string from itself
            int[] bitArray; //hodls the possible prefixes of source
            int[] patternMask = new int[1024];
            int i, d;

            if (string.IsNullOrEmpty(source)) return FuzzyResult.NoResult();
            if (sourceLength > 31)
            {
                throw new ArgumentException("Source string too long, try another method");
            }

            bitArray = new int[(maxDistance + 1) * sizeof(int)];
            for (i = 0; i <= maxDistance; ++i)
                bitArray[i] = ~1;

            for (i = 0; i <= 1023; ++i)
                patternMask[i] = ~0;

            for (i = 0; i < sourceLength; ++i)
            {
                patternMask[source[i]] &= ~(1 << i);
            }

            int best = maxDistance;

            for (i = 0; i < target.Length; ++i)
            {
                int oldRd1 = bitArray[0];

                bitArray[0] |= patternMask[target[i]];
                bitArray[0] <<= 1;

                for (d = 1; d <= maxDistance; ++d)
                {
                    int tmp = bitArray[d];

                    bitArray[d] = (oldRd1 & (bitArray[d] | patternMask[target[i]])) << 1;
                    oldRd1 = tmp;

                    if (0 == (bitArray[d] & (1 << sourceLength)))
                    {
                        if (d <= best)
                        {
                            best = d;
                            index = (i - sourceLength) + 1;

                            if (best == 1 && target.Substring(index, sourceLength) == source)
                            {
                                best = 0;
                            }
                        }
                        //break;
                    }
                }
            }

            return new FuzzyResult
            {
                DistanceFunction = nameof(SearchMethod.Bitap),
                Source = source,
                Target = target,
                Start = index,
                End = index + sourceLength,
                Metric = best,
                Ratio = Scoring.SlidingScore(source, target, best)
            };
        }

    }

}

