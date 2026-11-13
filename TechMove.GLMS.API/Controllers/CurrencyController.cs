using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.API.DTOs;
using TechMove.GLMS.API.Services;

namespace TechMove.GLMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CurrencyController : ControllerBase
    {
        private readonly IServiceRequestService _requestService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(
            IServiceRequestService requestService,
            IExchangeRateService exchangeRateService,
            ILogger<CurrencyController> logger)
        {
            _requestService = requestService;
            _exchangeRateService = exchangeRateService;
            _logger = logger;
        }

        /// <summary>
        /// Converts a specified amount from an international currency (USD, EUR, GBP) to South African Rand (ZAR)
        /// using the GoF Strategy Pattern and live foreign exchange rates.
        /// </summary>
        /// <param name="amount">The numeric amount to convert.</param>
        /// <param name="currency">Source currency code (USD, EUR, GBP).</param>
        [HttpGet("convert")]
        [ProducesResponseType(typeof(CurrencyConversionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CurrencyConversionResultDto>> ConvertCurrency(
            [FromQuery] decimal amount,
            [FromQuery] string currency = "USD")
        {
            if (amount < 0)
            {
                return BadRequest(new { message = "Conversion amount cannot be negative." });
            }

            try
            {
                var result = await _requestService.CalculateCostAsync(amount, currency);
                return Ok(new CurrencyConversionResultDto
                {
                    OriginalAmount = result.OriginalAmount,
                    FromCurrency = result.FromCurrency,
                    ToCurrency = result.ToCurrency,
                    ExchangeRate = result.ExchangeRate,
                    ConvertedAmount = result.ConvertedAmount,
                    StrategyUsed = result.StrategyUsed,
                    ConvertedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform currency conversion for amount {Amount} and currency {Currency}", amount, currency);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the current live spot exchange rate between two currencies.
        /// </summary>
        [HttpGet("rate")]
        [ProducesResponseType(typeof(ExchangeRateDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ExchangeRateDto>> GetRate(
            [FromQuery] string from = "USD",
            [FromQuery] string to = "ZAR")
        {
            var rate = await _exchangeRateService.GetExchangeRateAsync(from, to);
            return Ok(new ExchangeRateDto
            {
                FromCurrency = from.ToUpperInvariant(),
                ToCurrency = to.ToUpperInvariant(),
                Rate = rate,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

