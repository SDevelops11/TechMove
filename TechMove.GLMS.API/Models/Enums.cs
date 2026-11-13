namespace TechMove.GLMS.API.Models
{
    public enum ContractStatus
    {
        Draft,
        Active,
        OnHold,
        Expired
    }

    public enum ServiceLevel
    {
        Standard,
        Premium,
        International,
        Express
    }

    public enum ServiceRequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}

