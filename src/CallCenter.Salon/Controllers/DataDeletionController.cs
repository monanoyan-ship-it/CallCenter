using System.Net.Http.Json;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

[AllowAnonymous]
public class DataDeletionController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DataDeletionController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("data-deletion")]
    [HttpGet("kvkk-request")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("data-deletion")]
    [HttpPost("kvkk-request")]
    public async Task<IActionResult> Submit([FromBody] PublicDataSubjectRequestCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        dto.Source = string.IsNullOrWhiteSpace(dto.Source) ? "sln-web" : dto.Source;

        var client = _httpClientFactory.CreateClient("SalonApi");
        var response = await client.PostAsJsonAsync("api/kvkk/public/requests", dto);
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }
}
