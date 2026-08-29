namespace DominoWebAPI.Extensions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DominoWebAPI.Common;

public static class ActionResultExtensions
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result, Func<T, object>? mapper = null)
    {
        if (result.IsSuccess)
        {
            var data = mapper != null && result.Data != null ? mapper(result.Data) : (object?)result.Data;
            return new OkObjectResult(data);
        }

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => new NotFoundObjectResult(result.ErrorMessage),
            ServiceErrorType.Unauthorized => new UnauthorizedObjectResult(result.ErrorMessage),
            ServiceErrorType.Forbidden => new ObjectResult(result.ErrorMessage) { StatusCode = StatusCodes.Status403Forbidden },
            ServiceErrorType.Conflict => new ConflictObjectResult(result.ErrorMessage),
            _ => new BadRequestObjectResult(result.ErrorMessage)
        };
    }

    public static IActionResult ToActionResult(this ServiceResult result, object? successValue = null)
    {
        if (result.IsSuccess)
        {
            return successValue != null ? new OkObjectResult(successValue) : new OkResult();
        }

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => new NotFoundObjectResult(result.ErrorMessage),
            ServiceErrorType.Unauthorized => new UnauthorizedObjectResult(result.ErrorMessage),
            ServiceErrorType.Forbidden => new ObjectResult(result.ErrorMessage) { StatusCode = StatusCodes.Status403Forbidden },
            ServiceErrorType.Conflict => new ConflictObjectResult(result.ErrorMessage),
            _ => new BadRequestObjectResult(result.ErrorMessage)
        };
    }
}

