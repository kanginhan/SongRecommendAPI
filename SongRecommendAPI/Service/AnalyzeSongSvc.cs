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
    public class AnalyzeSongSvc
    {
        public int SongId { get; set; }

        private bool _isCrawlingSuccess { get; set; }

        private string _title { get; set; }

        private string _singer { get; set; }

        private string _genre { get; set; }

        private string _releaseDate { get; set; }

        private string _albumCover { get; set; }

        private string _albumName { get; set; }

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

            _title = doc.SelectSingleNode(".//div[@class='song_name']").LastChild.InnerText.Trim();
            _singer = doc.SelectSingleNode(".//div[@class='artist']").InnerText.Trim();

            var genreNode = doc.SelectSingleNode(".//dt[text()='장르']");
            if (genreNode == null) {
                _genre = null;
            }
            else {
                _genre = genreNode.NextSibling.NextSibling.InnerText;
            }

            var releaseNode = doc.SelectSingleNode(".//dt[text()='발매일']");
            if (releaseNode == null) {
                _releaseDate = null;
            }
            else {
                _releaseDate = releaseNode.NextSibling.NextSibling.InnerText;
            }

            var albumCoverNode = doc.SelectSingleNode(".//a[@class='image_typeAll']").ChildNodes[1];
            if (albumCoverNode == null) {
                _albumCover = null;
            }
            else {
                _albumCover = albumCoverNode.GetAttributeValue("src", "");
            }

            var albumNameNode = doc.SelectSingleNode(".//dt[text()='앨범']");
            if (albumNameNode == null) {
                _albumName = null;
            }
            else {
                _albumName = albumNameNode.NextSibling.NextSibling.InnerText;
            }
        }



        public AnalyzeSongResult Execute()
        {
            //---------------------------
            // 가사 가져오기
            //---------------------------
            HttpClient client = new HttpClient();
            string jsonString = client.GetStringAsync($"https://www.melon.com/song/lyricInfo.json?songId={SongId}").Result;
            var lyric = JObject.Parse(jsonString).Value<string>("lyric");
            if (lyric == null || lyric.Length == 0) {
                return null;
            }

            var analyzeResult = AnalyzeRateSvc.Execute(lyric);

            //---------------------------
            // 좋아요 가져오기
            //---------------------------
            jsonString = client.GetStringAsync($"https://www.melon.com/commonlike/getSongLike.json?contsIds={SongId}").Result;
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
            crawler.Crawl(new Uri($"https://www.melon.com/song/detail.htm?songId={SongId}"));

            var song = new ProposeSong {
                SongId = SongId,
                Title = _title,
                Singer = _singer,
                Lyric = lyric,
                Rate = analyzeResult.Rate,
                Like = like,
                Genre = _genre,
                ReleaseDate = _releaseDate,
                AddDate = DateTime.Now
            };

            if (analyzeResult.Rate > 70) {
                using (var db = new SongRecommendContext()) {
                    if (db.ProposeSong.Find(SongId) == null) {
                        db.ProposeSong.Add(song);
                        db.SaveChanges();
                    }
                }
            }

            var resultLyric = lyric;

            foreach (var word in analyzeResult.Words) {
                resultLyric = resultLyric.Replace(word.Word, $@"<span class='v-chip theme--dark light-green darken-2'><span class='v-chip__content tooltip'>{word.Word}<span class='tooltiptext'>{(int)word.Rate}%</span></span></span>");
            }

            var result = new AnalyzeSongResult {
                SongId = SongId,
                Title = _title,
                Singer = _singer,
                Lyric = resultLyric,
                Rate = analyzeResult.Rate,
                AlbumCover = _albumCover,
                AlbumName = _albumName
            };

            return result;
        }
    }

    public class AnalyzeSongResult : ProposeSong
    {
        public string AlbumCover { get; set; }
        public string AlbumName { get; set; }
    }
}
