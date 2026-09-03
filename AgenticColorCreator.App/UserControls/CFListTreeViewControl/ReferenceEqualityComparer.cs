using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl;

/// <summary>
/// Compares source objects by identity so equal-valued resources remain distinct tree leaves.
/// </summary>
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
	public static ReferenceEqualityComparer Instance { get; } = new();

	public new bool Equals(object? x, object? y)
	{
		return ReferenceEquals(x, y);
	}

	public int GetHashCode(object obj)
	{
		return RuntimeHelpers.GetHashCode(obj);
	}
}
