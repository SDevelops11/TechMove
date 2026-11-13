using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TechMove.GLMS.API.DTOs;
using TechMove.GLMS.API.Models;
using Xunit;

namespace TechMove.GLMS.Tests.IntegrationTests
{
    public class ContractEndpointIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ContractEndpointIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetContracts_ReturnsSuccessStatusCode_AndJsonList()
        {
            // Act: Call GET /api/contracts
            var response = await _client.GetAsync("/api/contracts");

            // Assert: HTTP Status Code 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert: Returned JSON is not null and contains contracts
            var content = await response.Content.ReadAsStringAsync();
            var contracts = JsonSerializer.Deserialize<List<ContractDto>>(content, _jsonOptions);

            Assert.NotNull(contracts);
            Assert.NotEmpty(contracts);
            Assert.Contains(contracts, c => c.ContractNumber == "CON-2026-0001");
        }

        [Fact]
        public async Task GetContracts_WithFilter_ReturnsFilteredResults()
        {
            // Act: LINQ Filter by status=Active
            var response = await _client.GetAsync("/api/contracts?status=Active");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contracts = await response.Content.ReadFromJsonAsync<List<ContractDto>>(_jsonOptions);

            Assert.NotNull(contracts);
            Assert.NotEmpty(contracts);
            Assert.All(contracts, c => Assert.Equal(ContractStatus.Active, c.Status));
        }

        [Fact]
        public async Task GetContractById_WhenExists_Returns200Ok_AndCorrectData()
        {
            // Act: GET /api/contracts/1 (seeded active contract)
            var response = await _client.GetAsync("/api/contracts/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contract = await response.Content.ReadFromJsonAsync<ContractDto>(_jsonOptions);

            Assert.NotNull(contract);
            Assert.Equal(1, contract.Id);
            Assert.Equal("CON-2026-0001", contract.ContractNumber);
            Assert.Equal(ContractStatus.Active, contract.Status);
        }

        [Fact]
        public async Task GetContractById_WhenNotFound_Returns404NotFound()
        {
            // Act: GET /api/contracts/99999
            var response = await _client.GetAsync("/api/contracts/99999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateContract_WithValidData_Returns201Created_AndLocationHeader()
        {
            // Arrange
            var createRequest = new CreateContractRequestDto
            {
                ContractType = "Premium",
                ClientId = 1,
                Title = "Singapore-Cape Town Bulk Logistics Pact",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(12),
                BaseValueUsd = 75_000m
            };

            // Act: POST /api/contracts
            var response = await _client.PostAsJsonAsync("/api/contracts", createRequest);

            // Assert: HTTP Status Code 201 Created
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            var created = await response.Content.ReadFromJsonAsync<ContractDto>(_jsonOptions);
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.StartsWith("CON-", created.ContractNumber);
            Assert.Equal("Singapore-Cape Town Bulk Logistics Pact", created.Title);
            Assert.Equal(ContractStatus.Draft, created.Status);
        }

        [Fact]
        public async Task CreateThenRead_VerifiesDataIntegrityAcrossNetwork()
        {
            // Step 1: Create new contract
            var createRequest = new CreateContractRequestDto
            {
                ContractType = "International",
                ClientId = 2,
                Title = "Nordic Cold-Chain Refrigerated Freight Agreement",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(18),
                BaseValueUsd = 150_000m
            };

            var postResponse = await _client.PostAsJsonAsync("/api/contracts", createRequest);
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var created = await postResponse.Content.ReadFromJsonAsync<ContractDto>(_jsonOptions);
            Assert.NotNull(created);
            var contractId = created.Id;

            // Step 2: Read back from GET /api/contracts/{id}
            var getResponse = await _client.GetAsync($"/api/contracts/{contractId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var retrieved = await getResponse.Content.ReadFromJsonAsync<ContractDto>(_jsonOptions);
            Assert.NotNull(retrieved);

            // Step 3: Assert exact data integrity across write & read operations
            Assert.Equal(created.Id, retrieved.Id);
            Assert.Equal(created.ContractNumber, retrieved.ContractNumber);
            Assert.Equal(created.Title, retrieved.Title);
            Assert.Equal(created.ClientId, retrieved.ClientId);
            Assert.Equal(created.ContractType, retrieved.ContractType);
        }

        [Fact]
        public async Task CreateContract_WithInvalidDateRange_Returns400BadRequest()
        {
            // Arrange: End date earlier than start date
            var invalidRequest = new CreateContractRequestDto
            {
                ContractType = "Standard",
                ClientId = 1,
                Title = "Invalid Date Agreement",
                StartDate = DateTime.UtcNow.Date.AddDays(10),
                EndDate = DateTime.UtcNow.Date.AddDays(2), // Invalid!
                BaseValueUsd = 10_000m
            };

            // Act: POST /api/contracts
            var response = await _client.PostAsJsonAsync("/api/contracts", invalidRequest);

            // Assert: HTTP Status Code 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateContractStatus_ValidTransition_Returns200Ok_AndNotifiesObservers()
        {
            // Arrange: Transition active contract 2 to OnHold
            var patchRequest = new UpdateContractStatusRequestDto
            {
                NewStatus = ContractStatus.OnHold,
                Reason = "Periodic fleet compliance inspection scheduled."
            };

            // Act: PATCH /api/contracts/2/status
            var response = await _client.PatchAsJsonAsync("/api/contracts/2/status", patchRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<ContractDto>(_jsonOptions);

            Assert.NotNull(updated);
            Assert.Equal(ContractStatus.OnHold, updated.Status);
            Assert.NotEmpty(updated.StatusLogs);
        }

        [Fact]
        public async Task UpdateContractStatus_WhenContractNotFound_Returns404NotFound()
        {
            var patchRequest = new UpdateContractStatusRequestDto
            {
                NewStatus = ContractStatus.Active,
                Reason = "Re-activation attempt"
            };

            var response = await _client.PatchAsJsonAsync("/api/contracts/99999/status", patchRequest);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
