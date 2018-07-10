namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System;
	using System.Threading.Tasks;

	public class OneTask : WikiTask
	{
		public OneTask(WikiRunner parent)
			: base(parent)
		{
		}

		public override async Task Execute()
		{
			this.OnStarted(EventArgs.Empty);
			await Task.Delay(500);
			var numLoops = new Random().Next(1, 100);
			this.ProgressMaximum = numLoops;
			for (var taskProgress = 0; taskProgress < numLoops; taskProgress++)
			{
				await Task.Delay(100);
				this.Progress++;
				await this.UpdateProgressWriteLine("Quality is job #" + taskProgress.ToString());
			}

			this.OnCompleted(EventArgs.Empty);
		}
	}
}
