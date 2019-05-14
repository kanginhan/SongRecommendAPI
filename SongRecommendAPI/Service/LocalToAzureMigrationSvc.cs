using SongRecommendAPI.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Service
{
    public class LocalToAzureMigrationSvc
    {
        public void Execute()
        {
            using (var localDb = new SongRecommendLocalContext())
            using (var azureDb = new SongRecommendContext()) {
                foreach (var song in localDb.BaseWordCollectingSongs) {
                    var findSong = azureDb.BaseWordCollectingSong.Find(song.SongId);
                    if (findSong == null) {
                        var newSong = new Model.BaseWordCollectingSong {
                            SongId = song.SongId,
                            PlayListSeq = song.PlayListSeq,
                            Title = song.Title,
                            Singer = song.Singer,
                            Lyric = song.Lyric,
                            Status = song.Status,
                            Rate = song.Rate,
                            Message = song.Message,
                            IsPositive = song.PositiveGrade == 100 ? true : false
                        };

                        azureDb.BaseWordCollectingSong.Add(newSong);
                    }
                }


                foreach (var word in localDb.BaseWords) {
                    var findWord = azureDb.BaseWord.Find(word.Word);
                    if (findWord == null) {
                        var newWord = new Model.BaseWord {
                            Word = word.Word,
                            PositivePoint = word.PositivePoint / 100,
                            NegativePoint = word.NegativePoint / 100,
                        };

                        azureDb.BaseWord.Add(newWord);
                    }
                }

                azureDb.SaveChanges();
            }
        }
    }
}
