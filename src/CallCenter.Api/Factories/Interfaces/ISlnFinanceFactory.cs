using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnFinanceFactory
{
    // Adisyon (Invoice)
    Task<List<SlnInvoiceDto>> GetInvoicesAsync(int customerId, DateTime? from, DateTime? to, int? statusId = null);
    Task<SlnInvoiceDto?> GetInvoiceAsync(int invoiceId, int customerId);
    Task<(SlnInvoiceDto? Invoice, string? Error)> CreateInvoiceAsync(SlnInvoiceCreateDto dto, int userId, int customerId);
    Task<(bool Success, string? Error)> CancelInvoiceAsync(int invoiceId, int customerId);

    // Kasa
    Task<List<object>> GetCashRegistersAsync(int customerId);
    Task<object> CreateCashRegisterAsync(string name, int customerId);
    Task<List<SlnCashTransactionDto>> GetCashTransactionsAsync(int registerId, int customerId, DateTime? from, DateTime? to);
    Task<(SlnCashTransactionDto? Transaction, string? Error)> AddCashTransactionAsync(int registerId, int transactionTypeId, decimal amount, string description, int paymentMethodId, int userId, int customerId);

    // Gun Sonu Kasa Kapama
    Task<List<SlnCashClosingDto>> GetCashClosingsAsync(int customerId, int? registerId);
    Task<(SlnCashClosingDto? Closing, string? Error)> CreateCashClosingAsync(SlnCashClosingCreateDto dto, int userId, int customerId);
    Task<object> GetDailySummaryAsync(int registerId, int customerId);

    // Masraf
    Task<List<object>> GetExpenseCategoriesAsync(int customerId);
    Task<object> CreateExpenseCategoryAsync(string name, int customerId);
    Task<List<SlnExpenseDto>> GetExpensesAsync(int customerId, DateTime? from, DateTime? to, int? categoryId = null);
    Task<SlnExpenseDto> CreateExpenseAsync(SlnExpenseCreateDto dto, int userId, int customerId);
    Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId, int customerId);

    // Z Raporu
    Task<object> GetZReportAsync(int registerId, int customerId, DateTime? date = null);

    // Kasa Acilis
    Task<(object? Result, string? Error)> CreateCashOpeningAsync(int registerId, int customerId, decimal? manualBalance, int personnelId);

    // Musteri Cari Hesap
    Task<object> GetClientLedgerAsync(int customerId, int slnClientId);
    Task AddLedgerEntryAsync(int customerId, int slnClientId, int typeId, decimal amount, int? invoiceId, string? description);

    // Iade
    Task<(object? Result, string? Error)> CreateRefundAsync(int customerId, int invoiceId, decimal refundAmount, int refundMethodId, string reason, int personnelId);

    // Personel Hasilat
    Task<object> GetStaffRevenueAsync(int customerId, DateTime startDate, DateTime endDate);

    // Finans Raporlari
    Task<object> GetIncomeExpenseReportAsync(int customerId, DateTime startDate, DateTime endDate);
    Task<object> GetTaxReportAsync(int customerId, DateTime startDate, DateTime endDate);
}
