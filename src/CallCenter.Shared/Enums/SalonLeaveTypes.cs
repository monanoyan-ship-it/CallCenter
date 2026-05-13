namespace CallCenter.Shared.Enums;

public static class SalonLeaveTypes
{
    public static readonly TypeItem Annual = new(1, "Annual", "SalonLeaveType.Annual", "Yillik izin", "bi-calendar-check", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Sick = new(2, "Sick", "SalonLeaveType.Sick", "Saglik izni", "bi-heart-pulse", "bg-danger", 2);
    public static readonly TypeItem Excuse = new(3, "Excuse", "SalonLeaveType.Excuse", "Mazeret izni", "bi-chat-left-text", "bg-info", 3);
    public static readonly TypeItem Unpaid = new(4, "Unpaid", "SalonLeaveType.Unpaid", "Ucretsiz izin", "bi-wallet2", "bg-secondary", 4);

    public static IEnumerable<TypeItem> All => new[] { Annual, Sick, Excuse, Unpaid };
    public static TypeItem Default => Annual;
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Annual = 1;
        public const int Sick = 2;
        public const int Excuse = 3;
        public const int Unpaid = 4;
    }
}
