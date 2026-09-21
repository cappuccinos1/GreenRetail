using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;

namespace GreenRetail.Accounting;

public sealed record ManualJournalLineInput(
    Guid AccountId,
    long DebitKobo,
    long CreditKobo,
    string? Memo);

public sealed record CreateManualJournalCommand(
    DateTime Date,
    string? Reference,
    string? Memo,
    Guid? BranchId,
    Guid? CreatedByUserId,
    IReadOnlyList<ManualJournalLineInput> Lines);

public sealed record PostedJournalResult(
    Guid JournalId,
    string Number,
    DateTime Date,
    long TotalDebitKobo,
    long TotalCreditKobo);

public interface IPostManualJournalUseCase
    : IUseCase<CreateManualJournalCommand, Result<PostedJournalResult>>
{
}

public sealed class PostManualJournalUseCase : IPostManualJournalUseCase
{
    private readonly PosDbContext _db;
    private readonly IClock _clock;

    public PostManualJournalUseCase(PosDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<PostedJournalResult>> ExecuteAsync(
        CreateManualJournalCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
        {
            return Result<PostedJournalResult>.Fail("Journal must contain at least one line.");
        }

        if (command.Lines.Any(x => x.DebitKobo < 0 || x.CreditKobo < 0))
        {
            return Result<PostedJournalResult>.Fail("Journal amounts cannot be negative.");
        }

        if (command.Lines.Any(x => x.DebitKobo > 0 && x.CreditKobo > 0))
        {
            return Result<PostedJournalResult>.Fail("A journal line cannot contain both debit and credit.");
        }

        if (command.Lines.Any(x => x.DebitKobo == 0 && x.CreditKobo == 0))
        {
            return Result<PostedJournalResult>.Fail("Each journal line must contain a debit or credit amount.");
        }

        var totalDebit = command.Lines.Sum(x => x.DebitKobo);
        var totalCredit = command.Lines.Sum(x => x.CreditKobo);

        if (totalDebit == 0 || totalCredit == 0)
        {
            return Result<PostedJournalResult>.Fail("Journal must contain both debit and credit amounts.");
        }

        if (totalDebit != totalCredit)
        {
            return Result<PostedJournalResult>.Fail("Journal is not balanced. Total debits must equal total credits.");
        }

        var accountIds = command.Lines
            .Select(x => x.AccountId)
            .Distinct()
            .ToList();

        var accounts = await _db.Accounts
            .Where(x => accountIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (accounts.Count != accountIds.Count)
        {
            return Result<PostedJournalResult>.Fail("One or more accounts were not found.");
        }

        if (accounts.Any(x => !x.IsActive))
        {
            return Result<PostedJournalResult>.Fail("One or more accounts are inactive.");
        }

        var period = await _db.FiscalPeriods
            .FirstOrDefaultAsync(x => x.StartDate <= command.Date && x.EndDate >= command.Date, cancellationToken);

        if (period is null)
        {
            return Result<PostedJournalResult>.Fail("No fiscal period exists for the journal date.");
        }

        if (period.Status == PeriodStatus.Closed)
        {
            return Result<PostedJournalResult>.Fail("The fiscal period for this journal date is closed.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var prefix = "MAN";

            var sequence = await _db.JournalSequences
                .FirstOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);

            if (sequence is null)
            {
                sequence = new JournalSequence
                {
                    Prefix = prefix,
                    LastNumber = 0
                };

                _db.JournalSequences.Add(sequence);
            }

            sequence.LastNumber++;

            var number = $"{prefix}-{command.Date:yyyy}-{sequence.LastNumber:D6}";

            var journal = new Journal
            {
                Number = number,
                Date = command.Date,
                Reference = command.Reference,
                SourceType = AccountingSourceType.Manual,
                Memo = command.Memo,
                Status = JournalStatus.Posted,
                BranchId = command.BranchId,
                CreatedByUserId = command.CreatedByUserId,
                PostedByUserId = command.CreatedByUserId,
                PostedUtc = _clock.UtcNow,
                CreatedUtc = _clock.UtcNow
            };

            foreach (var line in command.Lines)
            {
                journal.Lines.Add(new JournalLine
                {
                    AccountId = line.AccountId,
                    DebitKobo = line.DebitKobo,
                    CreditKobo = line.CreditKobo,
                    Memo = line.Memo,
                    BranchId = command.BranchId
                });
            }

            _db.Journals.Add(journal);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<PostedJournalResult>.Ok(new PostedJournalResult(
                journal.Id,
                journal.Number,
                journal.Date,
                totalDebit,
                totalCredit));
        }
        catch (DbUpdateException)
        {
            return Result<PostedJournalResult>.Fail("Database error while posting journal.");
        }
    }
}