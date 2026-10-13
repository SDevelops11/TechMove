namespace TechMove.GLMS.Web.Factories
{
    public interface IContractNumberGenerator
    {
        string GenerateContractNumber();
        string GenerateServiceRequestNumber();
    }

    public class ContractNumberGenerator : IContractNumberGenerator
    {
        private static readonly Random _random = new();

        public string GenerateContractNumber()
        {
            var year = DateTime.UtcNow.Year;
            var num = _random.Next(1000, 9999);
            return $"CON-{year}-{num}";
        }

        public string GenerateServiceRequestNumber()
        {
            var year = DateTime.UtcNow.Year;
            var num = _random.Next(1000, 9999);
            return $"SR-{year}-{num}";
        }
    }
}
