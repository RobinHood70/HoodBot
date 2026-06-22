namespace RobinHood70.Robby;

using System;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;

/// <summary>Stores a MediaWiki page along with associated data.</summary>
/// <seealso cref="Page" />
public sealed class MessageInfo
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="MessageInfo"/> class.</summary>
	/// <param name="item">The AllMessagesItem to populate this instance from.</param>
	public MessageInfo(AllMessagesItem item)
	{
		ArgumentNullException.ThrowIfNull(item);
		this.Customized = item.Flags.HasAnyFlag(MessageFlags.Customized);
		this.DefaultMissing = item.Flags.HasAnyFlag(MessageFlags.DefaultMissing);
		this.IsMissing = item.Flags.HasAnyFlag(MessageFlags.Missing);
		this.DefaultMessage = item.Default;
		this.NormalizedName = item.NormalizedName;
		this.Text = item.Content ?? item.Default ?? string.Empty;
	}
	#endregion

	#region Public Properties

	/// <summary>Gets a value indicating whether this <see cref="MessageInfo"/> has been customized.</summary>
	/// <value><see langref="true" /> if customized; otherwise, <see langref="false" />.</value>
	public bool Customized { get; }

	/// <summary>Gets the default message.</summary>
	/// <value>The default message.</value>
	/// <remarks>If the message has been loaded via any of the <see cref="Site" /> GetMessage-related methods, this will contain the default version of the message, even if it has since been customized.</remarks>
	public string? DefaultMessage { get; }

	/// <summary>Gets a value indicating whether the default value was missing.</summary>
	/// <value><see langref="true" /> if the default value is missing; otherwise, <see langref="false" />.</value>
	public bool DefaultMissing { get; }

	/// <summary>Gets a value indicating whether this message is missing. If <see langword="true"/>, the message does not exist on the wiki and the text is an empty string. Note that a message may be missing even if it has a default value, if the default value is not present on the wiki.
	/// </summary>
	public bool IsMissing { get; }

	/// <summary>Gets the normalized name of the message.</summary>
	/// <value>The normalized name.</value>
	/// <remarks>For messages, spaces will be replaced by underscores, and the first letter will be converted to lower-case.</remarks>
	public string? NormalizedName { get; }

	/// <summary>Gets the text of the message. If the message is customized, this will be the customized text; otherwise, it will be the default text. If the message is missing, this will be an empty string.</summary>
	public string Text { get; }
	#endregion
}