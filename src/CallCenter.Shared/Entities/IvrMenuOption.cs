namespace CallCenter.Shared.Entities;

/// <summary>
/// IVR menusundeki tek bir tus secenegi.
/// Ornek: Digit=1, Action=transfer_queue, TargetQueueId=5, Label="Satis"
/// </summary>
public class IvrMenuOption
{
    public int Id { get; set; }
    public int IvrMenuId { get; set; }

    /// <summary>DTMF tusu: "0"-"9", "*", "#"</summary>
    public string Digit { get; set; } = string.Empty;

    /// <summary>Aksiyonlar: transfer_queue, transfer_extension, play_message, sub_menu, voicemail, hangup, repeat</summary>
    public string ActionType { get; set; } = "transfer_queue";

    /// <summary>transfer_queue aksiyonunda hedef kuyruk</summary>
    public int? TargetQueueId { get; set; }

    /// <summary>transfer_extension aksiyonunda hedef dahili numara</summary>
    public string? TargetExtension { get; set; }

    /// <summary>sub_menu aksiyonunda hedef alt menu</summary>
    public int? TargetIvrMenuId { get; set; }

    /// <summary>play_message aksiyonunda calinacak mesaj</summary>
    public int? TargetGreetingMessageId { get; set; }

    /// <summary>Gorsel etiket: "Satis", "Destek" vb.</summary>
    public string? Label { get; set; }

    public int SortOrder { get; set; }

    // Navigation
    public IvrMenu IvrMenu { get; set; } = null!;
    public Queue? TargetQueue { get; set; }
    public IvrMenu? TargetIvrMenu { get; set; }
    public GreetingMessage? TargetGreetingMessage { get; set; }
}
