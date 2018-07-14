namespace RobinHood70.HoodBot.Jobs.TaskResults
{
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;

	public class TemplateCollection : List<TemplateUsageRow>
	{
		public Dictionary<string, int> HeaderOrder { get; } = new Dictionary<string, int>();

		public void Add(string page, Template template) => this.Add(new TemplateUsageRow(page, template));

		internal static TemplateCollection GetTemplates(IEnumerable<string> allNames, PageCollection pages)
		{
			var allTemplates = new TemplateCollection();
			var headersLookup = new HashSet<string>(); // Unordered for fast lookups.
			IList<string> headerNames = new List<string>(); // Ordered because order should be maintained; defined as interface because implementation may change later.
			foreach (var page in pages)
			{
				var find = Template.Find(allNames);
				var matches = find.Matches(page.Text);
				foreach (Match match in matches)
				{
					var template = new Template(match.Value);
					template.ForceNames();
					allTemplates.Add(page.FullPageName, template);
					foreach (var param in template)
					{
						if (headersLookup.Add(param.Name))
						{
							headerNames.Add(param.Name);
						}
					}
				}
			}

			for (var i = 0; i < headerNames.Count; i++)
			{
				allTemplates.HeaderOrder.Add(headerNames[i], i);
			}

			return allTemplates;
		}
	}
}
