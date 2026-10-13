using Microsoft.Extensions.Logging;
using Moq;
using TechMove.GLMS.Web.Factories;
using TechMove.GLMS.Web.Models;
using Xunit;

namespace TechMove.GLMS.Tests
{
    /// <summary>
    /// Unit Tests for Factory Pattern implementation.
    /// Fulfills Rubric: Translation of Part 1 Factory Pattern design into code.
    /// </summary>
    public class ContractFactoryTests
    {
        private readonly Mock<IContractNumberGenerator> _mockNumberGen;
        private readonly Mock<ILogger<ContractFactory>> _mockLogger;
        private readonly ContractFactory _factory;

        public ContractFactoryTests()
        {
            _mockNumberGen = new Mock<IContractNumberGenerator>();
            _mockLogger = new Mock<ILogger<ContractFactory>>();

            _mockNumberGen.Setup(g => g.GenerateContractNumber()).Returns("CON-2026-8888");

            _factory = new ContractFactory(_mockNumberGen.Object, _mockLogger.Object);
        }

        [Theory]
        [InlineData("Standard", typeof(StandardContract), 1.00, ServiceLevel.Standard)]
        [InlineData("Premium", typeof(PremiumContract), 1.25, ServiceLevel.Premium)]
        [InlineData("International", typeof(InternationalContract), 1.50, ServiceLevel.International)]
        [InlineData("Express", typeof(ExpressContract), 1.75, ServiceLevel.Express)]
        public void CreateContract_InstantiatesCorrectConcreteTypeAndMultiplier(
            string typeName, Type expectedType, decimal expectedMultiplier, ServiceLevel expectedLevel)
        {
            // Arrange
            var start = DateTime.UtcNow;
            var end = start.AddMonths(4);

            // Act
            var contract = _factory.CreateContract(typeName, 1, "Test Agreement", start, end);

            // Assert
            Assert.IsType(expectedType, contract);
            Assert.Equal(expectedMultiplier, contract.BaseRateMultiplier);
            Assert.Equal(expectedLevel, contract.ServiceLevel);
            Assert.Equal("CON-2026-8888", contract.ContractNumber);
        }

        [Fact]
        public void CreateEntity_AppliesMultiplierAndCurrencyConversion()
        {
            // Arrange
            var start = DateTime.UtcNow;
            var end = start.AddMonths(3);
            decimal baseUsd = 10000m;
            decimal fxRate = 18.50m;

            // Act: Express contract has 1.75 multiplier -> 10000 * 1.75 = 17500 USD -> 17500 * 18.50 = 323750 ZAR
            var entity = _factory.CreateEntity("Express", 1, "Urgent Air Freight SLA", start, end, baseUsd, fxRate);

            // Assert
            Assert.NotNull(entity);
            Assert.Equal("Express", entity.ContractType);
            Assert.Equal(17500.00m, entity.EstimatedValueUsd);
            Assert.Equal(323750.00m, entity.EstimatedValueZar);
            Assert.Equal(ContractStatus.Draft, entity.Status);
        }

        [Fact]
        public void CreateEntity_PremiumContractUnder30Days_ThrowsValidationException()
        {
            // Arrange (Edge Case: Premium SLA requires at least 30 days)
            var start = DateTime.UtcNow;
            var end = start.AddDays(15); // only 15 days

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                _factory.CreateEntity("Premium", 1, "Short Premium SLA", start, end, 5000m, 18.50m);
            });

            Assert.Contains("minimum duration of 30 days", ex.Message);
        }

        [Fact]
        public void CreateEntity_EndDateBeforeStartDate_ThrowsValidationException()
        {
            // Arrange (Edge Case: Invalid date range)
            var start = DateTime.UtcNow;
            var end = start.AddDays(-5); // End is in past

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                _factory.CreateEntity("Standard", 1, "Invalid Dates", start, end, 5000m, 18.50m);
            });

            Assert.Contains("end date must be after the start date", ex.Message);
        }
    }
}
