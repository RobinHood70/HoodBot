namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

	[Flags]
	public enum ReplacementActions
	{
		None = 0,
		Move = 1,
		Edit = 1 << 1,
		Propose = 1 << 2,
		Skip = 1 << 3,
	}

	public sealed class Replacement : IComparable<Replacement>, IEquatable<Replacement>
	{
		#region Constructors
		public Replacement(Site site, string from, string to)
			: this(new Title(site, from), new Title(site, to))
		{
		}

		[JsonConstructor]
		public Replacement(ISimpleTitle from, ISimpleTitle to)
		{
			ThrowNull(from, nameof(from));
			ThrowNull(to, nameof(to));

			if (from == to)
			{
				throw new ArgumentException($"From and to pages cannot be the same: {from.FullPageName()} == {to.FullPageName()}");
			}

			this.From = from;
			this.To = to;
		}
		#endregion

		#region Public Properties
		public ReplacementActions Actions { get; set; }

		public ISimpleTitle From { get; }

		[JsonIgnore]
		public Page? FromPage { get; internal set; }

		public string? Reason { get; set; }

		public ISimpleTitle To { get; set; }

		[JsonIgnore]
		public Page? ToPage { get; set; }
		#endregion

		#region Operators
		public static bool operator ==(Replacement? left, Replacement? right) => left is null ? right is null : left.Equals(right);

		public static bool operator !=(Replacement? left, Replacement? right) => !(left == right);

		public static bool operator <(Replacement? left, Replacement? right) => left is null ? !(right is null) : left.CompareTo(right) < 0;

		public static bool operator <=(Replacement? left, Replacement? right) => left is null || left.CompareTo(right) <= 0;

		public static bool operator >(Replacement? left, Replacement? right) => !(left is null) && left.CompareTo(right) > 0;

		public static bool operator >=(Replacement? left, Replacement? right) => left is null ? right is null : left.CompareTo(right) >= 0;
		#endregion

		#region Public Methods
		public int CompareTo(Replacement? other)
		{
			ThrowNull(other, nameof(other));
			return TitleComparer<ISimpleTitle>.Instance.Compare(this.From, other.From);
		}

		public bool Equals(Replacement? other) => !(other is null) && this.From == other.From; // Nothing else is checked for equality, as multiple values for the same From are invalid.
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => this.Equals(obj as Replacement);

		public override int GetHashCode() => this.From?.GetHashCode() ?? 0;

		public override string ToString() => $"{this.Actions}: {this.From.FullPageName()} → {this.To.FullPageName()}";
		#endregion
	}
}