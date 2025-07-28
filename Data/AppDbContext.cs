using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using WebApplication.Models;

namespace WebApplication.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();//создние базы данных

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique(); // <-- Уникальный индекс по требованию задания

            base.OnModelCreating(modelBuilder);
        }
    }
}
