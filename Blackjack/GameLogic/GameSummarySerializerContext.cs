using System.Text.Json.Serialization;

namespace Blackjack.GameLogic;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GamesSummary))]
public partial class GameSummarySerializerContext : JsonSerializerContext
{
    
}