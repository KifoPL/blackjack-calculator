using System.Data;
using System.Globalization;
using System.Text.Json;
using Blackjack;
using Blackjack.Cards;
using Blackjack.GameLogic;
using Spectre.Console;
using Spectre.Console.Json;
using Rule = Spectre.Console.Rule;
using Table = Blackjack.GameLogic.Table;

const int startingBalance = 1000;
const int minimumBet = 25;
const int maximumBet = 500;

const int simGames = 100;
const int rounds = 25;

decimal totalBalance = startingBalance;

AnsiConsole.Write(new Rule("[yellow]Deck of Cards:[/]"));

foreach (var cardSuit in Deck.Cards.GroupBy(c => c.Suit))
{
    AnsiConsole.Write(new Columns(cardSuit.Select(c => (Text)c)));
}

var table = new Table(minimumBet, maximumBet);

bool anotherGame;

var player = new Player(startingBalance);
totalBalance -= player.Balance;

var autoStrategy = new AutoStrategy(table, player);

var chart = autoStrategy.Game.RenderGameStatus();
List<Game> games = [];

var rows = new Rows(chart, new Panel(new JsonText(JsonSerializer.Serialize(GameSummary(),
    GameSummarySerializerContext.Default.GamesSummary))));

GamesSummary GameSummary()
{
    int totalWon = 0;
    int totalLost = 0;
    int totalPlayed = 0;
    int totalDraws = 0;

    games.ForEach(g =>
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
    
    var status = player.Balance switch
    {
        <= 0 => GameSummaryStatus.Loss,
        > startingBalance => GameSummaryStatus.Win,
        _ => GameSummaryStatus.Inconclusive
    };

    var gamesSummary = new GamesSummary(status, totalPlayed, totalWon, totalLost, totalDraws, player.Balance);

    return gamesSummary;
}

var tableOfGames = new Spectre.Console.Table().Title("Games Summary").Border(TableBorder.Rounded)
    .AddColumns("Status", "Rounds Played", "Won", "Lost", "Draws", "Balance");

List<GamesSummary> summaries = [];

await SimulateGames(simGames, rounds);

async Task SimulateGames(int simGames, int rounds)
{
    for (int i = 0; i < simGames; i++)
    {
        player = new Player(startingBalance);
        totalBalance -= player.Balance;
        games = [];
        autoStrategy = new AutoStrategy(table, player);
        await SimulateGame(rounds);
    }
    
    int totalWins = summaries.Count(s => s.Status is GameSummaryStatus.Win);
    int totalLosses = summaries.Count(s => s.Status is GameSummaryStatus.Loss);
    int totalInconclusive = summaries.Count(s => s.Status is GameSummaryStatus.Inconclusive);

    var wlRatio = ((double)totalWins / totalLosses);
    tableOfGames.AddRow(new Text($"W {totalWins}, L {totalLosses}, I {totalInconclusive}. W/L % {wlRatio:##.00}"),
        new Text($"{summaries.Sum(s => s.TotalRounds)}, avg. {summaries.Average(s => s.TotalRounds)}"),
        new Text($"{summaries.Sum(s => s.PlayerWins)}, avg. {summaries.Average(s => s.PlayerWins)}"),
        new Text($"{summaries.Sum(s => s.DealerWins)}, avg. {summaries.Average(s => s.DealerWins)}"),
        new Text($"{summaries.Sum(s => s.Draws)}, avg. {summaries.Average(s => s.Draws)}"),
        new Text($"{totalBalance}"));
    
    AnsiConsole.Write(tableOfGames);

    var grossIncome = summaries.Sum(s => s.CurrentBalance);
    var netIncome = grossIncome - startingBalance * simGames;
    
    AnsiConsole.Write(new Rule($"Gross income: {grossIncome:##,###.00}, net income: {netIncome:##,###.00}"));
    
    if (netIncome > 0)
    {
        AnsiConsole.Write(new FigletText($"You won!"){Color = Color.Green});
    }
    else if (netIncome < 0)
    {
        AnsiConsole.Write(new FigletText($"You lost!"){Color = Color.Red});
    }
    else
    {
        AnsiConsole.Write(new FigletText($"It's a draw!"){Color = Color.Yellow});
    }
}

async Task SimulateGame(int rounds)
{
    try
    {
        for (int i = 0; i < rounds; i++)
        {
            games.Add(await autoStrategy.PlayGame());
            AnsiConsole.Write(new Rows(new Panel(new JsonText(JsonSerializer.Serialize(GameSummary(),
                GameSummarySerializerContext.Default.GamesSummary)))));
        }

        while (games.Last().Status == GameStatus.DealerWins)
        {
            games.Add(await autoStrategy.PlayGame());
            AnsiConsole.Write(new Rows(new Panel(new JsonText(JsonSerializer.Serialize(GameSummary(),
                GameSummarySerializerContext.Default.GamesSummary)))));
        }
    }
    catch (Exception e)
    {
        AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything);
    }

    var summary = GameSummary();
    summaries.Add(summary);
    
    var statusColor = summary.Status switch
    {
        GameSummaryStatus.Loss => Color.Red,
        GameSummaryStatus.Win => Color.Green,
        GameSummaryStatus.Inconclusive => Color.Yellow,
        _ => Color.Default
    };

    var status = new Markup($"{summary.Status.ToString()[0]}", statusColor);

    string[] cells =
    [
        summary.TotalRounds.ToString(), summary.PlayerWins.ToString(), summary.DealerWins.ToString(),
        summary.Draws.ToString(), summary.CurrentBalance.ToString(CultureInfo.InvariantCulture)
    ];

    tableOfGames.AddRow([status, ..cells.Select(s => new Text(s))]);

    AnsiConsole.Write(tableOfGames);
    totalBalance += player.Balance;
}

