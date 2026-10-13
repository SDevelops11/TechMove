namespace TechMove.GLMS.Web.Services
{
    public interface IExchangeRateService
    {
        Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency = "ZAR");
        Task<Dictionary<string, decimal>> GetAllRatesAsync(string baseCurrency = "USD");
    }
}
