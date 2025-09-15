namespace RobinHood70.HoodBot.Jobs;
using RobinHood70.Robby;

[method: JobInfo("One-Off Move Job")]
public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
{
	#region Protected Override Methods
	protected override bool BeforeMain()
	{
		this.MoveAction = MoveAction.MoveSafely;
		return base.BeforeMain();
	}

	protected override string GetEditSummary(Page page) => "Move mod pages into Morrowind Mod";

	protected override void PopulateMoves()
	{
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded", "Morrowind Mod:Sixth House Reloaded");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Axes", "Morrowind Mod:Sixth House Reloaded/Axes");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Blunt Weapons", "Morrowind Mod:Sixth House Reloaded/Blunt Weapons");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Creatures", "Morrowind Mod:Sixth House Reloaded/Creatures");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Dreamers", "Morrowind Mod:Sixth House Reloaded/Dreamers");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Items", "Morrowind Mod:Sixth House Reloaded/Items");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Items/Dagoth Honor Guard", "Morrowind Mod:Sixth House Reloaded/Items/Dagoth Honor Guard");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Items/Dreamer(style)", "Morrowind Mod:Sixth House Reloaded/Items/Dreamer(style)");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Items/Ingredients", "Morrowind Mod:Sixth House Reloaded/Items/Ingredients");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Items/Weapons", "Morrowind Mod:Sixth House Reloaded/Items/Weapons");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Long Blades", "Morrowind Mod:Sixth House Reloaded/Long Blades");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/MWSE Spell Effects", "Morrowind Mod:Sixth House Reloaded/MWSE Spell Effects");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Marksman Weapons", "Morrowind Mod:Sixth House Reloaded/Marksman Weapons");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Short Blades", "Morrowind Mod:Sixth House Reloaded/Short Blades");
		this.AddMove("User:ImmortalCorvus/Sixth House Reloaded/Spears", "Morrowind Mod:Sixth House Reloaded/Spears");
	}
	#endregion
}