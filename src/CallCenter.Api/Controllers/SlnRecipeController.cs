using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-recipes")]
[Authorize]
public class SlnRecipeController : ControllerBase
{
    private readonly ISlnRecipeFactory _recipeFactory;

    public SlnRecipeController(ISlnRecipeFactory recipeFactory) => _recipeFactory = recipeFactory;

    [HttpGet]
    public async Task<ActionResult<List<SlnRecipeDto>>> GetRecipes()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _recipeFactory.GetRecipesAsync(customerId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnRecipeDto>> GetRecipe(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var recipe = await _recipeFactory.GetRecipeAsync(id, customerId);
        return recipe != null ? Ok(recipe) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnRecipeDto>> CreateRecipe([FromBody] SlnRecipeCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var recipe = await _recipeFactory.CreateRecipeAsync(dto, customerId);
        return Ok(recipe);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateRecipe(int id, [FromBody] SlnRecipeCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _recipeFactory.UpdateRecipeAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRecipe(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _recipeFactory.DeleteRecipeAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
