using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnReportFactory
{
    Task<SlnKpiReportDto> GetKpiReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<SlnBranchComparisonReportDto> GetBranchComparisonReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<SlnSalesReportDto> GetSalesReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<SlnStaffReportDto> GetStaffReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<SlnStockReportDto> GetStockReportAsync(int customerId, int? branchId = null);
    Task<SlnFinanceReportDto> GetFinanceReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<SlnClientReportDto> GetClientReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<byte[]> ExportSalonReportCsvAsync(int customerId, string report, DateTime from, DateTime to, int? branchId = null);
    Task<byte[]> ExportSalonReportExcelAsync(int customerId, string report, DateTime from, DateTime to, int? branchId = null);
    Task<byte[]> ExportSalonReportPdfAsync(int customerId, string report, DateTime from, DateTime to, int? branchId = null);
}
