using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Api.Services;
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
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? statusId,
        [FromQuery] int? branchId, [FromQuery] int? slnClientId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var invoices = await _financeFactory.GetInvoicesAsync(
            customerId,
            from,
            to,
            statusId,
            ResolveBranchId(branchId),
            slnClientId);
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
    public async Task<ActionResult<SlnInvoiceDto>> CreateInvoice([FromBody] SlnInvoiceCreateDto dto, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var personnelId = GetPersonnelId();
        var (invoice, error) = await _financeFactory.CreateInvoiceAsync(dto, personnelId, customerId, ResolveBranchId(branchId));
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
    public async Task<ActionResult> GetCashRegisters([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var registers = await _financeFactory.GetCashRegistersAsync(customerId, ResolveBranchId(branchId));
        return Ok(registers);
    }

    [HttpPost("cash-registers")]
    public async Task<ActionResult> CreateCashRegister([FromBody] SlnCashRegisterCreateRequest req, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        // Sube Muduru ise JWT deki BranchId override (baskasinin subesinde kasa acmayi engelle)
        var branchScopeId = GetBranchScopeId();
        var effectiveBranchId = branchScopeId ?? req.BranchId ?? branchId;

        var register = await _financeFactory.CreateCashRegisterAsync(req.Name, customerId, effectiveBranchId);
        return Ok(register);
    }

    [HttpPut("cash-registers/{id}")]
    public async Task<ActionResult> UpdateCashRegister(int id, [FromBody] SlnCashRegisterUpdateRequest req, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var branchScopeId = GetBranchScopeId();
        var effectiveBranchId = branchScopeId ?? req.BranchId ?? branchId;
        var (success, error) = await _financeFactory.UpdateCashRegisterAsync(id, req.Name, effectiveBranchId, req.IsActive, customerId, branchScopeId);
        return success ? Ok() : BadRequest(new { message = error });
    }

    /// <summary>BranchId null olan eski kasalari merkez subeye tasir (bir seferlik)</summary>
    [HttpPost("cash-registers/normalize-branch")]
    [RequireSalonOwner]
    public async Task<ActionResult> NormalizeCashRegisterBranches()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var result = await _financeFactory.NormalizeCashRegisterBranchesAsync(customerId);
        return Ok(result);
    }

    [HttpGet("cash-registers/{registerId}/transactions")]
    public async Task<ActionResult<List<SlnCashTransactionDto>>> GetCashTransactions(
        int registerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var transactions = await _financeFactory.GetCashTransactionsAsync(registerId, customerId, from, to, ResolveBranchId(branchId));
        return Ok(transactions);
    }

    [HttpPost("cash-registers/{registerId}/transactions")]
    public async Task<ActionResult<SlnCashTransactionDto>> AddCashTransaction(int registerId, [FromBody] SlnCashTransactionCreateRequest req, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (transaction, error) = await _financeFactory.AddCashTransactionAsync(
            registerId, req.TransactionTypeId, req.Amount, req.Description, req.PaymentMethodId, userId, customerId, ResolveBranchId(branchId));

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
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? categoryId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var expenses = await _financeFactory.GetExpensesAsync(customerId, from, to, categoryId, ResolveBranchId(branchId));
        return Ok(expenses);
    }

    [HttpPost("expenses")]
    public async Task<ActionResult<SlnExpenseDto>> CreateExpense([FromBody] SlnExpenseCreateDto dto, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        if (dto.CategoryId <= 0) return BadRequest(new { error = "Kategori zorunludur" });

        var expense = await _financeFactory.CreateExpenseAsync(dto, userId, customerId, ResolveBranchId(branchId));
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
        return Ok(await _financeFactory.GetCashClosingsAsync(customerId, registerId, GetBranchScopeId()));
    }

    [HttpPost("cash-closings")]
    public async Task<ActionResult<SlnCashClosingDto>> CreateCashClosing([FromBody] SlnCashClosingCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (closing, error) = await _financeFactory.CreateCashClosingAsync(dto, userId, customerId, GetBranchScopeId());
        return closing != null ? Ok(closing) : BadRequest(error);
    }

    [HttpGet("cash-registers/{registerId}/daily-summary")]
    public async Task<ActionResult> GetDailySummary(int registerId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var summary = await _financeFactory.GetDailySummaryAsync(registerId, customerId, GetBranchScopeId());
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

    private int GetCustomerRoleId()
    {
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(claim, out var roleId) ? roleId : SalonRoles.Ids.SalonOwner;
    }

    private int? GetBranchScopeId()
        => GetCustomerRoleId() == SalonRoles.Ids.SalonOwner ? null : GetBranchId();

    private int? ResolveBranchId(int? requestedBranchId)
        => GetBranchScopeId() ?? requestedBranchId;

    // ═══ Z RAPORU ═══

    [HttpGet("z-report/{registerId}")]
    public async Task<ActionResult> GetZReport(int registerId, [FromQuery] DateTime? date)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetZReportAsync(registerId, cid, date, GetBranchScopeId()));
    }

    // ═══ KASA AÇILIŞ ═══

    [HttpPost("cash-registers/{registerId}/open")]
    public async Task<ActionResult> OpenCash(int registerId, [FromBody] CashOpeningRequest? request)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (result, error) = await _financeFactory.CreateCashOpeningAsync(registerId, cid, request?.ManualBalance, GetPersonnelId(), GetBranchScopeId());
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    // ═══ MÜŞTERİ CARİ HESAP ═══

    [HttpGet("client-ledger/{slnClientId}")]
    public async Task<ActionResult> GetClientLedger(int slnClientId, [FromQuery] int? branchId)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetClientLedgerAsync(cid, slnClientId, ResolveBranchId(branchId)));
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
        return Ok(await _financeFactory.GetStaffRevenueAsync(cid, startDate, endDate, GetBranchScopeId()));
    }

    // ═══ FİNANS RAPORLARI ═══

    [HttpGet("reports/income-expense")]
    public async Task<ActionResult> GetIncomeExpenseReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetIncomeExpenseReportAsync(cid, startDate, endDate, GetBranchScopeId()));
    }

    [HttpGet("reports/tax")]
    public async Task<ActionResult> GetTaxReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _financeFactory.GetTaxReportAsync(cid, startDate, endDate, GetBranchScopeId()));
    }

    /// <summary>
    /// PS.13 — salon hak edis raporu (sub-merchant settlement detay). Tarih araligindaki
    /// basarili tx-ler brut/komisyon/stopaj/net seklinde donulur. Sadece SalonAdisyon
    /// (post-pay) ve UyelikOdemesi (membership) sub-merchant split kapsamindadir.
    /// </summary>
    [HttpGet("reports/settlements")]
    public async Task<ActionResult<SettlementReportDto>> GetSettlementReport(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromServices] PaymentService paymentService)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var report = await paymentService.GetSettlementReportAsync(cid, startDate, endDate);
        return Ok(report);
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
