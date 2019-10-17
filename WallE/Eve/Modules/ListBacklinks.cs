#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListBacklinks : ListModule<BacklinksInput, BacklinksItem>, IGeneratorModule
	{
		#region Fields
		private readonly BacklinksTypes linkType;
		#endregion

		#region Contructors
		public ListBacklinks(WikiAbstractionLayer wal, BacklinksInput input)
			: this(wal, input, null)
		{
		}

		public ListBacklinks(WikiAbstractionLayer wal, BacklinksInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator) => this.linkType = input.LinkTypes.IsUniqueFlag() ? input.LinkTypes : throw new ArgumentException(EveMessages.InputNonUnique);
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 109;

		public override string Name
		{
			get
			{
				switch (this.linkType)
				{
					default:
					case BacklinksTypes.Backlinks:
						return "backlinks";
					case BacklinksTypes.EmbeddedIn:
						return "embeddedin";
					case BacklinksTypes.ImageUsage:
						return "imageusage";
				}
			}
		}
		#endregion

		#region Protected Override Properties
		protected override string Prefix
		{
			get
			{
				switch (this.linkType)
				{
					default:
					case BacklinksTypes.Backlinks:
						return "bl";
					case BacklinksTypes.EmbeddedIn:
						return "ei";
					case BacklinksTypes.ImageUsage:
						return "iu";
				}
			}
		}
		#endregion

		#region Public Static Methods
		public static ListBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new ListBacklinks(wal, input as BacklinksInput, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, BacklinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("title", input.Title)
				.AddIf("pageid", input.PageId, input.Title == null)
				.Add("namespace", input.Namespace)
				.AddIf("dir", "descending", input.SortDescending)
				.AddIf("redirect", input.Redirect, this.linkType != BacklinksTypes.EmbeddedIn)
				.AddFilterText("filterredir", "redirects", "nonredirects", input.FilterRedirects)
				.Add("limit", this.Limit);
		}

		protected override BacklinksItem? GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new BacklinksItem((int)result.NotNull("ns"), result.SafeString("title"), (long)result.NotNull("pageid"), result["redirect"].AsBCBool(), this.Input.LinkTypes);

			var redirLinks = result["redirlinks"];
			if (redirLinks != null && item.Redirects is List<ITitle> redirects)
			{
				if (redirLinks.Type == JTokenType.Array)
				{
					foreach (var entry in redirLinks)
					{
						redirects.Add(entry.GetWikiTitle());
					}
				}
				else
				{
					// See https://phabricator.wikimedia.org/T73907
					foreach (var entry in redirLinks)
					{
						var first = entry.First;
						if (first != null)
						{
							redirects.Add(first.GetWikiTitle());
						}
					}
				}
			}

			return item;
		}

		// This module does some really funky things with the limits in redirect mode that don't interact well with how I'm dealing with limits. So, if a specific limit isn't set, always return "max" (-1) rather than the actual limit number, because otherwise the limit will actually take effect in an undesired way.
		protected override int GetNumericLimit() => this.Input.Redirect && this.Input.MaxItems == int.MaxValue ? -1 : base.GetNumericLimit();
		#endregion
	}
}
