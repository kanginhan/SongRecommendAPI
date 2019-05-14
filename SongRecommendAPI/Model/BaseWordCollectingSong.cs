using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Model
{
    public class BaseWordCollectingSong
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SongId { get; set; }
        public int PlayListSeq { get; set; }
        [Column(TypeName = "NVARCHAR(1000)")]
        public string Title { get; set; }
        [Column(TypeName = "NVARCHAR(1000)")]
        public string Singer { get; set; }
        public bool? IsPositive { get; set; }
        [Column(TypeName = "NTEXT")]
        public string Lyric { get; set; }
        [MaxLength(20)]
        public string Status { get; set; }
        [Column(TypeName = "NVARCHAR(1000)")]
        public string Message { get; set; }
        public double? Rate { get; set; }
    }
}
