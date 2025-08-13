using System.ComponentModel.DataAnnotations;

namespace pharma.Models
{
    public class PacientEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Nume { get; set; } = string.Empty;

        [Required]
        public string Prenume { get; set; } = string.Empty;

        [Required]
        public List<int> SelectedMedicamentIds { get; set; } = new();

        // For displaying in the view
        public List<Medicament> AvailableMedicamente { get; set; } = new();
    }
}