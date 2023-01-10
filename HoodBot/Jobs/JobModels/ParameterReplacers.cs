namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public delegate void ParameterReplacer(Page page, SiteTemplateNode template);

	/// <summary>Underlying job to move pages. This partial class contains the parameter replacer methods, while the other one contains the job logic.</summary>
	public class ParameterReplacers
	{
		#region Fields
		private readonly Site site;
		private readonly List<ParameterReplacer> generalReplacers = new();
		private readonly IReadOnlyDictionary<Title, Title> globalUpdates;
		private readonly Dictionary<Title, List<ParameterReplacer>> templateReplacers = new(SimpleTitleComparer.Instance);
		private UespNamespaceList? nsList;
		#endregion

		// TODO: Create tags similar to JobInfo that'll tag each method with the site and template it's designed for, so AddAllReplacers can be programmatic rather than a manual list.
		#region Constructors
		internal ParameterReplacers(Site site, IReadOnlyDictionary<Title, Title> linkUpdates)
		{
			this.site = site.NotNull();
			this.globalUpdates = linkUpdates.NotNull();

			this.AddGeneralReplacers(this.GenericIcon, this.GenericImage);
			this.AddTemplateReplacers("Basic NPC Summary", this.BasicNpc);
			this.AddTemplateReplacers("Book Link", this.LoreFirst);
			this.AddTemplateReplacers("Bullet Link", this.BulletLink);
			this.AddTemplateReplacers("Cat Footer", this.CatFooter);
			this.AddTemplateReplacers("Cite Book", this.LoreFirst);
			this.AddTemplateReplacers("Edit Link", this.FullPageNameFirst);
			this.AddTemplateReplacers("ESO Antiquity Furnishing", this.EsoAntiquityReplacer);
			this.AddTemplateReplacers("ESO Set List", this.PageNameAllNumeric);
			this.AddTemplateReplacers("Furnishing Crafting Entry", this.FurnishingLink);
			this.AddTemplateReplacers("Furnishing General Entry", this.PageNameFirst);
			this.AddTemplateReplacers("Furnishing Link", this.FurnishingLink);
			this.AddTemplateReplacers("Furnishing Luxury Entry", this.FurnishingLink);
			this.AddTemplateReplacers("Furnishing Recipe Link", this.FurnishingLink);
			this.AddTemplateReplacers("Furnishing Recipe Short", this.FurnishingLink);
			this.AddTemplateReplacers("Game Book", this.GameBookGeneral);
			this.AddTemplateReplacers("Icon", this.Icon);
			this.AddTemplateReplacers("Lore Entry", this.GenericImage);
			this.AddTemplateReplacers("Lore Link", this.LoreFirst);
			this.AddTemplateReplacers("Multiple Images", this.MultipleImages);
			this.AddTemplateReplacers("NPC Summary", this.NpcSummary);
			this.AddTemplateReplacers("Online Furnishing Summary", this.GenericImage);
			this.AddTemplateReplacers("Online NPC Summary", this.EsoNpc);
			this.AddTemplateReplacers("Pages In Category", this.CategoryFirst);
			this.AddTemplateReplacers("Quest Header", this.GenericIcon);
			this.AddTemplateReplacers("Soft Redirect", this.FullPageNameFirst);
		}
		#endregion

		#region Private Properties
		private UespNamespaceList NamespaceList => this.nsList ??= new UespNamespaceList(this.site);
		#endregion

		#region Public Methods
		public void AddGeneralReplacers(params ParameterReplacer[] replacers)
		{
			foreach (var replacer in replacers)
			{
				this.generalReplacers.Add(replacer);
			}
		}

		public void AddTemplateReplacers(string name, params ParameterReplacer[] replacers)
		{
			var title = TitleFactory.FromUnvalidated(this.site[MediaWikiNamespaces.Template], name);
			if (!this.templateReplacers.TryGetValue(title, out var currentReplacers))
			{
				currentReplacers = new List<ParameterReplacer>();
				this.templateReplacers.Add(title, currentReplacers);
			}

			foreach (var replacer in replacers)
			{
				currentReplacers.Add(replacer);
			}
		}

		public void ReplaceAll(Page page, SiteTemplateNode template)
		{
			template.ThrowNull();
			foreach (var action in this.generalReplacers)
			{
				action(page, template);
			}

			if (this.templateReplacers.TryGetValue(template.TitleValue, out var replacementActions))
			{
				foreach (var action in replacementActions)
				{
					action(page, template);
				}
			}
		}
		#endregion

		#region Protected Methods
		protected void BasicNpc(Page page, SiteTemplateNode template)
		{
			if (this.NamespaceList.FromTitle(page) is UespNamespace nsPage)
			{
				this.PageNameReplace(nsPage.Parent, template.Find("race"));
			}
		}

		protected void BulletLink(Page page, SiteTemplateNode template)
		{
			page.ThrowNull();
			template.ThrowNull();
			if ((template.Find(1) ?? template.Find("link")) is not IParameterNode link)
			{
				return;
			}

			var nsParam = template.Find("ns_base", "ns_id");
			if (this.NamespaceList.GetNsBase(page, nsParam?.Value.ToValue()) is not UespNamespace oldNs)
			{
				return;
			}

			var oldTitle = link.Value.ToValue();
			var searchTitle = TitleFactory.FromUnvalidated(page.Site, oldNs.Full + oldTitle);
			if (!this.globalUpdates.TryGetValue(searchTitle, out var toTitle))
			{
				return;
			}

			link.Value.Clear();
			link.SetValue(toTitle.PageName, ParameterFormat.Copy);
			if (this.NamespaceList.FromTitle(new Title(toTitle)) is not UespNamespace newNs || !string.Equals(oldNs.Id, newNs.Id, StringComparison.Ordinal))
			{
				return;
			}

			if (nsParam == null)
			{
				template.Add("ns_base", newNs.Id);
			}
			else
			{
				nsParam.SetValue(newNs.Id, ParameterFormat.Copy);
			}

			if (toTitle.SimpleEquals(newNs.MainPage))
			{
				template.Add("altname", oldTitle);
			}
		}

		protected void CategoryFirst(Page page, SiteTemplateNode template) => this.PageNameReplace(page.Site[MediaWikiNamespaces.Category], template.Find(1));

		protected void CatFooter(Page page, SiteTemplateNode template)
		{
			foreach (var param in template.FindAll("Prev", "Prev2", "Next", "Next2", "Conc", "Up"))
			{
				this.PageNameReplace(page.Site[UespNamespaces.Category], param);
			}
		}

		protected void EsoAntiquityReplacer(Page page, SiteTemplateNode template) => this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("img"));

		protected void EsoNpc(Page page, SiteTemplateNode template)
		{
			this.PageNameReplace(page.Site[UespNamespaces.Online], template.Find("condition"));
			this.PageNameReplace(page.Site[UespNamespaces.Online], template.Find("race"));
		}

		protected void FullPageNameFirst(Page page, SiteTemplateNode template) => this.FullPageNameReplace(page, template.Find(1));

		protected void FurnishingLink(Page page, SiteTemplateNode template) => this.FurnishingLinkReplace(template.Find(1));

		protected void GameBookGeneral(Page page, SiteTemplateNode template) => this.PageNameReplace(page.Site[UespNamespaces.Lore], template.Find("lorename"));

		protected void GenericIcon(Page page, SiteTemplateNode template) => this.PageNameReplace(page.Namespace, template.Find("icon"));

		protected void GenericImage(Page page, SiteTemplateNode template)
		{
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("img"));
		}

		protected void Icon(Page page, SiteTemplateNode template)
		{
			var nsParam = template.Find("ns_base", "ns_id");
			if (this.NamespaceList.GetNsBase(page, nsParam?.Value.ToValue()) is UespNamespace oldNs)
			{
				var iconName = UespFunctions.IconAbbreviation(oldNs.Id, template);
				var title = TitleFactory.FromUnvalidated(this.site[MediaWikiNamespaces.File], iconName);
				if (this.globalUpdates.TryGetValue(title, out var toTitle))
				{
					var (_, abbr, name, _) = UespFunctions.AbbreviationFromIconName(this.NamespaceList, toTitle.PageName);
					if (template.Find(1) is IParameterNode param1 &&
						template.Find(2) is IParameterNode param2)
					{
						param1.SetValue(abbr, ParameterFormat.Copy);
						param2.SetValue(name, ParameterFormat.Copy);
					}
				}
			}
		}

		protected void LoreFirst(Page page, SiteTemplateNode template) => this.PageNameReplace(page.Site[UespNamespaces.Lore], template.Find(1));

		protected void MultipleImages(Page page, SiteTemplateNode template)
		{
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image1"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image2"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image3"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image4"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image5"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image6"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image7"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image8"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image9"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image10"));
		}

		protected void NpcSummary(Page page, SiteTemplateNode template)
		{
			if (this.NamespaceList.FromTitle(page) is UespNamespace nsPage)
			{
				this.PageNameReplace(nsPage.Parent, template.Find("race"));
			}
		}

		protected void PageNameFirst(Page page, SiteTemplateNode template) => this.PageNameReplace(page.Namespace, template.Find(1));
		#endregion

		#region Private Methods
		private void FullPageNameReplace([NotNull] Page page, IParameterNode? param)
		{
			page.ThrowNull();
			if (param != null
				&& TitleFactory.FromUnvalidated(page.Site, param.Value.ToValue()) is var from
				&& this.globalUpdates.TryGetValue(from, out var to))
			{
				param.SetValue(to.Namespace.DecoratedName + to.PageName, ParameterFormat.Copy);
			}
		}

		private void FurnishingLinkReplace(IParameterNode? param)
		{
			if (param != null)
			{
				this.PageNameReplace(this.site[UespNamespaces.Online], param);
				/* var name = "ON-furnishing-" + param.Value.ToValue() + ".jpg";
				if (TitleFactory.FromUnvalidated(this.site, MediaWikiNamespaces.File, name) is var title
					&& this.replacements.TryGetValue(title, out var replacement)
					&& replacement.To is Title toLink)
				{
					var newValue = toLink.PageName
						.Replace("-item-", "-", StringComparison.Ordinal)
						.Replace("ON-furnishing-", string.Empty, StringComparison.Ordinal)
						.Replace(".jpg", string.Empty, StringComparison.Ordinal);
					param.SetValue(newValue, ParameterFormat.Copy);
					return;
				}

				name = "ON-item-furnishing-" + param.Value.ToValue() + ".jpg";
				if (TitleFactory.FromUnvalidated(this.site, MediaWikiNamespaces.File, name) is var title2
					&& this.replacements.TryGetValue(title2, out var replacement2)
					&& replacement2.To is Title toLink2)
				{
					var newValue = toLink2.PageName
						.Replace("-item-", "-", StringComparison.Ordinal)
						.Replace("ON-furnishing-", string.Empty, StringComparison.Ordinal)
						.Replace(".jpg", string.Empty, StringComparison.Ordinal);
					param.SetValue(newValue, ParameterFormat.Copy);
				} */
			}
		}

		private void PageNameAllNumeric(Page page, SiteTemplateNode template)
		{
			foreach (var (_, param) in template.GetNumericParameters())
			{
				if ((Title)TitleFactory.FromUnvalidated(page.Namespace, param.Value.ToValue()) is var from
					&& this.globalUpdates.TryGetValue(from, out var to)
					&& from.Namespace.Id == to.Namespace.Id)
				{
					param.SetValue(to.PageName, ParameterFormat.Copy);
				}
			}
		}

		private void PageNameReplace(Namespace ns, IParameterNode? param)
		{
			/* var title2 = TitleFactory.FromUnvalidated(ns, param.Value.ToValue());
			var rep2 = this.replacements.TryGetValue(title2, out var replacement2);
			var ns2 = rep2 ? replacement2.To.Namespace : null; */
			if (param != null)
			{
				if (this.globalUpdates.TryGetValue(TitleFactory.FromUnvalidated(ns, param.Value.ToValue().Trim()), out var target) &&
					target.Namespace == ns)
				{
					param.SetValue(target.PageName, ParameterFormat.Copy);
				}
			}
		}
		#endregion
	}
}