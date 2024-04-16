#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class RawMessageInfo
	{
		#region Constructors
		internal RawMessageInfo(string? text)
		{
			this.RawMessages = [];
			this.Text = text;
		}

		internal RawMessageInfo(IReadOnlyList<MessageItem> rawMessages)
		{
			this.RawMessages = rawMessages;
			this.Text = null;
		}
		#endregion

		#region Public Properties
		public string? Text { get; }

		public IReadOnlyList<MessageItem> RawMessages { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Text ?? "<Raw Messages>";
		#endregion
	}
}