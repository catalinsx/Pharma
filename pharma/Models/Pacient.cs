using pharma.Models;
using System.ComponentModel.DataAnnotations;

namespace pharma.Models
{
    public class Pacient
    {
        public int Id { get; set; }

        [Required]
        public string Nume { get; set; } = string.Empty;

        [Required]
        public string Prenume { get; set; } = string.Empty;

        public List<Reteta> Retete { get; set; } = new();

        public DateTime? DataUrmatoareiVizite { get; set; }

        // Many-to-many relationship with Medicament through junction table
        public List<PacientMedicament> PacientMedicamente { get; set; } = new();
    }
}