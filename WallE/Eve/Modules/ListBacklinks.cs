#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static Properties.EveMessages;
	using static WikiCommon.Globals;

	internal class ListBacklinks : ListModule<BacklinksInput, BacklinksItem>, IGeneratorModule
	{
		#region Contructors
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "Validated in base class.")]
		public ListBacklinks(WikiAbstractionLayer wal, BacklinksInput input)
			: base(wal, input)
		{
			if (!input.LinkTypes.IsUniqueFlag())
			{
				throw new ArgumentException(InputNonUnique);
			}

			this.LinkType = input.LinkTypes;
		}
		#endregion

		#region Public Properties
		public BacklinksTypes LinkType { get; set; }
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 109;

		public override string Name
		{
			get
			{
				switch (this.LinkType)
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

		#region Public Override Properties
		protected override string BasePrefix
		{
			get
			{
				switch (this.LinkType)
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
		public static ListBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListBacklinks(wal, input as BacklinksInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, BacklinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("title", input.Title)
				.AddIf("pageid", input.PageId, input.Title == null)
				.Add("ns", input.Namespace)
				.AddIf("dir", "descending", input.SortDescending)
				.AddIf("redirect", input.Redirect, this.LinkType != BacklinksTypes.EmbeddedIn)
				.AddFilterText("filterredir", "redirects", "nonredirects", input.FilterRedirects)
				.Add("limit", this.Limit);
		}

		protected override BacklinksItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new BacklinksItem();
			item.GetWikiTitle(result);
			item.IsRedirect = result["redirect"].AsBCBool();
			item.Type = this.Input.LinkTypes;

			var redirLinks = result["redirlinks"];
			if (redirLinks != null)
			{
				var redirects = item.Redirects as List<ITitle>;
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
						redirects.Add(entry.First.GetWikiTitle());
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
