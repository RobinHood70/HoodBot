namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("Effects", "Starfield")]
internal sealed partial class SFEffects(JobManager jobManager) : CreateOrUpdateJob<SFEffects.Effect>(jobManager)
{
	#region Fields
	private readonly Title effectsTitle = TitleFactory.FromUnvalidated(jobManager.Site, "Starfield:Effects");
	#endregion

	#region Protected Override Properties
	protected override string? GetDisambiguator(Effect item) => "effect";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update effects";

	protected override TitleDictionary<Effect> GetExistingItems() => [];

	protected override void GetExternalData()
	{
		// NOTE: This was a hasty conversion to the new format that just stuffs everything in GetExternalData(). If used again in the future, it should probably be separated into its proper GetExternal/GetExisting/GetNew components.
		var effects = new Effects();
		var effectsPage = this.Site.LoadPage(this.effectsTitle) ?? throw new InvalidOperationException();
		var rows = EffectsRowFinder().Matches(effectsPage.Text);
		LoadExistingEffects(effects, rows);
		var newEffects = LoadNewEffects(effects);
		if (newEffects.Count == 0)
		{
			return;
		}

		foreach (var effect in newEffects)
		{
			if (effect.Link.Length > 0)
			{
				var siteLink = SiteLink.FromText(this.Site, effect.Link);
				this.Items.TryAdd(siteLink.Title, effect);
			}

			if (effect.EditorId.Length > 0)
			{
				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + effect.EditorId);
				this.Items.TryAdd(title, effect);
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
	}

	protected override TitleDictionary<Effect> GetNewItems() => [];

	protected override string GetNewPageText(Title title, Effect effect)
	{
		var siteLink = SiteLink.FromText(this.Site, effect.Link);
		return "#REDIRECT [[Starfield:Effects#" + siteLink.Text + "]]\n[[Category:Redirects to Broader Subjects]]\n[[Category:Starfield-Effects]]";
	}

	protected override void ItemPageLoaded(SiteParser parser, Effect item)
	{
		if (!parser.Page.IsRedirect)
		{
			throw new InvalidOperationException("Page is not a redirect!");
		}

		if (parser.LinkNodes.First() is not ILinkNode linkNode ||
			linkNode.GetTitle(parser.Site) != this.effectsTitle)
		{
			throw new InvalidOperationException("Redirect does not point to Effects page!");
		}
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
		var csvName = GameInfo.Starfield.ModFolder + "Effects.csv";
		if (!File.Exists(csvName))
		{
			return retval;
		}

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

	#region Internal Records

	internal sealed record Effect(string FormId, string EditorId, string Link, string ModTemplate, string Description);
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