namespace SwiftBets.Offer.Domain;

/// <summary>Decimal odds, at least 1.01, held to two places as bookmakers publish them.</summary>
public readonly record struct Odds
{
    public const decimal Minimum = 1.01m;

    private Odds(decimal value) => Value = value;

    public decimal Value { get; }

    public static Odds From(decimal value)
    {
        var rounded = decimal.Round(value, 2, MidpointRounding.ToZero);
        return rounded < Minimum
            ? throw new ArgumentOutOfRangeException(nameof(value), value, $"Odds must be at least {Minimum}.")
            : new Odds(rounded);
    }

    public override string ToString() => Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
