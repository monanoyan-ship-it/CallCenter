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

        var invoices = await _financeFactory.GetInvoicesAsync(customerId, from, to, statusId, GetBranchId());
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

        var personnelId = GetPersonnelId();
        var (invoice, error) = await _financeFactory.CreateInvoiceAsync(dto, personnelId, customerId, GetBranchId());
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

        var registers = await _financeFactory.GetCashRegistersAsync(customerId, GetBranchId());
        return Ok(registers);
    }

    [HttpPost("cash-registers")]
    public async Task<ActionResult> CreateCashRegister([FromBody] SlnCashRegisterCreateRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        // Sube Muduru ise JWT deki BranchId override (baskasinin subesinde kasa acmayi engelle)
        var jwtBranchId = GetBranchId();
        var effectiveBranchId = jwtBranchId ?? req.BranchId;

        var register = await _financeFactory.CreateCashRegisterAsync(req.Name, customerId, effectiveBranchId);
        return Ok(register);
    }

    [HttpPut("cash-registers/{id}")]
    public async Task<ActionResult> UpdateCashRegister(int id, [FromBody] SlnCashRegisterUpdateRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _financeFactory.UpdateCashRegisterAsync(id, req.Name, req.BranchId, req.IsActive, customerId);
        return success ? Ok() : BadRequest(new { message = error });
    }

    /// <summary>BranchId null olan eski kasalari merkez subeye tasir (bir seferlik)</summary>
    [HttpPost("cash-registers/normalize-branch")]
    public async Task<ActionResult> NormalizeCashRegisterBranches()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var result = await _financeFactory.NormalizeCashRegisterBranchesAsync(customerId);
        return Ok(result);
    }

    [HttpGet("cash-registers/{registerId}/transactions")]
    public async Task<ActionResult<List<SlnCashTransactionDto>>> GetCashTransactions(
        int registerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var transactions = await _financeFactory.GetCashTransactionsAsync(registerId, customerId, from, to, GetBranchId());
        return Ok(transactions);
    }

    [HttpPost("cash-registers/{registerId}/transactions")]
    public async Task<ActionResult<SlnCashTransactionDto>> AddCashTransaction(int registerId, [FromBody] SlnCashTransactionCreateRequest req)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (transaction, error) = await _financeFactory.AddCashTransactionAsync(
            registerId, req.TransactionTypeId, req.Amount, req.Description, req.PaymentMethodId, userId, customerId, GetBranchId());

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

        var expenses = await _financeFactory.GetExpensesAsync(customerId, from, to, categoryId, GetBranchId());
        return Ok(expenses);
    }

    [HttpPost("expenses")]
    public async Task<ActionResult<SlnExpenseDto>> CreateExpense([FromBody] SlnExpenseCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var expense = await _financeFactory.CreateExpenseAsync(dto, userId, customerId, GetBranchId());
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
        return Ok(await _financeFactory.GetCashClosingsAsync(customerId, registerId, GetBranchId()));
    }

    [HttpPost("cash-closings")]
    public async Task<ActionResult<SlnCashClosingDto>> CreateCashClosing([FromBody] SlnCashClosingCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (closing, error) = await _financeFactory.CreateCashClosingAsync(dto, userId, customerId, GetBranchId());
        return closing != null ? Ok(closing) : BadRequest(error);
    }

    [HttpGet("cash-registers/{registerId}/daily-summary")]
    public async Task<ActionResult> GetDailySummary(int registerId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var summary = await _financeFactory.GetDailySummaryAsync(registerId, customerId, GetBranchId());
        return Ok(summary);
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int GetPersonnelId()
        => int.Parse(User.FindFirst("CustomerPersonnelId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    // ═══ Z RAPORU ═══

    [HttpGet("z-report/{registerId}")]
    public async Task<ActionResult> GetZReport(int registerId, [FromQuery] DateTime? date)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetZReportAsync(registerId, cid, date, GetBranchId()));
    }

    // ═══ KASA AÇILIŞ ═══

    [HttpPost("cash-registers/{registerId}/open")]
    public async Task<ActionResult> OpenCash(int registerId, [FromBody] CashOpeningRequest? request)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (result, error) = await _financeFactory.CreateCashOpeningAsync(registerId, cid, request?.ManualBalance, GetPersonnelId(), GetBranchId());
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    // ═══ MÜŞTERİ CARİ HESAP ═══

    [HttpGet("client-ledger/{slnClientId}")]
    public async Task<ActionResult> GetClientLedger(int slnClientId)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetClientLedgerAsync(cid, slnClientId));
    }

    // ═══ İADE ═══

    [HttpPost("invoices/{invoiceId}/refund")]
    public async Task<ActionResult> CreateRefund(int invoiceId, [FromBody] RefundRequest request)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (result, error) = await _financeFactory.CreateRefundAsync(cid, invoiceId, request.Amount, request.RefundMethodId, request.Reason, GetPersonnelId());
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    // ═══ PERSONEL HASILAT ═══

    [HttpGet("staff-revenue")]
    public async Task<ActionResult> GetStaffRevenue([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetStaffRevenueAsync(cid, startDate, endDate, GetBranchId()));
    }

    // ═══ FİNANS RAPORLARI ═══

    [HttpGet("reports/income-expense")]
    public async Task<ActionResult> GetIncomeExpenseReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetIncomeExpenseReportAsync(cid, startDate, endDate, GetBranchId()));
    }

    [HttpGet("reports/tax")]
    public async Task<ActionResult> GetTaxReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetTaxReportAsync(cid, startDate, endDate, GetBranchId()));
    }
}

public class CashOpeningRequest
{
    public decimal? ManualBalance { get; set; }
}

public class SlnCashRegisterCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public int? BranchId { get; set; }
}

public class SlnCashRegisterUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RefundRequest
{
    public decimal Amount { get; set; }
    public int RefundMethodId { get; set; } = 1;
    public string Reason { get; set; } = string.Empty;
}

public class SlnCashTransactionCreateRequest
{
    public int TransactionTypeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int PaymentMethodId { get; set; } = 1;
}
