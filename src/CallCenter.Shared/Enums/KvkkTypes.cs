namespace CallCenter.Shared.Enums;

public static class PrivacyNoticeTypes
{
    public static readonly TypeItem CallRecording = new(1, "CallRecording", "PrivacyNotice.CallRecording", "Ses kaydi aydinlatma metni", "bi-mic-fill", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem DataProcessing = new(2, "DataProcessing", "PrivacyNotice.DataProcessing", "Veri isleme aydinlatma metni", "bi-database-fill-gear", "bg-info", 2);
    public static readonly TypeItem General = new(3, "General", "PrivacyNotice.General", "Genel aydinlatma metni", "bi-file-earmark-text", "bg-secondary", 3);

    public static IEnumerable<TypeItem> All => new[] { CallRecording, DataProcessing, General };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int CallRecording = 1;
        public const int DataProcessing = 2;
        public const int General = 3;
    }
}

public static class ConsentTypes
{
    public static readonly TypeItem CallRecording = new(1, "CallRecording", "ConsentType.CallRecording", "Arama kaydi rizasi", "bi-mic-fill", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem DataProcessing = new(2, "DataProcessing", "ConsentType.DataProcessing", "Veri isleme rizasi", "bi-database-fill-gear", "bg-info", 2);
    public static readonly TypeItem Marketing = new(3, "Marketing", "ConsentType.Marketing", "Pazarlama rizasi", "bi-megaphone-fill", "bg-warning text-dark", 3);

    public static IEnumerable<TypeItem> All => new[] { CallRecording, DataProcessing, Marketing };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int CallRecording = 1;
        public const int DataProcessing = 2;
        public const int Marketing = 3;
    }
}

public static class ConsentStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "ConsentStatus.Pending", "Beklemede", "bi-hourglass-split", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem Granted = new(2, "Granted", "ConsentStatus.Granted", "Verildi", "bi-check-circle-fill", "bg-success", 2);
    public static readonly TypeItem Revoked = new(3, "Revoked", "ConsentStatus.Revoked", "Iptal edildi", "bi-x-circle-fill", "bg-danger", 3);
    public static readonly TypeItem Expired = new(4, "Expired", "ConsentStatus.Expired", "Suresi doldu", "bi-clock-history", "bg-secondary", 4);

    public static IEnumerable<TypeItem> All => new[] { Pending, Granted, Revoked, Expired };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Pending = 1;
        public const int Granted = 2;
        public const int Revoked = 3;
        public const int Expired = 4;
    }
}

public static class DataSubjectRequestTypes
{
    public static readonly TypeItem Access = new(1, "Access", "DSR.Access", "Bilgi edinme hakki", "bi-info-circle-fill", "bg-primary", 1);
    public static readonly TypeItem Rectification = new(2, "Rectification", "DSR.Rectification", "Duzeltme hakki", "bi-pencil-fill", "bg-info", 2);
    public static readonly TypeItem Erasure = new(3, "Erasure", "DSR.Erasure", "Silme hakki", "bi-trash-fill", "bg-danger", 3);
    public static readonly TypeItem Restriction = new(4, "Restriction", "DSR.Restriction", "Kisitlama hakki", "bi-slash-circle-fill", "bg-warning text-dark", 4);
    public static readonly TypeItem Portability = new(5, "Portability", "DSR.Portability", "Tasinabilirlik hakki", "bi-box-arrow-right", "bg-success", 5);
    public static readonly TypeItem Objection = new(6, "Objection", "DSR.Objection", "Itiraz hakki", "bi-hand-thumbs-down-fill", "bg-secondary", 6);

    public static IEnumerable<TypeItem> All => new[] { Access, Rectification, Erasure, Restriction, Portability, Objection };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Access = 1;
        public const int Rectification = 2;
        public const int Erasure = 3;
        public const int Restriction = 4;
        public const int Portability = 5;
        public const int Objection = 6;
    }
}

public static class DataSubjectRequestStatuses
{
    public static readonly TypeItem Received = new(1, "Received", "DSRStatus.Received", "Alindi", "bi-envelope-fill", "bg-info", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "DSRStatus.InProgress", "Isleniyor", "bi-hourglass-split", "bg-warning text-dark", 2);
    public static readonly TypeItem Completed = new(3, "Completed", "DSRStatus.Completed", "Tamamlandi", "bi-check-circle-fill", "bg-success", 3);
    public static readonly TypeItem Rejected = new(4, "Rejected", "DSRStatus.Rejected", "Reddedildi", "bi-x-circle-fill", "bg-danger", 4);
    public static readonly TypeItem Overdue = new(5, "Overdue", "DSRStatus.Overdue", "Suresi gecti", "bi-exclamation-triangle-fill", "bg-danger", 5);

    public static IEnumerable<TypeItem> All => new[] { Received, InProgress, Completed, Rejected, Overdue };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Received = 1;
        public const int InProgress = 2;
        public const int Completed = 3;
        public const int Rejected = 4;
        public const int Overdue = 5;
    }
}

public static class BreachSeverities
{
    public static readonly TypeItem Low = new(1, "Low", "BreachSeverity.Low", "Dusuk", "bi-shield-fill", "bg-success", 1);
    public static readonly TypeItem Medium = new(2, "Medium", "BreachSeverity.Medium", "Orta", "bi-shield-fill-exclamation", "bg-warning text-dark", 2);
    public static readonly TypeItem High = new(3, "High", "BreachSeverity.High", "Yuksek", "bi-shield-fill-x", "bg-danger", 3);
    public static readonly TypeItem Critical = new(4, "Critical", "BreachSeverity.Critical", "Kritik", "bi-exclamation-triangle-fill", "bg-dark", 4);

