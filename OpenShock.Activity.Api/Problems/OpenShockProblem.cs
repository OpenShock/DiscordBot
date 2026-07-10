using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace OpenShock.Activity.Api.Problems;

/// <summary>
/// RFC 7807 problem response, mirroring the main OpenShock API's <c>OpenShockProblem</c>: a machine-readable
/// <see cref="ProblemDetails.Type"/> code (e.g. <c>Account.NotLinked</c>), a human <see cref="ProblemDetails.Title"/>,
/// optional <see cref="ProblemDetails.Detail"/>, and the request id for correlation.
/// </summary>
public class OpenShockProblem : ProblemDetails
{
    public OpenShockProblem(string type, string title, HttpStatusCode status, string? detail = null)
    {
        Type = type;
        Title = title;
        Status = (int)status;
        Detail = detail;
    }

    public string? RequestId { get; private set; }

    /// <summary>Materializes the problem into an <see cref="ObjectResult"/>, stamping the current request id.</summary>
    public ObjectResult ToObjectResult(HttpContext context)
    {
        RequestId = context.TraceIdentifier;
        return new ObjectResult(this) { StatusCode = Status };
    }
}
