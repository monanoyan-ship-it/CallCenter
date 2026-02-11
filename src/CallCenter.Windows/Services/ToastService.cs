namespace CallCenter.Windows.Services;

public class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowSuccess(string message) => Show(message, ToastType.Success);
    public void ShowError(string message) => Show(message, ToastType.Error);
    public void ShowWarning(string message) => Show(message, ToastType.Warning);
    public void ShowInfo(string message) => Show(message, ToastType.Info);

    private void Show(string message, ToastType type)
    {
        OnShow?.Invoke(new ToastMessage(message, type));
    }
}

public record ToastMessage(string Message, ToastType Type);

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}
