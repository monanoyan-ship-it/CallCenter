namespace CallCenter.Shared.Entities;

/// <summary>
/// Musteri memnuniyet anketi sablonu.
/// Firma admini olusturur, operator arama sonrasi doldurur.
/// </summary>
public class CrmSurvey
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>SurveyTypes TypeItem referansi (CSAT=1, NPS=2, Custom=3)</summary>
    public int TypeId { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int CreatedByPersonnelId { get; set; }
    public CustomerPersonnel CreatedByPersonnel { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CrmSurveyQuestion> Questions { get; set; } = new List<CrmSurveyQuestion>();
    public ICollection<CrmSurveyResponse> Responses { get; set; } = new List<CrmSurveyResponse>();
}

/// <summary>
/// Anket sorusu.
/// </summary>
public class CrmSurveyQuestion
{
    public int Id { get; set; }

    public int SurveyId { get; set; }
    public CrmSurvey Survey { get; set; } = null!;

    public string Text { get; set; } = string.Empty;

    /// <summary>SurveyQuestionTypes TypeItem referansi (Rating=1, MultiChoice=2, Text=3, YesNo=4)</summary>
    public int QuestionTypeId { get; set; } = 1;

    /// <summary>Coktan secmeli secenekler (JSON array: ["Cok iyi","Iyi","Orta","Kotu"])</summary>
    public string? Options { get; set; }

    /// <summary>Puan tipi sorularda min deger (orn: 1)</summary>
    public int? MinValue { get; set; }

    /// <summary>Puan tipi sorularda max deger (CSAT: 5, NPS: 10)</summary>
    public int? MaxValue { get; set; }

    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Bir kisinin bir ankete verdigi toplam yanit.
/// </summary>
public class CrmSurveyResponse
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public int SurveyId { get; set; }
    public CrmSurvey Survey { get; set; } = null!;

    /// <summary>Yanit veren kisi (CRM contact)</summary>
    public int? ContactId { get; set; }
    public Contact? Contact { get; set; }

    /// <summary>Ilgili arama kaydi</summary>
    public int? CallRecordId { get; set; }
    public CallRecord? CallRecord { get; set; }

    /// <summary>Yanit verenin telefonu (contact yoksa)</summary>
    public string? RespondentPhone { get; set; }

    /// <summary>Yanit verenin adi (contact yoksa)</summary>
    public string? RespondentName { get; set; }

    /// <summary>Genel skor (ortalama veya NPS degeri)</summary>
    public decimal? OverallScore { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Anketi dolduran personel</summary>
    public int? CreatedByPersonnelId { get; set; }
    public CustomerPersonnel? CreatedByPersonnel { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public ICollection<CrmSurveyAnswer> Answers { get; set; } = new List<CrmSurveyAnswer>();
}

/// <summary>
/// Tek soru yaniti.
/// </summary>
public class CrmSurveyAnswer
{
    public int Id { get; set; }

    public int ResponseId { get; set; }
    public CrmSurveyResponse Response { get; set; } = null!;

    public int QuestionId { get; set; }
    public CrmSurveyQuestion Question { get; set; } = null!;

    /// <summary>Metin/secim yaniti</summary>
    public string? AnswerText { get; set; }

    /// <summary>Puan yaniti</summary>
    public int? AnswerScore { get; set; }
}
