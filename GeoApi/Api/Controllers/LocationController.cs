using GeoApi.Api.Dto;
using GeoApi.Api.Mappers;
using GeoApi.Api.Messages;
using GeoApi.Application.Abstractions;
using GeoApi.Application.Dto;
using Microsoft.AspNetCore.Mvc;

namespace GeoApi.Api.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController(ILocationService locationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<int>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationMessage location, CancellationToken ct)
    {
        int id = await locationService.CreateAsync(location.Point!.MapToPointDto(), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("batch")]
    [ProducesResponseType<IReadOnlyList<int>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocationBatch(
        [FromBody] CreateLocationBatchMessage message,
        CancellationToken ct)
    {
        IReadOnlyList<int> ids = await locationService.BulkCreateAsync(message.Points.MapToPointDtos(), ct);
        return Ok(ids);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateLocation(
        int id,
        [FromBody] UpdateLocationMessage message,
        CancellationToken ct)
    {
        LocationDto location = await locationService.UpdateAsync(message.MapToUpdateDto(id), ct);
        return Ok(location.MapToResponse());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        LocationDto location = await locationService.GetByIdAsync(id, ct);
        return Ok(location.MapToResponse());
    }

    [HttpGet("radius")]
    [ProducesResponseType<PagedResponse<LocationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInRadius([FromQuery] GetLocationsInRadiusQuery query, CancellationToken ct)
    {
        PagedResultDto<LocationDto> page = await locationService.GetInRadiusAsync(query.MapToQueryDto(), ct);
        return Ok(page.MapToResponse(LocationMapper.MapToResponse));
    }

    [HttpGet("clusters")]
    [ProducesResponseType<IReadOnlyList<GridClusterResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetClusters([FromQuery] GetLocationClustersQuery query, CancellationToken ct)
    {
        IReadOnlyList<GridClusterDto> clusters = await locationService.GetClustersAsync(query.MapToQueryDto(), ct);
        return Ok(clusters.MapToResponses());
    }
}
