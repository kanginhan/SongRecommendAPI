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
    public class AddSingSongSvc
    {
        public int SongId { get; set; }

        private bool _isCrawlingSuccess { get; set; }

        private string _genre { get; set; }

        private string _releaseDate { get; set; }

        private string _message { get; set; }

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

        private void ProcessPageCrawlCompletedAsync(object sender, PageCrawlCompletedArgs e)
        {
            try {
                using (var db = new SongRecommendContext()) {
                    //---------------------------
                    // 가사 가져오기
                    //---------------------------
                    HttpClient client = new HttpClient();
                    string jsonString = client.GetStringAsync($"https://www.melon.com/song/lyricInfo.json?songId={SongId}").Result;
                    var lyric = JObject.Parse(jsonString).Value<string>("lyric");
                    if (string.IsNullOrEmpty(lyric)) {
                        _isCrawlingSuccess = false;
                        _message = "가사가 없습니다";
                        return;
                    }
                    
                    //---------------------------
                    // DB 저장
                    //---------------------------
                    var crawledPage = e.CrawledPage;
                    var doc = crawledPage.HtmlDocument.DocumentNode;

                    //---------------------------
                    // 노래정보 파싱
                    //---------------------------
                    var title = doc.SelectSingleNode(".//div[@class='song_name']").ChildNodes[2].InnerText.Trim();
                    if (string.IsNullOrEmpty(title)) {
                        _isCrawlingSuccess = false;
                        _message = "곡명이 없습니다";
                        return;
                    }

                    var singer = doc.SelectSingleNode(".//div[@class='artist']").InnerText.Trim();
                    if (string.IsNullOrEmpty(singer)) {
                        _isCrawlingSuccess = false;
                        _message = "가수명이 없습니다";
                        return;
                    }

                    db.BaseWordCollectingSong.Add(new BaseWordCollectingSong {
                        SongId = SongId,
                        Title = title,
                        Singer = singer,
                        Lyric = Regex.Replace(lyric, @"<br>|<br/>|</br>", " ", RegexOptions.IgnoreCase).Trim(),
                        Status = CrawlingStatus.Ready
                    });

                    db.SaveChanges();
                    _isCrawlingSuccess = true;
                    _message = $"{title} 을 추가했습니다";
                }
            }
            catch (Exception ex) {
                _isCrawlingSuccess = false;
                _message = ex.Message;
            }
        }

        public string Execute()
        {
            using (var db = new SongRecommendContext()) {
                if (db.BaseWordCollectingSong.Find(SongId) != null) {
                    return "이미 추가된 곡입니다";
                }
            }

            //---------------------------
            // 크롤링 설정
            //---------------------------
            var pageRequester = new PageRequester(_config);
            var crawler = new PoliteWebCrawler(_config, null, null, null, pageRequester, null, null, null, null);
            crawler.PageCrawlCompletedAsync += ProcessPageCrawlCompletedAsync;

            //---------------------------
            // 크롤링 시작
            //---------------------------
            crawler.Crawl(new Uri($"https://www.melon.com/song/detail.htm?songId={SongId}"));

            return _message;
        }
    }
}
