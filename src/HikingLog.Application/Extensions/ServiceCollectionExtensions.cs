namespace HikingLog.Application.Extensions;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.HikeLogs.Commands;
using HikingLog.Application.HikeLogs.Queries;
using HikingLog.Application.Routes.Commands;
using HikingLog.Application.Routes.Queries;
using HikingLog.Application.Stages.Commands;
using HikingLog.Application.Stages.Queries;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

/// <summary>Extension methods for registering Application layer services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds all Application handlers and validators to the DI container.</summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Routes — handlers
        services.AddScoped<ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>, AddRouteHandler>();
        services.AddScoped<ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>>, UpdateRouteHandler>();
        services.AddScoped<ICommandHandler<DeleteRoute, OneOf<Success, NotFound>>, DeleteRouteHandler>();
        services.AddScoped<IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>>, GetRoutesHandler>();
        services.AddScoped<IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>>, GetRouteHandler>();

        // Routes — validators
        services.AddScoped<IValidator<AddRoute>, AddRouteValidator>();
        services.AddScoped<IValidator<UpdateRoute>, UpdateRouteValidator>();

        // Stages — handlers (child of Route: AddStage carries NotFound)
        services.AddScoped<ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>>, AddStageHandler>();
        services.AddScoped<ICommandHandler<UpdateStage, OneOf<UpdateStageResult, ValidationFailed, NotFound>>, UpdateStageHandler>();
        services.AddScoped<ICommandHandler<DeleteStage, OneOf<Success, NotFound>>, DeleteStageHandler>();
        services.AddScoped<IQueryHandler<GetStagesByRoute, IReadOnlyList<StageDto>>, GetStagesByRouteHandler>();
        services.AddScoped<IQueryHandler<GetStage, OneOf<StageDto, NotFound>>, GetStageHandler>();

        // Stages — validators
        services.AddScoped<IValidator<AddStage>, AddStageValidator>();
        services.AddScoped<IValidator<UpdateStage>, UpdateStageValidator>();

        // HikeLogs — handlers (child of Stage: AddHikeLog and UpdateHikeLog carry NotFound)
        services.AddScoped<ICommandHandler<AddHikeLog, OneOf<AddHikeLogResult, ValidationFailed, NotFound>>, AddHikeLogHandler>();
        services.AddScoped<ICommandHandler<UpdateHikeLog, OneOf<UpdateHikeLogResult, ValidationFailed, NotFound>>, UpdateHikeLogHandler>();
        services.AddScoped<ICommandHandler<DeleteHikeLog, OneOf<Success, NotFound>>, DeleteHikeLogHandler>();
        services.AddScoped<IQueryHandler<GetHikeLogs, IReadOnlyList<HikeLogDto>>, GetHikeLogsHandler>();
        services.AddScoped<IQueryHandler<GetHikeLog, OneOf<HikeLogDto, NotFound>>, GetHikeLogHandler>();
        services.AddScoped<IQueryHandler<GetHikeLogsByStage, IReadOnlyList<HikeLogDto>>, GetHikeLogsByStageHandler>();

        // HikeLogs — validators
        services.AddScoped<IValidator<AddHikeLog>, AddHikeLogValidator>();
        services.AddScoped<IValidator<UpdateHikeLog>, UpdateHikeLogValidator>();

        return services;
    }
}
