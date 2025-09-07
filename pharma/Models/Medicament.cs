using pharma.Models;
using System.ComponentModel.DataAnnotations;

namespace pharma.Models
{
    public class Medicament
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Numele medicamentului este obligatoriu")]
        [StringLength(50, ErrorMessage = "Numele nu poate avea mai mult de 50 de caractere")]
        public string Nume { get; set; } = string.Empty;

        public List<Reteta> Retete { get; set; } = new();

        // Many-to-many relationship with Pacient through junction table
        public List<PacientMedicament> PacientMedicamente { get; set; } = new();
    }
}