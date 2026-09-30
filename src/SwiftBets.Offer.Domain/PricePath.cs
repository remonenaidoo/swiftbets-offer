namespace SwiftBets.Offer.Domain;

/// <summary>Moves a price linearly from its historical opening to its closing value as the market approaches kickoff.</summary>
public static class PricePath
{
    public static Odds At(decimal opening, decimal closing, double progress)
    {
        var clamped = (decimal)Math.Clamp(progress, 0d, 1d);
        return Odds.From(opening + ((closing - opening) * clamped));
    }
}
