namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnDashboardFactory
{
    Task<object> GetDashboardAsync(int customerId, int? branchId = null);
}
