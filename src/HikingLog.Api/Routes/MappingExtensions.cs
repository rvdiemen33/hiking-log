namespace HikingLog.Api.Routes;

using HikingLog.Application.Routes.Commands;
using HikingLog.Application.Routes.Queries;

/// <summary>Mapping extensions for converting between route API models and Application layer types.</summary>
internal static class RouteMappingExtensions
{
    /// <summary>Maps a <see cref="CreateRouteRequest"/> to an <see cref="AddRoute"/> command.</summary>
    /// <param name="request">The incoming create request.</param>
    /// <returns>The corresponding <see cref="AddRoute"/> command.</returns>
    internal static AddRoute ToCommand(this CreateRouteRequest request) =>
        new(request.Name, request.Code, request.Country, request.TotalDistanceKm, request.Description);

    /// <summary>Maps an <see cref="UpdateRouteRequest"/> to an <see cref="UpdateRoute"/> command, injecting the route id from the URL.</summary>
    /// <param name="request">The incoming update request.</param>
    /// <param name="id">The route id from the URL segment.</param>
    /// <returns>The corresponding <see cref="UpdateRoute"/> command.</returns>
    internal static UpdateRoute ToCommand(this UpdateRouteRequest request, int id) =>
        new(id, request.Name, request.Code, request.Country, request.TotalDistanceKm, request.Description);

    /// <summary>Maps a <see cref="RouteDto"/> to a <see cref="RouteResponse"/>.</summary>
    /// <param name="dto">The Application layer DTO.</param>
    /// <returns>The corresponding API response model.</returns>
    internal static RouteResponse ToResponse(this RouteDto dto) =>
        new(dto.Id, dto.Name, dto.Code, dto.Country, dto.TotalDistanceKm, dto.Description);
}
