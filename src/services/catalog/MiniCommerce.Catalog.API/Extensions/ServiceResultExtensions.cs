using MiniCommerce.Shared.Common;

namespace MiniCommerce.Catalog.API.Extensions;

public static class ServiceResultExtensions
{
    public static IResult ToApiResult(this ServiceResult result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return MapFailure(result);
    }

    public static IResult ToApiResult<T>(
        this ServiceResult<T> result,
        Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is null
                ? Results.Ok(result.Data)
                : onSuccess(result.Data!);
        }

        return MapFailure(result);
    }

    private static IResult MapFailure(ServiceResult result)
    {
        if (result.ValidationErrors is not null)
        {
            return Results.ValidationProblem(result.ValidationErrors);
        }

        if (result.Error.Code.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound(new
            {
                error = result.Error
            });
        }

        return Results.BadRequest(new
        {
            error = result.Error
        });
    }
}
