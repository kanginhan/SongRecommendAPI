﻿using Abot.Core;
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
    public class FindChartListSvc
    {
        public int PlayListSeq { get; set; }

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

        private void ProcessPageCrawlCompletedAsync(object sender, PageCrawlCompletedArgs e)
        {
            var crawledPage = e.CrawledPage;
            var doc = crawledPage.HtmlDocument.DocumentNode;
            var songNodes = doc.SelectNodes("//table/tbody/tr");

            //---------------------------
            // 크롤링 유효성 검사
            //---------------------------
            if (songNodes == null || songNodes.Count == 0) {
                _isCrawlingSuccess = false;
                return;
            }

            _isCrawlingSuccess = true;
            foreach (var node in songNodes) {
                try {
                    using (var db = new SongRecommendContext()) {
                        //---------------------------
                        // 노래정보 파싱
                        //---------------------------
                        var songId = node.SelectSingleNode(".//input[@class='input_check'] | .//input[@class='input_check ']").GetAttributeValue("value", 0);
                        var title = node.SelectSingleNode(".//div[@class='ellipsis rank01']//a | .//div[@class='ellipsis rank01']//span[@class='fc_lgray']").InnerText;
                        var singer = node.SelectSingleNode(".//div[@class='ellipsis rank02']//span").InnerText;
                        if (songId == 0) {
                            continue;
                        }

                        //---------------------------
                        // 가사 가져오기
                        //---------------------------
                        HttpClient client = new HttpClient();
                        string jsonString = client.GetStringAsync($"https://www.melon.com/song/lyricInfo.json?songId={songId}").Result;
                        var lyric = JObject.Parse(jsonString).Value<string>("lyric");
                        if (lyric == null || lyric.Length == 0) {
                            continue;
                        }

                        //---------------------------
                        // 적합도 분석
                        //---------------------------
                        var rate = AnalyzeRateSvc.Execute(lyric).Rate;

                        if (db.BaseWordCollectingSong.Find(songId) != null) {
                            db.BaseWordCollectingSong.Add(new BaseWordCollectingSong {
                                SongId = songId,
                                PlayListSeq = PlayListSeq,
                                Title = title,
                                Singer = singer,
                                Lyric = lyric,
                                Status = "READY",
                            });
                        }

                        //---------------------------
                        // DB 저장
                        //---------------------------
                        if (rate > 70 && db.ProposeSong.Find(songId) == null) {
                            //---------------------------
                            // 좋아요 가져오기
                            //---------------------------
                            jsonString = client.GetStringAsync($"https://www.melon.com/commonlike/getSongLike.json?contsIds={songId}").Result;
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
                            crawler.Crawl(new Uri($"https://www.melon.com/song/detail.htm?songId={songId}"));

                            db.ProposeSong.Add(new ProposeSong {
                                SongId = songId,
                                PlayListSeq = PlayListSeq,
                                Title = title,
                                Singer = singer,
                                Lyric = lyric,
                                Rate = rate,
                                Like = like,
                                Genre = _genre,
                                ReleaseDate = _releaseDate,
                                AddDate = DateTime.Now
                            });
                            _successCount++;
                        }

                        db.SaveChanges();
                    }
                }
                catch {
                }
            }
        }

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
            // 1. DJ플레이리스트 크롤링
            _isCrawlingSuccess = true;
            for (var startIndex = 1; _isCrawlingSuccess == true && startIndex <= 51; startIndex += 50) {
                //---------------------------
                // 크롤링 설정
                //---------------------------
                var pageRequester = new PageRequester(_config);
                var crawler = new PoliteWebCrawler(_config, null, null, null, pageRequester, null, null, null, null);
                crawler.PageCrawlCompletedAsync += ProcessPageCrawlCompletedAsync;

                //---------------------------
                // 크롤링 시작
                //---------------------------
                crawler.Crawl(new Uri($"https://www.melon.com/chart/index.htm#params%5Bidx%5D={startIndex}"));
            }

            return _successCount;
        }
    }
}
