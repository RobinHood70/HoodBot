namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	[Flags]
	public enum ReplacementActions
	{
		None = 0,
		Move = 1,
		Edit = 1 << 1,
		Propose = 1 << 2,
		Skip = 1 << 3,
		UpdateLinks = 1 << 4,
		NeedsEdited = Edit | Propose
	}

	public sealed class Replacement
	{
		#region Constructors
		public Replacement(Site site, string from, string to)
			: this(TitleFactory.FromName(site, from).ToTitle(), TitleFactory.FromName(site, to).ToTitle())
		{
		}

		[JsonConstructor]
		public Replacement(ISimpleTitle from, Title to)
		{
			this.From = from.NotNull(nameof(from));
			this.To = to.NotNull(nameof(to));
		}
		#endregion

		#region Public Properties
		public ReplacementActions Actions { get; set; }

		public ISimpleTitle From { get; }

		public string? Reason { get; set; }

		public Title To { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.Actions}: {this.From.FullPageName()} → {this.To.FullPageName()}";
		#endregion
	}
}