#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class AllCategoriesItem
	{
		#region Constructors
		internal AllCategoriesItem(string category, int files, bool hidden, int pages, int size, int subcats)
		{
			this.Category = category;
			this.Files = files;
			this.Hidden = hidden;
			this.Pages = pages;
			this.Size = size;
			this.Subcategories = subcats;
		}
		#endregion

		#region Public Properties
		public string Category { get; }

		public int Files { get; }

		public bool Hidden { get; }

		public int Pages { get; }

		public int Size { get; }

		public int Subcategories { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Category;
		#endregion
	}
}
