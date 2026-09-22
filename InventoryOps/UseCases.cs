using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.InventoryOps;

public sealed record RequestStockAdjustmentCommand(
    Guid ProductId,
    decimal QuantityChange,
    string Reason,
    string? Note,
    Guid? RequestedById,
    Guid? BranchId);

public sealed record StockAdjustmentRequestResult(
    Guid RequestId,
    StockAdjustmentStatus Status);

public interface IRequestStockAdjustmentUseCase
    : IUseCase<RequestStockAdjustmentCommand, Result<StockAdjustmentRequestResult>>
{
}

public sealed class RequestStockAdjustmentUseCase : IRequestStockAdjustmentUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public RequestStockAdjustmentUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<StockAdjustmentRequestResult>> ExecuteAsync(
        RequestStockAdjustmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.QuantityChange == 0m)
        {
            return Result<StockAdjustmentRequestResult>.Fail("Quantity change cannot be zero.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result<StockAdjustmentRequestResult>.Fail("Reason is required.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var productExists = await db.Products
            .AnyAsync(x => x.Id == command.ProductId, cancellationToken);

        if (!productExists)
        {
            return Result<StockAdjustmentRequestResult>.Fail("Product not found.");
        }

        var request = new StockAdjustmentRequest
        {
            ProductId = command.ProductId,
            QuantityChange = command.QuantityChange,
            Reason = command.Reason,
            Note = command.Note,
            Status = StockAdjustmentStatus.Requested,
            RequestedById = command.RequestedById,
            BranchId = command.BranchId,
            CreatedUtc = _clock.UtcNow
        };

        db.StockAdjustmentRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentRequestResult>.Ok(new StockAdjustmentRequestResult(
            request.Id,
            request.Status));
    }
}

public sealed record ApproveStockAdjustmentCommand(
    Guid RequestId,
    Guid? ApprovedById);

public interface IApproveStockAdjustmentUseCase
    : IUseCase<ApproveStockAdjustmentCommand, Result<StockAdjustmentRequestResult>>
{
}

public sealed class ApproveStockAdjustmentUseCase : IApproveStockAdjustmentUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public ApproveStockAdjustmentUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<StockAdjustmentRequestResult>> ExecuteAsync(
        ApproveStockAdjustmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var request = await db.StockAdjustmentRequests
            .FirstOrDefaultAsync(x => x.Id == command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<StockAdjustmentRequestResult>.Fail("Stock adjustment request not found.");
        }

        if (request.Status != StockAdjustmentStatus.Requested)
        {
            return Result<StockAdjustmentRequestResult>.Fail("Stock adjustment request has already been processed.");
        }

        if (!request.BranchId.HasValue)
            return Result<StockAdjustmentRequestResult>.Fail("A branch is required before stock can be posted.");

        var stock = await db.StockLevels
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId && x.BranchId == request.BranchId, cancellationToken);

        if (stock is null)
        {
            stock = new StockLevel
            {
                ProductId = request.ProductId,
                BranchId = request.BranchId.Value,
                Quantity = 0m
            };

            db.StockLevels.Add(stock);
        }

        stock.Quantity += request.QuantityChange;

        db.StockLedger.Add(new StockLedgerEntry
        {
            ProductId = request.ProductId,
            BranchId = request.BranchId.Value,
            QuantityChange = request.QuantityChange,
            Reason = StockMovementReason.Adjustment,
            Note = request.Reason,
            CreatedUtc = _clock.UtcNow
        });

        request.Status = StockAdjustmentStatus.Posted;
        request.ApprovedById = command.ApprovedById;
        request.ProcessedUtc = _clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<StockAdjustmentRequestResult>.Ok(new StockAdjustmentRequestResult(
            request.Id,
            request.Status));
    }
}

public sealed record RequestStockOverrideCommand(
    Guid ProductId,
    Guid? CashierId,
    Guid? BranchId,
    string? Reason);

public sealed record StockOverrideRequestResult(
    Guid RequestId,
    StockOverrideStatus Status);

public interface IRequestStockOverrideUseCase
    : IUseCase<RequestStockOverrideCommand, Result<StockOverrideRequestResult>>
{
}

public sealed class RequestStockOverrideUseCase : IRequestStockOverrideUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public RequestStockOverrideUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<StockOverrideRequestResult>> ExecuteAsync(
        RequestStockOverrideCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var productExists = await db.Products
            .AnyAsync(x => x.Id == command.ProductId, cancellationToken);

        if (!productExists)
        {
            return Result<StockOverrideRequestResult>.Fail("Product not found.");
        }

        var request = new StockOverrideRequest
        {
            ProductId = command.ProductId,
            CashierId = command.CashierId,
            BranchId = command.BranchId,
            Reason = command.Reason,
            Status = StockOverrideStatus.Requested,
            CreatedUtc = _clock.UtcNow
        };

        db.StockOverrideRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);

        return Result<StockOverrideRequestResult>.Ok(new StockOverrideRequestResult(
            request.Id,
            request.Status));
    }
}

public sealed record ResolveStockOverrideCommand(
    Guid RequestId,
    decimal QuantityToAdd,
    Guid? ResolvedById);

public interface IResolveStockOverrideUseCase
    : IUseCase<ResolveStockOverrideCommand, Result<StockOverrideRequestResult>>
{
}

public sealed class ResolveStockOverrideUseCase : IResolveStockOverrideUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public ResolveStockOverrideUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<StockOverrideRequestResult>> ExecuteAsync(
        ResolveStockOverrideCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.QuantityToAdd <= 0m)
        {
            return Result<StockOverrideRequestResult>.Fail("Quantity to add must be greater than zero.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var request = await db.StockOverrideRequests
            .FirstOrDefaultAsync(x => x.Id == command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<StockOverrideRequestResult>.Fail("Stock override request not found.");
        }

        if (request.Status != StockOverrideStatus.Requested)
        {
            return Result<StockOverrideRequestResult>.Fail("Stock override request has already been processed.");
        }

        if (!request.BranchId.HasValue)
            return Result<StockOverrideRequestResult>.Fail("A branch is required before stock can be posted.");

        var stock = await db.StockLevels
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId && x.BranchId == request.BranchId, cancellationToken);

        if (stock is null)
        {
            stock = new StockLevel
            {
                ProductId = request.ProductId,
                BranchId = request.BranchId.Value,
                Quantity = 0m
            };

            db.StockLevels.Add(stock);
        }

        stock.Quantity += command.QuantityToAdd;

        db.StockLedger.Add(new StockLedgerEntry
        {
            ProductId = request.ProductId,
            BranchId = request.BranchId.Value,
            QuantityChange = command.QuantityToAdd,
            Reason = StockMovementReason.Adjustment,
            Note = "POS stock override",
            CreatedUtc = _clock.UtcNow
        });

        request.Status = StockOverrideStatus.Resolved;
        request.ResolvedById = command.ResolvedById;
        request.ResolvedUtc = _clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<StockOverrideRequestResult>.Ok(new StockOverrideRequestResult(
            request.Id,
            request.Status));
    }
}