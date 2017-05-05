namespace RobinHood70.Robby.Pages
{
	using System;
	using System.Collections.Generic;
	using Design;
	using WallE.Base;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	public class Page : Title, IMessageSource
	{
		#region Constructors
		public Page(Site site, string fullPageName)
			: base(site, fullPageName, null)
		{
		}

		public Page(Site site, string fullPageName, string key)
			: base(site, fullPageName, key)
		{
		}

		public Page(Site site, int ns, string pageName)
			: base(site, ns, pageName)
		{
		}

		public Page(IWikiTitle title)
			: base(title)
		{
		}

		public Page(Site site, string fullPageName, PageLoadOptions loadOptions)
			: base(site, fullPageName)
		{
			ThrowNull(loadOptions, nameof(loadOptions));
			this.LoadOptions = loadOptions;
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<Page, EventArgs> PageLoaded;
		#endregion

		#region Public Properties
		public IReadOnlyList<CategoryTitle> Categories { get; } = new List<CategoryTitle>();

		public bool Invalid { get; protected set; }

		public IReadOnlyList<Title> Links { get; } = new List<Title>();

		public PageLoadOptions LoadOptions { get; set; }

		public bool Missing { get; protected set; } = true;

		public bool New { get; protected set; }

		public IReadOnlyDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

		public bool Redirect { get; protected set; }

		public RevisionCollection Revisions { get; } = new RevisionCollection();

		public DateTime? StartTimestamp { get; protected set; }

		public IReadOnlyCollection<Title> Templates { get; } = new List<Title>();

		public string Text { get; set; }
		#endregion

		#region Public Static Methods
		public static bool Exists(Site site, string fullName) => new Page(site, fullName).Exists();
		#endregion

		#region Public Methods
		public bool Exists()
		{
			this.Load(PageModules.None);
			return !this.Missing && !this.Invalid;
		}

		public bool IsDisambiguation()
		{
			if (this.Site.DisambiguatorAvailable)
			{
				// Disambiguator is 1.21+, so we don't need to worry about the fact that page properties are 1.17+.
				if (!this.LoadOptions.Modules.HasFlag(PageModules.Properties))
				{
					this.Site.PublishWarning(this, CurrentCulture(ModuleNotLoaded, nameof(PageModules.Properties), nameof(this.IsDisambiguation)));
					this.LoadExtras(new PageLoadOptions(PageModules.Properties));
				}

				return this.Properties.ContainsKey("disambiguation");
			}

			if (!this.LoadOptions.Modules.HasFlag(PageModules.Templates))
			{
				this.Site.PublishWarning(this, CurrentCulture(ModuleNotLoaded, nameof(PageModules.Templates), nameof(this.IsDisambiguation)));
				this.LoadExtras(new PageLoadOptions(PageModules.Templates));
			}

			var templates = new HashSet<Title>(this.Templates, new IWikiTitleEqualityComparer());
			templates.IntersectWith(this.Site.DisambiguationTemplates);
			return templates.Count > 0;
		}

		public void Load()
		{
			if (this.LoadOptions == null)
			{
				this.Load(this.Site.DefaultLoadOptions);
			}
			else
			{
				this.Reload();
			}
		}

		public void Load(PageModules pageModules) => this.Load(new PageLoadOptions(pageModules));

		public void Load(PageLoadOptions options)
		{
			this.LoadOptions = options;
			this.Reload();
		}

		public void Move(string toPage, string reason) => this.Move(toPage, reason, true, true, true);

		public void Move(string toPage, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect)
		{
			ThrowNull(toPage, nameof(toPage));
			ThrowNull(reason, nameof(reason));
			if (!this.Site.AllowEditing)
			{
				return;
			}

			var input = new MoveInput(this.FullPageName, toPage)
			{
				IgnoreWarnings = true,
				MoveSubpages = moveSubpages,
				MoveTalk = moveTalk,
				NoRedirect = suppressRedirect,
				Reason = reason,
			};
			var result = this.Site.AbstractionLayer.Move(input);

			var errors = false;
			foreach (var page in result)
			{
				errors |= page.Error != null;
			}

			if (errors)
			{
				this.Site.PublishWarning(this, CurrentCulture(MovePageWarning, this.FullPageName, toPage));
			}

			this.Rename(toPage);
		}

		// Assumes title-related properties have already been provided in the constructor.
		public virtual void Populate(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			var flags = pageItem.Flags;
			var revs = this.Revisions;
			revs.Clear();
			(this.Categories as List<CategoryTitle>).Clear();
			(this.Links as List<Title>).Clear();
			(this.Properties as Dictionary<string, string>).Clear();
			(this.Templates as List<Title>).Clear();

			var categories = this.Categories as List<CategoryTitle>;
			foreach (var category in pageItem.Categories)
			{
				categories.Add(new CategoryTitle(this.Site, category.Title, category.SortKey, category.Hidden));
			}

			var links = this.Links as List<Title>;
			foreach (var link in pageItem.Links)
			{
				links.Add(new Title(this.Site, link.Title));
			}

			this.GetExtraCollections(pageItem);
			foreach (var rev in pageItem.Revisions)
			{
				revs.Add(new Revision(rev));
			}

			this.Invalid = flags.HasFlag(PageFlags.Invalid);
			this.Missing = flags.HasFlag(PageFlags.Missing);
			this.StartTimestamp = this.Site.AbstractionLayer.CurrentTimestamp;

			var info = pageItem.Info;
			if (info == null)
			{
				this.New = false;
				this.Redirect = false;
			}
			else
			{
				this.New = info.Flags.HasFlag(PageInfoFlags.New);
				this.Redirect = info.Flags.HasFlag(PageInfoFlags.Redirect);
				try
				{
					revs.Current = revs[info.LastRevisionId];
					this.Text = revs.Current.Text;
				}
				catch (KeyNotFoundException)
				{
					// We don't set revs.Current here because it will either have been set internally by .Add or set by a successful try.
					this.Text = null;
				}
			}

			this.PopulateCustomResults(pageItem);
		}

		public void Save(string editSummary, bool isMinor) => this.Save(editSummary, isMinor, true, false);

		public void Save(string editSummary, bool isMinor, bool isBotEdit, bool recreateIfJustDeleted)
		{
			if (!this.Site.AllowEditing)
			{
				return;
			}

			var input = new EditInput(this.FullPageName, this.Text)
			{
				BaseTimestamp = this.Revisions?.Current?.Timestamp,
				StartTimestamp = this.StartTimestamp,
				Bot = isBotEdit,
				Minor = isMinor.ToTristate(),
				Recreate = recreateIfJustDeleted,
				Summary = editSummary,
			};
			this.Site.AbstractionLayer.Edit(input);
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void BuildCustomPropertyInputs()
		{
		}

		protected virtual void PopulateCustomResults(PageItem pageItem)
		{
		}

		protected virtual void OnLoaded() => this.PageLoaded?.Invoke(this, EventArgs.Empty);
		#endregion

		#region Private Methods
		private void GetExtraCollections(PageItem page)
		{
			if (page.Properties?.Count > 0)
			{
				var properties = this.Properties as Dictionary<string, string>;
				properties.Clear();
				foreach (var property in page.Properties)
				{
					properties.Add(property.Key, property.Value);
				}
			}

			if (page.Templates.Count > 0)
			{
				var templates = this.Templates as List<Title>;
				templates.Clear();
				foreach (var link in page.Templates)
				{
					templates.Add(new Title(this.Site, link.Title));
				}
			}
		}

		private void LoadExtras(PageLoadOptions options)
		{
			options.Modules &= ~(PageModules.Info | PageModules.Revisions | PageModules.Custom);
			var tempInputs = this.Site.PageBuilder.GetPropertyInputs(options);
			var pageSetInput = new PageSetInput(new[] { this.FullPageName });
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, tempInputs);
			this.GetExtraCollections(result.First());
			this.LoadOptions.Modules |= options.Modules;
		}

		private void Reload()
		{
			var builder = this.Site.PageBuilder;
			var propertyInputs = builder.GetPropertyInputs(this.LoadOptions);
			var pageSetInput = new PageSetInput(new[] { this.FullPageName });
			var result = this.Site.AbstractionLayer.LoadPages(pageSetInput, propertyInputs, builder.CreatePageItem);
			builder.Populate(this, result.First());
			this.OnLoaded();
		}
		#endregion
	}
}