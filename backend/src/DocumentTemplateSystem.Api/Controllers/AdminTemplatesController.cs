using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTemplateSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/templates")]
[Produces("application/json")]
public sealed class AdminTemplatesController(
    AdminCatalogService service,
    AdminTemplateVersionService versionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminTemplateSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AdminTemplateSummaryDto>>> GetTemplates(
        CancellationToken cancellationToken) =>
        Ok(await service.GetTemplatesAsync(cancellationToken));

    [HttpGet("{templateId:guid}")]
    [ProducesResponseType(typeof(AdminTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminTemplateDetailDto>> GetTemplate(
        Guid templateId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetTemplateAsync(templateId, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(AdminTemplateDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminTemplateDetailDto>> CreateTemplate(
        CreateTemplateRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateTemplateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTemplate), new { templateId = result.Id }, result);
    }

    [HttpPatch("{templateId:guid}")]
    [ProducesResponseType(typeof(AdminTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminTemplateDetailDto>> UpdateTemplate(
        Guid templateId,
        UpdateDraftTemplateRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateTemplateAsync(templateId, request, cancellationToken));

    [HttpPost("{templateId:guid}/activate")]
    [ProducesResponseType(typeof(AdminTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminTemplateDetailDto>> ActivateTemplate(
        Guid templateId,
        CancellationToken cancellationToken) =>
        Ok(await service.ActivateTemplateAsync(templateId, cancellationToken));

    [HttpPost("{templateId:guid}/deactivate")]
    [ProducesResponseType(typeof(AdminTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminTemplateDetailDto>> DeactivateTemplate(
        Guid templateId,
        CancellationToken cancellationToken) =>
        Ok(await service.DeactivateTemplateAsync(templateId, cancellationToken));

    [HttpGet("{templateId:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminTemplateVersionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AdminTemplateVersionSummaryDto>>> GetVersions(
        Guid templateId,
        CancellationToken cancellationToken) =>
        Ok(await versionService.GetVersionsAsync(templateId, cancellationToken));

    [HttpPost("{templateId:guid}/versions")]
    [ProducesResponseType(typeof(AdminTemplateVersionDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminTemplateVersionDetailDto>> CreateDraftVersion(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var result = await versionService.CreateDraftVersionAsync(templateId, cancellationToken);
        return Created($"/api/admin/template-versions/{result.Id}", result);
    }
}
