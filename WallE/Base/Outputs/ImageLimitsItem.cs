#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WallE.Properties;
	using static RobinHood70.CommonCode.Globals;

	public class ImageLimitsItem
	{
		#region Constructors
		internal ImageLimitsItem(int width, int height)
		{
			this.Width = width;
			this.Height = height;
		}
		#endregion

		#region Public Properties
		public int Height { get; }

		public int Width { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => CurrentCulture(Messages.CommaText, this.Width, this.Height);
		#endregion
	}
}
