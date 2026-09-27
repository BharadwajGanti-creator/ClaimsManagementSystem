using Claims.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Claims.API.Common;

/// <summary>
/// Translates the application's <see cref="Result"/> type into ASP.NET Core
/// action results, mapping error categories onto HTTP status codes (RFC 7807).
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : controller.Problem(result.Error!);

    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : controller.Problem(result.Error!);

    public static IActionResult ToCreatedResult<T>(
        this Result<T> result, ControllerBase controller, string actionName, Func<T, object> routeValues) =>
        result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues(result.Value), result.Value)
            : controller.Problem(result.Error!);

    private static IActionResult Problem(this ControllerBase controller, Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return controller.Problem(
            detail: error.Message,
            statusCode: status,
            title: error.Code);
    }
}
