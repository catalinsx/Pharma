using System.ComponentModel.DataAnnotations;

namespace pharma.Models
{
    public class Reteta
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Data rețetei este obligatorie")]
        [Display(Name = "Data rețetei")]
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

        // FK catre pacient
        [Required(ErrorMessage = "Pacientul este obligatoriu")]
        [Display(Name = "Pacient")]
        public int PacientId { get; set; }
        public Pacient? pacient { get; set; }

        // FK catre medicament
        [Required(ErrorMessage = "Medicamentul este obligatoriu")]
        [Display(Name = "Medicament")]
        public int MedicamentId { get; set; }
        public Medicament? medicament { get; set; }

        // data la care pacientul ar trebui să revină
        [Display(Name = "Data următoarei vizite")]
        public DateTime? DataUrmatoareiVizite { get; set; }
    }

}
