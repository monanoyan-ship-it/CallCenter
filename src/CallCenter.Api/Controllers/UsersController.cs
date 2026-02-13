using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : AuditableControllerBase
{
    public UsersController(ServiceFactory factory) : base(factory) { }

    /// <summary>Sayfalamali kullanici listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null)
    {
        var svc = Factory.CreateUserService();
        return Ok(await svc.GetAllAsync(page, pageSize, search, roleId));
    }

    /// <summary>Kullanici detay</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserListDto>> GetById(int id)
    {
        var svc = Factory.CreateUserService();
        var result = await svc.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Kullanici bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni kullanici olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(UserCreateDto dto)
    {
        var svc = Factory.CreateUserService();
        var (success, id, error) = await svc.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "User", id.ToString(),
            $"Kullanici olusturuldu: '{dto.UserName}'");

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Kullanici guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UserUpdateDto dto)
    {
        var svc = Factory.CreateUserService();
        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success)
        {
            if (error == "Kullanici bulunamadi.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        await AuditCrudAsync("Update", "User", id.ToString(),
            $"Kullanici guncellendi: ID={id}");

        return NoContent();
    }

    /// <summary>Kullanici sil (soft delete — IsActive=false)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var svc = Factory.CreateUserService();
        var (success, error) = await svc.DeleteAsync(id, currentUserId);
        if (!success)
        {
            if (error == "Kullanici bulunamadi.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        await AuditCrudAsync("Delete", "User", id.ToString(),
            $"Kullanici silindi (deaktif): ID={id}");

        return NoContent();
    }
}
