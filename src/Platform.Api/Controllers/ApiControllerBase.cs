using Microsoft.AspNetCore.Mvc;

namespace Platform.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult NotFoundProblem(string resourceName, object resourceId)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource not found.",
            type: "https://httpstatuses.com/404",
            detail: $"{resourceName} '{resourceId}' was not found.");
    }
}
