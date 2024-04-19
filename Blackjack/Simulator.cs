using System.Globalization;
using System.Text.Json;
using Blackjack.GameLogic;
using Spectre.Console;
using Spectre.Console.Json;

namespace Blackjack;

public class GameSimulator
{
    private readonly int _roundsTotal;
    private Player Player { get; }
    private List<Round> Rounds { get; }
    private Strategy Strategy { get; }
    private BjTable BjTable { get; }

    public GameSimulator(int roundsTotal)
    {
        _roundsTotal = roundsTotal;
        BjTable = new BjTable(Consts.MinimumBetConst, Consts.MaximumBetConst);

        Player = new Player(Consts.StartingBalanceConst);
        Rounds = [];
        Strategy = new Strategy(BjTable, Player);
    }
    
    public async Task<GameSummary> SimulateGame()
    {
        try
        {
            for (int i = 0; i < _roundsTotal; i++)
            {
                Rounds.Add(await Strategy.PlayGame());
                AnsiConsole.Write(new Rows(new Panel(new JsonText(JsonSerializer.Serialize(GameSummary(),
                    GameSummarySerializerContext.Default.GameSummary)))));
            }

            while (Rounds.Last().Status == GameStatus.DealerWins)
            {
                Rounds.Add(await Strategy.PlayGame());
                AnsiConsole.Write(new Rows(new Panel(new JsonText(JsonSerializer.Serialize(GameSummary(),
                    GameSummarySerializerContext.Default.GameSummary)))));
            }
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything);
        }

        var summary = GameSummary();
        
        return summary;
    }

    GameSummary GameSummary()
    {
        int totalWon = 0;
        int totalLost = 0;
        int totalPlayed = 0;
        int totalDraws = 0;

        Rounds.ForEach(g =>
        {
            switch (g.Status)
            {
                case GameStatus.PlayerWins:
                    totalWon++;
                    break;
                case GameStatus.DealerWins:
                    totalLost++;
                    break;
                case GameStatus.Draw:
                    totalDraws++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            totalPlayed++;
        });

        var status = Player.Balance switch
        {
            <= 0 => GameSummaryStatus.Loss,
            > Consts.StartingBalanceConst => GameSummaryStatus.Win,
            _ => GameSummaryStatus.Inconclusive
        };

        var gamesSummary = new GameSummary(status, totalPlayed, totalWon, totalLost, totalDraws, Player.Balance);

        return gamesSummary;
    }
}