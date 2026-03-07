using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private readonly IConfiguration _config;

    public VersionController(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>Belirli bir uygulama icin guncel versiyon bilgisi</summary>
    [HttpGet("check")]
    public ActionResult<AppVersionInfoDto> Check([FromQuery] string appName, [FromQuery] string currentVersion)
    {
        var section = _config.GetSection($"AppVersions:{appName}");
        if (!section.Exists())
            return NotFound(new { message = $"'{appName}' uygulamasi tanimli degil." });

        var latestVersion = section["LatestVersion"] ?? "1.0.0";
        var downloadUrl = section["DownloadUrl"];
        var releaseNotes = section["ReleaseNotes"];
        var isMandatory = bool.TryParse(section["IsMandatory"], out var m) && m;

        return Ok(new AppVersionInfoDto
        {
            AppName = appName,
            LatestVersion = latestVersion,
            DownloadUrl = downloadUrl,
            ReleaseNotes = releaseNotes,
            IsMandatory = isMandatory
        });
    }
}
