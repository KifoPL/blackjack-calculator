using System.Collections.Immutable;

namespace Blackjack.Cards;

public static class Deck
{
    public static ImmutableArray<string> CardValues { get; } =
        [..Enum.GetValues<CardValue>().Select(c => c.AsString())];
    
    public static ImmutableArray<string> CardSuits { get; } =
        [..Enum.GetValues<CardSuit>().Select(c => c.AsString())];
    
    public static ImmutableArray<Card> Cards { get; } =
        [..CardSuits.SelectMany(s => CardValues.Select(v => (Card)$"{v}{s}"))];
}