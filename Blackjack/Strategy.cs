using Blackjack.GameLogic;
using Spectre.Console;

namespace Blackjack;

public class Strategy(BjTable bjTable, Player player)
{
    public Round Round => BjTable.Game ?? BjTable.NewGame(Player);
    public BjTable BjTable { get; init; } = bjTable;
    public Player Player { get; init; } = player;

    private BettingStrategy BettingStrategy { get; } = new(bjTable, player);

    public async Task<Round> PlayGame()
    {
        if (BjTable.Game is null or { Status: not GameStatus.InProgress })
        {
            BjTable.NewGame(Player);
        }

        if (Round.Status is GameStatus.New)
        {
            var bet = BettingStrategy.GetBet();
            
            await BjTable.StartGame(bet);
            
            BjTable.Context?.UpdateTarget(Round.RenderGameStatus());
            BjTable.Context?.Refresh();
        }

        while (Round.Status is GameStatus.InProgress)
        {
            await PlayMove(SelectMove());
        }

        BettingStrategy.Update(Round.Status);

        return Round;
    }

    public Move SelectMove()
    {
        if (Round.PlayerHand.IsSoft)
        {
            return Round.PlayerHand.Score switch
            {
                >= 20 => Move.Stand,
                19 => Round.DealerHand.Score switch
                {
                    6 => Move.Double,
                    _ => Move.Stand
                },
                18 => Round.DealerHand.Score switch
                {
                    >= 9 => Move.Hit,
                    >= 7 => Move.Stand,
                    _ => Move.Double
                },
                17 => Round.DealerHand.Score switch
                {
                    >= 7 => Move.Hit,
                    >= 3 => Move.Double,
                    _ => Move.Stand
                },
                15 or 16 => Round.DealerHand.Score switch
                {
                    >= 7 => Move.Hit,
                    >= 4 => Move.Double,
                    _ => Move.Stand
                },
                14 or 13 => Round.DealerHand.Score switch
                {
                    >= 7 => Move.Hit,
                    >= 5 => Move.Double,
                    _ => Move.Stand
                },
                _ => Move.Hit
            };
        }

        return Round.PlayerHand.Score switch
        {
            >= 17 => Move.Stand,
            >= 13 => Round.DealerHand.Score switch
            {
                >= 7 => Move.Hit,
                _ => Move.Stand
            },
            12 => Round.DealerHand.Score switch
            {
                < 4 or > 6 => Move.Hit,
                _ => Move.Stand
            },
            11 => Move.Double,
            10 => Round.DealerHand.Score switch
            {
                < 10 => Move.Double,
                _ => Move.Hit
            },
            9 => Round.DealerHand.Score switch
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
                await Round.Hit();
                break;
            case Move.Stand:
                await Round.Stand();
                break;
            case Move.Double:
                if (Round.PlayerHand.Cards.Count is 2 && Player.CanBet(Player.CurrentBet))
                {
                    BettingStrategy.PreviousBet *= 2;
                    await Round.DoubleDown();
                }
                else
                {
                    await Round.Hit();
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

public class BettingStrategy(BjTable bjTable, Player player)
{
    public GameStatus PreviousGameStatus { get; private set; } = GameStatus.New;
    public decimal PreviousBet { get; set; } = bjTable.MinimumBet;

    public void Update(GameStatus gameStatus)
    {
        PreviousGameStatus = gameStatus;
    }

    public decimal GetBet()
    {
        var currentBet = PreviousGameStatus switch
        {
            GameStatus.DealerWins => PreviousBet * 2,
            GameStatus.PlayerWins => bjTable.MinimumBet,
            GameStatus.Draw => PreviousBet,
            GameStatus.New => bjTable.MinimumBet,
            _ => PreviousBet
        };

        //currentBet = table.MinimumBet;

        if (currentBet > player.Balance)
        {
            currentBet = player.Balance;
        }

        if (currentBet < bjTable.MinimumBet)
        {
            currentBet = bjTable.MinimumBet;
        }

        if (currentBet > bjTable.MaximumBet)
        {
            currentBet = bjTable.MaximumBet;
        }

        PreviousBet = currentBet;

        return currentBet;
    }
}