    public static IEnumerable<TypeItem> All => new[] { Low, Medium, High, Critical };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Low = 1;
        public const int Medium = 2;
        public const int High = 3;
        public const int Critical = 4;
    }
}

public static class BreachStatuses
{
    public static readonly TypeItem Detected = new(1, "Detected", "BreachStatus.Detected", "Tespit edildi", "bi-eye-fill", "bg-danger", 1, isDefault: true);
    public static readonly TypeItem Investigating = new(2, "Investigating", "BreachStatus.Investigating", "Arastiriliyor", "bi-search", "bg-warning text-dark", 2);
    public static readonly TypeItem Contained = new(3, "Contained", "BreachStatus.Contained", "Kontrol altinda", "bi-shield-fill-check", "bg-info", 3);
    public static readonly TypeItem NotifiedAuthority = new(4, "NotifiedAuthority", "BreachStatus.NotifiedAuthority", "Kuruma bildirildi", "bi-building", "bg-primary", 4);
    public static readonly TypeItem NotifiedSubjects = new(5, "NotifiedSubjects", "BreachStatus.NotifiedSubjects", "Ilgili kisilere bildirildi", "bi-people-fill", "bg-primary", 5);
    public static readonly TypeItem Resolved = new(6, "Resolved", "BreachStatus.Resolved", "Cozumlendi", "bi-check-circle-fill", "bg-success", 6);

    public static IEnumerable<TypeItem> All => new[] { Detected, Investigating, Contained, NotifiedAuthority, NotifiedSubjects, Resolved };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Detected = 1;
        public const int Investigating = 2;
        public const int Contained = 3;
        public const int NotifiedAuthority = 4;
        public const int NotifiedSubjects = 5;
        public const int Resolved = 6;
    }
}

public static class DestructionMethods
{
    public static readonly TypeItem SoftDelete = new(1, "SoftDelete", "Destruction.SoftDelete", "Yumusak silme", "bi-trash", "bg-secondary", 1);
    public static readonly TypeItem Anonymization = new(2, "Anonymization", "Destruction.Anonymization", "Anonimlestime", "bi-person-x-fill", "bg-info", 2);
    public static readonly TypeItem CryptoErasure = new(3, "CryptoErasure", "Destruction.CryptoErasure", "Kriptografik silme", "bi-key-fill", "bg-warning text-dark", 3);
    public static readonly TypeItem PhysicalDestruction = new(4, "PhysicalDestruction", "Destruction.PhysicalDestruction", "Fiziksel imha", "bi-fire", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { SoftDelete, Anonymization, CryptoErasure, PhysicalDestruction };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int SoftDelete = 1;
        public const int Anonymization = 2;
        public const int CryptoErasure = 3;
        public const int PhysicalDestruction = 4;
    }
}

public static class RetentionCategories
{
    public static readonly TypeItem CallRecording = new(1, "CallRecording", "Retention.CallRecording", "Ses kayitlari", "bi-mic-fill", "bg-primary", 1);
    public static readonly TypeItem AuditLog = new(2, "AuditLog", "Retention.AuditLog", "Denetim kayitlari", "bi-journal-text", "bg-info", 2);
    public static readonly TypeItem AccessLog = new(3, "AccessLog", "Retention.AccessLog", "Erisim kayitlari", "bi-file-earmark-lock", "bg-success", 3);
    public static readonly TypeItem PersonalData = new(4, "PersonalData", "Retention.PersonalData", "Kisisel veriler", "bi-person-fill-lock", "bg-warning text-dark", 4);
    public static readonly TypeItem FinancialData = new(5, "FinancialData", "Retention.FinancialData", "Finansal veriler", "bi-currency-exchange", "bg-danger", 5);

    public static IEnumerable<TypeItem> All => new[] { CallRecording, AuditLog, AccessLog, PersonalData, FinancialData };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int CallRecording = 1;
        public const int AuditLog = 2;
        public const int AccessLog = 3;
        public const int PersonalData = 4;
        public const int FinancialData = 5;
    }
}

public static class TransferSafeguards
{
    public static readonly TypeItem AdequateCountry = new(1, "AdequateCountry", "Safeguard.AdequateCountry", "Yeterli koruma bulunan ulke", "bi-globe", "bg-success", 1);
    public static readonly TypeItem BindingCorporateRules = new(2, "BindingCorporateRules", "Safeguard.BindingCorporateRules", "Baglayici sirket kurallari", "bi-building", "bg-primary", 2);
    public static readonly TypeItem StandardContractualClauses = new(3, "StandardContractualClauses", "Safeguard.StandardContractualClauses", "Standart sozlesme hukumleri", "bi-file-earmark-text", "bg-info", 3);
    public static readonly TypeItem ExplicitConsent = new(4, "ExplicitConsent", "Safeguard.ExplicitConsent", "Acik riza", "bi-hand-thumbs-up", "bg-warning text-dark", 4);
    public static readonly TypeItem LegalObligation = new(5, "LegalObligation", "Safeguard.LegalObligation", "Kanuni zorunluluk", "bi-bank", "bg-secondary", 5);
    public static readonly TypeItem VitalInterest = new(6, "VitalInterest", "Safeguard.VitalInterest", "Hayati menfaat", "bi-heart-pulse", "bg-danger", 6);

    public static IEnumerable<TypeItem> All => new[] { AdequateCountry, BindingCorporateRules, StandardContractualClauses, ExplicitConsent, LegalObligation, VitalInterest };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int AdequateCountry = 1;
        public const int BindingCorporateRules = 2;
        public const int StandardContractualClauses = 3;
        public const int ExplicitConsent = 4;
        public const int LegalObligation = 5;
        public const int VitalInterest = 6;
    }
}
