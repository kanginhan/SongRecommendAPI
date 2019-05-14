using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SongRecommendAPI.Enums;
using SongRecommendAPI.Infra;
using SongRecommendAPI.Model;
using SongRecommendAPI.Service;

namespace SongRecommendAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class WordController : ControllerBase
    {
        [HttpPost]
        public int AddSongs([FromBody] PlayListRequestDto request)
        {
            var biz = new AddSongsSvc();
            biz.PlayListSeq = request.PlayListSeq;
            return biz.Execute();
        }

        [HttpGet]
        public List<BaseWordCollectingSong> GetListBaseSongs()
        {
            using (var db = new SongRecommendContext()) {
                return db.BaseWordCollectingSong.Select(x => new BaseWordCollectingSong {
                    SongId = x.SongId,
                    Title = x.Title,
                    Singer = x.Singer,
                    PlayListSeq = x.PlayListSeq,
                    IsPositive = x.IsPositive,
                    Status = x.Status,
                    Message = x.Message,
                    Rate = x.Rate
                }).ToList();
            }
        }

        [HttpGet]
        public IActionResult GetLyrics(int songId)
        {
            using (var db = new SongRecommendContext()) {
                var findSong = db.BaseWordCollectingSong.Find(songId);
                if (findSong == null) {
                    return BadRequest();
                }

                return Ok(findSong.Lyric);
            }
        }

        [HttpPost]
        public IActionResult ChangePositive([FromBody]ChangePositiveDto request)
        {
            using (var db = new SongRecommendContext()) {
                var findSong = db.BaseWordCollectingSong.Find(request.SongId);
                if (findSong == null) {
                    return BadRequest();
                }

                findSong.IsPositive = request.IsPositive;
                if (findSong.Status == CrawlingStatus.Ready) {
                    findSong.Status = CrawlingStatus.PositiveSet;
                }
                db.SaveChanges();

                return Ok();
            }
        }

        [HttpGet]
        public IActionResult AnalyzeLyrics()
        {
            var svc = new AnalyzeLyricsSvc();
            var result = svc.Execute();
            return Ok(result);
        }

        [HttpPost]
        public IActionResult FindPlayList([FromBody] PlayListRequestDto request)
        {
            var svc = new FindPlayListSvc();
            svc.PlayListSeq = request.PlayListSeq;
            var result = svc.Execute();

            return Ok(result);
        }

        [HttpGet]
        public IActionResult LocalToAzureMigration()
        {
            var svc = new LocalToAzureMigrationSvc();
            svc.Execute();
            return Ok();
        }

        [HttpGet]
        public IActionResult GetListBaseWord()
        {
            using (var db = new SongRecommendContext()) {
                var result = db.BaseWord.ToList();
                return Ok(result);
            }
        }

        [HttpGet]
        public IActionResult CalcWordsRate()
        {
            using (var db = new SongRecommendContext()) {
                var totalPositive = db.BaseWord.Sum(x => x.PositivePoint * 100);
                var totalNegative = db.BaseWord.Sum(x => x.NegativePoint * 100);

                foreach (var word in db.BaseWord) {
                    word.Rate = AnalyzeRateSvc.GetAdjustedRate(word.PositivePoint * 100, word.NegativePoint * 100, totalPositive, totalNegative);
                }

                db.SaveChanges();
                return Ok();
            }
        }

        [HttpGet]
        public IActionResult GetListProposeSong()
        {
            using (var db = new SongRecommendContext()) {
                var result = db.ProposeSong.ToList();
                return Ok(result);
            }
        }

        [HttpGet]
        public IActionResult CalcSongsRate()
        {
            using(var db = new SongRecommendContext()) {
                foreach(var song in db.ProposeSong) {
                    song.Rate = AnalyzeRateSvc.Execute(song.Lyric).Rate;
                }
                db.SaveChanges();
                return Ok();
            }
        }

        [HttpGet]
        public IActionResult SearchKeyword(string query)
        {
            HttpClient client = new HttpClient();
            string jsonString = client.GetStringAsync($"https://www.melon.com/search/keyword/index.json?query={query}").Result;
            var jObject = JObject.Parse(jsonString);
            return Ok(jObject);
        }

        [HttpGet]
        public IActionResult AnalyzeSong(int songId)
        {
            var svc = new AnalyzeSongSvc();
            svc.SongId = songId;
            var result = svc.Execute();

            return Ok(result);
        }
    }

    public class PlayListRequestDto
    {
        public int PlayListSeq { get; set; }
    }

    public class ChangePositiveDto
    {
        public int SongId { get; set; }
        public bool IsPositive { get; set; }
    }
}
