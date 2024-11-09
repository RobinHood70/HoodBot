namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	[method: JobInfo("Effects", "Starfield")]
	internal sealed partial class SFEffects(JobManager jobManager) : CreateOrUpdateJob<SFEffects.Effect>(jobManager)
	{
		#region Protected Override Properties
		protected override string? Disambiguator => "effect";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Update effects";

		protected override bool IsValid(SiteParser parser, Effect item) =>
			parser.Page.IsRedirect &&
			parser.LinkNodes.First() is SiteLinkNode linkNode &&
			linkNode.Title == "Starfield:Effects";

		protected override IDictionary<Title, Effect> LoadItems()
		{
			var effects = LoadEffects();
			var effectsPage = this.Site.LoadPage("Starfield:Effects") ?? throw new InvalidOperationException();
			effectsPage.Text = effectsPage.Text.Replace("|-\n|}", "|}", StringComparison.Ordinal);
			var rows = EffectsRowFinder().Matches(effectsPage.Text);
			FilterEffects(effects, rows);
			effects.Sort();

			var pos = 0;
			if (rows.Count > 0)
			{
				var lastRow = rows[^1];
				pos = lastRow.Index + lastRow.Length;
			}

			var insertPos = effectsPage.Text.IndexOf("|}", pos, StringComparison.Ordinal);

			var sb = new StringBuilder();
			var retval = new Dictionary<Title, Effect>();
			foreach (var effect in effects)
			{
				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + effect.Name);
				retval.TryAdd(title, effect);
				sb
					.Append("|-\n");
				if (effect.Name.Length == 0)
				{
					sb.Append("|NO NAME");
				}
				else
				{
					sb
						.Append("|{{Anchor|")
						.Append(title.ToSiteLink().AsLink(LinkFormat.PipeTrick))
						.Append("}}");
				}

				sb
					.Append(GameInfo.Starfield.ModTemplate)
					.Append(" || ")
					.Append(effect.FormId)
					.Append(" || ")
					.Append(effect.EditorId)
					.Append(" || ")
					.Append(effect.Description)
					.Append('\n');
			}

			effectsPage.Text = effectsPage.Text.Insert(insertPos, sb.ToString());
			this.Pages.Add(effectsPage);

			return retval;
		}

		private static void FilterEffects(Effects effects, IReadOnlyList<Match> rows)
		{
			foreach (var row in rows)
			{
				var key = row.Groups["EditorId"].Value;
				if (effects.Remove(row.Groups["EditorId"].Value))
				{
					Debug.WriteLine(key);
				}
			}
		}
		#endregion

		#region Private Static Methods
		[GeneratedRegex(@"\|-\s+\|\s*(NO NAME|{{Anchor\|.*?}})\s*\|\|\s*0x[x0-9A-F]{8}.*?\s*\|\|\s*(?<EditorId>.*?)\s*\|\|.*?", RegexOptions.ExplicitCapture, 10000)]
		private static partial Regex EffectsRowFinder();

		private static Effects LoadEffects()
		{
			var retval = new Effects();
			var effectsFile = new CsvFile(GameInfo.Starfield.ModFolder + "Effects.csv");
			foreach (var row in effectsFile.ReadRows())
			{
				var effect = new Effect(
				row["FormID"][2..],
				row["EditorID"],
				row["Name"],
				row["Description"]);
				retval.Add(effect);
			}

			return retval;
		}
		#endregion

		#region Internal Records

		internal record Effect(string FormId, string EditorId, string Name, string Description);
		#endregion

		#region Private Classes
		private sealed class Effects() : KeyedCollection<string, Effect>(StringComparer.Ordinal)
		{
			public void Sort() => ((List<Effect>)this.Items).Sort((effect1, effect2) => effect1.Name.CompareTo(effect2.Name));

			protected override string GetKeyForItem(Effect item)
			{
				ArgumentNullException.ThrowIfNull(item);
				return item.EditorId;
			}
		}
		#endregion
	}
}
