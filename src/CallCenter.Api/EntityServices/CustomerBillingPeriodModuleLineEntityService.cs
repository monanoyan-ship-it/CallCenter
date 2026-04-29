using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CustomerBillingPeriodModuleLineEntityService : ICustomerBillingPeriodModuleLineEntityService
{
    private readonly AppDbContext _db;

    public CustomerBillingPeriodModuleLineEntityService(AppDbContext db) => _db = db;

    public IQueryable<CustomerBillingPeriodModuleLine> GetAllQueryable()
        => _db.CustomerBillingPeriodModuleLines.AsQueryable();
}
