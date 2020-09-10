namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoReplacement
	{
		#region Constructors
		public EsoReplacement(string from, string to)
		{
			this.From = from;
			this.To = NodeCollection.Parse(to);
		}
		#endregion

		#region Public Properties
		public string From { get; }

		public NodeCollection To { get; }
		#endregion
	}
}
