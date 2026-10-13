using Microsoft.Extensions.Logging;
using TechMove.GLMS.Web.Services;

namespace TechMove.GLMS.Web.Strategies
{
    public abstract class BaseCurrencyConverter : ICurrencyConverter
    {
        protected readonly IExchangeRateService ExchangeRateService;
        protected readonly ILogger Logger;

        public abstract string CurrencyCode { get; }

        protected BaseCurrencyConverter(IExchangeRateService exchangeRateService, ILogger logger)
        {
            ExchangeRateService = exchangeRateService;
            Logger = logger;
        }

        public virtual async Task<decimal> GetExchangeRateAsync(string toCurrency = "ZAR")
        {
            return await ExchangeRateService.GetExchangeRateAsync(CurrencyCode, toCurrency);
        }

        public virtual async Task<CurrencyConversionResult> ConvertAsync(decimal amount, string toCurrency = "ZAR")
        {
            if (amount < 0)
            {
                throw new ArgumentException("Conversion amount cannot be negative.", nameof(amount));
            }

            var rate = await GetExchangeRateAsync(toCurrency);
            var converted = Math.Round(amount * rate, 2);

            Logger.LogInformation("Converted {Amount} {From} to {Converted} {To} using rate {Rate} ({Strategy})",
                amount, CurrencyCode, converted, toCurrency, rate, GetType().Name);

            return new CurrencyConversionResult
            {
                OriginalAmount = amount,
                FromCurrency = CurrencyCode,
                ToCurrency = toCurrency,
                ExchangeRate = rate,
                ConvertedAmount = converted,
                StrategyUsed = GetType().Name,
                ConvertedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Specialized converter strategy for United States Dollar (USD -> ZAR).
    /// </summary>
    public class UsdToZarConverter : BaseCurrencyConverter
    {
        public override string CurrencyCode => "USD";

        public UsdToZarConverter(IExchangeRateService exchangeRateService, ILogger<UsdToZarConverter> logger)
            : base(exchangeRateService, logger)
        {
        }
    }

    /// <summary>
    /// Specialized converter strategy for Euro (EUR -> ZAR).
    /// </summary>
    public class EurToZarConverter : BaseCurrencyConverter
    {
        public override string CurrencyCode => "EUR";

        public EurToZarConverter(IExchangeRateService exchangeRateService, ILogger<EurToZarConverter> logger)
            : base(exchangeRateService, logger)
        {
        }
    }

    /// <summary>
    /// Specialized converter strategy for British Pound (GBP -> ZAR).
    /// </summary>
    public class GbpToZarConverter : BaseCurrencyConverter
    {
        public override string CurrencyCode => "GBP";

        public GbpToZarConverter(IExchangeRateService exchangeRateService, ILogger<GbpToZarConverter> logger)
            : base(exchangeRateService, logger)
        {
        }
    }

    /// <summary>
    /// General-purpose API-driven strategy for international currencies.
    /// </summary>
    public class ApiBasedConverter : BaseCurrencyConverter
    {
        private string _dynamicCurrency = "USD";
        public override string CurrencyCode => _dynamicCurrency;

        public ApiBasedConverter(IExchangeRateService exchangeRateService, ILogger<ApiBasedConverter> logger)
            : base(exchangeRateService, logger)
        {
        }

        public void SetCurrencyCode(string currencyCode)
        {
            _dynamicCurrency = (currencyCode ?? "USD").Trim().ToUpperInvariant();
        }
    }
}
