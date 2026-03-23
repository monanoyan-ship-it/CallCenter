using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CallCenter.Landing.Pages;

public class DownloadModel : PageModel
{
    public string WindowsVersion { get; set; } = "0.3.1";
    public string WindowsDownloadUrl { get; set; } = "https://storage.googleapis.com/corplynk-releases/CorpLynk-0.3.1-setup.exe";

    public void OnGet()
    {
    }
}
