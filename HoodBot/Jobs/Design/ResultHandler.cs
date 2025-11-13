namespace RobinHood70.HoodBot.Jobs.Design;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Text;
using RobinHood70.HoodBot.Properties;

/// <summary>Interface for objects which handle job results.</summary>
/// <remarks>Initializes a new instance of the <see cref="ResultHandler"/> class.</remarks>
public abstract class ResultHandler
{
	#region Fields
	#endregion

	#region Constructors
	protected ResultHandler()
	{
		this.DefaultText = this.ResourceManager.GetString("BotResults", this.Culture) ?? "Bot Results";
		this.Description = this.DefaultText;
	}
	#endregion

	#region Public Properties

	/// <summary>Gets or sets the culture, which controls the language used for the class.</summary>
	/// <value>The culture.</value>
	public CultureInfo Culture { get; set; } = CultureInfo.CurrentUICulture;

	/// <summary>Gets or sets the default text for the <see cref="Description"/>.</summary>
	/// <value>The default text for the <see cref="Description"/>.</value>
	/// <remarks>This property should be set in a derived class to set default text for the description if the caller doesn't customize it. If the derived class fails to set this, it will attempt to return the localized version of "Bot Results" or, failing all else, the English version of it.</remarks>
	[AllowNull]
	public string DefaultText { get; set; }

	/// <summary>Gets or sets the description text.</summary>
	/// <value>The description.</value>
	/// <remarks>Use of this property is handler-specific, typically for edit summaries or e-mail subjects. Some handlers may ignore it altogether, so it should not be used to convey essential information. Setting this property to <see langword="null"/> will cause it to return <see cref="DefaultText"/>.</remarks>
	[AllowNull]
	public string Description { get; set; }

	/// <summary>Gets the <see cref="StringBuilder"/> that holds the result text.</summary>
	/// <value>The string builder.</value>
	public StringBuilder StringBuilder { get; } = new StringBuilder();
	#endregion

	#region Protected Properties

	/// <summary>Gets the resource manager.</summary>
	/// <value>The resource manager.</value>
	protected ResourceManager ResourceManager { get; } = new ResourceManager(typeof(Resources));
	#endregion

	#region Public Abstract Methods

	/// <summary>Saves the results.</summary>
	/// <remarks>This saves the results in whatever way is appropriate to the handler (e.g., saving for pages and files, sending in the case of an e-mail handler).</remarks>
	public abstract void Save();
	#endregion

	#region Public Virtual Methods

	/// <summary>Clears all results and sets the <see cref="Description"/> to <see langword="null"/>.</summary>
	public virtual void Clear()
	{
		this.StringBuilder.Clear();
		this.Description = null;
	}

	/// <summary>Writes the result to the handler's result data.</summary>
	/// <param name="text">The text to add to the result.</param>
	public virtual void Write(string text) => this.StringBuilder.Append(text);
	#endregion
}