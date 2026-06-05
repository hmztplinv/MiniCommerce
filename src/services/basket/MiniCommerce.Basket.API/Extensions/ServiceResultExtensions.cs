using MiniCommerce.Shared.Common;

namespace MiniCommerce.Basket.API.Extensions;

public static class ServiceResultExtensions
{
    public static IResult ToHttpResult(this ServiceResult result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return MapFailure(result);
    }

    public static IResult ToHttpResult<T>(this ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Data);
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
