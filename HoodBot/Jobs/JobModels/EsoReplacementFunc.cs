namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.BasicParser;

	// TODO: Created to possibly merge all global ESO replacements into a single operation. Abandoned for now, as that's not my primary focus, but this could/should be considered later.
	// The one concern with this methods would be the ESOLinks replacements, which require pulling in more data both before and after the actual text found. Some method will need to be developed to handle that.
	internal sealed class EsoReplacementFunc
	{
		#region Constructors
		public EsoReplacementFunc(string from, Func<string, IEnumerable<IWikiNode>> function)
		{
			this.From = from;
			this.Function = function;
		}
		#endregion

		#region Public Properties
		public string From { get; }

		public Func<string, IEnumerable<IWikiNode>> Function { get; }
		#endregion
	}
}
