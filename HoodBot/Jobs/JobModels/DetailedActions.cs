namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using RobinHood70.CommonCode;

	[Flags]
	public enum ReplacementActions
	{
		None = 0,
		Move = 1,
		Edit = 1 << 1,
		Propose = 1 << 2,
		Skip = 1 << 3,
		NeedsEdited = Edit | Propose
	}

	public sealed class DetailedActions(ReplacementActions actions, string? reason)
	{
		#region Public Properties
		public ReplacementActions Actions { get; private set; } = actions;

		public string? Reason { get; private set; } = reason;
		#endregion

		#region Public Methods
		public bool HasAction(ReplacementActions action) => this.Actions.HasAnyFlag(action);

		public void SetActionFlag(ReplacementActions actions, string reason)
		{
			this.Actions |= actions;
			this.Reason = reason;
		}

		public void SetMoveActions(ReplacementActions actions, string reason)
		{
			this.Actions = actions;
			this.Reason = reason;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Actions.ToString() + (string.IsNullOrEmpty(this.Reason) ? string.Empty : " (" + this.Reason + ")");
		#endregion
	}
}