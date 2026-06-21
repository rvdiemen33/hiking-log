namespace HikingLog.Api.Stages;

using HikingLog.Api.Extensions;
using HikingLog.Application.Common;
using HikingLog.Application.Stages.Commands;
using HikingLog.Application.Stages.Queries;
using Microsoft.AspNetCore.Mvc;
using OneOf;

/// <summary>Controller for managing hiking stages.</summary>
[ApiController]
public sealed class StagesController(
    ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>> addHandler,
    ICommandHandler<UpdateStage, OneOf<UpdateStageResult, ValidationFailed, NotFound>> updateHandler,
    ICommandHandler<DeleteStage, OneOf<Success, NotFound>> deleteHandler,
    IQueryHandler<GetStagesByRoute, IReadOnlyList<StageDto>> getStagesByRouteHandler,
    IQueryHandler<GetStage, OneOf<StageDto, NotFound>> getStageHandler) : ControllerBase
{
    /// <summary>Gets all stages for a route.</summary>
    /// <param name="routeId">The primary key of the parent route.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>A list of all stages belonging to the specified route.</returns>
    [HttpGet("routes/{routeId:int}/stages")]
    [ProducesResponseType<IReadOnlyList<StageResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int routeId, CancellationToken ct)
    {
        var result = await getStagesByRouteHandler.Handle(new GetStagesByRoute(routeId), ct);
        return Ok(result.Select(s => s.ToResponse()));
    }

    /// <summary>Gets a single stage by id.</summary>
    /// <param name="id">The primary key of the stage.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The matching stage, or 404 if not found.</returns>
    [HttpGet("stages/{id:int}")]
    [ProducesResponseType<StageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await getStageHandler.Handle(new GetStage(id), ct);
        return result.Match<IActionResult>(dto => Ok(dto.ToResponse()), _ => NotFound());
    }

    /// <summary>Creates a new stage.</summary>
    /// <param name="request">The stage data.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>201 Created with the new stage, 400 on validation failure, or 404 if the parent route does not exist.</returns>
    [HttpPost("stages")]
    [ProducesResponseType<StageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateStageRequest request, CancellationToken ct)
    {
        var result = await addHandler.Handle(request.ToCommand(), ct);
        return result.Match<IActionResult>(
            r => CreatedAtAction(nameof(GetById), new { id = r.Id },
                new StageResponse(r.Id, request.RouteId, request.Number, request.Name, request.StartPoint,
                    request.EndPoint, request.DistanceKm, request.ElevationDifferenceM, request.Difficulty)),
            v => ValidationProblem(v.ToModelStateDictionary()),
            _ => NotFound());
    }

    /// <summary>Updates an existing stage.</summary>
    /// <param name="id">The primary key of the stage to update.</param>
    /// <param name="request">The updated stage data.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>200 OK with the updated stage, 400 on validation failure, or 404 if not found.</returns>
    [HttpPut("stages/{id:int}")]
    [ProducesResponseType<StageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStageRequest request, CancellationToken ct)
    {
        var result = await updateHandler.Handle(request.ToCommand(id), ct);
        return result.Match<IActionResult>(
            r => Ok(new StageResponse(r.Id, request.RouteId, request.Number, request.Name, request.StartPoint,
                request.EndPoint, request.DistanceKm, request.ElevationDifferenceM, request.Difficulty)),
            v => ValidationProblem(v.ToModelStateDictionary()),
            _ => NotFound());
    }

    /// <summary>Deletes a stage.</summary>
    /// <param name="id">The primary key of the stage to delete.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>204 No Content on success, or 404 if not found.</returns>
    [HttpDelete("stages/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await deleteHandler.Handle(new DeleteStage(id), ct);
        return result.Match<IActionResult>(_ => NoContent(), _ => NotFound());
    }
}
