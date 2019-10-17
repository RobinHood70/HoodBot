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

	// Args is not implemented here because it just echoes back the input.
	public class AllMessagesItem
	{
		#region Constructors
		internal AllMessagesItem(string? content, string? def, MessageFlags flags, string name, string normalizedName)
		{
			this.Content = content;
			this.Default = def;
			this.Flags = flags;
			this.Name = name;
			this.NormalizedName = normalizedName;
		}
		#endregion

		#region Public Properties
		public string? Content { get; }

		public string? Default { get; }

		public MessageFlags Flags { get; }

		public string Name { get; }

		public string NormalizedName { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}