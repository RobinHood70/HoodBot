namespace RobinHood70.HoodBot.Jobs
{
	using System.Globalization;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class AddHashToBG : ParsedPageJob
	{
		[JobInfo("Add hash to BG", "Maintenance")]
		public AddHashToBG(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override string EditSummary => "Add hash to BG value";

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:BG", BacklinksTypes.EmbeddedIn);

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			foreach (var template in parsedPage.TemplateNodes)
			{
				if (template.Parameters.Count >= 1)
				{
					if (template.Find(1) is IParameterNode parameter
						&& parameter.Value.ToValue() is string hex
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