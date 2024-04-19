using Spectre.Console;
using Spectre.Console.Rendering;

namespace Blackjack.Cards;

public readonly struct Card
{
    public required CardSuit Suit { get; init; }
    public required CardValue Value { get; init; }

    public override string ToString()
    {
        return $"{Value.AsString()}{Suit.AsString()}";
    }

    public static explicit operator Card(string card)
    {
        if (card.Length > 3)
        {
            throw new ArgumentException("Card must not be longer than 3 characters");
        }

        return new Card
        {
            Value = card[..^1] switch
            {
                "A" => CardValue.Ace,
                "2" => CardValue.Two,
                "3" => CardValue.Three,
                "4" => CardValue.Four,
                "5" => CardValue.Five,
                "6" => CardValue.Six,
                "7" => CardValue.Seven,
                "8" => CardValue.Eight,
                "9" => CardValue.Nine,
                "10" => CardValue.Ten,
                "J" => CardValue.Jack,
                "Q" => CardValue.Queen,
                "K" => CardValue.King,
                _ => throw new ArgumentException($"Invalid card value {card[..^1]}")
            },
            Suit = card[^1] switch
            {
                '♣' => CardSuit.Clubs,
                '♦' => CardSuit.Diamonds,
                '♥' => CardSuit.Hearts,
                '♠' => CardSuit.Spades,
                _ => throw new ArgumentException($"Invalid card suit {card[^1]}")
            }
        };
    }
    
    public static implicit operator Text(Card card)
    {
        Color color = card.Suit is CardSuit.Diamonds or CardSuit.Hearts ? Color.Red : Color.Black;
        
        return new Text(card.ToString().EscapeMarkup(), new Style(color, Color.White));
    }
}