using System.Text.Json.Serialization;

namespace Blackjack.GameLogic;

[JsonSourceGenerationOptions(WriteIndented = true,
    Converters = [typeof(JsonStringEnumConverter)])]
[JsonSerializable(typeof(GameSummary))]
public partial class GameSummarySerializerContext : JsonSerializerContext;