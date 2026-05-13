namespace CallCenter.Shared.Enums;

public static class SalonSupplierOrderStatuses
{
    public static readonly TypeItem Draft = new(1, "Draft", "SalonSupplierOrderStatus.Draft", "Taslak", "bi-file-earmark", "bg-secondary", 1);
    public static readonly TypeItem Ordered = new(2, "Ordered", "SalonSupplierOrderStatus.Ordered", "Siparis Verildi", "bi-send", "bg-primary", 2);
    public static readonly TypeItem PartiallyReceived = new(3, "PartiallyReceived", "SalonSupplierOrderStatus.PartiallyReceived", "Kismi Teslim", "bi-box", "bg-warning", 3);
    public static readonly TypeItem Received = new(4, "Received", "SalonSupplierOrderStatus.Received", "Teslim Alindi", "bi-check2-circle", "bg-success", 4);
    public static readonly TypeItem Cancelled = new(5, "Cancelled", "SalonSupplierOrderStatus.Cancelled", "Iptal", "bi-x-circle", "bg-danger", 5);

    public static IEnumerable<TypeItem> All => new[] { Draft, Ordered, PartiallyReceived, Received, Cancelled };

    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Draft = 1;
        public const int Ordered = 2;
        public const int PartiallyReceived = 3;
        public const int Received = 4;
        public const int Cancelled = 5;
    }
}
