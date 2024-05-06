namespace Juice.EF
{
    public interface IAuditableDbContext
    {
        /// <summary>
        /// Type of the event to be published, leave null if you don't want to publish any event
        /// </summary>
        Type? EventType { get; }

        List<AuditEntry>? PendingAuditEntries { get; }

        string? User { get; }
    }
}
