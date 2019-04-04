namespace RobinHood70.HoodBot.Jobs.Design
{
	using RobinHood70.Robby;
	using static RobinHood70.WikiCommon.Globals;

	public enum ReplacementAction
	{
		Unknown,
		Skip,
		Move,
		ProposeForDeletion,
	}

	public class Replacement
	{
		public Replacement(Site site, string from, string to)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(from, nameof(from));
			ThrowNull(to, nameof(to));
			this.From = new Title(site, from);
			this.To = new Title(site, to);
		}

		public Replacement(Title from, Title to)
		{
			ThrowNull(from, nameof(from));
			ThrowNull(to, nameof(to));
			this.From = from;
			this.To = to;
		}

		public ReplacementAction Action { get; set; } = ReplacementAction.Unknown;

		public string ActionReason { get; set; }

		public Title From { get; }

		public Title To { get; }
	}
}
