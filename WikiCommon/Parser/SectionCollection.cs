namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;

	/// <summary>A wrapper for a list of <see cref="Section"/>s that includes various management functions.</summary>
	public class SectionCollection : List<Section>
	{
		#region Constructors
		public SectionCollection(int level)
			: this(level, null)
		{
		}

		public SectionCollection(int level, IEnumerable<Section>? sections)
		{
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

		public Section? FindFirst(string name) => FindFirst(name, StringComparer.Ordinal, 0);

		public Section? FindFirst(string name, int startIndex) => FindFirst(name, StringComparer.Ordinal, startIndex);

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

		public int IndexOf(string name) => IndexOf(name, StringComparer.Ordinal, 0);

		public int IndexOf(string name, int startIndex) => IndexOf(name, StringComparer.Ordinal, startIndex);

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
			for (var i = 0; i < this.Count; i++)
			{
				var title = this[i].GetTitle();
				if (title is not null && names.Contains(title))
				{
					return i;
				}
			}

			return -1;
		}
		#endregion
	}
}
