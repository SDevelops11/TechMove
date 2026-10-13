namespace TechMove.GLMS.Web.Strategies
{
    public interface ICurrencyConversionContext
    {
        void SetStrategy(ICurrencyConverter strategy);
        Task<CurrencyConversionResult> ExecuteConversionAsync(decimal amount, string fromCurrency, string toCurrency = "ZAR");
        Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency = "ZAR");
    }
}
