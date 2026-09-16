namespace Common.Base;

public interface IEntity
{
    bool IsActive { get; set; }

    Guid? CreatedBy { get; set; }
    DateTime CreatedAt { get; set; }

    Guid? ModifiedBy { get; set; }
    DateTime? ModifiedAt { get; set; }

    Guid? DeletedBy { get; set; }
    DateTime? DeletedAt { get; set; }

    Guid Id { get; set; }
}

public interface IConcurrencyEntity
{
    long RowVersion { get; set; }
}

public class BaseEntity : IEntity
{
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; } = null;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; } = null;
    public DateTime? ModifiedAt { get; set; } = null!;
    public Guid? DeletedBy { get; set; } = null!;
    public DateTime? DeletedAt { get; set; } = null!;
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.Now);
}
