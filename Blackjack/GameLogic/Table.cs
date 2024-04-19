using Spectre.Console;

namespace Blackjack.GameLogic;

public class Table
{
    public Table(decimal minimumBet)
    {
        MinimumBet = minimumBet;
        MaximumBet = minimumBet * 20;
    }
    
    public Table(decimal minimumBet, decimal maximumBet)
    {
        MinimumBet = minimumBet;
        MaximumBet = maximumBet;
    }

    public Game? Game { get; private set; }
    
    public Game NewGame(Player player)
    {
        Game = new()
        {
            Player = player,
            Delay = TimeSpan.Zero
        };
        
        return Game;
    }
    
    public async Task<Game> StartGame(decimal bet)
    {
        ArgumentNullException.ThrowIfNull(Game);
        
        if (Game.Status is GameStatus.InProgress)
        {
            throw new InvalidOperationException("Game is already in progress");
        }
        
        if (bet < MinimumBet || bet > MaximumBet)
        {
            throw new ArgumentOutOfRangeException("Bet is outside of allowed range");
        }
        
        if (Game.Player.Balance < bet)
        {
            throw new InvalidOperationException("Player does not have enough money to bet that amount");
        }
        
        await Game.StartGame(bet);
        
        Context?.UpdateTarget(Game.RenderGameStatus());
        Context?.Refresh();
        
        return Game;
    }
    
    public decimal MinimumBet { get; init; }

    public decimal MaximumBet
    {
        get => _maximumBet;
        init
        {
            if (value < MinimumBet)
            {
                throw new ArgumentException("Maximum bet cannot be lower than minimum bet");
            }

            _maximumBet = value;
        }
    }

    private readonly decimal _maximumBet;

    public static LiveDisplayContext? Context { get; set; }
}