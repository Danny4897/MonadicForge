using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonadicLeaf.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public sealed class BillingController : ControllerBase
{
    // Step 6 — MonadicLeaf.Modules.Billing will implement these.

    [HttpPost("checkout")]
    public IActionResult CreateCheckout() =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { step = 6, message = "Implement MonadicLeaf.Modules/Billing" });

    [HttpPost("portal")]
    public IActionResult CreatePortal() =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { step = 6, message = "Implement MonadicLeaf.Modules/Billing" });
}

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    // Stripe and GitHub webhooks — no JWT auth, signature verified in Step 6/9.

    [HttpPost("stripe")]
    public IActionResult StripeWebhook() =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { step = 6, message = "Implement Stripe webhook handler" });

    [HttpPost("github")]
    public IActionResult GitHubWebhook() =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { step = 9, message = "Implement GitHub webhook handler" });
}
