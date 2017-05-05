#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Globalization;
	using System.Text.RegularExpressions;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	// TODO: Consider allowing modules to remove inappropriate inputs/outputs from modules when in generator mode. Easiest implementation is probably to allow normal Build, then implement a RemoveForGenerator virtual method that will come along behind after the build and remove inappropriate parameters from the request.
	public abstract class QueryModule<TInput, TOutput> : IQueryModule
		where TInput : class
		where TOutput : class
	{
		#region Static Fields
		private static Regex limitFinder = new Regex(@"\A(?<module>.*?)limit may not be over (?<limit>[0-9]+)", RegexOptions.Compiled);
		#endregion

		#region Fields
		private int maxItems;
		private int requestedLimit;
		#endregion

		#region Constructors
		protected QueryModule([ValidatedNotNull] WikiAbstractionLayer wal, [ValidatedNotNull] TInput input, TOutput output)
		{
			ThrowNull(wal, nameof(wal));
			ThrowNull(input, nameof(input));
			this.Wal = wal;
			this.SiteVersion = wal.SiteVersion;
			this.Input = input;
			this.SetItemsRemaining(0);
			this.Output = output;
			if (input is ILimitableInput limitable)
			{
				this.maxItems = limitable.MaxItems;
				this.requestedLimit = (limitable.Limit == 0 || limitable.MaxItems < limitable.Limit) ? this.maxItems : limitable.Limit;
			}
			else
			{
				this.maxItems = 0;
				this.requestedLimit = 0;
			}
		}
		#endregion

		#region Public Properties
		public virtual string ContinueName { get; } = "continue";

		public virtual bool ContinueParsing => this.ItemsRemaining > 0;

		// Unlike Action modules, Query modules cannot take inputs during their BuildRequest phase, so we take them as part of the constructor and store them here until they're needed.
		public TInput Input { get; private set; }

		public bool IsGenerator { get; set; } = false;

		public abstract int MinimumVersion { get; }

		public int ModuleLimit { get; set; } = int.MaxValue;

		public abstract string Name { get; }

		// Unlike Action modules, Query modules cannot return results directly during their Submit phase, so we store them here until the client requests them. Setter is protected so that Prop modules can set current page as needed.
		public TOutput Output { get; protected set; }

		public virtual string Prefix => (this.IsGenerator ? "g" : string.Empty) + this.BasePrefix;

		public WikiAbstractionLayer Wal { get; }
		#endregion

		#region Protected Properties
		protected int ItemsRemaining { get; set; }

		protected string Limit
		{
			get
			{
				var limit = this.GetNumericLimit();
				return limit == -1 ? "max" : limit.ToStringInvariant();
			}
		}

		protected int LoopCount { get; set; } = 1;

		protected int SiteVersion { get; }
		#endregion

		#region Protected Abstract Properties
		protected abstract string BasePrefix { get; }

		protected abstract string ModuleType { get; }
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
				request.AddForced(this.ModuleType, new[] { this.Name });
			}

			request.Prefix = this.Prefix;
			this.BuildRequestLocal(request, this.Input);
			request.Prefix = string.Empty;
		}

		public virtual void Deserialize(JToken parent)
		{
			if (parent != null)
			{
				this.DeserializeParent(parent, this.Output);
				var result = parent[this.ResultName];
				if (result != null && result.Type != JTokenType.Null)
				{
					this.DeserializeResult(result, this.Output);
				}
			}
		}

		public virtual bool HandleWarning(string from, string text)
		{
			if (from == this.Name)
			{
				var match = limitFinder.Match(text);
				if (match != null && match.Groups["module"].Value == this.Prefix)
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

		protected abstract void DeserializeResult(JToken result, TOutput output);
		#endregion

		#region Protected Virtual Methods
		protected virtual void DeserializeParent(JToken parent, TOutput output)
		{
		}

		/// <summary>Gets the appropriate limit to send, attempting to reduce the amount of data returned, if desired and possible.</summary>
		/// <remarks>This only applies to ILimitableModules, but is convenient to have here.</remarks>
		/// <returns>The calculated limit value (or -1 for "max"), taking into account the reported limit from the wiki (if any), the number of items remaining, and a fudge-factor for generators.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Performs a complex calculation that will often need to rely on a base/derived call chain.")]
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
