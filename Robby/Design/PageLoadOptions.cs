namespace RobinHood70.Robby.Design
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum PageModules
	{
		None = 0,
		Categories = 1,
		Info = 1 << 1,
		Links = 1 << 2,
		Properties = 1 << 3,
		Revisions = 1 << 4,
		Templates = 1 << 5,
		FileInfo = 1 << 6,
		CategoryInfo = 1 << 7,
		Custom = 1 << 15,
		Simple = Info | Revisions,
		All = Categories | CategoryInfo | FileInfo | Info | Links | Properties | Revisions | Templates | Custom
	}
	#endregion

	public class PageLoadOptions
	{
		#region Constructors
		public PageLoadOptions(PageModules modules) => this.Modules = modules;

		public PageLoadOptions(PageModules modules, DateTime? from, DateTime? to)
			: this(modules)
		{
			this.RevisionFrom = from;
			this.RevisionTo = to;
		}

		public PageLoadOptions(PageModules modules, long fromId, long toId)
			: this(modules)
		{
			this.RevisionFromId = fromId;
			this.RevisionToId = toId;
		}

		public PageLoadOptions(PageModules modules, DateTime from, bool newer, int count)
			: this(modules)
		{
			this.RevisionFrom = from;
			this.RevisionNewer = newer;
			this.RevisionCount = count;
		}

		public PageLoadOptions(PageModules modules, long fromId, bool newer, int count)
			: this(modules)
		{
			this.RevisionFromId = fromId;
			this.RevisionNewer = newer;
			this.RevisionCount = count;
		}
		#endregion

		#region Public Static Properties
		public static PageLoadOptions None => new PageLoadOptions(PageModules.None);

		public static PageLoadOptions Simple => new PageLoadOptions(PageModules.Simple);
		#endregion

		#region Public Properties
		public int ImageRevisionCount { get; set; }

		public PageModules Modules { get; internal set; }

		public int RevisionCount { get; set; }

		public DateTime? RevisionFrom { get; set; }

		public long RevisionFromId { get; set; }

		public bool RevisionNewer { get; set; }

		public DateTime? RevisionTo { get; set; }

		public long RevisionToId { get; set; }
		#endregion
	}
}