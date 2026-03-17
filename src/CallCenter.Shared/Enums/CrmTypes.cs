namespace CallCenter.Shared.Enums;

/// <summary>Talep durumlari</summary>
public static class TicketStatuses
{
    public static readonly TypeItem Open = new(1, "Open", "Ticket.Open", "Acik", "bi-circle-fill", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "Ticket.InProgress", "Devam Ediyor", "bi-arrow-repeat", "bg-info", 2);
    public static readonly TypeItem Waiting = new(3, "Waiting", "Ticket.Waiting", "Beklemede", "bi-hourglass-split", "bg-secondary", 3);
    public static readonly TypeItem Resolved = new(4, "Resolved", "Ticket.Resolved", "Cozuldu", "bi-check-circle-fill", "bg-success", 4);
    public static readonly TypeItem Closed = new(5, "Closed", "Ticket.Closed", "Kapandi", "bi-x-circle-fill", "bg-dark", 5);

    public static IEnumerable<TypeItem> All => new[] { Open, InProgress, Waiting, Resolved, Closed };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Open = 1;
        public const int InProgress = 2;
        public const int Waiting = 3;
        public const int Resolved = 4;
        public const int Closed = 5;
    }
}

/// <summary>Talep oncelikleri</summary>
public static class TicketPriorities
{
    public static readonly TypeItem Low = new(1, "Low", "Ticket.Low", "Dusuk", "bi-arrow-down", "bg-secondary", 1);
    public static readonly TypeItem Medium = new(2, "Medium", "Ticket.Medium", "Orta", "bi-dash", "bg-info", 2, isDefault: true);
    public static readonly TypeItem High = new(3, "High", "Ticket.High", "Yuksek", "bi-arrow-up", "bg-warning text-dark", 3);
    public static readonly TypeItem Urgent = new(4, "Urgent", "Ticket.Urgent", "Acil", "bi-exclamation-triangle-fill", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { Low, Medium, High, Urgent };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Low = 1;
        public const int Medium = 2;
        public const int High = 3;
        public const int Urgent = 4;
    }
}

/// <summary>Firsat asamalari (pipeline)</summary>
public static class DealStages
{
    public static readonly TypeItem Lead = new(1, "Lead", "Deal.Lead", "Lead", "bi-funnel-fill", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Qualified = new(2, "Qualified", "Deal.Qualified", "Nitelikli", "bi-person-check-fill", "bg-info", 2);
    public static readonly TypeItem Proposal = new(3, "Proposal", "Deal.Proposal", "Teklif", "bi-file-earmark-text-fill", "bg-primary", 3);
    public static readonly TypeItem Negotiation = new(4, "Negotiation", "Deal.Negotiation", "Muzakere", "bi-chat-dots-fill", "bg-warning text-dark", 4);
    public static readonly TypeItem Won = new(5, "Won", "Deal.Won", "Kazanildi", "bi-trophy-fill", "bg-success", 5);
    public static readonly TypeItem Lost = new(6, "Lost", "Deal.Lost", "Kaybedildi", "bi-x-octagon-fill", "bg-danger", 6);

    public static IEnumerable<TypeItem> All => new[] { Lead, Qualified, Proposal, Negotiation, Won, Lost };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Lead = 1;
        public const int Qualified = 2;
        public const int Proposal = 3;
        public const int Negotiation = 4;
        public const int Won = 5;
        public const int Lost = 6;
    }
}

/// <summary>Etkilesim turleri</summary>
public static class ActivityTypes
{
    public static readonly TypeItem Call = new(1, "Call", "Activity.Call", "Arama", "bi-telephone-fill", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Email = new(2, "Email", "Activity.Email", "E-posta", "bi-envelope-fill", "bg-info", 2);
    public static readonly TypeItem Meeting = new(3, "Meeting", "Activity.Meeting", "Toplanti", "bi-camera-video-fill", "bg-success", 3);
    public static readonly TypeItem Note = new(4, "Note", "Activity.Note", "Not", "bi-sticky-fill", "bg-warning text-dark", 4);
    public static readonly TypeItem Task = new(5, "Task", "Activity.Task", "Gorev", "bi-check2-square", "bg-secondary", 5);
    public static readonly TypeItem Sms = new(6, "Sms", "Activity.Sms", "SMS", "bi-chat-left-text-fill", "bg-dark", 6);

    public static IEnumerable<TypeItem> All => new[] { Call, Email, Meeting, Note, Task, Sms };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Call = 1;
        public const int Email = 2;
        public const int Meeting = 3;
        public const int Note = 4;
        public const int Task = 5;
        public const int Sms = 6;
    }
}

/// <summary>Anket turleri</summary>
public static class SurveyTypes
{
    public static readonly TypeItem CSAT = new(1, "CSAT", "Survey.CSAT", "Musteri Memnuniyeti", "bi-emoji-smile-fill", "bg-success", 1, isDefault: true);
    public static readonly TypeItem NPS = new(2, "NPS", "Survey.NPS", "Net Promoter Score", "bi-graph-up-arrow", "bg-primary", 2);
    public static readonly TypeItem Custom = new(3, "Custom", "Survey.Custom", "Ozel Anket", "bi-card-checklist", "bg-info", 3);

    public static IEnumerable<TypeItem> All => new[] { CSAT, NPS, Custom };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int CSAT = 1;
        public const int NPS = 2;
        public const int Custom = 3;
    }
}

/// <summary>Anket soru turleri</summary>
public static class SurveyQuestionTypes
{
    public static readonly TypeItem Rating = new(1, "Rating", "SurveyQ.Rating", "Puan (1-5/1-10)", "bi-star-fill", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem MultiChoice = new(2, "MultiChoice", "SurveyQ.MultiChoice", "Coktan Secmeli", "bi-list-check", "bg-info", 2);
    public static readonly TypeItem Text = new(3, "Text", "SurveyQ.Text", "Serbest Metin", "bi-chat-left-text-fill", "bg-secondary", 3);
    public static readonly TypeItem YesNo = new(4, "YesNo", "SurveyQ.YesNo", "Evet/Hayir", "bi-toggle-on", "bg-primary", 4);

    public static IEnumerable<TypeItem> All => new[] { Rating, MultiChoice, Text, YesNo };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Rating = 1;
        public const int MultiChoice = 2;
        public const int Text = 3;
        public const int YesNo = 4;
    }
}

/// <summary>CRM gorev durumlari</summary>
public static class CrmTaskStatuses
{
    public static readonly TypeItem Todo = new(1, "Todo", "CrmTask.Todo", "Yapilacak", "bi-circle", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "CrmTask.InProgress", "Devam Ediyor", "bi-arrow-repeat", "bg-info", 2);
    public static readonly TypeItem Done = new(3, "Done", "CrmTask.Done", "Tamamlandi", "bi-check-circle-fill", "bg-success", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", "CrmTask.Cancelled", "Iptal", "bi-x-circle-fill", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { Todo, InProgress, Done, Cancelled };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Todo = 1;
        public const int InProgress = 2;
        public const int Done = 3;
        public const int Cancelled = 4;
    }
}
