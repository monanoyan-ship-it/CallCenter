using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerBillingPeriodModuleLineEntityService
{
    IQueryable<CustomerBillingPeriodModuleLine> GetAllQueryable();
}
