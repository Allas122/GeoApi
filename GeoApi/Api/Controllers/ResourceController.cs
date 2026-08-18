using GeoApi.Api.Dto;
using GeoApi.Api.Mappers;
using GeoApi.Api.Messages;
using GeoApi.Application.Abstractions;
using GeoApi.Application.Dto;
using Microsoft.AspNetCore.Mvc;

namespace GeoApi.Api.Controllers;

[ApiController]
[Route("api/resource")]
public class ResourceController(IResourceService resourceService) : ControllerBase
{
    [HttpPost("batch")]
    [ProducesResponseType<IReadOnlyList<CreatedResourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBatch(
        [FromBody] CreateResourceBatchMessage message,
        CancellationToken ct)
    {
        IReadOnlyList<CreatedResourceDto> created =
            await resourceService.CreateBatchAsync(message.Resources.MapToCreateResourceDtos(), ct);
        return Ok(created.MapToResponses());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ResourceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateResourceMessage message,
        CancellationToken ct)
    {
        ResourceDto resource = await resourceService.UpdateAsync(message.MapToUpdateDto(id), ct);
        return Ok(resource.MapToResponse());
    }

    [HttpPut("{id:int}/locations")]
    [ProducesResponseType<IReadOnlyList<int>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReplaceLocations(
        int id,
        [FromBody] ReplaceResourceLocationsMessage message,
        CancellationToken ct)
    {
        IReadOnlyList<int> locationIds =
            await resourceService.ReplaceLocationsAsync(message.MapToReplacementDto(id), ct);
        return Ok(locationIds);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await resourceService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/locations/{locationId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkLocation(int id, int locationId, CancellationToken ct)
    {
        await resourceService.LinkLocationAsync(id, locationId, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/locations/{locationId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkLocation(int id, int locationId, CancellationToken ct)
    {
        await resourceService.UnlinkLocationAsync(id, locationId, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/locations")]
    [ProducesResponseType<IReadOnlyList<int>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnlinkLocations(
        int id,
        [FromQuery] UnlinkResourceLocationsQuery query,
        CancellationToken ct)
    {
        IReadOnlyList<int> unlinked =
            await resourceService.BulkUnlinkLocationsAsync(query.MapToUnlinkDto(id), ct);
        return Ok(unlinked);
    }

    [HttpGet("by-ids")]
    [ProducesResponseType<IReadOnlyList<ResourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByIds([FromQuery] GetResourcesByIdsQuery query, CancellationToken ct)
    {
        IReadOnlyList<ResourceDto> resources = await resourceService.GetByIdsAsync(query.MapToQueryDto(), ct);
        return Ok(resources.MapToResponses());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<ResourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPage([FromQuery] GetResourcesPageQuery query, CancellationToken ct)
    {
        PagedResultDto<ResourceDto> page = await resourceService.GetPageAsync(query.MapToQueryDto(), ct);
        return Ok(page.MapToResponse(ResourceMapper.MapToResponse));
    }

    [HttpGet("subtree")]
    [ProducesResponseType<PagedResponse<ResourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSubtree([FromQuery] GetResourceSubtreeQuery query, CancellationToken ct)
    {
        PagedResultDto<ResourceDto> page = await resourceService.GetSubtreeAsync(query.MapToQueryDto(), ct);
        return Ok(page.MapToResponse(ResourceMapper.MapToResponse));
    }

    [HttpGet("ancestors")]
    [ProducesResponseType<PagedResponse<ResourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAncestors([FromQuery] GetResourceAncestorsQuery query, CancellationToken ct)
    {
        PagedResultDto<ResourceDto> page = await resourceService.GetAncestorsAsync(query.MapToQueryDto(), ct);
        return Ok(page.MapToResponse(ResourceMapper.MapToResponse));
    }

    [HttpGet("search")]
    [ProducesResponseType<PagedResponse<ResourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] GetResourcesByBranchPatternQuery query,
        CancellationToken ct)
    {
        PagedResultDto<ResourceDto> page = await resourceService.GetByBranchPatternAsync(query.MapToQueryDto(), ct);
        return Ok(page.MapToResponse(ResourceMapper.MapToResponse));
    }

    [HttpGet("by-location/{locationId:int}")]
    [ProducesResponseType<PagedResponse<ResourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByLocationId(
        int locationId,
        [FromQuery] GetResourcesByLocationIdQuery query,
        CancellationToken ct)
    {
        PagedResultDto<ResourceDto> page =
            await resourceService.GetByLocationIdAsync(query.MapToQueryDto(locationId), ct);
        return Ok(page.MapToResponse(ResourceMapper.MapToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ResourceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        int id,
        [FromQuery] GetResourceByIdQuery query,
        CancellationToken ct)
    {
        ResourceDto resource = await resourceService.GetByIdAsync(query.MapToQueryDto(id), ct);
        return Ok(resource.MapToResponse());
    }

    [HttpGet("{id:int}/locations")]
    [ProducesResponseType<PagedResponse<LocationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLocations(
        int id,
        [FromQuery] GetResourceLocationsQuery query,
        CancellationToken ct)
    {
        PagedResultDto<LocationDto> page = await resourceService.GetLocationsAsync(query.MapToQueryDto(id), ct);
        return Ok(page.MapToResponse(LocationMapper.MapToResponse));
    }
}
