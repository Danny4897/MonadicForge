using Microsoft.AspNetCore.Mvc;
using MonadicSharp;

namespace MonadicLeaf.Api.Extensions;

public static class ResultExtensions
{
    /// <summary>Converts a Result&lt;T&gt; to an IActionResult using standard HTTP status mappings.</summary>
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : MapErrorToHttp(result.Error);

    private static IActionResult MapErrorToHttp(Error error) =>
        error.Code switch
        {
            "LEAF_TENANT_NOT_FOUND" or "NOT_FOUND" =>
                new NotFoundObjectResult(error),

            "LEAF_PLAN_LIMIT_EXCEEDED" or "LEAF_UNAUTHORIZED_FEATURE" or "LEAF_INVALID_STRIPE_SIGNATURE" =>
                new ObjectResult(error) { StatusCode = StatusCodes.Status403Forbidden },

            "LEAF_CODE_TOO_LARGE" or "VALIDATION_ERROR" =>
                new BadRequestObjectResult(error),

            _ => new ObjectResult(error) { StatusCode = StatusCodes.Status500InternalServerError }
        };
}
