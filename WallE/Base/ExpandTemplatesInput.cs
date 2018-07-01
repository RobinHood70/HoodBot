#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using static RobinHood70.WallE.ProjectGlobals;

	[Flags]
	public enum ExpandTemplatesProperties
	{
		None = 0,
		WikiText = 0x1,
		Categories = 0x1 << 1,
		Properties = 0x1 << 2,
		Volatile = 0x1 << 3,
		Ttl = 0x1 << 4,
		Modules = 0x1 << 5,
		JsConfigVars = 0x1 << 6,
		//// EncodedJsConfigVars = 0x1 << 7, Not added because it appears to be a simple reformat of the above in JSON
		ParseTree = 0x1 << 8,
		All = WikiText | Categories | Properties | Volatile | Ttl | Modules | JsConfigVars | ParseTree
	}

	public class ExpandTemplatesInput
	{
		#region Constructors
		public ExpandTemplatesInput(string text)
		{
			ThrowNullOrWhiteSpace(text, nameof(text));
			this.Text = text;
		}
		#endregion

		#region Public Properties
		public bool IncludeComments { get; set; }

		public ExpandTemplatesProperties Properties { get; set; }

		public long RevisionId { get; set; }

		public string Text { get; }

		public string Title { get; set; }
		#endregion
	}
}