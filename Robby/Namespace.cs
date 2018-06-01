namespace RobinHood70.Robby
{
	using System.Collections.Generic;
	using WallE.Base;

	public class Namespace
	{
		#region Fields
		private readonly HashSet<string> allNames = new HashSet<string>();
		private readonly NamespaceFlags flags;
		#endregion

		#region Constructors
		internal Namespace(NamespacesItem ns, List<string> aliases)
		{
			aliases = aliases ?? new List<string>();
			this.Id = ns.Id;
			this.Name = ns.Name;
			this.flags = ns.Flags;
			this.CanonicalName = ns.CanonicalName;
			this.Aliases = aliases.AsReadOnly();

			this.AddName(ns.Name);
			this.AddName(this.CanonicalName);
			foreach (var item in aliases)
			{
				this.AddName(item);
			}

			this.allNames.TrimExcess();
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Aliases { get; }

		public IReadOnlyCollection<string> AllNames => this.allNames;

		public bool AllowsSubpages => this.flags.HasFlag(NamespaceFlags.Subpages);

		public string CanonicalName { get; }

		public bool CaseSensitive => this.flags.HasFlag(NamespaceFlags.CaseSensitive);

		public bool ContentSpace => this.flags.HasFlag(NamespaceFlags.ContentSpace);

		public string DecoratedName => this.Id == 0 ? string.Empty : this.Name + ':';

		public int Id { get; private set; }

		public string Name { get; private set; }

		public int SubjectSpaceId => this.Id >= 0 ? this.Id & 0x7ffffffe : this.Id;

		public int? TalkSpaceId => this.Id >= 0 ? (int?)this.Id | 1 : null;
		#endregion

		#region Public Operators
		public static bool operator ==(Namespace left, string right) => left?.allNames.Contains(right) ?? false;

		public static bool operator !=(Namespace left, string right) => !left?.allNames.Contains(right) ?? true;

		public static bool operator ==(string left, Namespace right) => right?.allNames.Contains(left) ?? false;

		public static bool operator !=(string left, Namespace right) => right?.allNames.Contains(left) ?? true;
		#endregion

		#region Public Overrides
		public override bool Equals(object obj)
		{
			var rhs = obj as Namespace;
			if (rhs == null)
			{
				return false;
			}

			return this.Id == rhs.Id;
		}

		public override int GetHashCode() => this.Id.GetHashCode();

		public override string ToString() => this.Name;
		#endregion

		#region Internal Methods
		internal string AddName(string name)
		{
			// TODO: For now, this uses ToLowerInvariant for the first letter. Is this a concern? Possibly add localization based on wiki's reported language.
			this.allNames.Add(name);
			if (!this.CaseSensitive && name.Length > 0)
			{
				var lowerName = char.ToLowerInvariant(name[0]) + (name.Length == 1 ? string.Empty : name.Substring(1));
				this.allNames.Add(lowerName);
				return lowerName;
			}

			return null;
		}
		#endregion
	}
}