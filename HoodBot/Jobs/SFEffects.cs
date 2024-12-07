namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

// CONSIDER: Should redirects from EDID pages be to EDID targets with Anchor including both Name and EDID?
[method: JobInfo("Effects", "Starfield")]
internal sealed partial class SFEffects(JobManager jobManager) : CreateOrUpdateJob<SFEffects.Effect>(jobManager)
{
	#region Fields
	private readonly Title effectsTitle = TitleFactory.FromUnvalidated(jobManager.Site, "Starfield:Effects");
	#endregion

	#region Protected Override Properties
	protected override string? Disambiguator => "effect";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update effects";

	protected override bool IsValid(SiteParser parser, Effect item) =>
		parser.Page.IsRedirect &&
		parser.LinkNodes.First() is ILinkNode linkNode &&
		linkNode.GetTitle(parser.Site) == this.effectsTitle;

	protected override IDictionary<Title, Effect> LoadItems()
	{
		this.NewPageText = this.GetNewPageText;
		var effects = new Effects();
		var effectsPage = this.Site.LoadPage(this.effectsTitle) ?? throw new InvalidOperationException();
		var rows = EffectsRowFinder().Matches(effectsPage.Text);
		LoadExistingEffects(effects, rows);
		var newEfects = LoadNewEffects(effects);
		var retval = new Dictionary<Title, Effect>();
		foreach (var effect in newEfects)
		{
			if (effect.Link.Length > 0)
			{
				var siteLink = SiteLink.FromText(this.Site, effect.Link);
				retval.TryAdd(siteLink.Title, effect);
			}

			if (effect.EditorId.Length > 0)
			{
				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + effect.EditorId);
				retval.TryAdd(title, effect);
			}
		}

		effects.Sort();

		var startPos = rows[0].Index;
		var endPos = effectsPage.Text.IndexOf("|}", startPos, StringComparison.Ordinal);
		effectsPage.Text = effectsPage.Text.Remove(startPos, endPos - startPos);

		var sb = new StringBuilder();
		foreach (var effect in effects)
		{
			sb.Append("|-\n");
			if (effect.Link.Length == 0)
			{
				sb.Append("|NO NAME");
			}
			else
			{
				sb
					.Append("|{{Anchor|")
					.Append(effect.Link)
					.Append("}}");
			}

			sb
				.Append(effect.ModTemplate)
				.Append(" || {{ID|")
				.Append(effect.FormId)
				.Append("}} || ")
				.Append(effect.EditorId)
				.Append(" || ")
				.Append(effect.Description)
				.Append('\n');
		}

		effectsPage.Text = effectsPage.Text.Insert(startPos, sb.ToString());
		this.Pages.Add(effectsPage);

		return retval;
	}
	#endregion

	#region Private Static Methods
	[GeneratedRegex(@"\|-\s+\|\s*(NO NAME|{{Anchor\|(?<Link>\[\[.*?\]\])}})(?<Mod>{{.*?}})?\s*\|\|\s*({{ID\|(?<FormId>[x0-9A-F]{8}.*?)}}|(0x)?(?<FormId>[x0-9A-F]{8}.*?))\s*\|\|\s*(?<EditorId>.*?)\s*\|\|(?<Comment>.*?)$", RegexOptions.ExplicitCapture | RegexOptions.Multiline, 10000)]
	private static partial Regex EffectsRowFinder();

	private static void LoadExistingEffects(Effects effects, IReadOnlyList<Match> rows)
	{
		foreach (var row in rows)
		{
			var link = row.Groups["Link"].Value;
			var mod = row.Groups["Mod"].Value;
			var formid = row.Groups["FormId"].Value.Trim();
			var edid = row.Groups["EditorId"].Value.Trim();
			var desc = row.Groups["Comment"].Value.Trim();
			effects.Add(new Effect(formid, edid, link, mod, desc));
		}
	}

	private static List<Effect> LoadNewEffects(Effects effects)
	{
		var retval = new List<Effect>();
		var effectsFile = new CsvFile(GameInfo.Starfield.ModFolder + "Effects.csv");
		foreach (var row in effectsFile.ReadRows())
		{
			var link = row["Name"];
			if (link.Length > 0)
			{
				link = $"[[SF:{link}|{link}]]";
			}

			var effect = new Effect(
				row["FormID"][2..].TrimEnd(),
				row["EditorID"].Trim(),
				link,
				GameInfo.Starfield.ModTemplate,
				row["Description"].Trim());
			if (!effects.Remove(effect))
			{
				retval.Add(effect);
			}

			effects.Add(effect);
		}

		return retval;
	}
	#endregion

	#region Private Methods
	private string GetNewPageText(Title title, Effect effect)
	{
		var siteLink = SiteLink.FromText(this.Site, effect.Link);
		return "#REDIRECT [[Starfield:Effects#" + siteLink.Text + "]]\n[[Category:Redirects to Broader Subjects]]\n[[Category:Starfield-Effects]]";
	}
	#endregion

	#region Internal Records

	internal record Effect(string FormId, string EditorId, string Link, string ModTemplate, string Description);
	#endregion

	#region Private Classes
	private sealed class Effects() : KeyedCollection<string, Effect>(StringComparer.Ordinal)
	{
		public void Sort()
		{
			var list = (List<Effect>)this.Items;
			list.Sort(EffectSorter);
		}

		protected override string GetKeyForItem(Effect item)
		{
			ArgumentNullException.ThrowIfNull(item);
			return item.EditorId;
		}

		private static int EffectSorter(Effect effect1, Effect effect2)
		{
			var retval = effect1.Link.CompareTo(effect2.Link);
			if (retval == 0)
			{
				retval = effect1.EditorId.CompareTo(effect2.EditorId);
			}

			return retval;
		}
	}
	#endregion
}