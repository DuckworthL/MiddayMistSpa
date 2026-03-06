using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Transaction;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Customer;
using MiddayMistSpa.Core.Entities.Transaction;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class TransactionService : ITransactionService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<TransactionService> _logger;
    private readonly IAccountingService _accountingService;
    private readonly IInventoryService _inventoryService;
    private readonly ICurrencyService _currencyService;
    private readonly INotificationService _notificationService;
    private const decimal TAX_RATE = 0.12m; // 12% VAT in Philippines

    public TransactionService(SpaDbContext context, ILogger<TransactionService> logger, IAccountingService accountingService, IInventoryService inventoryService, ICurrencyService currencyService, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _accountingService = accountingService;
        _inventoryService = inventoryService;
        _currencyService = currencyService;
        _notificationService = notificationService;
    }

    // ============================================================================
    // Transaction CRUD
    // ============================================================================

    public async Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, int cashierId)
    {
        // Validate at least one item exists
        if ((request.ServiceItems == null || request.ServiceItems.Count == 0) &&
            (request.ProductItems == null || request.ProductItems.Count == 0))
            throw new InvalidOperationException("Transaction must contain at least one service or product item");

        // Validate customer
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer == null)
            throw new InvalidOperationException("Customer not found");

        // Validate appointment if provided
        if (request.AppointmentId.HasValue)
        {
            var appointment = await _context.Appointments.FindAsync(request.AppointmentId.Value);
            if (appointment == null)
                throw new InvalidOperationException("Appointment not found");
        }

        // Validate discount cap against membership tier
        if (request.DiscountPercentage > 0)
        {
            var tierDiscount = DomainConstants.MembershipTiers.GetTierDiscount(customer.MembershipType) * 100;
            if (request.DiscountPercentage > tierDiscount && tierDiscount > 0)
                throw new InvalidOperationException(
                    $"Discount {request.DiscountPercentage}% exceeds maximum allowed for {customer.MembershipType} tier ({tierDiscount}%)");
        }

        // Wrap entire creation in a database transaction for atomicity
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        // Generate transaction number
        var transactionNumber = await GenerateTransactionNumberAsync();

        // Create transaction
        var transaction = new Transaction
        {
            TransactionNumber = transactionNumber,
            CustomerId = request.CustomerId,
            AppointmentId = request.AppointmentId,
            CashierId = cashierId,
            DiscountPercentage = request.DiscountPercentage,
            DiscountAmount = request.DiscountAmount,
            TipAmount = request.TipAmount,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "Paid",
            TransactionDate = PhilippineTime.Now,
            CreatedAt = DateTime.UtcNow
        };

        // Add service items
        decimal serviceSubtotal = 0;
        foreach (var item in request.ServiceItems ?? [])
        {
            var service = await _context.Services.FindAsync(item.ServiceId);
            if (service == null)
                throw new InvalidOperationException($"Service with ID {item.ServiceId} not found");

            var unitPrice = item.UnitPrice ?? service.RegularPrice;
            var totalPrice = unitPrice * item.Quantity;
            serviceSubtotal += totalPrice;

            // Get therapist commission rate from service
            decimal commissionRate = service.TherapistCommissionRate;

            transaction.ServiceItems.Add(new TransactionServiceItem
            {
                ServiceId = item.ServiceId,
                TherapistId = item.TherapistId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = totalPrice,
                CommissionRate = commissionRate,
                CommissionAmount = totalPrice * commissionRate,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Add product items
        decimal productSubtotal = 0;
        foreach (var item in request.ProductItems ?? [])
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product with ID {item.ProductId} not found");

            if (product.CurrentStock < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for product {product.ProductName}");

            var unitPrice = item.UnitPrice ?? product.SellingPrice ?? product.CostPrice;
            var totalPrice = unitPrice * item.Quantity;
            productSubtotal += totalPrice;

            transaction.ProductItems.Add(new TransactionProductItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = totalPrice,
                CommissionRate = product.RetailCommissionRate,
                CommissionAmount = totalPrice * product.RetailCommissionRate,
                CreatedAt = DateTime.UtcNow
            });

            // Deduct stock with audit trail
            product.CurrentStock -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
        }

        // Calculate totals
        transaction.Subtotal = serviceSubtotal + productSubtotal;

        // Apply discount
        decimal discountTotal = request.DiscountAmount;
        if (request.DiscountPercentage > 0)
        {
            discountTotal += transaction.Subtotal * (request.DiscountPercentage / 100);
        }

        // Apply loyalty points redemption (1 point = ₱1 discount)
        int pointsRedeemed = 0;
        if (request.LoyaltyPointsToRedeem > 0)
        {
            if (request.LoyaltyPointsToRedeem > customer.LoyaltyPoints)
                throw new InvalidOperationException(
                    $"Customer only has {customer.LoyaltyPoints} loyalty points available");

            decimal maxRedeemable = transaction.Subtotal - discountTotal;
            decimal pointsValue = Math.Min(request.LoyaltyPointsToRedeem, maxRedeemable);
            pointsRedeemed = (int)Math.Floor(pointsValue);
            discountTotal += pointsRedeemed;
        }

        transaction.DiscountAmount = discountTotal;

        // Calculate tax (on discounted amount)
        var taxableAmount = transaction.Subtotal - discountTotal;
        transaction.TaxAmount = taxableAmount * TAX_RATE;

        // Calculate total
        transaction.TotalAmount = taxableAmount + transaction.TaxAmount + request.TipAmount;

        // Calculate change for cash payments
        if (request.PaymentMethod == DomainConstants.PaymentMethods.Cash && request.AmountTendered.HasValue)
        {
            if (request.AmountTendered.Value < transaction.TotalAmount)
                throw new InvalidOperationException(
                    $"Amount tendered (₱{request.AmountTendered.Value:N2}) is less than total (₱{transaction.TotalAmount:N2})");

            transaction.AmountTendered = request.AmountTendered.Value;
            transaction.ChangeAmount = request.AmountTendered.Value - transaction.TotalAmount;
        }

        // Earn loyalty points (1 point per ₱100 spent)
        EarnLoyaltyPoints(customer, transaction);

        // Deduct redeemed points from customer balance
        if (pointsRedeemed > 0)
        {
            DeductLoyaltyPoints(customer, pointsRedeemed,
                DomainConstants.LoyaltyTransactionTypes.Redeem,
                $"Redeemed {pointsRedeemed} pts on transaction {transaction.TransactionNumber}",
                null);
        }

        // Multi-currency: populate entity fields if client uses a non-PHP currency
        var clientCurrency = request.ClientCurrency?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(clientCurrency) && clientCurrency != "PHP")
        {
            try
            {
                var conversion = await _currencyService.ConvertAsync(new MiddayMistSpa.API.DTOs.Currency.ConvertCurrencyRequest
                {
                    Amount = transaction.TotalAmount,
                    FromCurrency = "PHP",
                    ToCurrency = clientCurrency
                });
                transaction.ClientCurrency = clientCurrency;
                transaction.ExchangeRate = conversion.ExchangeRate;
                transaction.TotalInClientCurrency = conversion.ConvertedAmount;
                transaction.ClientIPAddress = request.ClientIPAddress;

                // Try to resolve country code from IP
                if (!string.IsNullOrEmpty(request.ClientIPAddress))
                {
                    try
                    {
                        var clientInfo = await _currencyService.DetectClientInfoAsync(request.ClientIPAddress);
                        transaction.ClientCountryCode = clientInfo.CountryCode;
                    }
                    catch { /* non-critical */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert transaction to {Currency}. Recording as PHP only.", clientCurrency);
                // Leave defaults (PHP, rate 1.0, no converted amount)
            }
        }

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Create stock adjustment audit records for product sales
        foreach (var item in transaction.ProductItems)
        {
            _context.StockAdjustments.Add(new MiddayMistSpa.Core.Entities.Inventory.StockAdjustment
            {
                ProductId = item.ProductId,
                AdjustmentType = "Sold",
                QuantityBefore = item.Product.CurrentStock + item.Quantity, // before deduction
                QuantityChange = -item.Quantity,
                QuantityAfter = item.Product.CurrentStock,
                Reason = "Retail sale",
                ReferenceNumber = transaction.TransactionNumber,
                AdjustedBy = cashierId,
                CreatedAt = DateTime.UtcNow
            });
        }
        if (transaction.ProductItems.Any())
            await _context.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        // Fire low-stock / out-of-stock notifications for products that dropped at or below reorder level
        foreach (var item in transaction.ProductItems)
        {
            if (item.Product.CurrentStock <= item.Product.ReorderLevel)
            {
                try
                {
                    await _notificationService.SendLowStockAlertAsync(item.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send low-stock notification for product {ProductId}", item.ProductId);
                }
            }
        }

        // Auto-create journal entry for the paid transaction (debit/credit)
        try
        {
            await _accountingService.CreateTransactionJournalEntryAsync(transaction.TransactionId, cashierId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create auto-JE for transaction {TransactionId}. GL may need manual entry.", transaction.TransactionId);
        }

        var result = await GetTransactionByIdAsync(transaction.TransactionId);
        return result!;
    }

    /// <summary>
    /// Creates a Pending transaction when an appointment service starts (In Progress).
    /// No stock deduction, no journal entry, no loyalty points — those happen at finalization.
    /// </summary>
    public async Task<TransactionResponse> CreatePendingTransactionForAppointmentAsync(int appointmentId, int cashierId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Service)
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Appointment not found");

        // Check if a pending transaction already exists for this appointment
        var existing = await _context.Transactions
            .FirstOrDefaultAsync(t => t.AppointmentId == appointmentId && t.PaymentStatus == "Pending");
        if (existing != null)
        {
            var existingResult = await GetTransactionByIdAsync(existing.TransactionId);
            return existingResult!;
        }

        var transactionNumber = await GenerateTransactionNumberAsync();

        var transaction = new Transaction
        {
            TransactionNumber = transactionNumber,
            CustomerId = appointment.CustomerId,
            AppointmentId = appointmentId,
            CashierId = cashierId,
            PaymentMethod = "Pending",
            PaymentStatus = "Pending",
            TransactionDate = PhilippineTime.Now,
            CreatedAt = DateTime.UtcNow
        };

        // Build service items from appointment service items (multi-service) or fallback to primary service
        decimal serviceSubtotal = 0;
        if (appointment.ServiceItems.Any())
        {
            foreach (var si in appointment.ServiceItems)
            {
                var unitPrice = si.UnitPrice;
                var totalPrice = unitPrice * si.Quantity;
                serviceSubtotal += totalPrice;

                decimal commissionRate = si.Service?.TherapistCommissionRate ?? 0;
                transaction.ServiceItems.Add(new TransactionServiceItem
                {
                    ServiceId = si.ServiceId,
                    TherapistId = appointment.TherapistId,
                    Quantity = si.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    CommissionRate = commissionRate,
                    CommissionAmount = totalPrice * commissionRate,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (appointment.Service != null)
        {
            var unitPrice = appointment.Service.RegularPrice;
            serviceSubtotal = unitPrice;
            decimal commissionRate = appointment.Service.TherapistCommissionRate;
            transaction.ServiceItems.Add(new TransactionServiceItem
            {
                ServiceId = appointment.ServiceId,
                TherapistId = appointment.TherapistId,
                Quantity = 1,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice,
                CommissionRate = commissionRate,
                CommissionAmount = unitPrice * commissionRate,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Calculate totals (no discount, no tip yet — those come at finalization)
        transaction.Subtotal = serviceSubtotal;
        transaction.TaxAmount = serviceSubtotal * TAX_RATE;
        transaction.TotalAmount = serviceSubtotal + transaction.TaxAmount;

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created pending transaction {TransactionNumber} for appointment {AppointmentId}",
            transactionNumber, appointmentId);

        var result = await GetTransactionByIdAsync(transaction.TransactionId);
        return result!;
    }

    /// <summary>
    /// Finalizes a Pending transaction: adds products, sets payment, deducts stock,
    /// earns loyalty points, and creates journal entry. Transitions Pending → Paid.
    /// </summary>
    public async Task<TransactionResponse> FinalizePendingTransactionAsync(int transactionId, FinalizePendingTransactionRequest request, int cashierId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.ServiceItems)
            .Include(t => t.ProductItems)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        if (transaction.PaymentStatus != "Pending")
            throw new InvalidOperationException($"Cannot finalize transaction with status: {transaction.PaymentStatus}");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        // Add product items
        decimal productSubtotal = 0;
        foreach (var item in request.ProductItems ?? [])
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product with ID {item.ProductId} not found");

            if (product.CurrentStock < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for product {product.ProductName}");

            var unitPrice = item.UnitPrice ?? product.SellingPrice ?? product.CostPrice;
            var totalPrice = unitPrice * item.Quantity;
            productSubtotal += totalPrice;

            transaction.ProductItems.Add(new TransactionProductItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = totalPrice,
                CommissionRate = product.RetailCommissionRate,
                CommissionAmount = totalPrice * product.RetailCommissionRate,
                CreatedAt = DateTime.UtcNow
            });

            // Deduct stock
            product.CurrentStock -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
        }

        // Recalculate totals
        var serviceSubtotal = transaction.ServiceItems.Sum(si => si.TotalPrice);
        transaction.Subtotal = serviceSubtotal + productSubtotal;

        // Load customer (needed for loyalty points)
        var customer = await _context.Customers.FindAsync(transaction.CustomerId);

        // Apply discount
        decimal discountTotal = request.DiscountAmount;
        if (request.DiscountPercentage > 0)
            discountTotal += transaction.Subtotal * (request.DiscountPercentage / 100);

        // Apply loyalty points redemption (1 point = ₱1 discount)
        int pointsRedeemed = 0;
        if (request.LoyaltyPointsToRedeem > 0 && customer != null)
        {
            if (request.LoyaltyPointsToRedeem > customer.LoyaltyPoints)
                throw new InvalidOperationException(
                    $"Customer only has {customer.LoyaltyPoints} loyalty points available");

            decimal maxRedeemable = transaction.Subtotal - discountTotal;
            decimal pointsValue = Math.Min(request.LoyaltyPointsToRedeem, maxRedeemable);
            pointsRedeemed = (int)Math.Floor(pointsValue);
            discountTotal += pointsRedeemed;
        }

        transaction.DiscountAmount = discountTotal;
        transaction.DiscountPercentage = request.DiscountPercentage;

        // Tax on discounted amount
        var taxableAmount = transaction.Subtotal - discountTotal;
        transaction.TaxAmount = taxableAmount * TAX_RATE;

        // Tip
        transaction.TipAmount = request.TipAmount;

        // Total
        transaction.TotalAmount = taxableAmount + transaction.TaxAmount + request.TipAmount;

        // Payment
        transaction.PaymentMethod = request.PaymentMethod;
        transaction.PaymentStatus = "Paid";
        transaction.CashierId = cashierId;
        transaction.UpdatedAt = DateTime.UtcNow;

        // Calculate change for cash payments
        if (request.PaymentMethod == DomainConstants.PaymentMethods.Cash && request.AmountTendered.HasValue)
        {
            if (request.AmountTendered.Value < transaction.TotalAmount)
                throw new InvalidOperationException(
                    $"Amount tendered (₱{request.AmountTendered.Value:N2}) is less than total (₱{transaction.TotalAmount:N2})");

            transaction.AmountTendered = request.AmountTendered.Value;
            transaction.ChangeAmount = request.AmountTendered.Value - transaction.TotalAmount;
        }

        // Earn loyalty points
        if (customer != null)
        {
            EarnLoyaltyPoints(customer, transaction);

            // Deduct redeemed points from customer balance
            if (pointsRedeemed > 0)
            {
                DeductLoyaltyPoints(customer, pointsRedeemed,
                    DomainConstants.LoyaltyTransactionTypes.Redeem,
                    $"Redeemed {pointsRedeemed} pts on transaction {transaction.TransactionNumber}",
                    null);
            }
        }

        // Multi-currency
        var clientCurrency = request.ClientCurrency?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(clientCurrency) && clientCurrency != "PHP")
        {
            try
            {
                var conversion = await _currencyService.ConvertAsync(new MiddayMistSpa.API.DTOs.Currency.ConvertCurrencyRequest
                {
                    Amount = transaction.TotalAmount,
                    FromCurrency = "PHP",
                    ToCurrency = clientCurrency
                });
                transaction.ClientCurrency = clientCurrency;
                transaction.ExchangeRate = conversion.ExchangeRate;
                transaction.TotalInClientCurrency = conversion.ConvertedAmount;
                transaction.ClientIPAddress = request.ClientIPAddress;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert transaction to {Currency}.", clientCurrency);
            }
        }

        await _context.SaveChangesAsync();

        // Create stock adjustment audit records
        foreach (var item in transaction.ProductItems.Where(p => request.ProductItems?.Any(r => r.ProductId == p.ProductId) == true))
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                _context.StockAdjustments.Add(new MiddayMistSpa.Core.Entities.Inventory.StockAdjustment
                {
                    ProductId = item.ProductId,
                    AdjustmentType = "Sold",
                    QuantityBefore = product.CurrentStock + item.Quantity,
                    QuantityChange = -item.Quantity,
                    QuantityAfter = product.CurrentStock,
                    Reason = "Retail sale",
                    ReferenceNumber = transaction.TransactionNumber,
                    AdjustedBy = cashierId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        if (transaction.ProductItems.Any())
            await _context.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        // Fire low-stock / out-of-stock notifications for products that dropped at or below reorder level
        foreach (var item in transaction.ProductItems.Where(p => request.ProductItems?.Any(r => r.ProductId == p.ProductId) == true))
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null && product.CurrentStock <= product.ReorderLevel)
            {
                try
                {
                    await _notificationService.SendLowStockAlertAsync(item.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send low-stock notification for product {ProductId}", item.ProductId);
                }
            }
        }

        // Auto-create journal entry
        try
        {
            await _accountingService.CreateTransactionJournalEntryAsync(transaction.TransactionId, cashierId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create auto-JE for transaction {TransactionId}.", transaction.TransactionId);
        }

        _logger.LogInformation("Finalized pending transaction {TransactionId} to Paid", transactionId);

        var result = await GetTransactionByIdAsync(transaction.TransactionId);
        return result!;
    }

    public async Task<TransactionResponse?> GetPendingTransactionByAppointmentAsync(int appointmentId)
    {
        var transaction = await _context.Transactions
            .AsQueryable()
            .Include(t => t.Customer)
            .Include(t => t.Cashier)
            .Include(t => t.ServiceItems).ThenInclude(si => si.Service)
            .Include(t => t.ServiceItems).ThenInclude(si => si.Therapist)
            .Include(t => t.ProductItems).ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(t => t.AppointmentId == appointmentId && t.PaymentStatus == "Pending");

        if (transaction == null)
            return null;

        return MapToResponse(transaction);
    }

    public async Task<TransactionResponse?> GetTransactionByIdAsync(int transactionId)
    {
        var transaction = await _context.Transactions
            .AsQueryable()
            .Include(t => t.Customer)
            .Include(t => t.Cashier)
            .Include(t => t.VoidedByUser)
            .Include(t => t.ServiceItems)
                .ThenInclude(si => si.Service)
            .Include(t => t.ServiceItems)
                .ThenInclude(si => si.Therapist)
            .Include(t => t.ProductItems)
                .ThenInclude(pi => pi.Product)
            .Include(t => t.Refunds)
                .ThenInclude(r => r.ApprovedByUser)
            .Include(t => t.Refunds)
                .ThenInclude(r => r.ProcessedByUser)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            return null;

        return MapToResponse(transaction);
    }

    public async Task<TransactionResponse?> GetTransactionByNumberAsync(string transactionNumber)
    {
        var transaction = await _context.Transactions
            .AsQueryable()
            .Include(t => t.Customer)
            .Include(t => t.Cashier)
            .Include(t => t.VoidedByUser)
            .Include(t => t.ServiceItems)
                .ThenInclude(si => si.Service)
            .Include(t => t.ServiceItems)
                .ThenInclude(si => si.Therapist)
            .Include(t => t.ProductItems)
                .ThenInclude(pi => pi.Product)
            .Include(t => t.Refunds)
            .FirstOrDefaultAsync(t => t.TransactionNumber == transactionNumber);

        if (transaction == null)
            return null;

        return MapToResponse(transaction);
    }

    public async Task<PagedResponse<TransactionListResponse>> SearchTransactionsAsync(TransactionSearchRequest request)
    {
        var query = _context.Transactions
            .AsQueryable()
            .Include(t => t.Customer)
            .Include(t => t.ServiceItems)
            .Include(t => t.ProductItems)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(t =>
                t.TransactionNumber.ToLower().Contains(term) ||
                (t.Customer.FirstName + " " + t.Customer.LastName).ToLower().Contains(term));
        }

        if (request.CustomerId.HasValue)
            query = query.Where(t => t.CustomerId == request.CustomerId);

        if (request.CashierId.HasValue)
            query = query.Where(t => t.CashierId == request.CashierId);

        if (request.DateFrom.HasValue)
            query = query.Where(t => t.TransactionDate >= request.DateFrom.Value.Date);

        if (request.DateTo.HasValue)
            query = query.Where(t => t.TransactionDate < request.DateTo.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            query = query.Where(t => t.PaymentStatus == request.PaymentStatus);

        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            query = query.Where(t => t.PaymentMethod == request.PaymentMethod);

        if (request.MinAmount.HasValue)
            query = query.Where(t => t.TotalAmount >= request.MinAmount.Value);

        if (request.MaxAmount.HasValue)
            query = query.Where(t => t.TotalAmount <= request.MaxAmount.Value);

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "amount" => request.SortDescending
                ? query.OrderByDescending(t => t.TotalAmount)
                : query.OrderBy(t => t.TotalAmount),
            "customer" => request.SortDescending
                ? query.OrderByDescending(t => t.Customer.LastName)
                : query.OrderBy(t => t.Customer.LastName),
            "number" => request.SortDescending
                ? query.OrderByDescending(t => t.TransactionNumber)
                : query.OrderBy(t => t.TransactionNumber),
            _ => request.SortDescending
                ? query.OrderByDescending(t => t.TransactionDate)
                : query.OrderBy(t => t.TransactionDate)
        };

        var totalCount = await query.CountAsync();
        var transactions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TransactionListResponse
            {
                TransactionId = t.TransactionId,
                TransactionNumber = t.TransactionNumber,
                CustomerName = t.Customer.FirstName + " " + t.Customer.LastName,
                TotalAmount = t.TotalAmount,
                PaymentMethod = t.PaymentMethod,
                PaymentStatus = t.PaymentStatus,
                ServiceItemCount = t.ServiceItems.Count,
                ProductItemCount = t.ProductItems.Count,
                TransactionDate = t.TransactionDate,
                ClientCurrency = t.ClientCurrency ?? "PHP",
                TotalInClientCurrency = t.TotalInClientCurrency
            })
            .ToListAsync();

        return new PagedResponse<TransactionListResponse>
        {
            Items = transactions,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    // ============================================================================
    // Payment Processing
    // ============================================================================

    public async Task<TransactionResponse> ProcessPaymentAsync(int transactionId, ProcessPaymentRequest request)
    {
        var transaction = await _context.Transactions.FindAsync(transactionId);
        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        if (transaction.PaymentStatus == "Paid")
            throw new InvalidOperationException("Transaction is already paid");

        if (transaction.PaymentStatus == "Voided")
            throw new InvalidOperationException("Cannot process payment for voided transaction");

        transaction.PaymentMethod = request.PaymentMethod;
        transaction.PaymentStatus = "Paid";

        // Calculate change for cash payments
        if (request.PaymentMethod == DomainConstants.PaymentMethods.Cash && request.AmountTendered.HasValue)
        {
            if (request.AmountTendered.Value < transaction.TotalAmount)
                throw new InvalidOperationException(
                    $"Amount tendered (₱{request.AmountTendered.Value:N2}) is less than total (₱{transaction.TotalAmount:N2})");

            transaction.AmountTendered = request.AmountTendered.Value;
            transaction.ChangeAmount = request.AmountTendered.Value - transaction.TotalAmount;
        }

        // Earn loyalty points with audit trail (skip if already earned during creation)
        var customer = await _context.Customers.FindAsync(transaction.CustomerId);
        if (customer != null && transaction.LoyaltyPointsEarned == 0)
        {
            EarnLoyaltyPoints(customer, transaction);
        }

        await _context.SaveChangesAsync();

        // Auto-create journal entry for the paid transaction
        try
        {
            await _accountingService.CreateTransactionJournalEntryAsync(transactionId, transaction.CashierId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create auto-JE for transaction {TransactionId}. GL may need manual entry.", transactionId);
        }

        var result = await GetTransactionByIdAsync(transactionId);
        return result!;
    }

    public async Task<TransactionResponse> VoidTransactionAsync(int transactionId, VoidTransactionRequest request, int voidedById)
    {
        var transaction = await _context.Transactions
            .Include(t => t.ProductItems)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        if (transaction.PaymentStatus == "Voided")
            throw new InvalidOperationException("Transaction is already voided");

        // Restore product stock with audit trail
        foreach (var item in transaction.ProductItems)
        {
            var quantityBefore = item.Product.CurrentStock;
            item.Product.CurrentStock += item.Quantity;
            item.Product.UpdatedAt = DateTime.UtcNow;

            _context.StockAdjustments.Add(new MiddayMistSpa.Core.Entities.Inventory.StockAdjustment
            {
                ProductId = item.ProductId,
                AdjustmentType = "Return to Stock",
                QuantityBefore = quantityBefore,
                QuantityChange = item.Quantity,
                QuantityAfter = item.Product.CurrentStock,
                Reason = $"Void: {request.Reason}",
                ReferenceNumber = transaction.TransactionNumber,
                AdjustedBy = voidedById,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Void the transaction
        var wasPaid = transaction.PaymentStatus == "Paid";
        transaction.PaymentStatus = "Voided";
        transaction.VoidedAt = DateTime.UtcNow;
        transaction.VoidedBy = voidedById;
        transaction.VoidReason = request.Reason;

        // Reverse loyalty points and customer stats if already paid
        if (wasPaid)
        {
            var customer = await _context.Customers.FindAsync(transaction.CustomerId);
            if (customer != null)
            {
                if (transaction.LoyaltyPointsEarned > 0)
                {
                    DeductLoyaltyPoints(customer, transaction.LoyaltyPointsEarned,
                        DomainConstants.LoyaltyTransactionTypes.Adjust,
                        $"Voided transaction {transaction.TransactionNumber}",
                        transaction.TransactionId);
                }
                customer.TotalSpent = Math.Max(0, customer.TotalSpent - transaction.TotalAmount);
                customer.TotalVisits = Math.Max(0, customer.TotalVisits - 1);

                // Recalculate LastVisitDate from most recent non-voided transaction
                var lastValidTransaction = await _context.Transactions
                    .Where(t => t.CustomerId == customer.CustomerId
                        && t.TransactionId != transaction.TransactionId
                        && t.PaymentStatus != "Voided")
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();
                customer.LastVisitDate = lastValidTransaction?.CreatedAt;
                customer.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        var result = await GetTransactionByIdAsync(transactionId);
        return result!;
    }

    // ============================================================================
    // Refunds
    // ============================================================================

    public async Task<RefundResponse> ProcessRefundAsync(int transactionId, CreateRefundRequest request, int approvedById, int processedById)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Refunds)
            .Include(t => t.ProductItems)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        if (transaction.PaymentStatus != "Paid")
            throw new InvalidOperationException("Can only refund paid transactions");

        // Validate refund amount
        var totalRefunded = transaction.Refunds.Sum(r => r.RefundAmount);
        var maxRefundable = transaction.TotalAmount - totalRefunded;

        if (request.RefundAmount > maxRefundable)
            throw new InvalidOperationException($"Refund amount exceeds maximum refundable: {maxRefundable:C}");

        var refund = new Refund
        {
            TransactionId = transactionId,
            RefundAmount = request.RefundAmount,
            RefundMethod = request.RefundMethod,
            RefundType = request.RefundType,
            Reason = request.Reason,
            ApprovedBy = approvedById,
            ProcessedBy = processedById,
            RefundDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Refunds.Add(refund);

        // Update transaction status if fully refunded
        var isFullRefund = request.RefundType == "Full" || request.RefundAmount >= maxRefundable;
        if (isFullRefund)
        {
            transaction.PaymentStatus = "Refunded";
        }

        // Restore product stock on refund with audit trail
        if (isFullRefund && (request.ProductItems == null || !request.ProductItems.Any()))
        {
            // Full refund without specific items: restore all product stock
            foreach (var item in transaction.ProductItems)
            {
                var qtyBefore = item.Product.CurrentStock;
                item.Product.CurrentStock += item.Quantity;
                item.Product.UpdatedAt = DateTime.UtcNow;

                _context.StockAdjustments.Add(new MiddayMistSpa.Core.Entities.Inventory.StockAdjustment
                {
                    ProductId = item.ProductId,
                    AdjustmentType = "Return to Stock",
                    QuantityBefore = qtyBefore,
                    QuantityChange = item.Quantity,
                    QuantityAfter = item.Product.CurrentStock,
                    Reason = $"Full refund: {request.Reason}",
                    ReferenceNumber = transaction.TransactionNumber,
                    AdjustedBy = processedById,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (request.ProductItems != null && request.ProductItems.Any())
        {
            // Specific items being refunded: restore only those
            foreach (var refundItem in request.ProductItems)
            {
                var txnItem = transaction.ProductItems
                    .FirstOrDefault(pi => pi.ProductId == refundItem.ProductId);
                if (txnItem == null)
                    throw new InvalidOperationException($"Product {refundItem.ProductId} not found in this transaction");

                if (refundItem.Quantity > txnItem.Quantity)
                    throw new InvalidOperationException(
                        $"Refund quantity ({refundItem.Quantity}) exceeds sold quantity ({(int)txnItem.Quantity}) for product {txnItem.Product.ProductName}");

                var qtyBefore = txnItem.Product.CurrentStock;
                txnItem.Product.CurrentStock += refundItem.Quantity;
                txnItem.Product.UpdatedAt = DateTime.UtcNow;

                _context.StockAdjustments.Add(new MiddayMistSpa.Core.Entities.Inventory.StockAdjustment
                {
                    ProductId = refundItem.ProductId,
                    AdjustmentType = "Return to Stock",
                    QuantityBefore = qtyBefore,
                    QuantityChange = refundItem.Quantity,
                    QuantityAfter = txnItem.Product.CurrentStock,
                    Reason = $"Partial refund: {request.Reason}",
                    ReferenceNumber = transaction.TransactionNumber,
                    AdjustedBy = processedById,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Deduct loyalty points proportional to refund amount
        var customer = await _context.Customers.FindAsync(transaction.CustomerId);
        if (customer != null)
        {
            var pointsToRemove = (int)(request.RefundAmount / 100) * DomainConstants.LoyaltyConfig.DefaultPointsPerHundredPesos;
            if (pointsToRemove > 0)
            {
                DeductLoyaltyPoints(customer, pointsToRemove,
                    DomainConstants.LoyaltyTransactionTypes.Adjust,
                    $"Refund on transaction {transaction.TransactionNumber}",
                    transaction.TransactionId);
            }
            customer.TotalSpent = Math.Max(0, customer.TotalSpent - request.RefundAmount);
        }

        await _context.SaveChangesAsync();

        // Auto-create reversing journal entry for the refund
        try
        {
            await _accountingService.CreateRefundJournalEntryAsync(
                transactionId, refund.RefundId, request.RefundAmount,
                request.RefundMethod, request.Reason, processedById);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create refund journal entry for transaction {TransactionId}", transactionId);
        }

        // Load navigation properties for response
        await _context.Entry(refund).Reference(r => r.ApprovedByUser).LoadAsync();
        await _context.Entry(refund).Reference(r => r.ProcessedByUser).LoadAsync();

        return new RefundResponse
        {
            RefundId = refund.RefundId,
            TransactionId = refund.TransactionId,
            RefundAmount = refund.RefundAmount,
            RefundMethod = refund.RefundMethod,
            RefundType = refund.RefundType,
            Reason = refund.Reason,
            ApprovedByName = refund.ApprovedByUser != null
                ? $"{refund.ApprovedByUser.FirstName} {refund.ApprovedByUser.LastName}"
                : "Unknown",
            ProcessedByName = refund.ProcessedByUser != null
                ? $"{refund.ProcessedByUser.FirstName} {refund.ProcessedByUser.LastName}"
                : "Unknown",
            RefundDate = refund.RefundDate
        };
    }

    public async Task<List<RefundResponse>> GetRefundsByTransactionAsync(int transactionId)
    {
        return await _context.Refunds
            .Where(r => r.TransactionId == transactionId)
            .Include(r => r.ApprovedByUser)
            .Include(r => r.ProcessedByUser)
            .Select(r => new RefundResponse
            {
                RefundId = r.RefundId,
                TransactionId = r.TransactionId,
                RefundAmount = r.RefundAmount,
                RefundMethod = r.RefundMethod,
                RefundType = r.RefundType,
                Reason = r.Reason,
                ApprovedByName = r.ApprovedByUser != null
                    ? r.ApprovedByUser.FirstName + " " + r.ApprovedByUser.LastName
                    : "Unknown",
                ProcessedByName = r.ProcessedByUser != null
                    ? r.ProcessedByUser.FirstName + " " + r.ProcessedByUser.LastName
                    : "Unknown",
                RefundDate = r.RefundDate
            })
            .ToListAsync();
    }

    // ============================================================================
    // Customer History
    // ============================================================================

    public async Task<PagedResponse<TransactionListResponse>> GetCustomerTransactionsAsync(int customerId, int page = 1, int pageSize = 20)
    {
        var query = _context.Transactions
            .Where(t => t.CustomerId == customerId)
            .Include(t => t.Customer)
            .Include(t => t.ServiceItems)
            .Include(t => t.ProductItems)
            .OrderByDescending(t => t.TransactionDate);

        var totalCount = await query.CountAsync();
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionListResponse
            {
                TransactionId = t.TransactionId,
                TransactionNumber = t.TransactionNumber,
                CustomerName = t.Customer.FirstName + " " + t.Customer.LastName,
                TotalAmount = t.TotalAmount,
                PaymentMethod = t.PaymentMethod,
                PaymentStatus = t.PaymentStatus,
                ServiceItemCount = t.ServiceItems.Count,
                ProductItemCount = t.ProductItems.Count,
                TransactionDate = t.TransactionDate,
                ClientCurrency = t.ClientCurrency ?? "PHP",
                TotalInClientCurrency = t.TotalInClientCurrency
            })
            .ToListAsync();

        return new PagedResponse<TransactionListResponse>
        {
            Items = transactions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // ============================================================================
    // Reports & Dashboard
    // ============================================================================

    public async Task<POSDashboardResponse> GetPOSDashboardAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var transactions = await _context.Transactions
            .Where(t => t.TransactionDate >= startOfDay && t.TransactionDate < endOfDay)
            .Include(t => t.Refunds)
            .ToListAsync();

        var response = new POSDashboardResponse
        {
            Date = date.Date,
            TotalTransactions = transactions.Count(t => t.PaymentStatus != "Voided"),
            TotalSales = transactions.Where(t => t.PaymentStatus == "Paid").Sum(t => t.TotalAmount),
            TotalTips = transactions.Where(t => t.PaymentStatus == "Paid").Sum(t => t.TipAmount),
            TotalDiscounts = transactions.Where(t => t.PaymentStatus != "Voided").Sum(t => t.DiscountAmount),
            TotalRefunds = transactions.SelectMany(t => t.Refunds).Sum(r => r.RefundAmount),
            VoidedCount = transactions.Count(t => t.PaymentStatus == "Voided"),
            RefundedCount = transactions.Count(t => t.PaymentStatus == "Refunded")
        };

        response.NetSales = response.TotalSales - response.TotalRefunds;

        // Group by payment method
        var byMethod = transactions
            .Where(t => t.PaymentStatus == "Paid")
            .GroupBy(t => t.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Count());
        response.ByPaymentMethod = byMethod;

        var amountByMethod = transactions
            .Where(t => t.PaymentStatus == "Paid")
            .GroupBy(t => t.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.TotalAmount));
        response.AmountByPaymentMethod = amountByMethod;

        return response;
    }

    public async Task<DailySalesReportResponse> GetDailySalesReportAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var transactions = await _context.Transactions
            .Where(t => t.TransactionDate >= startOfDay && t.TransactionDate < endOfDay)
            .Where(t => t.PaymentStatus != "Voided")
            .Include(t => t.ServiceItems)
            .Include(t => t.ProductItems)
            .Include(t => t.Refunds)
            .ToListAsync();

        var paidTransactions = transactions.Where(t => t.PaymentStatus == "Paid").ToList();

        var response = new DailySalesReportResponse
        {
            Date = date.Date,
            TransactionCount = paidTransactions.Count,
            GrossSales = paidTransactions.Sum(t => t.Subtotal),
            DiscountsGiven = transactions.Sum(t => t.DiscountAmount),
            TaxCollected = paidTransactions.Sum(t => t.TaxAmount),
            TipsReceived = paidTransactions.Sum(t => t.TipAmount),
            RefundsProcessed = transactions.SelectMany(t => t.Refunds).Sum(r => r.RefundAmount),
            ServiceRevenue = paidTransactions.SelectMany(t => t.ServiceItems).Sum(s => s.TotalPrice),
            ProductRevenue = paidTransactions.SelectMany(t => t.ProductItems).Sum(p => p.TotalPrice),
            TotalCommissions = paidTransactions.SelectMany(t => t.ServiceItems).Sum(s => s.CommissionAmount)
                + paidTransactions.SelectMany(t => t.ProductItems).Sum(p => p.CommissionAmount)
        };

        response.NetSales = response.GrossSales - response.DiscountsGiven - response.RefundsProcessed;

        return response;
    }

    public async Task<TransactionSalesReportResponse> GetSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var transactions = await _context.Transactions
            .Where(t => t.TransactionDate >= start && t.TransactionDate < end)
            .Where(t => t.PaymentStatus == "Paid")
            .Include(t => t.ServiceItems)
                .ThenInclude(s => s.Service)
            .Include(t => t.ProductItems)
                .ThenInclude(p => p.Product)
            .Include(t => t.Refunds)
            .ToListAsync();

        var response = new TransactionSalesReportResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalTransactions = transactions.Count,
            GrossSales = transactions.Sum(t => t.Subtotal),
            TotalDiscounts = transactions.Sum(t => t.DiscountAmount),
            TotalTax = transactions.Sum(t => t.TaxAmount),
            TotalTips = transactions.Sum(t => t.TipAmount),
            TotalRefunds = transactions.SelectMany(t => t.Refunds).Sum(r => r.RefundAmount),
            ServiceRevenue = transactions.SelectMany(t => t.ServiceItems).Sum(s => s.TotalPrice),
            ProductRevenue = transactions.SelectMany(t => t.ProductItems).Sum(p => p.TotalPrice),
            TotalCommissions = transactions.SelectMany(t => t.ServiceItems).Sum(s => s.CommissionAmount)
                + transactions.SelectMany(t => t.ProductItems).Sum(p => p.CommissionAmount)
        };

        response.NetSales = response.GrossSales - response.TotalDiscounts - response.TotalRefunds + response.TotalTax;
        response.AverageTransactionValue = transactions.Count > 0
            ? transactions.Average(t => t.TotalAmount)
            : 0;

        // Group by payment method
        response.ByPaymentMethod = transactions
            .GroupBy(t => t.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.TotalAmount));

        // Top services
        response.TopServices = transactions
            .SelectMany(t => t.ServiceItems)
            .GroupBy(s => new { s.ServiceId, s.Service.ServiceName })
            .Select(g => new TopServiceResponse
            {
                ServiceId = g.Key.ServiceId,
                ServiceName = g.Key.ServiceName,
                QuantitySold = g.Sum(s => s.Quantity),
                Revenue = g.Sum(s => s.TotalPrice)
            })
            .OrderByDescending(s => s.Revenue)
            .Take(10)
            .ToList();

        // Top products
        response.TopProducts = transactions
            .SelectMany(t => t.ProductItems)
            .GroupBy(p => new { p.ProductId, p.Product.ProductName })
            .Select(g => new TopProductResponse
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                QuantitySold = (int)g.Sum(p => p.Quantity),
                Revenue = g.Sum(p => p.TotalPrice)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToList();

        return response;
    }

    public async Task<CashierShiftReportResponse> GetCashierShiftReportAsync(int cashierId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var cashier = await _context.Users.FindAsync(cashierId);
        if (cashier == null)
            throw new InvalidOperationException("Cashier not found");

        var transactions = await _context.Transactions
            .Where(t => t.CashierId == cashierId)
            .Where(t => t.TransactionDate >= startOfDay && t.TransactionDate < endOfDay)
            .Include(t => t.Refunds)
            .ToListAsync();

        var paidTransactions = transactions.Where(t => t.PaymentStatus == "Paid").ToList();

        return new CashierShiftReportResponse
        {
            CashierId = cashierId,
            CashierName = $"{cashier.FirstName} {cashier.LastName}",
            ShiftDate = date.Date,
            TransactionCount = paidTransactions.Count,
            TotalSales = paidTransactions.Sum(t => t.TotalAmount),
            CashSales = paidTransactions.Where(t => t.PaymentMethod == "Cash").Sum(t => t.TotalAmount),
            CardSales = paidTransactions.Where(t => t.PaymentMethod == "Card").Sum(t => t.TotalAmount),
            TipsCollected = paidTransactions.Sum(t => t.TipAmount),
            VoidsCount = transactions.Count(t => t.PaymentStatus == "Voided"),
            RefundsCount = transactions.SelectMany(t => t.Refunds).Count(),
            RefundsAmount = transactions.SelectMany(t => t.Refunds).Sum(r => r.RefundAmount)
        };
    }

    // ============================================================================
    // Receipt
    // ============================================================================

    public async Task<ReceiptResponse> GenerateReceiptAsync(int transactionId)
    {
        var transaction = await _context.Transactions
            .AsQueryable()
            .Include(t => t.Customer)
            .Include(t => t.Cashier)
            .Include(t => t.ServiceItems)
                .ThenInclude(si => si.Service)
            .Include(t => t.ServiceItems)
                .ThenInclude(si => si.Therapist)
            .Include(t => t.ProductItems)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        var receipt = new ReceiptResponse
        {
            TransactionNumber = transaction.TransactionNumber,
            TransactionDate = transaction.TransactionDate,
            BusinessName = "MiddayMist Spa",
            CustomerName = $"{transaction.Customer.FirstName} {transaction.Customer.LastName}",
            MembershipType = transaction.Customer.MembershipType,
            Subtotal = transaction.Subtotal,
            DiscountAmount = transaction.DiscountAmount,
            TaxAmount = transaction.TaxAmount,
            TipAmount = transaction.TipAmount,
            TotalAmount = transaction.TotalAmount,
            PaymentMethod = transaction.PaymentMethod,
            AmountTendered = transaction.AmountTendered,
            ChangeAmount = transaction.ChangeAmount,
            CashierName = $"{transaction.Cashier.FirstName} {transaction.Cashier.LastName}",
            LoyaltyPointsEarned = transaction.LoyaltyPointsEarned,
            LoyaltyPointsBalance = transaction.Customer.LoyaltyPoints,
            ThankYouMessage = "Thank you for visiting MiddayMist Spa! We hope to see you again soon."
        };

        // Add service items
        foreach (var item in transaction.ServiceItems)
        {
            receipt.Items.Add(new ReceiptItemResponse
            {
                ItemType = "Service",
                ItemName = item.Service.ServiceName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                TherapistName = item.Therapist != null
                    ? $"{item.Therapist.FirstName} {item.Therapist.LastName}"
                    : null
            });
        }

        // Add product items
        foreach (var item in transaction.ProductItems)
        {
            receipt.Items.Add(new ReceiptItemResponse
            {
                ItemType = "Product",
                ItemName = item.Product.ProductName,
                Quantity = (int)item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            });
        }

        return receipt;
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private async Task<string> GenerateTransactionNumberAsync()
    {
        var today = PhilippineTime.Now;
        var prefix = $"TXN-{today:yyyyMMdd}-";

        for (int attempt = 0; attempt < 5; attempt++)
        {
            var lastTransaction = await _context.Transactions
                .Where(t => t.TransactionNumber.StartsWith(prefix))
                .OrderByDescending(t => t.TransactionNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastTransaction != null)
            {
                var lastNumberStr = lastTransaction.TransactionNumber.Replace(prefix, "");
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            var candidate = $"{prefix}{nextNumber:D4}";
            var exists = await _context.Transactions.AnyAsync(t => t.TransactionNumber == candidate);
            if (!exists) return candidate;

            // Collision detected — retry with incremented number
            await Task.Delay(50 * (attempt + 1));
        }

        // Fallback: use timestamp-based suffix
        return $"{prefix}{PhilippineTime.Now:HHmmssfff}";
    }

    /// <summary>
    /// Earn loyalty points for a paid transaction. Creates a LoyaltyPointTransaction audit record.
    /// Uses DomainConstants.LoyaltyConfig for rate (1 point per ₱100).
    /// </summary>
    private void EarnLoyaltyPoints(Core.Entities.Customer.Customer customer, Transaction transaction)
    {
        var pointsEarned = (int)(transaction.TotalAmount / 100) * DomainConstants.LoyaltyConfig.DefaultPointsPerHundredPesos;
        if (pointsEarned <= 0) return;

        transaction.LoyaltyPointsEarned = pointsEarned;
        customer.LoyaltyPoints += pointsEarned;
        customer.TotalSpent += transaction.TotalAmount;

        // Only increment TotalVisits for standalone transactions (no appointment).
        // Appointment-linked transactions already had TotalVisits incremented
        // in AppointmentService.CompleteServiceAsync to avoid double-counting.
        if (transaction.AppointmentId == null)
        {
            customer.TotalVisits += 1;
        }

        customer.LastVisitDate = DateTime.UtcNow;
        if (customer.FirstVisitDate == null)
            customer.FirstVisitDate = DateTime.UtcNow;

        var loyaltyTxn = new LoyaltyPointTransaction
        {
            CustomerId = customer.CustomerId,
            TransactionType = DomainConstants.LoyaltyTransactionTypes.Earn,
            Points = pointsEarned,
            BalanceRemaining = pointsEarned,
            EarnedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddMonths(DomainConstants.LoyaltyConfig.DefaultExpiryMonths),
            TransactionId = transaction.TransactionId > 0 ? transaction.TransactionId : null,
            Description = $"Earned {pointsEarned} pts from transaction {transaction.TransactionNumber}",
            CreatedAt = DateTime.UtcNow
        };

        _context.LoyaltyPointTransactions.Add(loyaltyTxn);

        // Auto-upgrade membership tier
        var newTier = DomainConstants.MembershipTiers.GetTierForPoints(customer.LoyaltyPoints);
        if (customer.MembershipType != newTier)
        {
            _logger.LogInformation("Customer {CustomerId} upgraded from {OldTier} to {NewTier}",
                customer.CustomerId, customer.MembershipType, newTier);
            customer.MembershipType = newTier;
        }
    }

    /// <summary>
    /// Deduct loyalty points (for void/refund). Creates a LoyaltyPointTransaction audit record.
    /// </summary>
    private void DeductLoyaltyPoints(Core.Entities.Customer.Customer customer, int points,
        string transactionType, string description, int? transactionId)
    {
        if (points <= 0) return;

        customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints - points);

        var loyaltyTxn = new LoyaltyPointTransaction
        {
            CustomerId = customer.CustomerId,
            TransactionType = transactionType,
            Points = -points,
            BalanceRemaining = 0,
            EarnedDate = DateTime.UtcNow,
            TransactionId = transactionId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _context.LoyaltyPointTransactions.Add(loyaltyTxn);

        // Re-evaluate membership tier after deduction
        var newTier = DomainConstants.MembershipTiers.GetTierForPoints(customer.LoyaltyPoints);
        if (customer.MembershipType != newTier)
        {
            _logger.LogInformation("Customer {CustomerId} tier changed from {OldTier} to {NewTier} after point deduction",
                customer.CustomerId, customer.MembershipType, newTier);
            customer.MembershipType = newTier;
        }
    }

    private TransactionResponse MapToResponse(Transaction transaction)
    {
        return new TransactionResponse
        {
            TransactionId = transaction.TransactionId,
            TransactionNumber = transaction.TransactionNumber,
            CustomerId = transaction.CustomerId,
            CustomerName = transaction.Customer != null
                ? $"{transaction.Customer.FirstName} {transaction.Customer.LastName}"
                : "Unknown",
            MembershipType = transaction.Customer?.MembershipType ?? "Regular",
            AppointmentId = transaction.AppointmentId,
            Subtotal = transaction.Subtotal,
            DiscountAmount = transaction.DiscountAmount,
            DiscountPercentage = transaction.DiscountPercentage,
            TaxAmount = transaction.TaxAmount,
            TipAmount = transaction.TipAmount,
            TotalAmount = transaction.TotalAmount,
            PaymentMethod = transaction.PaymentMethod,
            PaymentStatus = transaction.PaymentStatus,
            AmountTendered = transaction.AmountTendered,
            ChangeAmount = transaction.ChangeAmount,
            LoyaltyPointsEarned = transaction.LoyaltyPointsEarned,
            TransactionDate = transaction.TransactionDate,
            CashierId = transaction.CashierId,
            CashierName = transaction.Cashier != null
                ? $"{transaction.Cashier.FirstName} {transaction.Cashier.LastName}"
                : "Unknown",
            VoidedAt = transaction.VoidedAt,
            VoidedByName = transaction.VoidedByUser != null
                ? $"{transaction.VoidedByUser.FirstName} {transaction.VoidedByUser.LastName}"
                : null,
            VoidReason = transaction.VoidReason,

            // Multi-Currency
            ClientCurrency = transaction.ClientCurrency ?? "PHP",
            ClientCountryCode = transaction.ClientCountryCode,
            ClientIPAddress = transaction.ClientIPAddress,
            ExchangeRate = transaction.ExchangeRate,
            TotalInClientCurrency = transaction.TotalInClientCurrency,

            ServiceItems = transaction.ServiceItems.Select(si => new TransactionServiceItemResponse
            {
                TransactionServiceItemId = si.TransactionServiceItemId,
                ServiceId = si.ServiceId,
                ServiceName = si.Service?.ServiceName ?? "Unknown",
                ServiceCode = si.Service?.ServiceCode ?? "",
                TherapistId = si.TherapistId,
                TherapistName = si.Therapist != null
                    ? $"{si.Therapist.FirstName} {si.Therapist.LastName}"
                    : null,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                CommissionRate = si.CommissionRate,
                CommissionAmount = si.CommissionAmount
            }).ToList(),
            ProductItems = transaction.ProductItems.Select(pi => new TransactionProductItemResponse
            {
                TransactionProductItemId = pi.TransactionProductItemId,
                ProductId = pi.ProductId,
                ProductName = pi.Product?.ProductName ?? "Unknown",
                ProductCode = pi.Product?.ProductCode ?? "",
                Quantity = (int)pi.Quantity,
                UnitPrice = pi.UnitPrice,
                TotalPrice = pi.TotalPrice,
                CommissionRate = pi.CommissionRate,
                CommissionAmount = pi.CommissionAmount
            }).ToList(),
            Refunds = transaction.Refunds.Select(r => new RefundResponse
            {
                RefundId = r.RefundId,
                TransactionId = r.TransactionId,
                RefundAmount = r.RefundAmount,
                RefundMethod = r.RefundMethod,
                RefundType = r.RefundType,
                Reason = r.Reason,
                ApprovedByName = r.ApprovedByUser != null
                    ? $"{r.ApprovedByUser.FirstName} {r.ApprovedByUser.LastName}"
                    : "Unknown",
                ProcessedByName = r.ProcessedByUser != null
                    ? $"{r.ProcessedByUser.FirstName} {r.ProcessedByUser.LastName}"
                    : "Unknown",
                RefundDate = r.RefundDate
            }).ToList(),
            CreatedAt = transaction.CreatedAt
        };
    }
}
