using AeroLink.Web.Models;

namespace AeroLink.Web.Services;

/// <summary>
/// Verifies a scanned or typed bag tag against a departure flight's manifest
/// and confirms valid bags as loaded. Supports user story I2.
/// </summary>
public interface IBagVerificationService
{
    /// <summary>
    /// Checks a bag tag against the selected flight's manifest without
    /// changing any data. Rejects tags that are invalid, belong to another
    /// flight, or have already been loaded.
    /// </summary>
    Task<BagVerificationResult> VerifyAsync(int flightId, string tag);

    /// <summary>
    /// Confirms a previously verified bag as loaded and increments the
    /// flight's loaded-bag count. Re-verifies the tag first so a bag cannot
    /// be confirmed twice or out of sequence.
    /// </summary>
    Task<BagVerificationResult> ConfirmLoadedAsync(int flightId, string tag);
}
