namespace RobinHood70.HoodBot.Jobs.EsoSkillSummaries
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public abstract class EsoSkillJob : EditJob
	{
		protected EsoSkillJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
			: base(site, asyncInfo, tasks)
		{
			this.StatusWriteLine("Getting Replacements");
			EsoReplacer.Initialize(this.Site);
		}
	}
}
