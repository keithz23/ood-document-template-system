using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTemplateSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = "User,Admin")]
[Route("api/documents")]
[Produces("application/json")]
public sealed class DocumentsController(DocumentService documentService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentDetailDto>> CreateDraft(
        CreateDraftDocumentRequestDto request,
        CancellationToken cancellationToken)
    {
        var document = await documentService.CreateDraftAsync(request, cancellationToken);
        return Created($"/api/documents/{document.Id}", document);
    }
}
