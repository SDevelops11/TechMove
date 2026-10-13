namespace TechMove.GLMS.Web.Models
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
