namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnDashboardFactory
{
    Task<object> GetDashboardAsync(int customerId, int? branchId = null, int roleId = 0, int personnelId = 0);

    /// <summary>
    /// Hizmet veren personelin kendi hakedisi: donem icinde kendi urettigi ciro + prim ozeti
    /// ve hareket (fatura kalemi) listesi. Salt-goruntuleme; maas/bordro YOK. Prim hesabi
    /// bordro (PortalFactory.CalculateCommission) ile ayni cozumlemeyi kullanir.
    /// </summary>
    Task<object> GetMyEarningsAsync(int customerId, int personnelId, DateTime startUtc, DateTime endUtc, int? branchId = null);
}
