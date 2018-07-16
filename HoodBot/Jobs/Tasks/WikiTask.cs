namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class WikiTask : WikiRunner
	{
		#region Constructors
		protected WikiTask(WikiRunner parent)
			: base(parent?.Site)
		{
			ThrowNull(parent, nameof(parent));
			this.Parent = parent;
			this.Job = (parent as WikiJob) ?? (parent as WikiTask).Job;
		}
		#endregion

		#region Public Properties
		public WikiJob Job { get; } // Top-level Job object.

		public WikiRunner Parent { get; } // Immediate parent, in the event of task nesting.
		#endregion

		#region Internal Methods
		internal void SetAsyncInfoWithIntercept(Progress<double> taskProgressIntercept) => this.AsyncInfo = this.Parent.AsyncInfo.With(taskProgressIntercept);
		#endregion
	}
}
