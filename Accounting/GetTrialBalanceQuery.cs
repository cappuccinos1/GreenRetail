using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;

namespace GreenRetail.Accounting;

public sealed record TrialBalanceQueryRequest(DateTime? AsOfUtc);

public sealed record TrialBalanceLine(
    string AccountCode,
    string AccountName,
    AccountType AccountType,
    NormalBalance NormalBalance,
    long DebitKobo,
    long CreditKobo,
    long BalanceKobo);

public sealed record TrialBalanceResult(
    DateTime AsOfUtc,
    long TotalDebitKobo,
    long TotalCreditKobo,
    IReadOnlyList<TrialBalanceLine> Lines);

public interface IGetTrialBalanceQuery
    : IUseCase<TrialBalanceQueryRequest, Result<TrialBalanceResult>>
{
}

public sealed class GetTrialBalanceQuery : IGetTrialBalanceQuery
{
    private readonly PosDbContext _db;
    private readonly IClock _clock;

    public GetTrialBalanceQuery(PosDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<TrialBalanceResult>> ExecuteAsync(
        TrialBalanceQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var asOf = request.AsOfUtc ?? _clock.UtcNow;

        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var movements = await _db.JournalLines
            .AsNoTracking()
            .Where(x => x.Journal!.Status == JournalStatus.Posted && x.Journal.Date <= asOf)
            .GroupBy(x => x.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Debit = g.Sum(x => x.DebitKobo),
                Credit = g.Sum(x => x.CreditKobo)
            })
            .ToListAsync(cancellationToken);

        var movementLookup = movements.ToDictionary(x => x.AccountId);

        var lines = new List<TrialBalanceLine>();

        foreach (var account in accounts)
        {
            var debit = 0L;
            var credit = 0L;

            if (movementLookup.TryGetValue(account.Id, out var movement))
            {
                debit = movement.Debit;
                credit = movement.Credit;
            }

            var balance = debit - credit;

            lines.Add(new TrialBalanceLine(
                account.Code,
                account.Name,
                account.Type,
                account.NormalBalance,
                debit,
                credit,
                balance));
        }

        var totalDebit = lines.Sum(x => x.DebitKobo);
        var totalCredit = lines.Sum(x => x.CreditKobo);

        return Result<TrialBalanceResult>.Ok(new TrialBalanceResult(
            asOf,
            totalDebit,
            totalCredit,
            lines));
    }
}