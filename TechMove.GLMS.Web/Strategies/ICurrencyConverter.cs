namespace TechMove.GLMS.Web.Strategies
{
    public class CurrencyConversionResult
    {
        public decimal OriginalAmount { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = "ZAR";
        public decimal ExchangeRate { get; set; }
        public decimal ConvertedAmount { get; set; }
        public string StrategyUsed { get; set; } = string.Empty;
        public DateTime ConvertedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Strategy interface for currency conversion algorithms.
    /// Matches Part 1 Architecture specification.
    /// </summary>
    public interface ICurrencyConverter
    {
        string CurrencyCode { get; }
        Task<CurrencyConversionResult> ConvertAsync(decimal amount, string toCurrency = "ZAR");
        Task<decimal> GetExchangeRateAsync(string toCurrency = "ZAR");
    }
}
