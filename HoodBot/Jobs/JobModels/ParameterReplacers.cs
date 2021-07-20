namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public delegate void ParameterReplacer(Page page, SiteTemplateNode template);

	/// <summary>Underlying job to move pages. This partial class contains the parameter replacer methods, while the other one contains the job logic.</summary>
	public class ParameterReplacers : Dictionary<ISimpleTitle, ICollection<ParameterReplacer>>
	{
		#region Fields
		private readonly MovePagesJob job;
		private readonly UespNamespaceList uespNamespaceList;
		private readonly ICollection<ParameterReplacer> generic = new List<ParameterReplacer>();
		#endregion

		// TODO: Create tags similar to JobInfo that'll tag each method with the site and template it's designed for, so AddAllReplacers can be programmatic rather than a manual list.
		#region Constructors
		public ParameterReplacers(MovePagesJob job)
			: base(SimpleTitleEqualityComparer.Instance)
		{
			ThrowNull(job, nameof(job));
			this.job = job;
			this.uespNamespaceList = new UespNamespaceList(job.Site);
			this.Add("Book Link", this.LoreFirst);
			this.Add("Bullet Link", this.BulletLink);
			this.Add("Cat Footer", this.CatFooter);
			this.Add("Cite Book", this.LoreFirst);
			this.Add("Edit Link", this.FullPageNameFirst);
			this.Add("ESO Set List", this.PageNameAllNumeric);
			this.Add("Lore Link", this.LoreFirst);
			this.Add("Game Book", this.GameBookGeneral);
			this.Add("Online NPC Summary", this.EsoNpc);
			this.Add("Pages In Category", this.CategoryFirst);
			this.Add("Quest Header", this.GenericIcon);
			this.Add("Soft Redirect", this.FullPageNameFirst);

			this.generic.Add(this.GenericIcon);
			this.generic.Add(this.GenericImage);
		}
		#endregion

		#region Public Methods
		public void Add(string name, params ParameterReplacer[] replacers)
		{
			var title = Title.Coerce(this.job.Site, MediaWikiNamespaces.Template, name);
			if (!this.TryGetValue(title, out var currentReplacers))
			{
				currentReplacers = new List<ParameterReplacer>();
				this.Add(title, currentReplacers);
			}

			foreach (var replacer in replacers)
			{
				currentReplacers.Add(replacer);
			}
		}

		public void ReplaceAll(Page page, SiteTemplateNode template)
		{
			ThrowNull(template, nameof(template));
			foreach (var action in this.generic)
			{
				action(page, template);
			}

			if (this.TryGetValue(template.TitleValue, out var replacementActions))
			{
				foreach (var action in replacementActions)
				{
					action(page, template);
				}
			}
		}
		#endregion

		#region Protected Methods
		protected void BulletLink(Page page, SiteTemplateNode template)
		{
			if ((template.Find(1) ?? template.Find("link")) is IParameterNode link)
			{
				ThrowNull(page, nameof(page));
				var nsBase = template.Find("ns_base", "ns_id");
				var ns = nsBase != null && this.uespNamespaceList.TryGetValue(nsBase.Value.ToValue(), out var uespNamespace)
					? uespNamespace.BaseTitle.Namespace
					: page.Namespace;

				var oldTitle = link.Value.ToValue();
				var searchTitle = new Title(ns, oldTitle);
				if (this.job.Replacements.TryGetValue(searchTitle, out var replacement))
				{
					link.Value.Clear();
					link.SetValue(replacement.To.PageName);

					if (this.uespNamespaceList.FromTitle(replacement.To) is UespNamespace newNs
						&& newNs.BaseTitle.Namespace != ns)
					{
						if (nsBase == null)
						{
							template.Add("ns_base", newNs.Id);
						}
						else
						{
							nsBase.SetValue(newNs.Id);
						}

						if (replacement.To == newNs.MainPage)
						{
							template.Add("altname", oldTitle);
						}
					}
				}
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

		protected void EsoNpc(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find("condition"), UespNamespaces.Online);

		protected void FullPageNameFirst(Page page, SiteTemplateNode template) => this.FullPageNameReplace(page, template.Find(1));

		protected void GameBookGeneral(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find("lorename"), UespNamespaces.Lore);

		protected void GenericIcon(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find("icon"), MediaWikiNamespaces.File);

		protected void GenericImage(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find("image"), MediaWikiNamespaces.File);

		protected void LoreFirst(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find(1), UespNamespaces.Lore);
		#endregion

		#region Private Methods
		private void FullPageNameReplace([NotNull] Page page, IParameterNode? param)
		{
			ThrowNull(page, nameof(page));
			if (param != null
				&& Title.FromName(page.Site, param.Value.ToValue()) is var title
				&& this.job.Replacements.TryGetValue(title, out var replacement)
				&& replacement.To is ISimpleTitle toLink)
			{
				param.SetValue(toLink.FullPageName);
			}
		}

		private void PageNameAllNumeric(Page page, SiteTemplateNode template)
		{
			foreach (var (_, param) in template.GetNumericParameters())
			{
				if (Title.FromName(page.Site, page.Namespace.Id, param.Value.ToValue()) is var title
					&& this.job.Replacements.TryGetValue(title, out var replacement)
					&& replacement.To is ISimpleTitle toLink
					&& replacement.From.Namespace.Id == toLink.Namespace.Id)
				{
					param.SetValue(toLink.PageName);
				}
			}
		}

		private void PageNameReplace(IParameterNode? param, int ns)
		{
			if (param != null
				&& new Title(this.job.Site[ns], param.Value.ToValue()) is var title
				&& this.job.Replacements.TryGetValue(title, out var replacement)
				&& replacement.To is ISimpleTitle toLink
				&& toLink.Namespace == ns)
			{
				param.SetValue(toLink.PageName);
			}
		}

		private void PageNameReplaceAddExtension(IParameterNode? param, int ns, string ext)
		{
			if (param != null
				&& new Title(this.job.Site[ns], param.Value.ToValue() + ext) is var title
				&& this.job.Replacements.TryGetValue(title, out var replacement)
				&& replacement.To is ISimpleTitle toLink
				&& toLink.Namespace == ns)
			{
				param.SetValue(toLink.PageName[0..^ext.Length]);
			}
		}
		#endregion
	}
}