#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum MessageFlags
	{
		None = 0,
		Customized = 1,
		DefaultMissing = 1 << 1,
		Missing = 1 << 2
	}
	#endregion

	public class AllMessagesItem
	{
		#region Public Properties
		public string Content { get; set; }

		public string Default { get; set; }

		public MessageFlags Flags { get; set; }

		public string Name { get; set; }

		public string NormalizedName { get; set; }
		#endregion
	}
}