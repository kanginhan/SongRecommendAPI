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
    public class BaseSongToProposeSvc
    {
        private bool _isCrawlingSuccess { get; set; }

        private int _successCount { get; set; }

        private string _genre { get; set; }

        private string _releaseDate { get; set; }

        private CrawlConfiguration _config = new CrawlConfiguration {
            CrawlTimeoutSeconds = 0,
            DownloadableContentTypes = "text/html, text/plain",
            HttpServicePointConnectionLimit = 200,
            HttpRequestTimeoutInSeconds = 35,
            HttpRequestMaxAutoRedirects = 7,
            IsExternalPageCrawlingEnabled = false,
            IsExternalPageLinksCrawlingEnabled = false,
            IsUriRecrawlingEnabled = false,
            IsHttpRequestAutoRedirectsEnabled = true,
            IsHttpRequestAutomaticDecompressionEnabled = false,
            IsRespectRobotsDotTextEnabled = false,
            IsRespectMetaRobotsNoFollowEnabled = false,
            IsRespectAnchorRelNoFollowEnabled = false,
            IsForcedLinkParsingEnabled = false,
            /* activate */
            IsSendingCookiesEnabled = true,
            MaxConcurrentThreads = 10,
            MaxPagesToCrawl = 1000,
            MaxPagesToCrawlPerDomain = 0,
            MaxPageSizeInBytes = 0,
            MaxMemoryUsageInMb = 0,
            MaxMemoryUsageCacheTimeInSeconds = 0,
            MaxRobotsDotTextCrawlDelayInSeconds = 5,
            MaxCrawlDepth = 0,
            MinAvailableMemoryRequiredInMb = 0,
            MinCrawlDelayPerDomainMilliSeconds = 1000,
            UserAgentString = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko"
        };
        

        private void ProcessDetailPageCrawlCompletedAsync(object sender, PageCrawlCompletedArgs e)
        {
            var crawledPage = e.CrawledPage;
            var doc = crawledPage.HtmlDocument.DocumentNode;

            var genreNode = doc.SelectSingleNode(".//dt[text()='장르']");
            if(genreNode == null) {
                _genre = null;
            }
            else {
                _genre = genreNode.NextSibling.NextSibling.InnerText;
            }
                
            var releaseNode = doc.SelectSingleNode(".//dt[text()='발매일']");
            if(releaseNode == null) {
                _releaseDate = null;
            }
            else {
                _releaseDate = releaseNode.NextSibling.NextSibling.InnerText;
            }
        }


        public int Execute()
        {
            using (var db = new SongRecommendContext()) {
                // 대상 조회
                var targetSong = from baseSong in db.BaseWordCollectingSong
                                 join proposeSong in db.ProposeSong
                                 on baseSong.SongId equals proposeSong.SongId into proposeSongs
                                 from defaultPropose in proposeSongs.DefaultIfEmpty()
                                 where baseSong.Status == "Tokenized" && defaultPropose == null
                                 select baseSong;

                // Rate 계산
                foreach (var song in targetSong) {
                    try {
                        var rateResult = AnalyzeRateSvc.Execute(song.Lyric);
                        song.Rate = rateResult.Rate;
                        song.Status = "Analyzed";

                        if (song.Rate > 70) {
                            //---------------------------
                            // 좋아요 가져오기
                            //---------------------------
                            HttpClient client = new HttpClient();
                            var jsonString = client.GetStringAsync($"https://www.melon.com/commonlike/getSongLike.json?contsIds={song.SongId}").Result;
                            var like = 0;
                            try {
                                like = JObject.Parse(jsonString).Value<IEnumerable<JToken>>("contsLike").First().Value<int>("SUMMCNT");
                            }
                            catch { }

                            //---------------------------
                            // 크롤링 설정
                            //---------------------------
                            var pageRequester = new PageRequester(_config);
                            var crawler = new PoliteWebCrawler(_config, null, null, null, pageRequester, null, null, null, null);
                            crawler.PageCrawlCompletedAsync += ProcessDetailPageCrawlCompletedAsync;

                            //---------------------------
                            // 크롤링 시작
                            //---------------------------
                            crawler.Crawl(new Uri($"https://www.melon.com/song/detail.htm?songId={song.SongId}"));
                            
                            db.ProposeSong.Add(new ProposeSong {
                                SongId = song.SongId,
                                PlayListSeq = song.PlayListSeq,
                                Title = song.Title,
                                Singer = song.Singer,
                                Lyric = song.Lyric,
                                Rate = song.Rate ?? 0,
                                Like = like,
                                Genre = _genre,
                                ReleaseDate = _releaseDate,
                                AddDate = DateTime.Now
                            });
                            _successCount++;
                        }
                        
                    }
                    catch { }
                }

                db.SaveChanges();

                return _successCount;
            }
        }
    }
}
