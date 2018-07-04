namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;
	using System.Threading;

	public class OneTask : WikiTask
	{
		public OneTask(IWikiTask parent)
			: base(parent)
		{
		}

		public override void Execute()
		{
			this.OnStarted(EventArgs.Empty);
			Thread.Sleep(500);
			var numLoops = new Random().Next(1, 100);
			this.ProgressMaximum = numLoops;
			for (var taskProgress = 0; taskProgress < numLoops; taskProgress++)
			{
				Thread.Sleep(100);
				this.Progress++;
			}

			this.OnFinished(EventArgs.Empty);
		}
	}
}
