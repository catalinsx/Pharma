using System.ComponentModel.DataAnnotations;

namespace pharma.Models
{
    public class RetetaCreateViewModel
    {
        [Required(ErrorMessage = "Medicamentul este obligatoriu")]
        public int MedicamentId { get; set; }

        public string MedicamentNume { get; set; } = "";

        [Required(ErrorMessage = "Data rețetei este obligatorie")]
        [Display(Name = "Data rețetei")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [StringLength(50, ErrorMessage = "Seria nu poate avea mai mult de 50 de caractere")]
        [Display(Name = "Serie")]
        public string? Serie { get; set; }

        [StringLength(50, ErrorMessage = "Numărul rețetei nu poate avea mai mult de 50 de caractere")]
        [Display(Name = "Numărul rețetei")]
        public string? NrReteta { get; set; }

        [StringLength(1000, ErrorMessage = "Observațiile nu pot avea mai mult de 1000 de caractere")]
        [Display(Name = "Observații")]
        public string? Observatii { get; set; }

        [Required(ErrorMessage = "Pacientul este obligatoriu")]
        [Display(Name = "Pacient")]
        public int PacientId { get; set; }

        [Display(Name = "Data următoarei vizite")]
        [DataType(DataType.Date)]
        public DateTime? DataUrmatoareiVizite { get; set; }

        public List<Pacient> PacientiDisponibili { get; set; } = new List<Pacient>();
    }
}