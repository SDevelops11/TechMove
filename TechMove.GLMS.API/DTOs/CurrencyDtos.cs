namespace TechMove.GLMS.API.DTOs
{
    public class CurrencyConversionResultDto
    {
        public decimal OriginalAmount { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = "ZAR";
        public decimal ExchangeRate { get; set; }
        public decimal ConvertedAmount { get; set; }
        public string StrategyUsed { get; set; } = string.Empty;
        public DateTime ConvertedAt { get; set; } = DateTime.UtcNow;
    }

    public class ExchangeRateDto
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = "ZAR";
        public decimal Rate { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

