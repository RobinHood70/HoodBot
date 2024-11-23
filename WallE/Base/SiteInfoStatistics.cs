#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class SiteInfoStatistics
{
	#region Constructors
	internal SiteInfoStatistics(long activeUsers, long admins, long articles, long edits, long images, long jobs, long pages, long users, long views)
	{
		this.ActiveUsers = activeUsers;
		this.Admins = admins;
		this.Articles = articles;
		this.Edits = edits;
		this.Images = images;
		this.Jobs = jobs;
		this.Pages = pages;
		this.Users = users;
		this.Views = views;
	}
	#endregion

	#region Public Properties
	public long ActiveUsers { get; }

	public long Admins { get; }

	public long Articles { get; }

	public long Edits { get; }

	public long Images { get; }

	public long Jobs { get; }

	public long Pages { get; }

	public long Users { get; }

	public long Views { get; }
	#endregion
}