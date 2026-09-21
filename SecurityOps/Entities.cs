using GreenRetail.Data.Entities;

namespace GreenRetail.SecurityOps;

public enum VerificationStatus
{
    Verified,
    Flagged
}

public class ReceiptVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid? VerifiedById { get; set; }
    public AppUser? VerifiedBy { get; set; }

    public VerificationStatus Status { get; set; } = VerificationStatus.Verified;

    public string? Notes { get; set; }

    public DateTime VerifiedUtc { get; set; }
}

public class SecurityIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid? ReportedById { get; set; }
    public AppUser? ReportedBy { get; set; }

    public string IncidentType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    public string Status { get; set; } = "Open";

    public DateTime CreatedUtc { get; set; }
}