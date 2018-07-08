namespace RobinHood70.HoodBot.Jobs.Design
{
	using System.Threading.Tasks;

	// Original was a struct, but converted it to a class, since it's not really a value type and should not support anything but reference equality.
	public class PauseToken
	{
		#region Fields
		private readonly PauseTokenSource source;
		#endregion

		#region Constructors
		internal PauseToken(PauseTokenSource source) => this.source = source;
		#endregion

		#region Public Properties
		public bool IsPaused => this.source?.IsPaused ?? false;
		#endregion

		#region Public Methods
		public Task WaitWhilePausedAsync() => this.IsPaused ? this.source.WaitWhilePausedAsync() : PauseTokenSource.CompletedTask;
		#endregion
	}
}