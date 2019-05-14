using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Model.Local
{
    public class BaseWord
    {
        [Key]
        [Column(TypeName = "NVARCHAR(100)")]
        public string Word { get; set; }
        public int Count { get; set; }
        public double PositivePoint { get; set; }
        public double NegativePoint { get; set; }

        // 조정자
        // Count에 곱해서 조정한다
        public double AdjustCount { get; set; }
    }
}
