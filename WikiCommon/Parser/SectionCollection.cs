namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;

	/// <summary>A wrapper for a list of <see cref="Section"/>s that includes various management functions.</summary>
	public class SectionCollection : List<Section>
	{
		#region Constructors
		public SectionCollection(IWikiNodeFactory factory, int level)
			: this(factory, level, null)
		{
		}

		public SectionCollection(IWikiNodeFactory factory, int level, IEnumerable<Section>? sections)
		{
			ArgumentNullException.ThrowIfNull(factory);
			this.Factory = factory;
			if (sections is not null)
			{
				this.AddRange(sections);
			}

			this.Level = level is >= 1 and <= 6
				? level
				: throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 6.");
		}
		#endregion

		#region Public Properties
		public IWikiNodeFactory Factory { get; }

		public int Level { get; }
		#endregion

		#region Public Methods
		public IEnumerable<Section> FindAll(string name) => this.FindAll(name, StringComparer.Ordinal, 0);

		public IEnumerable<Section> FindAll(string name, int startIndex) => this.FindAll(name, StringComparer.Ordinal, startIndex);

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

		public IEnumerable<Section> FindAll(ICollection<string> names) => this.FindAll(names, 0);

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
					var title = this[i].GetTitle();
					if (title is not null && names.Contains(title))
					{
						yield return this[i];
					}
				}
			}
		}

		public Section? FindFirst(string name) => this.FindFirst(name, StringComparer.Ordinal, 0);

		public Section? FindFirst(string name, int startIndex) => this.FindFirst(name, StringComparer.Ordinal, startIndex);

		public Section? FindFirst(string name, StringComparer comparer, int startIndex)
		{
			ArgumentNullException.ThrowIfNull(name);
			ArgumentNullException.ThrowIfNull(comparer);
			using var enumerator = this.FindAll(name, comparer, startIndex).GetEnumerator();
			return enumerator.MoveNext() ? enumerator.Current : null;
		}

		public Section? FindFirst(ICollection<string> names) => this.FindFirst(names, 0);

		public Section? FindFirst(ICollection<string> names, int startIndex)
		{
			ArgumentNullException.ThrowIfNull(names);
			using var enumerator = this.FindAll(names, startIndex).GetEnumerator();
			return enumerator.MoveNext() ? enumerator.Current : null;
		}

		public int IndexOf(string name) => this.IndexOf(name, StringComparer.Ordinal, 0);

		public int IndexOf(string name, int startIndex) => this.IndexOf(name, StringComparer.Ordinal, startIndex);

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

		public int IndexOf(ICollection<string> names) => this.IndexOf(names, 0);

		public int IndexOf(ICollection<string> names, int startIndex)
		{
			ArgumentNullException.ThrowIfNull(names);
			for (var i = startIndex; i < this.Count; i++)
			{
				var title = this[i].GetTitle();
				if (title is not null && names.Contains(title))
				{
					return i;
				}
			}

			return -1;
		}

		public void InsertWithSpaceBefore(int index, Section section, string spaceBefore = "\n\n")
		{
			ArgumentNullException.ThrowIfNull(section);
			ArgumentNullException.ThrowIfNull(spaceBefore);
			if (index > 0 && this.Count > 0)
			{
				var previous = this[index - 1].Content;
				previous.TrimEnd();
				previous.AddText(spaceBefore);
			}

			this.Insert(index, section);
		}
		#endregion
	}
}
