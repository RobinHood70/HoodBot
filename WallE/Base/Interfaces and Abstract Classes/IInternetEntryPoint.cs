namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WallE.Clients;

	/// <summary>Represents an abstraction layer that uses the Internet (e.g., one based on api.php or index.php).</summary>
	public interface IInternetEntryPoint
	{
		/// <summary>Gets the internet client.</summary>
		/// <value>The client.</value>
		IMediaWikiClient Client { get; }

		/// <summary>Gets the Uri to the entry point (e.g., api.php or index.php).</summary>
		/// <value>The URI to the entry point.</value>
		Uri EntryPoint { get; }
	}
}