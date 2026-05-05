namespace AgenticColorCreator.Core.Models;

public sealed record AgenticColorItem(string Name, string HexValue, string Description, InteractionState State = InteractionState.Default);
