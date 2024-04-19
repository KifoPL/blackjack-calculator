using Blackjack.GameLogic;
using Spectre.Console;
using Table = Blackjack.GameLogic.Table;

namespace Blackjack;

public class AutoStrategy(Table table, Player player)
{
    public Game Game => Table.Game ?? Table.NewGame(Player);
    public Table Table { get; init; } = table;
    public Player Player { get; init; } = player;

    private BettingStrategy BettingStrategy { get; } = new(table, player);

    public async Task<Game> PlayGame()
    {
        if (Table.Game is null or { Status: not GameStatus.InProgress })
        {
            Table.NewGame(Player);
        }

        if (Game.Status is GameStatus.New)
        {
            var bet = BettingStrategy.GetBet();
            
            await Table.StartGame(bet);
            
            Table.Context?.UpdateTarget(Game.RenderGameStatus());
            Table.Context?.Refresh();
        }

        while (Game.Status is GameStatus.InProgress)
        {
            await PlayMove(SelectMove());
        }

        BettingStrategy.Update(Game.Status);

        return Game;
    }

    public Move SelectMove()
    {
        if (Game.PlayerHand.IsSoft)
        {
            return Game.PlayerHand.Score switch
            {
                >= 20 => Move.Stand,
                19 => Game.DealerHand.Score switch
                {
                    6 => Move.Double,
                    _ => Move.Stand
                },
                18 => Game.DealerHand.Score switch
                {
                    >= 9 => Move.Hit,
                    >= 7 => Move.Stand,
                    _ => Move.Double
                },
                17 => Game.DealerHand.Score switch
                {
                    >= 7 => Move.Hit,
                    >= 3 => Move.Double,
                    _ => Move.Stand
                },
                15 or 16 => Game.DealerHand.Score switch
                {
                    >= 7 => Move.Hit,
                    >= 4 => Move.Double,
                    _ => Move.Stand
                },
                14 or 13 => Game.DealerHand.Score switch
                {
                    >= 7 => Move.Hit,
                    >= 5 => Move.Double,
                    _ => Move.Stand
                },
                _ => Move.Hit
            };
        }

        return Game.PlayerHand.Score switch
        {
            >= 17 => Move.Stand,
            >= 13 => Game.DealerHand.Score switch
            {
                >= 7 => Move.Hit,
                _ => Move.Stand
            },
            12 => Game.DealerHand.Score switch
            {
                < 4 or > 6 => Move.Hit,
                _ => Move.Stand
            },
            11 => Move.Double,
            10 => Game.DealerHand.Score switch
            {
                < 10 => Move.Double,
                _ => Move.Hit
            },
            9 => Game.DealerHand.Score switch
            {
                < 7 => Move.Double,
                _ => Move.Hit
            },
            _ => Move.Hit
        };
    }

    public async Task PlayMove(Move move)
    {
        switch (move)
        {
            case Move.Hit:
                await Game.Hit();
                break;
            case Move.Stand:
                await Game.Stand();
                break;
            case Move.Double:
                if (Game.PlayerHand.Cards.Count is 2 && Player.CanBet(Player.CurrentBet))
                {
                    BettingStrategy.PreviousBet *= 2;
                    await Game.DoubleDown();
                }
                else
                {
                    await Game.Hit();
                }

                break;
            case Move.Split:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(move), move, null);
        }
    }
}

public enum Move
{
    Hit,
    Stand,
    Double,
    Split
}

public class BettingStrategy(Table table, Player player)
{
    public GameStatus PreviousGameStatus { get; private set; } = GameStatus.New;
    public decimal PreviousBet { get; set; } = table.MinimumBet;

    public void Update(GameStatus gameStatus)
    {
        PreviousGameStatus = gameStatus;
    }

    public decimal GetBet()
    {
        var currentBet = PreviousGameStatus switch
        {
            GameStatus.DealerWins => PreviousBet * 2,
            GameStatus.PlayerWins => table.MinimumBet,
            GameStatus.Draw => PreviousBet,
            GameStatus.New => table.MinimumBet,
            _ => PreviousBet
        };

        //currentBet = table.MinimumBet;

        if (currentBet > player.Balance)
        {
            currentBet = player.Balance;
        }

        if (currentBet < table.MinimumBet)
        {
            currentBet = table.MinimumBet;
        }

        if (currentBet > table.MaximumBet)
        {
            currentBet = table.MaximumBet;
        }

        PreviousBet = currentBet;

        return currentBet;
    }
}