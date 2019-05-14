using SongRecommendAPI.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Service
{
    public static class AnalyzeRateSvc
    {
        public static AnalyzeRateResult Execute(string lyrics)
        {
            using (var db = new SongRecommendContext()) {
                var totalPositive = db.BaseWord.Sum(x => x.PositivePoint * 100);
                var totalNegative = db.BaseWord.Sum(x => x.NegativePoint * 100);

                var exceptTokenType = new List<KoreanPos> { KoreanPos.Space, KoreanPos.Josa, KoreanPos.Determiner, KoreanPos.Alpha, KoreanPos.Punctuation, KoreanPos.Foreign, KoreanPos.Hashtag, KoreanPos.Number, KoreanPos.CashTag, KoreanPos.URL, KoreanPos.KoreanParticle };
                var words = lyrics.Normalize()             // 형태소 분석 토큰
                    .Tokenize()
                    .Where(x => exceptTokenType.Contains(x.Pos) == false)
                    .Select(x => x.Text)
                    .Distinct();

                var analyzeResults = (from baseWord in db.BaseWord
                                      join word in words
                                      on baseWord.Word equals word
                                      select new AnalyzeResult {
                                          Word = word,
                                          PositivePoint = baseWord.PositivePoint * 100,
                                          NegativePoint = baseWord.NegativePoint * 100
                                      }).ToList();

                var adjResults = analyzeResults.Where(x => {
                    var rate = GetAdjustedRate(x.PositivePoint, x.NegativePoint, totalPositive, totalNegative);
                    x.Rate = rate;
                    return rate > 80 && (x.NegativePoint + x.PositivePoint) / 100 > 10;
                });

                var resultRate = Math.Sqrt(adjResults.Sum(x => x.Rate) / 1800) * 100;

                return new AnalyzeRateResult {
                    Rate = resultRate,
                    Words = adjResults
                };
            }
        }

        public static double GetAdjustedRate(double positivePoint, double negativePoint, double totalPositive, double totalNegative)
        {
            var positiveAdjust = positivePoint * (totalPositive + totalNegative) / totalPositive / 2;
            var negativeAdjust = negativePoint * (totalPositive + totalNegative) / totalNegative / 2;

            var adjCount = (positiveAdjust + negativeAdjust) / 100;
            var adjRate = (positiveAdjust - negativeAdjust) / adjCount / 2 + 50;

            return adjRate;
        }
    }

    public class AnalyzeResult
    {
        public string Word { get; set; }
        public double PositivePoint { get; set; }
        public double NegativePoint { get; set; }
        public double Rate { get; set; }
    }

    public class AnalyzeRateResult
    {
        public double Rate { get; set; }
        public IEnumerable<AnalyzeResult> Words { get; set; }
    }
}
