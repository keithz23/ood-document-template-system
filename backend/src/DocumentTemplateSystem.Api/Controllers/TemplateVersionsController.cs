using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTemplateSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = "User,Admin")]
[Route("api/template-versions")]
[Produces("application/json")]
public sealed class TemplateVersionsController(TemplateService templateService) : ControllerBase
{
    [HttpGet("{templateVersionId}/placeholders")]
    [ProducesResponseType(typeof(IReadOnlyList<PlaceholderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<PlaceholderDto>>> GetPlaceholders(
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        return Ok(await templateService.GetCurrentVersionPlaceholdersAsync(
            templateVersionId,
            cancellationToken));
    }
}
