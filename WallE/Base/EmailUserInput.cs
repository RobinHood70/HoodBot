#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class EmailUserInput
	{
		#region Constructors
		public EmailUserInput(string target, string text)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(target);
			ArgumentException.ThrowIfNullOrWhiteSpace(text);
			this.Target = target;
			this.Text = text;
		}
		#endregion

		#region Public Properties
		public bool CCMe { get; set; }

		public string? Subject { get; set; }

		public string Target { get; }

		public string Text { get; }

		public string? Token { get; set; }
		#endregion
	}
}