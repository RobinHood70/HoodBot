#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;

	// TODO: Consider allowing modules to remove inappropriate inputs/outputs from modules when in generator mode. Easiest implementation is probably to allow normal Build, then implement a RemoveForGenerator virtual method that will come along behind after the build and remove inappropriate parameters from the request.
	public abstract class QueryModule<TInput, TOutput> : IQueryModule
		where TInput : class
		where TOutput : class
	{
		#region Static Fields
		private static readonly Regex LimitFinder = new Regex(@"\A(?<module>.*?)limit may not be over (?<limit>[0-9]+)", RegexOptions.Compiled, DefaultRegexTimeout);
		#endregion

		#region Fields
		private readonly int maxItems;
		private readonly int requestedLimit;
		private readonly IPageSetGenerator? pageSetGenerator;
		#endregion

		#region Constructors
		protected QueryModule([NotNull, ValidatedNotNull] WikiAbstractionLayer wal, [NotNull, ValidatedNotNull] TInput input, IPageSetGenerator? pageSetGenerator)
		{
			ThrowNull(wal, nameof(wal));
			ThrowNull(input, nameof(input));
			this.Wal = wal;
			this.SiteVersion = wal.SiteVersion;
			this.Input = input;
			if (input is ILimitableInput limitable)
			{
				this.maxItems = limitable.MaxItems;
				this.requestedLimit = limitable.Limit; // (limitable.Limit == 0 || limitable.MaxItems < limitable.Limit) ? this.maxItems : limitable.Limit;
			}
			else
			{
				this.maxItems = 0;
				this.requestedLimit = 0;
			}

			this.SetItemsRemaining(0);
			this.pageSetGenerator = pageSetGenerator;
		}
		#endregion

		#region Public Properties

		// Unlike Action modules, Query modules cannot take inputs during their BuildRequest phase, so we take them as part of the constructor and store them here until they're needed.
		public TInput Input { get; }

		public int ModuleLimit { get; set; } = int.MaxValue;

		// Unlike Action modules, Query modules cannot return results directly during their Submit phase, so we store them here until the client requests them. Setter is protected so that Prop modules can set current page as needed.
		[DisallowNull]
		public TOutput? Output { get; protected set; }

		public WikiAbstractionLayer Wal { get; }
		#endregion

		#region Public Abstract Properties
		public abstract int MinimumVersion { get; }

		public abstract string Name { get; }
		#endregion

		#region Public Virtual Properties
		public virtual string ContinueName => "continue";

		public virtual bool ContinueParsing => this.ItemsRemaining > 0;

		public virtual string FullPrefix => (this.IsGenerator ? "g" : string.Empty) + this.Prefix;
		#endregion

		#region Protected Properties
		protected bool IsGenerator => this.pageSetGenerator?.Generator == this;

		protected int ItemsRemaining { get; set; }

		protected string Limit
		{
			get
			{
				var limit = this.GetNumericLimit();
				return limit == -1 ? "max" : limit.ToStringInvariant();
			}
		}

		protected int SiteVersion { get; }
		#endregion

		#region Protected Abstract Properties
		protected abstract string ModuleType { get; }

		protected abstract string Prefix { get; }
		#endregion

		#region Protected Virtual Properties

		protected virtual string ResultName => this.Name;
		#endregion

		#region Public Virtual Methods
		public virtual void BuildRequest(Request request)
		{
			ThrowNull(request, nameof(request));
			if (this.ModuleType != null && !this.IsGenerator)
			{
				request.Prefix = string.Empty;
				request.AddToPiped(this.ModuleType, this.Name);
			}

			request.Prefix = this.FullPrefix;
			this.BuildRequestLocal(request, this.Input);
			request.Prefix = string.Empty;
		}

		public virtual void Deserialize(JToken parent)
		{
			if (parent == null)
			{
				throw ParsingExtensions.MalformedException("<Deserialize>", parent);
			}

			this.DeserializeParent(parent);
			if (parent[this.ResultName] is JToken result && result.Type != JTokenType.Null)
			{
				this.DeserializeResult(result);
			}

			if (this.Output == null)
			{
				// If we didn't find the node, or deserialization failed silently for some reason, throw an error.
				throw ParsingExtensions.MalformedException(this.ResultName, parent);
			}
		}

		public virtual bool HandleWarning(string? from, string? text)
		{
			if (string.Equals(from, this.Name, System.StringComparison.Ordinal))
			{
				var match = LimitFinder.Match(text);
				if (match != null && string.Equals(match.Groups["module"].Value, this.FullPrefix, System.StringComparison.Ordinal))
				{
					this.ModuleLimit = int.Parse(match.Groups["limit"].Value, CultureInfo.InvariantCulture);
					return true;
				}
			}

			return false;
		}
		#endregion

		#region Protected Methods
		protected void SetItemsRemaining(int listCount) => this.ItemsRemaining = this.maxItems == 0 ? int.MaxValue : this.maxItems - listCount;
		#endregion

		#region Protected Abstract Methods
		protected abstract void BuildRequestLocal(Request request, TInput input);

		protected abstract void DeserializeResult(JToken? result);
		#endregion

		#region Protected Virtual Methods
		protected virtual void DeserializeParent(JToken parent)
		{
		}

		/// <summary>Gets the appropriate limit to send, attempting to reduce the amount of data returned, if desired and possible.</summary>
		/// <remarks>This only applies to ILimitableModules, but is convenient to have here.</remarks>
		/// <returns>The calculated limit value (or -1 for "max"), taking into account the reported limit from the wiki (if any), the number of items remaining, and a fudge-factor for generators.</returns>
		protected virtual int GetNumericLimit()
		{
			var limit = this.ItemsRemaining;
			if (this.IsGenerator)
			{
				// When used by a generator, ItemsRemaining can fall short due to non-unique pages, which can cause numerous small requests as it tries to fill itself out, so pad it a bit to reduce or, hopefully, eliminate the amount it falls short.
				if (limit < WikiAbstractionLayer.LimitSmall1)
				{
					limit += 5;
				}
				else if (limit != int.MaxValue)
				{
					limit += limit / 10;
				}
			}

			if (this.ModuleLimit == int.MaxValue)
			{
				// If we don't know the limit, use "max" if we're over the smallest possible limit (LIMIT_SML1 in ApiBase.php). This ensures that if we're doing a MaxItems query, we don't get more results than we need.
				if (limit > WikiAbstractionLayer.LimitSmall1)
				{
					limit = -1;
				}
			}
			else if (limit > this.ModuleLimit)
			{
				limit = this.ModuleLimit;
			}

			if (this.requestedLimit > 0 && (limit > this.requestedLimit || limit == -1))
			{
				limit = this.requestedLimit;
			}

			return limit;
		}
		#endregion
	}
}
