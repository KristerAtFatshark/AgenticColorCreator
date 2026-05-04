using System.Collections.Generic;

namespace AgenticColorCreator.Core.Models;

public sealed record ColorCategory(string Name, IReadOnlyList<AgenticColorItem> Colors, bool IsExpanded = true);
