namespace RobinHood70.WallE.Base;

/*
Pre-1.20:
---------

Edit:       per session      from prop=info&intokens=edit
  * The following tokens can also be requested, but will be the same as Edit: Block, Delete, Email, Import, Move, Protect, Unblock
  * The following tokens cannot be requested and expect the Edit token: FileRevert, Undelete
Watch:      per session      from prop=info&intokens=watch
Patrol:     per session      from list=recentchanges&rctoken=patrol
  * In 1.16 was the same as the Edit token
Userrights: per user         from list=users&ustoken=userrights&ususers=...
Rollback:   per user & title from prop=revisions&rvtoken=rollback&rvuser=...&titles=...

1.20-1.23
---------
Edit:       per session      from action=tokens&type=edit
  * The following tokens can also be requested, but will be the same as Edit: Block, Delete, Email, Import, Move, Options, Protect, Unblock
  * The following tokens cannot be requested and expect the Edit token: FileRevert, ImageRotate, Options, SetNotificationTimestamp, Undelete
Watch:      per session      from action=tokens&type=watch
Patrol:     per session      from action=tokens&type=patrol
Userrights: per user         from list=users&ustoken=userrights&ususers=...
Rollback:   per user & title from prop=revisions&rvtoken=rollback&rvuser=...&titles=...

1.24+
-----
All:        per session      from meta=tokens&type=csrf|patrol|rollback|userrights|watch

1.27+
-----
As above, but add "login" to types.
*/

/// <summary>Specifies the methods required by all token managers.</summary>
public interface ITokenManager
{
	#region Methods

	/// <summary>Clears all tokens.</summary>
	void Clear();

	/// <summary>Gets a login token for MediaWiki versions 1.27 and above.</summary>
	/// <returns>A login token.</returns>
	string? LoginToken();

	/// <summary>Gets a rollback token based on the page ID.</summary>
	/// <param name="pageId">The page ID.</param>
	/// <returns>A rollback token.</returns>
	string? RollbackToken(long pageId);

	/// <summary>Gets a rollback token based on the page title.</summary>
	/// <param name="title">The page title.</param>
	/// <returns>A rollback token.</returns>
	string? RollbackToken(string title);

	/// <summary>Gets a session token.</summary>
	/// <param name="type">The type of token to get.</param>
	/// <returns>A session token of the type requested.</returns>
	string? SessionToken(string type);

	/// <summary>Gets a user rights token.</summary>
	/// <param name="userName">The user name.</param>
	/// <returns>A user rights token.</returns>
	string? UserRightsToken(string userName);
	#endregion
}