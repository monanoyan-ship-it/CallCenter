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

    // Masraf
    Task<List<object>> GetExpenseCategoriesAsync(int customerId);
    Task<object> CreateExpenseCategoryAsync(string name, int customerId);
    Task<List<SlnExpenseDto>> GetExpensesAsync(int customerId, DateTime? from, DateTime? to, int? categoryId = null);
    Task<SlnExpenseDto> CreateExpenseAsync(SlnExpenseCreateDto dto, int userId, int customerId);
    Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId, int customerId);
}
