using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Model
{
    public class BaseWord
    {
        [Key]
        [Column(TypeName = "NVARCHAR(100)")]
        public string Word { get; set; }
        public double PositivePoint { get; set; }
        public double NegativePoint { get; set; }
        public double? Rate { get; set; }
    }
}
