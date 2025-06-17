using Microsoft.EntityFrameworkCore;
using pharma.Models;

namespace pharma.Data
{
    public class PharmaContext : DbContext 
    {
        public PharmaContext(DbContextOptions<PharmaContext> options) : base(options) { }

        public DbSet<Client> Clienti { get; set; }
        public DbSet<Medicament> Medicamente { get; set; }
        public DbSet<Reteta> Retete { get; set; }

    }
}
