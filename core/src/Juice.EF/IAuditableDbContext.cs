namespace Juice.EF
{
    public interface IAuditableDbContext
    {
        Type EventType { get; }

        List<AuditEntry>? PendingAuditEntries { get; }

        string? User { get; }
    }
}