// for (int i = 0; i < 100; i++)
// {
//     games.Add(await autoStrategy.PlayGame());
//     AnsiConsole.Write(new Rows(new Panel(new JsonText(JsonSerializer.Serialize(GameSummary(),
//         GameSummarySerializerContext.Default.GamesSummary)))));
// }
//
// while (games.Last().Status == GameStatus.DealerWins)
// {
//     games.Add(await autoStrategy.PlayGame());
//     AnsiConsole.Write(new Rows(new Panel(new JsonText(JsonSerializer.Serialize(GameSummary(),
//         GameSummarySerializerContext.Default.GamesSummary)))));
// }

// await AnsiConsole.Live(rows)
//     .StartAsync(async ctx =>
//     {
//         Table.Context = ctx;
//
//         Table.Context = null;
//     });

// do
// {
//     var game = new Game
//     {
//         Delay = TimeSpan.FromMilliseconds(200),
//         Player = player
//     };
//
//     decimal bet = 0;
//     do
//     {
//         bet = AnsiConsole.Prompt(new TextPrompt<decimal>(
//             $"Your balance is {player.Balance} {Player.Currency}. Enter your bet (min bet - {table.MinimumBet}, max bet - {table.MaximumBet}):"));
//
//         if (!player.CanBet(bet))
//         {
//             AnsiConsole.MarkupLine("[red]You don't have enough money to bet that amount.[/]");
//         }
//     } while (!player.CanBet(bet));
//
//     await game.StartGame(bet);
//
//     while (game.CanMakeMove())
//     {
//         AnsiConsole.Write(new Rule("[yellow]Current turn:[/]"));
//         game.DealerHand.Render();
//         game.PlayerHand.Render();
//
//         var selectionPrompt = new SelectionPrompt<string>()
//             .Title("Do you want to hit or stand?")
//             .PageSize(3)
//             .AddChoices("Hit", "Stand");
//
//         if (game.PlayerHand.Cards.Count == 2)
//         {
//             selectionPrompt.AddChoices("Double Down");
//         }
//
//         var choice = AnsiConsole.Prompt(selectionPrompt);
//
//         switch (choice)
//         {
//             case "Hit":
//                 await game.Hit();
//                 break;
//             case "Double Down":
//                 await game.DoubleDown();
//                 break;
//             default:
//                 await game.Stand();
//                 break;
//         }
//     }
//
//     if (player.Balance < table.MinimumBet)
//     {
//         AnsiConsole.MarkupLine("[red]You don't have enough money to play another game.[/]");
//         anotherGame = false;
//     }
//     else
//     {
//         anotherGame = AnsiConsole.Prompt(new SelectionPrompt<bool>()
//             .Title($"Do you want to play another game? Your balance is {player.Balance} {Player.Currency}.")
//             .AddChoices(true, false));
//     }
// } while (anotherGame);