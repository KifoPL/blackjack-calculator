namespace Blackjack.GameLogic;

public record GameSummary(GameSummaryStatus Status, int TotalRounds, int PlayerWins, int DealerWins, int Draws, decimal CurrentBalance);

public enum GameSummaryStatus
{
    Win,
    Loss,
    Inconclusive
}