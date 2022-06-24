using System.ComponentModel;

namespace FuzzyMatching
{
    public enum RatioMethod
    {
        [Description("Hamming Distance considers subsitutions but not additions or deletions")]
        Hamming,
        [Description("Partial Hamming Distance considers subsitutions but not additions or deletions on the best matching substring")]
        Partial,
        [Description("Levenshtein Distance considers edit distance as additions, deletions, and substitutions")]
        Levenshtein,
        [Description("OSA, or Damerau-Levenshteing, considers edit distance as additions, deletions, substitutions and proximal transpositions")]
        OSA,
        [Description("Jaccard considers the general set similarity of string bigrams")]
        Jaccard,
        [Description("Token Sort Ratio sorts tokens in order of size and applies the OSA algorithm")]
        TokenSort,
        [Description("Token Set Ratio creates a unique set, then sorts tokens in order of size and applies the OSA algorithm")]
        TokenSet,
        [Description("Token Sort Ratio sorts tokens in order of size and applies the Sliding Hamming algorithm")]
        PartialTokenSort,
        [Description("Token Sett Ratio creates a unique set, sorts tokens in order of size and applies the Sliding Hamming algorithm")]
        PartialTokenSet
    }

    public enum SearchMethod
    {
        [Description("Fast fuzzy best guess substring search")]
        Bitap,
        [Description("Searches individual tokens along the target string and returns the smallest target substring")]
        TokenSearch,
        [Description("Selects the best fit search algorithm")]
        SlidingSearch,
        [Description("Use the PartialSearch algorithm")]
        PartialSearch,
        [Description("Use the PartialSearch CJK algorithm")]
        PartialCJKSearch,

    }
}
