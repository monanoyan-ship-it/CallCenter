namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnDashboardFactory
{
    Task<object> GetDashboardAsync(int customerId, int? branchId = null, int roleId = 0, int personnelId = 0);
}
