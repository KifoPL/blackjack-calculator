using System.Collections.Immutable;
using Blackjack.Cards;
using Spectre.Console;

namespace Blackjack.GameLogic;

public class Hand(string player)
{
    public string Player { get; } = player;

    public IReadOnlyCollection<Card> Cards => _cards.AsReadOnly();

    private readonly List<Card> _cards = [];

    public int Score
    {
        get
        {
            int score = 0;
            int aceCount = 0;

            foreach (var card in _cards)
            {
                if (card.Value is CardValue.Ace)
                {
                    aceCount++;
                }

                score += card.Value switch
                {
                    CardValue.Ace => 11,
                    CardValue.Jack or CardValue.Queen or CardValue.King => 10,
                    _ => (int)card.Value
                };
            }

            while (score > 21 && aceCount > 0)
            {
                score -= 10;
                aceCount--;
            }

            return score;
        }
    }

    public void DealCard(Card card)
    {
        _cards.Add(card);
    }

    public bool IsSoft
    {
        get
        {
            int score = 0;
            int aceCount = 0;

            foreach (var card in _cards)
            {
                if (card.Value is CardValue.Ace)
                {
                    aceCount++;
                }

                score += card.Value switch
                {
                    CardValue.Ace => 11,
                    CardValue.Jack or CardValue.Queen or CardValue.King => 10,
                    _ => (int)card.Value
                };
            }

            while (score > 21 && aceCount > 0)
            {
                score -= 10;
                aceCount--;
            }

            return aceCount > 0 && score != 21;
        }
    }

    public void Render()
    {
        var columns = new Columns([
            new Text($"{(IsSoft ? "soft " : "")}{Score}"), new Text("-"), .._cards.Select(c => (Text)c)
        ])
        {
            Expand = false
        };

        var borderColor = Score switch
        {
            > 21 => Color.Red,
            21 => Color.Green,
            _ => Color.Default
        };

        var panel = new Panel(columns)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(borderColor),
            Header = new PanelHeader(Player)
        };

        AnsiConsole.Write(panel);
    }
}