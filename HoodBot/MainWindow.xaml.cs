namespace RobinHood70.HoodBot
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using RobinHood70.HoodBot.ViewModel;

	/// <summary>Interaction logic for MainWindow.xaml.</summary>
	public partial class MainWindow : Window
	{
		private MainViewModel mainViewModel = new MainViewModel();

		public MainWindow()
		{
			Globals.ContactInfo = "robinhood70@live.ca";
			if (Globals.ApplicationDataPath != null)
			{
				// Ignored if the path already exists, so just create it.
				Directory.CreateDirectory(Globals.ApplicationDataPath);
			}

			this.DataContext = this.mainViewModel;
			this.InitializeComponent();
			this.WikiCombo.DataContext = WikiInfoViewModel.Load();
		}

		private void EditWikiList_Click(object sender, RoutedEventArgs e)
		{
			var editWikiWindow = new EditWikiList(this.WikiCombo.DataContext as WikiInfoViewModel);
			editWikiWindow.Show();
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				var random = new Random();
				var numTasks = random.Next(1, 5);
				this.mainViewModel.StartJob();
				Thread.Sleep(1000);
				this.mainViewModel.SetNumberOfTasks(numTasks);
				for (var taskNum = 0; taskNum < numTasks; taskNum++)
				{
					var numLoops = random.Next(1, 100);
					this.mainViewModel.StartTask();
					Thread.Sleep(500);
					this.mainViewModel.SetNumberOfLoops(numLoops);
					for (var taskProgress = 0; taskProgress < numLoops; taskProgress++)
					{
						Thread.Sleep(100);
						this.mainViewModel.IncrementTaskProgress();
					}
				}

				this.mainViewModel.EndJob();
			});
		}
	}
}
