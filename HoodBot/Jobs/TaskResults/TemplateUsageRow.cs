namespace RobinHood70.HoodBot.Jobs.TaskResults
{
	using RobinHood70.WikiClasses;

	public class TemplateUsageRow
	{
		public TemplateUsageRow(string page, Template template)
		{
			this.Page = page;
			this.Template = template;
		}

		public string Page { get; }

		public Template Template { get; }
	}
}
