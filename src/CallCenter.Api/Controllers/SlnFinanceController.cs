using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-finance")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnInvoices)]
public class SlnFinanceController : ControllerBase
{
    private readonly ISlnFinanceFactory _financeFactory;

    public SlnFinanceController(ISlnFinanceFactory financeFactory) => _financeFactory = financeFactory;

    // ═══ Adisyon (Invoice) ═══

    [HttpGet("invoices")]
    public async Task<ActionResult<List<SlnInvoiceDto>>> GetInvoices(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? statusId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var invoices = await _financeFactory.GetInvoicesAsync(customerId, from, to, statusId);
        return Ok(invoices);
    }

    [HttpGet("invoices/{id}")]
    public async Task<ActionResult<SlnInvoiceDto>> GetInvoice(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var invoice = await _financeFactory.GetInvoiceAsync(id, customerId);
        return invoice != null ? Ok(invoice) : NotFound();
    }

    [HttpPost("invoices")]
    public async Task<ActionResult<SlnInvoiceDto>> CreateInvoice([FromBody] SlnInvoiceCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (invoice, error) = await _financeFactory.CreateInvoiceAsync(dto, userId, customerId);
        return invoice != null ? Ok(invoice) : BadRequest(error);
    }

    [HttpPut("invoices/{id}/cancel")]
    public async Task<ActionResult> CancelInvoice(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _financeFactory.CancelInvoiceAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Kasa ═══

    [HttpGet("cash-registers")]
    public async Task<ActionResult> GetCashRegisters()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var registers = await _financeFactory.GetCashRegistersAsync(customerId);
        return Ok(registers);
    }

    [HttpPost("cash-registers")]
    public async Task<ActionResult> CreateCashRegister([FromBody] SlnNameRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var register = await _financeFactory.CreateCashRegisterAsync(req.Name, customerId);
        return Ok(register);
    }

    [HttpGet("cash-registers/{registerId}/transactions")]
    public async Task<ActionResult<List<SlnCashTransactionDto>>> GetCashTransactions(
        int registerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var transactions = await _financeFactory.GetCashTransactionsAsync(registerId, customerId, from, to);
        return Ok(transactions);
    }

    [HttpPost("cash-registers/{registerId}/transactions")]
    public async Task<ActionResult<SlnCashTransactionDto>> AddCashTransaction(int registerId, [FromBody] SlnCashTransactionCreateRequest req)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (transaction, error) = await _financeFactory.AddCashTransactionAsync(
            registerId, req.TransactionTypeId, req.Amount, req.Description, req.PaymentMethodId, userId, customerId);

        return transaction != null ? Ok(transaction) : BadRequest(error);
    }

    // ═══ Masraf ═══

    [HttpGet("expense-categories")]
    public async Task<ActionResult> GetExpenseCategories()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var categories = await _financeFactory.GetExpenseCategoriesAsync(customerId);
        return Ok(categories);
    }

    [HttpPost("expense-categories")]
    public async Task<ActionResult> CreateExpenseCategory([FromBody] SlnNameRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var category = await _financeFactory.CreateExpenseCategoryAsync(req.Name, customerId);
        return Ok(category);
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<List<SlnExpenseDto>>> GetExpenses(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? categoryId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var expenses = await _financeFactory.GetExpensesAsync(customerId, from, to, categoryId);
        return Ok(expenses);
    }

    [HttpPost("expenses")]
    public async Task<ActionResult<SlnExpenseDto>> CreateExpense([FromBody] SlnExpenseCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var expense = await _financeFactory.CreateExpenseAsync(dto, userId, customerId);
        return Ok(expense);
    }

    [HttpDelete("expenses/{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _financeFactory.DeleteExpenseAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Gun Sonu Kasa Kapama ═══

    [HttpGet("cash-closings")]
    public async Task<ActionResult<List<SlnCashClosingDto>>> GetCashClosings([FromQuery] int? registerId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _financeFactory.GetCashClosingsAsync(customerId, registerId));
    }

    [HttpPost("cash-closings")]
    public async Task<ActionResult<SlnCashClosingDto>> CreateCashClosing([FromBody] SlnCashClosingCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (closing, error) = await _financeFactory.CreateCashClosingAsync(dto, userId, customerId);
        return closing != null ? Ok(closing) : BadRequest(error);
    }

    [HttpGet("cash-registers/{registerId}/daily-summary")]
    public async Task<ActionResult> GetDailySummary(int registerId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var summary = await _financeFactory.GetDailySummaryAsync(registerId, customerId);
        return Ok(summary);
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}

public class SlnCashTransactionCreateRequest
{
    public int TransactionTypeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int PaymentMethodId { get; set; } = 1;
}
