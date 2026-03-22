using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonadicLeaf.Api.Extensions;
using MonadicLeaf.Modules.Analyze.Contracts;
using MonadicLeaf.SharedKernel.Context;

namespace MonadicLeaf.Api.Controllers;

[ApiController]
[Route("api/analyze")]
[Authorize]
public sealed class AnalyzeController : ControllerBase
{
    private readonly IAnalyzeService _analyzeService;

    public AnalyzeController(IAnalyzeService analyzeService) =>
        _analyzeService = analyzeService;

    /// <summary>Analyzes C# code for green-code violations and returns a Green Score.</summary>
    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
    {
        var context = HttpContext.Items["LeafContext"] as LeafContext;
        if (context is null) return Unauthorized();

        var result = await _analyzeService.AnalyzeAsync(request, context);
        return result.ToActionResult();
    }
}

[ApiController]
[Route("api/history")]
[Authorize]
public sealed class HistoryController : ControllerBase
{
    private readonly IAnalyzeService _analyzeService;

    public HistoryController(IAnalyzeService analyzeService) =>
        _analyzeService = analyzeService;

    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var context = HttpContext.Items["LeafContext"] as LeafContext;
        if (context is null) return Unauthorized();

        var result = await _analyzeService.GetHistoryAsync(page, size, context);
        return result.ToActionResult();
    }
}
