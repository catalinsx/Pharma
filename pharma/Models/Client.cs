namespace pharma.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string? Nume { get; set; }
        public string? Prenume { get; set; }

        public List<Reteta> Retete { get; set; } = new();

    }
}
