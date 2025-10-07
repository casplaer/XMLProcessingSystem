using DataProcessorService.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DataProcessorService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        public DbSet<Module> Modules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ModuleConfiguration());
        }
    }
}
