using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;

namespace GreenRetail.SecurityOps;

public sealed record VerifyReceiptCommand(
    Guid SaleId,
    Guid? VerifiedById,
    string? Notes);

public sealed record ReceiptVerificationResult(
    bool IsValid,
    Guid SaleId,
    int LineCount,
    decimal TotalQuantity,
    long TotalKobo);

public interface IVerifyReceiptUseCase
    : IUseCase<VerifyReceiptCommand, Result<ReceiptVerificationResult>>
{
}

public sealed class VerifyReceiptUseCase : IVerifyReceiptUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public VerifyReceiptUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<ReceiptVerificationResult>> ExecuteAsync(
        VerifyReceiptCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sale = await db.Sales
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.SaleId, cancellationToken);

        if (sale is null)
        {
            return Result<ReceiptVerificationResult>.Fail("Receipt not found.");
        }

        var lineCount = sale.Items.Count;
        var totalQuantity = sale.Items.Sum(x => x.Quantity);
        var totalKobo = sale.TotalKobo;

        db.ReceiptVerifications.Add(new ReceiptVerification
        {
            SaleId = sale.Id,
            VerifiedById = command.VerifiedById,
            Status = VerificationStatus.Verified,
            Notes = command.Notes,
            VerifiedUtc = _clock.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        return Result<ReceiptVerificationResult>.Ok(new ReceiptVerificationResult(
            true,
            sale.Id,
            lineCount,
            totalQuantity,
            totalKobo));
    }
}

public sealed record ReportSecurityIncidentCommand(
    Guid? SaleId,
    Guid? ReportedById,
    string IncidentType,
    string Details);

public sealed record SecurityIncidentResult(
    Guid IncidentId,
    string Status);

public interface IReportSecurityIncidentUseCase
    : IUseCase<ReportSecurityIncidentCommand, Result<SecurityIncidentResult>>
{
}

public sealed class ReportSecurityIncidentUseCase : IReportSecurityIncidentUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public ReportSecurityIncidentUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<SecurityIncidentResult>> ExecuteAsync(
        ReportSecurityIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.IncidentType))
        {
            return Result<SecurityIncidentResult>.Fail("Incident type is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Details))
        {
            return Result<SecurityIncidentResult>.Fail("Incident details are required.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var incident = new SecurityIncident
        {
            SaleId = command.SaleId,
            ReportedById = command.ReportedById,
            IncidentType = command.IncidentType,
            Details = command.Details,
            Status = "Open",
            CreatedUtc = _clock.UtcNow
        };

        db.SecurityIncidents.Add(incident);
        await db.SaveChangesAsync(cancellationToken);

        return Result<SecurityIncidentResult>.Ok(new SecurityIncidentResult(
            incident.Id,
            incident.Status));
    }
}