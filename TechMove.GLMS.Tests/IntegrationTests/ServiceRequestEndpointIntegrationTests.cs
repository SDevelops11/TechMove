using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TechMove.GLMS.API.DTOs;
using TechMove.GLMS.API.Models;
using Xunit;

namespace TechMove.GLMS.Tests.IntegrationTests
{
    public class ServiceRequestEndpointIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ServiceRequestEndpointIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllServiceRequests_ReturnsSuccessStatusCode_AndList()
        {
            // Act: GET /api/servicerequests
            var response = await _client.GetAsync("/api/servicerequests");

            // Assert: 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var requests = await response.Content.ReadFromJsonAsync<List<ServiceRequestDto>>(_jsonOptions);
            Assert.NotNull(requests);
        }

        [Fact]
        public async Task CreateServiceRequest_AgainstActiveContract_Returns201Created_AndConvertsCurrency()
        {
            // Arrange: Contract 1 is seeded as Active
            var requestDto = new CreateServiceRequestRequestDto
            {
                ContractId = 1,
                Description = "Refrigerated Container Dispatch from Durban to Johannesburg",
                Origin = "Port of Durban Container Terminal",
                Destination = "City Deep Freight Hub, Johannesburg",
                Currency = "USD",
                Amount = 2500m
            };

            // Act: POST /api/servicerequests
            var response = await _client.PostAsJsonAsync("/api/servicerequests", requestDto);

            // Assert: HTTP Status Code 201 Created
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            var created = await response.Content.ReadFromJsonAsync<ServiceRequestDto>(_jsonOptions);
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.StartsWith("SR-", created.RequestNumber);
            Assert.Equal(2500m, created.OriginalCostUsd);
            Assert.Equal("USD", created.OriginalCurrency);
            Assert.True(created.Cost > 0, "Converted ZAR cost must be strictly greater than zero.");
            Assert.True(created.ExchangeRateUsed > 0, "Exchange rate used must be recorded.");
            Assert.Equal(ServiceRequestStatus.Pending, created.Status);
        }

        [Fact]
        public async Task CreateThenRead_ServiceRequest_VerifiesDataIntegrity()
        {
            // Step 1: Create request
            var requestDto = new CreateServiceRequestRequestDto
            {
                ContractId = 1,
                Description = "High-Security Transit: Platinum Ingot Haulage",
                Origin = "OR Tambo Air Cargo Hub",
                Destination = "Pretoria Mint Logistics Vault",
                Currency = "USD",
                Amount = 4000m
            };

            var postResponse = await _client.PostAsJsonAsync("/api/servicerequests", requestDto);
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var created = await postResponse.Content.ReadFromJsonAsync<ServiceRequestDto>(_jsonOptions);
            Assert.NotNull(created);

            // Step 2: Read back from GET /api/servicerequests/{id}
            var getResponse = await _client.GetAsync($"/api/servicerequests/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var retrieved = await getResponse.Content.ReadFromJsonAsync<ServiceRequestDto>(_jsonOptions);
            Assert.NotNull(retrieved);

            // Step 3: Verify data integrity
            Assert.Equal(created.Id, retrieved.Id);
            Assert.Equal(created.RequestNumber, retrieved.RequestNumber);
            Assert.Equal(created.Description, retrieved.Description);
            Assert.Equal(created.Origin, retrieved.Origin);
            Assert.Equal(created.Destination, retrieved.Destination);
            Assert.Equal(created.Cost, retrieved.Cost);
        }

        [Fact]
        public async Task CreateServiceRequest_AgainstExpiredContract_Returns400BadRequest_WithPolicyViolation()
        {
            // Arrange: Contract 4 is seeded with Status = Expired
            var invalidRequest = new CreateServiceRequestRequestDto
            {
                ContractId = 4, // Expired contract!
                Description = "Attempted Dispatch on Expired Agreement",
                Origin = "Cape Town Port",
                Destination = "Walvis Bay Hub",
                Currency = "USD",
                Amount = 1800m
            };

            // Act: POST /api/servicerequests
            var response = await _client.PostAsJsonAsync("/api/servicerequests", invalidRequest);

            // Assert: HTTP Status Code 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var rawBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("Expired", rawBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Policy Violation", rawBody, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateServiceRequest_AgainstOnHoldContract_Returns400BadRequest_WithPolicyViolation()
        {
            // Arrange: Contract 3 is seeded with Status = OnHold
            var invalidRequest = new CreateServiceRequestRequestDto
            {
                ContractId = 3, // OnHold contract!
                Description = "Attempted Dispatch on On-Hold Agreement",
                Origin = "Port Elizabeth Harbor",
                Destination = "Bloemfontein Central",
                Currency = "USD",
                Amount = 3200m
            };

            // Act: POST /api/servicerequests
            var response = await _client.PostAsJsonAsync("/api/servicerequests", invalidRequest);

            // Assert: HTTP Status Code 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var rawBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("OnHold", rawBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Policy Violation", rawBody, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateServiceRequest_WithNonExistentContract_Returns400BadRequest()
        {
            var invalidRequest = new CreateServiceRequestRequestDto
            {
                ContractId = 99999, // Non-existent
                Description = "Dispatch with fake contract",
                Origin = "A",
                Destination = "B",
                Currency = "USD",
                Amount = 100m
            };

            var response = await _client.PostAsJsonAsync("/api/servicerequests", invalidRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
