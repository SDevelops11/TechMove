using Microsoft.Extensions.Logging;

namespace TechMove.GLMS.Web.Strategies
{
    /// <summary>
    /// Context class for the Strategy pattern.
    /// Chooses the appropriate currency converter strategy at runtime and executes conversions.
    /// </summary>
    public class CurrencyConversionContext : ICurrencyConversionContext
    {
        private ICurrencyConverter? _currentStrategy;
        private readonly IEnumerable<ICurrencyConverter> _strategies;
        private readonly ILogger<CurrencyConversionContext> _logger;

        public CurrencyConversionContext(IEnumerable<ICurrencyConverter> strategies, ILogger<CurrencyConversionContext> logger)
        {
            _strategies = strategies;
            _logger = logger;
        }

        public void SetStrategy(ICurrencyConverter strategy)
        {
            _currentStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _logger.LogInformation("Active currency converter explicitly set to {Strategy}", strategy.GetType().Name);
        }

        public async Task<CurrencyConversionResult> ExecuteConversionAsync(decimal amount, string fromCurrency, string toCurrency = "ZAR")
        {
            var strategy = ResolveStrategy(fromCurrency);
            return await strategy.ConvertAsync(amount, toCurrency);
        }

        public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency = "ZAR")
        {
            var strategy = ResolveStrategy(fromCurrency);
            return await strategy.GetExchangeRateAsync(toCurrency);
        }

        private ICurrencyConverter ResolveStrategy(string currencyCode)
        {
            if (_currentStrategy != null && _currentStrategy.CurrencyCode.Equals(currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return _currentStrategy;
            }

            var normalized = (currencyCode ?? "USD").Trim().ToUpperInvariant();
            var strategy = _strategies.FirstOrDefault(s => s.CurrencyCode.Equals(normalized, StringComparison.OrdinalIgnoreCase));

            if (strategy != null)
            {
                return strategy;
            }

            // Fallback to ApiBasedConverter
            var apiStrategy = _strategies.OfType<ApiBasedConverter>().FirstOrDefault();
            if (apiStrategy != null)
            {
                apiStrategy.SetCurrencyCode(normalized);
                return apiStrategy;
            }

            // Default to USD converter if none matched
            return _strategies.OfType<UsdToZarConverter>().FirstOrDefault()
                ?? throw new InvalidOperationException($"No currency converter strategy available for {currencyCode}");
        }
    }
}
