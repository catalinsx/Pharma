using System.ComponentModel.DataAnnotations;

namespace pharma.Models
{
    public class RetetaCreateViewModel
    {
        [Required]
        public DateTime Data { get; set; }

        public string? Serie { get; set; }

        public string? NrReteta { get; set; }

        public string? Observatii { get; set; }

        [Required]
        public int PacientId { get; set; }

        [Required]
        public int MedicamentId { get; set; }

        public DateTime? DataUrmatoareiVizite { get; set; }

        // For display purposes
        public string MedicamentNume { get; set; } = string.Empty;
        public List<Pacient> PacientiDisponibili { get; set; } = new();
    }
}