namespace RobinHood70.HoodBot.Uesp
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class UespNamespace
	{
		#region Constructors
		internal UespNamespace(Site site, string line)
		{
			var nsData = string.Concat(line.NotNull(nameof(line)), ";;;;;;").Split(TextArrays.Semicolon);
			for (var i = 0; i < nsData.Length; i++)
			{
				nsData[i] = nsData[i].Trim();
			}

			var baseName = nsData[0];
			this.Base = baseName;
			this.IsPseudoNamespace = !site.Namespaces.TryGetValue(baseName, out var nsBase);
			this.BaseTitle = (nsBase is not null
				? TitleFactory.Direct(nsBase, string.Empty)
				: TitleFactory.FromName(site, baseName)).ToTitle();
			this.Full = baseName + (this.IsPseudoNamespace ? '/' : ':');
			this.Id = nsData[1].Length == 0 ? baseName.ToUpperInvariant() : nsData[1];
			var parentName = nsData[2].Length == 0 ? baseName : nsData[2];
			this.Parent = site[parentName];
			this.Name = nsData[3].Length == 0 ? baseName : nsData[3];
			this.MainPage = TitleFactory.FromName(site, nsData[4].Length == 0 ? this.Full + this.Name : nsData[4]).ToTitle();
			this.Category = nsData[5].Length == 0 ? baseName : nsData[5];
			this.Trail = nsData[6].Length == 0 ? string.Concat("[[", this.MainPage, "|", this.Name, "]]") : nsData[6];
			this.IsGameSpace = UespNamespaces.IsGamespace(this.Parent.Id);
		}
		#endregion

		#region Public Properties
		public string Base { get; }

		public Title BaseTitle { get; }

		public string Category { get; }

		public string Full { get; }

		public string Id { get; }

		public bool IsGameSpace { get; }

		public bool IsPseudoNamespace { get; }

		public Title MainPage { get; }

		public string Name { get; }

		public Namespace Parent { get; }

		public string Trail { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Base;
		#endregion
	}
}