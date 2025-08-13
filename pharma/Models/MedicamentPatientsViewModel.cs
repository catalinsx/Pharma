namespace pharma.Models
{
    public class MedicamentPatientsViewModel
    {
        public int MedicamentId { get; set; }
        public string MedicamentNume { get; set; } = string.Empty;
        public List<Pacient> AssociatedPatients { get; set; } = new();
        public List<Pacient> AvailablePatients { get; set; } = new();
    }
}