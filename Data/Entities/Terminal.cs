namespace GreenRetail.Data.Entities;

public class Terminal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
}
