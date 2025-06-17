namespace pharma.Models
{
    public class Reteta
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string? Serie { get; set; }
        public string? NrReteta { get; set; }
        public string? Observatii { get; set; }

        public int ClientId { get; set; }
        public Client? client { get; set; }



    }
}
