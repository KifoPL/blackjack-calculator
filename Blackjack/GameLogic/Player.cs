using Spectre.Console;

namespace Blackjack.GameLogic;

public class Player(decimal balance)
{
    public decimal Balance { get; private set; } = balance;
    public decimal CurrentBet { get; private set; }
    
    public decimal Blackjack()
    {
        try
        {
            Balance += CurrentBet * 2.5m;
            return CurrentBet * 1.5m;
        }
        finally
        {
            CurrentBet = 0;
        }
    }
    
    public decimal Win()
    {
        try
        {
            Balance += CurrentBet * 2;
            return CurrentBet;
        }
        finally
        {
            CurrentBet = 0;    
        }
    }
    
    public void Bet(decimal amount)
    {
        Balance -= amount;
        CurrentBet = amount;
        
        AnsiConsole.WriteLine($"Player bets {amount}.");
    }
    
    public void Lose()
    {
        CurrentBet = 0;
    }
    
    public void Draw()
    {
        Balance += CurrentBet;
        CurrentBet = 0;
    }
    
    public void Reset()
    {
        CurrentBet = 0;
    }
    
    public bool CanBet(decimal amount)
    {
        return Balance >= amount;
    }
    
    public bool CanPlay()
    {
        return Balance > 0;
    }
    
    public void DoubleDown()
    {
        Bet(CurrentBet);
        CurrentBet *= 2;
        
        AnsiConsole.WriteLine($"Player doubles down, total bet {CurrentBet}.");
    }

    public const string Currency = "PLN";
}