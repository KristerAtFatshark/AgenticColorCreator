using System.Collections.Generic;

namespace AgenticColorCreator.Core.Models;

public sealed record AgenticColorsDocument(string Title, string FormatVersion, IReadOnlyList<ColorCategory> Categories);
