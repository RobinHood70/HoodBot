namespace RobinHood70.HoodBot.Jobs.Design;

using System.Threading;
using System.Threading.Tasks;

// Taken from: https://blogs.msdn.microsoft.com/pfxteam/2013/01/13/cooperatively-pausing-async-methods/
public class PauseTokenSource
{
	internal static readonly Task CompletedTask = Task.FromResult(true);
	private volatile TaskCompletionSource<bool>? paused;

	public bool IsPaused
	{
		get => this.paused != null;
		set
		{
			if (value)
			{
				Interlocked.CompareExchange(ref this.paused, new TaskCompletionSource<bool>(), null);
			}
			else
			{
				// TODO: See if this can be re-written. This seems like it could be optimized to avoid the ugly "while (true)" construct, but since I'm not terribly familiar with multi-threading-specific code, I've left it untouched for now.
				while (true)
				{
					var tcs = this.paused;
					if (tcs == null)
					{
						return;
					}

					if (Interlocked.CompareExchange(ref this.paused, null, tcs) == tcs)
					{
						tcs.SetResult(true);
						break;
					}
				}
			}
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