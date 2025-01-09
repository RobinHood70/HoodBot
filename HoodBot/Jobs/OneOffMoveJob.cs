namespace RobinHood70.HoodBot.Jobs;
using RobinHood70.Robby;

[method: JobInfo("One-Off Move Job")]
public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
{
	#region Protected Override Methods
	protected override bool BeforeMain()
	{
		this.FollowUpActions |= FollowUpActions.NeedsCategoryMembers;
		this.MoveAction = MoveAction.None;
		return base.BeforeMain();
	}

	protected override string GetEditSummary(Page page) => "Update categories";

	protected override void PopulateMoves()
	{
		this.AddLinkUpdate("Category:Online-Sets with Weapon Damage", "Category:Online-Sets with Weapon/Spell Damage");
		this.AddLinkUpdate("Category:Online-Sets with Spell Damage", "Category:Online-Sets with Weapon/Spell Damage");
		this.AddLinkUpdate("Category:Online-Sets with Double Weapon Damage", "Category:Online-Sets with Double Weapon/Spell Damage");
		this.AddLinkUpdate("Category:Online-Sets with Double Spell Damage", "Category:Online-Sets with Double Weapon/Spell Damage");
		this.AddLinkUpdate("Category:Online-Sets with Triple Weapon Damage", "Category:Online-Sets with Triple Weapon/Spell Damage");
		this.AddLinkUpdate("Category:Online-Sets with Triple Spell Damage", "Category:Online-Sets with Triple Weapon/Spell Damage");
		this.AddLinkUpdate("Category:Online-Sets with Weapon Critical", "Category:Online-Sets with Weapon/Spell Critical");
		this.AddLinkUpdate("Category:Online-Sets with Spell Critical", "Category:Online-Sets with Weapon/Spell Critical");
		this.AddLinkUpdate("Category:Online-Sets with Double Weapon Critical", "Category:Online-Sets with Double Weapon/Spell Critical");
		this.AddLinkUpdate("Category:Online-Sets with Double Spell Critical", "Category:Online-Sets with Double Weapon/Spell Critical");
		this.AddLinkUpdate("Category:Online-Sets with Triple Weapon Critical", "Category:Online-Sets with Triple Weapon/Spell Critical");
		this.AddLinkUpdate("Category:Online-Sets with Triple Spell Critical", "Category:Online-Sets with Triple Weapon/Spell Critical");
		this.AddLinkUpdate("Category:Online-Sets with Physical Penetration", "Category:Online-Sets with Physical/Spell Penetration");
		this.AddLinkUpdate("Category:Online-Sets with Spell Penetration", "Category:Online-Sets with Physical/Spell Penetration");
		this.AddLinkUpdate("Category:Online-Sets with Double Physical Penetration", "Category:Online-Sets with Double Physical/Spell Penetration");
		this.AddLinkUpdate("Category:Online-Sets with Double Spell Penetration", "Category:Online-Sets with Double Physical/Spell Penetration");
		this.AddLinkUpdate("Category:Online-Sets with Triple Physical Penetration", "Category:Online-Sets with Triple Physical/Spell Penetration");
		this.AddLinkUpdate("Category:Online-Sets with Triple Spell Penetration", "Category:Online-Sets with Triple Physical/Spell Penetration");
		this.AddLinkUpdate("Category:Online-Sets with Physical Resistance", "Category:Online-Sets with Physical/Spell Resistance");
		this.AddLinkUpdate("Category:Online-Sets with Spell Resistance", "Category:Online-Sets with Physical/Spell Resistance");
		this.AddLinkUpdate("Category:Online-Sets with Double Physical Resistance", "Category:Online-Sets with Double Physical/Spell Resistance");
		this.AddLinkUpdate("Category:Online-Sets with Double Spell Resistance", "Category:Online-Sets with Double Physical/Spell Resistance");
	}
	#endregion
}