using pharma.Models;
using System.ComponentModel.DataAnnotations;

namespace pharma.Models
{
    public class Medicament
    {
        public int Id { get; set; }
        [Required]
        public string? Nume { get; set; }

        public List<Reteta> Retete { get; set; } = new();

        // Many-to-many relationship with Pacient through junction table
        public List<PacientMedicament> PacientMedicamente { get; set; } = new();
    }
}