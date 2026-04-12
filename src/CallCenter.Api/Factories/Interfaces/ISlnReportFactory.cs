using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnReportFactory
{
    Task<SlnSalesReportDto> GetSalesReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<SlnStaffReportDto> GetStaffReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<SlnStockReportDto> GetStockReportAsync(int customerId);
    Task<SlnFinanceReportDto> GetFinanceReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
    Task<SlnClientReportDto> GetClientReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null);
}
