namespace FuzzyMatching
{
    public struct FuzzyResult
    {
        /// <summary>
        /// Distance function used
        /// </summary>
        public string DistanceFunction { get; set; }

        /// <summary>
        /// The ratio score of the string match
        /// </summary>
        public double Ratio { get; set; }

        /// <summary>
        /// The edit distance or similarity metric
        /// </summary>
        public int Metric { get; set; }

        /// <summary>
        /// The lowest index of the Fuzzy match. This will be the 0 if not using token sort.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The highest index of the Fuzzy match. This will be the target string length if not using token sort.
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// The target string to match
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// The source match string
        /// </summary>
        public string Source { get; set; }


        public static FuzzyResult NoResult(string message = "Failure")
        {
            return new FuzzyResult
            {
                DistanceFunction = message,
                Ratio = 0,
                Metric = int.MaxValue,
                Start = 0,
                End = 0,
            };
        }
    }

}
