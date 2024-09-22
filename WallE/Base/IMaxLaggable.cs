namespace RobinHood70.WallE.Base
{
	/// <summary>Interface to support <c>maxlag</c> functionality.</summary>
	public interface IMaxLaggable
	{
		/// <summary>Gets or sets the <c>maxlag</c> value to be used with the site. A value of 5 is recommended by MediaWiki, but smaller sites may want to use a different value to be more (or less) responsive to lag conditions. The lower the number, the more often the bot will pause in response to lag.</summary>
		/// <value>The maximum lag.</value>
		/// <remarks>This value has no effect on wikis that don't use a replicated database cluster. Once the internal site info has been retrieved, this will stop being emitted in the request if the site doesn't support it. See MediaWiki's <see href="https://www.mediawiki.org/wiki/Manual:Maxlag_parameter">Maxlag parameter</see> for full details.</remarks>
		int MaxLag { get; set; }

		/// <summary>Gets a value indicating whether the site supports <see href="https://www.mediawiki.org/wiki/Manual:Maxlag_parameter">maxlag checking</see>.</summary>
		/// <value><see langword="true" /> if the site supports <c>maxlag</c> checking; otherwise, <see langword="false" />.</value>
		bool SupportsMaxLag { get; }
	}
}