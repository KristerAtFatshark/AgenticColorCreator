using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl
{
	/// <summary>
	/// Compares class instances by identity so equal-valued objects remain distinct dictionary keys.
	/// </summary>
	public class ReferenceEqualityComparer<T> : IEqualityComparer<T>
		where T : class
	{
		public static readonly IEqualityComparer<T> Default = new ReferenceEqualityComparer<T>();

		private ReferenceEqualityComparer()
		{
		}

		public bool Equals(
			#if NETCORE
			T? x, T? y
			#else
			T x, T y
			#endif
			)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
