using Microsoft.Extensions.Logging;
using Moq;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Observers;
using TechMove.GLMS.Web.Services;
using Xunit;

namespace TechMove.GLMS.Tests
{
    /// <summary>
    /// Unit Tests for Observer Pattern implementation and Contract Lifecycle Transitions.
    /// Fulfills Rubric: Translation of Part 1 Observer Pattern design into code.
    /// </summary>
    public class StatusTransitionTests
    {
        [Fact]
        public async Task ContractNotifier_NotifyAsync_DispatchesEventsToAllFourObservers()
        {
            // Arrange (Part 1 GoF Observer Pattern: 4 Departmental Observers)
            var contract = new Contract
            {
                Id = 1,
                ContractNumber = "CON-2026-TEST",
                Status = ContractStatus.Active,
                EstimatedValueZar = 100000m
            };

            var opsLogger = new Mock<ILogger<OperationsDepartmentObserver>>();
            var finLogger = new Mock<ILogger<FinanceDepartmentObserver>>();
            var csLogger = new Mock<ILogger<ClientServicesObserver>>();
            var compLogger = new Mock<ILogger<ComplianceDepartmentObserver>>();
            var notifLogger = new Mock<ILogger<ContractNotifier>>();

            var observers = new List<IContractObserver>
            {
                new OperationsDepartmentObserver(opsLogger.Object),
                new FinanceDepartmentObserver(finLogger.Object),
                new ClientServicesObserver(csLogger.Object),
                new ComplianceDepartmentObserver(compLogger.Object)
            };

            var notifier = new ContractNotifier(observers, notifLogger.Object);

            // Act
            var logs = await notifier.NotifyAsync(contract, ContractStatus.Draft, ContractStatus.Active);

            // Assert
            Assert.Equal(4, logs.Count);
            Assert.Contains(logs, l => l.Department == "Operations Department");
            Assert.Contains(logs, l => l.Department == "Finance Department");
            Assert.Contains(logs, l => l.Department == "Client Services");
            Assert.Contains(logs, l => l.Department == "Compliance & Legal");

            foreach (var log in logs)
            {
                Assert.Equal("Draft", log.OldStatus);
                Assert.Equal("Active", log.NewStatus);
                Assert.False(string.IsNullOrWhiteSpace(log.ActionTaken));
            }
        }

        [Fact]
        public void CanTransitionStatus_ExpiredContract_RejectsTransition()
        {
            // Arrange
            var contractService = CreateContractServiceStub();

            // Act
            bool canTransition = contractService.CanTransitionStatus(
                ContractStatus.Expired,
                ContractStatus.Active,
                out string? errorMessage);

            // Assert
            Assert.False(canTransition);
            Assert.Contains("Expired contract cannot change status", errorMessage);
        }

        [Fact]
        public void CanTransitionStatus_IdenticalStatus_ReturnsFalse()
        {
            // Arrange
            var contractService = CreateContractServiceStub();

            // Act
            bool canTransition = contractService.CanTransitionStatus(
                ContractStatus.Active,
                ContractStatus.Active,
                out string? errorMessage);

            // Assert
            Assert.False(canTransition);
            Assert.Contains("already in 'Active' status", errorMessage);
        }

        [Theory]
        [InlineData(ContractStatus.Draft, ContractStatus.Active)]
        [InlineData(ContractStatus.Active, ContractStatus.OnHold)]
        [InlineData(ContractStatus.OnHold, ContractStatus.Active)]
        [InlineData(ContractStatus.Active, ContractStatus.Expired)]
        public void CanTransitionStatus_ValidTransitions_ReturnsTrue(ContractStatus current, ContractStatus target)
        {
            // Arrange
            var contractService = CreateContractServiceStub();

            // Act
            bool canTransition = contractService.CanTransitionStatus(current, target, out string? errorMessage);

            // Assert
            Assert.True(canTransition);
            Assert.Null(errorMessage);
        }

        private static ContractService CreateContractServiceStub()
        {
            // Lightweight instance for pure workflow rule testing
            return new ContractService(
                Mock.Of<TechMove.GLMS.Web.Repositories.IContractRepository>(),
                Mock.Of<TechMove.GLMS.Web.Factories.IContractFactory>(),
                Mock.Of<IContractNotifier>(),
                Mock.Of<IFileStorageService>(),
                Mock.Of<IExchangeRateService>(),
                null!,
                Mock.Of<ILogger<ContractService>>());
        }
    }
}
