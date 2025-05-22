namespace VideoScripts.Data.Common;

public abstract class BaseEntity : IEntity
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty; // Changed from nullable to match IEntity
    public DateTime LastModifiedAt { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty; // Changed from nullable to match IEntity
    public bool IsDeleted { get; set; }
}
