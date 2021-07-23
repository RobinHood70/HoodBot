namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	[Flags]
	public enum ReplacementActions
	{
		None = 0,
		Move = 1,
		Edit = 1 << 1,
		Propose = 1 << 2,
		Skip = 1 << 3,
		UpdateLinks = 1 << 4,
	}

	public sealed class Replacement : IEquatable<Replacement>
	{
		#region Constructors
		public Replacement(Site site, string from, string to)
			: this(Title.FromName(site, from), Title.FromName(site, to))
		{
		}

		[JsonConstructor]
		public Replacement(Title from, Title to)
		{
			ThrowNull(from, nameof(from));
			ThrowNull(to, nameof(to));
			this.From = from;
			this.To = to;
		}
		#endregion

		#region Public Properties
		public ReplacementActions Actions { get; set; }

		public Title From { get; }

		public bool IsSameNamespace => this.From.Namespace == this.To.Namespace;

		public bool NoChange => this.From == this.To;

		public string? Reason { get; set; }

		public Title To { get; set; }
		#endregion

		#region Public Methods
		public bool Equals(Replacement? other) => !(other is null) && this.From == other.From && this.To == other.To;
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => this.Equals(obj as Replacement);

		public override int GetHashCode() => this.From?.GetHashCode() ?? 0;

		public override string ToString() => $"{this.Actions}: {this.From.FullPageName} → {this.To.FullPageName}";
		#endregion
	}
}