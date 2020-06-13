namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoReplacement : IComparable<EsoReplacement>
	{
		#region Constructors
		public EsoReplacement(string from, string to)
		{
			this.From = from;
			this.To = to;
			this.ToNodes = WikiTextParser.Parse(to);
		}
		#endregion

		#region Public Properties
		public string From { get; }

		public string To { get; }

		public NodeCollection ToNodes { get; }
		#endregion

		#region Public Methods
		public int CompareTo(EsoReplacement? other) => string.Compare(this.From, other?.From, StringComparison.Ordinal);
		#endregion
	}
}
