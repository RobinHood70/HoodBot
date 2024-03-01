namespace RobinHood70.HoodBot.Jobs
{
	using System.IO;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	[method: JobInfo("One-Off Job")]
	internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
	{
		#region Protected Override Methods
		protected override void Main()
		{
			File.Delete("D:\\AllBooks.txt");
			var books = new PageCollection(this.Site, PageModules.Info | PageModules.Revisions | PageModules.Properties);
			books.GetCategoryMembers("Lore-Books");
			foreach (var book in books)
			{
				if (!book.IsRedirect && (!book.IsDisambiguation ?? false))
				{
					var bookText = $"= {book.Title.PageName} =\n" + book.Text + "\n\n\n";
					File.AppendAllText("D:\\AllBooks.txt", bookText);
				}
			}
		}
		#endregion
	}
}