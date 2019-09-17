namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.WikiCommon.Globals;

	public enum ReplacementAction
	{
		Unknown,
		Skip,
		Move,
		ProposeForDeletion,
	}

	public sealed class Replacement : IComparable<Replacement>, IEquatable<Replacement>
	{
		#region Constructors
		public Replacement(Site site, string from, string to)
			: this(new Title(site, from), new Title(site, to))
		{
		}

		public Replacement(Title from, Title to)
		{
			ThrowNull(from, nameof(from));
			ThrowNull(to, nameof(to));

			if (from == to)
			{
				throw new ArgumentException($"From and to pages cannot be the same: {from.FullPageName} == {to.FullPageName}");
			}

			this.From = from;
			this.To = to;
		}

		[JsonConstructor]
		private Replacement()
		{
		}
		#endregion

		#region Public Properties
		public ReplacementAction Action { get; set; } = ReplacementAction.Unknown;

		public string ActionReason { get; set; }

		public string DeleteReason { get; set; }

		public Title From { get; set; }

		public Title To { get; set; }
		#endregion

		#region Operators
		public static bool operator ==(Replacement left, Replacement right) => left is null ? right is null : left.Equals(right);

		public static bool operator !=(Replacement left, Replacement right) => !(left == right);

		public static bool operator <(Replacement left, Replacement right) => left is null ? !(right is null) : left.CompareTo(right) < 0;

		public static bool operator <=(Replacement left, Replacement right) => left is null || left.CompareTo(right) <= 0;

		public static bool operator >(Replacement left, Replacement right) => !(left is null) && left.CompareTo(right) > 0;

		public static bool operator >=(Replacement left, Replacement right) => left is null ? right is null : left.CompareTo(right) >= 0;
		#endregion

		#region Public Methods
		public int CompareTo(Replacement other)
		{
			ThrowNull(other, nameof(other));
			return TitleComparer<Title>.Instance.Compare(this.From, other.From);
		}

		public bool Equals(Replacement other) => !(other is null) && this.From == other.From; // Nothing else is checked for equality, as multiple values for the same From are invalid.
		#endregion

		#region Public Override Methods
		public override bool Equals(object obj) => this.Equals(obj as Replacement);

		public override int GetHashCode() => this.From.GetHashCode();

		public override string ToString() => $"{this.Action}: {this.From.FullPageName} → {this.To.FullPageName}";
		#endregion
	}
}
