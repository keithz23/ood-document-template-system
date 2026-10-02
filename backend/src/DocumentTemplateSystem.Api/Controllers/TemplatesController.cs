using DocumentTemplateSystem.Application.Authorization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTemplateSystem.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.TemplatesView)]
[Route("api/templates")]
[Produces("application/json")]
public sealed class TemplatesController(TemplateService templateService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateGalleryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TemplateGalleryItemDto>>> GetActiveTemplates(
        CancellationToken cancellationToken)
    {
        return Ok(await templateService.GetActiveTemplatesAsync(cancellationToken));
    }

    [HttpGet("{templateId}")]
    [ProducesResponseType(typeof(TemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TemplateDetailDto>> GetTemplateDetail(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        return Ok(await templateService.GetTemplateDetailAsync(
            templateId,
            cancellationToken));
    }

    [HttpGet("{templateId}/current-version")]
    [ProducesResponseType(typeof(TemplateVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TemplateVersionDetailDto>> GetCurrentVersion(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        return Ok(await templateService.GetCurrentVersionAsync(
            templateId,
            cancellationToken));
    }
}
