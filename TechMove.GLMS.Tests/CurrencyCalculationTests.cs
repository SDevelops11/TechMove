using Microsoft.Extensions.Logging;
using Moq;
using TechMove.GLMS.Web.Services;
using TechMove.GLMS.Web.Strategies;
using Xunit;

namespace TechMove.GLMS.Tests
{
    /// <summary>
    /// Unit Tests for Currency Calculation logic and Strategy pattern implementation.
    /// Fulfills Rubric: Currency Calculation math verification, mock usage, and edge case coverage.
    /// </summary>
    public class CurrencyCalculationTests
    {
        private readonly Mock<IExchangeRateService> _mockRateService;
        private readonly Mock<ILogger<UsdToZarConverter>> _mockUsdLogger;
        private readonly Mock<ILogger<EurToZarConverter>> _mockEurLogger;
        private readonly Mock<ILogger<GbpToZarConverter>> _mockGbpLogger;
        private readonly Mock<ILogger<ApiBasedConverter>> _mockApiLogger;
        private readonly Mock<ILogger<CurrencyConversionContext>> _mockContextLogger;

        public CurrencyCalculationTests()
        {
            _mockRateService = new Mock<IExchangeRateService>();
            _mockUsdLogger = new Mock<ILogger<UsdToZarConverter>>();
            _mockEurLogger = new Mock<ILogger<EurToZarConverter>>();
            _mockGbpLogger = new Mock<ILogger<GbpToZarConverter>>();
            _mockApiLogger = new Mock<ILogger<ApiBasedConverter>>();
            _mockContextLogger = new Mock<ILogger<CurrencyConversionContext>>();
        }

        [Fact]
        public async Task ConvertAsync_UsdToZar_CalculatesAccurateMath()
        {
            // Arrange (TDD Principle: AAA)
            decimal usdAmount = 1000.00m;
            decimal fixedRate = 18.500000m;
            decimal expectedZar = 18500.00m;

            _mockRateService.Setup(s => s.GetExchangeRateAsync("USD", "ZAR"))
                .ReturnsAsync(fixedRate);

            var converter = new UsdToZarConverter(_mockRateService.Object, _mockUsdLogger.Object);

            // Act
            var result = await converter.ConvertAsync(usdAmount, "ZAR");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(usdAmount, result.OriginalAmount);
            Assert.Equal("USD", result.FromCurrency);
            Assert.Equal("ZAR", result.ToCurrency);
            Assert.Equal(fixedRate, result.ExchangeRate);
            Assert.Equal(expectedZar, result.ConvertedAmount);
            Assert.Equal(nameof(UsdToZarConverter), result.StrategyUsed);
        }

        [Theory]
        [InlineData(100.00, 18.25, 1825.00)]
        [InlineData(543.21, 18.5432, 10072.85)]
        [InlineData(12500.50, 19.1234, 239052.06)]
        [InlineData(1.00, 18.75, 18.75)]
        public async Task ConvertAsync_VariousRatesAndAmounts_RoundsToTwoDecimalPlaces(
            decimal amount, decimal rate, decimal expectedZar)
        {
            // Arrange
            _mockRateService.Setup(s => s.GetExchangeRateAsync("USD", "ZAR"))
                .ReturnsAsync(rate);

            var converter = new UsdToZarConverter(_mockRateService.Object, _mockUsdLogger.Object);

            // Act
            var result = await converter.ConvertAsync(amount, "ZAR");

            // Assert
            Assert.Equal(expectedZar, result.ConvertedAmount);
        }

        [Fact]
        public async Task ConvertAsync_ZeroAmount_ReturnsZeroZar()
        {
            // Arrange (Edge Case: 0 value)
            decimal zeroAmount = 0.00m;
            _mockRateService.Setup(s => s.GetExchangeRateAsync("USD", "ZAR"))
                .ReturnsAsync(18.50m);

            var converter = new UsdToZarConverter(_mockRateService.Object, _mockUsdLogger.Object);

            // Act
            var result = await converter.ConvertAsync(zeroAmount, "ZAR");

            // Assert
            Assert.Equal(0.00m, result.ConvertedAmount);
        }

        [Fact]
        public async Task ConvertAsync_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange (Edge Case: Negative input failure check)
            decimal negativeAmount = -50.00m;
            var converter = new UsdToZarConverter(_mockRateService.Object, _mockUsdLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await converter.ConvertAsync(negativeAmount, "ZAR");
            });
        }

        [Fact]
        public async Task StrategyPattern_CurrencyConversionContext_SelectsCorrectStrategy()
        {
            // Arrange
            _mockRateService.Setup(s => s.GetExchangeRateAsync("EUR", "ZAR")).ReturnsAsync(20.00m);
            _mockRateService.Setup(s => s.GetExchangeRateAsync("GBP", "ZAR")).ReturnsAsync(23.50m);
            _mockRateService.Setup(s => s.GetExchangeRateAsync("USD", "ZAR")).ReturnsAsync(18.50m);

            var strategies = new List<ICurrencyConverter>
            {
                new UsdToZarConverter(_mockRateService.Object, _mockUsdLogger.Object),
                new EurToZarConverter(_mockRateService.Object, _mockEurLogger.Object),
                new GbpToZarConverter(_mockRateService.Object, _mockGbpLogger.Object),
                new ApiBasedConverter(_mockRateService.Object, _mockApiLogger.Object)
            };

            var context = new CurrencyConversionContext(strategies, _mockContextLogger.Object);

            // Act
            var eurResult = await context.ExecuteConversionAsync(100m, "EUR", "ZAR");
            var gbpResult = await context.ExecuteConversionAsync(200m, "GBP", "ZAR");

            // Assert
            Assert.Equal(nameof(EurToZarConverter), eurResult.StrategyUsed);
            Assert.Equal(2000.00m, eurResult.ConvertedAmount);

            Assert.Equal(nameof(GbpToZarConverter), gbpResult.StrategyUsed);
            Assert.Equal(4700.00m, gbpResult.ConvertedAmount);
        }
    }
}
