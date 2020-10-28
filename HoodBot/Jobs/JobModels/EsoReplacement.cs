namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoReplacement
	{
		#region Constructors
		public EsoReplacement(string from, NodeCollection to)
		{
			this.From = from;
			this.To = to;
		}
		#endregion

		#region Public Properties
		public string From { get; }

		public NodeCollection To { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.From;
		#endregion
	}
}
