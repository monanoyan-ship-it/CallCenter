using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrossBorderTransferEntityService
{
    IQueryable<CrossBorderTransfer> GetAllQueryable();
    Task<CrossBorderTransfer?> GetByIdAsync(int id);
    Task<CrossBorderTransfer?> GetByUidAsync(Guid uid);
    void Add(CrossBorderTransfer entity);
    void Update(CrossBorderTransfer entity);
    void Remove(CrossBorderTransfer entity);
}
