using MediatR;

namespace Juice.Domain
{
    public class DataEvent : INotification
    {
        public DataEvent(string name)
        {
            Validator.NotNullOrWhiteSpace(name, nameof(name));
            Name = name;
        }
        public string Name { get; private set; }

        public AuditRecord? AuditRecord { get; protected set; }
        public virtual DataEvent SetAuditRecord(AuditRecord record)
        {
            AuditRecord = record;
            return this;
        }
    }

    public class DataEvent<T> : DataEvent
    {
        public DataEvent(string name) : base(name)
        {
        }

        public override DataEvent<T> SetAuditRecord(AuditRecord record)
        {
            AuditRecord = record;
            return this;
        }
    }

    public static class DataEvents
    {
        public static DataEvent Inserted = new(nameof(Inserted));
        public static DataEvent Modified = new(nameof(Modified));
        public static DataEvent Deleted = new(nameof(Deleted));
    }

    public static class DataEventExtensions
    {
        public static DataEvent Create(this DataEvent dataEvent, Type? entityType, AuditRecord record)
        {
            if(entityType == null)
            {
                return new DataEvent(dataEvent.Name).SetAuditRecord(record);
            }
            var constructor = typeof(DataEvent<>).MakeGenericType(entityType).GetConstructor(new[] { typeof(string) });
            return ((DataEvent)constructor!.Invoke(new object[] { dataEvent.Name })).SetAuditRecord(record);
        }
    }
}
