using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonadicLeaf.Api.Controllers;

[ApiController]
[Route("api/analyze")]
[Authorize]
public sealed class AnalyzeController : ControllerBase
{
    // Step 4 — MonadicLeaf.Modules.Analyze will implement this.
    [HttpPost]
    public IActionResult Analyze([FromBody] AnalyzeRequest request) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { step = 4, message = "Implement MonadicLeaf.Modules/Analyze" });
}

[ApiController]
[Route("api/history")]
[Authorize]
public sealed class HistoryController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHistory([FromQuery] int page = 1, [FromQuery] int size = 20) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { step = 4, message = "Implement analysis history persistence" });
}

public sealed record AnalyzeRequest(string Code, string? FileName = null);
