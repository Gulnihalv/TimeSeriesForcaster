using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Common;

namespace TimeSeriesForecaster.WebAPI.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult(Result result, Func<IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess?.Invoke() ?? NoContent();
        }

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(result.Error),
            ResultErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result.Error),
            ResultErrorType.ValidationError => BadRequest(result.Error),
            ResultErrorType.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
        };
    }

    protected IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess?.Invoke(result.Value!) ?? Ok(result.Value);
        }

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(result.Error),
            ResultErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result.Error),
            ResultErrorType.ValidationError => BadRequest(result.Error),
            ResultErrorType.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
        };
    }
}