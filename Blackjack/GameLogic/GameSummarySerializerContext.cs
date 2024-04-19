using System.Text.Json.Serialization;

namespace Blackjack.GameLogic;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GameSummary))]
public partial class GameSummarySerializerContext : JsonSerializerContext
{
    
}