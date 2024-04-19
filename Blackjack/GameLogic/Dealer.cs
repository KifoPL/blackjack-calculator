using Blackjack.Cards;

namespace Blackjack.GameLogic;

public class Dealer
{
    private IReadOnlyCollection<Card> AvailableCards => _cards.AsReadOnly();

    private List<Card> _cards = Deck.Cards.ToList();

    public void NewDeck()
    {
        _cards = Deck.Cards.ToList();
    }

    public Card NextCard()
    {
        var card = Random.Shared.GetItems(_cards.ToArray(), 1)[0];
        _cards.Remove(card);
        return card;
    }
}