using Microsoft.Extensions.Logging;
using Moq;
using TechMove.GLMS.Web.Factories;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Repositories;
using TechMove.GLMS.Web.Services;
using TechMove.GLMS.Web.Strategies;
using Xunit;

namespace TechMove.GLMS.Tests
{
    /// <summary>
    /// Unit Tests for Workflow Logic and Business Rules.
    /// Fulfills Rubric: Verifying that Service Requests cannot be raised on Expired or On Hold contracts.
    /// </summary>
    public class WorkflowValidationTests
    {
        private readonly Mock<IServiceRequestRepository> _mockRequestRepo;
        private readonly Mock<IContractRepository> _mockContractRepo;
        private readonly Mock<ICurrencyConversionContext> _mockCurrencyContext;
        private readonly Mock<IContractNumberGenerator> _mockNumberGen;
        private readonly Mock<ILogger<ServiceRequestService>> _mockLogger;
        private readonly ServiceRequestService _service;

        public WorkflowValidationTests()
        {
            _mockRequestRepo = new Mock<IServiceRequestRepository>();
            _mockContractRepo = new Mock<IContractRepository>();
            _mockCurrencyContext = new Mock<ICurrencyConversionContext>();
            _mockNumberGen = new Mock<IContractNumberGenerator>();
            _mockLogger = new Mock<ILogger<ServiceRequestService>>();

            _mockNumberGen.Setup(g => g.GenerateServiceRequestNumber()).Returns("SR-2026-9999");
            _mockCurrencyContext.Setup(c => c.ExecuteConversionAsync(It.IsAny<decimal>(), It.IsAny<string>(), "ZAR"))
                .ReturnsAsync(new CurrencyConversionResult
                {
                    OriginalAmount = 1000m,
                    ExchangeRate = 18.50m,
                    ConvertedAmount = 18500m,
                    FromCurrency = "USD",
                    ToCurrency = "ZAR"
                });

            _service = new ServiceRequestService(
                _mockRequestRepo.Object,
                _mockContractRepo.Object,
                _mockCurrencyContext.Object,
                _mockNumberGen.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateServiceRequest_ParentContractIsExpired_ThrowsInvalidOperationException()
        {
            // Arrange (POE Workflow Rule: Expired contract blocks service request)
            var expiredContract = new Contract
            {
                Id = 10,
                ContractNumber = "CON-2026-EXPIRED",
                Status = ContractStatus.Expired
            };
            _mockContractRepo.Setup(r => r.GetByIdAsync(10, false)).ReturnsAsync(expiredContract);

            var dto = new CreateServiceRequestDto
            {
                ContractId = 10,
                Description = "Urgent cross-border haulage",
                Amount = 1500m,
                Currency = "USD"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _service.CreateServiceRequestAsync(dto);
            });

            Assert.Contains("Expired", ex.Message);
            _mockRequestRepo.Verify(r => r.AddAsync(It.IsAny<ServiceRequest>()), Times.Never);
        }

        [Fact]
        public async Task CreateServiceRequest_ParentContractIsOnHold_ThrowsInvalidOperationException()
        {
            // Arrange (POE Workflow Rule: On Hold contract blocks service request)
            var onHoldContract = new Contract
            {
                Id = 20,
                ContractNumber = "CON-2026-ONHOLD",
                Status = ContractStatus.OnHold
            };
            _mockContractRepo.Setup(r => r.GetByIdAsync(20, false)).ReturnsAsync(onHoldContract);

            var dto = new CreateServiceRequestDto
            {
                ContractId = 20,
                Description = "Refrigerated pharmaceutical transport",
                Amount = 2500m,
                Currency = "USD"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _service.CreateServiceRequestAsync(dto);
            });

            Assert.Contains("OnHold", ex.Message);
            _mockRequestRepo.Verify(r => r.AddAsync(It.IsAny<ServiceRequest>()), Times.Never);
        }

        [Fact]
        public async Task CreateServiceRequest_ParentContractIsActive_SuccessfullyDispatchesRequest()
        {
            // Arrange (Happy Path: Active contract allows service requests)
            var activeContract = new Contract
            {
                Id = 30,
                ContractNumber = "CON-2026-ACTIVE",
                Status = ContractStatus.Active
            };
            _mockContractRepo.Setup(r => r.GetByIdAsync(30, false)).ReturnsAsync(activeContract);

            var dto = new CreateServiceRequestDto
            {
                ContractId = 30,
                Description = "Heavy mining gear transport from Saldanha",
                Amount = 5000m,
                Currency = "USD"
            };

            // Act
            var result = await _service.CreateServiceRequestAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SR-2026-9999", result.RequestNumber);
            Assert.Equal(30, result.ContractId);
            Assert.Equal(5000m, result.OriginalCostUsd);
            Assert.Equal(18500m, result.Cost);
            _mockRequestRepo.Verify(r => r.AddAsync(It.IsAny<ServiceRequest>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task CreateServiceRequest_ZeroOrNegativeAmount_ThrowsArgumentException(decimal invalidAmount)
        {
            // Arrange (Edge Case: Zero / Negative input)
            var dto = new CreateServiceRequestDto
            {
                ContractId = 1,
                Description = "Bulk freight haulage",
                Amount = invalidAmount,
                Currency = "USD"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.CreateServiceRequestAsync(dto);
            });
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task CreateServiceRequest_EmptyOrNullDescription_ThrowsArgumentException(string? invalidDesc)
        {
            // Arrange (Edge Case: Missing description)
            var dto = new CreateServiceRequestDto
            {
                ContractId = 1,
                Description = invalidDesc!,
                Amount = 1000m,
                Currency = "USD"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.CreateServiceRequestAsync(dto);
            });
        }

        [Fact]
        public async Task CreateServiceRequest_NonExistentContract_ThrowsKeyNotFoundException()
        {
            // Arrange (Edge Case: Non-existent parent ID)
            _mockContractRepo.Setup(r => r.GetByIdAsync(999, false)).ReturnsAsync((Contract?)null);

            var dto = new CreateServiceRequestDto
            {
                ContractId = 999,
                Description = "Valid description",
                Amount = 1000m,
                Currency = "USD"
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _service.CreateServiceRequestAsync(dto);
            });
        }
    }
}
