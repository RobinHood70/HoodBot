﻿namespace RobinHood70.HoodBot.Jobs
{
	using System.Drawing;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class RgbToHsl : EditJob
	{
		private readonly Page dyes;

		#region Constructors
		[JobInfo("RGB to HSL")]
		public RgbToHsl(JobManager jobManager)
			: base(jobManager) => this.dyes = TitleFactory.DirectNormalized(this.Site, UespNamespaces.Online, "Dyes").ToPage();
		#endregion

		protected override void BeforeLogging()
		{
			var pages = PageCollection.Unlimited(this.Site);
			pages.GetNamespace(MediaWikiNamespaces.Template, Filter.Any, "ESO ArmorDye Icon/");
			this.dyes.Load();
			var parser = new ContextualParser(this.dyes);
			foreach (var templateNode in parser.FindTemplates("ESO Dye"))
			{
				var name = templateNode.Find(1)?.Value.ToValue();
				var pageName = "Template:ESO ArmorDye Icon/" + name;
				if (pages.TryGetValue(pageName, out var page))
				{
					var value = new ContextualParser(page, InclusionType.Transcluded, true).Nodes.ToValue();
					var r = int.Parse(value.Substring(0, 2), NumberStyles.HexNumber, this.Site.Culture);
					var g = int.Parse(value.Substring(2, 2), NumberStyles.HexNumber, this.Site.Culture);
					var b = int.Parse(value.Substring(4, 2), NumberStyles.HexNumber, this.Site.Culture);
					var color = Color.FromArgb(0, r, g, b);

					templateNode.AddOrChange("rgb", value);
					templateNode.AddOrChange("h", color.GetHue().ToString("N1", this.Site.Culture));
					templateNode.AddOrChange("s", (100 * color.GetSaturation()).ToString("N1", this.Site.Culture));
					templateNode.AddOrChange("l", (100 * color.GetBrightness()).ToString("N1", this.Site.Culture));
				}
			}

			this.dyes.Text = parser.ToRaw();
		}

		protected override void Main() => this.dyes.Save("Add color information", false);
	}
}
