using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TechMove.GLMS.Web.Services
{
    /// <summary>
    /// Service that integrates with external Currency Exchange API (ExchangeRate-API)
    /// using HttpClient and async/await with robust try/catch fallback error handling.
    /// </summary>
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExchangeRateService> _logger;

        private const string CacheKeyPrefix = "FxRate_";

        public ExchangeRateService(
            HttpClient httpClient,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<ExchangeRateService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency = "ZAR")
        {
            fromCurrency = (fromCurrency ?? "USD").Trim().ToUpperInvariant();
            toCurrency = (toCurrency ?? "ZAR").Trim().ToUpperInvariant();

            if (fromCurrency == toCurrency)
                return 1.0m;

            string cacheKey = $"{CacheKeyPrefix}{fromCurrency}_{toCurrency}";

            if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
            {
                _logger.LogDebug("Returning cached exchange rate for {From}->{To}: {Rate}", fromCurrency, toCurrency, cachedRate);
                return cachedRate;
            }

            try
            {
                var rates = await GetAllRatesAsync(fromCurrency);
                if (rates.TryGetValue(toCurrency, out var rate) && rate > 0)
                {
                    var cacheTtl = TimeSpan.FromMinutes(_configuration.GetValue("CurrencyApi:CacheDurationMinutes", 60));
                    _cache.Set(cacheKey, rate, cacheTtl);
                    _logger.LogInformation("Fetched live exchange rate from API for {From}->{To}: {Rate}", fromCurrency, toCurrency, rate);
                    return rate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch live exchange rate for {From}->{To}. Utilizing fallback rate.", fromCurrency, toCurrency);
            }

            // Fallback rates if external API is unreachable or rate missing
            return GetFallbackRate(fromCurrency, toCurrency);
        }

        public async Task<Dictionary<string, decimal>> GetAllRatesAsync(string baseCurrency = "USD")
        {
            baseCurrency = (baseCurrency ?? "USD").Trim().ToUpperInvariant();
            string cacheKey = $"{CacheKeyPrefix}All_{baseCurrency}";

            if (_cache.TryGetValue(cacheKey, out Dictionary<string, decimal>? cachedDict) && cachedDict != null)
            {
                return cachedDict;
            }

            var results = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string baseUrl = _configuration.GetValue<string>("CurrencyApi:BaseUrl") ?? "https://open.er-api.com/v6/latest/";
                string endpoint = $"{baseUrl.TrimEnd('/')}/{baseCurrency}";

                _logger.LogInformation("Requesting external currency API at {Endpoint}", endpoint);
                using var response = await _httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);

                    if (doc.RootElement.TryGetProperty("rates", out var ratesElement))
                    {
                        foreach (var prop in ratesElement.EnumerateObject())
                        {
                            if (prop.Value.TryGetDecimal(out var val))
                            {
                                results[prop.Name] = val;
                            }
                        }
                    }

                    if (results.Count > 0)
                    {
                        _cache.Set(cacheKey, results, TimeSpan.FromMinutes(60));
                        return results;
                    }
                }
                else
                {
                    _logger.LogWarning("Currency API returned HTTP status {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while consuming external currency API");
            }

            // Fallback dictionary
            results["ZAR"] = GetFallbackRate(baseCurrency, "ZAR");
            results["USD"] = baseCurrency == "USD" ? 1.0m : 0.054m;
            results["EUR"] = baseCurrency == "EUR" ? 1.0m : 0.92m;
            results["GBP"] = baseCurrency == "GBP" ? 1.0m : 0.79m;

            return results;
        }

        private decimal GetFallbackRate(string from, string to)
        {
            if (from == to) return 1.0m;

            if (to == "ZAR")
            {
                return from switch
                {
                    "USD" => _configuration.GetValue("CurrencyApi:FallbackUsdToZarRate", 18.50m),
                    "EUR" => _configuration.GetValue("CurrencyApi:FallbackEurToZarRate", 20.10m),
                    "GBP" => _configuration.GetValue("CurrencyApi:FallbackGbpToZarRate", 23.40m),
                    _ => 18.50m
                };
            }

            return 1.0m;
        }
    }
}
