#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	public class HelpResult
	{
		#region Constructors
		internal HelpResult(IReadOnlyList<string> help, string mime)
		{
			this.Help = help;
			this.Mime = mime;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Help { get; }

		public string Mime { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Help.Count == 0
			? this.Mime
			: this.Help[0].Ellipsis(30);
		#endregion
	}
}