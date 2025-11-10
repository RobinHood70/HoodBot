namespace RobinHood70.Robby;

using System;
using System.Collections.Generic;
using System.Linq;
using RobinHood70.Robby.Design;

/// <summary>This is a wrapper around a Dictionary with the comparer always set to <see cref="TitleComparer"/>.</summary>
/// <typeparam name="T">The value associated with the <see cref="Title"/>.</typeparam>
/// <remarks>For simplicity, this only supports real <see cref="Title"/>s, not <see cref="ITitle"/>s.</remarks>
public class TitleDictionary<T> : Dictionary<Title, T>
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="TitleDictionary{T}"/> class.</summary>
	public TitleDictionary()
		: base(TitleComparer.Instance)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="TitleDictionary{T}"/> class.</summary>
	/// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
	public TitleDictionary(int capacity)
		: base(capacity, TitleComparer.Instance)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="TitleDictionary{T}"/> class.</summary>
	/// <param name="dictionary">The <see cref="IDictionary{Title, TValue}"/> whose elements are copied to the new dictionary.</param>
	public TitleDictionary(IDictionary<Title, T> dictionary)
		: base(dictionary, TitleComparer.Instance)
	{
	}
	#endregion

	#region Public Methods

	/// <summary>Creates a <see cref="TitleCollection"/> from the keys of this dictionary.</summary>
	/// <returns>A new <see cref="TitleCollection"/> comprised of the keys of this dictionary.</returns>
	/// <exception cref="InvalidOperationException">Thrown if there are no elements in this dictionary.</exception>
	/// <remarks>There must be at least one element in the dictionary for this to succeed.</remarks>
	public TitleCollection ToTitleCollection() => this.First() is KeyValuePair<Title, T> item
		? this.ToTitleCollection(item.Key.Site)
		: throw new InvalidOperationException("Dictionary must have values to create a TitleCollection with no site.");

	/// <summary>Creates a <see cref="TitleCollection"/> from the keys of this dictionary.</summary>
	/// <param name="site">The <see cref="Site"/> the collection belongs to.</param>
	/// <returns>A new <see cref="TitleCollection"/> comprised of the keys of this dictionary.</returns>
	public TitleCollection ToTitleCollection(Site site)
	{
		ArgumentNullException.ThrowIfNull(site);
		return new(site, this.Keys);
	}
	#endregion
}