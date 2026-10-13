using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Factories
{
    public interface IContractFactory
    {
        IContract CreateContract(string contractType, int clientId, string title, DateTime startDate, DateTime endDate);
        Contract CreateEntity(string contractType, int clientId, string title, DateTime startDate, DateTime endDate, decimal baseValueUsd, decimal exchangeRate);
    }
}
