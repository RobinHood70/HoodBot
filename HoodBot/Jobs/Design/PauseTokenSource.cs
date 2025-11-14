namespace RobinHood70.HoodBot.Jobs.Design;

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

// Taken from: https://blogs.msdn.microsoft.com/pfxteam/2013/01/13/cooperatively-pausing-async-methods/
public class PauseTokenSource
{
	internal static readonly Task CompletedTask = Task.FromResult(true);
	private volatile TaskCompletionSource<bool>? paused;

	[SuppressMessage("Design", "MA0173:Use LazyInitializer.EnsureInitialize", Justification = "Suggested fix raises another warning.")]
	public bool IsPaused
	{
		get => this.paused != null;
		set
		{
			if (value)
			{
				Interlocked.CompareExchange(ref this.paused, new TaskCompletionSource<bool>(), null);
				return;
			}

			// Wait for this.paused to toggle. Since this.paused is volatile, we need to assign it to a local variable to ensure we're working with the same value throughout the loop.
			var tcs = this.paused;
			while (tcs is not null && Interlocked.CompareExchange(ref this.paused, null, tcs) != tcs)
			{
				tcs = this.paused;
			}

			tcs?.SetResult(true);
		}
	}

	public PauseToken Token => new(this);

	public void ToggleState()
	{
		var current = this.IsPaused;
		this.IsPaused = !current;
	}

	internal Task WaitWhilePausedAsync()
	{
		var cur = this.paused;
		return cur != null ? cur.Task : CompletedTask;
	}
}