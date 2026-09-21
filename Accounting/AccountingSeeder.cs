using Microsoft.EntityFrameworkCore;
using GreenRetail.Data;

namespace GreenRetail.Accounting;

public static class AccountingSeeder
{
    public static async Task SeedAsync(PosDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedChartOfAccountsAsync(db, cancellationToken);
        await SeedFiscalPeriodAsync(db, cancellationToken);
    }

    private static async Task SeedChartOfAccountsAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Accounts.AnyAsync(cancellationToken))
            return;

        var assets = new Account
        {
            Code = "1000",
            Name = "Assets",
            Type = AccountType.Asset,
            SubType = "Header",
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var liabilities = new Account
        {
            Code = "2000",
            Name = "Liabilities",
            Type = AccountType.Liability,
            SubType = "Header",
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var equity = new Account
        {
            Code = "3000",
            Name = "Equity",
            Type = AccountType.Equity,
            SubType = "Header",
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var revenue = new Account
        {
            Code = "4000",
            Name = "Revenue",
            Type = AccountType.Revenue,
            SubType = "Header",
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var expenses = new Account
        {
            Code = "5000",
            Name = "Expenses",
            Type = AccountType.Expense,
            SubType = "Header",
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var cash = new Account
        {
            Code = "1010",
            Name = "Cash on Hand",
            Type = AccountType.Asset,
            SubType = "Cash",
            Parent = assets,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            IsCashAccount = true,
            CreatedUtc = DateTime.UtcNow
        };

        var bank = new Account
        {
            Code = "1020",
            Name = "Bank Account",
            Type = AccountType.Asset,
            SubType = "Bank",
            Parent = assets,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            IsBankAccount = true,
            CreatedUtc = DateTime.UtcNow
        };

        var cardReceivable = new Account
        {
            Code = "1030",
            Name = "Card Receivables",
            Type = AccountType.Asset,
            SubType = "Receivable",
            Parent = assets,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var mobileMoneyReceivable = new Account
        {
            Code = "1040",
            Name = "Mobile Money Receivables",
            Type = AccountType.Asset,
            SubType = "Receivable",
            Parent = assets,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var inventory = new Account
        {
            Code = "1100",
            Name = "Inventory Asset",
            Type = AccountType.Asset,
            SubType = "Inventory",
            Parent = assets,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var supplierPayable = new Account
        {
            Code = "2010",
            Name = "Supplier Payables",
            Type = AccountType.Liability,
            SubType = "Payable",
            Parent = liabilities,
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var storeCreditLiability = new Account
        {
            Code = "2020",
            Name = "Store Credit Liability",
            Type = AccountType.Liability,
            SubType = "CustomerCredit",
            Parent = liabilities,
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var taxPayable = new Account
        {
            Code = "2030",
            Name = "Tax Payable",
            Type = AccountType.Liability,
            SubType = "Tax",
            Parent = liabilities,
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            IsTaxAccount = true,
            CreatedUtc = DateTime.UtcNow
        };

        var ownersCapital = new Account
        {
            Code = "3010",
            Name = "Owner's Capital",
            Type = AccountType.Equity,
            SubType = "Capital",
            Parent = equity,
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var retainedEarnings = new Account
        {
            Code = "3020",
            Name = "Retained Earnings",
            Type = AccountType.Equity,
            SubType = "RetainedEarnings",
            Parent = equity,
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var openingBalanceEquity = new Account
        {
            Code = "3999",
            Name = "Opening Balance Equity",
            Type = AccountType.Equity,
            SubType = "OpeningBalance",
            Parent = equity,
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var salesRevenue = new Account
        {
            Code = "4010",
            Name = "Sales Revenue",
            Type = AccountType.Revenue,
            SubType = "Sales",
            Parent = revenue,
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var salesReturns = new Account
        {
            Code = "4020",
            Name = "Sales Returns and Allowances",
            Type = AccountType.Revenue,
            SubType = "ContraRevenue",
            Parent = revenue,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var otherIncome = new Account
        {
            Code = "4030",
            Name = "Other Income",
            Type = AccountType.Revenue,
            SubType = "OtherIncome",
            Parent = revenue,
            NormalBalance = NormalBalance.Credit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var costOfGoodsSold = new Account
        {
            Code = "5010",
            Name = "Cost of Goods Sold",
            Type = AccountType.Expense,
            SubType = "COGS",
            Parent = expenses,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var inventoryShrinkage = new Account
        {
            Code = "5020",
            Name = "Inventory Shrinkage and Damage",
            Type = AccountType.Expense,
            SubType = "Shrinkage",
            Parent = expenses,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var rentExpense = new Account
        {
            Code = "5030",
            Name = "Rent Expense",
            Type = AccountType.Expense,
            SubType = "Rent",
            Parent = expenses,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var utilitiesExpense = new Account
        {
            Code = "5040",
            Name = "Utilities Expense",
            Type = AccountType.Expense,
            SubType = "Utilities",
            Parent = expenses,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var salariesExpense = new Account
        {
            Code = "5050",
            Name = "Salaries Expense",
            Type = AccountType.Expense,
            SubType = "Payroll",
            Parent = expenses,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var bankCharges = new Account
        {
            Code = "5060",
            Name = "Bank Charges",
            Type = AccountType.Expense,
            SubType = "BankFee",
            Parent = expenses,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var transitLoss = new Account
        {
            Code = "5100",
            Name = "Transit Loss Expense",
            Type = AccountType.Expense,
            SubType = "TransitLoss",
            Parent = expenses,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        var miscellaneousExpense = new Account
        {
            Code = "5900",
            Name = "Miscellaneous Expense",
            Type = AccountType.Expense,
            SubType = "Miscellaneous",
            Parent = expenses,
            NormalBalance = NormalBalance.Debit,
            IsSystem = true,
            CreatedUtc = DateTime.UtcNow
        };

        db.Accounts.AddRange(
            assets,
            liabilities,
            equity,
            revenue,
            expenses,
            cash,
            bank,
            cardReceivable,
            mobileMoneyReceivable,
            inventory,
            supplierPayable,
            storeCreditLiability,
            taxPayable,
            ownersCapital,
            retainedEarnings,
            openingBalanceEquity,
            salesRevenue,
            salesReturns,
            otherIncome,
            costOfGoodsSold,
            inventoryShrinkage,
            rentExpense,
            utilitiesExpense,
            salariesExpense,
            bankCharges,
            transitLoss,
            miscellaneousExpense
        );

        db.SystemAccountSettings.AddRange(
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.Cash,
                Account = cash,
                Description = "Default cash account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.Bank,
                Account = bank,
                Description = "Default bank account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.CardReceivable,
                Account = cardReceivable,
                Description = "Default card receivable account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.MobileMoneyReceivable,
                Account = mobileMoneyReceivable,
                Description = "Default mobile money receivable account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.Inventory,
                Account = inventory,
                Description = "Default inventory asset account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.SupplierPayable,
                Account = supplierPayable,
                Description = "Default supplier payable account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.StoreCreditLiability,
                Account = storeCreditLiability,
                Description = "Default store credit liability account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.TaxPayable,
                Account = taxPayable,
                Description = "Default tax payable account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.SalesRevenue,
                Account = salesRevenue,
                Description = "Default sales revenue account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.SalesReturns,
                Account = salesReturns,
                Description = "Default sales returns account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.CostOfGoodsSold,
                Account = costOfGoodsSold,
                Description = "Default cost of goods sold account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.InventoryShrinkage,
                Account = inventoryShrinkage,
                Description = "Default inventory shrinkage account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.TransitLoss,
                Account = transitLoss,
                Description = "Default transit loss account"
            },
            new SystemAccountSetting
            {
                Key = SystemAccountKeys.OpeningBalanceEquity,
                Account = openingBalanceEquity,
                Description = "Default opening balance equity account"
            }
        );

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedFiscalPeriodAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.FiscalPeriods.AnyAsync(cancellationToken))
            return;

        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        db.FiscalPeriods.Add(new FiscalPeriod
        {
            Year = start.Year,
            PeriodNumber = start.Month,
            Name = start.ToString("yyyy-MM"),
            StartDate = start,
            EndDate = end,
            Status = PeriodStatus.Open,
            IsYearEnd = start.Month == 12,
            CreatedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}