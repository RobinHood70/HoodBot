namespace RobinHood70.Robby.Design;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>An ISimpleTitle comparer which sorts by namespace and page name.</summary>
/// <seealso cref="Comparer{T}" />
public sealed class TitleComparer : IComparer<ITitle>, IComparer, IEqualityComparer<ITitle>
{
	#region Constructors
	private TitleComparer()
	{
	}
	#endregion

	#region Public Static Properties

	/// <summary>Gets the singleton instance.</summary>
	/// <value>The instance.</value>
	public static TitleComparer Instance { get; } = new TitleComparer();
	#endregion

	#region Public Static Methods

	/// <summary>Compares two <see cref="Title"/>s and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
	/// <param name="x">The first title to compare.</param>
	/// <param name="y">The second title to compare.</param>
	/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
	public static int DirectCompare(Title? x, Title? y)
	{
		if (x is null)
		{
			return y is null ? 0 : -1;
		}

		if (y is null)
		{
			return 1;
		}

		var nsCompare = Namespace.Compare(x.Namespace, y.Namespace);
		return nsCompare == 0
			? x.Namespace.ComparePageNames(x.PageName, y.PageName)
			: nsCompare;
	}
	#endregion

	#region Public Methods

	/// <summary>Compares two <see cref="ITitle"/>s and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
	/// <param name="x">The first title to compare.</param>
	/// <param name="y">The second title to compare.</param>
	/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />.</returns>
	public int Compare(ITitle? x, ITitle? y) => DirectCompare(x?.Title, y?.Title);

	/// <inheritdoc/>
	public bool Equals(ITitle? x, ITitle? y) => x is null
		? y is null
		: y is not null && x.Title == y.Title;

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] ITitle obj) => HashCode.Combine(obj?.Title);

	int IComparer.Compare(object? x, object? y) => this.Compare(x as ITitle, y as ITitle);
	#endregion
}