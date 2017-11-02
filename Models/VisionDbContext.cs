using System;
using Microsoft.EntityFrameworkCore;

namespace app.Models
{
       public class VisionDbContext : DbContext
    {
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Label> Label { get; set; }
          protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=photos.db");
        }
    }
}