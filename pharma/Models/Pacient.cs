using pharma.Models;
using System.ComponentModel.DataAnnotations;

namespace pharma.Models
{
    public class Pacient
    {

        public List<Reteta> Retete { get; set; } = new();

        public DateTime? DataUrmatoareiVizite { get; set; }

        // Many-to-many relationship with Medicament through junction table
        public List<PacientMedicament> PacientMedicamente { get; set; } = new();


        public int Id { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [StringLength(50, ErrorMessage = "Numele nu poate avea mai mult de 50 de caractere")]
        [Display(Name = "Nume de familie")]
        public string Nume { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        [StringLength(50, ErrorMessage = "Prenumele nu poate avea mai mult de 50 de caractere")]
        [Display(Name = "Prenume")]
        public string Prenume { get; set; } = string.Empty;

        [StringLength(13, MinimumLength = 13, ErrorMessage = "CNP-ul trebuie să aibă exact 13 cifre")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "CNP-ul trebuie să conțină doar cifre")]
        [Display(Name = "CNP")]
        public string? CNP { get; set; }

        [Phone(ErrorMessage = "Formatul numărului de telefon nu este valid")]
        [StringLength(15, ErrorMessage = "Numărul de telefon nu poate avea mai mult de 15 caractere")]
        [Display(Name = "Număr de telefon")]
        public string? NrTelefon { get; set; }

        [StringLength(1000, ErrorMessage = "Detaliile nu pot avea mai mult de 1000 de caractere")]
        [Display(Name = "Alte detalii")]
        public string? AlteDetalii { get; set; }

        public string NumeComplet => $"{Nume} {Prenume}";


    }
}