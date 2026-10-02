using DocumentTemplateSystem.Application.Authorization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTemplateSystem.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.TemplatesManage)]
[Route("api/admin/template-versions")]
[Produces("application/json")]
public sealed class AdminTemplateVersionsController(AdminTemplateVersionService service)
    : ControllerBase
{
    [HttpGet("{versionId:guid}")]
    [ProducesResponseType(typeof(AdminTemplateVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminTemplateVersionDetailDto>> GetVersion(
        Guid versionId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetVersionAsync(versionId, cancellationToken));

    [HttpPatch("{versionId:guid}")]
    [ProducesResponseType(typeof(AdminTemplateVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminTemplateVersionDetailDto>> UpdateVersion(
        Guid versionId,
        UpdateDraftTemplateVersionRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateDraftVersionAsync(versionId, request, cancellationToken));

    [HttpPost("{versionId:guid}/publish")]
    [ProducesResponseType(typeof(AdminTemplateVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AdminTemplateVersionDetailDto>> Publish(
        Guid versionId,
        CancellationToken cancellationToken) =>
        Ok(await service.PublishAsync(versionId, cancellationToken));

    [HttpPost("{versionId:guid}/set-current")]
    [ProducesResponseType(typeof(AdminTemplateVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminTemplateVersionDetailDto>> SetCurrent(
        Guid versionId,
        CancellationToken cancellationToken) =>
        Ok(await service.SetCurrentAsync(versionId, cancellationToken));

    [HttpGet("{versionId:guid}/placeholders")]
    [ProducesResponseType(typeof(IReadOnlyList<PlaceholderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PlaceholderDto>>> GetPlaceholders(
        Guid versionId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetPlaceholdersAsync(versionId, cancellationToken));

    [HttpPost("{versionId:guid}/placeholders")]
    [ProducesResponseType(typeof(PlaceholderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlaceholderDto>> CreatePlaceholder(
        Guid versionId,
        CreatePlaceholderRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreatePlaceholderAsync(versionId, request, cancellationToken);
        return Created(
            $"/api/admin/template-versions/{versionId}/placeholders/{result.Id}",
            result);
    }

    [HttpPatch("{versionId:guid}/placeholders/{placeholderId:guid}")]
    [ProducesResponseType(typeof(PlaceholderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlaceholderDto>> UpdatePlaceholder(
        Guid versionId,
        Guid placeholderId,
        UpdatePlaceholderRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdatePlaceholderAsync(
            versionId,
            placeholderId,
            request,
            cancellationToken));

    [HttpDelete("{versionId:guid}/placeholders/{placeholderId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemovePlaceholder(
        Guid versionId,
        Guid placeholderId,
        CancellationToken cancellationToken)
    {
        await service.RemovePlaceholderAsync(versionId, placeholderId, cancellationToken);
        return NoContent();
    }
}
