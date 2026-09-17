using HKERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HKERP.Infrastructure.Data
{
    public class ControlDbContext : DbContext
    {
        public ControlDbContext(DbContextOptions<ControlDbContext> options) : base(options) { }

        public DbSet<ApiAccessControl> ApiAccessControls { get; set; }
        public DbSet<ApiAccessLog> ApiAccessLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApiAccessControl>(e =>
            {
                e.ToTable("MST_ApiAccessControl");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.Property(x => x.ApiName).HasMaxLength(200).IsRequired();
            });

            modelBuilder.Entity<ApiAccessLog>(e =>
            {
                e.ToTable("TRN_ApiAccessLog");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.Property(x => x.ApiName).HasMaxLength(200).IsRequired();
            });
        }
    }
}
