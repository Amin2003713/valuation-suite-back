namespace Domain.Common;

public abstract class BaseEntity
{
    protected BaseEntity() { }

    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
}
