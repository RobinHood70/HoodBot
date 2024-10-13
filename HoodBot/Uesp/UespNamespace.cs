namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public sealed class UespNamespace : IEquatable<UespNamespace>
	{
		#region Constructors
		internal UespNamespace(Site site, NsinfoItem ns)
		{
			ArgumentNullException.ThrowIfNull(site);
			ArgumentNullException.ThrowIfNull(ns);
			this.Base = ns.Base;
			this.BaseNamespace = site[ns.NsId];
			this.Category = ns.Category;
			this.Full = ns.Full;
			this.Icon = ns.Icon;
			this.IconUrl = ns.IconUrl.Length == 0 ? null : new Uri(ns.IconUrl);
			this.Id = ns.Id;
			this.IsGamespace = ns.IsGamespace;
			this.IsModspace = ns.IsModspace;
			this.IsPseudoNamespace = ns.IsPseudospace;
			this.MainPage = TitleFactory.FromUnvalidated(site, ns.MainPage);
			this.ModName = ns.ModName;
			this.ModParent = ns.ModParent;
			this.Name = ns.Name;
			this.PageName = ns.PageName;
			this.Parent = site[ns.Parent];
			this.Trail = ns.Trail;
		}
		#endregion

		#region Public Properties
		public string Base { get; }

		public Namespace BaseNamespace { get; }

		public string Category { get; }

		public string Full { get; }

		public string Icon { get; }

		public Uri? IconUrl { get; }

		public string Id { get; }

		public bool IsGamespace { get; }

		public bool IsModspace { get; }

		public bool IsPseudoNamespace { get; }

		public Title MainPage { get; }

		public string ModName { get; }

		public string ModParent { get; }

		public string Name { get; }

		public string PageName { get; }

		public Namespace Parent { get; }

		public Site Site => this.BaseNamespace.Site;

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