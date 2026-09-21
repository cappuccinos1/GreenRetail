using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Data;

namespace GreenRetail.Features.Catalog;

public sealed record SearchProductsQueryRequest(string? SearchText);

public sealed record ProductSummary(
    Guid Id,
    string Name,
    string Sku,
    Money SellingPrice,
    bool IsWeighed,
    string UnitAbbreviation);

public interface ISearchProductsQuery
    : IUseCase<SearchProductsQueryRequest, Result<IReadOnlyList<ProductSummary>>>
{
}

public sealed class SearchProductsQuery : ISearchProductsQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public SearchProductsQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<IReadOnlyList<ProductSummary>>> ExecuteAsync(
        SearchProductsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Data.Entities.Product> query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var term = request.SearchText.Trim();

            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Sku.Contains(term) ||
                p.Barcodes.Any(b => b.Value == term));
        }

        var projection = await query
            .OrderBy(p => p.Name)
            .Take(100)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Sku,
                p.SellingPrice,
                p.IsWeighed,
                UnitAbbreviation = p.Unit != null ? p.Unit.Abbreviation : string.Empty
            })
            .ToListAsync(cancellationToken);

        var products = projection
            .Select(x => new ProductSummary(
                x.Id,
                x.Name,
                x.Sku,
                Money.FromNaira(x.SellingPrice),
                x.IsWeighed,
                x.UnitAbbreviation))
            .ToList();

        return Result<IReadOnlyList<ProductSummary>>.Ok(products);
    }
}