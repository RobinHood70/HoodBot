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

	public class ParameterReplacers
	{
		#region Fields
		private readonly Site site;
		private readonly List<ParameterReplacer> generalReplacers = [];
		private readonly IReadOnlyDictionary<Title, Title> globalUpdates;
		private readonly Dictionary<Title, List<ParameterReplacer>> templateReplacers = [];
		private UespNamespaceList? nsList;
		#endregion

		// TODO: Create tags similar to JobInfo that'll tag each method with the site and template it's designed for, so AddAllReplacers can be programmatic rather than a manual list.
		#region Constructors
		internal ParameterReplacers(Site site, IReadOnlyDictionary<Title, Title> linkUpdates)
		{
			ArgumentNullException.ThrowIfNull(site);
			ArgumentNullException.ThrowIfNull(linkUpdates);
			this.site = site;
			this.globalUpdates = linkUpdates;
			this.AddGeneralReplacers(this.GenericIcon, this.GenericImage);
			this.AddTemplateReplacers("Basic NPC Summary", this.BasicNpc);
			this.AddTemplateReplacers("Book Link", this.LoreFirst);
			this.AddTemplateReplacers("Bullet Link", this.BulletLink);
			this.AddTemplateReplacers("Cat Footer", this.CatFooter);
			this.AddTemplateReplacers("Cite Book", this.DefaultLoreFirst);
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
			this.AddTemplateReplacers("Online Furnishing Antiquity/Row", this.AntiquityRow);
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
				currentReplacers = [];
				this.templateReplacers.Add(title, currentReplacers);
			}

			foreach (var replacer in replacers)
			{
				currentReplacers.Add(replacer);
			}
		}

		public void ReplaceAll(Page page, SiteTemplateNode template)
		{
			ArgumentNullException.ThrowIfNull(template);
			foreach (var action in this.generalReplacers)
			{
				action(page, template);
			}

			if (this.templateReplacers.TryGetValue(template.Title, out var replacementActions))
			{
				foreach (var action in replacementActions)
				{
					action(page, template);
				}
			}
		}
		#endregion

		#region Protected Methods
		protected void AntiquityRow(Page page, SiteTemplateNode template)
		{
			var nameParam = template.Find("name", "1");
			var name = nameParam is null
				? page.Title.PageName
				: nameParam.GetValue();
			this.GenericIconWithDefault(page, template, $"ON-icon-lead-{name}.png");
		}

		protected void BasicNpc(Page page, SiteTemplateNode template)
		{
			if (this.NamespaceList.FromTitle(page.Title) is UespNamespace nsPage)
			{
				this.PageNameReplace(nsPage.Parent, template.Find("race"));
			}
		}

		protected void BulletLink(Page page, SiteTemplateNode template)
		{
			ArgumentNullException.ThrowIfNull(page);
			ArgumentNullException.ThrowIfNull(template);
			if ((template.Find(1) ?? template.Find("link")) is not IParameterNode link)
			{
				return;
			}

			var nsParam = template.Find("ns_base", "ns_id");
			if (this.NamespaceList.GetNsBase(nsParam?.GetValue(), page.Title) is not UespNamespace oldNs)
			{
				return;
			}

			var oldTitle = link.GetValue();
			var searchTitle = TitleFactory.FromUnvalidated(page.Site, oldNs.Full + oldTitle);
			if (!this.globalUpdates.TryGetValue(searchTitle, out var toTitle))
			{
				return;
			}

			link.SetValue(toTitle.PageName, ParameterFormat.Copy);
			if (this.NamespaceList.FromTitle(toTitle) is not UespNamespace newNs || !oldNs.Id.OrdinalEquals(newNs.Id))
			{
				return;
			}

			if (!newNs.Id.OrdinalEquals(oldNs.Id))
			{
				if (nsParam == null)
				{
					template.Add("ns_base", newNs.Id);
				}
				else
				{
					nsParam.SetValue(newNs.Id, ParameterFormat.Copy);
				}
			}

			if (toTitle == newNs.MainPage)
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

		protected void GenericIcon(Page page, SiteTemplateNode template) => this.PageNameReplace(page.Site[UespNamespaces.File], template.Find("icon"));

		protected void GenericIconWithDefault(Page page, SiteTemplateNode template, string defaultValue)
		{
			var param = template.Find("icon");
			var addedDefault = false;
			if (param is null)
			{
				defaultValue = defaultValue.Replace("{{PAGENAME}}", page.Title.PageName, StringComparison.Ordinal);
				param = template.Add("icon", defaultValue);
				addedDefault = true;
			}

			this.PageNameReplace(this.site[MediaWikiNamespaces.File], param);
			if (addedDefault && param.GetRaw().OrdinalEquals(defaultValue))
			{
				template.Remove("icon");
			}
		}

		protected void GenericImage(Page page, SiteTemplateNode template)
		{
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("image"));
			this.PageNameReplace(page.Site[MediaWikiNamespaces.File], template.Find("img"));
		}

		protected void Icon(Page page, SiteTemplateNode template)
		{
			var nsParam = template.Find("ns_base", "ns_id");
			if (this.NamespaceList.GetNsBase(nsParam?.GetValue(), page.Title) is UespNamespace oldNs)
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

		protected void DefaultLoreFirst(Page page, SiteTemplateNode template)
		{
			var nsParam = template.Find("ns_base", "ns_id");
			var baseName = nsParam?.GetValue() ?? "Lore";
			var nsBase = this.NamespaceList[baseName];
			if (template.Find(1) is not IParameterNode pageNameParam ||
				!this.globalUpdates.TryGetValue(nsBase.GetTitle(pageNameParam.GetValue()), out var target) ||
				this.NamespaceList.FromTitle(target) is not UespNamespace targetNsBase)
			{
				return;
			}

			pageNameParam.SetValue(target.PageName, ParameterFormat.NoChange);
			template.Remove("ns_base");
			template.Remove("ns_id");
			if (!targetNsBase.Base.OrdinalEquals("Lore"))
			{
				template.Add("ns_base", targetNsBase.Id);
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
			if (this.NamespaceList.FromTitle(page.Title) is UespNamespace nsPage)
			{
				this.PageNameReplace(nsPage.Parent, template.Find("race"));
			}
		}

		protected void PageNameFirst(Page page, SiteTemplateNode template) => this.PageNameReplace(page.Title.Namespace, template.Find(1));
		#endregion

		#region Private Methods
		private void FullPageNameReplace([NotNull] Page page, IParameterNode? param)
		{
			ArgumentNullException.ThrowIfNull(page);
			if (param != null
				&& TitleFactory.FromUnvalidated(page.Site, param.GetValue()) is var from
				&& this.globalUpdates.TryGetValue(from, out var to))
			{
				param.SetValue(to.Namespace.DecoratedName() + to.PageName, ParameterFormat.Copy);
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
				Title from = TitleFactory.FromUnvalidated(page.Title.Namespace, param.GetValue());
				if (this.globalUpdates.TryGetValue(from, out var to) && from.Namespace == to.Namespace)
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
				var paramValue = param.GetValue();
				var findTitle = TitleFactory.FromUnvalidated(ns, paramValue);
				if (this.globalUpdates.TryGetValue(findTitle, out var target) &&
					target.Namespace == ns)
				{
					param.SetValue(target.PageName, ParameterFormat.Copy);
				}
			}
		}
		#endregion
	}
}