using System;

namespace FuzzyMatching
{
    static class Scoring
    {
        public static double SimpleScore(string a, string b, int distance)
        {
            var sum = a.Length + b.Length;

            if (sum <= 0) return 0;

            return ((sum - distance) / (double)sum) * 100;
        }

        public static double PartialScore(string a, string b, int partialDistance)
        {
            var sum = a.Length + b.Length;
            var delta = Math.Max(a.Length, b.Length) - Math.Min(a.Length, b.Length);

            if (sum <= 0) return 0;

            return ((sum - partialDistance + delta) / (double)sum) * 100;
        }

        public static double SlidingScore(string small, string big, int distance)
        {
            var sum = small.Length * 2;

            if (sum <= 0) return 0;

            return ((sum - distance) / (double)sum) * 100;
        }

    }
}
