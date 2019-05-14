using Abot.Core;
using Abot.Crawler;
using Abot.Poco;
using Newtonsoft.Json.Linq;
using SongRecommendAPI.Enums;
using SongRecommendAPI.Infra;
using SongRecommendAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SongRecommendAPI.Service
{
    public class AnalyzeLyricsSvc
    {
        private int _successCount { get; set; }

        public int Execute()
        {
            IEnumerable<BaseWordCollectingSong> songList = null;
            
            //---------------------------
            // 목록 가져오기
            //---------------------------
            using (var db = new SongRecommendContext()) {
                songList = db.BaseWordCollectingSong.Where(x => x.Status == CrawlingStatus.PositiveSet).ToList();
            }

            foreach (var song in songList) {
                using (var db = new SongRecommendContext()) {
                    var exceptTokenType = new List<KoreanPos> { KoreanPos.Space, KoreanPos.Josa, KoreanPos.Determiner, KoreanPos.Alpha, KoreanPos.Punctuation, KoreanPos.Foreign, KoreanPos.Hashtag, KoreanPos.Number, KoreanPos.CashTag, KoreanPos.URL, KoreanPos.KoreanParticle };
                    var words = song.Lyric.Normalize()             // 형태소 분석 토큰
                        .Tokenize()
                        .Where(x => exceptTokenType.Contains(x.Pos) == false)
                        .Select(x => x.Text)
                        .Distinct();

                    //------------------------------------
                    // 3. 단어 DB에 Update 또는 저장
                    //------------------------------------
                    foreach (var word in words) {
                        var baseWord = db.BaseWord.Find(word);
                        if (baseWord == null) {
                            db.BaseWord.Add(new BaseWord {
                                Word = word,
                                PositivePoint = song.IsPositive == true ? 1 : 0,
                                NegativePoint = song.IsPositive == false ? 1 : 0,
                            });
                        }
                        else {
                            if (song.IsPositive == true) {
                                baseWord.PositivePoint++;
                            }
                            if (song.IsPositive == false) {
                                baseWord.NegativePoint++;
                            }
                        }
                    }

                    var findSong = db.BaseWordCollectingSong.Find(song.SongId);
                    findSong.Status = CrawlingStatus.Tokenized;
                    db.SaveChanges();
                    _successCount++;
                }
            }

            return _successCount;
        }
    }
}
