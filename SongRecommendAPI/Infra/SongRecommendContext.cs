using Microsoft.EntityFrameworkCore;
using SongRecommendAPI.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Infra
{
    public class SongRecommendContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(ConfigurationManager.ConnectionStrings["PotpolioAzure"].ConnectionString);

        public DbSet<BaseWordCollectingSong> BaseWordCollectingSong { get; set; }
        public DbSet<BaseWord> BaseWord { get; set; }
        public DbSet<ProposeSong> ProposeSong { get; set; }
    }
}
