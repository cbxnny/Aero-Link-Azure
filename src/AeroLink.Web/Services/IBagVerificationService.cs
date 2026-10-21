using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Verifies a scanned or typed bag tag against a departure flight's manifest
/// and confirms valid bags as loaded. Supports user stories I2 and I3.
/// </summary>
public interface IBagVerificationService
{
    /// <summary>
    /// Checks a bag tag against the selected flight's manifest without
    /// changing any data. Rejects tags that are invalid, belong to another
    /// flight, or have already been loaded. Also blocks bags approved not to
    /// load or with an open exception recorded by the I4/I5 workflow.
    /// </summary>
    Task<BagVerificationResult> VerifyAsync(int flightId, string tag);

    /// <summary>
    /// Confirms a previously verified bag as loaded and increments the
    /// flight's loaded-bag count. Re-verifies the tag first so a bag cannot
    /// be confirmed twice or loaded after an exception report or not-to-load decision.
    /// Special bags require acknowledgement with this request; acknowledgement
    /// never overrides an exception or a Supervisor's not-to-load decision.
    /// </summary>
    Task<BagVerificationResult> ConfirmLoadedAsync(int flightId, string tag);
}
