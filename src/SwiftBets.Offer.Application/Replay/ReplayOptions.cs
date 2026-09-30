using System.ComponentModel.DataAnnotations;

namespace SwiftBets.Offer.Application.Replay;

public sealed class ReplayOptions
{
    public const string SectionName = "Replay";

    /// <summary>Real time between consecutive kickoffs; a 380-match season with 10 s slots replays in about 63 minutes.</summary>
    [Range(1, 3600)]
    public int SlotSeconds { get; set; } = 10;

    /// <summary>How many slots before kickoff a fixture is listed and bettable.</summary>
    [Range(1, 380)]
    public int ListLeadSlots { get; set; } = 30;

    [Range(1, 60)]
    public int TickSeconds { get; set; } = 2;

    /// <summary>Every Nth match gets its official result corrected one slot later (0 disables), so resettlement runs live.</summary>
    [Range(0, 1000)]
    public int CorrectionEvery { get; set; } = 25;

    public DateTimeOffset Epoch { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
