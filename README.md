# FuzzyMatchingDotNet
C# .NET Standard library for string fuzzy matching principally for Roman scripts, but also supports Chinese and Japanese fuzzy string matching through Han decomposition.

FuzzyMathcing library has two modes of operation: Fuzzy String matching and fuzzy string search, using  multiple user specified edit distance methods (Hamming, Levenshtein, Jacquard, Bitap). The first will return a result object with the ratio and edit distance scores of the fuzzy matching result, which you can use in tokenization schemes, user input or other fuzzy matching applications. The second will search for a string in a fuzzy manner, akin to Regex search but with multiple configurable modes of operation. Refer to the tests for more details.