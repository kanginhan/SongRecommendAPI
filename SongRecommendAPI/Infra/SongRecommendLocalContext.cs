using Microsoft.EntityFrameworkCore;
using SongRecommendAPI.Model.Local;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Infra
{
    public class SongRecommendLocalContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(ConfigurationManager.ConnectionStrings["SongRecommend"].ConnectionString);

        public DbSet<BaseWordCollectingSong> BaseWordCollectingSongs { get; set; }
        public DbSet<BaseWord> BaseWords { get; set; }
    }
}
