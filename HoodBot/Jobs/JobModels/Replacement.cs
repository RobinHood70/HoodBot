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
		public Replacement(ISimpleTitle from, Title to, DetailedActions actions)
		{
			this.From = from.NotNull(nameof(from));
			this.To = to.NotNull(nameof(to));
			this.MoveActions = actions;
		}

		[JsonConstructor]
		public Replacement(ISimpleTitle from, Title to)
			: this(from, to, new DetailedActions(ReplacementActions.None, string.Empty))
		{
		}
		#endregion

		#region Public Properties
		public ISimpleTitle From { get; }

		public DetailedActions MoveActions { get; }

		public Title To { get; }

		public void SetMoveActionFlag(ReplacementActions actions, string reason)
		{
			this.MoveActions.Actions |= actions;
			this.MoveActions.Reason = reason;
		}

		public void SetMoveActions(DetailedActions detailedActions)
		{
			this.MoveActions.Actions = detailedActions.Actions;
			this.MoveActions.Reason = detailedActions.Reason;
		}

		public void SetMoveActions(ReplacementActions actions, string reason)
		{
			this.MoveActions.Actions = actions;
			this.MoveActions.Reason = reason;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.MoveActions}: {this.From.FullPageName()} → {this.To.FullPageName()}";
		#endregion

		#region Public Classes
		public class DetailedActions
		{
			#region Constructors
			public DetailedActions(ReplacementActions actions)
				: this(actions, string.Empty)
			{
			}

			public DetailedActions(ReplacementActions actions, string reason)
			{
				this.Actions = actions;
				this.Reason = reason;
			}
			#endregion

			#region Public Properties
			public ReplacementActions Actions { get; set; }

			public string Reason { get; set; }
			#endregion

			#region Public Methods
			public bool HasAction(ReplacementActions action) => (this.Actions & action) != 0;
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Actions.ToString() + (string.IsNullOrEmpty(this.Reason) ? string.Empty : " (" + this.Reason + ")");
			#endregion
		}
		#endregion
	}
}