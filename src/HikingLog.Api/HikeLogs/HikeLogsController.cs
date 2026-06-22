namespace HikingLog.Api.HikeLogs;

using HikingLog.Api.Extensions;
using HikingLog.Application.Common;
using HikingLog.Application.HikeLogs.Commands;
using HikingLog.Application.HikeLogs.Queries;
using Microsoft.AspNetCore.Mvc;
using OneOf;

/// <summary>Controller for managing hike log entries.</summary>
[ApiController]
public sealed class HikeLogsController(
    ICommandHandler<AddHikeLog, OneOf<AddHikeLogResult, ValidationFailed, NotFound>> addHandler,
    ICommandHandler<UpdateHikeLog, OneOf<UpdateHikeLogResult, ValidationFailed, NotFound>> updateHandler,
    ICommandHandler<DeleteHikeLog, OneOf<Success, NotFound>> deleteHandler,
    IQueryHandler<GetHikeLogs, IReadOnlyList<HikeLogDto>> getHikeLogsHandler,
    IQueryHandler<GetHikeLog, OneOf<HikeLogDto, NotFound>> getHikeLogHandler,
    IQueryHandler<GetHikeLogsByStage, IReadOnlyList<HikeLogDto>> getHikeLogsByStageHandler) : ControllerBase
{
    /// <summary>Gets all hike logs, optionally filtered by year.</summary>
    /// <param name="year">When provided, restricts results to logs whose hike date falls in this year.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>A list of all hike logs, optionally filtered by year.</returns>
    [HttpGet("hikelogs")]
    [ProducesResponseType<IReadOnlyList<HikeLogResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? year, CancellationToken ct)
    {
        var result = await getHikeLogsHandler.Handle(new GetHikeLogs(year), ct);
        return Ok(result.Select(h => h.ToResponse()));
    }

    /// <summary>Gets a single hike log by id.</summary>
    /// <param name="id">The primary key of the hike log.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The matching hike log, or 404 if not found.</returns>
    [HttpGet("hikelogs/{id:int}")]
    [ProducesResponseType<HikeLogResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await getHikeLogHandler.Handle(new GetHikeLog(id), ct);
        return result.Match<IActionResult>(dto => Ok(dto.ToResponse()), _ => NotFound());
    }

    /// <summary>Gets all hike logs for a specific stage.</summary>
    /// <param name="stageId">The primary key of the parent stage.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>A list of all hike logs belonging to the specified stage.</returns>
    [HttpGet("stages/{stageId:int}/hikelogs")]
    [ProducesResponseType<IReadOnlyList<HikeLogResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStage(int stageId, CancellationToken ct)
    {
        var result = await getHikeLogsByStageHandler.Handle(new GetHikeLogsByStage(stageId), ct);
        return Ok(result.Select(h => h.ToResponse()));
    }

    /// <summary>Creates a new hike log entry.</summary>
    /// <param name="request">The hike log data.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>201 Created with the new hike log, 400 on validation failure, or 404 if the parent stage does not exist.</returns>
    [HttpPost("hikelogs")]
    [ProducesResponseType<HikeLogResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateHikeLogRequest request, CancellationToken ct)
    {
        var result = await addHandler.Handle(request.ToCommand(), ct);
        return result.Match<IActionResult>(
            r => CreatedAtAction(nameof(GetById), new { id = r.Id },
                new HikeLogResponse(r.Id, request.StageId, request.DateHiked, request.DurationMinutes,
                    request.Weather, request.Notes, request.Rating)),
            v => ValidationProblem(v.ToModelStateDictionary()),
            _ => NotFound());
    }

    /// <summary>Updates an existing hike log entry.</summary>
    /// <param name="id">The primary key of the hike log to update.</param>
    /// <param name="request">The updated hike log data.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>200 OK with the updated hike log, 400 on validation failure, or 404 if not found.</returns>
    [HttpPut("hikelogs/{id:int}")]
    [ProducesResponseType<HikeLogResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHikeLogRequest request, CancellationToken ct)
    {
        var result = await updateHandler.Handle(request.ToCommand(id), ct);
        return result.Match<IActionResult>(
            r => Ok(new HikeLogResponse(r.Id, request.StageId, request.DateHiked, request.DurationMinutes,
                request.Weather, request.Notes, request.Rating)),
            v => ValidationProblem(v.ToModelStateDictionary()),
            _ => NotFound());
    }

    /// <summary>Deletes a hike log entry.</summary>
    /// <param name="id">The primary key of the hike log to delete.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>204 No Content on success, or 404 if not found.</returns>
    [HttpDelete("hikelogs/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await deleteHandler.Handle(new DeleteHikeLog(id), ct);
        return result.Match<IActionResult>(_ => NoContent(), _ => NotFound());
    }
}
