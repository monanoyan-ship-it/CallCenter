using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CallCenter.Landing.Pages;

public class DownloadModel : PageModel
{
    public string WindowsVersion { get; set; } = "0.4.0";
    public string WindowsDownloadUrl { get; set; } = "https://storage.googleapis.com/corplynk-releases/CorpLynk-0.4.0-setup.exe";

    public void OnGet()
    {
    }
}
