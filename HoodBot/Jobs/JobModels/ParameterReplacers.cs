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
		private readonly MovePagesJob job;
		private readonly List<ParameterReplacer> generalReplacers = new();
		private readonly ReplacementCollection replacements;
		private readonly Dictionary<Title, List<ParameterReplacer>> templateReplacers = new(SimpleTitleComparer.Instance);
		private UespNamespaceList? nsList;
		#endregion

		// TODO: Create tags similar to JobInfo that'll tag each method with the site and template it's designed for, so AddAllReplacers can be programmatic rather than a manual list.
		#region Constructors
		internal ParameterReplacers(MovePagesJob job, ReplacementCollection replacements)
		{
			this.job = job.NotNull();
			this.replacements = replacements.NotNull();
			this.AddAllReplacers();
		}
		#endregion

		#region Private Properties
		private UespNamespaceList NamespaceList => this.nsList ??= new UespNamespaceList(this.job.Site);
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
			var title = CreateTitle.FromUnvalidated(this.job.Site, MediaWikiNamespaces.Template, name);
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
				this.PageNameReplace(template.Find("race"), nsPage.Parent.Id);
			}
		}

		protected void BulletLink(Page page, SiteTemplateNode template)
		{
			page.ThrowNull();
			if ((template.NotNull().Find(1)
				?? template.Find("link")) is not IParameterNode link)
			{
				return;
			}

			var (oldNs, nsParam) = this.GetNsBase(page, template);
			if (oldNs is null)
			{
				return;
			}

			var oldTitle = link.Value.ToValue();
			var searchTitle = CreateTitle.FromUnvalidated(page.Site, oldNs.Full + oldTitle);
			if (!this.replacements.TryGetValue(searchTitle, out var replacement))
			{
				return;
			}

			link.Value.Clear();
			link.SetValue(replacement.To.PageName, ParameterFormat.Copy);
			if (this.NamespaceList.FromTitle(replacement.To) is not UespNamespace newNs || !string.Equals(oldNs.Id, newNs.Id, StringComparison.Ordinal))
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

			if (replacement.To.SimpleEquals(newNs.MainPage))
			{
				template.Add("altname", oldTitle);
			}
		}

		protected void CategoryFirst(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find(1), MediaWikiNamespaces.Category);

		protected void CatFooter(Page page, SiteTemplateNode template)
		{
			foreach (var param in template.FindAll("Prev", "Prev2", "Next", "Next2", "Conc", "Up"))
			{
				this.PageNameReplace(param, MediaWikiNamespaces.Category);
			}
		}

		protected void EsoNpc(Page page, SiteTemplateNode template)
		{
			this.PageNameReplace(template.Find("condition"), UespNamespaces.Online);
			this.PageNameReplace(template.Find("race"), UespNamespaces.Online);
		}

		protected void FullPageNameFirst(Page page, SiteTemplateNode template) => this.FullPageNameReplace(page, template.Find(1));

		protected void FurnishingLink(Page page, SiteTemplateNode template) => this.FurnishingLinkReplace(template.Find(1));

		protected void FurnishingGeneralEntry(SiteTemplateNode template) => this.FurnishingLinkReplace(template.Find("page"));

		protected void GameBookGeneral(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find("lorename"), UespNamespaces.Lore);

		protected void GenericIcon(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find("icon"), MediaWikiNamespaces.File);

		protected void GenericImage(SiteTemplateNode template) => this.PageNameReplace(template.Find("image"), MediaWikiNamespaces.File);

		protected void Icon(Page page, SiteTemplateNode template)
		{
			var (oldNs, _) = this.GetNsBase(page, template);
			if (oldNs is null)
			{
				return;
			}

			var iconName = UespFunctions.IconAbbreviation(oldNs.Id, template);
			var title = CreateTitle.FromUnvalidated(this.job.Site, MediaWikiNamespaces.File, iconName);
			if (this.replacements.TryGetValue(title, out var replacement))
			{
				var (_, abbr, name, _) = UespFunctions.AbbreviationFromIconName(this.NamespaceList, replacement.To.PageName);
				if (template.Find(1) is IParameterNode param1 &&
					template.Find(2) is IParameterNode param2)
				{
					param1.SetValue(abbr, ParameterFormat.Copy);
					param2.SetValue(name, ParameterFormat.Copy);
				}
			}
		}

		protected void LoreFirst(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find(1), UespNamespaces.Lore);

		protected void NpcSummary(Page page, SiteTemplateNode template)
		{
			if (this.NamespaceList.FromTitle(page) is UespNamespace nsPage)
			{
				this.PageNameReplace(template.Find("race"), nsPage.Parent.Id);
			}
		}
		#endregion

		#region Private Methods
		private void AddAllReplacers()
		{
			// this.AddGeneralReplacers(this.GenericIcon, this.GenericImage);
			this.AddTemplateReplacers("Basic NPC Summary", this.BasicNpc);
			this.AddTemplateReplacers("Book Link", this.LoreFirst);
			this.AddTemplateReplacers("Bullet Link", this.BulletLink);
			this.AddTemplateReplacers("Cat Footer", this.CatFooter);
			this.AddTemplateReplacers("Cite Book", this.LoreFirst);
			this.AddTemplateReplacers("Edit Link", this.FullPageNameFirst);
			this.AddTemplateReplacers("ESO Set List", this.PageNameAllNumeric);
			this.AddTemplateReplacers("Furnishing Crafting Entry", this.FurnishingLink);
			this.AddTemplateReplacers("Furnishing Link", this.FurnishingLink);
			this.AddTemplateReplacers("Furnishing Luxury Entry", this.FurnishingLink);
			this.AddTemplateReplacers("Furnishing Recipe Link", this.FurnishingLink);
			this.AddTemplateReplacers("Furnishing Recipe Short", this.FurnishingLink);
			this.AddTemplateReplacers("Icon", this.Icon);
			this.AddTemplateReplacers("Lore Link", this.LoreFirst);
			this.AddTemplateReplacers("Game Book", this.GameBookGeneral);
			this.AddTemplateReplacers("NPC Summary", this.NpcSummary);
			this.AddTemplateReplacers("Online NPC Summary", this.EsoNpc);
			this.AddTemplateReplacers("Pages In Category", this.CategoryFirst);
			this.AddTemplateReplacers("Quest Header", this.GenericIcon);
			this.AddTemplateReplacers("Soft Redirect", this.FullPageNameFirst);
		}

		private void FullPageNameReplace([NotNull] Page page, IParameterNode? param)
		{
			page.ThrowNull();
			if (param != null
				&& CreateTitle.FromUnvalidated(page.Site, param.Value.ToValue()) is var title
				&& this.replacements.TryGetValue(title, out var replacement)
				&& replacement.To is Title toLink)
			{
				param.SetValue(toLink.FullPageName, ParameterFormat.Copy);
			}
		}

		private void FurnishingLinkReplace(IParameterNode? param)
		{
			if (param != null)
			{
				var name = "ON-furnishing-" + param.Value.ToValue() + ".jpg";
				if (CreateTitle.FromUnvalidated(this.job.Site, MediaWikiNamespaces.File, name) is var title
					&& this.replacements.TryGetValue(title, out var replacement)
					&& replacement.To is Title toLink)
				{
					param.SetValue(toLink.PageName, ParameterFormat.Copy);
					return;
				}

				name = "ON-item-furnishing-" + param.Value.ToValue() + ".jpg";
				if (CreateTitle.FromUnvalidated(this.job.Site, MediaWikiNamespaces.File, name) is var title2
					&& this.replacements.TryGetValue(title2, out var replacement2)
					&& replacement2.To is Title toLink2)
				{
					param.SetValue(toLink2.PageName, ParameterFormat.Copy);
				}
			}
		}

		private (UespNamespace? Namespace, IParameterNode? NsParameter) GetNsBase(Page page, SiteTemplateNode template)
		{
			var nsBase = template.Find("ns_base", "ns_id");
			var ns = nsBase != null && this.NamespaceList.TryGetValue(nsBase.Value.ToValue(), out var uespNamespace)
				? uespNamespace
				: this.NamespaceList.FromTitle(page);

			return (ns, nsBase);
		}

		private void PageNameAllNumeric(Page page, SiteTemplateNode template)
		{
			foreach (var (_, param) in template.GetNumericParameters())
			{
				if (CreateTitle.FromUnvalidated(page.Site, page.Namespace.Id, param.Value.ToValue()) is var title
					&& this.replacements.TryGetValue(title, out var replacement)
					&& replacement.To is Title toLink
					&& replacement.From.Namespace.Id == toLink.Namespace.Id)
				{
					param.SetValue(toLink.PageName, ParameterFormat.Copy);
				}
			}
		}

		private void PageNameReplace(IParameterNode? param, int ns)
		{
			if (param != null
				&& CreateTitle.FromUnvalidated(this.job.Site, ns, param.Value.ToValue()) is var title
				&& this.replacements.TryGetValue(title, out var replacement)
				&& replacement.To.Namespace == ns)
			{
				param.SetValue(replacement.To.PageName, ParameterFormat.Copy);
			}
		}
		#endregion
	}
}