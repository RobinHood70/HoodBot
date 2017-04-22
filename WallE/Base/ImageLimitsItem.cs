#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using static Properties.Messages;
	using static RobinHood70.Globals;

	public class ImageLimitsItem
	{
		#region Constructors
		public ImageLimitsItem(int width, int height)
		{
			this.Width = width;
			this.Height = height;
		}
		#endregion

		#region Public Properties
		public int Height { get; }

		public int Width { get; }

		public override string ToString() => CurrentCulture(CommaText, this.Width, this.Height);
		#endregion
	}
}
