namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.Robby;

[method: JobInfo("One-Off Move Job")]
public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
{
	#region Protected Override Methods
	protected override bool BeforeMain()
	{
		this.MoveAction = MoveAction.MoveSafely;
		return true;
	}

	protected override string GetEditSummary(Page page) => "Move mod pages into Morrowind Mod";

	protected override void PopulateMoves()
	{
		this.AddMove("Online:Ansei Frandar Hunding (collectible)", "Online:Ansei Frandar Hunding (patron)");
		this.AddMove("Online:Count Stefan Mornard (collectible)", "Online:Count Stefan Mornard (major adornment)");
		this.AddMove("Online:Duke of Crows (collectible)", "Online:Duke of Crows (patron)");
		this.AddMove("Online:Frost Troll (collectible)", "Online:Frost Troll (polymorph)");
		this.AddMove("Online:Grandmaster Delmene Hlaalu (collectible)", "Online:Grandmaster Delmene Hlaalu (patron)");
		this.AddMove("Online:Hermaeus Mora (collectible)", "Online:Hermaeus Mora (patron)");
		this.AddMove("Online:Psijic Loremaster Celarus (collectible)", "Online:Psijic Loremaster Celarus (patron)");
		this.AddMove("Online:Rajhin (collectible)", "Online:Rajhin (patron)");
		this.AddMove("Online:Red Eagle (collectible)", "Online:Red Eagle (patron)");
		this.AddMove("Online:Saint Pelin (collectible)", "Online:Saint Pelin (patron)");
		this.AddMove("Online:Scribing (collectible)", "Online:Scribing (upgrade)");
		this.AddMove("Online:Sorcerer-King Orgnum (collectible)", "Online:Sorcerer-King Orgnum (patron)");
		this.AddMove("Online:The Druid King (collectible)", "Online:The Druid King (patron)");
		this.AddMove("Online:Vampire Lord (collectible)", "Online:Vampire Lord (costume)");
		this.AddMove("Online:Antiquarian's Eye (Mementos)", "Online:Antiquarian's Eye (memento)");
		this.AddMove("Online:Antiquarian's Eye (Tools)", "Online:Antiquarian's Eye (tool)");
		this.AddMove("Online:First Cat's Pride Tattoo (head)", "Online:First Cat's Pride Tattoo (head marking)");
		this.AddMove("Online:First Cat's Pride Tattoo", "Online:First Cat's Pride Tattoo (body marking)");
	}
	#endregion
}