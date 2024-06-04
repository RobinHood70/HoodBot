namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Globalization;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Add hash to BG", "Maintenance")]
	public class AddHashToBG(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Add hash to BG value";

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:BG", BacklinksTypes.EmbeddedIn);

		protected override void ParseText(ContextualParser parser)
		{
			ArgumentNullException.ThrowIfNull(parser);
			foreach (var template in parser.TemplateNodes)
			{
				if (template.Parameters.Count >= 1 &&
					template.Find(1) is IParameterNode parameter &&
					parameter.Value.ToValue() is string hex &&
					hex.Length == 6 &&
					int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
				{
					parameter.SetValue("#" + hex, ParameterFormat.Copy);
				}
			}
		}
		#endregion
	}
}