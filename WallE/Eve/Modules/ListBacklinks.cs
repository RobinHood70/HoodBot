namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListBacklinks : ListModule<BacklinksInput, BacklinksItem>, IGeneratorModule
	{
		#region Contructors
		public ListBacklinks(WikiAbstractionLayer wal, BacklinksInput input)
			: this(wal, input, null)
		{
		}

		[SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Other cases are covered by preceding error message.")]
		public ListBacklinks(WikiAbstractionLayer wal, BacklinksInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
			if (!input.LinkTypes.IsUniqueFlag())
			{
				throw new InvalidOperationException(Globals.CurrentCulture(EveMessages.InputNonUnique, nameof(ListAllLinks), input.LinkTypes));
			}

			(this.Prefix, this.Name) = input.LinkTypes switch
			{
				BacklinksTypes.Backlinks => ("bl", "backlinks"),
				BacklinksTypes.EmbeddedIn => ("ei", "embeddedin"),
				BacklinksTypes.ImageUsage => ("iu", "imageusage"),
				_ => throw new InvalidOperationException(GlobalMessages.InvalidSwitchValue)
			};
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 109;

		public override string Name { get; }
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; }
		#endregion

		#region Public Static Methods
		public static ListBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (BacklinksInput)input, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, BacklinksInput input)
		{
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.AddIfNotNull("title", input.Title)
				.AddIf("pageid", input.PageId, input.Title == null)
				.Add("namespace", input.Namespace)
				.AddIf("dir", "descending", input.SortDescending)
				.AddIf("redirect", input.Redirect, input.LinkTypes is not BacklinksTypes.EmbeddedIn and not BacklinksTypes.None)
				.AddFilterText("filterredir", "redirects", "nonredirects", input.FilterRedirects)
				.Add("limit", this.Limit);
		}

		protected override BacklinksItem? GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			List<IApiTitle> redirects = new();
			if (result["redirlinks"] is JToken redirLinks)
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
						if (entry.First is JToken first)
						{
							redirects.Add(first.GetWikiTitle());
						}
					}
				}
			}

			return new BacklinksItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				isRedirect: result["redirect"].GetBCBool(),
				pageId: (long)result.MustHave("pageid"),
				redirects: redirects,
				type: this.Input.LinkTypes);
		}

		// This module does some really funky things with the limits in redirect mode that don't interact well with how I'm dealing with limits. So, if a specific limit isn't set, always return "max" (-1) rather than the actual limit number, because otherwise the limit will actually take effect in an undesired way.
		protected override int GetNumericLimit() => this.Input.Redirect && this.Input.MaxItems == int.MaxValue ? -1 : base.GetNumericLimit();
		#endregion
	}
}
