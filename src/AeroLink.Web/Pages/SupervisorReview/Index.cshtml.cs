using AeroLink.Web.Models;
using AeroLink.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.SupervisorReview;

/// <summary>
/// Implements user story I5: a Baggage Supervisor reviews each open
/// exception and either resolves the problem (bag remains pending for
/// loading) or explicitly approves the bag not to load, recording a reason
/// either way.
/// </summary>
public class IndexModel : PageModel
{
    private readonly ISupervisorReviewService _reviewService;

    /// <summary>Creates the page model with an injected supervisor review service.</summary>
    public IndexModel(ISupervisorReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Open exceptions awaiting a decision.</summary>
    public List<BaggageException> Exceptions { get; set; } = new();

    /// <summary>Message from the most recent decision attempt, if any.</summary>
    public string? StatusMessage { get; set; }

    /// <summary>Whether the most recent decision attempt succeeded.</summary>
    public bool DecisionSucceeded { get; set; }

    /// <summary>Loads every open exception for review.</summary>
    public async Task OnGetAsync()
    {
        Exceptions = await _reviewService.GetOpenExceptionsAsync();
    }

    /// <summary>Records a Resolve or Approve-not-to-load decision against one exception.</summary>
    public async Task<IActionResult> OnPostDecideAsync(int exceptionId, string decision, string reason)
    {
        var decidedBy = User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name)
            ? User.Identity.Name
            : "Baggage Supervisor";

        var result = decision == "notload"
            ? await _reviewService.ApproveNotToLoadAsync(exceptionId, reason, decidedBy)
            : await _reviewService.ResolveAsync(exceptionId, reason, decidedBy);

        StatusMessage = result.Message;
        DecisionSucceeded = result.Success;
        Exceptions = await _reviewService.GetOpenExceptionsAsync();
        return Page();
    }
}
