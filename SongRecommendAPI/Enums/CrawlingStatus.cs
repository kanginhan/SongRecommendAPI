using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Enums
{
    public static class CrawlingStatus
    {
        public const string Ready = "READY";
        public const string Error = "ERROR";
        public const string PositiveSet = "PositiveSet";
        public const string Tokenized = "Tokenized";
    }
}
