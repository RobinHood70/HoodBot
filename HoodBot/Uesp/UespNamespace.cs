namespace RobinHood70.HoodBot.Uesp
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	public class UespNamespace
	{
		#region Constructors
		internal UespNamespace(Site site, string line)
		{
			ThrowNull(line, nameof(line));
			var nsData = string.Concat(line, ";;;;;;").Split(TextArrays.Semicolon);
			for (var i = 0; i < nsData.Length; i++)
			{
				nsData[i] = nsData[i].Trim();
			}

			this.Base = nsData[0];
			var baseSplit = this.Base.Split(TextArrays.Colon, 2);
			this.BaseNamespace = site[baseSplit[0]];
			this.IsPseudoNamespace = baseSplit.Length == 2;
			this.Full = this.Base + (this.IsPseudoNamespace ? '/' : ':');
			this.Id = nsData[1].Length == 0 ? this.Base.ToUpperInvariant() : nsData[1];
			var parentName = nsData[2].Length == 0 ? this.Base : nsData[2];
			this.Parent = site[parentName];
			this.Name = nsData[3].Length == 0 ? this.Base : nsData[3];
			this.MainPage = Title.FromName(site, nsData[4].Length == 0 ? this.Full + this.Name : nsData[4]);
			this.Category = nsData[5].Length == 0 ? this.Base : nsData[5];
			this.Trail = nsData[6].Length == 0 ? string.Concat("[[", this.MainPage, "|", this.Name, "]]") : nsData[6];
		}
		#endregion

		#region Public Properties
		public string Base { get; }

		public Namespace BaseNamespace { get; }

		public string Category { get; }

		public string Full { get; }

		public string Id { get; }

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