using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Accounting;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Accounting;
using MiddayMistSpa.Core.Entities.Transaction;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class AccountingService : IAccountingService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<AccountingService> _logger;

    public AccountingService(SpaDbContext context, ILogger<AccountingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ============================================================================
    // Chart of Accounts
    // ============================================================================

    public async Task<ChartOfAccountResponse?> GetAccountByIdAsync(int accountId)
    {
        var account = await _context.ChartOfAccounts.FindAsync(accountId);
        if (account == null)
            return null;

        return MapToAccountResponse(account);
    }

    public async Task<PagedResponse<ChartOfAccountResponse>> SearchAccountsAsync(AccountSearchRequest request)
    {
        var query = _context.ChartOfAccounts.AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(a => a.AccountCode.Contains(request.SearchTerm) || a.AccountName.Contains(request.SearchTerm));
        }
        if (!string.IsNullOrEmpty(request.AccountType))
        {
            query = query.Where(a => a.AccountType == request.AccountType);
        }
        if (request.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync();
        var accounts = await query
            .OrderBy(a => a.AccountCode)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResponse<ChartOfAccountResponse>
        {
            Items = accounts.Select(MapToAccountResponse).ToList(),
            TotalCount = totalCount,
            Page = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<List<ChartOfAccountResponse>> GetAccountsHierarchyAsync()
    {
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.ParentAccountId == null && a.IsActive)
            .Include(a => a.ChildAccounts)
            .OrderBy(a => a.AccountCode)
            .ToListAsync();

        return accounts.Select(a => MapToAccountResponseWithChildren(a)).ToList();
    }

    public async Task<ChartOfAccountResponse> CreateAccountAsync(CreateAccountRequest request)
    {
        // Validate account type
        if (!DomainConstants.AccountTypes.IsValid(request.AccountType))
            throw new InvalidOperationException($"Invalid account type '{request.AccountType}'. Valid types: {string.Join(", ", DomainConstants.AccountTypes.All)}");

        // Check for duplicate account code
        var existing = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCode == request.AccountCode);

        if (existing != null)
            throw new InvalidOperationException($"Account with code {request.AccountCode} already exists");

        // Auto-derive NormalBalance from AccountType if not specified
        var normalBalance = request.NormalBalance ?? DeriveNormalBalance(request.AccountType);

        var account = new ChartOfAccount
        {
            AccountCode = request.AccountCode,
            AccountName = request.AccountName,
            AccountType = request.AccountType,
            NormalBalance = normalBalance,
            AccountCategory = request.AccountCategory,
            ParentAccountId = request.ParentAccountId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChartOfAccounts.Add(account);
        await _context.SaveChangesAsync();

        return MapToAccountResponse(account);
    }

    public async Task<ChartOfAccountResponse> UpdateAccountAsync(int accountId, UpdateAccountRequest request)
    {
        var account = await _context.ChartOfAccounts.FindAsync(accountId);
        if (account == null)
            throw new InvalidOperationException("Account not found");

        if (!string.IsNullOrEmpty(request.AccountName))
            account.AccountName = request.AccountName;
        if (request.AccountCategory != null)
            account.AccountCategory = request.AccountCategory;
        if (request.IsActive.HasValue)
            account.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return MapToAccountResponse(account);
    }

    public async Task<bool> DeleteAccountAsync(int accountId)
    {
        var account = await _context.ChartOfAccounts.FindAsync(accountId);
        if (account == null)
            return false;

        // Check if account has journal entries
        var hasEntries = await _context.JournalEntryLines
            .AnyAsync(l => l.AccountId == accountId);

        if (hasEntries)
            throw new InvalidOperationException("Cannot delete account with journal entries. Deactivate instead.");

        _context.ChartOfAccounts.Remove(account);
        await _context.SaveChangesAsync();
        return true;
    }

    // ============================================================================
    // Journal Entries
    // ============================================================================

    public async Task<JournalEntryResponse?> GetJournalEntryByIdAsync(int journalEntryId)
    {
        var entry = await _context.JournalEntries
            .Include(j => j.Lines)
            .ThenInclude(l => l.Account)
            .Include(j => j.CreatedByUser)
            .FirstOrDefaultAsync(j => j.JournalEntryId == journalEntryId);

        if (entry == null)
            return null;

        return MapToJournalEntryResponse(entry);
    }

    public async Task<PagedResponse<JournalEntryResponse>> SearchJournalEntriesAsync(JournalEntrySearchRequest request)
    {
        var query = _context.JournalEntries.AsQueryable();

        if (request.StartDate.HasValue)
            query = query.Where(j => j.EntryDate >= request.StartDate.Value);
        if (request.EndDate.HasValue)
            query = query.Where(j => j.EntryDate <= request.EndDate.Value);
        if (!string.IsNullOrEmpty(request.ReferenceType))
            query = query.Where(j => j.ReferenceType == request.ReferenceType);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(j => j.Status == request.Status);
        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(j => j.EntryNumber.Contains(request.SearchTerm) ||
                                     (j.Description != null && j.Description.Contains(request.SearchTerm)));

        if (request.AccountId.HasValue)
        {
            var entryIds = await _context.JournalEntryLines
                .Where(l => l.AccountId == request.AccountId.Value)
                .Select(l => l.JournalEntryId)
                .Distinct()
                .ToListAsync();

            query = query.Where(j => entryIds.Contains(j.JournalEntryId));
        }

        var totalCount = await query.CountAsync();
        var entries = await query
            .Include(j => j.Lines)
            .ThenInclude(l => l.Account)
            .Include(j => j.CreatedByUser)
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.EntryNumber)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResponse<JournalEntryResponse>
        {
            Items = entries.Select(MapToJournalEntryResponse).ToList(),
            TotalCount = totalCount,
            Page = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<JournalEntryResponse> CreateJournalEntryAsync(CreateJournalEntryRequest request, int userId)
    {
        // Validate lines balance
        var totalDebits = request.Lines.Sum(l => l.DebitAmount);
        var totalCredits = request.Lines.Sum(l => l.CreditAmount);

        if (Math.Abs(totalDebits - totalCredits) > 0.001m)
            throw new InvalidOperationException($"Journal entry is not balanced. Debits: {totalDebits:C}, Credits: {totalCredits:C}");

        if (totalDebits == 0)
            throw new InvalidOperationException("Journal entry must have at least one debit and credit");

        // Validate each line has either debit OR credit, not both
        foreach (var line in request.Lines)
        {
            if (line.DebitAmount > 0 && line.CreditAmount > 0)
                throw new InvalidOperationException("A journal entry line cannot have both a debit and credit amount");
            if (line.DebitAmount < 0 || line.CreditAmount < 0)
                throw new InvalidOperationException("Debit and credit amounts must be non-negative");
        }

        // Validate all referenced accounts exist and are active
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _context.ChartOfAccounts
            .Where(a => accountIds.Contains(a.AccountId))
            .ToListAsync();

        if (accounts.Count != accountIds.Count)
        {
            var missingIds = accountIds.Except(accounts.Select(a => a.AccountId));
            throw new InvalidOperationException($"Account(s) not found: {string.Join(", ", missingIds)}");
        }

        var inactiveAccounts = accounts.Where(a => !a.IsActive).Select(a => a.AccountCode).ToList();
        if (inactiveAccounts.Any())
            throw new InvalidOperationException($"Cannot post to inactive account(s): {string.Join(", ", inactiveAccounts)}");

        // Validate status
        var status = request.Status ?? "Draft";
        if (status != "Draft" && status != "Posted")
            throw new InvalidOperationException("Status must be 'Draft' or 'Posted'");

        // Generate entry number with retry to handle race conditions
        var entryNumber = await GenerateEntryNumberAsync();

        var journalEntry = new JournalEntry
        {
            EntryNumber = entryNumber,
            EntryDate = request.EntryDate,
            Description = request.Description,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            TotalDebit = totalDebits,
            TotalCredit = totalCredits,
            Status = status,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var line in request.Lines)
        {
            journalEntry.Lines.Add(new JournalEntryLine
            {
                AccountId = line.AccountId,
                DebitAmount = line.DebitAmount,
                CreditAmount = line.CreditAmount,
                Description = line.Description,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.JournalEntries.Add(journalEntry);
        await _context.SaveChangesAsync();

        return await GetJournalEntryByIdAsync(journalEntry.JournalEntryId) ?? throw new InvalidOperationException("Failed to retrieve created entry");
    }

    public async Task<JournalEntryResponse> PostJournalEntryAsync(int journalEntryId, int userId)
    {
        var entry = await _context.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.JournalEntryId == journalEntryId);

        if (entry == null)
            throw new InvalidOperationException("Journal entry not found");

        if (entry.Status == "Posted")
            throw new InvalidOperationException("Journal entry is already posted");

        if (entry.Status == "Voided")
            throw new InvalidOperationException("Cannot post a voided journal entry");

        // Re-validate balance before posting
        if (Math.Abs(entry.TotalDebit - entry.TotalCredit) > 0.001m)
            throw new InvalidOperationException("Cannot post an unbalanced journal entry");

        entry.Status = "Posted";
        await _context.SaveChangesAsync();

        return await GetJournalEntryByIdAsync(journalEntryId) ?? throw new InvalidOperationException("Failed to retrieve posted entry");
    }

    public async Task<JournalEntryResponse> VoidJournalEntryAsync(int journalEntryId, int userId, string? reason = null)
    {
        var entry = await _context.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.JournalEntryId == journalEntryId);

        if (entry == null)
            throw new InvalidOperationException("Journal entry not found");

        if (entry.Status == "Voided")
            throw new InvalidOperationException("Journal entry is already voided");

        // Mark original as voided
        entry.Status = "Voided";
        entry.VoidedBy = userId;
        entry.VoidedAt = DateTime.UtcNow;
        entry.VoidReason = reason;

        // Create a reversing entry for audit trail (only if the original was Posted)
        if (entry.Lines.Any())
        {
            var reversalNumber = await GenerateEntryNumberAsync();

            var reversalEntry = new JournalEntry
            {
                EntryNumber = reversalNumber,
                EntryDate = DateTime.UtcNow.Date,
                Description = $"Reversal of {entry.EntryNumber}: {reason ?? "Voided"}",
                ReferenceType = "Reversal",
                ReferenceId = entry.EntryNumber,
                TotalDebit = entry.TotalDebit,
                TotalCredit = entry.TotalCredit,
                Status = "Posted",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                ReversalOfEntryId = entry.JournalEntryId
            };

            // Swap debits and credits to reverse the original
            foreach (var line in entry.Lines)
            {
                reversalEntry.Lines.Add(new JournalEntryLine
                {
                    AccountId = line.AccountId,
                    DebitAmount = line.CreditAmount,   // Swap
                    CreditAmount = line.DebitAmount,   // Swap
                    Description = $"Reversal: {line.Description}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.JournalEntries.Add(reversalEntry);
        }

        await _context.SaveChangesAsync();

        return await GetJournalEntryByIdAsync(journalEntryId) ?? throw new InvalidOperationException("Failed to retrieve voided entry");
    }

    // ============================================================================
    // Financial Reports
    // ============================================================================

    public async Task<TrialBalanceResponse> GetTrialBalanceAsync(DateTime asOfDate)
    {
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.IsActive)
            .Include(a => a.JournalEntryLines)
            .ThenInclude(l => l.JournalEntry)
            .OrderBy(a => a.AccountCode)
            .ToListAsync();

        var items = new List<TrialBalanceLineItem>();
        decimal totalDebits = 0;
        decimal totalCredits = 0;

        foreach (var account in accounts)
        {
            var entries = account.JournalEntryLines
                .Where(l => l.JournalEntry.EntryDate <= asOfDate && l.JournalEntry.Status == "Posted");

            var debitSum = entries.Sum(l => l.DebitAmount);
            var creditSum = entries.Sum(l => l.CreditAmount);
            var balance = debitSum - creditSum;

            // Only include accounts with activity
            if (debitSum > 0 || creditSum > 0)
            {
                var item = new TrialBalanceLineItem
                {
                    AccountId = account.AccountId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    AccountType = account.AccountType,
                    DebitBalance = balance > 0 ? balance : 0,
                    CreditBalance = balance < 0 ? Math.Abs(balance) : 0
                };

                items.Add(item);
                totalDebits += item.DebitBalance;
                totalCredits += item.CreditBalance;
            }
        }

        return new TrialBalanceResponse
        {
            AsOfDate = asOfDate,
            Items = items,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            IsBalanced = totalDebits == totalCredits
        };
    }

    public async Task<IncomeStatementResponse> GetIncomeStatementAsync(DateTime startDate, DateTime endDate)
    {
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.IsActive && (a.AccountType == "Revenue" || a.AccountType == "Expense"))
            .Include(a => a.JournalEntryLines)
            .ThenInclude(l => l.JournalEntry)
            .OrderBy(a => a.AccountCode)
            .ToListAsync();

        var revenueSection = new IncomeStatementSection { SectionName = "Revenue" };
        var expenseSection = new IncomeStatementSection { SectionName = "Expenses" };

        foreach (var account in accounts.Where(a => a.AccountType == "Revenue"))
        {
            var amount = CalculateAccountBalance(account, startDate, endDate);
            // Contra-revenue accounts (debit-normal like Sales Returns) reduce revenue
            if (account.NormalBalance == "Debit") amount = -amount;
            if (amount != 0)
            {
                revenueSection.Items.Add(new IncomeStatementLineItem
                {
                    AccountId = account.AccountId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Amount = amount
                });
            }
        }
        revenueSection.Total = revenueSection.Items.Sum(i => i.Amount);

        foreach (var account in accounts.Where(a => a.AccountType == "Expense"))
        {
            var amount = CalculateAccountBalance(account, startDate, endDate);
            if (amount != 0)
            {
                expenseSection.Items.Add(new IncomeStatementLineItem
                {
                    AccountId = account.AccountId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Amount = amount // Already positive for debit-normal accounts
                });
            }
        }
        expenseSection.Total = expenseSection.Items.Sum(i => i.Amount);

        return new IncomeStatementResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            Sections = new List<IncomeStatementSection> { revenueSection, expenseSection },
            TotalRevenue = revenueSection.Total,
            TotalExpenses = expenseSection.Total,
            NetIncome = revenueSection.Total - expenseSection.Total
        };
    }

    public async Task<BalanceSheetResponse> GetBalanceSheetAsync(DateTime asOfDate)
    {
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.IsActive && (a.AccountType == "Asset" || a.AccountType == "Liability" || a.AccountType == "Equity"))
            .Include(a => a.JournalEntryLines)
            .ThenInclude(l => l.JournalEntry)
            .OrderBy(a => a.AccountCode)
            .ToListAsync();

        var assetSection = new BalanceSheetSection { SectionName = "Assets" };
        var liabilitySection = new BalanceSheetSection { SectionName = "Liabilities" };
        var equitySection = new BalanceSheetSection { SectionName = "Equity" };

        foreach (var account in accounts.Where(a => a.AccountType == "Asset"))
        {
            var amount = CalculateAccountBalance(account, null, asOfDate);
            if (amount != 0)
            {
                assetSection.Items.Add(new BalanceSheetLineItem
                {
                    AccountId = account.AccountId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Category = account.AccountCategory,
                    Amount = amount
                });
            }
        }
        assetSection.Total = assetSection.Items.Sum(i => i.Amount);

        foreach (var account in accounts.Where(a => a.AccountType == "Liability"))
        {
            var amount = CalculateAccountBalance(account, null, asOfDate);
            if (amount != 0)
            {
                liabilitySection.Items.Add(new BalanceSheetLineItem
                {
                    AccountId = account.AccountId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Category = account.AccountCategory,
                    Amount = amount // Already positive for credit-normal accounts
                });
            }
        }
        liabilitySection.Total = liabilitySection.Items.Sum(i => i.Amount);

        foreach (var account in accounts.Where(a => a.AccountType == "Equity"))
        {
            var amount = CalculateAccountBalance(account, null, asOfDate);
            if (amount != 0)
            {
                equitySection.Items.Add(new BalanceSheetLineItem
                {
                    AccountId = account.AccountId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Category = account.AccountCategory,
                    Amount = amount // Already positive for credit-normal accounts
                });
            }
        }
        equitySection.Total = equitySection.Items.Sum(i => i.Amount);

        return new BalanceSheetResponse
        {
            AsOfDate = asOfDate,
            Assets = assetSection,
            Liabilities = liabilitySection,
            Equity = equitySection,
            TotalAssets = assetSection.Total,
            TotalLiabilities = liabilitySection.Total,
            TotalEquity = equitySection.Total,
            IsBalanced = assetSection.Total == (liabilitySection.Total + equitySection.Total)
        };
    }

    public async Task<AccountLedgerResponse> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate)
    {
        var account = await _context.ChartOfAccounts.FindAsync(accountId);
        if (account == null)
            throw new InvalidOperationException("Account not found");

        var lines = await _context.JournalEntryLines
            .Where(l => l.AccountId == accountId && l.JournalEntry.Status == "Posted" && l.JournalEntry.EntryDate <= endDate)
            .Include(l => l.JournalEntry)
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ThenBy(l => l.JournalEntry.EntryNumber)
            .ToListAsync();

        // Calculate running balance respecting normal balance direction
        bool isDebitNormal = account.NormalBalance == "Debit";
        int sign = isDebitNormal ? 1 : -1;

        var openingBalance = lines
            .Where(l => l.JournalEntry.EntryDate < startDate)
            .Sum(l => (l.DebitAmount - l.CreditAmount) * sign);

        var periodEntries = lines.Where(l => l.JournalEntry.EntryDate >= startDate && l.JournalEntry.EntryDate <= endDate);
        var ledgerEntries = new List<LedgerEntry>();
        var runningBalance = openingBalance;

        foreach (var line in periodEntries)
        {
            runningBalance += (line.DebitAmount - line.CreditAmount) * sign;
            ledgerEntries.Add(new LedgerEntry
            {
                Date = line.JournalEntry.EntryDate,
                EntryNumber = line.JournalEntry.EntryNumber,
                Description = line.Description ?? line.JournalEntry.Description,
                Debit = line.DebitAmount,
                Credit = line.CreditAmount,
                Balance = runningBalance
            });
        }

        return new AccountLedgerResponse
        {
            AccountId = account.AccountId,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            StartDate = startDate,
            EndDate = endDate,
            OpeningBalance = openingBalance,
            ClosingBalance = runningBalance,
            Entries = ledgerEntries
        };
    }

    // ============================================================================
    // Expense/Income Tracking
    // ============================================================================

    public async Task<PagedResponse<ExpenseResponse>> GetExpensesAsync(DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
    {
        var query = _context.JournalEntries
            .Where(j => j.ReferenceType == "Expense" && j.Status != "Voided");

        if (startDate.HasValue)
            query = query.Where(j => j.EntryDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(j => j.EntryDate <= endDate.Value);

        var totalCount = await query.CountAsync();
        var entries = await query
            .Include(j => j.Lines)
            .ThenInclude(l => l.Account)
            .Include(j => j.CreatedByUser)
            .OrderByDescending(j => j.EntryDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entries.Select(e =>
        {
            var expenseLine = e.Lines.FirstOrDefault(l => l.Account.AccountType == "Expense");
            return new ExpenseResponse
            {
                JournalEntryId = e.JournalEntryId,
                EntryNumber = e.EntryNumber,
                Date = e.EntryDate,
                Description = e.Description,
                Vendor = e.ReferenceId,
                Category = expenseLine?.Account.AccountName ?? "Uncategorized",
                Amount = e.TotalDebit,
                Status = e.Status,
                CreatedByName = $"{e.CreatedByUser.FirstName} {e.CreatedByUser.LastName}"
            };
        }).ToList();

        return new PagedResponse<ExpenseResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, int userId)
    {
        var journalRequest = new CreateJournalEntryRequest
        {
            EntryDate = request.Date,
            Description = request.Description,
            ReferenceType = "Expense",
            ReferenceId = request.Vendor,
            Lines = new List<CreateJournalLineRequest>
            {
                new() { AccountId = request.ExpenseAccountId, DebitAmount = request.Amount, CreditAmount = 0 },
                new() { AccountId = request.PaymentAccountId, DebitAmount = 0, CreditAmount = request.Amount }
            }
        };

        var entry = await CreateJournalEntryAsync(journalRequest, userId);

        var expenseAccount = await _context.ChartOfAccounts.FindAsync(request.ExpenseAccountId);
        var createdByUser = await _context.Users.FindAsync(userId);

        return new ExpenseResponse
        {
            JournalEntryId = entry.JournalEntryId,
            EntryNumber = entry.EntryNumber,
            Date = entry.EntryDate,
            Description = entry.Description,
            Vendor = request.Vendor,
            Category = expenseAccount?.AccountName ?? "Uncategorized",
            Amount = request.Amount,
            Status = entry.Status,
            CreatedByName = createdByUser != null ? $"{createdByUser.FirstName} {createdByUser.LastName}" : "Unknown"
        };
    }

    public async Task<PagedResponse<IncomeRecordResponse>> GetIncomeRecordsAsync(DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
    {
        var query = _context.JournalEntries
            .Where(j => j.ReferenceType == "Income" && j.Status != "Voided");

        if (startDate.HasValue)
            query = query.Where(j => j.EntryDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(j => j.EntryDate <= endDate.Value);

        var totalCount = await query.CountAsync();
        var entries = await query
            .Include(j => j.Lines)
            .ThenInclude(l => l.Account)
            .OrderByDescending(j => j.EntryDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entries.Select(e =>
        {
            var revenueLine = e.Lines.FirstOrDefault(l => l.Account.AccountType == "Revenue");
            return new IncomeRecordResponse
            {
                JournalEntryId = e.JournalEntryId,
                EntryNumber = e.EntryNumber,
                Date = e.EntryDate,
                Description = e.Description,
                Customer = e.ReferenceId,
                Category = revenueLine?.Account.AccountName ?? "Uncategorized",
                Amount = e.TotalCredit,
                Status = e.Status
            };
        }).ToList();

        return new PagedResponse<IncomeRecordResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IncomeRecordResponse> CreateIncomeRecordAsync(CreateIncomeRequest request, int userId)
    {
        var journalRequest = new CreateJournalEntryRequest
        {
            EntryDate = request.Date,
            Description = request.Description,
            ReferenceType = "Income",
            ReferenceId = request.CustomerName,
            Lines = new List<CreateJournalLineRequest>
            {
                new() { AccountId = request.DepositAccountId, DebitAmount = request.Amount, CreditAmount = 0 },
                new() { AccountId = request.RevenueAccountId, DebitAmount = 0, CreditAmount = request.Amount }
            }
        };

        var entry = await CreateJournalEntryAsync(journalRequest, userId);
        var revenueAccount = await _context.ChartOfAccounts.FindAsync(request.RevenueAccountId);

        return new IncomeRecordResponse
        {
            JournalEntryId = entry.JournalEntryId,
            EntryNumber = entry.EntryNumber,
            Date = entry.EntryDate,
            Description = entry.Description,
            Customer = request.CustomerName,
            Category = revenueAccount?.AccountName ?? "Uncategorized",
            Amount = request.Amount,
            Status = entry.Status
        };
    }

    // ============================================================================
    // Auto Journal Entries (from operations)
    // ============================================================================

    /// <summary>
    /// Create a journal entry for a completed (paid) transaction.
    /// DR: Cash/Bank (payment account)
    /// CR: Service Revenue / Product Revenue
    /// Looks up accounts by AccountCategory convention.
    /// </summary>
    public async Task CreateTransactionJournalEntryAsync(int transactionId, int userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.ServiceItems).ThenInclude(si => si.Service)
            .Include(t => t.ProductItems).ThenInclude(pi => pi.Product)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        // Determine payment account based on payment method
        var paymentCategory = transaction.PaymentMethod switch
        {
            "Cash" => "Cash",
            "Card" or "GCash" or "Maya" => "Bank",
            "Bank Transfer" => "Bank",
            _ => "Cash"
        };

        var paymentAccount = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == paymentCategory && a.AccountType == "Asset" && a.IsActive);

        var serviceRevenueAccount = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Service Revenue" && a.IsActive);

        var productRevenueAccount = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Product Revenue" && a.IsActive);

        // If accounts aren't set up yet, skip auto-JE silently
        if (paymentAccount == null || (serviceRevenueAccount == null && productRevenueAccount == null))
        {
            _logger.LogWarning("Skipping auto-JE for transaction {TransactionId}: required chart of accounts not set up", transactionId);
            return;
        }

        var lines = new List<CreateJournalLineRequest>();
        var totalAmount = transaction.TotalAmount;

        // Debit: Payment account for total
        lines.Add(new CreateJournalLineRequest
        {
            AccountId = paymentAccount.AccountId,
            DebitAmount = totalAmount,
            CreditAmount = 0,
            Description = $"Payment received - {transaction.PaymentMethod}"
        });

        // Credit: Revenue accounts
        var serviceTotal = transaction.ServiceItems?.Sum(si => si.UnitPrice * si.Quantity) ?? 0;
        var productTotal = transaction.ProductItems?.Sum(pi => pi.UnitPrice * pi.Quantity) ?? 0;
        var discountAmount = transaction.DiscountAmount;

        // Distribute discount proportionally
        var grossTotal = serviceTotal + productTotal;
        if (grossTotal > 0 && discountAmount > 0)
        {
            serviceTotal -= discountAmount * (serviceTotal / grossTotal);
            productTotal -= discountAmount * (productTotal / grossTotal);
        }

        if (serviceTotal > 0 && serviceRevenueAccount != null)
        {
            lines.Add(new CreateJournalLineRequest
            {
                AccountId = serviceRevenueAccount.AccountId,
                DebitAmount = 0,
                CreditAmount = Math.Round(serviceTotal, 2),
                Description = "Service revenue"
            });
        }

        if (productTotal > 0 && productRevenueAccount != null)
        {
            lines.Add(new CreateJournalLineRequest
            {
                AccountId = productRevenueAccount.AccountId,
                DebitAmount = 0,
                CreditAmount = Math.Round(productTotal, 2),
                Description = "Product sales revenue"
            });
        }

        // Handle rounding: ensure debits == credits
        var totalCredits = lines.Where(l => l.CreditAmount > 0).Sum(l => l.CreditAmount);
        if (totalCredits != totalAmount && lines.Count > 1)
        {
            var lastCreditLine = lines.Last(l => l.CreditAmount > 0);
            lastCreditLine.CreditAmount += totalAmount - totalCredits;
        }

        // If we only have a payment account but no revenue breakdown, credit a generic revenue
        if (lines.Count == 1)
        {
            var genericRevenue = serviceRevenueAccount ?? productRevenueAccount;
            if (genericRevenue != null)
            {
                lines.Add(new CreateJournalLineRequest
                {
                    AccountId = genericRevenue.AccountId,
                    DebitAmount = 0,
                    CreditAmount = totalAmount,
                    Description = "Revenue"
                });
            }
            else return; // Can't create unbalanced JE
        }

        var customerName = transaction.Customer != null
            ? $"{transaction.Customer.FirstName} {transaction.Customer.LastName}"
            : "Walk-in";

        var request = new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow.Date,
            Description = $"Transaction #{transaction.TransactionId} - {customerName}",
            ReferenceType = "Transaction",
            ReferenceId = transaction.TransactionId.ToString(),
            Status = "Posted",
            Lines = lines
        };

        await CreateJournalEntryAsync(request, userId);
        _logger.LogInformation("Auto-JE created for transaction {TransactionId}, amount {Amount}", transactionId, totalAmount);
    }

    /// <summary>
    /// Create a reversing journal entry for a refund.
    /// DR: Revenue accounts (refund reduces revenue)
    /// CR: Payment account (cash/bank going out)
    /// </summary>
    public async Task CreateRefundJournalEntryAsync(int transactionId, int refundId, decimal refundAmount, string refundMethod, string reason, int userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.ServiceItems).ThenInclude(si => si.Service)
            .Include(t => t.ProductItems).ThenInclude(pi => pi.Product)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        // Determine payment account based on refund method
        var paymentCategory = refundMethod switch
        {
            "Cash" => "Cash",
            "Card Reversal" or "GCash" or "Maya" => "Bank",
            _ => "Cash"
        };

        var paymentAccount = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == paymentCategory && a.AccountType == "Asset" && a.IsActive);

        // Use contra-revenue accounts (Sales Returns & Allowances) instead of direct revenue accounts
        var serviceReturnsAccount = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Service Returns" && a.IsActive);

        var productReturnsAccount = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Product Returns" && a.IsActive);

        // Fallback to direct revenue accounts if contra-revenue not set up
        var serviceRevenueAccount = serviceReturnsAccount ?? await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Service Revenue" && a.IsActive);

        var productRevenueAccount = productReturnsAccount ?? await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Product Revenue" && a.IsActive);

        if (paymentAccount == null || (serviceRevenueAccount == null && productRevenueAccount == null))
        {
            _logger.LogWarning("Skipping refund JE for transaction {TransactionId}: required chart of accounts not set up", transactionId);
            return;
        }

        var lines = new List<CreateJournalLineRequest>();

        // Calculate proportional split between services and products
        var serviceTotal = transaction.ServiceItems?.Sum(si => si.UnitPrice * si.Quantity) ?? 0;
        var productTotal = transaction.ProductItems?.Sum(pi => pi.UnitPrice * pi.Quantity) ?? 0;
        var grossTotal = serviceTotal + productTotal;

        if (grossTotal > 0)
        {
            var serviceRefund = Math.Round(refundAmount * (serviceTotal / grossTotal), 2);
            var productRefund = refundAmount - serviceRefund;

            // Debit: Contra-revenue accounts (Sales Returns & Allowances)
            if (serviceRefund > 0 && serviceRevenueAccount != null)
            {
                lines.Add(new CreateJournalLineRequest
                {
                    AccountId = serviceRevenueAccount.AccountId,
                    DebitAmount = serviceRefund,
                    CreditAmount = 0,
                    Description = "Sales return - Service"
                });
            }

            if (productRefund > 0 && productRevenueAccount != null)
            {
                lines.Add(new CreateJournalLineRequest
                {
                    AccountId = productRevenueAccount.AccountId,
                    DebitAmount = productRefund,
                    CreditAmount = 0,
                    Description = "Sales return - Product"
                });
            }
        }
        else
        {
            // Fallback: debit generic returns account
            var genericReturns = serviceRevenueAccount ?? productRevenueAccount;
            if (genericReturns != null)
            {
                lines.Add(new CreateJournalLineRequest
                {
                    AccountId = genericReturns.AccountId,
                    DebitAmount = refundAmount,
                    CreditAmount = 0,
                    Description = "Sales return"
                });
            }
        }

        // Credit: Payment account (money going out)
        lines.Add(new CreateJournalLineRequest
        {
            AccountId = paymentAccount.AccountId,
            DebitAmount = 0,
            CreditAmount = refundAmount,
            Description = $"Refund issued - {refundMethod}"
        });

        // Ensure debits == credits
        var totalDebits = lines.Where(l => l.DebitAmount > 0).Sum(l => l.DebitAmount);
        var totalCredits = lines.Where(l => l.CreditAmount > 0).Sum(l => l.CreditAmount);
        if (totalDebits != totalCredits && lines.Any(l => l.DebitAmount > 0))
        {
            var lastDebitLine = lines.Last(l => l.DebitAmount > 0);
            lastDebitLine.DebitAmount += totalCredits - totalDebits;
        }

        var customerName = transaction.Customer != null
            ? $"{transaction.Customer.FirstName} {transaction.Customer.LastName}"
            : "Walk-in";

        var request = new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow.Date,
            Description = $"Refund - Transaction #{transaction.TransactionNumber} - {customerName} ({reason})",
            ReferenceType = "Refund",
            ReferenceId = transaction.TransactionId.ToString(),
            Status = "Posted",
            Lines = lines
        };

        await CreateJournalEntryAsync(request, userId);
        _logger.LogInformation("Refund JE created for transaction {TransactionId}, refund amount {Amount}", transactionId, refundAmount);
    }

    /// <summary>
    /// Create a journal entry for a finalized payroll period.
    /// DR: Salary Expense (gross pay)
    /// CR: SSS Payable, PhilHealth Payable, Pag-IBIG Payable, Tax Payable, Cash/Bank (net pay)
    /// </summary>
    public async Task CreatePayrollJournalEntryAsync(int payrollPeriodId, int userId)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.PayrollPeriodId == payrollPeriodId);

        if (period == null)
            throw new InvalidOperationException("Payroll period not found");

        var records = period.PayrollRecords.ToList();
        if (!records.Any()) return;

        // Look up required accounts by category
        var salaryExpenseAcct = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Salary Expense" && a.IsActive);
        var sssPayableAcct = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "SSS Payable" && a.IsActive);
        var philhealthPayableAcct = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "PhilHealth Payable" && a.IsActive);
        var pagibigPayableAcct = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Pag-IBIG Payable" && a.IsActive);
        var taxPayableAcct = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Withholding Tax Payable" && a.IsActive);
        var cashAcct = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Cash" && a.AccountType == "Asset" && a.IsActive);

        if (salaryExpenseAcct == null || cashAcct == null)
        {
            _logger.LogWarning("Skipping auto-JE for payroll period {PeriodId}: required chart of accounts not set up", payrollPeriodId);
            return;
        }

        var totalGrossPay = records.Sum(r => r.GrossPay);
        var totalSSS = records.Sum(r => r.SSSContribution);
        var totalPhilHealth = records.Sum(r => r.PhilHealthContribution);
        var totalPagIBIG = records.Sum(r => r.PagIBIGContribution);
        var totalTax = records.Sum(r => r.WithholdingTax);
        var totalNetPay = records.Sum(r => r.NetPay);

        var lines = new List<CreateJournalLineRequest>
        {
            // DR: Salary Expense
            new() { AccountId = salaryExpenseAcct.AccountId, DebitAmount = totalGrossPay, CreditAmount = 0, Description = "Gross payroll" }
        };

        // CR: Government contribution payables (if accounts exist)
        if (sssPayableAcct != null && totalSSS > 0)
            lines.Add(new() { AccountId = sssPayableAcct.AccountId, DebitAmount = 0, CreditAmount = totalSSS, Description = "SSS employee share" });

        if (philhealthPayableAcct != null && totalPhilHealth > 0)
            lines.Add(new() { AccountId = philhealthPayableAcct.AccountId, DebitAmount = 0, CreditAmount = totalPhilHealth, Description = "PhilHealth employee share" });

        if (pagibigPayableAcct != null && totalPagIBIG > 0)
            lines.Add(new() { AccountId = pagibigPayableAcct.AccountId, DebitAmount = 0, CreditAmount = totalPagIBIG, Description = "Pag-IBIG employee share" });

        if (taxPayableAcct != null && totalTax > 0)
            lines.Add(new() { AccountId = taxPayableAcct.AccountId, DebitAmount = 0, CreditAmount = totalTax, Description = "Withholding tax" });

        // CR: Cash for net pay
        var totalCredited = lines.Where(l => l.CreditAmount > 0).Sum(l => l.CreditAmount);
        var cashCredit = totalGrossPay - totalCredited;
        if (cashCredit > 0)
            lines.Add(new() { AccountId = cashAcct.AccountId, DebitAmount = 0, CreditAmount = cashCredit, Description = "Net pay disbursement" });

        var request = new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow.Date,
            Description = $"Payroll - {period.PeriodName} ({period.StartDate:MMM d} - {period.EndDate:MMM d, yyyy})",
            ReferenceType = "Payroll",
            ReferenceId = payrollPeriodId.ToString(),
            Status = "Posted",
            Lines = lines
        };

        await CreateJournalEntryAsync(request, userId);
        _logger.LogInformation("Auto-JE created for payroll period {PeriodId}, gross pay {GrossPay}", payrollPeriodId, totalGrossPay);
    }

    /// <summary>
    /// Create a journal entry when a purchase order is received.
    /// DR: Inventory (cost of goods received)
    /// CR: Accounts Payable (supplier liability)
    /// </summary>
    public async Task CreatePurchaseOrderJournalEntryAsync(int purchaseOrderId, int userId)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId);

        if (po == null)
            throw new InvalidOperationException("Purchase order not found");

        var inventoryAcct = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Inventory" && a.AccountType == "Asset" && a.IsActive);
        var apAcct = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCategory == "Accounts Payable" && a.IsActive);

        if (inventoryAcct == null || apAcct == null)
        {
            _logger.LogWarning("Skipping auto-JE for PO {PONumber}: Inventory or AP account not set up", po.PONumber);
            return;
        }

        var totalAmount = po.TotalAmount;

        var lines = new List<CreateJournalLineRequest>
        {
            // DR: Inventory
            new() { AccountId = inventoryAcct.AccountId, DebitAmount = totalAmount, CreditAmount = 0, Description = $"Inventory received - {po.Supplier.SupplierName}" },
            // CR: Accounts Payable
            new() { AccountId = apAcct.AccountId, DebitAmount = 0, CreditAmount = totalAmount, Description = $"Payable to {po.Supplier.SupplierName}" }
        };

        var jeRequest = new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow.Date,
            Description = $"PO #{po.PONumber} received from {po.Supplier.SupplierName}",
            ReferenceType = "PurchaseOrder",
            ReferenceId = purchaseOrderId.ToString(),
            Status = "Posted",
            Lines = lines
        };

        await CreateJournalEntryAsync(jeRequest, userId);
        _logger.LogInformation("Auto-JE created for PO {PONumber}, amount {Amount}", po.PONumber, totalAmount);
    }

    // ============================================================================
    // Dashboard/Summary
    // ============================================================================

    public async Task<AccountingSummaryResponse> GetAccountingSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var incomeStatement = await GetIncomeStatementAsync(startDate, endDate);

        // Get cash accounts balance
        var cashAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountCategory == "Cash" || a.AccountCategory == "Bank")
            .Include(a => a.JournalEntryLines)
            .ThenInclude(l => l.JournalEntry)
            .ToListAsync();

        var cashOnHand = cashAccounts.Sum(a => CalculateAccountBalance(a, null, endDate));

        // Get AR balance
        var arAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountCategory == "Accounts Receivable")
            .Include(a => a.JournalEntryLines)
            .ThenInclude(l => l.JournalEntry)
            .ToListAsync();

        var accountsReceivable = arAccounts.Sum(a => CalculateAccountBalance(a, null, endDate));

        // Get AP balance
        var apAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountCategory == "Accounts Payable")
            .Include(a => a.JournalEntryLines)
            .ThenInclude(l => l.JournalEntry)
            .ToListAsync();

        var accountsPayable = apAccounts.Sum(a => CalculateAccountBalance(a, null, endDate));

        // Get monthly trend
        var monthlyTrend = new List<MonthlyTrendItem>();
        var monthStart = new DateTime(startDate.Year, startDate.Month, 1);
        while (monthStart <= endDate)
        {
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            if (monthEnd > endDate) monthEnd = endDate;

            var monthlyIncome = await GetIncomeStatementAsync(monthStart, monthEnd);
            monthlyTrend.Add(new MonthlyTrendItem
            {
                Month = monthStart.ToString("MMM yyyy"),
                Revenue = monthlyIncome.TotalRevenue,
                Expenses = monthlyIncome.TotalExpenses,
                NetIncome = monthlyIncome.NetIncome
            });

            monthStart = monthStart.AddMonths(1);
        }

        // Get expense breakdown by category
        var expenseAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountType == "Expense" && a.IsActive)
            .Include(a => a.JournalEntryLines)
            .ThenInclude(l => l.JournalEntry)
            .ToListAsync();

        var expensesByCategory = expenseAccounts
            .Select(a => new { Category = a.AccountCategory ?? a.AccountName, Amount = CalculateAccountBalance(a, startDate, endDate) })
            .Where(x => x.Amount > 0)
            .GroupBy(x => x.Category)
            .Select(g => new ExpenseCategoryItem
            {
                Category = g.Key,
                Amount = g.Sum(x => x.Amount),
                Percentage = 0
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalExpenses = expensesByCategory.Sum(x => x.Amount);
        foreach (var item in expensesByCategory)
        {
            item.Percentage = totalExpenses > 0 ? (item.Amount / totalExpenses) * 100 : 0;
        }

        // Get revenue streams
        var revenueAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountType == "Revenue" && a.IsActive)
            .Include(a => a.JournalEntryLines)
            .ThenInclude(l => l.JournalEntry)
            .ToListAsync();

        // Map contra-revenue categories to their parent revenue categories for netting
        var contraRevenueMap = new Dictionary<string, string>
        {
            { "Service Returns", "Service Revenue" },
            { "Product Returns", "Product Revenue" }
        };

        var revenueStreams = revenueAccounts
            .Select(a =>
            {
                var amount = CalculateAccountBalance(a, startDate, endDate);
                // Contra-revenue (debit-normal) reduces revenue
                if (a.NormalBalance == "Debit") amount = -amount;
                var category = a.AccountCategory ?? a.AccountName;
                // Map contra-revenue to parent category so they net out
                if (contraRevenueMap.TryGetValue(category, out var parentCategory))
                    category = parentCategory;
                return new { Stream = category, Amount = amount };
            })
            .Where(x => x.Amount != 0)
            .GroupBy(x => x.Stream)
            .Select(g => new RevenueStreamItem
            {
                Stream = g.Key,
                Amount = g.Sum(x => x.Amount),
                Percentage = 0
            })
            .Where(x => x.Amount > 0) // Only show categories with positive net revenue
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalRevenue = revenueStreams.Sum(x => x.Amount);
        foreach (var item in revenueStreams)
        {
            item.Percentage = totalRevenue > 0 ? (item.Amount / totalRevenue) * 100 : 0;
        }

        return new AccountingSummaryResponse
        {
            TotalRevenue = incomeStatement.TotalRevenue,
            TotalExpenses = incomeStatement.TotalExpenses,
            NetIncome = incomeStatement.NetIncome,
            CashOnHand = cashOnHand,
            AccountsReceivable = accountsReceivable,
            AccountsPayable = accountsPayable,
            MonthlyTrend = monthlyTrend,
            ExpensesByCategory = expensesByCategory,
            RevenueStreams = revenueStreams
        };
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    /// <summary>
    /// Calculate account balance respecting normal balance direction.
    /// Debit-normal (Asset, Expense): balance = Debits - Credits
    /// Credit-normal (Liability, Equity, Revenue): balance = Credits - Debits
    /// Result is always positive when the account is in its normal state.
    /// </summary>
    private decimal CalculateAccountBalance(ChartOfAccount account, DateTime? startDate, DateTime? endDate)
    {
        var entries = account.JournalEntryLines
            .Where(l => l.JournalEntry.Status == "Posted" &&
                       (!startDate.HasValue || l.JournalEntry.EntryDate >= startDate.Value) &&
                       (!endDate.HasValue || l.JournalEntry.EntryDate <= endDate.Value));

        var rawBalance = entries.Sum(l => l.DebitAmount - l.CreditAmount);

        // Flip sign for credit-normal accounts so balances display as positive
        return account.NormalBalance == "Credit" ? -rawBalance : rawBalance;
    }

    /// <summary>
    /// Derive the normal balance direction from the account type.
    /// Assets and Expenses are debit-normal, others are credit-normal.
    /// </summary>
    private static string DeriveNormalBalance(string accountType) => accountType switch
    {
        "Asset" or "Expense" => "Debit",
        "Liability" or "Equity" or "Revenue" => "Credit",
        _ => "Debit"
    };

    /// <summary>
    /// Generate a unique entry number with retry to handle race conditions.
    /// </summary>
    private async Task<string> GenerateEntryNumberAsync()
    {
        var now = DateTime.UtcNow;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            var entryCount = await _context.JournalEntries
                .CountAsync(j => j.EntryNumber.StartsWith($"JE-{now:yyyyMM}-"));

            var candidate = $"JE-{now:yyyyMM}-{(entryCount + 1 + attempt):D4}";
            var exists = await _context.JournalEntries.AnyAsync(j => j.EntryNumber == candidate);
            if (!exists) return candidate;
        }

        // Fallback: use timestamp-based unique number
        return $"JE-{now:yyyyMMdd}-{now:HHmmss}-{Guid.NewGuid().ToString()[..4]}";
    }

    private ChartOfAccountResponse MapToAccountResponse(ChartOfAccount account)
    {
        return new ChartOfAccountResponse
        {
            AccountId = account.AccountId,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            AccountType = account.AccountType,
            NormalBalance = account.NormalBalance,
            AccountCategory = account.AccountCategory,
            ParentAccountId = account.ParentAccountId,
            ParentAccountName = account.ParentAccount?.AccountName,
            IsActive = account.IsActive,
            CreatedAt = account.CreatedAt,
            Balance = 0 // Calculated on-demand via ledger/trial balance
        };
    }

    private ChartOfAccountResponse MapToAccountResponseWithChildren(ChartOfAccount account)
    {
        var response = MapToAccountResponse(account);
        response.ChildAccounts = account.ChildAccounts
            .OrderBy(c => c.AccountCode)
            .Select(MapToAccountResponseWithChildren)
            .ToList();
        return response;
    }

    private JournalEntryResponse MapToJournalEntryResponse(JournalEntry entry)
    {
        return new JournalEntryResponse
        {
            JournalEntryId = entry.JournalEntryId,
            EntryNumber = entry.EntryNumber,
            EntryDate = entry.EntryDate,
            Description = entry.Description,
            ReferenceType = entry.ReferenceType,
            ReferenceId = entry.ReferenceId,
            TotalDebit = entry.TotalDebit,
            TotalCredit = entry.TotalCredit,
            IsBalanced = entry.IsBalanced,
            Status = entry.Status,
            CreatedBy = entry.CreatedBy,
            CreatedByName = entry.CreatedByUser != null ? $"{entry.CreatedByUser.FirstName} {entry.CreatedByUser.LastName}" : "Unknown",
            CreatedAt = entry.CreatedAt,
            VoidedBy = entry.VoidedBy,
            VoidedByName = entry.VoidedByUser != null ? $"{entry.VoidedByUser.FirstName} {entry.VoidedByUser.LastName}" : null,
            VoidedAt = entry.VoidedAt,
            VoidReason = entry.VoidReason,
            ReversalOfEntryId = entry.ReversalOfEntryId,
            Lines = entry.Lines.Select(l => new JournalEntryLineResponse
            {
                JournalLineId = l.JournalLineId,
                AccountId = l.AccountId,
                AccountCode = l.Account?.AccountCode ?? "",
                AccountName = l.Account?.AccountName ?? "",
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description = l.Description
            }).ToList()
        };
    }
}
