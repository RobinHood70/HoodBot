namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public delegate void ParameterReplacer(Page page, SiteTemplateNode template);

	/// <summary>Underlying job to move pages. This partial class contains the parameter replacer methods, while the other one contains the job logic.</summary>
	public class ParameterReplacers : Dictionary<ISimpleTitle, ICollection<ParameterReplacer>>
	{
		#region Fields
		private readonly UespNamespaceList uespNamespaceList;
		#endregion

		// TODO: Create tags similar to JobInfo that'll tag each method with the site and template it's designed for, so AddAllReplacers can be programmatic rather than a manual list.
		#region Constructors
		public ParameterReplacers(MovePagesJob job)
			: base(SimpleTitleEqualityComparer.Instance)
		{
			this.Site = job.Site;
			this.Replacements = job.Replacements;
			this.uespNamespaceList = new UespNamespaceList(job.Site);
			this.Add("Book Link", this.LoreFirst);
			this.Add("Bullet Link", this.BulletLink);
			this.Add("Cat Footer", this.CatFooter);
			this.Add("Cite Book", this.LoreFirst);
			this.Add("Edit Link", this.FullPageNameFirst);
			this.Add("Lore Link", this.LoreFirst);
			this.Add("Game Book", this.GameBookGeneral);
			this.Add("Pages In Category", this.CategoryFirst);
		}
		#endregion

		#region Public Properties
		public ReplacementCollection Replacements { get; }

		public Site Site { get; }
		#endregion

		#region Public Methods
		public void Add(string name, ParameterReplacer replacer)
		{
			var title = Title.Coerce(this.Site, MediaWikiNamespaces.Template, name);
			if (!this.TryGetValue(title, out var replacers))
			{
				replacers = new List<ParameterReplacer>();
				this.Add(title, replacers);
			}

			replacers.Add(replacer);
		}
		#endregion

		#region Protected Methods
		protected void BulletLink(Page page, SiteTemplateNode template)
		{
			if ((template.Find(1) ?? template.Find("link")) is IParameterNode link)
			{
				var nsBase = template.Find("ns_base", "ns_id");
				var ns = nsBase != null && this.uespNamespaceList.TryGetValue(nsBase.Value.ToValue(), out var uespNamespace)
					? uespNamespace.BaseNamespace
					: page.Namespace;

				var oldTitle = link.Value.ToValue();
				var searchTitle = new Title(ns, oldTitle);
				if (this.Replacements.TryGetValue(searchTitle, out var replacement))
				{
					link.Value.Clear();
					link.SetValue(replacement.To.PageName);

					if (this.uespNamespaceList.FromTitle(replacement.To) is UespNamespace newNs
						&& newNs.BaseNamespace != ns)
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

		protected void GameBookGeneral(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find("lorename"), UespNamespaces.Lore);

		protected void LoreFirst(Page page, SiteTemplateNode template) => this.PageNameReplace(template.Find(1), UespNamespaces.Lore);

		protected void FullPageNameFirst(Page page, SiteTemplateNode template) => this.FullPageNameReplace(page, template.Find(1));
		#endregion

		#region Private Methods
		private void FullPageNameReplace(Page page, IParameterNode? param)
		{
			if (param != null
				&& Title.FromName(page.Site, param.Value.ToValue()) is var title
				&& this.Replacements.TryGetValue(title, out var replacement)
				&& replacement.To is ISimpleTitle toLink)
			{
				param.SetValue(toLink.FullPageName);
			}
		}

		private void PageNameReplace(IParameterNode? param, int ns)
		{
			if (param != null
				&& new Title(this.Site[ns], param.Value.ToValue()) is var title
				&& this.Replacements.TryGetValue(title, out var replacement)
				&& replacement.To is ISimpleTitle toLink
				&& toLink.Namespace == ns)
			{
				param.SetValue(toLink.PageName);
			}
		}
		#endregion
	}
}