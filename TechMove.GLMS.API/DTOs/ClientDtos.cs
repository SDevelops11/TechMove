namespace TechMove.GLMS.API.DTOs
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ContractsCount { get; set; }
    }
}
