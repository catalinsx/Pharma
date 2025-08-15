namespace pharma.Models
{
    // Junction table for Many-to-Many relationship between Pacient and Medicament
    public class PacientMedicament
    {
        public int PacientId { get; set; }
        public Pacient Pacient { get; set; } = null!;

        public int MedicamentId { get; set; }
        public Medicament Medicament { get; set; } = null!;

        // Additional properties if needed in the future
        public DateTime DataAsocierii { get; set; } = DateTime.Now;
    }
}