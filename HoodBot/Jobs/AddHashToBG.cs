namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class AddHashToBG : ParsedPageJob
	{
		[JobInfo("Add hash to BG", "Maintenance")]
		public AddHashToBG([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		protected override string EditSummary => "Add hash to BG value";

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:BG", BacklinksTypes.EmbeddedIn);

		protected override void ParseText(object sender, Page page, ContextualParser parsedPage)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(parsedPage, nameof(parsedPage));
			var templates = parsedPage.FindAllRecursive<TemplateNode>();
			foreach (var template in templates)
			{
				if (template.Parameters.Count >= 1)
				{
					if (template.FindNumberedParameter(1) is ParameterNode parameter
						&& parameter.ValueToText() is string hex
						&& hex.Length == 6
						&& int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
					{
						parameter.SetValue("#" + hex);
					}
				}
			}
		}
	}
}