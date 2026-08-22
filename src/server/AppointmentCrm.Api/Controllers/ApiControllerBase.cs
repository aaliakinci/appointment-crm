using AppointmentCrm.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AppointmentCrm.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected bool TryCreatePageRequest(
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortDirection,
        string defaultSort,
        IReadOnlySet<string> allowedSorts,
        out PageRequest request,
        out IActionResult? error)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors[nameof(page)] = ["Page must be greater than zero."];
        }

        if (pageSize is < 1 or > PageRequest.MaximumPageSize)
        {
            errors[nameof(pageSize)] =
            [
                $"PageSize must be between 1 and {PageRequest.MaximumPageSize}.",
            ];
        }

        if (search?.Length > 160)
        {
            errors[nameof(search)] = ["Search cannot exceed 160 characters."];
        }

        string selectedSort = string.IsNullOrWhiteSpace(sortBy) ? defaultSort : sortBy.Trim();
        if (!allowedSorts.Contains(selectedSort))
        {
            errors[nameof(sortBy)] = ["SortBy is not valid for this resource."];
        }

        string direction = string.IsNullOrWhiteSpace(sortDirection)
            ? "asc"
            : sortDirection.Trim().ToLowerInvariant();
        if (direction is not ("asc" or "desc"))
        {
            errors[nameof(sortDirection)] = ["SortDirection must be 'asc' or 'desc'."];
        }

        if (errors.Count > 0)
        {
            request = null!;
            error = ValidationProblem(new ValidationProblemDetails(errors));
            return false;
        }

        request = new PageRequest(page, pageSize, search?.Trim(), selectedSort, direction == "desc");
        error = null;
        return true;
    }

    protected IActionResult InvalidArgument(ArgumentException exception)
    {
        string field = string.IsNullOrWhiteSpace(exception.ParamName)
            ? "request"
            : exception.ParamName;
        return ValidationProblem(new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                [field] = [exception.Message],
            }));
    }

    protected ObjectResult ApiProblem(int status, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        var result = new ObjectResult(problem) { StatusCode = status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
