using GeoApi.Api.Mappers;
using GeoApi.Api.Messages;
using GeoApi.Application.Abstractions;
using GeoApi.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;

namespace GeoApi.Api.Controllers;

[ApiController]
[Route("location")]
public class LocationController(ILocationService locationService) : ControllerBase
{
    private readonly ILocationService _locationService = locationService;

    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationMessage location)
    {
        int id = await _locationService.CreateAsync(location.Point.MapToPoint());
        return Ok(id);
    }
}