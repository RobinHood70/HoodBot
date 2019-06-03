namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class OneOffJob : WikiJob
	{
		[JobInfo("One-Off Job")]
		public OneOffJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		protected override void Main()
		{
			this.Site.UserFunctions.DefaultResultDestination = ResultDestination.LocalFile;
			this.Site.UserFunctions.InitializeResult(ResultDestination.LocalFile, null, @"D:\Data\HoodBot\All Books.txt");
			var books = new PageCollection(this.Site);
			books.GetCategoryMembers("Lore-Books", false);
			books.Sort();
			foreach (var book in books)
			{
				this.WriteLine($"== {book.PageName} ==");
				this.WriteLine(book.Text);
				this.WriteLine();
				this.WriteLine();
			}
		}
	}
}
