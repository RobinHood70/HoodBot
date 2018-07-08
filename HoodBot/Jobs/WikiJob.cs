namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;

	public abstract class WikiJob : WikiRunner
	{
		protected WikiJob(Site site, AsyncInfo asyncInfo)
			: base(site) => this.AsyncInfo = asyncInfo;

		public virtual void FetchParameterData() => throw new NotImplementedException();
	}
}