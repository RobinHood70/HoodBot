namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using System.Threading;

	public class AsyncInfo
	{
		public AsyncInfo(CancellationToken cancellationToken, PauseToken pauseToken, IProgress<double> progressMonitor, IProgress<string> statusMonitor)
		{
			this.Cancel = cancellationToken;
			this.Pause = pauseToken;
			this.ProgressMonitor = progressMonitor;
			this.StatusMonitor = statusMonitor;
		}

		public string Source { get; set; }

		public CancellationToken Cancel { get; }

		public PauseToken Pause { get; }

		public IProgress<double> ProgressMonitor { get; set; }

		public IProgress<string> StatusMonitor { get; }

		public AsyncInfo With(IProgress<double> newProgressMonitor) => new AsyncInfo(this.Cancel, this.Pause, newProgressMonitor, this.StatusMonitor);
	}
}
