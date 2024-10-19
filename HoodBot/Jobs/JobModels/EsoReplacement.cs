namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoReplacement(string from, IList<IWikiNode> to)
	{
		#region Public Properties
		public string From { get; } = from;

		public IList<IWikiNode> To { get; } = to;
		#endregion

		#region Public Override Methods
		public override string ToString() => this.From;
		#endregion
	}
}