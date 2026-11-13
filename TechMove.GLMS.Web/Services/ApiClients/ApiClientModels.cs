namespace TechMove.GLMS.Web.Services.ApiClients
{
    public class ApiLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ApiAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ApiContractDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Models.ContractStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public Models.ServiceLevel ServiceLevel { get; set; }
        public string ServiceLevelName => ServiceLevel.ToString();
        public string ContractType { get; set; } = "Standard";
        public decimal EstimatedValueUsd { get; set; }
        public decimal EstimatedValueZar { get; set; }
        public string? AgreementFileName { get; set; }
        public bool HasAgreementFile => !string.IsNullOrWhiteSpace(AgreementFileName);
        public DateTime CreatedAt { get; set; }
        public int ActiveServiceRequestsCount { get; set; }
        public List<ApiContractStatusLogDto> StatusLogs { get; set; } = new();
    }

    public class ApiContractStatusLogDto
    {
        public int Id { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ApiCreateContractRequest
    {
        public string ContractType { get; set; } = "Standard";
        public int ClientId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal BaseValueUsd { get; set; }
    }

    public class ApiUpdateContractStatusRequest
    {
        public Models.ContractStatus NewStatus { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ApiClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ContractsCount { get; set; }
    }

    public class ApiServiceRequestDto
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractTitle { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public decimal OriginalCostUsd { get; set; }
        public string OriginalCurrency { get; set; } = "USD";
        public decimal ExchangeRateUsed { get; set; }
        public Models.ServiceRequestStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime CreatedDate { get; set; }
    }

    public class ApiCreateServiceRequestRequest
    {
        public int ContractId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal Amount { get; set; }
    }

    public class ApiCurrencyConversionResult
    {
        public decimal OriginalAmount { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = "ZAR";
        public decimal ExchangeRate { get; set; }
        public decimal ConvertedAmount { get; set; }
        public string StrategyUsed { get; set; } = string.Empty;
        public DateTime ConvertedAt { get; set; }
    }

    public class ApiExchangeRateResult
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = "ZAR";
        public decimal Rate { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

