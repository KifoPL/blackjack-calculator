using Spectre.Console;
using Spectre.Console.Rendering;

namespace Blackjack.GameLogic;

public class Round
{
    public Hand DealerHand { get; } = new("Dealer");
    public Hand PlayerHand { get; } = new("Player");
    public required Player Player { get; init; }

    public GameStatus Status { get; private set; } = GameStatus.New;
    public TimeSpan Delay { get; init; } = TimeSpan.Zero;
    public string StatusMessage { get; private set; } = "";

    private readonly Dealer _dealer = new();

    public async Task StartRound(decimal bet)
    {
        if (Consts.LogVerbosity <= LogVerbosity.PerRound)
        {
            AnsiConsole.Write(new Rule("New Round"));    
        }
        
        Player.Bet(bet);
        _dealer.NewDeck();
        DealerHand.DealCard(_dealer.NextCard());
        PlayerHand.DealCard(_dealer.NextCard());
        PlayerHand.DealCard(_dealer.NextCard());

        if (Consts.LogVerbosity <= LogVerbosity.EveryMove)
        {
            DealerHand.Render();
            PlayerHand.Render();   
        }

        Status = GameStatus.InProgress;

        if (PlayerHand.Score == 21)
        {
            Status = GameStatus.PlayerWins;
            StatusMessage = "Blackjack!";
            var bj = Player.Blackjack();
            
            if (Consts.LogVerbosity <= LogVerbosity.PerRound)
            {
                AnsiConsole.Write(new FigletText(StatusMessage) { Color = Color.Gold1 });
                AnsiConsole.Write(new Panel(new Markup($"You won [green]{bj} {Player.Currency}[/]"))
                    { Border = new DoubleBoxBorder() });
            }
        }
    }

    public bool CanMakeMove()
    {
        return Status is GameStatus.InProgress;
    }

    public async Task Hit()
    {
        PlayerHand.DealCard(_dealer.NextCard());
        
        if (Consts.LogVerbosity <= LogVerbosity.EveryMove)
        {
            AnsiConsole.Write(new Columns(new Text("Player hits"), (Text)PlayerHand.Cards.Last()) { Expand = false });
            BjTable.Context?.UpdateTarget(RenderGameStatus());
            BjTable.Context?.Refresh();
            PlayerHand.Render();
        }
        
        if (PlayerHand.Score == 21)
        {
            await Stand();
            return;
        }

        if (PlayerHand.Score <= 21) return;

        Status = GameStatus.DealerWins;
        StatusMessage = "Player busts!";

        await FinishGame();
    }

    private async Task FinishGame()
    {
        Color color = Status switch
        {
            GameStatus.PlayerWins => Color.Green,
            GameStatus.DealerWins => Color.Red,
            GameStatus.Draw => Color.Yellow,
            _ => Color.White
        };

        await Task.Delay(Delay);



        if (Consts.LogVerbosity <= LogVerbosity.PerRound)
        {
            AnsiConsole.Write(new FigletText(StatusMessage) { Color = color });
        }

        switch (Status)
        {
            case GameStatus.PlayerWins:
                var wonBet = PlayerHand is { Score: 21, Cards.Count: 2 } ? Player.Blackjack() : Player.Win();
                if (Consts.LogVerbosity <= LogVerbosity.PerRound)
                    AnsiConsole.Write(new Panel(new Markup($"You won [green]{wonBet} {Player.Currency}[/]")) { Border = new DoubleBoxBorder() });
                break;
            case GameStatus.DealerWins:
                Player.Lose();
                break;
            case GameStatus.Draw:
                Player.Draw();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        if (Consts.LogVerbosity <= LogVerbosity.PerRound)
        {
            BjTable.Context?.UpdateTarget(RenderGameStatus());
            BjTable.Context?.Refresh();
        }
    }

    public async Task Stand()
    {
        if (Consts.LogVerbosity <= LogVerbosity.EveryMove)
        {
            AnsiConsole.WriteLine("Player stands");
            BjTable.Context?.Refresh();
        }

        while (DealerHand.Score < 17)
        {
            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay);
            }
            DealerHand.DealCard(_dealer.NextCard());
            if (Consts.LogVerbosity <= LogVerbosity.EveryMove)
            {
                AnsiConsole.Write(new Columns(new Text("Dealer hits"), (Text)DealerHand.Cards.Last()) { Expand = false });
                BjTable.Context?.UpdateTarget(RenderGameStatus());
                BjTable.Context?.Refresh();
                DealerHand.Render();
            }
        }

        if (DealerHand.Score > 21)
        {
            Status = GameStatus.PlayerWins;
            StatusMessage = "Dealer busts!";
        }
        else if (PlayerHand.Score > 21)
        {
            Status = GameStatus.DealerWins;
            StatusMessage = "Player busts!";
        }
        else if (PlayerHand.Score > DealerHand.Score)
        {
            Status = GameStatus.PlayerWins;
            StatusMessage = "Player wins!";
        }
        else if (PlayerHand.Score == DealerHand.Score)
        {
            Status = GameStatus.Draw;
            StatusMessage = "It's a draw!";
        }
        else
        {
            Status = GameStatus.DealerWins;
            StatusMessage = "Dealer wins!";
        }

        await FinishGame();
    }

    public async Task DoubleDown()
    {
        if (!Player.CanBet(Player.CurrentBet))
        {
            AnsiConsole.WriteLine("You don't have enough money to double down");
            return;
        }

        if (PlayerHand.Cards.Count != 2)
        {
            AnsiConsole.WriteLine("You can only double down on your first turn");
            return;
        }

        if (Consts.LogVerbosity <= LogVerbosity.EveryMove)
        {
            AnsiConsole.WriteLine("Player doubles down");
            BjTable.Context?.UpdateTarget(RenderGameStatus());
            BjTable.Context?.Refresh();
        }
        Player.DoubleDown();
        await Hit();

        if (Status is GameStatus.InProgress)
        {
            await Stand();
        }
    }
    
    public static BarChart? GameStatusChart { get; private set; }

    public BarChart RenderGameStatus()
    {
        var dealerColor = DealerHand.Score switch
        {
            > 21 => Color.Red,
            21 => Color.Green,
            _ => Color.Default
        };
        
        var playerColor = PlayerHand.Score switch
        {
            > 21 => Color.Red,
            21 => Color.Green,
            _ => Color.Default
        };

        GameStatusChart ??= new BarChart()
            .CenterLabel()
            .AddItem("Dealer", DealerHand.Score, dealerColor)
            .AddItem("Player", PlayerHand.Score, playerColor);

        GameStatusChart.Data[0] = new BarChartItem("Dealer", DealerHand.Score, dealerColor);
        GameStatusChart.Data[1] = new BarChartItem("Player", PlayerHand.Score, playerColor);
        
        return GameStatusChart;
    }
}