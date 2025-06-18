namespace pharma.Models
{
    public class Reteta
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string? Serie { get; set; }
        public string? NrReteta { get; set; }
        public string? Observatii { get; set; }

        // FK catre p acient
        public int PacientId { get; set; }
        public Pacient? pacient { get; set; }

        //FK catre medicament
        public int MedicamentId { get; set; }
        public Medicament? medicament { get; set; }

        // data la care pacientul ar trb sa revina 
        public DateTime? DataUrmatoareiVizite { get; set; }


    }
}
