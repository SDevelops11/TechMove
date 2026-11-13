namespace TechMove.GLMS.API.Services
{
    public interface IExchangeRateService
    {
        Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency = "ZAR");
        Task<Dictionary<string, decimal>> GetAllRatesAsync(string baseCurrency = "USD");
    }
}

