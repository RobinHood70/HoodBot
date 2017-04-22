#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class MoveItem
	{
		#region Public Properties
		public ErrorItem Error { get; set; }

		public string From { get; set; }

		public string To { get; set; }

		public bool MovedOverRedirect { get; set; }
		#endregion
	}
}
