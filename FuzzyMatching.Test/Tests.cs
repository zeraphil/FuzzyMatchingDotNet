using FuzzyMatching.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace FuzzyMatching.Testing
{
    public class Tests
    {
        private readonly ITestOutputHelper output;

        private readonly string s4;
        private readonly string s5;

        public Tests(ITestOutputHelper output)
        {
            this.s4 = "this is a test tests are the best";
            this.s5 = "this is a test tests are the dest";

            this.output = output;
        }

        [Fact]
        void CJKSearch()
        {
            var va = FuzzySearch.Search("投影模式", "打开投影模式设置", SearchMethod.PartialCJKSearch);
            Assert.Equal(2, va.Start);
            var vb = FuzzySearch.Search("电池", "所剩电池时间", SearchMethod.PartialCJKSearch);
            Assert.Equal(2, vb.Start);
            var vc = FuzzySearch.Search("波束成形", "将麦克风波束成形设置为仅我的声音", SearchMethod.PartialCJKSearch);
            Assert.Equal(4, vc.Start);
            var vd = FuzzySearch.SmartSearch("バッテリー設定", "Windows Update を開く");
            var ve = FuzzySearch.Search("电亁", "所剩电亁时间", SearchMethod.PartialCJKSearch);
            Assert.Equal(2, ve.Start);
        }


        [Fact]
        void Bitap()
        {
            var va = FuzzySearch.Search("laso", "lazolaso", SearchMethod.Bitap);
            FuzzySearch.Search("pía", "agonía", SearchMethod.Bitap);
            var vs = FuzzySearch.Search("spank", "bitbit spunky", SearchMethod.Bitap);
            var vb = FuzzyCompare.Ratio("baraspent", "baraspen", RatioMethod.Partial);
            var vc = FuzzyCompare.Ratio("baraspent", "baraspen", RatioMethod.Jaccard);
        }

        [Fact]
        void Ratios()
        {
            Assert.Equal(100, FuzzyCompare.Ratio("this is a test", "is this is a not really thing this is a test!", RatioMethod.Partial).Ratio);

            var s1 = "this is a test";
            var s2 = "this is a Test";
            Assert.Equal(1, FuzzyCompare.Ratio(s1, s2, RatioMethod.Levenshtein).Metric);

            var s1A = "this is a tset";
            Assert.Equal(1, FuzzyCompare.Ratio(s1, s1A, RatioMethod.OSA).Metric);

            var s8 = "test";
            var s9 = "the test";
            Assert.Equal(0, FuzzyCompare.Ratio(s8, s9, RatioMethod.Partial).Metric);
            Assert.True(FuzzyCompare.Ratio(s8, s9, RatioMethod.PartialTokenSort).Ratio > 90);

            Assert.True(FuzzyCompare.Ratio(this.s4, this.s5, RatioMethod.PartialTokenSort).Ratio > 95);
            Assert.Equal(1, FuzzyCompare.Ratio(this.s4, this.s5, RatioMethod.TokenSort).Metric);

            var partial = FuzzyCompare.Ratio("this is a test", "test not is this", RatioMethod.PartialTokenSort);
            // Assert???
        }

        [Fact]
        void ASpecialCase()
        {
            //var match = FuzzyMatch.PartialTokenSortRatio("flour", "come see my sunflower");

            //match = FuzzyMatch.SmartRatio("windows update", "open window updates");extended screen mode
            //var match = FuzzyMatch.SmartRatio("bluetooth settings", "snap top on primary monitor");
            var match = FuzzyCompare.SmartRatio("better battery", "open battery settings");
            match = FuzzyCompare.SmartRatio("open battery settings", "better battery");
            match = FuzzyCompare.SmartRatio("activate better battery", "better battery");
            match = FuzzyCompare.SmartRatio("activate better battery mode please", "better battery");

            match = FuzzySearch.Search("better battery", "activate better battery mode please", SearchMethod.TokenSearch);
            match = FuzzySearch.Search("better battery", "open battery settings", SearchMethod.TokenSearch);

        }

        [Fact]
        void ComplexRatios()
        {
            //bad use case
            Assert.True(FuzzyCompare.Ratio("sun flower", "sunflower", RatioMethod.TokenSort).Ratio > 90);
            //good use case
            Assert.Equal(1, FuzzyCompare.Ratio("sun flower", "sunflower", RatioMethod.Levenshtein).Metric);
            //yikes
            Assert.True(FuzzyCompare.Ratio("sun flower", "come see my sunflower", RatioMethod.Levenshtein).Ratio < 90);
            //yikes
            Assert.True(FuzzyCompare.Ratio("sun flower", "come see my sunflower", RatioMethod.TokenSet).Ratio < 90);

            //better, but still could improve
            Assert.True(FuzzyCompare.Ratio("sun flower", "come see my sunflower", RatioMethod.PartialTokenSet).Ratio < 90);

            //better, but still could improve
            Assert.True(FuzzyCompare.Ratio("sun flower", "come see my sunflower", RatioMethod.PartialTokenSort).Ratio < 90);
        }


        [Fact]
        void Mix()
        {
            var s1 = "HSINCHUANG";
            var s2 = "SINJHUAN";
            var s3 = "LSINJHUANG DISTRIC";
            var s4 = "SINJHUANG DISTRICT";
            Assert.True(FuzzyCompare.Ratio(s1, s4, RatioMethod.Partial).Ratio == 60);
            Assert.True(FuzzyCompare.Ratio(s1, s2, RatioMethod.Partial).Ratio > 75);
            Assert.True(FuzzyCompare.Ratio(s1, s3, RatioMethod.Partial).Ratio > 75);
        }

        [Fact]
        void Decomposition()
        {
            var transList = new List<string>(){
                "设定音量30",
                "打开蓝牙设置",
                "显示桌面",
                "进入休眠模式",
                "设置省电模式",
                "禁用相机隐私",
                "显示屏幕投影设置"
            };
            foreach (var trans in transList)
            {
                Stopwatch sw = Stopwatch.StartNew();
                var decomp = HanDecomposition.Decompose(trans);
                sw.Stop();
                this.output.WriteLine($"Trans: {trans}, Decomp: {decomp}, Decomp Time {sw.ElapsedMilliseconds}");
            }
        }

        [Fact]
        void EmptyStrings()
        {
            Assert.Equal(0, FuzzyCompare.Ratio("test_string", "", RatioMethod.Levenshtein).Ratio);
            Assert.Equal(0, FuzzyCompare.Ratio("test_string", "", RatioMethod.Partial).Ratio);
            Assert.Equal(0, FuzzyCompare.Ratio("", "", RatioMethod.Levenshtein).Ratio);
            Assert.Equal(0, FuzzyCompare.Ratio("", "", RatioMethod.Partial).Ratio);
        }

        [Fact]
        void SpeedCompare()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var ratio = (int)FuzzySearch.Search("update windos", "open the window update settings", SearchMethod.TokenSearch).Ratio;
            sw.Stop();
            this.output.WriteLine("HotFuzz Time " + sw.ElapsedMilliseconds);
            Assert.True(ratio > 90);

            sw.Restart();
            ratio = (int)FuzzyCompare.SmartRatio("update windos", "open the window update settings").Ratio;
            sw.Stop();
            this.output.WriteLine("HotFuzz Time " + sw.ElapsedMilliseconds);
            Assert.True(ratio > 90);

            sw.Restart();
            ratio = FuzzySharp.Fuzz.PartialTokenSortRatio("update windos", "open the window update settings");
            sw.Stop();
            this.output.WriteLine("FuzzSharp Time " + sw.ElapsedMilliseconds);
            Assert.True(ratio > 90);


            sw.Restart();

            ratio = FuzzySharp.Fuzz.WeightedRatio("update windos", "open the window update settings");
            sw.Stop();
            this.output.WriteLine("FuzzSharp Time " + sw.ElapsedMilliseconds);
            Assert.True(ratio < 90);        //weighted ratio sucks

            sw.Restart();

            int distance = s4
                .ToCharArray()
                .Zip(s5.ToCharArray(), (c1, c2) => new { c1, c2 })
                .Count(m => m.c1 != m.c2);
            sw.Stop();
            this.output.WriteLine("zip " + sw.ElapsedTicks);

            sw.Restart();

            distance = s4.ToCharArray().Except(s5.ToCharArray()).Count();

            sw.Stop();
            this.output.WriteLine("except " + sw.ElapsedTicks);
            sw.Restart();

            var count = 0;
            for (int i = 0; i < s4.Length; ++i)
            {
                if (s4[i] != s5[i])
                {
                    count++;
                }
            }

            sw.Stop();
            this.output.WriteLine("for " + sw.ElapsedTicks + "count" + count);


        }


        [Fact]
        void SlideHammingVsBitap()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var match = FuzzySearch.Search("update windos", "open the update window settings", SearchMethod.PartialSearch);
            Assert.True(match.Ratio > 90);

            sw.Stop();
            this.output.WriteLine("HotFuzz Time " + sw.ElapsedTicks);

            sw.Restart();
            match = FuzzySearch.Search("update windos", "open the update window settings");
            Assert.True(match.Ratio > 90);

            sw.Stop();
            this.output.WriteLine("HotFuzz Time " + sw.ElapsedTicks);

        }

        [Fact]
        void NGramTest()
        {
            var s = "student";
            var sa = "this is a student";
            this.output.WriteLine(string.Join(",", s.ToNGrams(2)));
            this.output.WriteLine(string.Join(",", sa.ToNGrams(2)));
        }

        [Fact]
        void Search()
        {
            var match = FuzzySearch.SmartSearch("best performance", "power mode better performance");
            match = FuzzySearch.SmartSearch("bolígrafo", "boligrafo rojo");
            match = FuzzySearch.SmartSearch("solo externo", "Cambiar la pantalla a externa solamente");
            match = FuzzySearch.SmartSearch("alarm and clock", "alarm  clock");

            this.output.WriteLine(match.Ratio.ToString());
        }

        [Fact]
        void Jaccard()
        {
            var match = FuzzyCompare.Ratio(this.s4, this.s5, RatioMethod.Jaccard);
            this.output.WriteLine(match.Ratio.ToString());
        }

        [Fact]
        void TokenNGramTest()
        {
            var tests = new List<(string input, int nGramLength, string[] expectedOutput)>
            {
                ( "This is a test", 2, new string[] { "This is", "is a", "a test" } ),
                ( "This is a test", 3, new string[] { "This is a", "is a test" } ),
                ( "Hello, how are you?", 2, new string[] { "Hello how", "how are", "are you" } ),
                ( "Hello, how are you?", 3, new string[] { "Hello how are", "how are you" } )
            };

            foreach (var (input, n, expectedOutput) in tests)
            {
                var actualOutput = input.ToTokenNGrams(n);
                Assert.Equal(actualOutput, expectedOutput);
            }
        }

        [Fact]
        void ShinglesTest()
        {
            var tests = new List<(string input, int minShingleSize, int maxShingleSize, string[] expectedOutput)>
            {
                ( "This is a test", 1, 0, new string[] { "This", "is", "a", "test", "This is", "is a", "a test", "This is a", "is a test", "This is a test" } ),
                ( "This is a test", 1, 4, new string[] { "This", "is", "a", "test", "This is", "is a", "a test", "This is a", "is a test", "This is a test" } ),
                ( "This is a test", 2, 2, new string[] { "This is", "is a", "a test" } ),
                ( "This is a test", 1, 3, new string[] { "This", "is", "a", "test", "This is", "is a", "a test", "This is a", "is a test" } ),
                ( "This is a test", 1, -1, new string[] { "This", "is", "a", "test", "This is", "is a", "a test", "This is a", "is a test" } ),
                ( "This is a test", 0, -1, new string[] { "This", "is", "a", "test", "This is", "is a", "a test", "This is a", "is a test" } ),
                ( "This is a test", 1, 1, new string[] { "This", "is", "a", "test" } ),
                ( "This is a test", 0, -100, new string[] { "This", "is", "a", "test" } ),
                ( "Hello, how are you?", 1, 0, new string[] { "Hello", "how", "are", "you", "Hello how", "how are", "are you", "Hello how are", "how are you", "Hello how are you" } ),
                ( "one", 1, 0, new string[] { "one" } ),
            };

            foreach (var (input, min, max, expectedOutput) in tests)
            {
                var actualOutput = input.ShingleFilter(min, max);
                this.output.WriteLine($"Test: Input={input}, Min={min}, Max={max}");
                this.output.WriteLine($"ActualOutput: {string.Join(", ", actualOutput)}");
                this.output.WriteLine($"Expected: {string.Join(", ", expectedOutput)}");
                this.output.WriteLine(Enumerable.SequenceEqual(actualOutput, expectedOutput) ? "PASS" : "FAIL");
                Assert.Equal(actualOutput, expectedOutput);
                this.output.WriteLine("************************************************");
            }
        }

        [Theory]
        [InlineData("a-okay", "a okay")]
        [InlineData("test(s)", "test s")]
        [InlineData("happy!", "happy")]
        [InlineData("imperative.", "imperative")]
        [InlineData("one, two, three", "one two three")]
        public void Clean(string input, string expectedOutput)
        {
            Assert.Equal(input.Clean(), expectedOutput);
        }
    }
}
