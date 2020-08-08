namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using System.Threading.Tasks;

	public struct PauseToken : IEquatable<PauseToken>
	{
		#region Fields
		private readonly PauseTokenSource source;
		#endregion

		#region Constructors
		internal PauseToken(PauseTokenSource source) => this.source = source;
		#endregion

		#region Public Static Properties
		public static PauseToken None { get; }
		#endregion

		#region Public Properties
		public bool IsPaused => this.source?.IsPaused ?? false;
		#endregion

		#region Public Operators
		public static bool operator ==(PauseToken left, PauseToken right) => left.Equals(right);

		public static bool operator !=(PauseToken left, PauseToken right) => !left.Equals(right);
		#endregion

		#region Public Methods
		public bool Equals(PauseToken other) => this.source == other.source;

		public Task WaitWhilePausedAsync() => this.IsPaused ? this.source.WaitWhilePausedAsync() : PauseTokenSource.CompletedTask;
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => obj is PauseToken token && this.Equals(token);

		public override int GetHashCode() => this.source?.GetHashCode() ?? 0;
		#endregion
	}
}