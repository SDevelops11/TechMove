using TechMove.GLMS.API.Models;

namespace TechMove.GLMS.API.Factories
{
    public interface IContractFactory
    {
        IContract CreateContract(string contractType, int clientId, string title, DateTime startDate, DateTime endDate);
        Contract CreateEntity(string contractType, int clientId, string title, DateTime startDate, DateTime endDate, decimal baseValueUsd, decimal exchangeRate);
    }
}

