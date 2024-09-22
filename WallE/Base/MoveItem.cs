#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class MoveItem
	{
		#region Constructors
		internal MoveItem(ErrorItem? error, string? from, bool movedOverRedirect, bool redirectCreated, string? to)
		{
			this.Error = error;
			this.From = from;
			this.MovedOverRedirect = movedOverRedirect;
			this.RedirectCreated = redirectCreated;
			this.To = to;
		}
		#endregion

		#region Public Properties
		public ErrorItem? Error { get; }

		public string? From { get; }

		public bool MovedOverRedirect { get; }

		public bool RedirectCreated { get; }

		public string? To { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.From + " => " + this.To;
		#endregion
	}
}