using Microsoft.EntityFrameworkCore;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Accounting;

public interface IAccountingPostingService
{
    Task PostSaleAsync(PosDbContext db, Sale sale, CancellationToken ct = default);
}

public sealed class AccountingPostingService : IAccountingPostingService
{
    public async Task PostSaleAsync(PosDbContext db, Sale sale, CancellationToken ct = default)
    {
        // 1. Fetch System Accounts (Standard Codes seeded by AccountingSeeder)
        var cashAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Code == "1000" && a.IsActive, ct);
        var salesAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Code == "4000" && a.IsActive, ct);
        var inventoryAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Code == "1200" && a.IsActive, ct);
        var cogsAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Code == "5000" && a.IsActive, ct);

        if (cashAccount is null || salesAccount is null || inventoryAccount is null || cogsAccount is null)
        {
            throw new InvalidOperationException("System accounting configuration is incomplete. Missing standard accounts.");
        }

        // 2. Calculate COGS (Cost of Goods Sold) in Kobo
        long totalCogsKobo = 0;
        foreach (var item in sale.Items)
        {
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);
            if (product != null)
            {
                // Convert decimal CostPrice to Kobo
                long costKobo = (long)Math.Round(product.CostPrice * 100m, MidpointRounding.AwayFromZero);
                totalCogsKobo += costKobo * (long)item.Quantity;
            }
        }

        // 3. Generate Journal Number
        var sequence = await db.JournalSequences.FirstOrDefaultAsync(s => s.Prefix == "JRN", ct);
        if (sequence is null)
        {
            sequence = new JournalSequence { Prefix = "JRN", LastNumber = 0 };
            db.JournalSequences.Add(sequence);
        }
        sequence.LastNumber++;
        var journalNumber = $"JRN-{DateTime.UtcNow:yyyyMMdd}-{sequence.LastNumber:D5}";

        // 4. Create Journal Header
        var journal = new Journal
        {
            Number = journalNumber,
            Date = sale.CreatedUtc,
            Reference = $"Sale {sale.IdempotencyKey}",
            SourceType = AccountingSourceType.Sale,
            SourceId = sale.Id,
            Memo = $"Automated posting for sale by {sale.CashierName}",
            Status = JournalStatus.Posted,
            BranchId = null, // TODO: Resolve from Terminal context
            PostedByUserId = sale.CashierId,
            PostedUtc = sale.CreatedUtc,
            CreatedUtc = sale.CreatedUtc
        };

        // 5. Create Journal Lines (Double Entry)
        
        // Dr Cash (Asset increases)
        journal.Lines.Add(new JournalLine
        {
            AccountId = cashAccount.Id,
            DebitKobo = sale.PaidKobo,
            CreditKobo = 0,
            Memo = "Cash received"
        });

        // Cr Sales Revenue (Revenue increases)
        journal.Lines.Add(new JournalLine
        {
            AccountId = salesAccount.Id,
            DebitKobo = 0,
            CreditKobo = sale.SubtotalKobo,
            Memo = "Sales revenue"
        });

        // Dr COGS (Expense increases) - Only if we have cost data
        if (totalCogsKobo > 0)
        {
            journal.Lines.Add(new JournalLine
            {
                AccountId = cogsAccount.Id,
                DebitKobo = totalCogsKobo,
                CreditKobo = 0,
                Memo = "Cost of goods sold"
            });

            // Cr Inventory (Asset decreases)
            journal.Lines.Add(new JournalLine
            {
                AccountId = inventoryAccount.Id,
                DebitKobo = 0,
                CreditKobo = totalCogsKobo,
                Memo = "Inventory relieved"
            });
        }

        // 6. Invariant Check: Debits MUST equal Credits
        long totalDebits = journal.Lines.Sum(l => l.DebitKobo);
        long totalCredits = journal.Lines.Sum(l => l.CreditKobo);

        if (totalDebits != totalCredits)
        {
            throw new InvalidOperationException($"Accounting invariant violation: Debits ({totalDebits}) do not equal Credits ({totalCredits}) for Sale {sale.Id}.");
        }

        // 7. Add to DbContext (Will be saved when the UseCase commits the transaction)
        db.Journals.Add(journal);
    }
}