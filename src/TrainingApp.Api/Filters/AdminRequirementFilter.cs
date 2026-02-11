using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Filters;

public class AdminRequirementFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var currentUser = ctx.HttpContext.RequestServices
            .GetRequiredService<ICurrentUserService>();

        if (!currentUser.IsAdmin)
            throw new ForbiddenException("Admin access required");

        return await next(ctx);
    }
}

public static class AdminFilterExtensions
{
    public static RouteGroupBuilder RequireAdmin(this RouteGroupBuilder group)
    {
        group.AddEndpointFilterFactory((ctx, next) =>
        {
            var filter = new AdminRequirementFilter();
            return invCtx => filter.InvokeAsync(invCtx, next);
        });
        return group;
    }
}
