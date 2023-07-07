namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public sealed class UespNamespace : IEquatable<UespNamespace>
	{
		#region Constructors
		internal UespNamespace(Site site, string line)
		{
			this.Site = site.NotNull();
			var nsData = string.Concat(line.NotNull(), ";;;;;;").Split(TextArrays.Semicolon);
			for (var i = 0; i < nsData.Length; i++)
			{
				nsData[i] = nsData[i].Trim();
			}

			var baseName = nsData[0];
			this.Base = baseName;
			var colonLoc = baseName.IndexOf(':', StringComparison.Ordinal);
			this.IsPseudoNamespace = colonLoc != -1;
			var baseNamespace = this.IsPseudoNamespace
				? baseName[..colonLoc]
				: baseName;
			this.BaseNamespace = site[baseNamespace];
			this.Full = baseName + (this.IsPseudoNamespace ? "/" : ":");
			this.Id = nsData[1].Length == 0 ? baseName.ToUpperInvariant() : nsData[1];
			var parentName = nsData[2].Length == 0 ? baseName : nsData[2];
			this.Parent = site[parentName];
			this.Name = nsData[3].Length == 0 ? baseName : nsData[3];
			this.MainPage = TitleFactory.FromUnvalidated(site, nsData[4].Length == 0 ? this.Full + this.Name : nsData[4]);
			this.Category = nsData[5].Length == 0 ? baseName : nsData[5];
			this.Trail = nsData[6].Length == 0 ? string.Concat("[[", this.MainPage, "|", this.Name, "]]") : nsData[6];
			this.IsGameSpace = UespNamespaces.IsGamespace(this.BaseNamespace.Id);
		}
		#endregion

		#region Public Properties
		public string Base { get; }

		public Namespace BaseNamespace { get; }

		public string Category { get; }

		public string Full { get; }

		public string Id { get; }

		public bool IsGameSpace { get; }

		public bool IsPseudoNamespace { get; }

		public Title MainPage { get; }

		public string Name { get; }

		public Namespace Parent { get; }

		public Site Site { get; }

		public string Trail { get; }
		#endregion

		#region Public Methods
		public bool Equals(UespNamespace? other) =>
			other is not null &&
			string.Equals(this.Base, other.Base, StringComparison.Ordinal);

		public Title GetTitle(string pageName) => TitleFactory.FromUnvalidated(this.Site, this.Full + pageName);
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => obj is UespNamespace other && this.Equals(other);

		public override int GetHashCode() => this.Base.GetHashCode(StringComparison.Ordinal);

		public override string ToString() => this.Base;
		#endregion
	}
}