namespace RobinHood70.HoodBot.Jobs.TaskResults
{
	using System.Collections.Generic;
	using RobinHood70.Robby;

	public class TemplateUsage
	{
		public TemplateUsage(IEnumerable<string> allNames, TitleCollection templates, PageCollection pages)
		{
			this.AllNames = allNames;
			this.Pages = pages;
			this.Templates = templates;
		}

		public IEnumerable<string> AllNames { get; internal set; }

		public PageCollection Pages { get; }

		public TitleCollection Templates { get; }
	}
}
