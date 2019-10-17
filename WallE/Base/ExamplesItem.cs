#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class ExamplesItem
	{
		#region Constructors
		internal ExamplesItem(string query, MessageItem description)
		{
			this.Query = query;
			this.Description = description;
		}
		#endregion

		#region Public Properties
		public MessageItem Description { get; }

		public string Query { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Query;
		#endregion
	}
}
