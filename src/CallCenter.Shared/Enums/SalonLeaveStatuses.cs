namespace CallCenter.Shared.Enums;

public static class SalonLeaveStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "SalonLeaveStatus.Pending", "Bekliyor", "bi-hourglass", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem Approved = new(2, "Approved", "SalonLeaveStatus.Approved", "Onaylandi", "bi-check2-circle", "bg-success", 2);
    public static readonly TypeItem Rejected = new(3, "Rejected", "SalonLeaveStatus.Rejected", "Reddedildi", "bi-x-circle", "bg-danger", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", "SalonLeaveStatus.Cancelled", "Iptal", "bi-slash-circle", "bg-secondary", 4);

    public static IEnumerable<TypeItem> All => new[] { Pending, Approved, Rejected, Cancelled };
    public static TypeItem Default => Pending;
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Pending = 1;
        public const int Approved = 2;
        public const int Rejected = 3;
        public const int Cancelled = 4;
    }
}
