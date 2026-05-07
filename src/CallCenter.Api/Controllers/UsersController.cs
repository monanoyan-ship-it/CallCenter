using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : AuditableControllerBase
{
    private readonly IUserFactory _userFactory;

    public UsersController(IAuditFactory auditFactory, IUserFactory userFactory) : base(auditFactory)
    {
        _userFactory = userFactory;
    }

    /// <summary>Sayfalamali kullanici listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null)
    {
        return Ok(await _userFactory.GetAllAsync(page, pageSize, search, roleId));
    }

    /// <summary>Kullanici detay</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserListDto>> GetById(int id)
    {
        var result = await _userFactory.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Kullanici bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni kullanici olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(UserCreateDto dto)
    {
        var (success, id, error) = await _userFactory.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "User", id.ToString(),
            $"Kullanici olusturuldu: '{dto.UserName}'");

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Kullanici guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UserUpdateDto dto)
    {
        var (success, error) = await _userFactory.UpdateAsync(id, dto);
        if (!success)
        {
            if (error == "Kullanici bulunamadi.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        await AuditCrudAsync("Update", "User", id.ToString(),
            $"Kullanici guncellendi: ID={id}");

        return NoContent();
    }

    /// <summary>Email adresini manuel olarak dogrulanmis isaretle (admin)</summary>
    [HttpPost("{id}/verify-email-manual")]
    public async Task<ActionResult> VerifyEmailManually(int id)
    {
        var (success, error) = await _userFactory.VerifyEmailManuallyAsync(id);
        if (!success)
        {
            if (error == "Kullanıcı bulunamadı.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        await AuditCrudAsync("VerifyEmailManual", "User", id.ToString(),
            $"Email manuel dogrulandi: ID={id}");

        return NoContent();
    }

    /// <summary>Kullanici sil (soft delete — IsActive=false)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _userFactory.DeleteAsync(id, currentUserId);
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
