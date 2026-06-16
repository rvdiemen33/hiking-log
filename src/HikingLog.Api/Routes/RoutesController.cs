namespace HikingLog.Api.Routes;

using HikingLog.Api.Extensions;
using HikingLog.Application.Common;
using HikingLog.Application.Routes.Commands;
using HikingLog.Application.Routes.Queries;
using Microsoft.AspNetCore.Mvc;
using OneOf;

/// <summary>Controller for managing long-distance hiking routes.</summary>
[ApiController]
[Route("[controller]")]
public sealed class RoutesController(
    ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>> addHandler,
    ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>> updateHandler,
    ICommandHandler<DeleteRoute, OneOf<Success, NotFound>> deleteHandler,
    IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>> getRoutesHandler,
    IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>> getRouteHandler) : ControllerBase
{
    /// <summary>Gets all routes.</summary>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>A list of all routes.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RouteResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await getRoutesHandler.Handle(new GetRoutes(), ct);
        return Ok(result.Select(r => r.ToResponse()));
    }

    /// <summary>Gets a single route by id.</summary>
    /// <param name="id">The primary key of the route.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The matching route, or 404 if not found.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await getRouteHandler.Handle(new GetRoute(id), ct);
        return result.Match<IActionResult>(dto => Ok(dto.ToResponse()), _ => NotFound());
    }

    /// <summary>Creates a new route.</summary>
    /// <param name="request">The route data.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>201 Created with the new route, or 400 Bad Request on validation failure.</returns>
    [HttpPost]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var result = await addHandler.Handle(request.ToCommand(), ct);
        return result.Match<IActionResult>(
            r => CreatedAtAction(nameof(GetById), new { id = r.Id },
                new RouteResponse(r.Id, request.Name, request.Code, request.Country, request.TotalDistanceKm, request.Description)),
            v => ValidationProblem(v.ToModelStateDictionary()));
    }

    /// <summary>Updates an existing route.</summary>
    /// <param name="id">The primary key of the route to update.</param>
    /// <param name="request">The updated route data.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>200 OK with the updated route, 400 on validation failure, or 404 if not found.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRouteRequest request, CancellationToken ct)
    {
        var result = await updateHandler.Handle(request.ToCommand(id), ct);
        return result.Match<IActionResult>(
            r => Ok(new RouteResponse(r.Id, request.Name, request.Code, request.Country, request.TotalDistanceKm, request.Description)),
            v => ValidationProblem(v.ToModelStateDictionary()),
            _ => NotFound());
    }

    /// <summary>Deletes a route.</summary>
    /// <param name="id">The primary key of the route to delete.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>204 No Content on success, or 404 if not found.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await deleteHandler.Handle(new DeleteRoute(id), ct);
        return result.Match<IActionResult>(_ => NoContent(), _ => NotFound());
    }
}
