namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;

public class OneOffMoveJob : MovePagesJob
{
	#region Constructors
	[JobInfo("One-Off Move Job")]
	public OneOffMoveJob(JobManager jobManager, bool updateUserSpace)
		: base(jobManager, updateUserSpace)
	{
		this.MoveAction = MoveAction.MoveSafely;
		this.FollowUpActions = FollowUpActions.CheckLinksRemaining | FollowUpActions.EmitReport | FollowUpActions.FixLinks;
	}
	#endregion

	#region Protected Override Methods
	protected override void CustomEdit(SiteParser parser, Title from) => parser.AddCategory("Daggerfall-NPC Face Flats", true);

	protected override string GetEditSummary(Page page) => "Fix name";

	protected override void PopulateMoves()
	{
		for (var i = 163; i <= 212; i++)
		{
			this.AddReplacement($"File:DF-Face-Flat-{i}.png", $"File:DF-Face-Flat {i}.png", ReplacementActions.Move | ReplacementActions.Edit, null);
		}

		this.AddReplacement($"File:DF-Face-Flat-360.png", $"File:DF-Face-Flat 360.png", ReplacementActions.Move | ReplacementActions.Edit, null);
		this.AddReplacement($"File:DF-Face-Flat-396-0.png", $"File:DF-Face-Flat 396-0.png", ReplacementActions.Move | ReplacementActions.Edit, null);
		this.AddReplacement($"File:DF-Face-Flat-415.png", $"File:DF-Face-Flat 415.png", ReplacementActions.Move | ReplacementActions.Edit, null);
		this.AddReplacement($"File:DF-Face-Flat-444.png", $"File:DF-Face-Flat 444.png", ReplacementActions.Move | ReplacementActions.Edit, null);
		this.AddReplacement($"File:DF-Face-Flat-445.png", $"File:DF-Face-Flat 445.png", ReplacementActions.Move | ReplacementActions.Edit, null);
		this.AddReplacement($"File:DF-Face-Flat-492.png", $"File:DF-Face-Flat 492.png", ReplacementActions.Move | ReplacementActions.Edit, null);
	}
	#endregion
}