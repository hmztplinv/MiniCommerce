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

        if (result.ValidationErrors is not null)
        {
            return Results.ValidationProblem(result.ValidationErrors);
        }

        return Results.BadRequest(new
        {
            result.Error.Code,
            result.Error.Message
        });
    }

    public static IResult ToHttpResult<T>(this ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Data);
        }

        if (result.ValidationErrors is not null)
        {
            return Results.ValidationProblem(result.ValidationErrors);
        }

        return Results.BadRequest(new
        {
            result.Error.Code,
            result.Error.Message
        });
    }
}
