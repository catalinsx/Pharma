using Microsoft.EntityFrameworkCore;
using pharma.Models;

namespace pharma.Data
{
    public class PharmaContext : DbContext
    {
        public PharmaContext(DbContextOptions<PharmaContext> options) : base(options) { }

        public DbSet<Pacient> Pacienti { get; set; }
        public DbSet<Medicament> Medicamente { get; set; }
        public DbSet<Reteta> Retete { get; set; }
        public DbSet<PacientMedicament> PacientMedicamente { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Many-to-Many relationship between Pacient and Medicament
            modelBuilder.Entity<PacientMedicament>()
                .HasKey(pm => new { pm.PacientId, pm.MedicamentId });

            modelBuilder.Entity<PacientMedicament>()
                .HasOne(pm => pm.Pacient)
                .WithMany(p => p.PacientMedicamente)
                .HasForeignKey(pm => pm.PacientId);

            modelBuilder.Entity<PacientMedicament>()
                .HasOne(pm => pm.Medicament)
                .WithMany(m => m.PacientMedicamente)
                .HasForeignKey(pm => pm.MedicamentId);

            base.OnModelCreating(modelBuilder);
        }
    }
}