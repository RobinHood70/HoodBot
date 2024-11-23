namespace RobinHood70.WikiCommon.Parser;

using System;
using System.Collections.Generic;

/// <summary>A wrapper for a list of <see cref="Section"/>s that includes various management functions.</summary>
public class SectionCollection : List<Section>
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="SectionCollection"/> class.</summary>
	/// <param name="factory">The factory to use for text manipulation. This ensures a factory is available even if there are no sections.</param>
	/// <param name="level">The <b>deepest</b> section level in the collection.</param>
	public SectionCollection(IWikiNodeFactory factory, int level)
	{
		ArgumentNullException.ThrowIfNull(factory);
		this.Factory = factory;
		this.Level = level is >= 1 and <= 6
			? level
			: throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 6.");
	}
	#endregion

	#region Public Properties

	/// <summary>Gets or sets the string comparer for non-collection-based searches when none is specified. Colleection-based searches use the default search from the collection's Contains method.</summary>
	public StringComparer Comparer { get; set; } = StringComparer.Ordinal;

	/// <summary>Gets the factory to use for text manipulation.</summary>
	public IWikiNodeFactory Factory { get; }

	/// <summary>Gets the deepest section level in the collection at creation time. This indicates only the intended section level of the collection; nothing prevents deeper sections from being added.</summary>
	public int Level { get; }
	#endregion

	#region Public Methods

	/// <summary>Finds all sections with the given header name.</summary>
	/// <param name="name">The name to search for.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	public IEnumerable<Section> FindAll(string name) => this.FindAll(name, this.Comparer, 0);

	/// <summary>Finds all sections with the given header name.</summary>
	/// <param name="name">The name to search for.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public IEnumerable<Section> FindAll(string name, int startIndex) => this.FindAll(name, this.Comparer, startIndex);

	/// <summary>Finds all sections with the given header name.</summary>
	/// <param name="name">The name to search for.</param>
	/// <param name="comparer">The string comparer to use.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public IEnumerable<Section> FindAll(string name, StringComparer comparer, int startIndex)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(comparer);
		return startIndex >= 0 && startIndex < this.Count
			? FindAllLocal(name, comparer, startIndex)
			: throw new ArgumentOutOfRangeException(nameof(startIndex));

		IEnumerable<Section> FindAllLocal(string name, StringComparer comparer, int startIndex)
		{
			for (var i = startIndex; i < this.Count; i++)
			{
				if (comparer.Equals(this[i].GetTitle(), name))
				{
					yield return this[i];
				}
			}
		}
	}

	/// <summary>Finds all sections with any of the given header names.</summary>
	/// <param name="names">The names to search for. Case sensitivity comes from the collection's Contains method.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	public IEnumerable<Section> FindAll(ICollection<string> names) => this.FindAll(names, 0);

	/// <summary>Finds all sections with any of the given header names.</summary>
	/// <param name="names">The names to search for. Case sensitivity comes from the collection's Contains method.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public IEnumerable<Section> FindAll(ICollection<string> names, int startIndex)
	{
		ArgumentNullException.ThrowIfNull(names);
		return startIndex >= 0 && startIndex < this.Count
			? FindAnyLocal(names, startIndex)
			: throw new ArgumentOutOfRangeException(nameof(startIndex));
		IEnumerable<Section> FindAnyLocal(ICollection<string> names, int startIndex)
		{
			for (var i = startIndex; i < this.Count; i++)
			{
				if (this[i].GetTitle() is string title && names.Contains(title))
				{
					yield return this[i];
				}
			}
		}
	}

	/// <summary>Finds the first section with the given name.</summary>
	/// <param name="name">The name to search for.</param>
	/// <returns>The first section that satisfies the criteria.</returns>
	public Section? FindFirst(string name) => this.FindFirst(name, this.Comparer, 0);

	/// <summary>Finds the first section with the given name.</summary>
	/// <param name="name">The name to search for.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>The first section that satisfies the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public Section? FindFirst(string name, int startIndex) => this.FindFirst(name, this.Comparer, startIndex);

	/// <summary>Finds the first section with the given name.</summary>
	/// <param name="name">The name to search for.</param>
	/// <param name="comparer">The string comparer to use.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>The first section that satisfies the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public Section? FindFirst(string name, StringComparer comparer, int startIndex)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(comparer);
		using var enumerator = this.FindAll(name, comparer, startIndex).GetEnumerator();
		return enumerator.MoveNext() ? enumerator.Current : null;
	}

	/// <summary>Finds the first section with the given header name.</summary>
	/// <param name="names">The names to search for.</param>
	/// <returns>The first section that satisfies the criteria.</returns>
	public Section? FindFirst(ICollection<string> names) => this.FindFirst(names, 0);

	/// <summary>Finds the first section with the given header name.</summary>
	/// <param name="names">The names to search for.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>The first section that satisfies the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public Section? FindFirst(ICollection<string> names, int startIndex)
	{
		ArgumentNullException.ThrowIfNull(names);
		using var enumerator = this.FindAll(names, startIndex).GetEnumerator();
		return enumerator.MoveNext() ? enumerator.Current : null;
	}

	/// <summary>Finds the first section with the given name and returns its index in the collection.</summary>
	/// <param name="name">The name to search for.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	public int IndexOf(string name) => this.IndexOf(name, this.Comparer, 0);

	/// <summary>Finds the first section with the given name and returns its index in the collection.</summary>
	/// <param name="name">The name to search for.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public int IndexOf(string name, int startIndex) => this.IndexOf(name, this.Comparer, startIndex);

	/// <summary>Finds the first section with the given name and returns its index in the collection.</summary>
	/// <param name="name">The name to search for.</param>
	/// <param name="comparer">The string comparer to use.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public int IndexOf(string name, StringComparer comparer, int startIndex)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(comparer);
		if (startIndex < 0 || startIndex >= this.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(startIndex));
		}

		for (var i = startIndex; i < this.Count; i++)
		{
			if (comparer.Equals(this[i], name))
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>Finds the first section with any of the given names and returns its index in the collection.</summary>
	/// <param name="names">The name to search for.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	public int IndexOf(ICollection<string> names) => this.IndexOf(names, 0);

	/// <summary>Finds the first section with any of the given names and returns its index in the collection.</summary>
	/// <param name="names">The name to search for.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>An enumeration of all the sections that satisfy the criteria.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is out of range.</exception>
	public int IndexOf(ICollection<string> names, int startIndex)
	{
		ArgumentNullException.ThrowIfNull(names);
		return this.FindIndex(startIndex, s =>
			s.GetTitle() is string title &&
			names.Contains(title));
	}

	/// <summary>Inserts a section into the collection and adjusts the space before it to be the specified value.</summary>
	/// <param name="index">The index to insert the section at.</param>
	/// <param name="section">The section to insert.</param>
	/// <param name="textBefore">The text to insert into the previous section. (Default: two linefeeds.) If no previous section exists, the text will not be inserted.</param>
	public void InsertWithSpaceBefore(int index, Section section, string textBefore = "\n\n")
	{
		ArgumentNullException.ThrowIfNull(section);
		ArgumentNullException.ThrowIfNull(textBefore);
		if (index > 0 && index <= this.Count)
		{
			var previous = this[index - 1].Content;
			previous.TrimEnd();
			previous.AddText(textBefore);
		}

		this.Insert(index, section);
	}
	#endregion
}