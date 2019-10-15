#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class AllImagesItem : ImageInfoItem, ITitle
	{
		#region Constructors
		internal AllImagesItem(int ns, string title, string name)
		{
			this.Namespace = ns;
			this.Title = title;
			this.Name = name;
		}
		#endregion

		#region Public Properties
		public string? DescriptionUrl { get; set; }

		public string Name { get; set; }

		public int Namespace { get; }

		public string Title { get; }

		public string? Url { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
