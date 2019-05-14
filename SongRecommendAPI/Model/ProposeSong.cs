using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Model
{
    public class ProposeSong
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SongId { get; set; }
        public int PlayListSeq { get; set; }
        [Column(TypeName = "NVARCHAR(1000)")]
        public string Title { get; set; }
        [Column(TypeName = "NVARCHAR(1000)")]
        public string Singer { get; set; }
        [Column(TypeName = "NTEXT")]
        public string Lyric { get; set; }
        [Column(TypeName = "NVARCHAR(1000)")]
        public string Genre { get; set; }
        public string ReleaseDate { get; set; }
        public double Rate { get; set; }
        public int Like { get; set; }
        public DateTime AddDate { get; set; }
    }
}
