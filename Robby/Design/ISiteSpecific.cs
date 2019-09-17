namespace RobinHood70.Robby.Design
{
	/// <summary>Interface applied to site-specific classes.</summary>
	public interface ISiteSpecific
	{
		/// <summary>Gets the site the item belongs to.</summary>
		/// <value>The site.</value>
		Site Site { get; }
	}
}
